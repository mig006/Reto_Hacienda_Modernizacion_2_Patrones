using Bib_Hacienda.Contratos;
using System.Text;

namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// ADR-07 · Receptor de eventos con alcance de UNA operación.
    ///
    /// Sustituye a las lambdas que se suscribían con += y nunca se desuscribían. Se
    /// construye dentro del método que dispara los eventos y muere con él, así que no
    /// hay nada que registrar ni que limpiar.
    ///
    /// Reproduce los DOS patrones de acumulación que había en el código original, que no
    /// eran el mismo:
    ///   · Potrero.anadir_res y alimentar_res CONCATENABAN los mensajes separados por
    ///     salto de línea, saltándose los vacíos → <see cref="Texto"/>.
    ///   · aplicar_vacuna se quedaba con el ÚLTIMO mensaje, sobrescribiendo
    ///     (Hacienda.cs:515, :543) → <see cref="Ultimo"/>.
    ///
    /// NOTA PARA EL DIAGRAMA TO-BE: ADRs.md §3.2 lista esta clase en el paquete de
    /// infraestructura. Tiene que estar en el DOMINIO, porque quienes la construyen son
    /// los servicios de dominio y la entidad Potrero, que no pueden depender de la web.
    /// Registrado en 04-evidencia/enmiendas-ADRs.md.
    /// </summary>
    public sealed class AcumuladorMensajes : IReceptorEventos
    {
        private readonly StringBuilder _acumulado = new StringBuilder();

        public void Notificar(string mensaje)
        {
            Ultimo = mensaje;

            if (!string.IsNullOrEmpty(mensaje))
            {
                _acumulado.Append(mensaje).Append("\n");
            }
        }

        /// <summary>Último mensaje recibido, o cadena vacía si no hubo ninguno.</summary>
        public string Ultimo { get; private set; } = "";

        /// <summary>Todos los mensajes, en orden de llegada, terminados en salto de línea.</summary>
        public string Texto => _acumulado.ToString();

        public bool HayMensajes => _acumulado.Length > 0;
    }
}
