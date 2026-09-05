using Bib_Hacienda.Clases;
using Bib_Hacienda.Clases.Validaciones;
using Bib_Hacienda.Clases.Validaciones.ReglasRes;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Estrategias;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Fabricas;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using p_mvcHacienda.Infraestructura;
using p_mvcHacienda.Servicios;
using System.Globalization;
using System.Text;
using static Bib_Hacienda.Clases.Potrero;
using static Bib_Hacienda.Clases.Viva;

namespace Demostracion
{
    /// <summary>
    /// Programa principal de demostración del sistema rediseñado.
    /// </summary>
    public static class PuntoDeEntrada
    {
        private static readonly DateTime Venc = new DateTime(2030, 12, 31);
        private static readonly DateTime Aplic = new DateTime(2030, 1, 15);

        private static PotreroService _potreros;
        private static ResService _reses;
        private static VacunaService _vacunas;
        private static VentaService _ventas;
        private static UsuarioService _usuarios;
        private static FabricaVacunas _fabricaVacunas;
        private static string _directorioDatos;

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string trabajo = Path.Combine(Path.GetTempPath(), "demo-hacienda");
            if (Directory.Exists(trabajo)) Directory.Delete(trabajo, recursive: true);
            Directory.CreateDirectory(Path.Combine(trabajo, "Datos"));

            Componer(trabajo);

            Titulo("HACIENDA GANADERA · DEMOSTRACIÓN DEL SISTEMA REDISEÑADO");
            Console.WriteLine("Datos de trabajo: " + Path.Combine(trabajo, "Datos"));
            Console.WriteLine("Todos los mensajes que verá son los MISMOS que produce el sistema original.");

            Escenario1_Potreros();
            Escenario2_AltaDeGanado();
            Escenario3_Engorde();
            Escenario4_Vacunacion();
            Escenario5_Venta();
            Escenario6_Consultas();
            Escenario7_CaminosDeError();
            Escenario8_DeudaCongelada();
            Escenario9_SolicitudDeCambio2();
            Escenario10_SolicitudDeCambioSC1();

            Titulo("FIN DE LA DEMOSTRACIÓN");
            Console.WriteLine("Los seis archivos .txt quedaron en " + Path.Combine(trabajo, "Datos"));
            return 0;
        }

        /// <summary>
        /// Composición manual del grafo de objetos: exactamente el mismo que registra
        /// RaizComposicion en la aplicación web, pero construido a mano para que se vea
        /// de un vistazo quién depende de quién.
        /// </summary>
        private static void Componer(string raizContenido)
        {
            string datos = Path.Combine(raizContenido, "Datos");
            _directorioDatos = datos;

            // Infraestructura: las implementaciones concretas solo se nombran AQUÍ.
            var fabricas = new IFabricaRes[] { new FabricaTernero(), new FabricaCebon(), new FabricaNovillo() };
            var fabricasVacuna = new IFabricaVacuna[] { new FabricaVacunaBacteriana(), new FabricaVacunaViva() };
            var efectosVenta = new IEfectoVenta[] { new EfectoVentaRetiroInventario(), new EfectoVentaSinEfecto() };
            var politica = new PoliticaCapacidadPotrero();

            // Observer (P-03, Actividad 2) · publicadores, registrados una sola vez.
            // El orden de cada lista de abajo ES el orden de disparo.
            var publisherMitad = new PublisherPotreroMitad();
            var publisherLleno = new PublisherPotreroLleno();
            var publisherPesoMin = new PublisherPesoMin();
            var publisherPesoVenta = new PublisherPesoVenta();
            var publisherVacunacionCompletada = new PublisherVacunacionCompletada();
            var publisherVacunaVencida = new PublisherVacunaVencida();

            var avisosAltaDeRes = new IPublicadorEvento[] { publisherMitad, publisherLleno, publisherPesoMin, publisherPesoVenta };
            var avisosAlimentacion = new IPublicadorEvento[] { publisherPesoMin, publisherPesoVenta };
            var avisosVacunacion = new IPublicadorEvento[] { publisherVacunacionCompletada };

            IRepositorioPotreros repoPotreros = new RepositorioPotrerosArchivo(datos, politica, fabricas, avisosAltaDeRes);
            IRepositorioVentas repoVentas = new RepositorioVentasArchivo(datos, fabricas);
            IRepositorioCatalogoVacunas repoCatalogo = new RepositorioCatalogoVacunasArchivo(datos);
            IRepositorioUsuarios repoUsuarios = new RepositorioUsuariosArchivo(datos);

            // Composite (P-05, Actividad 2) · mismo registro que RaizComposicion: no
            // nula primero, porque las otras tres reglas asumen una res no nula.
            var validadorRes = new ValidadorCompuesto<Res>(new IValidador<Res>[]
            {
                new ReglaResNoNula(), new ReglaNombreObligatorio(), new ReglaPesoPositivo(), new ReglaEdadPositiva(),
            });

            var guardado = new GuardadoValidado(repoPotreros, repoVentas, repoCatalogo,
                new ValidadorPotrero(), validadorRes, new ValidadorVacuna(), new ValidadorVenta());

            // Dominio: la raíz de agregados y los cinco servicios en que se partió Hacienda.
            var hacienda = new Hacienda();
            var gestorPotreros = new GestorPotreros(hacienda);
            var gestorReses = new GestorReses(hacienda, gestorPotreros, politica, fabricas, avisosAltaDeRes, avisosAlimentacion);
            var servicioVenta = new ServicioVenta(hacienda, gestorPotreros, efectosVenta);
            _fabricaVacunas = new FabricaVacunas(hacienda, fabricasVacuna);
            var servicioVacunacion = new ServicioVacunacion(hacienda, gestorPotreros, publisherVacunaVencida, avisosVacunacion);

            // Aplicación.
            _potreros = new PotreroService(hacienda, gestorPotreros, gestorReses, guardado);
            // SC-2 · La solicitud de cambio implementada en el Reto 1. Para enchufarla NO
            // hubo que modificar ninguna línea anterior de este método: solo pasar un
            // validador más.
            _reses = new ResService(hacienda, gestorPotreros, gestorReses, servicioVenta, guardado, new ValidadorChip());
            _vacunas = new VacunaService(hacienda, _fabricaVacunas, servicioVacunacion, gestorPotreros, guardado, repoCatalogo);
            // SC-1 · La solicitud de cambio del Reto 2: VentaService gana las dos
            // dependencias que necesita para vender un producto derivado (Strategy P-02).
            _ventas = new VentaService(hacienda, servicioVenta, guardado);
            _usuarios = new UsuarioService(repoUsuarios);
            _usuarios.CargarUsuarios();

        }

        // ── Escenarios ────────────────────────────────────────────────────────

        private static void Escenario1_Potreros()
        {
            Titulo("1 · El administrador organiza la finca en potreros");
            Ejecutar("Crear potrero de terneros", () => _potreros.CrearPotrero("Potrero_Terneros", l_tipos_potreros.ternero));
            Ejecutar("Crear potrero de cebones", () => _potreros.CrearPotrero("Potrero_Cebones", l_tipos_potreros.cebon));
            Ejecutar("Crear potrero de novillos", () => _potreros.CrearPotrero("Potrero_Novillos", l_tipos_potreros.novillo));
            Ejecutar("Crear un potrero repetido", () => _potreros.CrearPotrero("Potrero_Terneros", l_tipos_potreros.ternero));
        }

        private static void Escenario2_AltaDeGanado()
        {
            Titulo("2 · El operario da de alta ganado");
            Nota("El subtipo de res lo decide el potrero, a través de su IFabricaRes.");
            Ejecutar("Alta de 'Pinta' (6 meses, 100 kg) en el potrero de terneros",
                () => _potreros.AgregarRes("Potrero_Terneros", "Pinta", 6, 100));
            Ejecutar("Alta de 'Rayo' (20 meses, 200 kg) en el potrero de cebones",
                () => _potreros.AgregarRes("Potrero_Cebones", "Rayo", 20, 200));
            Ejecutar("Alta de 'Trueno' (60 meses, 300 kg) en el potrero de novillos",
                () => _potreros.AgregarRes("Potrero_Novillos", "Trueno", 60, 300));
            Nota("Los tres avisos de desnutrición los emitió el dominio, no la vista:");
            Nota("el peso mínimo lo declara cada subtipo de Res, no un `if` externo.");
        }

        private static void Escenario3_Engorde()
        {
            Titulo("3 · El operario alimenta el ganado hasta el peso de venta");
            Ejecutar("Alimentar 'Pinta' con 1 kg", () => _reses.AlimentarRes("Potrero_Terneros", "Pinta", 1));
            Ejecutar("Alimentar 'Pinta' con 149 kg", () => _reses.AlimentarRes("Potrero_Terneros", "Pinta", 149));
            Ejecutar("Alimentar 'Trueno' con 250 kg", () => _reses.AlimentarRes("Potrero_Novillos", "Trueno", 250));
        }

        private static void Escenario4_Vacunacion()
        {
            Titulo("4 · El veterinario crea inventario y completa el esquema de un ternero");
            Ejecutar("Crear vacuna bacteriana 'BacA' (lote LB-A)",
                () => _vacunas.CrearVacuna("Bacteriana", "BacA", "LB-A", Venc, Aplic, 2, null));
            Ejecutar("Crear vacuna bacteriana 'BacB' (lote LB-B)",
                () => _vacunas.CrearVacuna("Bacteriana", "BacB", "LB-B", Venc, Aplic, 3, null));
            Ejecutar("Crear vacuna bacteriana 'BacC' (lote LB-C)",
                () => _vacunas.CrearVacuna("Bacteriana", "BacC", "LB-C", Venc, Aplic, 4, null));
            Ejecutar("Crear vacuna viva 'VivA' (lote LV-A, atenuación 10)",
                () => _vacunas.CrearVacuna("Viva", "VivA", "LV-A", Venc, Aplic, null, enum_l_atenuaciones.Atenuacion10));

            Nota("Un ternero completa su esquema con 3 bacterianas y 1 viva.");
            Ejecutar("Aplicar LB-A a 'Pinta'", () => _vacunas.AplicarVacuna("Potrero_Terneros", "Pinta", "LB-A"));
            Ejecutar("Aplicar LB-B a 'Pinta'", () => _vacunas.AplicarVacuna("Potrero_Terneros", "Pinta", "LB-B"));
            Ejecutar("Aplicar LB-C a 'Pinta'", () => _vacunas.AplicarVacuna("Potrero_Terneros", "Pinta", "LB-C"));
            Ejecutar("Aplicar LV-A a 'Pinta'", () => _vacunas.AplicarVacuna("Potrero_Terneros", "Pinta", "LV-A"));

            Ejecutar("Crear 'BacE' e intentar una cuarta bacteriana",
                () => _vacunas.CrearVacuna("Bacteriana", "BacE", "LB-E", Venc, Aplic, 2, null));
            Ejecutar("Aplicar LB-E a 'Pinta' (excede el límite del ternero)",
                () => _vacunas.AplicarVacuna("Potrero_Terneros", "Pinta", "LB-E"));
        }

        private static void Escenario5_Venta()
        {
            Titulo("5 · El área comercial vende una res");
            Ejecutar("Vender 'Trueno' por 2.500.000", () => _reses.VenderRes("Potrero_Novillos", "Trueno", 2500000));
        }

        private static void Escenario6_Consultas()
        {
            Titulo("6 · Listados y estadísticas");

            Console.WriteLine("\n  Reses en la hacienda:");
            Console.WriteLine("  {0,-18} {1,-10} {2,-9} {3,7} {4,6} {5,8}", "POTRERO", "RES", "TIPO", "PESO", "EDAD", "VACUNAS");
            foreach (var (potrero, res) in _reses.ObtenerTodasLasReses())
            {
                Console.WriteLine("  {0,-18} {1,-10} {2,-9} {3,7} {4,6} {5,8}",
                    potrero.Identificacion, res.Nombre, res.GetType().Name, res.Peso, res.Edad, res.L_vacunas_aplicadas.Count);
            }

            VolcarDiccionario("Estadísticas de reses", _reses.ObtenerEstadisticas());
            VolcarDiccionario("Estadísticas de potreros", _potreros.ObtenerEstadisticas());
            VolcarDiccionario("Estadísticas de vacunas", _vacunas.ObtenerEstadisticas());
            VolcarDiccionario("Estadísticas de ventas", _ventas.ObtenerEstadisticas());
        }

        private static void Escenario7_CaminosDeError()
        {
            Titulo("7 · Caminos de error · la cadena de mensajes anidados se conserva intacta");
            Nota("Cada capa envuelve el mensaje de la anterior. Ese texto es lo que ve el operario.");
            Ejecutar("Alta con edad fuera del rango del potrero",
                () => _potreros.AgregarRes("Potrero_Terneros", "Anciana", 30, 200));
            Ejecutar("Alta en un potrero que no existe",
                () => _potreros.AgregarRes("NoExiste", "Fantasma", 6, 200));
            Ejecutar("Alta con un nombre ya usado en el potrero",
                () => _potreros.AgregarRes("Potrero_Terneros", "Pinta", 6, 100));
            Ejecutar("Aplicar una vacuna cuyo lote no está en el inventario",
                () => _vacunas.AplicarVacuna("Potrero_Terneros", "Pinta", "LOTE-INEXISTENTE"));
        }

        private static void Escenario8_DeudaCongelada()
        {
            Titulo("8 · Deuda técnica congelada · defectos que sabemos corregir y NO corregimos");
            Nota("El enunciado prohíbe cambiar el comportamiento observable y solo autoriza");
            Nota("como excepción las tres solicitudes de cambio. Estos cinco defectos están");
            Nota("diagnosticados, verificados y con el parche escrito, esperando autorización.");

            Console.WriteLine("\n  D-2 · un alta CORRECTA de usuario se reporta como error");
            var alta = _usuarios.CrearUsuario("santi", "santi11");
            Console.WriteLine("      mensaje ....... " + alta.Mensaje);
            Console.WriteLine("      Exito (real) .. " + alta.Exito);
            Console.WriteLine("      lo que pinta la pantalla: " +
                (ClasificacionAsIs.ExitoSegunAsIs(alta, ClasificacionAsIs.SondaUsuario) ? "VERDE + redirige" : "ROJO + se queda en el formulario"));
            Console.WriteLine("      el usuario SÍ quedó creado: " + (_usuarios.BuscarUsuario("santi") != null));
            Console.WriteLine("      parche: sustituir el cuerpo de ExitoSegunAsIs por `=> r.Exito;`  (una línea)");

            // LB-E se creó en el escenario 4 y NO se aplicó (excedía el límite del
            // ternero), así que sigue en el inventario: duplicarlo sí falla de verdad.
            Console.WriteLine("\n  D-3 · un lote de vacuna DUPLICADO se reporta como éxito");
            var duplicado = _vacunas.CrearVacuna("Bacteriana", "BacE", "LB-E", Venc, Aplic, 2, null);
            Console.WriteLine("      mensaje ....... " + duplicado.Mensaje);
            Console.WriteLine("      Exito (real) .. " + duplicado.Exito);
            Console.WriteLine("      lo que pinta la pantalla: " +
                (ClasificacionAsIs.ExitoSegunAsIs(duplicado, ClasificacionAsIs.SondaVacuna) ? "VERDE + redirige" : "ROJO + se queda en el formulario"));
            Console.WriteLine("      causa: el mensaje contiene la 'x' de «existe»");

            Console.WriteLine("\n  D-1 · la atenuación de las vacunas vivas se pierde al reiniciar");
            _vacunas.CrearVacuna("Viva", "PerdidaD1", "LV-D1", Venc, Aplic, null, enum_l_atenuaciones.Atenuacion30);
            var enMemoria = (Viva)_vacunas.ObtenerVacunasDisponibles().First(v => v.Lote == "LV-D1");
            Console.WriteLine("      grado en memoria .......... " + enMemoria.Periodo_atenuacion);
            Console.WriteLine("      lo que se escribe en disco  " + MapeadorVacuna.ALineaCatalogo(enMemoria));
            Console.WriteLine("      tras reiniciar volvería como Atenuacion10 (el mapeo está congelado)");
            Console.WriteLine("      ADR-06 sí expuso Viva.Periodo_atenuacion: el defecto pasó de estar");
            Console.WriteLine("      en la jerarquía a estar en dos ramas de un mapeador.");

            Console.WriteLine("\n  D-5 · el mensaje de lote bacteriano imprime {nombre} en crudo");
            Console.WriteLine("      (la sobrecarga de lote no es alcanzable desde la web; se invoca sobre el dominio)");
            foreach (var linea in _fabricaVacunas.CrearLote("Bacteriana", "LoteBac", "LB-LOTE", Venc, Aplic, 2u, null, 3u).Split('\n'))
            {
                Console.WriteLine("      " + linea);
            }
            Console.WriteLine("      ^ la segunda línea muestra el marcador sin sustituir: falta el prefijo $");
            Console.WriteLine("      contraste, la misma operación con vacunas vivas:");
            foreach (var linea in _fabricaVacunas.CrearLote("Viva", "LoteViv", "LV-LOTE", Venc, Aplic, null, enum_l_atenuaciones.Atenuacion30, 3u).Split('\n'))
            {
                Console.WriteLine("      " + linea);
            }

            Console.WriteLine("\n  D-4 · el rechazo por edad en Novillo nombra al ternero");
            try
            {
                _ = new Novillo("Incoherente", 500, 10);
            }
            catch (Exception ex)
            {
                Console.WriteLine("      new Novillo(edad 10) lanza: " + ex.Message);
                Console.WriteLine("      ...dentro de la clase Novillo. Alcanzable al reconstruir ventas desde disco.");
            }
        }

        private static void Escenario9_SolicitudDeCambio2()
        {
            Titulo("9 · SC-2 IMPLEMENTADA · chips de geolocalización para las reses");
            Nota("La decisión de modelado de ADR-11: conectar un chip es una OPERACIÓN");
            Nota("sobre un animal que ya existe, no un parámetro de su construcción.");
            Nota("Consecuencia: IFabricaRes, las tres fábricas, los tres subtipos de Res y");
            Nota("Potrero.anadir_res NO se tocaron. Cinco firmas encadenadas -> cero.");

            Ejecutar("Conectar CHIP-0417 a 'Pinta'",
                () => _reses.AsignarChip("Potrero_Terneros", "Pinta", "CHIP-0417", 6.244200, -75.581200));
            Ejecutar("Intentar reutilizar CHIP-0417 en otra res",
                () => _reses.AsignarChip("Potrero_Cebones", "Rayo", "CHIP-0417", 0, 0));
            Ejecutar("Conectar un chip con latitud imposible (120)",
                () => _reses.AsignarChip("Potrero_Terneros", "Pinta", "CHIP-0419", 120, 0));
            Ejecutar("El ganado se mueve: nueva lectura de CHIP-0417",
                () => _reses.RegistrarPosicion("CHIP-0417", 6.249900, -75.589000));

            Console.WriteLine("\n  Reses y su posición:");
            foreach (var (potrero, res) in _reses.ObtenerTodasLasReses())
            {
                string posicion = res.Chip == null
                    ? "sin chip"
                    : $"{res.Chip.Identificador}  ({res.Chip.Latitud.ToString("F6", CultureInfo.InvariantCulture)}, {res.Chip.Longitud.ToString("F6", CultureInfo.InvariantCulture)})";
                Console.WriteLine($"      {res.Nombre,-10} {potrero.Identificacion,-18} {posicion}");
            }

            Titulo("9b · ADR-12 · las dos formas de línea conviven en Reses.txt");
            Nota("Columnas nuevas SOLO al final, y SOLO si la res tiene chip.");
            Nota("La guarda del parser es >= 5, así que ya toleraba columnas de más.");
            string ruta = Path.Combine(_directorioDatos, "Reses.txt");
            foreach (var linea in File.ReadAllLines(ruta))
            {
                Console.WriteLine($"      [{linea.Split('|').Length} col] {linea}");
            }
            Nota("'Rayo' se quedó sin chip: su línea tiene cinco columnas, idénticas a las de");
            Nota("siempre. Por eso los quince casos de caracterización siguen pasando tras SC-2.");
        }

        private static void Escenario10_SolicitudDeCambioSC1()
        {
            Titulo("10 · SC-1 IMPLEMENTADA (Reto 2) · venta de productos derivados");
            Nota("Strategy (P-02, Actividad 2): vender una res retira del potrero;");
            Nota("vender un derivado NO toca el inventario. Es el mismo ServicioVenta,");
            Nota("sin un solo `if` nuevo: el efecto lo resuelve el registro de IEfectoVenta.");

            var reses_antes = _reses.ObtenerTodasLasReses().Count;

            Ejecutar("Vender 200 litros de leche de 'Potrero_Cebones'",
                () => _ventas.VenderProducto("Potrero_Cebones", TipoProducto.Lacteo, 200, 800000));
            Ejecutar("Vender 80 kg de carne de 'Potrero_Novillos'",
                () => _ventas.VenderProducto("Potrero_Novillos", TipoProducto.Carne, 80, 1200000));
            Ejecutar("Vender 5 unidades de piel de 'Potrero_Novillos'",
                () => _ventas.VenderProducto("Potrero_Novillos", TipoProducto.Piel, 5, 300000));

            var reses_despues = _reses.ObtenerTodasLasReses().Count;
            Console.WriteLine($"\n  Reses antes de las tres ventas: {reses_antes}   ·   después: {reses_despues}");
            Nota("El inventario de ganado no cambió: EfectoVentaSinEfecto no hace nada.");

            Console.WriteLine("\n  VentaService.ObtenerTodasLasVentas (incluye reses y derivados):");
            foreach (var venta in _ventas.ObtenerTodasLasVentas().Take(5))
            {
                Console.WriteLine($"      {venta.Fecha:yyyy-MM-dd}  {venta.Articulo.Nombre,-10}  {venta.Potrero.Identificacion,-18}  ${venta.Monto:N0}");
            }

            Titulo("10b · las filas de res y de producto conviven en Ventas.txt");
            Nota("Misma forma de fila para las dos: {potrero}|{fecha}|{nombre}|{magnitud}|{edad}|{tipo}|{monto}.");
            Nota("Una venta de res no cambia un solo byte: sigue siendo la misma rama de siempre.");
            string rutaVentas = Path.Combine(_directorioDatos, "Ventas.txt");
            foreach (var linea in File.ReadAllLines(rutaVentas))
            {
                Console.WriteLine("      " + linea);
            }
        }

        // ── Utilidades de presentación ────────────────────────────────────────

        private static void Ejecutar(string titulo, Func<ResultadoOperacion> accion)
        {
            Console.WriteLine("\n  ▸ " + titulo);
            try
            {
                var resultado = accion();
                foreach (var linea in resultado.Mensaje.Split('\n'))
                {
                    Console.WriteLine("      " + linea);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("      [error] " + ex.Message);
            }
        }

        private static void VolcarDiccionario(string titulo, Dictionary<string, object> datos)
        {
            Console.WriteLine("\n  " + titulo + ":");
            foreach (var par in datos)
            {
                Console.WriteLine($"      {par.Key,-18} {par.Value}");
            }
        }

        private static void Titulo(string texto)
        {
            Console.WriteLine();
            Console.WriteLine(new string('═', 78));
            Console.WriteLine(" " + texto);
            Console.WriteLine(new string('═', 78));
        }

        private static void Nota(string texto) => Console.WriteLine("    · " + texto);
    }
}
