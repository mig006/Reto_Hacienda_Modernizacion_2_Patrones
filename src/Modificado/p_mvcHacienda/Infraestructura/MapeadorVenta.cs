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
    /// La venta guarda una FOTO del artículo vendido y no una referencia: si es una res,
    /// esa res ya no existe en ningún potrero. Por eso al cargar hay que reconstruirla,
    /// cosa que delega en <see cref="MapeadorRes"/>.
    ///
    /// Si el potrero de la venta ya no existe, se fabrica uno suelto de tipo ternero
    /// (:429-432). Es un potrero fantasma que solo vive dentro de la venta; se conserva.
    ///
    /// ══════════════════════════════════════════════════════════════════════════════
    /// SC-1 · P-02 (Actividad 1) · UNA RES SIGUE ESCRIBIENDO EXACTAMENTE LA MISMA LÍNEA
    ///
    /// La regla del proyecto (MapeadorRes.cs:37-40, ADR-12) es columnas nuevas solo al
    /// final, nunca intercaladas. Aquí no hace falta ni eso: las siete columnas ya
    /// tenían una de magnitud (peso) y una de discriminador (tipo), y un producto
    /// derivado no necesita más que eso —una magnitud (cantidad) y un discriminador
    /// (Lacteo/Carne/Piel)—, así que se REUTILIZAN las mismas cinco posiciones de
    /// contenido en vez de abrir columnas nuevas:
    ///
    ///   Res:       {potrero}|{fecha}|{nombre}|{peso}    |{edad}|{tipoRes}   |{monto}
    ///   Producto:  {potrero}|{fecha}|{nombre}|{cantidad}|0     |{tipoProd.} |{monto}
    ///
    /// Ninguna fila de una venta de res existente cambia un solo byte: la rama que la
    /// escribe es la misma de siempre, carácter por carácter. La columna de edad se
    /// fija en 0 para un producto porque no aplica; se conserva la posición en vez de
    /// quitarla para no desplazar el monto, que si es lo que el sistema concilia contra caja.
    /// ══════════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class MapeadorVenta
    {
        public static string ALinea(Venta venta)
        {
            string fecha = venta.Fecha.ToString("yyyy-MM-dd");
            (string nombre, uint magnitud, ushort edad, string tipo) = DescomponerArticulo(venta.Articulo);

            return $"{venta.Potrero.Identificacion}|{fecha}|{nombre}|{magnitud}|{edad}|{tipo}|{venta.Monto}";
        }

        private static (string Nombre, uint Magnitud, ushort Edad, string Tipo) DescomponerArticulo(IArticuloVendible articulo)
        {
            if (articulo is Res res)
            {
                return (res.Nombre, res.Peso, res.Edad, res.GetType().Name);
            }

            if (articulo is ProductoDerivado producto)
            {
                return (producto.Nombre, producto.Cantidad, 0, producto.Tipo.ToString());
            }

            throw new System.ArgumentException($"Tipo de artículo vendible no soportado: {articulo.GetType().Name}");
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

            string nombre = partes[2];
            uint magnitud = uint.Parse(partes[3]);
            ushort edad = ushort.Parse(partes[4]);
            string tipo = partes[5];
            uint monto = uint.Parse(partes[6]);

            var potrero = potreros.FirstOrDefault(p => string.Equals(p.Identificacion, potreroId, StringComparison.OrdinalIgnoreCase))
                          ?? new Potrero(potreroId, l_tipos_potreros.ternero);

            IArticuloVendible articulo = Enum.TryParse<TipoProducto>(tipo, ignoreCase: false, out var tipoProducto)
                ? new ProductoDerivado(tipoProducto, magnitud)
                // Puede lanzar si la fila es incoherente: es la vía por la que D-4 llega a
                // la consola de arranque. Se conserva.
                : MapeadorRes.ReconstruirRes(fabricas, tipo, nombre, magnitud, edad);

            venta = new Venta(potrero, fecha, articulo, monto);
            return true;
        }
    }
}
