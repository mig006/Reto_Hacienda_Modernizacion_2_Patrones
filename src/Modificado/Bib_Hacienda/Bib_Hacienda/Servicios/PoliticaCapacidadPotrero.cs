using Bib_Hacienda.Reglas;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-05 · Decide si cabe una res más en un potrero.
    ///
    /// Sale de Potrero.anadir_res porque cambia por OTRA razón que el resto del método:
    /// la capacidad la decide el negocio ("cuántas reses caben"), mientras que la
    /// traducción tipo→subtipo cambia cuando aparece un tipo de res nuevo. Dos razones
    /// de cambio distintas, dos clases.
    ///
    /// ATENCIÓN · la comparación es POR IGUALDAD, no "mayor o igual". Un potrero que ya
    /// tuviera más reses que el máximo seguiría admitiendo altas. Es lo que hace
    /// Potrero.cs:55 y se conserva: cambiarlo a >= alteraría el comportamiento en ese
    /// caso límite.
    /// </summary>
    public sealed class PoliticaCapacidadPotrero
    {
        public bool Cabe(int cantidadActual) => cantidadActual != ReglaPotrero.max_reses_potrero;
    }
}
