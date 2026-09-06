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
using System.Reflection;
using static Bib_Hacienda.Clases.Potrero;
using static Bib_Hacienda.Clases.Viva;

namespace Caracterizacion.Rediseno
{
    /// <summary>
    /// Los mismos casos de ADRs.md §8.6, ejecutados contra el sistema REDISEÑADO.
    ///
    /// Compárese <see cref="Componer"/> con el del arnés de la línea base: allí hay que
    /// falsear un IHttpContextAccessor y un IWebHostEnvironment para que el sistema
    /// devuelva los mensajes que ve el operario, porque PersistenciaService leía el
    /// resultado de la validación de HttpContext.Items (H-14). Aquí no hace falta
    /// ninguna de las dos cosas: el dominio y la aplicación no saben que existe la web.
    /// Esa diferencia ES la inversión DE-2 hecha visible.
    /// </summary>
    public sealed class Escenarios
    {
        // Fechas fijas y lejanas: así PublisherVacunaVencida cae siempre en su rama
        // estable ("es válida (vence el ...)") y la salida no depende del día.
        private static readonly DateTime Venc = new DateTime(2030, 12, 31);
        private static readonly DateTime Aplic = new DateTime(2030, 1, 15);

        private readonly Registro _r;
        private readonly string _raizContenido;

        private Hacienda _hacienda;
        private FabricaVacunas _fabricaVacunas;
        private IRepositorioPotreros _repositorioPotreros;
        private IRepositorioVentas _repositorioVentas;
        private IRepositorioCatalogoVacunas _repositorioCatalogo;
        private IRepositorioUsuarios _repositorioUsuarios;
        private GuardadoValidado _guardado;
        private PotreroService _potreroService;
        private ResService _resService;
        private VacunaService _vacunaService;
        private VentaService _ventaService;
        private UsuarioService _usuarioService;

        public Escenarios(Registro registro, string raizContenido)
        {
            _r = registro;
            _raizContenido = raizContenido;
            Componer();
        }

        /// <summary>
        /// Equivalente de RaizComposicion sin servidor web. Es literalmente el mismo
        /// grafo de objetos que registra el composition root de la aplicación.
        /// </summary>
        private void Componer()
        {
            string datos = Path.Combine(_raizContenido, "Datos");

            // Las tres fábricas de res: el único punto que habría que tocar para
            // incorporar un cuarto tipo de ganado al dominio.
            var fabricas = new IFabricaRes[] { new FabricaTernero(), new FabricaCebon(), new FabricaNovillo() };
            var fabricasVacuna = new IFabricaVacuna[] { new FabricaVacunaBacteriana(), new FabricaVacunaViva() };
            var efectosVenta = new IEfectoVenta[] { new EfectoVentaRetiroInventario(), new EfectoVentaSinEfecto() };
            var politica = new PoliticaCapacidadPotrero();

            // Observer (P-03, Actividad 2) · mismo registro que RaizComposicion.
            var publisherMitad = new PublisherPotreroMitad();
            var publisherLleno = new PublisherPotreroLleno();
            var publisherPesoMin = new PublisherPesoMin();
            var publisherPesoVenta = new PublisherPesoVenta();
            var publisherVacunacionCompletada = new PublisherVacunacionCompletada();
            var publisherVacunaVencida = new PublisherVacunaVencida();

            var avisosAltaDeRes = new IPublicadorEvento[] { publisherMitad, publisherLleno, publisherPesoMin, publisherPesoVenta };
            var avisosAlimentacion = new IPublicadorEvento[] { publisherPesoMin, publisherPesoVenta };
            var avisosVacunacion = new IPublicadorEvento[] { publisherVacunacionCompletada };

            _repositorioPotreros = new RepositorioPotrerosArchivo(datos, politica, fabricas, avisosAltaDeRes);
            _repositorioVentas = new RepositorioVentasArchivo(datos, fabricas);
            _repositorioCatalogo = new RepositorioCatalogoVacunasArchivo(datos);
            _repositorioUsuarios = new RepositorioUsuariosArchivo(datos);

            _guardado = new GuardadoValidado(
                _repositorioPotreros, _repositorioVentas, _repositorioCatalogo,
                new ValidadorCompuesto<Potrero>(new IValidador<Potrero>[] { new ValidadorPotrero() }),
                new ValidadorCompuesto<Res>(new IValidador<Res>[] { new ValidadorRes() }),
                new ValidadorCompuesto<Vacuna>(new IValidador<Vacuna>[] { new ValidadorVacuna() }),
                new ValidadorCompuesto<Venta>(new IValidador<Venta>[] { new ValidadorVenta() }));

            _hacienda = new Hacienda();

            // Los cinco servicios de dominio en que se partió Hacienda.
            var gestorPotreros = new GestorPotreros(_hacienda);
            var gestorReses = new GestorReses(_hacienda, gestorPotreros, politica, fabricas, avisosAltaDeRes, avisosAlimentacion);
            var servicioVenta = new ServicioVenta(_hacienda, gestorPotreros, efectosVenta);
            _fabricaVacunas = new FabricaVacunas(_hacienda, fabricasVacuna);
            var servicioVacunacion = new ServicioVacunacion(_hacienda, gestorPotreros, publisherVacunaVencida, avisosVacunacion);

            _potreroService = new PotreroService(_hacienda, gestorPotreros, gestorReses, _guardado);
            _resService = new ResService(_hacienda, gestorPotreros, gestorReses, servicioVenta, _guardado, new ValidadorCompuesto<Chip>(new IValidador<Chip>[] { new ValidadorChip() }));
            _vacunaService = new VacunaService(_hacienda, _fabricaVacunas, servicioVacunacion, gestorPotreros, _guardado, _repositorioCatalogo);
            _ventaService = new VentaService(_hacienda, servicioVenta, _guardado);
            _usuarioService = new UsuarioService(_repositorioUsuarios);
            _usuarioService.CargarUsuarios();
        }

        /// <summary>
        /// En la línea base, esto renovaba el HttpContext para imitar el ciclo por
        /// petición, del que dependía el texto devuelto por cada guardado. Aquí no hay
        /// nada que renovar: el resultado de la validación viaja por el valor de retorno.
        /// Se conservan las llamadas para que los dos arneses sean comparables línea a línea.
        /// </summary>
        private void NuevaPeticion() { }

        // ------------------------------------------------------------------
        // Fixture F1 · datos históricos reales
        // ------------------------------------------------------------------

        public void CargaDeArranque()
        {
            _r.Seccion("FIXTURE F1 · carga de arranque sobre los datos históricos");
            NuevaPeticion();
            _r.Titulo("Arranque · Program.cs:33-74");
            try
            {
                var potreros = _repositorioPotreros.CargarPotreros();
                foreach (var potrero in potreros) _hacienda.AgregarPotrero(potrero);

                _repositorioPotreros.CargarReses(_hacienda.L_potreros);
                _repositorioPotreros.CargarVacunasAplicadas(_hacienda.L_potreros);

                var ventas = _repositorioVentas.CargarVentas(_hacienda.L_potreros);
                foreach (var venta in ventas) _hacienda.RegistrarVenta(venta);

                var vacunas = _repositorioCatalogo.CargarVacunas();
                foreach (var vacuna in vacunas) _hacienda.AgregarVacuna(vacuna);

                _r.Campo("consola", $"Datos cargados: {potreros.Count} potreros, {ventas.Count} ventas, {vacunas.Count} vacunas");
            }
            catch (Exception ex)
            {
                _r.Campo("consola", $"Error al cargar datos: {ex.Message}");
            }

            VolcarInventario("Inventario tras la carga");

            _r.Titulo("Round-trip de guardado sobre los datos históricos");
            NuevaPeticion();
            _r.Campo("GuardarPotreros", Normalizador.Normalizar(GuardadoValidado.Texto(_guardado.GuardarPotreros(_hacienda.L_potreros))));
            _r.Campo("GuardarReses", Normalizador.Normalizar(GuardadoValidado.Texto(_guardado.GuardarReses(_hacienda.L_potreros))));
            _r.Campo("GuardarVacunas", Normalizador.Normalizar(GuardadoValidado.Texto(_guardado.GuardarVacunas(_hacienda.L_vacunas))));
            _r.Campo("GuardarVacunasAplicadas", Normalizador.Normalizar(GuardadoValidado.Texto(_guardado.GuardarVacunasAplicadas(_hacienda.L_potreros))));
            _r.Campo("GuardarVentas", Normalizador.Normalizar(GuardadoValidado.Texto(_guardado.GuardarVentas(_hacienda.L_ventas))));
            _r.Campo("GuardarUsuarios", Normalizador.Normalizar(_repositorioUsuarios.GuardarUsuarios(_usuarioService.ObtenerTodosLosUsuarios())));
        }

        // ------------------------------------------------------------------
        // Fixture F2 · los casos guionizados
        // ------------------------------------------------------------------

        public void Ejecutar()
        {
            _r.Seccion("FIXTURE F2 · los quince casos de caracterización");

            Caso01CrearPotrero();
            Caso02AnadirRes();
            Caso03AlimentarPorDebajoDelMinimo();
            Caso04AlimentarHastaPesoDeVenta();
            Caso05CrearVacunas();
            Caso06CompletarEsquemaDeTernero();
            Caso07VenderRes();
            Caso08ListarReses();
            Caso09EdadFueraDeRango();
            Caso10PotreroLleno();
            Caso11PotreroInexistente();
            Caso12NombreDuplicado();
            Caso13LimitesDeVacunacion();
            Caso14DeudaCongeladaD2D3D5();
            Caso15DeudaCongeladaD1();
            Caso15BisDeudaCongeladaD4();
        }

        // --- Camino feliz -------------------------------------------------

        private void Caso01CrearPotrero()
        {
            _r.Seccion("CASO 01 · Crear potrero");
            CrearPotrero("Potrero_Terneros", l_tipos_potreros.ternero);
            CrearPotrero("Potrero_Cebones", l_tipos_potreros.cebon);
            CrearPotrero("Potrero_Novillos", l_tipos_potreros.novillo);
            CrearPotrero("Corral_Lleno", l_tipos_potreros.ternero);
        }

        private void CrearPotrero(string identificacion, l_tipos_potreros tipo)
        {
            NuevaPeticion();
            _r.ComoControladorQueRelanza($"crear potrero '{identificacion}' ({tipo})",
                () => _potreroService.CrearPotrero(identificacion, tipo));
        }

        private void Caso02AnadirRes()
        {
            _r.Seccion("CASO 02 · Añadir res del tipo correcto");
            AnadirRes("Potrero_Terneros", "Pinta", 6, 100);
            AnadirRes("Potrero_Cebones", "Rayo", 20, 200);
            AnadirRes("Potrero_Novillos", "Trueno", 60, 300);
        }

        private void AnadirRes(string potreroId, string nombre, ushort edad, uint peso)
        {
            NuevaPeticion();
            _r.ComoControladorQueRelanza($"añadir res '{nombre}' a '{potreroId}' (edad {edad}, peso {peso})",
                () => _potreroService.AgregarRes(potreroId, nombre, edad, peso));
        }

        private void Caso03AlimentarPorDebajoDelMinimo()
        {
            _r.Seccion("CASO 03 · Alimentar res por debajo del peso mínimo · los tres tipos");
            Alimentar("Potrero_Terneros", "Pinta", 1);
            Alimentar("Potrero_Cebones", "Rayo", 1);
            Alimentar("Potrero_Novillos", "Trueno", 1);
        }

        private void Caso04AlimentarHastaPesoDeVenta()
        {
            _r.Seccion("CASO 04 · Alimentar res hasta el peso de venta · los tres tipos");
            Alimentar("Potrero_Terneros", "Pinta", 149);   // 101 -> 250
            Alimentar("Potrero_Cebones", "Rayo", 219);     // 201 -> 420
            Alimentar("Potrero_Novillos", "Trueno", 249);  // 301 -> 550
        }

        private void Alimentar(string potreroId, string nombreRes, uint cantidad)
        {
            NuevaPeticion();
            _r.ComoControladorQueRelanza($"alimentar '{nombreRes}' en '{potreroId}' con {cantidad}",
                () => _resService.AlimentarRes(potreroId, nombreRes, cantidad));
        }

        private void Caso05CrearVacunas()
        {
            _r.Seccion("CASO 05 · Crear vacuna bacteriana y viva");
            CrearVacunaBacteriana("Bravox", "LoteB1", 3);
            CrearVacunaViva("AtuVac", "LoteV1", enum_l_atenuaciones.Atenuacion20);
        }

        private void CrearVacunaBacteriana(string nombre, string lote, uint periodo)
        {
            NuevaPeticion();
            _r.ComoControladorQueClasifica($"crear vacuna bacteriana '{nombre}' lote '{lote}'",
                () => _vacunaService.CrearVacuna("Bacteriana", nombre, lote, Venc, Aplic, periodo, null), "x");
        }

        private void CrearVacunaViva(string nombre, string lote, enum_l_atenuaciones atenuacion)
        {
            NuevaPeticion();
            _r.ComoControladorQueClasifica($"crear vacuna viva '{nombre}' lote '{lote}' ({atenuacion})",
                () => _vacunaService.CrearVacuna("Viva", nombre, lote, Venc, Aplic, null, atenuacion), "x");
        }

        private void Caso06CompletarEsquemaDeTernero()
        {
            _r.Seccion("CASO 06 · Aplicar vacunas hasta completar el esquema de un ternero");
            CrearVacunaBacteriana("BacA", "LB-A", 2);
            CrearVacunaBacteriana("BacB", "LB-B", 3);
            CrearVacunaBacteriana("BacC", "LB-C", 4);
            CrearVacunaViva("VivA", "LV-A", enum_l_atenuaciones.Atenuacion10);

            AplicarVacuna("Potrero_Terneros", "Pinta", "LB-A");
            AplicarVacuna("Potrero_Terneros", "Pinta", "LB-B");
            AplicarVacuna("Potrero_Terneros", "Pinta", "LB-C");
            AplicarVacuna("Potrero_Terneros", "Pinta", "LV-A");
        }

        private void AplicarVacuna(string potreroId, string nombreRes, string lote)
        {
            NuevaPeticion();
            _r.ComoControladorQueClasifica($"aplicar lote '{lote}' a '{nombreRes}' en '{potreroId}'",
                () => _vacunaService.AplicarVacuna(potreroId, nombreRes, lote), "x");
        }

        private void Caso07VenderRes()
        {
            _r.Seccion("CASO 07 · Vender res");
            NuevaPeticion();
            _r.ComoControladorQueRelanza("vender 'Trueno' de 'Potrero_Novillos' por 2500000",
                () => _resService.VenderRes("Potrero_Novillos", "Trueno", 2500000));
            VolcarVentas();
        }

        private void Caso08ListarReses()
        {
            _r.Seccion("CASO 08 · Listar reses · estadísticas y tabla");
            NuevaPeticion();
            VolcarEstadisticasDeReses();
            VolcarTablaDeReses();
        }

        // --- Caminos de error ---------------------------------------------

        private void Caso09EdadFueraDeRango()
        {
            _r.Seccion("CASO 09 · Añadir res con edad fuera del rango del potrero");
            AnadirRes("Potrero_Terneros", "Anciana", 30, 200);
        }

        private void Caso10PotreroLleno()
        {
            _r.Seccion("CASO 10 · Añadir res a potrero lleno (150 reses)");
            for (int i = 1; i <= 150; i++)
            {
                string nombre = $"Lleno_{i:D3}";
                if (i == 1 || i == 75 || i == 150)
                {
                    AnadirRes("Corral_Lleno", nombre, 6, 200);
                }
                else
                {
                    NuevaPeticion();
                    _potreroService.AgregarRes("Corral_Lleno", nombre, 6, 200);
                }
            }
            AnadirRes("Corral_Lleno", "Sobrante", 6, 200);
        }

        private void Caso11PotreroInexistente()
        {
            _r.Seccion("CASO 11 · Añadir res a un potrero inexistente · cadena de cuatro wrappers");
            AnadirRes("NoExiste", "Fantasma", 6, 200);
        }

        private void Caso12NombreDuplicado()
        {
            _r.Seccion("CASO 12 · Añadir res con nombre duplicado · pérdida del mensaje original");
            AnadirRes("Potrero_Terneros", "Pinta", 6, 100);
        }

        private void Caso13LimitesDeVacunacion()
        {
            _r.Seccion("CASO 13 · Vacuna ya aplicada y límite por tipo de res");
            CrearVacunaBacteriana("BacA", "LB-D", 2);
            AplicarVacuna("Potrero_Terneros", "Pinta", "LB-D");

            CrearVacunaBacteriana("BacE", "LB-E", 2);
            AplicarVacuna("Potrero_Terneros", "Pinta", "LB-E");
        }

        // --- Deuda congelada ----------------------------------------------

        private void Caso14DeudaCongeladaD2D3D5()
        {
            _r.Seccion("CASO 14 · Deuda congelada D-2, D-3 y D-5");

            NuevaPeticion();
            _r.ComoControladorQueClasifica("D-2 · crear usuario válido 'santi'",
                () => _usuarioService.CrearUsuario("santi", "santi11"), "✅");

            NuevaPeticion();
            _r.ComoControladorQueClasifica("D-3 · crear vacuna con lote duplicado 'LoteB1'",
                () => _vacunaService.CrearVacuna("Bacteriana", "Bravox", "LoteB1", Venc, Aplic, 3, null), "x");

            NuevaPeticion();
            _r.ComoControladorQueRelanza("D-5 · crear lote bacteriano de 3 (mensaje con {nombre} literal)",
                () => _fabricaVacunas.CrearLote("Bacteriana", "LoteBac", "LB-LOTE", Venc, Aplic, 2u, null, 3u));

            NuevaPeticion();
            _r.ComoControladorQueRelanza("D-5 · contraste: lote vivo de 3 (mensaje correcto)",
                () => _fabricaVacunas.CrearLote("Viva", "LoteViv", "LV-LOTE", Venc, Aplic, null, enum_l_atenuaciones.Atenuacion30, 3u));
        }

        private void Caso15DeudaCongeladaD1()
        {
            _r.Seccion("CASO 15 · Deuda congelada D-1 · la atenuación de las vivas se pierde al reiniciar");

            NuevaPeticion();
            _r.ComoControladorQueClasifica("crear vacuna viva 'PerdidaD1' lote 'LV-D1' con Atenuacion30",
                () => _vacunaService.CrearVacuna("Viva", "PerdidaD1", "LV-D1", Venc, Aplic, null, enum_l_atenuaciones.Atenuacion30), "x");

            _r.Titulo("línea escrita en Vacunas.txt para el lote 'LV-D1'");
            _r.Campo("linea", LineaDeArchivo("Vacunas.txt", "LV-D1"));

            _r.Titulo("reinicio · se recarga todo desde disco");
            var haciendaAntes = _hacienda;
            Componer();
            NuevaPeticion();
            RecargarDesdeDisco();

            _r.Titulo("grado de atenuación recuperado del lote 'LV-D1'");
            var recuperada = _hacienda.L_vacunas.FirstOrDefault(v => v.Lote == "LV-D1");
            _r.Campo("tipo", recuperada == null ? "(no encontrada)" : recuperada.GetType().Name);
            _r.Campo("atenuacion", GradoDeAtenuacion(recuperada));
            _r.Campo("esperado-por-el-usuario", "Atenuacion30");
            _r.Campo("veredicto-D-1", "el grado se pierde en cada reinicio; se conserva a propósito");

            VolcarInventario("Inventario tras el reinicio");
            _ = haciendaAntes;
        }

        private void Caso15BisDeudaCongeladaD4()
        {
            _r.Seccion("CASO 15-BIS · Deuda congelada D-4 · el rechazo por edad en Novillo nombra al ternero");

            string rutaVentas = Path.Combine(_raizContenido, "Datos", "Ventas.txt");
            var lineas = File.Exists(rutaVentas) ? File.ReadAllLines(rutaVentas).ToList() : new List<string>();
            lineas.Add("Potrero_Novillos|2030-05-05|Incoherente|500|10|Novillo|1000000");
            File.WriteAllLines(rutaVentas, lineas);

            _r.Titulo("Ventas.txt con una fila incoherente (Novillo de 10 meses)");
            _r.Campo("fila", "Potrero_Novillos|2030-05-05|Incoherente|500|10|Novillo|1000000");

            _r.Titulo("reinicio · Program.cs:33-74 sobre el archivo incoherente");
            Componer();
            NuevaPeticion();
            RecargarDesdeDisco();

            _r.Campo("veredicto-D-4", "el literal nombra al ternero dentro de Novillo; se conserva a propósito");
            VolcarInventario("Inventario tras el arranque fallido");
        }

        /// <summary>Mismo cuerpo que CargadorInicial.Cargar, sin contenedor.</summary>
        private void RecargarDesdeDisco()
        {
            try
            {
                var potreros = _repositorioPotreros.CargarPotreros();
                foreach (var potrero in potreros) _hacienda.AgregarPotrero(potrero);
                _repositorioPotreros.CargarReses(_hacienda.L_potreros);
                _repositorioPotreros.CargarVacunasAplicadas(_hacienda.L_potreros);
                var ventas = _repositorioVentas.CargarVentas(_hacienda.L_potreros);
                foreach (var venta in ventas) _hacienda.RegistrarVenta(venta);
                var vacunas = _repositorioCatalogo.CargarVacunas();
                foreach (var vacuna in vacunas) _hacienda.AgregarVacuna(vacuna);
                _r.Campo("consola", $"Datos cargados: {potreros.Count} potreros, {ventas.Count} ventas, {vacunas.Count} vacunas");
            }
            catch (Exception ex)
            {
                _r.Campo("consola", $"Error al cargar datos: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Fixture F3 · SC-2 · casos 16 a 18 de ADRs.md §8.6
        //
        // Estos tres NO se comparan contra la línea base, y no puede ser de otra forma:
        // el sistema original no tiene chips. Por eso van a un archivo aparte,
        // salida-sc2.txt, y no contaminan el diff que prueba la preservación.
        //
        // Esa separación ES la política de §8.1: ninguna salida existente cambia; SC-2
        // solo agrega salidas nuevas, y las nuevas están inventariadas en §8.7.
        // ------------------------------------------------------------------

        public void EjecutarSC2(string rutaResesOriginal)
        {
            _r.Seccion("FIXTURE F3 · SC-2 · casos 16 a 18");
            _r.Linea("Ejecutados SOLO contra el sistema rediseñado: el original no tiene chips.");

            Caso16CargarHistoricoSinTocar();
            Caso17GuardarSinChipsNoModificaNada(rutaResesOriginal);
            Caso18CicloCompletoDelChip();
        }

        /// <summary>
        /// CASO 16 · Verificación de ADR-12. Cargar el `Reses.txt` histórico —el de cinco
        /// columnas, sin tocar— con el código nuevo: las reses aparecen TODAS, con
        /// `Chip == null`, y los conteos del listado no cambian.
        /// </summary>
        private void Caso16CargarHistoricoSinTocar()
        {
            _r.Seccion("CASO 16 · Cargar el Reses.txt histórico de cinco columnas");

            RecargarDesdeDisco();

            var todas = _resService.ObtenerTodasLasReses();
            _r.Titulo("las reses históricas cargan íntegras y sin chip");
            _r.Campo("reses-cargadas", todas.Count.ToString());
            _r.Campo("reses-con-chip", todas.Count(r => r.Res.Chip != null).ToString());
            _r.Campo("reses-sin-chip", todas.Count(r => r.Res.Chip == null).ToString());
            _r.Campo("veredicto-ADR-12", "ninguna línea histórica se descartó ni cambió de significado");

            VolcarEstadisticasDeReses();
        }

        /// <summary>
        /// CASO 17 · La prueba de que SC-2 AGREGA y no MODIFICA. Se guarda el estado sin
        /// que ninguna res tenga chip y se compara el archivo resultante con el que
        /// produjo el SISTEMA ORIGINAL. Debe ser idéntico byte a byte.
        /// </summary>
        private void Caso17GuardarSinChipsNoModificaNada(string rutaResesOriginal)
        {
            _r.Seccion("CASO 17 · Guardar sin ninguna res con chip");

            GuardadoValidado.Texto(_guardado.GuardarReses(_hacienda.L_potreros));

            string rutaNuestro = Path.Combine(_raizContenido, "Datos", "Reses.txt");
            _r.Titulo("comparación byte a byte contra el archivo del sistema original");

            if (!File.Exists(rutaResesOriginal))
            {
                _r.Campo("resultado", "(no se encontró el archivo del sistema original; ejecute antes su arnés)");
                return;
            }

            byte[] original = File.ReadAllBytes(rutaResesOriginal);
            byte[] nuestro = File.ReadAllBytes(rutaNuestro);

            _r.Campo("bytes-original", original.Length.ToString());
            _r.Campo("bytes-rediseñado", nuestro.Length.ToString());
            _r.Campo("identicos", original.SequenceEqual(nuestro) ? "SÍ" : "NO");
            _r.Campo("columnas-por-linea", string.Join(", ",
                File.ReadAllLines(rutaNuestro).Select(l => l.Split('|').Length).Distinct().OrderBy(n => n)));
            _r.Campo("veredicto", "una res sin chip escribe las mismas cinco columnas de siempre");
        }

        /// <summary>
        /// CASO 18 · La funcionalidad de SC-2 de extremo a extremo, y a la vez la prueba
        /// de que las dos formas de línea conviven en el mismo archivo.
        /// </summary>
        private void Caso18CicloCompletoDelChip()
        {
            _r.Seccion("CASO 18 · Conectar chip, registrar dos posiciones, reiniciar y consultar");

            var primera = _resService.ObtenerTodasLasReses().First();
            string potreroId = primera.Potrero.Identificacion;
            string nombreRes = primera.Res.Nombre;

            _r.ComoControladorQueRelanza($"conectar CHIP-0417 a '{nombreRes}' en '{potreroId}'",
                () => _resService.AsignarChip(potreroId, nombreRes, "CHIP-0417", 6.244200, -75.581200));

            _r.ComoControladorQueRelanza("primera lectura de posición",
                () => _resService.RegistrarPosicion("CHIP-0417", 6.250100, -75.575000));

            _r.ComoControladorQueRelanza("segunda lectura de posición",
                () => _resService.RegistrarPosicion("CHIP-0417", 6.249900, -75.589000));

            _r.Titulo("las dos formas de línea conviven en Reses.txt");
            string ruta = Path.Combine(_raizContenido, "Datos", "Reses.txt");
            var lineas = File.ReadAllLines(ruta);
            _r.Campo("linea-con-chip", Normalizador.Normalizar(lineas.First(l => l.Split('|').Length == 9)));
            _r.Campo("linea-sin-chip", Normalizador.Normalizar(lineas.First(l => l.Split('|').Length == 5)));
            _r.Campo("reparto-de-columnas", string.Join(", ",
                lineas.GroupBy(l => l.Split('|').Length).OrderBy(g => g.Key).Select(g => $"{g.Count()} líneas de {g.Key}")));

            _r.Titulo("reinicio · se recarga todo desde disco");
            Componer();
            RecargarDesdeDisco();

            var recuperada = _resService.ObtenerTodasLasReses()
                .Where(r => r.Res.Chip != null)
                .Select(r => r.Res)
                .FirstOrDefault();

            _r.Titulo("estado del chip tras el reinicio");
            _r.Campo("res", recuperada == null ? "(ninguna con chip)" : recuperada.Nombre);
            _r.Campo("identificador", recuperada?.Chip.Identificador ?? "(n/a)");
            _r.Campo("latitud", recuperada?.Chip.Latitud.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) ?? "(n/a)");
            _r.Campo("longitud", recuperada?.Chip.Longitud.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) ?? "(n/a)");
            _r.Campo("esperado", "CHIP-0417 con la SEGUNDA lectura: 6.249900, -75.589000");
            _r.Campo("reses-sin-chip-que-siguen-cargando",
                _resService.ObtenerTodasLasReses().Count(r => r.Res.Chip == null).ToString());
            _r.Campo("veredicto-ADR-11", "el identificador se conserva; el objeto de valor se reemplazó, no se mutó");
        }

        // ------------------------------------------------------------------
        // Volcados de estado observable
        // ------------------------------------------------------------------

        private void VolcarInventario(string titulo)
        {
            _r.Titulo(titulo);
            _r.Campo("potreros", _hacienda.L_potreros.Count.ToString());
            _r.Campo("reses", _hacienda.L_potreros.Sum(p => p.L_reses.Count).ToString());
            _r.Campo("vacunas-catalogo", _hacienda.L_vacunas.Count.ToString());
            _r.Campo("vacunas-aplicadas", _hacienda.L_potreros.Sum(p => p.L_reses.Sum(res => res.L_vacunas_aplicadas.Count)).ToString());
            _r.Campo("ventas", _hacienda.L_ventas.Count.ToString());
        }

        private void VolcarEstadisticasDeReses()
        {
            _r.Titulo("ResService.ObtenerEstadisticas");
            foreach (var par in _resService.ObtenerEstadisticas())
            {
                _r.Campo(par.Key, Convert.ToString(par.Value, System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private void VolcarTablaDeReses()
        {
            _r.Titulo("ResService.ObtenerTodasLasReses · orden y contenido de la tabla");
            foreach (var (potrero, res) in _resService.ObtenerTodasLasReses().Take(10))
            {
                _r.Linea($"{potrero.Identificacion}|{res.Nombre}|{res.GetType().Name}|{res.Peso}|{res.Edad}|{res.L_vacunas_aplicadas.Count}");
            }
            _r.Campo("total-filas", _resService.ObtenerTodasLasReses().Count.ToString());
        }

        private void VolcarVentas()
        {
            _r.Titulo("VentaService.ObtenerTodasLasVentas");
            foreach (var venta in _ventaService.ObtenerTodasLasVentas())
            {
                _r.Linea(Normalizador.Normalizar(LineaDeVenta(venta)));
            }
        }

        /// <summary>
        /// P-02 (Actividad 1) · SC-1 · Mismo volcado que en el Reto 1 cuando el
        /// artículo es una Res —byte a byte, para que los casos 01-15 sigan
        /// comparando idéntico contra la línea base—; rama nueva y autorizada cuando
        /// es un producto derivado.
        /// </summary>
        private static string LineaDeVenta(Venta venta)
        {
            if (venta.Articulo is Res res)
            {
                return $"{venta.Potrero.Identificacion}|{venta.Fecha:yyyy-MM-dd}|{res.Nombre}|{res.Peso}|{res.Edad}|{res.GetType().Name}|{venta.Monto}";
            }

            var producto = (ProductoDerivado)venta.Articulo;
            return $"{venta.Potrero.Identificacion}|{venta.Fecha:yyyy-MM-dd}|{producto.Nombre}|{producto.Cantidad}|{producto.Tipo}|{venta.Monto}";
        }

        private string LineaDeArchivo(string archivo, string clave)
        {
            string ruta = Path.Combine(_raizContenido, "Datos", archivo);
            if (!File.Exists(ruta)) return "(archivo inexistente)";
            return File.ReadAllLines(ruta).FirstOrDefault(l => l.Contains(clave)) ?? "(sin coincidencia)";
        }

        /// <summary>
        /// Idéntico al del arnés de la línea base. Allí tiene que caer al campo privado
        /// por reflexión porque Viva no expone su atenuación (Viva.cs:21); aquí
        /// encuentra la propiedad pública que añadió ADR-06. El valor que devuelve es
        /// el mismo en las dos versiones —Atenuacion10— porque el mapeo sigue congelado.
        /// </summary>
        private static string GradoDeAtenuacion(Vacuna vacuna)
        {
            if (vacuna == null) return "(n/a)";
            Type tipo = vacuna.GetType();

            PropertyInfo propiedad = tipo.GetProperty("Periodo_atenuacion",
                BindingFlags.Public | BindingFlags.Instance);
            if (propiedad != null) return Convert.ToString(propiedad.GetValue(vacuna));

            FieldInfo campo = tipo.GetField("periodo_atenuacion",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (campo != null) return Convert.ToString(campo.GetValue(vacuna));

            return "(no expuesto)";
        }
    }
}
