using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using System;

namespace Bib_Hacienda.Estrategias
{
    /// <summary>
    /// Strategy (Actividad 2 · P-02) · SC-1 · Vender leche, carne o piel no toca el
    /// inventario: el potrero conserva la res que produjo el artículo. Es el otro
    /// extremo del mismo contrato que <see cref="EfectoVentaRetiroInventario"/>, y su
    /// existencia es la prueba de que P-02 quedó realmente resuelto y no solo movido:
    /// un tercer artículo con un tercer efecto es una clase más, no una rama nueva en
    /// ServicioVenta.
    /// </summary>
    public sealed class EfectoVentaSinEfecto : IEfectoVenta
    {
        public Type TipoSoportado => typeof(ProductoDerivado);

        public void Aplicar(Hacienda hacienda, Potrero potrero, IArticuloVendible articulo)
        {
            // Intencionalmente vacío: SC-1 no reduce el inventario de ganado.
        }
    }
}
