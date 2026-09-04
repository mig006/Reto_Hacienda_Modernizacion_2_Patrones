using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using System;

namespace Bib_Hacienda.Eventos
{
    /// <summary>ADR-07 · Informa si una res completó su esquema de vacunación.</summary>
    public class PublisherVacunacionCompletada
    {
        //Metodo para informar que una res ha completado su esquema de vacunacion
        public bool Informar_Vacunacion_Completada(Res res, ushort contador_bacterianas, ushort contador_vivas, IReceptorEventos receptor)
        {
            try
            {
                if (res == null)
                {
                    throw new ArgumentNullException(nameof(res), "La res no puede ser null");
                }

                //Verificar si la res ha completado su esquema de vacunacion
                // ADR-05 · Antes eran tres ramas que repetían la misma comparación con
                // constantes distintas por tipo. Ahora la pregunta se le hace a la res.
                bool esquema_completo = res.EsquemaCompleto(contador_bacterianas, contador_vivas);

                // Disparar el evento con el mensaje apropiado
                string mensaje;
                if (esquema_completo)
                {
                    mensaje = $"[Evento] La res '{res.Nombre}' ha completado su esquema de vacunación.";
                }
                else
                {
                    mensaje = $"[Evento] La res '{res.Nombre}' aún no ha completado su esquema de vacunación. Bacterianas: {contador_bacterianas}, Vivas: {contador_vivas}";
                }
                receptor.Notificar(mensaje);

                return esquema_completo;
            }
            catch (Exception er)
            {
                throw new Exception("[evento] Error inesperado en el metodo Informar_Vacunacion_Completada: " + er.Message);
            }
        }
    }
}
