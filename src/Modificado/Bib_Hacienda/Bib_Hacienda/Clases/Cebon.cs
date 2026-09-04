using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// ADR-05 · Cebón: 13 a 48 meses.
    /// </summary>
    public class Cebon : Res //Hereda de Res
    {

        //Constructor
        public Cebon(string nombre, uint peso, ushort edad) : base(nombre, peso, edad)
        {
        }

        // (12, 48] en el original; aquí [13, 48]. Idéntico sobre enteros, y es el rango
        // que asignaba Potrero.cs:71-76.
        //
        // Se declara como constante de tipo para que FabricaCebon lea EXACTAMENTE
        // el mismo rango que aplica el constructor. Antes esa correspondencia estaba
        // escrita dos veces —en el switch de Potrero y en el setter del subtipo— y nada
        // garantizaba que coincidieran.
        public static readonly RangoEdad Rango = new RangoEdad((ushort)(ReglaRes.edad_max_ternero + 1), ReglaRes.edad_max_cebon);

        public override RangoEdad EdadPermitida => Rango;

        public override uint PesoMinimo => ReglaRes.peso_min_cebon;

        public override uint PesoRecomendadoVenta => ReglaRes.peso_recom_venta_cebon;

        public override byte MaxVacunasBacterianas => ReglaVacuna.max_bac_cebon;

        public override byte MaxVacunasVivas => ReglaVacuna.max_viv_cebon;

        // Literal exacto de Cebon.cs:23.
        protected override string MensajeEdadInvalida => "El cebon excedió la edad maxima";
    }
}
