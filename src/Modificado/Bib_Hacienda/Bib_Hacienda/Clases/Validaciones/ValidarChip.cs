using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones
{
    /// <summary>
    /// SC-2 · ADR-11 · Valida objetos de tipo <see cref="Chip"/>.
    ///
    /// ══ Aquí se cobra la deuda que pagó ADR-03 ═══════════════════════════════════
    /// En el sistema original, declarar una validación nueva costaba SEIS ARCHIVOS: había
    /// que añadir el método a `IValidarInformacion`, a la clase abstracta `Validacion` y a
    /// los cuatro validadores existentes, tres de los cuales lo habrían implementado
    /// lanzando una excepción de método no implementado. Es lo que
    /// `05-SolicitudesCambio.md` §5.1 midió para SC-3, y el mismo efecto aplica aquí.
    ///
    /// Con `IValidador&lt;T&gt;` cuesta UN ARCHIVO: este. Los cuatro validadores existentes
    /// no se enteran de que el chip existe, y en el composition root se AGREGA una línea
    /// sin tocar las cuatro anteriores.
    /// </summary>
    public sealed class ValidadorChip : IValidador<Chip>
    {
        public ResultadoValidacion Validar(Chip chip)
        {
            if (chip == null
                || string.IsNullOrWhiteSpace(chip.Identificador)
                || chip.Latitud < -90 || chip.Latitud > 90
                || chip.Longitud < -180 || chip.Longitud > 180)
            {
                return ResultadoValidacion.Invalido();
            }
            return ResultadoValidacion.Valido();
        }
    }
}
