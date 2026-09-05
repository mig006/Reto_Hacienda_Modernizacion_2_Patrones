using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// Observer (Actividad 2 · P-03) · Un aviso del dominio.
    ///
    /// ══ Qué sustituye ═══════════════════════════════════════════════════════════
    /// Las ocho instanciaciones de publicadores concretos con <c>new</c> dentro del
    /// dominio: cuatro en Potrero.anadir_res, dos en GestorReses.alimentar_res (sus dos
    /// sobrecargas) y dos en ServicioVacunacion. Cada publicador tenía su propia firma
    /// —<c>Informar_Peso_Min(Res, IReceptorEventos)</c> frente a
    /// <c>Informar_Potrero_Mitad(ushort, Potrero, IReceptorEventos)</c>—, así que no
    /// existía ninguna colección sobre la que iterar ni forma de inyectarlos.
    ///
    /// ══ Por qué cinco de los seis, no los seis ════════════════════════════════════
    /// <c>PublisherVacunaVencida</c> queda fuera a propósito: devuelve <c>bool</c> y
    /// ServicioVacunacion.aplicar_vacuna lanza según ese valor. No es una notificación,
    /// es una guarda de flujo — meterla aquí convertiría un aviso en control de flujo y
    /// forzaría a esta interfaz a devolver algo que los otros cuatro no necesitan.
    /// Sigue inyectada por constructor (ya no hay `new` para ella tampoco), solo que no
    /// por esta interfaz.
    ///
    /// ══ Un tercer aviso nuevo cuesta ══════════════════════════════════════════════
    /// Una clase que implemente esta interfaz, más una línea en la raíz de composición
    /// para agregarla a la lista de avisos que corresponda. Ni Potrero, ni GestorReses,
    /// ni ServicioVacunacion se vuelven a tocar.
    /// </summary>
    public interface IPublicadorEvento
    {
        /// <summary>
        /// Evalúa el contexto y, si corresponde, notifica al receptor. El orden en que
        /// se invocan varios avisos sobre el mismo receptor es comportamiento observable
        /// y lo fija el orden de la colección en la raíz de composición, no esta interfaz.
        /// </summary>
        void Informar(ContextoAviso contexto, IReceptorEventos receptor);
    }
}
