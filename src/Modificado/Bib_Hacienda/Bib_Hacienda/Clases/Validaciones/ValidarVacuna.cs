using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// ADR-03 · Valida objetos de tipo <see cref="Vacuna"/>.
    /// Regla idéntica a ValidarVacuna.cs:14-18.
    /// </summary>
    public sealed class ValidadorVacuna : IValidador<Vacuna>
    {
        public ResultadoValidacion Validar(Vacuna vacuna)
        {
            if (vacuna == null || string.IsNullOrWhiteSpace(vacuna.Nombre) || string.IsNullOrWhiteSpace(vacuna.Lote))
            {
                return ResultadoValidacion.Invalido();
            }
            return ResultadoValidacion.Valido();
        }
    }
}
