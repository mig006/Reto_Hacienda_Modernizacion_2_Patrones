using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones.ReglasRes
{
    /// <summary>
    /// Composite (P-05, Actividad 2) · Primera de las cuatro reglas de ValidarRes.cs:19.
    ///
    /// Va PRIMERA en el registro de <see cref="ValidadorCompuesto{T}"/> a propósito:
    /// las otras tres asumen una res no nula, igual que el `||` original nunca
    /// evaluaba <c>res.Nombre</c> si <c>res</c> ya era null.
    /// </summary>
    public sealed class ReglaResNoNula : IValidador<Res>
    {
        public ResultadoValidacion Validar(Res res)
            => res == null ? ResultadoValidacion.Invalido() : ResultadoValidacion.Valido();
    }
}
