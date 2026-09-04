using Bib_Hacienda.Valores;
using p_mvcHacienda.Servicios;
using System.Text;

namespace Caracterizacion.Rediseno
{
    /// <summary>
    /// Acumulador de la salida de caracterización.
    ///
    /// Registra las tres primeras categorías de "observable" de ADRs.md §8.2:
    ///   1. el texto exacto del mensaje,
    ///   2. el tipo de alerta (success / danger),
    ///   3. la navegación resultante (redirigir al índice vs. re-renderizar el formulario).
    ///
    /// Esta clase es idéntica en los dos arneses (línea base y rediseño) a propósito:
    /// si el andamiaje de medición fuera distinto, la comparación no probaría nada.
    /// </summary>
    public sealed class Registro
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public void Seccion(string titulo)
        {
            _sb.AppendLine();
            _sb.AppendLine("################################################################");
            _sb.AppendLine("## " + titulo);
            _sb.AppendLine("################################################################");
        }

        public void Titulo(string titulo)
        {
            _sb.AppendLine();
            _sb.AppendLine("=== " + titulo + " ===");
        }

        public void Campo(string clave, string valor) => _sb.AppendLine("[" + clave + "] " + valor);

        public void Linea(string texto) => _sb.AppendLine(texto);

        public override string ToString() => _sb.ToString();

        /// <summary>
        /// Reproduce el patrón de PotreroController.Create y ResController.Create /
        /// Alimentar / Vender: el servicio LANZA en caso de error, y el controlador
        /// distingue éxito de fallo por haber entrado o no en el catch. Nunca consulta
        /// el contenido del mensaje.
        /// </summary>
        public void ComoControladorQueRelanza(string titulo, Func<string> accion)
        {
            Titulo(titulo);
            try
            {
                string mensaje = accion();
                Campo("mensaje", Normalizador.Normalizar(mensaje));
                Campo("tipo", "success");
                Campo("navegacion", "RedirectToAction(Index)");
            }
            catch (Exception ex)
            {
                Campo("mensaje", Normalizador.Normalizar(ex.Message));
                Campo("tipo", "danger");
                Campo("navegacion", "View()");
            }
        }

        /// <summary>
        /// Reproduce el patrón de VacunaController.Create / Aplicar y UsuarioController.Create:
        /// el servicio DEVUELVE el texto del error en lugar de lanzarlo, y el controlador
        /// clasifica olfateando una subcadena.
        ///
        /// La sonda se conserva tal cual (ADR-08b): "x" para vacunas, "✅" para usuarios.
        /// Los defectos D-2 y D-3 se manifiestan justamente aquí.
        /// </summary>
        public void ComoControladorQueClasifica(string titulo, Func<string> accion, string sonda)
        {
            Titulo(titulo);
            try
            {
                string mensaje = accion();
                bool exito = mensaje.Contains(sonda);
                Campo("mensaje", Normalizador.Normalizar(mensaje));
                Campo("sonda", sonda);
                Campo("tipo", exito ? "success" : "danger");
                Campo("navegacion", exito ? "RedirectToAction(Index)" : "View()");
            }
            catch (Exception ex)
            {
                Campo("mensaje", Normalizador.Normalizar(ex.Message));
                Campo("tipo", "danger");
                Campo("navegacion", "View()");
            }
        }
        // ── Sobrecargas del rediseño ─────────────────────────────────────────
        //
        // Los servicios de aplicación ya no devuelven string sino ResultadoOperacion
        // (ADR-08a). Estas dos sobrecargas registran EXACTAMENTE los mismos campos que
        // las de arriba, de modo que la salida sigue siendo comparable carácter por
        // carácter con la de la línea base.
        //
        // La diferencia importante está en la segunda: la clasificación ya no la hace el
        // arnés olfateando la cadena, sino ClasificacionAsIs, que es código de producción.
        // Así el arnés EJERCITA el adaptador de compatibilidad en lugar de imitarlo.

        public void ComoControladorQueRelanza(string titulo, Func<ResultadoOperacion> accion)
            => ComoControladorQueRelanza(titulo, () => accion().Mensaje);

        public void ComoControladorQueClasifica(string titulo, Func<ResultadoOperacion> accion, string sonda)
        {
            Titulo(titulo);
            try
            {
                ResultadoOperacion resultado = accion();
                bool exito = ClasificacionAsIs.ExitoSegunAsIs(resultado, sonda);
                Campo("mensaje", Normalizador.Normalizar(resultado.Mensaje));
                Campo("sonda", sonda);
                Campo("tipo", exito ? "success" : "danger");
                Campo("navegacion", exito ? "RedirectToAction(Index)" : "View()");
            }
            catch (Exception ex)
            {
                Campo("mensaje", Normalizador.Normalizar(ex.Message));
                Campo("tipo", "danger");
                Campo("navegacion", "View()");
            }
        }
    }

    /// <summary>
    /// Sustituye los fragmentos que dependen del día de ejecución para que las dos
    /// corridas sean comparables aunque no ocurran en el mismo instante.
    ///
    /// Solo hay dos: la fecha de una venta (Venta.Fecha = DateTime.Now, que termina
    /// en Ventas.txt) y ToShortDateString() de PublisherVacunaVencida. Todas las
    /// fechas de vacuna de los escenarios son fijas y lejanas, de modo que el
    /// publisher cae siempre en su rama estable.
    /// </summary>
    public static class Normalizador
    {
        public static string Normalizar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            DateTime hoy = DateTime.Now;
            return texto
                .Replace(hoy.ToString("yyyy-MM-dd"), "<HOY>")
                .Replace(hoy.ToShortDateString(), "<HOY-CORTO>");
        }
    }
}
