using Bib_Hacienda.Contratos;

namespace Bib_Hacienda.Clases
{
    /// <summary>Las tres líneas de producto que el Anexo B autoriza para SC-1.</summary>
    public enum TipoProducto { Lacteo, Carne, Piel }

    /// <summary>
    /// SC-1 · Artículo vendible que no es una res: lácteos, carne o piel.
    ///
    /// No tiene ciclo de vida en el dominio —no vive en un potrero, no se alimenta, no
    /// se vacuna— así que, a propósito, no tiene fábrica ni repositorio propio: nace
    /// dentro de <see cref="Servicios.ServicioVenta"/> en el instante de la venta y no
    /// vuelve a existir como objeto después. Lo único que sobrevive es la línea que
    /// <c>MapeadorVenta</c> escribe en Ventas.txt.
    /// </summary>
    public sealed class ProductoDerivado : IArticuloVendible
    {
        public TipoProducto Tipo { get; }
        public string Nombre { get; }
        public uint Cantidad { get; }

        public ProductoDerivado(TipoProducto tipo, uint cantidad)
        {
            if (cantidad <= 0)
                throw new System.ArgumentException("La cantidad vendida debe ser mayor a 0", nameof(cantidad));

            Tipo = tipo;
            Nombre = tipo.ToString();
            Cantidad = cantidad;
        }
    }
}
