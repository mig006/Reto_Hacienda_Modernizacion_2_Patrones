using System;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// ADR-06 · Vacuna de virus vivo atenuado.
    ///
    /// ══ La violación de LSP que se corrige aquí ══════════════════════════════════
    /// El contrato implícito de Vacuna —y el que todo el sistema asume— es que una
    /// vacuna guardada se recupera igual. Bacteriana lo cumple: expone
    /// Periodo_aplicacion y ese dato se persiste. Viva NO lo cumplía: guardaba
    /// periodo_atenuacion en un campo privado y NO LO EXPONÍA (Viva.cs:21), de modo que
    /// la persistencia no tenía forma de leerlo aunque quisiera.
    ///
    /// VEREDICTO LSP AS-IS: NO PASA, por la invariante de recuperabilidad del estado.
    ///
    /// ══ La corrección, partida en dos mitades ════════════════════════════════════
    /// MITAD QUE SÍ SE APLICA · exponer la propiedad. Verificado que es INVISIBLE:
    /// ninguna vista muestra la atenuación de una vacuna viva. Las dos pantallas que
    /// muestran datos de vacuna consultan exclusivamente el subtipo bacteriano
    /// (Vacuna/Index.cshtml:82 y Res/DetalleVacunas.cshtml:48, ambas con `is Bacteriana`)
    /// y para las vivas escriben la etiqueta fija "Atenuada". El getter restaura la
    /// simetría con Bacteriana y cierra la violación a nivel del tipo.
    ///
    /// MITAD QUE NO SE APLICA · el mapeo. Que MapeadorVacuna escriba el valor real en
    /// lugar de 0 cambiaría el contenido de dos .txt y el estado en memoria tras
    /// reiniciar. Es comportamiento observable y la restricción dura lo prohíbe.
    /// Queda como deuda D-1, con el parche escrito en MapeadorVacuna.
    ///
    /// ══ Por qué esto no es trabajo a medias ══════════════════════════════════════
    /// Un lector rápido verá una propiedad pública que nadie usa. Lo que consigue es
    /// RECLASIFICAR el defecto: deja de ser «la jerarquía esconde estado» —un problema
    /// de diseño, caro— y pasa a ser «el mapeador decide no leerlo» —un fallo de dos
    /// ramas en un archivo, localizable y barato—. Esa reclasificación es en sí misma
    /// un resultado del rediseño.
    ///
    /// Alternativa evaluada: doble despacho con IVisitanteVacuna&lt;T&gt;, para que un tercer
    /// tipo de vacuna rompa la compilación del mapeador. Descartada: resuelve un problema
    /// que este sistema no tiene —solo hay dos tipos y ninguna solicitud de cambio pide
    /// más— a cambio de una interfaz que hay que modificar cada vez que aparece un
    /// subtipo, o sea cerrada a la extensión. Queda anotada por si aparece un tercero.
    /// </summary>
    public class Viva : Vacuna //Hereda de Vacuna
    {

        //Enum para las atenuaciones
        public enum enum_l_atenuaciones
        {
            Atenuacion10 = 10,
            Atenuacion20 = 20,
            Atenuacion30 = 30
        }

        //Atributos
        private enum_l_atenuaciones periodo_atenuacion;

        //Constructor
        public Viva(string nombre, string lote, DateTime fecha_vencimiento, DateTime fecha_aplicacion, enum_l_atenuaciones periodo_atenuacion) : base(nombre, lote, fecha_vencimiento, fecha_aplicacion)
        {
            this.periodo_atenuacion = periodo_atenuacion;
        }

        //Accesores
        /// <summary>
        /// ADR-06 · Estado expuesto, igual que Bacteriana.Periodo_aplicacion.
        /// Es de solo lectura porque el grado se fija al crear la vacuna y no cambia.
        /// </summary>
        public enum_l_atenuaciones Periodo_atenuacion => periodo_atenuacion;
    }
}
