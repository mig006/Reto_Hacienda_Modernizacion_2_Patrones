using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// ADR-03 · Valida objetos de tipo <see cref="Potrero"/>.
    ///
    /// ANTES heredaba de la clase abstracta Validacion y, además de su método útil,
    /// arrastraba ValidarRes, ValidarVacuna y ValidarVenta, los tres lanzando una
    /// excepción de método no implementado (ValidarPotrero.cs:21-33).
    ///
    /// AHORA realiza IValidador&lt;Potrero&gt;: un solo método, cero excepciones.
    /// La relación con el contrato es de REALIZACIÓN, no de generalización.
    ///
    /// La regla de negocio no cambia ni un carácter respecto de ValidarPotrero.cs:14-18.
    /// </summary>
    public sealed class ValidadorPotrero : IValidador<Potrero>
    {
        public ResultadoValidacion Validar(Potrero potrero)
        {
            if (potrero == null || string.IsNullOrWhiteSpace(potrero.Identificacion))
            {
                return ResultadoValidacion.Invalido();
            }
            return ResultadoValidacion.Valido();
        }
    }
}
