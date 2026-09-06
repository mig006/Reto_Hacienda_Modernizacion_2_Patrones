using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones.ReglasRes
{
    /// <summary>
    /// Composite (P-05, Actividad 2) · Tercera regla de ValidarRes.cs:19. Se apoya en
    /// que <see cref="ReglaResNoNula"/> ya corrió primero: aquí <c>res</c> nunca es null.
    /// </summary>
    public sealed class ReglaPesoPositivo : IValidador<Res>
    {
        public ResultadoValidacion Validar(Res res)
            => res.Peso <= 0 ? ResultadoValidacion.Invalido() : ResultadoValidacion.Valido();
    }
}
