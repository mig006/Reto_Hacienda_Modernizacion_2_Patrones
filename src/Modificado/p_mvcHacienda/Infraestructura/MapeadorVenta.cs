using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using System.Globalization;
using static Bib_Hacienda.Clases.Potrero;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · Traduce entre una línea de Ventas.txt y una <see cref="Venta"/>.
    ///
    /// Traducción literal de PersistenciaService.cs:158-160 (escritura) y :414-442 (lectura).
    ///
    /// La venta guarda una FOTO de la res vendida —nombre, peso, edad y tipo— y no una
    /// referencia: la res ya no existe en ningún potrero. Por eso al cargar hay que
    /// reconstruir el subtipo, cosa que delega en <see cref="MapeadorRes"/>.
    ///
    /// Si el potrero de la venta ya no existe, se fabrica uno suelto de tipo ternero
    /// (:429-432). Es un potrero fantasma que solo vive dentro de la venta; se conserva.
    /// </summary>
    public static class MapeadorVenta
    {
        public static string ALinea(Venta venta)
        {
            string fecha = venta.Fecha.ToString("yyyy-MM-dd");
            string tipoRes = venta.Res.GetType().Name;
            return $"{venta.Potrero.Identificacion}|{fecha}|{venta.Res.Nombre}|{venta.Res.Peso}|{venta.Res.Edad}|{tipoRes}|{venta.Monto}";
        }

        public static bool TryDeLinea(string linea, List<Potrero> potreros,
            IEnumerable<IFabricaRes> fabricas, out Venta venta)
        {
            venta = null!;
            if (string.IsNullOrWhiteSpace(linea)) return false;

            var partes = linea.Split('|');
            if (partes.Length < 7) return false;

            string potreroId = partes[0].Trim();
            if (!DateTime.TryParseExact(partes[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                return false;
            }

            string resNombre = partes[2];
            uint resPeso = uint.Parse(partes[3]);
            ushort resEdad = ushort.Parse(partes[4]);
            string resTipo = partes[5];
            uint monto = uint.Parse(partes[6]);

            var potrero = potreros.FirstOrDefault(p => string.Equals(p.Identificacion, potreroId, StringComparison.OrdinalIgnoreCase))
                          ?? new Potrero(potreroId, l_tipos_potreros.ternero);

            // Puede lanzar si la fila es incoherente: es la vía por la que D-4 llega a
            // la consola de arranque. Se conserva.
            Res res = MapeadorRes.ReconstruirRes(fabricas, resTipo, resNombre, resPeso, resEdad);

            venta = new Venta(potrero, fecha, res, monto);
            return true;
        }
    }
}
