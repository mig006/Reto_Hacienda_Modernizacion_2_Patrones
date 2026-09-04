using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// ADR-05 · Novillo: 49 meses en adelante.
    ///
    /// ── Sobre la refutación de H-17 ───────────────────────────────────────────────
    /// El diagnóstico sostuvo en su momento que la condición de Novillo estaba
    /// INVERTIDA respecto de sus clases hermanas. La verificación demostró que no lo
    /// está: los tres setters cubrían rangos consecutivos y sin solape —ternero [0,12],
    /// cebón [13,48], novillo [49,∞)— que coinciden exactamente con los que asignaba
    /// Potrero.anadir_res. Un novillo con edad válida se aceptaba correctamente. H-17
    /// quedó cerrado por refutación.
    ///
    /// Lo verdaderamente defectuoso es el MENSAJE, y ese sí sobrevive: ver más abajo.
    /// </summary>
    public class Novillo : Res //Hereda de Res
    {

        //Constructor
        public Novillo(string nombre, uint peso, ushort edad) : base(nombre, peso, edad)
        {
        }

        // (48, ∞) en el original; aquí [49, ushort.MaxValue]. Idéntico sobre ushort.
        //
        // Se declara como constante de tipo para que FabricaNovillo lea EXACTAMENTE
        // el mismo rango que aplica el constructor. Antes esa correspondencia estaba
        // escrita dos veces —en el switch de Potrero y en el setter del subtipo— y nada
        // garantizaba que coincidieran.
        public static readonly RangoEdad Rango = new RangoEdad((ushort)(ReglaRes.edad_max_cebon + 1), ushort.MaxValue);

        public override RangoEdad EdadPermitida => Rango;

        public override uint PesoMinimo => ReglaRes.peso_min_novillo;

        public override uint PesoRecomendadoVenta => ReglaRes.peso_recom_venta_novillo;

        public override byte MaxVacunasBacterianas => ReglaVacuna.max_bac_novillo;

        public override byte MaxVacunasVivas => ReglaVacuna.max_viv_novillo;

        /// <summary>
        /// DEUDA D-4 · Literal exacto de Novillo.cs:23. Dice "ternero" dentro de la
        /// clase Novillo —nombra al animal equivocado— y describe el fallo al revés,
        /// porque el rechazo ocurre cuando la edad está POR DEBAJO del mínimo, no
        /// cuando excede un máximo.
        ///
        /// Es alcanzable: MapeadorVenta reconstruye subtipos desde disco sin validar el
        /// rango, así que un Ventas.txt con una fila incoherente produce este texto en
        /// la consola de arranque. Por eso NO se corrige.
        ///
        /// PARCHE: cambiar este literal. Una línea.
        /// </summary>
        protected override string MensajeEdadInvalida => "El ternero excedió la edad maxima";
    }
}
