using System;
using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Eventos
{
    /// <summary>
    /// ADR-07 · Observer (P-03, Actividad 2) · Informa si una res está por debajo de su
    /// peso mínimo.
    ///
    /// El delegado y el evento desaparecieron: el destinatario llega por parámetro y
    /// vive lo que dura la llamada. Y con ellos desapareció el OPERADOR IMPLÍCITO que
    /// esta clase declaraba desde PublisherPesoVenta y cuyo cuerpo era lanzar (H-15):
    /// el compilador autorizaba una conversión que en ejecución siempre fallaba, y creaba
    /// una dependencia entre dos publishers que no tienen ninguna relación. Ningún
    /// cliente lo invocaba, así que borrarlo no cambió ninguna salida: cuatro líneas
    /// menos y el arreglo con mejor relación costo/beneficio de todo el diagnóstico.
    /// </summary>
    public class PublisherPesoMin : IPublicadorEvento
    {
        /// <summary>Lee Res de ContextoAviso; ignora Potrero, CantidadReses y los contadores.</summary>
        public void Informar(ContextoAviso contexto, IReceptorEventos receptor)
            => Informar_Peso_Min(contexto.Res, receptor);

        //Metodo para informar si la res está por debajo del peso mínimo
        public void Informar_Peso_Min(Res res, IReceptorEventos receptor)
        {
            try
            {
                // ADR-05 · Antes, una cadena de tres `is` decidía DESDE FUERA una regla
                // que es propiedad del tipo de res, y si alguien agregaba un cuarto tipo y
                // se olvidaba de tocar aquí, el peso mínimo quedaba en 0: un animal que
                // nunca se reportaba como desnutrido, y el compilador no decía nada.
                // Ahora la regla vive en la res y el compilador obliga a declararla.
                uint peso_minimo = res.PesoMinimo;

                //Informar si la res está en desnutrición
                if (res.Peso < peso_minimo)
                {
                    string mensaje = $"[Evento] La res '{res.Nombre}' tiene un peso {res.Peso}, está en desnutrición.";
                    receptor.Notificar(mensaje);
                }
            }
            catch (Exception er)
            {
                throw new Exception("[Evento] Error inesperado en el metodo Informar_Peso_Min: " + er.Message);
            }
        }
    }
}
