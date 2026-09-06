using Bib_Hacienda.Clases;
using Bib_Hacienda.Clases.Validaciones;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Estrategias;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Fabricas;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using p_mvcHacienda.Infraestructura;
using p_mvcHacienda.Servicios;

namespace p_mvcHacienda.Composicion
{
    /// <summary>
    /// ADR-01 y ADR-02 · COMPOSITION ROOT (componente investigativo).
    ///
    /// ── Qué es y por qué debe haber exactamente uno ───────────────────────────────
    /// Es el único lugar de la aplicación donde se conoce a la vez qué abstracciones
    /// existen y qué implementaciones las satisfacen, y donde por tanto se construye el
    /// grafo completo de objetos. Está tan cerca del punto de entrada como es posible
    /// —lo invoca Program.Main en una línea— porque es el último momento en que se puede
    /// decidir la configuración sin que ninguna clase de negocio se entere de ella.
    ///
    /// Debe ser UNO SOLO porque cada punto adicional donde se elige una implementación
    /// concreta es un punto donde el desacoplamiento se pierde. El sistema original lo
    /// ilustraba: PersistenciaService construía sus propios proxies internamente
    /// (:41-57), de modo que nadie desde fuera podía sustituir la validación.
    ///
    /// ── Qué había antes ──────────────────────────────────────────────────────────
    /// Program.cs:30,77-81 registraba CLASES CONCRETAS: AddSingleton&lt;PersistenciaService&gt;(),
    /// AddSingleton&lt;PotreroService&gt;()… No había un solo registro de la forma
    /// AddSingleton&lt;IContrato, Implementacion&gt;(). El contenedor funcionaba como
    /// localizador de instancias, no como inyector: el tipo que se pedía era el que se
    /// obtenía, así que no desacoplaba nada. Existían cinco interfaces en el proyecto y
    /// NINGUNA aparecía en un registro ni se usaba como tipo de campo (H-08).
    ///
    /// ── Cómo se evita el antipatrón Service Locator ──────────────────────────────
    /// Ninguna clase recibe IServiceProvider ni le pide instancias en ejecución. Todas
    /// las dependencias entran por constructor y quedan en campos readonly. El único
    /// lugar donde aparece IServiceProvider es dentro de las lambdas de esta clase, que
    /// ES el composition root y por definición sí conoce el contenedor.
    /// </summary>
    public static class RaizComposicion
    {
        public static void Registrar(WebApplicationBuilder builder)
        {
            string datos = Path.Combine(builder.Environment.ContentRootPath, "Datos");

            // ── Infraestructura: contrato → implementación ────────────────────────
            // Aquí es donde ADR-09 queda a una línea de distancia: sustituir
            // RepositorioPotrerosArchivo por RepositorioPotrerosSql no obligaría a tocar
            // ninguna clase de dominio ni de aplicación.
            //
            // Ciclo de vida Singleton: no tienen estado propio, solo la ruta del
            // directorio, así que construirlos por petición no aportaría nada.
            builder.Services.AddSingleton<IRepositorioPotreros>(sp => new RepositorioPotrerosArchivo(
                datos,
                sp.GetRequiredService<PoliticaCapacidadPotrero>(),
                sp.GetServices<IFabricaRes>(),
                new IPublicadorEvento[]
                {
                    sp.GetRequiredService<PublisherPotreroMitad>(),
                    sp.GetRequiredService<PublisherPotreroLleno>(),
                    sp.GetRequiredService<PublisherPesoMin>(),
                    sp.GetRequiredService<PublisherPesoVenta>(),
                }));
            builder.Services.AddSingleton<IRepositorioVentas>(sp => new RepositorioVentasArchivo(
                datos,
                sp.GetServices<IFabricaRes>()));
            builder.Services.AddSingleton<IRepositorioCatalogoVacunas>(_ => new RepositorioCatalogoVacunasArchivo(datos));
            builder.Services.AddSingleton<IRepositorioUsuarios>(_ => new RepositorioUsuariosArchivo(datos));

            // SC-2 no añade repositorio: ADR-11 descartó `Chip` como entidad con archivo
            // propio. El chip se persiste con su res, en cuatro columnas anexadas al final
            // de la línea por MapeadorRes (ADR-12). Siguen siendo cuatro repositorios.

            // ── Validadores: cuatro registros cerrados, cero implementaciones vacías ──
            // Antes eran cuatro proxies de Castle creados perezosamente dentro de
            // PersistenciaService. Ese es el costo aceptado de ADR-03.
            // Singleton porque no tienen estado y son deterministas.
            // Composite (P-05) · Cada IValidador<T> se resuelve como un ValidadorCompuesto<T>
            // que AGRUPA las reglas de ese tipo y corta en la primera que falla. Hoy cada
            // compuesto envuelve su validador existente —cuyo if NO se toca (ValidarRes.cs:19)—;
            // una regla nueva pasa a ser una clase más añadida a esta lista, sin modificar
            // código existente (OCP). Esta raíz es el único lugar donde se lee cómo se compuso
            // una validación. GuardadoValidado sigue dependiendo de IValidador<T> (DIP intacto).
            builder.Services.AddSingleton<IValidador<Potrero>>(_ =>
                new ValidadorCompuesto<Potrero>(new IValidador<Potrero>[] { new ValidadorPotrero() }));
            builder.Services.AddSingleton<IValidador<Res>>(_ =>
                new ValidadorCompuesto<Res>(new IValidador<Res>[] { new ValidadorRes() }));
            builder.Services.AddSingleton<IValidador<Vacuna>>(_ =>
                new ValidadorCompuesto<Vacuna>(new IValidador<Vacuna>[] { new ValidadorVacuna() }));
            builder.Services.AddSingleton<IValidador<Venta>>(_ =>
                new ValidadorCompuesto<Venta>(new IValidador<Venta>[] { new ValidadorVenta() }));

            // SC-2 · Se AGREGA una línea; los cuatro de arriba NO se tocan. Con la
            // jerarquía Validacion original, declarar esta validación habría costado seis
            // archivos y habría añadido cuatro excepciones a clases que no tienen nada que
            // ver con el chip. Es la deuda que pagó ADR-03, cobrada aquí.
            builder.Services.AddSingleton<IValidador<Chip>>(_ =>
                new ValidadorCompuesto<Chip>(new IValidador<Chip>[] { new ValidadorChip() }));

            // ── Fábricas de res · ÚNICO punto a tocar al agregar un tipo de res ──
            //
            // SC-2 NO aparece aquí, y esa ausencia es el contenido de ADR-11: el chip no
            // es un parámetro de construcción, así que ni IFabricaRes ni las tres fábricas
            // se enteran de que existe.
            //
            // ADR-05 · Aquí está la prueba de OCP y también su límite honesto. Agregar
            // un cuarto tipo de res (búfalos de agua, por ejemplo) cuesta hoy 9
            // modificaciones en 8 archivos, ninguna verificada por el compilador. Tras el
            // rediseño cuesta UNA CLASE NUEVA Y UNA LÍNEA AQUÍ para todo el dominio.
            //
            // No es cero, y no se presenta como cero: mientras exista un composition
            // root, alguien tiene que registrar el tipo nuevo. Lo que cambia es que ahora
            // EL COMPILADOR PROTEGE donde importa: una subclase de Res que olvide
            // implementar PesoMinimo no compila. Antes, olvidar PublisherPesoMin.cs:25-27
            // compilaba y producía un animal que nunca aparecía como desnutrido.
            //
            // Sobreviven además tres puntos de PRESENTACIÓN que se conservan a propósito:
            // la clave del diccionario de ResService y las dos vistas de Res. Están
            // declarados en ADRs.md §9.2.1.
            builder.Services.AddSingleton<IFabricaRes, FabricaTernero>();
            builder.Services.AddSingleton<IFabricaRes, FabricaCebon>();
            builder.Services.AddSingleton<IFabricaRes, FabricaNovillo>();

            // ── Fábricas de vacuna · Factory Method (ADR-13, P-01, Actividad 2) ────
            //
            // Mismo costo y mismo mecanismo que las tres de arriba: un tipo de vacuna
            // nuevo es una clase que implementa IFabricaVacuna más esta línea. Sustituye
            // a los cuatro métodos fijos que tenía FabricaVacunas y a las decisiones
            // repetidas en VacunaService, VacunaController y MapeadorVacuna.
            builder.Services.AddSingleton<IFabricaVacuna, FabricaVacunaBacteriana>();
            builder.Services.AddSingleton<IFabricaVacuna, FabricaVacunaViva>();

            // ── Estrategias de venta · Strategy (ADR-14, P-02, Actividad 2) ─────────
            //
            // Qué le pasa al inventario al vender ya no es un `if` dentro de
            // ServicioVenta: es este registro, resuelto por el tipo del artículo
            // vendido, igual que las dos líneas de arriba resuelven IFabricaVacuna. Un
            // artículo con un efecto de inventario nuevo es una clase más y una línea
            // aquí; ServicioVenta no se vuelve a tocar.
            builder.Services.AddSingleton<IEfectoVenta, EfectoVentaRetiroInventario>();
            builder.Services.AddSingleton<IEfectoVenta, EfectoVentaSinEfecto>();

            // ── Publicadores de eventos · Observer (ADR-15, P-03, Actividad 2) ──────
            //
            // Las ocho instanciaciones con `new` que había dentro de Potrero, GestorReses
            // y ServicioVacunacion desaparecen. Cada publicador se registra UNA VEZ como
            // singleton concreto; a quién se le entrega y en qué orden se decide más abajo,
            // al construir GestorReses y ServicioVacunacion — eso ES el patrón: quién
            // colabora con quién se lee aquí, no adivinando entre `new` repartidos.
            builder.Services.AddSingleton<PublisherPotreroMitad>();
            builder.Services.AddSingleton<PublisherPotreroLleno>();
            builder.Services.AddSingleton<PublisherPesoMin>();
            builder.Services.AddSingleton<PublisherPesoVenta>();
            builder.Services.AddSingleton<PublisherVacunacionCompletada>();

            // PublisherVacunaVencida NO implementa IPublicadorEvento: su bool decide si
            // ServicioVacunacion.aplicar_vacuna lanza, así que es una guarda de flujo, no
            // un aviso (ver la ficha de Observer en la Actividad 3.3). Se registra igual,
            // para que tampoco quede un `new` suyo dentro del dominio.
            builder.Services.AddSingleton<PublisherVacunaVencida>();

            builder.Services.AddSingleton<PoliticaCapacidadPotrero>();

            // ── Servicios de dominio · las seis responsabilidades que tenía Hacienda ──
            builder.Services.AddSingleton<GestorPotreros>();

            // GestorReses necesita DOS listas de avisos distintas (alta de res dispara
            // cuatro; alimentar dispara dos), así que no se puede dejar que el contenedor
            // resuelva IEnumerable<IPublicadorEvento> por sí solo: daría la MISMA lista
            // completa a los dos parámetros. El orden de cada lista es el orden de
            // disparo — mitad, lleno, peso mínimo, peso de venta para el alta, tal como
            // exigía Potrero.cs:135-138 del Reto 1.
            builder.Services.AddSingleton<GestorReses>(sp => new GestorReses(
                sp.GetRequiredService<Hacienda>(),
                sp.GetRequiredService<GestorPotreros>(),
                sp.GetRequiredService<PoliticaCapacidadPotrero>(),
                sp.GetServices<IFabricaRes>(),
                avisosAltaDeRes: new IPublicadorEvento[]
                {
                    sp.GetRequiredService<PublisherPotreroMitad>(),
                    sp.GetRequiredService<PublisherPotreroLleno>(),
                    sp.GetRequiredService<PublisherPesoMin>(),
                    sp.GetRequiredService<PublisherPesoVenta>(),
                },
                avisosAlimentacion: new IPublicadorEvento[]
                {
                    sp.GetRequiredService<PublisherPesoMin>(),
                    sp.GetRequiredService<PublisherPesoVenta>(),
                }));

            builder.Services.AddSingleton<ServicioVenta>();
            builder.Services.AddSingleton<FabricaVacunas>();

            // Un solo aviso en la lista (vacunación completada); PublisherVacunaVencida
            // entra por su propio parámetro, no por esta lista.
            builder.Services.AddSingleton<ServicioVacunacion>(sp => new ServicioVacunacion(
                sp.GetRequiredService<Hacienda>(),
                sp.GetRequiredService<GestorPotreros>(),
                sp.GetRequiredService<PublisherVacunaVencida>(),
                new IPublicadorEvento[] { sp.GetRequiredService<PublisherVacunacionCompletada>() }));

            // ── Secuencia validar → persistir ────────────────────────────────────
            builder.Services.AddSingleton<GuardadoValidado>();

            // ── Raíz de agregados en memoria ─────────────────────────────────────
            // Singleton porque es el estado compartido de toda la aplicación. Ya lo era
            // (Program.cs:33) y cambiarlo alteraría el comportamiento observable.
            builder.Services.AddSingleton<Hacienda>(sp => CargadorInicial.Cargar(sp));

            // ── Servicios de aplicación ──────────────────────────────────────────
            // Singleton por consistencia con Hacienda: hacerlos Scoped obligaría a que
            // Hacienda también lo fuera, y eso sí cambiaría el comportamiento.
            builder.Services.AddSingleton<PotreroService>();
            builder.Services.AddSingleton<ResService>();
            builder.Services.AddSingleton<VacunaService>();
            builder.Services.AddSingleton<VentaService>();
            builder.Services.AddSingleton<UsuarioService>(sp =>
            {
                var usuarioService = new UsuarioService(sp.GetRequiredService<IRepositorioUsuarios>());
                usuarioService.CargarUsuarios();
                return usuarioService;
            });

            // ── Lo que el contenedor NO construye ────────────────────────────────
            // Res, Vacuna, Venta y Potrero no se registran nunca: son entidades con
            // identidad y datos de negocio, no servicios. Se crean cuando el usuario da
            // de alta un animal o registra una venta, con valores que el contenedor no
            // puede conocer. La regla que separa ambos mundos: el contenedor construye
            // lo que tiene comportamiento y no tiene identidad; las fábricas construyen
            // lo que tiene identidad.
        }
    }
}
