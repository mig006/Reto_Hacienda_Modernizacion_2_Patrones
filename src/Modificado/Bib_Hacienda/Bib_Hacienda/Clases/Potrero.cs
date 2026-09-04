using System;
using System.Collections.Generic;
using System.Linq;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases
{
    public class Potrero
    {

        //Atributos
        public enum l_tipos_potreros {ternero, novillo, cebon};
        private string identificacion;
        private List<Res> l_reses = new List<Res>();
        private l_tipos_potreros tipo_potrero;

        //Eventos
        private PublisherPotreroMitad publisher_potrero_mitad = new PublisherPotreroMitad();
        private PublisherPotreroLleno publisher_potrero_lleno = new PublisherPotreroLleno();
        private PublisherPesoVenta publisher_peso_venta = new PublisherPesoVenta();
        private PublisherPesoMin publisher_peso_min = new PublisherPesoMin();

        //EventHandler
        internal void EventHandler() { }

        //Constructor
        public Potrero(string identificacion, l_tipos_potreros tipo_potrero)
        {
            this.Identificacion = identificacion;
            this.tipo_potrero = tipo_potrero;

        }

        /// <summary>
        /// ADR-05 · Añade una res al potrero.
        ///
        /// ══ De 124 líneas a ~35 ══════════════════════════════════════════════════
        /// El método original (Potrero.cs:38-161, H-11) hacía SIETE cosas: validaba el
        /// nombre, controlaba la capacidad, traducía el tipo de potrero a un rango de
        /// edad con un switch, construía la subclase concreta con un SEGUNDO switch
        /// sobre una cadena mágica, suscribía cuatro publishers, disparaba cuatro
        /// eventos y componía el mensaje de pantalla.
        ///
        /// Ahora solo: consulta la política, pide la res a la fábrica, la agrega y notifica.
        ///   · la capacidad la decide PoliticaCapacidadPotrero;
        ///   · los DOS switch los sustituye IFabricaRes, que además expone su rango para
        ///     que la precondición se consulte en lugar de descubrirse por excepción.
        ///
        /// ══ Lo que NO se puede tocar ═════════════════════════════════════════════
        /// El ORDEN de las comprobaciones y el ORDEN DE DISPARO de los eventos son
        /// comportamiento observable. Los eventos se disparan mitad → lleno → peso
        /// mínimo → peso de venta (Potrero.cs:135-138), que NO es el mismo orden en que
        /// se suscriben. Y el wrapper del catch conserva su literal exacto, con "metodo"
        /// sin tilde, porque es un eslabón de la cadena de mensajes anidados que ve el
        /// operario.
        ///
        /// Las fábricas y la política llegan POR PARÁMETRO, no por constructor: Potrero
        /// es una entidad con identidad, y el contenedor no la construye. Es el mismo
        /// criterio que ADR-07 aplica al receptor de eventos.
        /// </summary>
        public string anadir_res(string nombre, ushort edad, uint peso,
            PoliticaCapacidadPotrero politica, IReadOnlyCollection<IFabricaRes> fabricas)
        {
            try
            {
                //Validar parámetros
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre de la res no puede estar vacío", nameof(nombre));
                }

                if (!politica.Cabe(l_reses.Count()))
                {
                    //Validacion de potrero lleno
                    throw new Exception($"La res no puede ser añadida al potrero {this.identificacion} porque este está lleno");
                }

                //La fábrica del tipo de este potrero sustituye a los dos switch
                IFabricaRes fabrica = fabricas.First(f => f.TipoPotreroSoportado == tipo_potrero);

                //Validar que la edad de la res esté dentro del rango permitido para el potrero
                if (!fabrica.RangoSoportado.Contiene(edad))
                {
                    throw new Exception($"La res no puede ser añadida al potrero {this.identificacion} porque su edad no corresponde al tipo de potrero");
                }

                Res res = fabrica.Crear(nombre, peso, edad);
                l_reses.Add(res);

                //Cuenta las reses actuales en el potrero
                ushort cantidad_reses = (ushort)L_reses.Count();

                // ADR-07 · Aquí había CUATRO suscripciones con += que nunca se deshacían,
                // en un método que se ejecuta en cada alta de ganado. El receptor ahora se
                // entrega a cada publisher y muere con esta llamada: la fuga desaparece por
                // construcción, sin try/finally y sin nada que recordar desuscribir.
                var eventos = new AcumuladorMensajes();

                // EL ORDEN DE DISPARO ES COMPORTAMIENTO OBSERVABLE: mitad, lleno, peso
                // mínimo, peso de venta (Potrero.cs:135-138). Nótese que NO es el mismo
                // orden en que el código original se suscribía, que era irrelevante.
                publisher_potrero_mitad.Informar_Potrero_Mitad(cantidad_reses, this, eventos);
                publisher_potrero_lleno.Informar_Potrero_Lleno(cantidad_reses, this, eventos);
                publisher_peso_min.Informar_Peso_Min(res, eventos);
                publisher_peso_venta.Informar_Peso_Venta(res, eventos);

                //Construir mensaje de retorno
                string mensaje_final = $"La res {nombre} ha sido añadida al potrero {this.identificacion} con exito.";
                if (eventos.HayMensajes)
                {
                    mensaje_final += "\n" + eventos.Texto.TrimEnd();
                }

                return mensaje_final;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inesperado en el metodo anadir_res: " + ex.Message);
            }

        }

        //Metodo para buscar res por el nombre
        public Res buscar_res(string nombre)
        {
            try
            {
                // Validar nombre
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre de búsqueda no puede estar vacío.");
                }

                // Buscar la res que contengan el texto (ignorando mayúsculas/minúsculas)
                var res_encontrada = l_reses
                    .Where(p => p.Nombre.IndexOf(nombre, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                // Si no hay resultados
                if (res_encontrada.Count == 0)
                {
                    throw new Exception($"No se encontró ningúna vaca con el nombre o coincidencia '{nombre}'.");
                }

                // Si hay más de un resultado, mostrar opciones
                if (res_encontrada.Count > 1)
                {
                    throw new Exception($" se encontró mas de una res con el nombre o coincidencia '{nombre}'.");
                }

                //  devolver potrero
                return res_encontrada.First();
            }
            catch (Exception er)
            {
                // ERRATA CONGELADA: nombra al método equivocado. Se conserva porque el
                // texto llega a la pantalla del operario (ADRs.md §8.3).
                throw new Exception("Error inesperado en el método buscar_potrero: " + er.Message);
            }
        }

        //Accesores
        public List<Res> L_reses { get => l_reses; set => l_reses = value; }
        public string Identificacion { get => identificacion; set => identificacion = value; }
        public l_tipos_potreros Tipo_potrero { get => tipo_potrero; set => tipo_potrero = value; }

    }
}
