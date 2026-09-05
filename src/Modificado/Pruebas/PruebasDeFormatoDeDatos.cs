using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Fabricas;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using p_mvcHacienda.Infraestructura;
using Xunit;
using static Bib_Hacienda.Clases.Potrero;

namespace Bib_Hacienda.Pruebas
{
    /// <summary>
    /// SC-2 · ADR-12 · Pruebas de la regla de formato de datos.
    ///
    /// El riesgo mejor documentado de toda la Fase 2 no es de código sino de datos: los
    /// `.txt` no tienen cabecera ni marca de versión, y los cargadores parsean por índice
    /// posicional detrás de una guarda de longitud. Si el formato cambia de manera
    /// incompatible, las líneas no cumplen la guarda y se descartan SIN LANZAR EXCEPCIÓN:
    /// el histórico desaparece en silencio.
    ///
    /// Estas pruebas fijan la regla que lo evita.
    /// </summary>
    public class PruebasDeFormatoDeDatos
    {
        private static readonly IFabricaRes[] Fabricas =
            { new FabricaTernero(), new FabricaCebon(), new FabricaNovillo() };

        /// <summary>
        /// Una res SIN chip escribe exactamente las mismas cinco columnas de siempre.
        /// Es la propiedad que mantiene verdes los quince casos de caracterización.
        /// </summary>
        [Fact]
        public void UnaResSinChipEscribeLasCincoColumnasDeSiempre()
        {
            var potrero = new Potrero("Potrero_Cebones", l_tipos_potreros.cebon);
            var res = new Cebon("Rayo", 287, 14);

            string linea = MapeadorRes.ALinea(potrero, res);

            Assert.Equal("Potrero_Cebones|Rayo|287|14|Cebon", linea);
            Assert.Equal(5, linea.Split('|').Length);
        }

        /// <summary>
        /// Una res CON chip anexa cuatro columnas al final, sin mover las cinco primeras.
        /// El formato es el del ejemplo de ADR-12.
        /// </summary>
        [Fact]
        public void UnaResConChipAnexaCuatroColumnasAlFinal()
        {
            var potrero = new Potrero("Potrero_Cebones", l_tipos_potreros.cebon);
            var res = new Cebon("Rayo", 287, 14);
            res.AsignarChip(new Chip("CHIP-0417", 6.2442, -75.5812, new DateTime(2026, 8, 8)));

            string linea = MapeadorRes.ALinea(potrero, res);

            Assert.Equal("Potrero_Cebones|Rayo|287|14|Cebon|CHIP-0417|6.244200|-75.581200|2026-08-08", linea);
            Assert.Equal(9, linea.Split('|').Length);
            Assert.StartsWith("Potrero_Cebones|Rayo|287|14|Cebon", linea);   // las cinco primeras, intactas
        }

        /// <summary>
        /// LA PRUEBA QUE PROTEGE EL HISTÓRICO: una línea de cinco columnas —el formato de
        /// todos los datos que el cliente tiene hoy— se sigue leyendo entera, con la res
        /// completa y sin chip. Ninguna se descarta.
        /// </summary>
        [Fact]
        public void UnaLineaHistoricaDeCincoColumnasSeSigueLeyendoEntera()
        {
            bool leida = MapeadorRes.TryDeLinea("Potrero_Cebones|Rayo|287|14|Cebon",
                out var potrero, out var nombre, out var peso, out var edad, out var chip);

            Assert.True(leida);
            Assert.Equal("Potrero_Cebones", potrero);
            Assert.Equal("Rayo", nombre);
            Assert.Equal(287u, peso);
            Assert.Equal((ushort)14, edad);
            Assert.Null(chip);
        }

        [Fact]
        public void UnaLineaDeNueveColumnasDevuelveTambienElChip()
        {
            bool leida = MapeadorRes.TryDeLinea(
                "Potrero_Cebones|Rayo|287|14|Cebon|CHIP-0417|6.244200|-75.581200|2026-08-08",
                out _, out _, out _, out _, out var chip);

            Assert.True(leida);
            Assert.NotNull(chip);
            Assert.Equal("CHIP-0417", chip.Identificador);
            Assert.Equal(6.2442, chip.Latitud, 4);
            Assert.Equal(new DateTime(2026, 8, 8), chip.UltimaLectura);
        }

        /// <summary>
        /// Round-trip contra disco: se guarda un archivo con las dos formas de línea
        /// conviviendo y se recarga. Las reses sin chip siguen cargando y la del chip
        /// recupera su posición.
        /// </summary>
        [Fact]
        public void LasDosFormasDeLineaConvivenEnElMismoArchivo()
        {
            string datos = Path.Combine(Path.GetTempPath(), "pruebas-hacienda", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(datos);

            var repositorio = new RepositorioPotrerosArchivo(datos, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());
            var potrero = new Potrero("Potrero_Cebones", l_tipos_potreros.cebon);
            potrero.anadir_res("Rayo", 14, 287, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());
            potrero.anadir_res("Luna", 27, 301, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());
            potrero.L_reses[0].AsignarChip(new Chip("CHIP-0417", 6.2442, -75.5812, new DateTime(2026, 8, 8)));

            var enMemoria = new List<Potrero> { potrero };
            repositorio.GuardarPotreros(enMemoria);
            repositorio.GuardarReses(enMemoria);

            var lineas = File.ReadAllLines(Path.Combine(datos, "Reses.txt"));
            Assert.Equal(9, lineas[0].Split('|').Length);
            Assert.Equal(5, lineas[1].Split('|').Length);

            var recargados = repositorio.CargarPotreros();
            repositorio.CargarReses(recargados);

            var reses = recargados.Single().L_reses;
            Assert.Equal(2, reses.Count);
            Assert.Equal("CHIP-0417", reses[0].Chip.Identificador);
            Assert.Null(reses[1].Chip);
        }
    }
}
