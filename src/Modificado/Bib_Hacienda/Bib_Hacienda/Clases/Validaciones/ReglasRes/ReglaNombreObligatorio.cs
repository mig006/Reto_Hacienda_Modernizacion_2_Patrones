using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones.ReglasRes
{
    /// <summary>
    /// Composite (P-05, Actividad 2) · Segunda regla de ValidarRes.cs:19. Se apoya en
    /// que <see cref="ReglaResNoNula"/> ya corrió primero: aquí <c>res</c> nunca es null.
    /// </summary>
    public sealed class ReglaNombreObligatorio : IValidador<Res>
    {
        public ResultadoValidacion Validar(Res res)
            => string.IsNullOrWhiteSpace(res.Nombre) ? ResultadoValidacion.Invalido() : ResultadoValidacion.Valido();
    }
}
