using Bib_Hacienda.Clases;
using Bib_Hacienda.Valores;
using System;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-13 · Factory Method para el catálogo de vacunas (P-01, Actividad 2).
    ///
    /// ══ Qué sustituye ═══════════════════════════════════════════════════════════
    /// Los cuatro métodos fijos de FabricaVacunas (CrearBacteriana, CrearViva y sus dos
    /// variantes de lote) y las tres decisiones repetidas sobre "qué tipo es esto":
    /// el `if` que lo inferÍa mirando cuál parámetro opcional llegó con valor
    /// (VacunaService.cs:49), el que comparaba la cadena "Bacteriana"
    /// (VacunaController.cs:95) y las ramas de construcción de MapeadorVacuna.
    ///
    /// Un tipo de vacuna nuevo pasa a ser 1 clase que implementa esta interfaz + 1
    /// línea en la raíz de composición — el mismo costo que ya tiene agregar un tipo de
    /// res con <see cref="IFabricaRes"/>.
    ///
    /// ══ Por qué no basta un único método Crear(nombre, ...) ══════════════════════
    /// A diferencia de Res, los subtipos de Vacuna NO comparten un constructor de la
    /// misma forma: Bacteriana necesita un período de aplicación, Viva necesita un
    /// grado de atenuación. <see cref="SolicitudVacuna"/> transporta ambos como
    /// opcionales; cada implementación toma el suyo y valida el resto con el mismo
    /// mensaje que tenía la sobrecarga que sustituye.
    ///
    /// ══ Por qué los mensajes de éxito viven aquí y no en FabricaVacunas ══════════
    /// El texto difiere por tipo ("Período de aplicación: X semanas" frente a "Grado de
    /// atenuación: X") y, en el caso del lote bacteriano, reproduce a propósito la
    /// deuda D-5 (segunda línea sin interpolar). Es contenido específico del producto,
    /// así que es la fábrica concreta —no el orquestador— quien debe conocerlo: es
    /// exactamente el reparto de responsabilidades que el patrón declara.
    /// </summary>
    public interface IFabricaVacuna
    {
        /// <summary>
        /// Discriminador de negocio: coincide con vacuna.GetType().Name, con el valor de
        /// tipoVacuna que llega del formulario y con la columna 5 de Vacunas.txt
        /// ("Bacteriana" / "Viva"). Es la clave del registro en FabricaVacunas.
        /// </summary>
        string Tipo { get; }

        /// <summary>Tipo concreto que produce.</summary>
        Type TipoSoportado { get; }

        /// <summary>
        /// Construye la vacuna. Lanza ArgumentException si el parámetro específico de
        /// este tipo (periodo o atenuación) no llegó — caso que hoy es inalcanzable
        /// porque VacunaController siempre lo garantiza antes de llamar, y que se
        /// conserva como guarda defensiva, no como cambio de comportamiento observable.
        /// </summary>
        Vacuna Crear(SolicitudVacuna solicitud);

        /// <summary>Mensaje de éxito de la creación individual. Texto literal congelado.</summary>
        string MensajeIndividual(SolicitudVacuna solicitud);

        /// <summary>
        /// Mensaje de éxito de la creación por lote. Texto literal congelado, incluida
        /// la deuda D-5 de la bacteriana cuando corresponda.
        /// </summary>
        string MensajeLote(SolicitudVacuna solicitudBase, int creadas, uint cantidad);

        /// <summary>Etiqueta con la que se envuelve una excepción al crear una individual: "bacteriana" / "viva".</summary>
        string EtiquetaErrorIndividual { get; }

        /// <summary>Etiqueta con la que se envuelve una excepción al crear un lote: "lote bacteriano" / "lote vivo".</summary>
        string EtiquetaErrorLote { get; }
    }
}
