using Bib_Hacienda.Valores;
using System;
using System.Collections.Generic;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// ADR-05 · Raíz de la jerarquía de ganado, con el contrato INVERTIDO.
    ///
    /// ══ El diagnóstico ═══════════════════════════════════════════════════════════
    /// La jerarquía tenía dos problemas opuestos y complementarios:
    ///
    ///   1. RESTRINGÍA lo que la base permitía (H-10). Res.Edad era
    ///      `public virtual ushort Edad { get; set; }` y aceptaba cualquier ushort;
    ///      los tres subtipos sobrescribían el setter para acotarlo y lanzar Exception
    ///      genérica (Ternero.cs:19-24 y equivalentes). Eso es fortalecer una
    ///      precondición: código que recibiera una Res y le asignara una edad
    ///      funcionaba o fallaba según el subtipo que le hubiera tocado.
    ///      VEREDICTO LSP: NO PASA.
    ///
    ///   2. NO APORTABA el comportamiento que le correspondía (H-06). Las reglas por
    ///      tipo vivían FUERA, en siete puntos: Hacienda.cs:487-501 (máximo de vacunas),
    ///      PublisherPesoMin.cs:25-27, PublisherPesoVenta.cs:27-29,
    ///      PublisherVacunacionCompletada.cs:30-41, Potrero.cs:88-102 y
    ///      PersistenciaService.cs:434-440 (construcción por cadena mágica), y
    ///      ResService.cs:66-68.
    ///
    /// ══ La corrección, simétrica ═════════════════════════════════════════════════
    ///   · Se quita lo que restringe: Edad deja de ser virtual y de tener setter
    ///     público. El rango pasa de precondición fortalecida (ilegal) a invariante de
    ///     clase verificada en construcción (legal y más fuerte): una Res construida es
    ///     siempre válida. Verificado que no rompe ningún llamador — Edad no se asigna
    ///     en ningún punto fuera de los constructores.
    ///   · Se trae lo que falta: cinco miembros abstractos que todo subtipo DEBE
    ///     responder. El compilador lo exige. Hoy, olvidar PublisherPesoMin.cs:25-27
    ///     compilaba perfectamente y producía un peso mínimo de 0, es decir, un animal
    ///     que nunca aparecía como desnutrido.
    ///
    /// VEREDICTO LSP TO-BE: PASA. Ningún subtipo sobrescribe accesores, ninguno lanza
    /// fuera del constructor, ninguno añade tipos de excepción nuevos, y la invariante
    /// EdadPermitida.Contiene(Edad) se cumple siempre.
    ///
    /// ══ Lo que NO se corrigió ════════════════════════════════════════════════════
    /// El TIPO de excepción sigue siendo Exception genérica y el TEXTO de cada subtipo
    /// viaja intacto, erratas incluidas (deuda D-4). Se descartó introducir una
    /// excepción tipada EdadFueraDeRangoException: aportaba claridad al llamador a
    /// cambio de un mensaje nuevo, y el mensaje es salida observable. La mejora de LSP
    /// no depende del texto sino de DÓNDE se lanza.
    /// </summary>
    public abstract class Res
    {

        //Atributos
        private string nombre;
        private uint peso;
        private readonly ushort edad;
        private List<Vacuna> l_vacunas_aplicadas;

        internal void EventHandler() { }

        //Constructor
        protected Res(string nombre, uint peso, ushort edad)
        {
            this.Nombre = nombre;
            this.Peso = peso;

            // El rango se verifica AQUÍ, que es el único lugar donde el cliente sabe que
            // está eligiendo el tipo. Antes se verificaba en el setter, de modo que
            // cualquier asignación posterior podía fallar.
            //
            // Nota sobre llamar a miembros abstractos desde el constructor de la base:
            // es seguro en este caso porque las tres implementaciones devuelven
            // constantes y no dependen de estado del subtipo, que todavía no existe.
            if (!EdadPermitida.Contiene(edad))
            {
                throw new Exception(MensajeEdadInvalida);
            }

            this.edad = edad;
            this.l_vacunas_aplicadas = new List<Vacuna>();
        }

        //Accesores
        /// <summary>Inmutable: es la invariante que sostiene la sustituibilidad.</summary>
        public ushort Edad => edad;

        public List<Vacuna> L_vacunas_aplicadas { get => l_vacunas_aplicadas; set => l_vacunas_aplicadas = value; }

        /// <summary>
        /// SC-2 · ADR-11 · Chip de geolocalización conectado al animal; `null` si no lleva.
        ///
        /// ══ La decisión de modelado que lo cambia todo ═══════════════════════════
        /// Conectar un chip es una OPERACIÓN SOBRE UN ANIMAL QUE YA EXISTE, no un
        /// parámetro de su construcción. Es la lectura correcta del negocio —al animal se
        /// le pone el chip después de que llegó al potrero, no en el momento de nacer— y
        /// tiene una consecuencia arquitectónica grande:
        ///
        ///   IFabricaRes y las tres fábricas NO se tocan. Ternero, Cebon y Novillo NO se
        ///   tocan. Potrero.anadir_res NO se toca. La cadena de CINCO FIRMAS ENCADENADAS
        ///   que la Fase 2 midió como el costo dominante de esta solicitud desaparece del
        ///   cálculo, porque el chip nunca viaja por ella.
        ///
        /// La alternativa —chip como parámetro del constructor, que es lo que la medición
        /// de la Fase 2 asumió porque es lo que la estructura ANTIGUA obligaba— quedó
        /// descartada en ADR-11: reintroduciría los cinco eslabones que ADR-05 acaba de
        /// desmontar, y obligaría a inventar un chip ficticio para toda res ya registrada.
        ///
        /// ══ Por qué es opcional ═════════════════════════════════════════════════
        /// `null` es el estado de TODO el histórico. Esa opcionalidad es exactamente lo
        /// que hace el cambio compatible hacia atrás y lo que mantiene verdes los quince
        /// casos de caracterización: una res sin chip se guarda y se carga igual que antes,
        /// con las mismas cinco columnas, byte a byte.
        ///
        /// Costo aceptado: el sistema NO impide que una res quede sin chip. Si el negocio
        /// decidiera que es obligatorio, esa regla no está en el constructor y habría que
        /// añadirla como validación de aplicación. Se acepta porque hoy ninguna res lo
        /// tiene, y una regla que el histórico entero incumple no puede ser una invariante
        /// de construcción.
        ///
        /// ══ Sobre LSP ══════════════════════════════════════════════════════════
        /// Ningún subtipo la sobrescribe, la restringe ni la oculta, de modo que la
        /// verificación de sustituibilidad de §5.2 se mantiene íntegra: SC-2 no reabre el
        /// veredicto LSP de la jerarquía Res.
        /// </summary>
        public Chip Chip { get; private set; }

        /// <summary>
        /// SC-2 · ADR-11 · Conecta un chip al animal, o reemplaza el que llevaba.
        /// El estado pasa de `null` a asignado; el `private set` impide que nadie de
        /// fuera lo deje a medias.
        /// </summary>
        public void AsignarChip(Chip chip)
        {
            Chip = chip;
        }

        /// <summary>
        /// SC-2 · ADR-11 · Registra una lectura de posición del dispositivo.
        ///
        /// REEMPLAZA el objeto en lugar de mutarlo, porque `Chip` es un objeto de valor
        /// inmutable (§5.6). El identificador se conserva: es lo único que identifica al
        /// dispositivo físico a lo largo de sus lecturas.
        /// </summary>
        public void RegistrarPosicion(double latitud, double longitud, DateTime cuando)
        {
            if (Chip == null)
            {
                throw new Exception($"La res {Nombre} no tiene chip al que registrarle una posición");
            }

            Chip = new Chip(Chip.Identificador, latitud, longitud, cuando);
        }
        public string Nombre { get => nombre; set => nombre = value; }
        public uint Peso { get => peso; set => peso = value; }

        // ── Política del tipo · antes vivía en siete archivos ajenos ─────────────

        /// <summary>Rango de edad admisible. Sustituye al switch de Potrero.cs:62-83.</summary>
        public abstract RangoEdad EdadPermitida { get; }

        /// <summary>Por debajo de este peso el animal está desnutrido. Antes: PublisherPesoMin.cs:25-27.</summary>
        public abstract uint PesoMinimo { get; }

        /// <summary>A partir de este peso el animal es apto para venta. Antes: PublisherPesoVenta.cs:27-29.</summary>
        public abstract uint PesoRecomendadoVenta { get; }

        /// <summary>Máximo de vacunas bacterianas del esquema. Antes: Hacienda.cs:487-501.</summary>
        public abstract byte MaxVacunasBacterianas { get; }

        /// <summary>Máximo de vacunas vivas del esquema. Antes: Hacienda.cs:487-501.</summary>
        public abstract byte MaxVacunasVivas { get; }

        /// <summary>
        /// ¿Completó el animal su esquema de vacunación?
        /// Sustituye a la cadena de PublisherVacunacionCompletada.cs:30-41.
        /// </summary>
        public bool EsquemaCompleto(ushort bacterianas, ushort vivas)
            => bacterianas >= MaxVacunasBacterianas && vivas >= MaxVacunasVivas;

        /// <summary>
        /// Literal congelado por subtipo. Reproduce la salida actual, erratas incluidas:
        /// Ternero y Novillo devuelven AMBOS "El ternero excedió la edad maxima"
        /// (deuda D-4 en ADRs.md §8.3).
        /// </summary>
        protected abstract string MensajeEdadInvalida { get; }
    }
}
