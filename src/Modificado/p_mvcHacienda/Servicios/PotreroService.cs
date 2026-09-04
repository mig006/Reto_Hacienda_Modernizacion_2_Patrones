using Bib_Hacienda.Clases;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using static Bib_Hacienda.Clases.Potrero;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-02 · Servicio de aplicación de potreros.
    ///
    /// Antes recibía dos CLASES CONCRETAS por constructor: Hacienda y PersistenciaService
    /// (PotreroService.cs:9-17, H-08). Ahora la persistencia entra por
    /// <see cref="GuardadoValidado"/>, que a su vez depende solo de contratos del dominio.
    ///
    /// Verificación de ADR-02: ninguna clase de este paquete nombra un tipo del paquete
    /// de detalle técnico. Por eso este comentario tampoco escribe su nombre.
    /// </summary>
    public class PotreroService
    {
        // Atributos
        private readonly Hacienda _hacienda;
        private readonly GestorPotreros _gestorPotreros;
        private readonly GestorReses _gestorReses;
        private readonly GuardadoValidado _guardado;

        // Constructor
        //
        // ADR-04 · Costo aceptado de partir Hacienda: donde antes entraba una sola clase
        // de 558 líneas, ahora entran dos servicios de dominio con nombre. Es más
        // constructor, pero cada dependencia declara exactamente qué reglas usa este
        // servicio, y ninguna arrastra las otras cinco responsabilidades.
        public PotreroService(Hacienda hacienda, GestorPotreros gestorPotreros,
            GestorReses gestorReses, GuardadoValidado guardado)
        {
            _hacienda = hacienda;
            _gestorPotreros = gestorPotreros;
            _gestorReses = gestorReses;
            _guardado = guardado;
        }

        // Crear un nuevo potrero
        public ResultadoOperacion CrearPotrero(string identificacion, l_tipos_potreros tipo)
        {
            try
            {
                string validado;
                // Verificar si ya existe un potrero con esa identificación
                if (_hacienda.L_potreros.Any(p => p.Identificacion == identificacion))
                {
                    throw new InvalidOperationException($"Ya existe un potrero con la identificación '{identificacion}'");
                }

                // Intentar crear el potrero (mensaje de evento del dominio)
                string resultado = _gestorPotreros.crear_potrero(identificacion, tipo);

                // Guardar los cambios CON VALIDACIÓN (mensaje del contrato de texto congelado)
                validado = GuardadoValidado.Texto(_guardado.GuardarPotreros(_hacienda.L_potreros));

                // Mensaje compuesto: evento + guardado. ADR-08a: el texto no cambia; lo
                // que cambia es que ahora viaja acompañado de un booleano.
                return ResultadoOperacion.Exitoso($"{resultado}. {validado}");
            }
            catch (InvalidOperationException)
            {
                // ADR-08a · BLINDAJE: esto sigue LANZANDO, no devolviendo Fallido.
                // Convertirlo en un retorno haría que PotreroController.Create redirigiera
                // al índice en verde en lugar de re-renderizar el formulario en rojo.
                throw new InvalidOperationException("Validación fallida: El potrero no cumple los requisitos");
            }
            catch (Exception ex)
            {
                // Re-lanzar la excepción para que el controlador la maneje
                throw new Exception($"Error al crear el potrero: {ex.Message}");
            }
        }

        // Obtener todos los potreros
        public List<Potrero> ObtenerTodosLosPotreros()
        {
            return _hacienda.L_potreros.OrderBy(p => p.Identificacion).ToList();
        }

        // Obtener un potrero por identificación
        public Potrero? ObtenerPotreroPorIdentificacion(string identificacion)
        {
            try
            {
                return _gestorPotreros.buscar_potrero(identificacion);
            }
            catch
            {
                return null;
            }
        }

        // Agregar una res al potrero
        public ResultadoOperacion AgregarRes(string potreroId, string nombreRes, ushort edad, uint peso)
        {
            try
            {
                string validado;

                // Verificar que el potrero existe
                var potrero = _gestorPotreros.buscar_potrero(potreroId);
                if (potrero == null)
                {
                    throw new InvalidOperationException($"No se encontró el potrero '{potreroId}'");
                }

                // Verificar que no existe una res con ese nombre en el potrero
                if (potrero.L_reses.Any(r => r.Nombre == nombreRes))
                {
                    throw new InvalidOperationException($"Ya existe una res con el nombre '{nombreRes}' en el potrero '{potreroId}'");
                }

                // Usar el método de Hacienda (mensaje de evento del dominio)
                string resultado = _gestorReses.anadir_res_potrero(potreroId, nombreRes, edad, peso);

                // Guardar con validación
                validado = GuardadoValidado.Texto(_guardado.GuardarReses(_hacienda.L_potreros));

                // Mensaje compuesto: evento + guardado
                return ResultadoOperacion.Exitoso($"{resultado}. {validado}");
            }
            catch (InvalidOperationException)
            {
                // ADR-04 · Este catch DESCARTA el mensaje original y lo sustituye por un
                // texto genérico. Es pérdida de información y está diagnosticado, pero el
                // texto es lo que ve el operario: se conserva carácter por carácter.
                throw new InvalidOperationException("Validación fallida: La res no cumple los requisitos");
            }
            catch (Exception ex)
            {
                // Re-lanzar la excepción para que el controlador la maneje
                throw new Exception($"Error al agregar la res: {ex.Message}");
            }
        }

        // Obtener estadísticas
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            var potreros = _hacienda.L_potreros;

            return new Dictionary<string, object>
            {
                { "TotalPotreros", potreros.Count },
                { "TotalReses", potreros.Sum(p => p.L_reses.Count) },
                { "PotrerosVacios", potreros.Count(p => p.L_reses.Count ==0) },
                { "PotrerosConReses", potreros.Count(p => p.L_reses.Count >0) }
            };
        }
    }
}
