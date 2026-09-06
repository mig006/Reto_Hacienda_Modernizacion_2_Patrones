using System.Collections.Generic;
using System.Linq;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// Composite (Actividad 2 · P-05) · Agrupa varios <see cref="IValidador{T}"/> y los
    /// trata como si fueran uno solo.
    ///
    /// ── Qué resuelve ──────────────────────────────────────────────────────────────
    /// Ataca la mitad de P-05 que sí necesita un patrón: hoy cada validador colapsa
    /// todas sus reglas en un único <c>if</c> (ValidarRes.cs:19), de modo que una regla
    /// nueva obliga a modificar esa clase. Con el Composite, el componente y el
    /// compuesto comparten el mismo contrato (<see cref="IValidador{T}"/>): las HOJAS
    /// son los validadores existentes —su <c>if</c> NO se toca— y una regla nueva pasa a
    /// ser una clase más añadida a la lista en la raíz de composición, sin tocar código
    /// existente (OCP). La otra mitad de P-05 —las 7 dependencias y los 5 métodos iguales
    /// de GuardadoValidado— se resuelve con un método genérico, que no es un patrón.
    ///
    /// ── Por qué Composite y no Chain of Responsibility (B-05) ─────────────────────
    /// El compuesto declara «todas estas reglas son, juntas, la validación de este tipo»,
    /// no «la primera que pueda resuelve». El corte en la primera regla que falla es una
    /// OPTIMIZACIÓN, no el significado: ninguna regla sustituye a otra.
    ///
    /// ── Comportamiento congelado ──────────────────────────────────────────────────
    /// Devuelve tal cual el <see cref="ResultadoValidacion"/> de la primera regla
    /// inválida (mismo texto congelado <c>TextoInvalido</c>); si todas pasan, devuelve
    /// <see cref="ResultadoValidacion.Valido"/> (mismo <c>TextoValido</c>). Un compuesto
    /// que envuelve un único validador es, por tanto, indistinguible de ese validador.
    /// </summary>
    public sealed class ValidadorCompuesto<T> : IValidador<T>
    {
        private readonly IReadOnlyList<IValidador<T>> _reglas;

        public ValidadorCompuesto(IEnumerable<IValidador<T>> reglas)
        {
            _reglas = reglas.ToList();
        }

        public ResultadoValidacion Validar(T entidad)
        {
            foreach (IValidador<T> regla in _reglas)
            {
                ResultadoValidacion resultado = regla.Validar(entidad);
                if (!resultado.EsValido)
                {
                    return resultado; // corte temprano: optimización, no significado
                }
            }
            return ResultadoValidacion.Valido();
        }
    }
}
