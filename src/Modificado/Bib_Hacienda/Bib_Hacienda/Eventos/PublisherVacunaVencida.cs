using System;
using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;

namespace Bib_Hacienda.Eventos
{
    /// <summary>ADR-07 · Informa si una vacuna está vencida o por vencer.</summary>
    public class PublisherVacunaVencida
    {
        //Metodo para informar si la vacuna está vencida o está por vencer (un mes antes)
        public bool Informar_Vacuna_Vencida(Vacuna vacuna, IReceptorEventos receptor)
        {
            try
            {
                if (vacuna == null)
                {
                    throw new ArgumentNullException(nameof(vacuna), "La vacuna no puede ser null");
                }

                //Validar si la vacuna está vencida o vencerá en un mes
                DateTime fechaAlerta = DateTime.Now.AddMonths(1);
                bool esta_vencida = vacuna.Fecha_vencimiento <= DateTime.Now;
                bool alerta_vencimiento = vacuna.Fecha_vencimiento <= fechaAlerta && !esta_vencida;

                // Disparar el evento con el mensaje apropiado
                string mensaje;
                if (esta_vencida)
                {
                    mensaje = $"[Evento] La vacuna '{vacuna.Nombre}' del lote '{vacuna.Lote}' está vencida desde {vacuna.Fecha_vencimiento.ToShortDateString()}";
                }
                else if (alerta_vencimiento)
                {
                    int diasRestantes = (vacuna.Fecha_vencimiento - DateTime.Now).Days;
                    mensaje = $"[Evento] ⚠ ALERTA: La vacuna '{vacuna.Nombre}' del lote '{vacuna.Lote}' vencerá en {diasRestantes} días ({vacuna.Fecha_vencimiento.ToShortDateString()})";
                }
                else
                {
                    mensaje = $"[Evento] La vacuna '{vacuna.Nombre}' del lote '{vacuna.Lote}' es válida (vence el {vacuna.Fecha_vencimiento.ToShortDateString()})";
                }
                receptor.Notificar(mensaje);

                return esta_vencida;
            }
            catch (Exception er)
            {
                throw new Exception("[evento] Error inesperado en el metodo Informar_Vacuna_Vencida: " + er.Message);
            }
        }
    }
}
