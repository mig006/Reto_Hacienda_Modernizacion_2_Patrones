using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// ADR-03 · Valida objetos de tipo <see cref="Res"/>.
    ///
    /// La regla no cambia ni un carácter respecto de ValidarRes.cs:14-18, incluida la
    /// condición <c>res.Edad &lt;= 0</c>, que es la que hace alcanzable el corte
    /// temprano de GuardarReses: el formulario de alta admite edad 0 para potreros de
    /// ternero (Views/Res/Create.cshtml:64, min="0"), de modo que una sola res inválida
    /// impide escribir Reses.txt entero. Es comportamiento observable y se conserva.
    /// </summary>
    public sealed class ValidadorRes : IValidador<Res>
    {
        public ResultadoValidacion Validar(Res res)
        {
            if (res == null || string.IsNullOrWhiteSpace(res.Nombre) || res.Peso <= 0 || res.Edad <= 0)
            {
                return ResultadoValidacion.Invalido();
            }
            return ResultadoValidacion.Valido();
        }
    }
}
