using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System.Collections.Generic;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// Composite (Actividad 2 · P-05) · Agrupa varios <see cref="IValidador{T}"/> y los
    /// trata como uno solo.
    ///
    /// ══ Qué sustituye ═══════════════════════════════════════════════════════════
    /// El `if` de cuatro condiciones de ValidarRes.cs:19
    /// (<c>res == null || string.IsNullOrWhiteSpace(res.Nombre) || res.Peso &lt;= 0 || res.Edad &lt;= 0</c>).
    /// Cada condición pasa a ser su propia clase en <c>ReglasRes/</c>; una regla nueva
    /// es una clase más y una línea en la raíz de composición, no una condición más
    /// dentro de un `if` que ya tenía cuatro.
    ///
    /// ══ Por qué Composite y no Chain of Responsibility ════════════════════════════
    /// Las dos producen una lista de reglas y las dos permiten agregar sin tocar
    /// código existente, pero declaran cosas distintas. La cadena dice «alguien de
    /// estos se hará cargo»: el primero capaz resuelve y el resto no importa. El
    /// compuesto dice «todos estos, juntos, son la validación de este tipo» — que es
    /// lo que ocurre aquí: las cuatro condiciones de <c>Res</c> son la definición
    /// completa de una res válida, ninguna sustituye a otra (bitácora B-05).
    ///
    /// ══ Por qué corta en la primera regla inválida ════════════════════════════════
    /// Es optimización, no significado — reproduce el cortocircuito de `||` del `if`
    /// original, incluido que la regla de "no nula" debe registrarse primero para que
    /// las siguientes puedan asumir un objeto no nulo (exactamente como el `||`
    /// original nunca evaluaba <c>res.Nombre</c> si <c>res</c> era null).
    ///
    /// ══ El texto no cambia ═════════════════════════════════════════════════════
    /// <see cref="ResultadoValidacion.Invalido"/> devuelve siempre el mismo literal
    /// congelado sin importar cuál regla falló — igual que el `if` original, que nunca
    /// distinguía cuál de sus cuatro condiciones era la culpable. Descomponer el `if`
    /// es un cambio estructural, no un cambio en qué ve el operario.
    /// </summary>
    public sealed class ValidadorCompuesto<T> : IValidador<T>
    {
        private readonly IReadOnlyCollection<IValidador<T>> _reglas;

        public ValidadorCompuesto(IEnumerable<IValidador<T>> reglas)
        {
            _reglas = reglas as IReadOnlyCollection<IValidador<T>> ?? new List<IValidador<T>>(reglas);
        }

        public ResultadoValidacion Validar(T entidad)
        {
            foreach (var regla in _reglas)
            {
                var resultado = regla.Validar(entidad);
                if (!resultado.EsValido)
                {
                    return resultado;
                }
            }
            return ResultadoValidacion.Valido();
        }
    }
}
