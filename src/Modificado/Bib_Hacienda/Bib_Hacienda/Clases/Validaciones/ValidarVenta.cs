using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// ADR-03 · Valida objetos de tipo <see cref="Venta"/>.
    /// Regla idéntica a ValidarVenta.cs:14-18, con `venta.Res` generalizado a
    /// `venta.Articulo` por el cambio de modelado de P-02 (Actividad 1) / SC-1.
    /// </summary>
    public sealed class ValidadorVenta : IValidador<Venta>
    {
        public ResultadoValidacion Validar(Venta venta)
        {
            if (venta == null || venta.Potrero == null || venta.Articulo == null || venta.Monto <= 0)
            {
                return ResultadoValidacion.Invalido();
            }
            return ResultadoValidacion.Valido();
        }
    }
}
