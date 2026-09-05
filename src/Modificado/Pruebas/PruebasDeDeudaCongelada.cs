using Bib_Hacienda.Clases;
using Bib_Hacienda.Clases.Validaciones;
using Bib_Hacienda.Clases.Validaciones.ReglasRes;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Fabricas;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using p_mvcHacienda.Infraestructura;
using p_mvcHacienda.Servicios;
using Xunit;
using static Bib_Hacienda.Clases.Viva;

namespace Bib_Hacienda.Pruebas
{
    /// <summary>
    /// Pruebas que FIJAN los cinco defectos congelados de ADRs.md §8.3.
    ///
    /// Son deliberadamente incómodas: afirman que el sistema se comporta MAL, y pasan
    /// porque el sistema efectivamente se comporta mal. Su función es doble:
    ///
    ///   1. Son evidencia de preservación: el defecto sobrevivió al rediseño, que es lo
    ///      que la restricción dura del enunciado exige.
    ///   2. Son una red de seguridad al revés: si alguien "arregla" uno de estos
    ///      defectos sin autorización, la prueba falla y le obliga a leer el ADR antes de
    ///      cambiar una salida observable.
    ///
    /// El día que la líder técnica autorice las correcciones, estas pruebas se invierten.
    /// </summary>
    public class PruebasDeDeudaCongelada
    {
        private static readonly DateTime Venc = new DateTime(2030, 12, 31);
        private static readonly DateTime Aplic = new DateTime(2030, 1, 15);

        private static readonly IFabricaRes[] Fabricas =
            { new FabricaTernero(), new FabricaCebon(), new FabricaNovillo() };

        private static readonly IFabricaVacuna[] FabricasVacuna =
            { new FabricaVacunaBacteriana(), new FabricaVacunaViva() };

        /// <summary>
        /// D-1 · El grado de atenuación de toda vacuna viva se pierde en cada reinicio.
        /// Round-trip completo contra disco: se escribe Atenuacion30 y vuelve Atenuacion10.
        /// </summary>
        [Fact]
        public void D1_LaAtenuacionDeLasVivasSePierdeAlRecargar()
        {
            string datos = DirectorioTemporal();
            var repositorio = new RepositorioCatalogoVacunasArchivo(datos);

            var original = new Viva("PerdidaD1", "LV-D1", Venc, Aplic, enum_l_atenuaciones.Atenuacion30);
            repositorio.GuardarVacunas(new List<Vacuna> { original });

            // La sexta columna se escribió como 0, descartando el grado real.
            string linea = File.ReadAllLines(Path.Combine(datos, "Vacunas.txt"))[0];
            Assert.EndsWith("|Viva|0", linea);

            var recargada = (Viva)repositorio.CargarVacunas().Single();

            Assert.Equal(enum_l_atenuaciones.Atenuacion30, original.Periodo_atenuacion);
            Assert.Equal(enum_l_atenuaciones.Atenuacion10, recargada.Periodo_atenuacion);   // el defecto
        }

        /// <summary>
        /// D-2 · Un alta CORRECTA de usuario se clasifica como error.
        /// El resultado real es Exito=true, pero la sonda por texto dice lo contrario.
        /// </summary>
        [Fact]
        public void D2_UnAltaCorrectaDeUsuarioSeClasificaComoError()
        {
            var servicio = new UsuarioService(new RepositorioUsuariosArchivo(DirectorioTemporal()));

            ResultadoOperacion resultado = servicio.CrearUsuario("santi", "santi11");

            Assert.True(resultado.Exito);                                   // la operación SÍ salió bien
            Assert.Equal("Usuario 'santi' creado exitosamente", resultado.Mensaje);
            Assert.NotNull(servicio.BuscarUsuario("santi"));                // y el usuario quedó creado

            // ...pero la pantalla lo pinta en rojo y se queda en el formulario.
            Assert.False(ClasificacionAsIs.ExitoSegunAsIs(resultado, ClasificacionAsIs.SondaUsuario));
        }

        /// <summary>
        /// D-3 · Un lote de vacuna DUPLICADO se clasifica como éxito, porque el mensaje
        /// de error contiene la 'x' de «existe».
        /// </summary>
        [Fact]
        public void D3_UnLoteDeVacunaDuplicadoSeClasificaComoExito()
        {
            var servicio = ServicioDeVacunasEnTemporal();

            servicio.CrearVacuna("Bacteriana", "Bravox", "LoteB1", Venc, Aplic, 3, null);
            ResultadoOperacion duplicado = servicio.CrearVacuna("Bacteriana", "Bravox", "LoteB1", Venc, Aplic, 3, null);

            Assert.False(duplicado.Exito);                                  // la operación falló
            Assert.Contains("Ya existe una vacuna con el lote", duplicado.Mensaje);

            // ...pero la pantalla lo pinta en verde y redirige al índice.
            Assert.True(ClasificacionAsIs.ExitoSegunAsIs(duplicado, ClasificacionAsIs.SondaVacuna));
        }

        /// <summary>
        /// D-4 · El rechazo por edad dentro de la clase Novillo nombra al ternero.
        /// Se comprueba por la vía que lo hace alcanzable: la reconstrucción desde disco.
        /// </summary>
        [Fact]
        public void D4_ElRechazoPorEdadEnNovilloNombraAlTernero()
        {
            string datos = DirectorioTemporal();
            File.WriteAllLines(Path.Combine(datos, "Ventas.txt"),
                new[] { "Potrero_Novillos|2030-05-05|Incoherente|500|10|Novillo|1000000" });

            var repositorio = new RepositorioVentasArchivo(datos, Fabricas);

            var error = Assert.Throws<Exception>(() => repositorio.CargarVentas(new List<Potrero>()));
            Assert.Equal("Error al cargar ventas: El ternero excedió la edad maxima", error.Message);
        }

        /// <summary>
        /// D-5 · El mensaje de lote bacteriano imprime el marcador {nombre} en crudo,
        /// porque a esa línea le falta el prefijo de interpolación. La variante de lote
        /// vivo, en cambio, funciona bien.
        /// </summary>
        [Fact]
        public void D5_ElLoteBacterianoImprimeElMarcadorEnCrudo()
        {
            var fabrica = new FabricaVacunas(new Hacienda(), FabricasVacuna);

            string bacteriano = fabrica.CrearLote("Bacteriana", "LoteBac", "LB", Venc, Aplic, 2u, null, 3u);
            string vivo = fabrica.CrearLote("Viva", "LoteViv", "LV", Venc, Aplic, null, enum_l_atenuaciones.Atenuacion30, 3u);

            Assert.Contains("- Nombre: {nombre}", bacteriano);   // el defecto
            Assert.Contains("- Nombre: LoteViv", vivo);          // el contraste
        }

        // ── Utilidades ────────────────────────────────────────────────────────

        private static string DirectorioTemporal()
        {
            string ruta = Path.Combine(Path.GetTempPath(), "pruebas-hacienda", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ruta);
            return ruta;
        }

        private static VacunaService ServicioDeVacunasEnTemporal()
        {
            string datos = DirectorioTemporal();
            var hacienda = new Hacienda();
            var gestorPotreros = new GestorPotreros(hacienda);

            var avisosAltaDeRes = new IPublicadorEvento[]
            {
                new PublisherPotreroMitad(), new PublisherPotreroLleno(), new PublisherPesoMin(), new PublisherPesoVenta()
            };
            IRepositorioPotreros repoPotreros = new RepositorioPotrerosArchivo(datos, new PoliticaCapacidadPotrero(), Fabricas, avisosAltaDeRes);
            IRepositorioVentas repoVentas = new RepositorioVentasArchivo(datos, Fabricas);
            IRepositorioCatalogoVacunas repoCatalogo = new RepositorioCatalogoVacunasArchivo(datos);

            var validadorRes = new ValidadorCompuesto<Res>(new IValidador<Res>[]
            {
                new ReglaResNoNula(), new ReglaNombreObligatorio(), new ReglaPesoPositivo(), new ReglaEdadPositiva(),
            });
            var guardado = new GuardadoValidado(repoPotreros, repoVentas, repoCatalogo,
                new ValidadorPotrero(), validadorRes, new ValidadorVacuna(), new ValidadorVenta());

            var servicioVacunacion = new ServicioVacunacion(hacienda, gestorPotreros,
                new PublisherVacunaVencida(), new IPublicadorEvento[] { new PublisherVacunacionCompletada() });

            return new VacunaService(hacienda, new FabricaVacunas(hacienda, FabricasVacuna),
                servicioVacunacion, gestorPotreros, guardado, repoCatalogo);
        }
    }
}
