using Bib_Hacienda.Clases;
using System;
using System.Linq;
using static Bib_Hacienda.Clases.Potrero;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-04 · Reglas de creación y unicidad de potreros.
    ///
    /// Razón de cambio que la gobierna: la política de organización de la finca.
    /// Interlocutor que la solicita: el administrador de la finca.
    ///
    /// Absorbe crear_potrero (Hacienda.cs:61) y buscar_potrero (:91).
    /// </summary>
    public class GestorPotreros
    {
        private readonly Hacienda _hacienda;

        public GestorPotreros(Hacienda hacienda)
        {
            _hacienda = hacienda;
        }

        //Metodo para crear potreros
        public string crear_potrero(string indentificacion, l_tipos_potreros tipo_potrero)
        {
            try
            {
                //Validar que el nombre no este vacio o nulo
                if (string.IsNullOrWhiteSpace(indentificacion))
                {
                    throw new ArgumentException("El nombre de la res no puede estar vacío", nameof(indentificacion));
                }
                if (_hacienda.L_potreros.Any(p => p.Identificacion.Equals(indentificacion, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Ya existe un potrero con el nombre '{indentificacion}'.");
                }

                //Crear nuevo potrero

                Potrero nuevo_potrero = new Potrero(indentificacion, tipo_potrero);

                _hacienda.AgregarPotrero(nuevo_potrero);

                return ($"El potrero {indentificacion} se a añadido a la hacienda. ");

            }
            catch (Exception er)
            {
                // "metodo" sin tilde, como en Hacienda.cs:86. La mezcla de con/sin tilde
                // del original se conserva: es texto que llega a la pantalla.
                throw new Exception("Error inesperado en el metodo crear_potrero: " + er.Message);
            }
        }

        /// <summary>
        /// Busca un potrero por SUBCADENA, no por igualdad, y lanza si encuentra cero o
        /// más de uno (Hacienda.cs:91-125). Se conserva tal cual, incluido el espacio
        /// inicial del mensaje de coincidencia múltiple.
        /// </summary>
        public Potrero buscar_potrero(string nombre)
        {
            try
            {
                // Validar nombre
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre de búsqueda no puede estar vacío.");
                }

                // Buscar potreros que contengan el texto (ignorando mayúsculas/minúsculas)
                var potreros_encontrados = _hacienda.L_potreros
                    .Where(p => p.Identificacion.IndexOf(nombre, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                // Si no hay resultados
                if (potreros_encontrados.Count == 0)
                {
                    throw new Exception($"No se encontró ningún potrero con el nombre o coincidencia '{nombre}'.");
                }

                // Si hay más de un resultado, mostrar opciones
                if (potreros_encontrados.Count > 1)
                {
                    throw new Exception($" se encontró mas de un potrero con el nombre o coincidencia '{nombre}'.");
                }

                //  devolver potrero
                return potreros_encontrados.First();
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el método buscar_potrero: " + er.Message);
            }
        }
    }
}
