using Bib_Hacienda.Clases;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Valores;
using System;
using System.Linq;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-04 · Esquema y límites de vacunación.
    ///
    /// Razón de cambio que la gobierna: el esquema sanitario.
    /// Interlocutor que la solicita: el veterinario.
    ///
    /// Absorbe aplicar_vacuna (Hacienda.cs:451) y sustituye a la interfaz IVacunacion,
    /// que era un contrato de un solo método declarado sobre una clase de seis
    /// responsabilidades: al existir este tipo, pierde su razón de ser.
    ///
    /// Aquí se ve la ganancia de SRP en concreto: si el veterinario cambia el esquema de
    /// vacunación, antes había que abrir Hacienda —558 líneas, con las reglas de potreros
    /// y de ventas al lado—; ahora solo se abre este archivo.
    /// </summary>
    public class ServicioVacunacion
    {
        private readonly Hacienda _hacienda;
        private readonly GestorPotreros _gestorPotreros;

        //Eventos
        private PublisherVacunacionCompletada publisher_vacunacion_completa = new PublisherVacunacionCompletada();
        private PublisherVacunaVencida publisher_vacuna_vencida = new PublisherVacunaVencida();

        public ServicioVacunacion(Hacienda hacienda, GestorPotreros gestorPotreros)
        {
            _hacienda = hacienda;
            _gestorPotreros = gestorPotreros;
        }

        //Metodo para aplicar vacuna
        public string aplicar_vacuna(Vacuna vacuna, string nombre, string id_potrero)
        {
            try
            {
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                Res res = potrero.buscar_res( nombre);
                //Contadores de vacunas aplicadas
                byte contador_bacterianas = 0;
                byte contador_vivas = 0;

                //Validar parámetros
                if (vacuna == null) throw new ArgumentNullException(nameof(vacuna));
                if (res == null) throw new ArgumentNullException(nameof(res));

                // Validar si la vacuna ya fue aplicada (por nombre o lote)
                if (res.L_vacunas_aplicadas.Any(v => v.Nombre == vacuna.Nombre || v.Lote == vacuna.Lote))
                    throw new Exception($"La vacuna '{vacuna.Nombre}' ya fue aplicada a la res '{res.Nombre}'.");

                //Contar las vacunas ya aplicadas a la res
                foreach (Vacuna vac in res.L_vacunas_aplicadas)
                {
                    if (vac is Bacteriana)
                    {
                        contador_bacterianas++;
                    }
                    else if (vac is Viva)
                    {
                        contador_vivas++;
                    }
                }

                //Determinar el maximo segun el tipo de res
                //
                // ADR-05 · Antes esto era una cadena de tres `is` sobre el tipo concreto
                // (Hacienda.cs:487-501) que leía constantes de ReglaVacuna. Ahora la res
                // responde por su propio esquema y el compilador obliga a que todo subtipo
                // nuevo lo declare.
                byte max_bac = res.MaxVacunasBacterianas;
                byte max_viv = res.MaxVacunasVivas;

                // Validar límites antes de aplicar
                if (vacuna is Bacteriana && contador_bacterianas >= max_bac)
                    throw new Exception($"No se puede aplicar más vacunas bacterianas a la res '{res.Nombre}'. Ya tiene las {max_bac} permitidas.");

                if (vacuna is Viva && contador_vivas >= max_viv)
                    throw new Exception($"No se puede aplicar más vacunas vivas a la res '{res.Nombre}'. Ya tiene las {max_viv} permitidas.");


                // ADR-07 · Dos receptores distintos porque los dos eventos se consumían
                // de forma distinta: cada lambda tenía su propia variable capturada y ambas
                // se quedaban con el ÚLTIMO mensaje, no con la concatenación.
                var eventoVencimiento = new AcumuladorMensajes();

                //Validar fecha de vencimiento de la vacuna
                bool vacuna_vencida = publisher_vacuna_vencida.Informar_Vacuna_Vencida(vacuna, eventoVencimiento);

                if (vacuna_vencida)
                {
                    throw new Exception(eventoVencimiento.Ultimo);
                }
                else
                {
                    res.L_vacunas_aplicadas.Add(vacuna);
                    _hacienda.L_vacunas.Remove(vacuna);

                    //Actualizar contadores
                    if (vacuna is Bacteriana)
                    {
                        contador_bacterianas++;
                    }
                    else if (vacuna is Viva)
                    {
                        contador_vivas++;
                    }

                    var eventoEsquema = new AcumuladorMensajes();

                    //Disparar evento de vacunacion completa
                    bool esquema_completo = publisher_vacunacion_completa.Informar_Vacunacion_Completada(res, contador_bacterianas, contador_vivas, eventoEsquema);

                    return $"Vacuna aplicada correctamente a la res {res.Nombre}. {eventoEsquema.Ultimo}";
                }

            }
            catch (Exception err)
            {
                throw new Exception("Error inesperado en el metodo aplicar_vacuna: " + err.Message);
            }
        }
    }
}
