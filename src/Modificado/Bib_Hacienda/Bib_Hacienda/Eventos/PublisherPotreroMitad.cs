using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Reglas;
using System;

namespace Bib_Hacienda.Eventos
{
    /// <summary>ADR-07 · Informa que el potrero alcanzó la mitad de su capacidad.</summary>
    public class PublisherPotreroMitad
    {
        //Metodo que salta el evento cuando el potrero alcanza la mitad de su capacidad
        public void Informar_Potrero_Mitad(ushort cantidad_reses, Potrero potrero, IReceptorEventos receptor)
        {
            try
            {
                //Capacidad a la mitad del potrero
                ushort capacidad_mitad = (ushort)(ReglaPotrero.max_reses_potrero / 2);

                //Validar si la cantidad de reses es igual a la capacidad a la mitad
                if (cantidad_reses == capacidad_mitad)
                {
                    string mensaje = $"[Evento] El potrero '{potrero.Identificacion}' ha alcanzado la mitad de su capacidad máxima de reses.";
                    receptor.Notificar(mensaje);
                }
            }
            catch (Exception er)
            {
                throw new Exception("[Evento] Error inesperado en el metodo Informar_Potrero_Mitad: " + er.Message);
            }
        }
    }
}
