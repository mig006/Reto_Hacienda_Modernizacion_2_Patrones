namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-07 · DI-7 · Destinatario de los mensajes que emiten los publishers.
    ///
    /// ══ La fuga que elimina ══════════════════════════════════════════════════════
    /// Los publishers eran campos de instancia de objetos que viven dentro del Singleton
    /// Hacienda (Hacienda.cs:43-46) y del Potrero (:21-24). Cada operación se suscribía
    /// con += DENTRO del método y NUNCA se desuscribía:
    ///
    ///     publisher_peso_min.evt_peso_min += mensaje => { mensajes_eventos += ... };
    ///
    /// Tras n operaciones había n manejadores acumulados por evento, cada uno reteniendo
    /// la clausura de la operación anterior. Potrero.anadir_res suscribía CUATRO por
    /// llamada, alimentar_res DOS y aplicar_vacuna DOS: una jornada de 200 alimentaciones
    /// dejaba 400 manejadores vivos, y una de 200 altas de ganado, 800 (H-12). El sistema
    /// se degradaba progresivamente y la única solución accesible era reiniciarlo.
    ///
    /// ══ Por qué un parámetro y no un -= ══════════════════════════════════════════
    /// Alternativa A: añadir el -= al final de cada método. Descartada por frágil: si el
    /// método lanza entre el += y el -=, la suscripción queda viva; exige try/finally en
    /// cinco lugares y confía en que nadie olvide el patrón en el futuro.
    ///
    /// Alternativa B: un bus de mensajes con suscriptores registrados. Descartada por
    /// sobre-ingeniería: el único "suscriptor" real es la cadena de texto que se muestra
    /// en pantalla, y además cambiaría el orden de los mensajes, que es observable.
    ///
    /// ELEGIDA: el receptor se ENTREGA como parámetro de la operación. La fuga desaparece
    /// por construcción —el receptor nace y muere con la llamada— y no hay nada que
    /// desuscribir. No es Service Locator porque nadie lo pide: se lo entregan.
    ///
    /// ══ Costo aceptado ══════════════════════════════════════════════════════════
    /// Se pierde el mecanismo de eventos de C# (delegate + event), que era una decisión
    /// de diseño explícita del autor original y probablemente un requisito del curso en
    /// que se escribió el sistema. El patrón publicador/suscriptor queda expresado
    /// mediante una interfaz y no mediante el lenguaje. Se acepta porque el patrón estaba
    /// MAL USADO —la suscripción ocurría dentro del método que dispara, no en la
    /// configuración— y el mal uso producía una fuga activa.
    /// </summary>
    public interface IReceptorEventos
    {
        void Notificar(string mensaje);
    }
}
