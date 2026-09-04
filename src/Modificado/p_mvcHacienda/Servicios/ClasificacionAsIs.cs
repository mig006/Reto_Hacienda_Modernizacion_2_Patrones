using Bib_Hacienda.Valores;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-08b · COMPATIBILIDAD AS-IS DELIBERADA. Deudas D-2 y D-3 de ADRs.md §8.3.
    ///
    /// ══ Qué hace esta clase y por qué parece un error ════════════════════════════
    /// Clasifica el resultado de una operación olfateando una subcadena de su mensaje,
    /// que es exactamente lo que hacía la capa web y exactamente lo que ADR-08a viene a
    /// eliminar. A quien lea el código sin este comentario delante le parecerá un
    /// descuido. No lo es: es un defecto REPRODUCIDO A PROPÓSITO.
    ///
    /// ══ Los dos defectos que conserva, verificados en ejecución ══════════════════
    /// D-2 · UsuarioController buscaba "✅" en un mensaje que no lo contiene
    ///       ("Usuario '{nombre}' creado exitosamente"), así que UN ALTA CORRECTA DE
    ///       USUARIO SE MUESTRA EN ROJO y la pantalla se queda en el formulario. El
    ///       usuario sí queda creado y persistido.
    ///
    /// D-3 · VacunaController busca "x". Esta sonda acierta en casi todas las rutas:
    ///       los mensajes de éxito contienen la x de "éxito"/"exitoso", y los de error
    ///       reales no la llevan. EL ÚNICO FALLO REPRODUCIBLE es el lote duplicado:
    ///       "Ya existe una vacuna con el lote 'L' en el inventario" contiene la x de
    ///       «existe», de modo que UN INTENTO FALLIDO SE MUESTRA EN VERDE y redirige al
    ///       índice como si hubiera funcionado.
    ///
    ///       (Una versión anterior del ADR afirmaba que esta sonda era «esencialmente
    ///       aleatoria». Se comprobó mensaje por mensaje y no se sostiene; la corrección
    ///       quedó registrada porque sostener lo contrario en la sustentación habría sido
    ///       peor que no haberlo dicho.)
    ///
    /// ══ Por qué no se corrige ═══════════════════════════════════════════════════
    /// Corregirlo cambia el color de la alerta (categoría 2 de §8.2) y la navegación
    /// (categoría 3). El enunciado solo autoriza como excepción las tres solicitudes de
    /// cambio, y ninguna de ellas es esta. La decisión de corregir un defecto de
    /// producción le corresponde al cliente, no al equipo que refactoriza.
    ///
    /// ══ Qué SÍ se ganó ══════════════════════════════════════════════════════════
    /// Los tres Contains dispersos por dos controladores se concentraron aquí: UN SOLO
    /// PUNTO DE DECISIÓN en lugar de tres, con nombre y con su deuda declarada.
    ///
    /// PARCHE, cuando la líder técnica lo autorice:
    ///
    ///     public static bool ExitoSegunAsIs(ResultadoOperacion r, string sondaLegado)
    ///         => r.Exito;
    ///
    /// Una línea. Se puede aplicar en vivo durante la sustentación.
    /// </summary>
    public static class ClasificacionAsIs
    {
        public static bool ExitoSegunAsIs(ResultadoOperacion resultado, string sondaLegado)
            => resultado.Mensaje.Contains(sondaLegado);

        /// <summary>Sonda que usaba UsuarioController.cs:50.</summary>
        public const string SondaUsuario = "✅";

        /// <summary>Sonda que usaba VacunaController.cs:118 y :159.</summary>
        public const string SondaVacuna = "x";
    }
}
