using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System;

namespace Bib_Hacienda.Eventos
{
    /// <summary>
    /// ADR-07 · Observer (P-03, Actividad 2) · Informa si una res alcanzó el peso
    /// recomendado de venta.
    /// </summary>
    public class PublisherPesoVenta : IPublicadorEvento
    {
        /// <summary>Lee Res de ContextoAviso; ignora Potrero, CantidadReses y los contadores.</summary>
        public void Informar(ContextoAviso contexto, IReceptorEventos receptor)
            => Informar_Peso_Venta(contexto.Res, receptor);

        //Metodo para informar si la res está apta para la venta
        public void Informar_Peso_Venta(Res res, IReceptorEventos receptor)
        {
            try
            {
                    // ADR-05 · La regla la responde ahora la propia res.
                    uint peso_apto = res.PesoRecomendadoVenta;

                    //Informar si la res está apta para la venta
                    if (res.Peso >= peso_apto)
                    {
                        string mensaje = $"[Evento] La res '{res.Nombre}' tiene un peso {res.Peso}, apta para venta.";
                        receptor.Notificar(mensaje);
                    }

            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo Informar_Peso_Venta: " + er.Message);
            }
        }
    }
}
