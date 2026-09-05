using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;
using System;

namespace Bib_Hacienda.Eventos
{
    /// <summary>
    /// ADR-07 · Observer (P-03, Actividad 2) · Informa que el potrero alcanzó su
    /// capacidad máxima.
    /// </summary>
    public class PublisherPotreroLleno : IPublicadorEvento
    {
        /// <summary>Lee CantidadReses y Potrero de ContextoAviso; ignora Res y los contadores.</summary>
        public void Informar(ContextoAviso contexto, IReceptorEventos receptor)
            => Informar_Potrero_Lleno(contexto.CantidadReses, contexto.Potrero, receptor);

        //Metodo para informar que el potrero está lleno
        public void Informar_Potrero_Lleno(ushort cantidad_reses, Potrero potrero, IReceptorEventos receptor)
        {
            try
            {
                //Notificar que el potrero ha alcanzado su capacidad máxima
                if (cantidad_reses == ReglaPotrero.max_reses_potrero)
                {
                    string mensaje = $"[Evento] El potrero '{potrero.Identificacion}' ha alcanzado su capacidad máxima de reses ({ReglaPotrero.max_reses_potrero}). No se pueden agregar más reses.";
                    receptor.Notificar(mensaje);
                }
            }
            catch (Exception er)
            {
                throw new Exception("[Evento] Error inesperado en el metodo Informar_Potrero_Lleno: " + er.Message);
            }
        }
    }
}
