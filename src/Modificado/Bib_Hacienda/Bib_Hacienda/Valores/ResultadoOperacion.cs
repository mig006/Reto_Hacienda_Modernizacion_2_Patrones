namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// ADR-08a · Resultado de una operación de negocio: si salió bien, y qué decirle
    /// al operario.
    ///
    /// ══ El acoplamiento por texto que este tipo elimina del diseño ═══════════════
    /// Diez de los once métodos públicos de Hacienda devolvían una cadena pensada para
    /// la pantalla, y la capa web decidía si una operación había tenido éxito
    /// INSPECCIONANDO FRAGMENTOS DE ESE TEXTO (H-13):
    ///
    ///     UsuarioController.cs:50  ->  if (resultado.Contains("✅"))
    ///     VacunaController.cs:118  ->  if (resultado.Contains("x"))
    ///
    /// Con este tipo el dato viaja por un campo booleano, no por una subcadena.
    ///
    /// ══ Lo que este tipo NO cambia, y por qué ════════════════════════════════════
    /// Mensaje contiene EXACTAMENTE la misma cadena que hoy, carácter por carácter,
    /// incluidas las concatenaciones $"{resultado}. {validado}" y las erratas congeladas.
    /// Exito refleja el resultado REAL de la operación... y todavía no se consulta para
    /// pintar la pantalla.
    ///
    /// Esa es la partición de ADR-08: la mitad invisible (08a) se aplica, la mitad
    /// visible (08b) se congela, porque corregirla cambiaría el color de la alerta y la
    /// navegación —deudas D-2 y D-3—. El campo queda poblado y correcto, esperando que
    /// alguien lo consulte; el parche es una línea en ClasificacionAsIs.
    ///
    /// ══ Regla de uso obligatoria ════════════════════════════════════════════════
    /// Que este tipo exista NO autoriza a convertir en `return Fallido(...)` los `throw`
    /// que hoy hay. La asimetría entre servicios que lanzan (PotreroService, ResService)
    /// y servicios que devuelven el error (VacunaService, UsuarioService) es
    /// comportamiento observable: determina si el controlador entra en su catch y, con
    /// ello, si la pantalla redirige al índice o se queda en el formulario.
    /// </summary>
    public sealed class ResultadoOperacion
    {
        private ResultadoOperacion(bool exito, string mensaje)
        {
            Exito = exito;
            Mensaje = mensaje;
        }

        public bool Exito { get; }

        public string Mensaje { get; }

        public static ResultadoOperacion Exitoso(string mensaje) => new ResultadoOperacion(true, mensaje);

        public static ResultadoOperacion Fallido(string mensaje) => new ResultadoOperacion(false, mensaje);
    }
}
