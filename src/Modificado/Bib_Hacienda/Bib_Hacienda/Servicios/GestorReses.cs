using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Valores;
using System;
using System.Collections.Generic;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-04 · Reglas de ingreso y engorde de ganado.
    ///
    /// Razón de cambio que la gobierna: la política de manejo del ganado.
    /// Interlocutor que la solicita: el operario de campo.
    ///
    /// Absorbe anadir_res_potrero (Hacienda.cs:128) y las dos sobrecargas de
    /// alimentar_res (:171 y :220).
    ///
    /// Recibe la política de capacidad y las fábricas porque se las tiene que entregar a
    /// Potrero.anadir_res, que es una entidad y por tanto no las puede pedir al contenedor.
    /// </summary>
    public class GestorReses
    {
        private readonly Hacienda _hacienda;
        private readonly GestorPotreros _gestorPotreros;
        private readonly PoliticaCapacidadPotrero _politica;
        private readonly IReadOnlyCollection<IFabricaRes> _fabricas;

        //Eventos
        private PublisherPesoMin publisher_peso_min = new PublisherPesoMin();
        private PublisherPesoVenta publisher_peso_ideal = new PublisherPesoVenta();

        public GestorReses(Hacienda hacienda, GestorPotreros gestorPotreros,
            PoliticaCapacidadPotrero politica, IEnumerable<IFabricaRes> fabricas)
        {
            _hacienda = hacienda;
            _gestorPotreros = gestorPotreros;
            _politica = politica;
            _fabricas = new List<IFabricaRes>(fabricas);
        }

        //Metodo para  anadir res a un potrero
        public string anadir_res_potrero(string id_potrero, string nombre, ushort edad, uint peso)
        {
            try
            {
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                string resultado = potrero.anadir_res(nombre, edad, peso, _politica, _fabricas);
                return resultado;
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el método anadir_res_potrero: " + er.Message);
            }
        }

        //Metodo para alimentar una res
        public string alimentar_res(string id_potrero, string nombre)
        {
            try
            {
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                Res res = potrero.buscar_res(nombre);
                string mensaje_final= "";

                //Validar parámetros
                if (potrero == null) throw new ArgumentNullException(nameof(potrero));
                if (res == null) throw new ArgumentNullException(nameof(res));

                //Alimentar la res (incrementa el peso)
                res.Peso ++;

                // ADR-07 · Dos suscripciones por llamada que nunca se deshacían.
                var eventos = new AcumuladorMensajes();

                //Disparar los eventos con la res actualizada
                publisher_peso_min.Informar_Peso_Min(res, eventos);
                publisher_peso_ideal.Informar_Peso_Venta(res, eventos);

                //Construir mensaje de retorno
                mensaje_final = $"La res '{res.Nombre}' ha sido alimentada, ahora pesa {res.Peso} kg.";
                if (eventos.HayMensajes)
                {
                    mensaje_final += "\n" + eventos.Texto.TrimEnd();
                }
                return mensaje_final;
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo alimentar_res: " + er.Message);
            }
        }

        /// <summary>
        /// SC-2 · ADR-11 · Conecta un chip de geolocalización a una res existente.
        ///
        /// Nótese lo que este método NO hace: no construye la res, no pasa por
        /// `Potrero.anadir_res`, no toca ninguna `IFabricaRes` y no conoce ningún subtipo.
        /// El chip llega a un animal que ya está en el potrero, que es como funciona el
        /// negocio y también como el rediseño lo hace barato.
        ///
        /// Es una de las salidas nuevas autorizadas por §8.7 (A-2).
        /// </summary>
        public string asignar_chip(string id_potrero, string nombre_res, Chip chip)
        {
            try
            {
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                Res res = potrero.buscar_res(nombre_res);

                if (chip == null) throw new ArgumentNullException(nameof(chip));

                if (ResConChip(chip.Identificador) != null)
                {
                    throw new Exception($"Ya existe un chip con el identificador '{chip.Identificador}' conectado en la hacienda");
                }

                bool reemplazo = res.Chip != null;
                res.AsignarChip(chip);

                return reemplazo
                    ? $"El chip {chip.Identificador} reemplazó al anterior en la res {res.Nombre}."
                    : $"El chip {chip.Identificador} fue conectado a la res {res.Nombre}.";
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo asignar_chip: " + er.Message);
            }
        }

        /// <summary>
        /// SC-2 · ADR-11 · Registra una lectura de posición enviada por un dispositivo.
        /// El objeto de valor se reemplaza, no se muta.
        /// </summary>
        public string registrar_posicion(string identificador, double latitud, double longitud)
        {
            try
            {
                Res res = ResConChip(identificador);
                if (res == null)
                {
                    throw new Exception($"No se encontró ninguna res con el chip '{identificador}'");
                }

                res.RegistrarPosicion(latitud, longitud, DateTime.Now);
                return $"Posición del chip {identificador} actualizada para la res {res.Nombre}.";
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo registrar_posicion: " + er.Message);
            }
        }

        private Res ResConChip(string identificador)
        {
            foreach (var potrero in _hacienda.L_potreros)
            {
                foreach (var res in potrero.L_reses)
                {
                    if (res.Chip != null && res.Chip.Identificador.Equals(identificador, StringComparison.OrdinalIgnoreCase))
                    {
                        return res;
                    }
                }
            }
            return null;
        }

        //Metodo sobrecargado para alimentar una res con una cantidad de alimento especifica
        public string alimentar_res(string id_potrero, string nombre, uint cantidadAlimento)
        {
            try
            {
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                Res res = potrero.buscar_res(nombre);

                //Validar parámetros
                if (potrero == null) throw new ArgumentNullException(nameof(potrero));
                if (res == null) throw new ArgumentNullException(nameof(res));

                res.Peso += cantidadAlimento;

                // ADR-07 · Dos suscripciones por llamada que nunca se deshacían. En una
                // jornada de 200 alimentaciones había 400 manejadores vivos.
                var eventos = new AcumuladorMensajes();

                //Disparar los eventos con la res actualizada
                publisher_peso_min.Informar_Peso_Min(res, eventos);
                publisher_peso_ideal.Informar_Peso_Venta(res, eventos);

                //Construir mensaje de retorno
                string mensaje_final = $"La res '{res.Nombre}' ha sido alimentada, ahora pesa {res.Peso} kg.";
                if (eventos.HayMensajes)
                {
                    mensaje_final += "\n" + eventos.Texto.TrimEnd();
                }

                return mensaje_final;
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo alimentar_res: " + er.Message);
            }
        }
    }
}
