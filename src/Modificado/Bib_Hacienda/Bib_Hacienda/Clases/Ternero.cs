using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// ADR-05 · Ternero: 0 a 12 meses.
    ///
    /// Ya no sobrescribe el accesor de Edad —eso era la violación de LSP—; ahora aporta
    /// política. Los valores no cambian: son los mismos de ReglaRes y ReglaVacuna que
    /// antes se consultaban desde fuera preguntando por el tipo concreto.
    /// </summary>
    public class Ternero : Res //Hereda de Res
    {

        // Constructor
        public Ternero(string nombre, uint peso, ushort edad) : base(nombre, peso, edad)
        {
        }

        // Rango de ReglaRes.edad_max_ternero (12), idéntico al que asignaba Potrero.cs:66-69
        //
        // Se declara como constante de tipo para que FabricaTernero lea EXACTAMENTE
        // el mismo rango que aplica el constructor. Antes esa correspondencia estaba
        // escrita dos veces —en el switch de Potrero y en el setter del subtipo— y nada
        // garantizaba que coincidieran.
        public static readonly RangoEdad Rango = new RangoEdad(0, ReglaRes.edad_max_ternero);

        public override RangoEdad EdadPermitida => Rango;

        public override uint PesoMinimo => ReglaRes.peso_min_ternero;

        public override uint PesoRecomendadoVenta => ReglaRes.peso_recom_venta_ternero;

        public override byte MaxVacunasBacterianas => ReglaVacuna.max_bac_ternero;

        public override byte MaxVacunasVivas => ReglaVacuna.max_viv_ternero;

        // Literal exacto de Ternero.cs:23.
        protected override string MensajeEdadInvalida => "El ternero excedió la edad maxima";
    }
}
