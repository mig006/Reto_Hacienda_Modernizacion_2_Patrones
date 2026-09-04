using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-02 · Servicio de aplicación de reses.
    ///
    /// ── Sobre H-19, que merece una precisión ──────────────────────────────────────
    /// El hallazgo decía que ResService recibía PersistenciaService por constructor y
    /// que ninguno de sus métodos lo usaba, y ADRs.md §3.2 anota que ResService «solo
    /// pierde» dependencias. Aquí gana una: <see cref="GuardadoValidado"/>.
    ///
    /// No es una contradicción, es dónde quedó la orquestación. Antes, alimentar y
    /// vender NO pasaban por ningún servicio: ResController.cs:113-116 y :165-169
    /// llamaban directamente a Hacienda y a PersistenciaService desde el controlador.
    /// ADR-02 exige que el controlador pierda ambas, y ADR-03 que la validación sea una
    /// decisión explícita del servicio de aplicación; las dos cosas juntas obligan a que
    /// esas dos operaciones bajen aquí. La alternativa —inyectar seis dependencias en
    /// ResController— reintroduciría el acoplamiento que ADR-02 elimina.
    ///
    /// Lo que H-19 denunciaba se cumple igual: desaparece la dependencia de la clase
    /// gorda de 643 líneas que no se usaba, y entra un colaborador estrecho que sí se usa.
    /// </summary>
    public class ResService
    {
        // Atributos
        private readonly Hacienda _hacienda;
        private readonly GestorPotreros _gestorPotreros;
        private readonly GestorReses _gestorReses;
        private readonly ServicioVenta _servicioVenta;
        private readonly GuardadoValidado _guardado;
        private readonly IValidador<Chip> _validadorChip;

        // Constructor
        //
        // SC-2 · IValidador<Chip> entra como un colaborador más. No hubo que cambiar
        // ningún otro parámetro: es una adición, no una reescritura de la firma.
        public ResService(Hacienda hacienda, GestorPotreros gestorPotreros,
            GestorReses gestorReses, ServicioVenta servicioVenta, GuardadoValidado guardado,
            IValidador<Chip> validadorChip)
        {
            _hacienda = hacienda;
            _gestorPotreros = gestorPotreros;
            _gestorReses = gestorReses;
            _servicioVenta = servicioVenta;
            _guardado = guardado;
            _validadorChip = validadorChip;
        }

        // Obtener todas las reses de todos los potreros
        public List<(Potrero Potrero, Res Res)> ObtenerTodasLasReses()
        {
            // Lista para almacenar las reses junto con su potrero
            var resesConPotrero = new List<(Potrero, Res)>();

            // Recorrer cada potrero y sus reses
            foreach (var potrero in _hacienda.L_potreros)
            {
                // Agregar cada res junto con su potrero a la lista
                foreach (var res in potrero.L_reses)
                {
                    resesConPotrero.Add((potrero, res));
                }
            }

            return resesConPotrero;
        }

        // Buscar res en un potrero
        public Res? BuscarRes(string potreroId, string nombreRes) //signo de pregunta porque es nulleable o sea que
                                                                 //busca una res y si no la encuentra devuelve null
        {
            try
            {
                // Buscar el potrero por su identificación
                var potrero = _gestorPotreros.buscar_potrero(potreroId);
                return potrero.buscar_res(nombreRes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Igual que <see cref="BuscarRes"/> pero SIN tragarse la excepción.
        ///
        /// Existe porque ResController.DetalleVacunas muestra al operario el mensaje de
        /// error de la búsqueda (ResController.cs:56-61), y ese texto —con su cadena de
        /// wrappers anidados— es comportamiento observable. Si esa acción usara
        /// BuscarRes, el mensaje se perdería y la pantalla diría otra cosa.
        /// </summary>
        public Res BuscarResEstricta(string potreroId, string nombreRes)
        {
            var potrero = _gestorPotreros.buscar_potrero(potreroId);
            return potrero.buscar_res(nombreRes);
        }

        /// <summary>
        /// Alimenta una res y persiste el cambio.
        ///
        /// Traducción literal de ResController.cs:106-129: se alimenta, se guardan las
        /// reses y se devuelve el mensaje del dominio. El resultado del guardado se
        /// descarta, igual que hoy.
        /// </summary>
        public ResultadoOperacion AlimentarRes(string potreroId, string nombreRes, uint cantidadAlimento)
        {
            // ADR-08a · BLINDAJE: no hay catch. Si el dominio lanza, la excepción sube
            // hasta el controlador tal cual, igual que hoy.
            string mensaje = _gestorReses.alimentar_res(potreroId, nombreRes, cantidadAlimento);
            _guardado.GuardarReses(_hacienda.L_potreros);
            return ResultadoOperacion.Exitoso(mensaje);
        }

        /// <summary>
        /// Vende una res y persiste el cambio.
        ///
        /// Traducción literal de ResController.cs:132-181. El ORDEN de las dos escrituras
        /// —primero ventas, después reses— se conserva: si el proceso se interrumpe a
        /// mitad, el estado en disco debe ser el mismo que hoy (ADR-02).
        /// </summary>
        public ResultadoOperacion VenderRes(string potreroId, string nombreRes, uint monto)
        {
            string mensaje = _servicioVenta.vender_res(potreroId, nombreRes, monto);
            _guardado.GuardarVentas(_hacienda.L_ventas);
            _guardado.GuardarReses(_hacienda.L_potreros);
            return ResultadoOperacion.Exitoso(mensaje);
        }

        /// <summary>
        /// SC-2 · ADR-11 · Conecta un chip de geolocalización a una res y persiste.
        ///
        /// Sigue el mismo patrón que el resto del servicio: validar explícitamente
        /// (ADR-03), delegar la regla al dominio y persistir por contrato (ADR-02). La
        /// escritura reutiliza `GuardarReses`: el chip viaja con su res, no por un canal
        /// aparte, que es la consecuencia directa de ADR-12.
        ///
        /// Es una salida nueva autorizada por §8.7 (A-2).
        /// </summary>
        public ResultadoOperacion AsignarChip(string potreroId, string nombreRes,
            string identificador, double latitud, double longitud)
        {
            try
            {
                var chip = new Chip(identificador, latitud, longitud, DateTime.Now);

                ResultadoValidacion validacion = _validadorChip.Validar(chip);
                if (!validacion.EsValido)
                {
                    return ResultadoOperacion.Fallido(validacion.Mensaje);
                }

                string resultado = _gestorReses.asignar_chip(potreroId, nombreRes, chip);
                string validado = GuardadoValidado.Texto(_guardado.GuardarReses(_hacienda.L_potreros));

                return ResultadoOperacion.Exitoso($"{resultado} {validado}");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Fallido($"{ex.Message}");
            }
        }

        /// <summary>
        /// SC-2 · ADR-11 · Registra una lectura de posición enviada por un dispositivo.
        ///
        /// No tiene pantalla propia: §8.7 cierra el inventario de salidas nuevas en cuatro
        /// y ninguna es una pantalla de seguimiento. La operación existe porque el caso de
        /// caracterización 18 la ejercita y porque es la mitad viva de la funcionalidad —
        /// un chip que no puede reportar posición no geolocaliza nada.
        /// </summary>
        public ResultadoOperacion RegistrarPosicion(string identificador, double latitud, double longitud)
        {
            try
            {
                string resultado = _gestorReses.registrar_posicion(identificador, latitud, longitud);
                _guardado.GuardarReses(_hacienda.L_potreros);
                return ResultadoOperacion.Exitoso(resultado);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Fallido($"{ex.Message}");
            }
        }

        // Obtener estadísticas de reses
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            // Obtener todas las reses
            var todasLasReses = ObtenerTodasLasReses();

            // Estadísticas con orden correcto:
            // Terneros (0-12 meses) = jóvenes
            // Cebones (13-48 meses) = medios
            // Novillos (49+ meses) = viejos
            var estadisticas = new Dictionary<string, object>
            {
                { "TotalReses", todasLasReses.Count }
            };

            foreach (var par in ClavesDeVistaPorTipo)
            {
                estadisticas[par.Value] = todasLasReses.Count(r => r.Res.GetType() == par.Key);
            }

            estadisticas["PesoPromedio"] = todasLasReses.Any() ? todasLasReses.Average(r => r.Res.Peso) : 0;
            return estadisticas;
        }

        /// <summary>
        /// ADR-05 · §4.4 · Correspondencia tipo de res → clave del diccionario que
        /// consume Views/Res/Index.cshtml:38-40.
        ///
        /// ── Por qué esto NO baja al dominio ──────────────────────────────────────
        /// Las otras cuatro cadenas `is Ternero / is Cebon / is Novillo` del sistema
        /// desaparecieron: eran reglas de negocio escritas fuera del tipo y ahora las
        /// responde la propia Res. Esta quinta se queda, y es una decisión, no un olvido.
        ///
        /// La cadena "Terneros" NO SE MUESTRA NUNCA: el texto que ve el operario es el
        /// literal "Terneros (0-12m)" escrito en la vista. Es una clave de acoplamiento
        /// entre este servicio y una vista concreta, no un concepto de negocio. Llevarla
        /// al dominio —por ejemplo como una propiedad EtiquetaPlural en Res— eliminaría
        /// un punto de cambio a cambio de meter una cadena de pantalla dentro de una
        /// entidad, que es exactamente la violación que el propio diagnóstico registra en
        /// H-13. No se cambia un problema de OCP por uno de SRP.
        ///
        /// Lo que sí se gana: la cadena de `is` se convierte en una TABLA DECLARATIVA en
        /// un único sitio. El punto de cambio permanece y está declarado en §9.2.1.
        /// </summary>
        private static readonly Dictionary<Type, string> ClavesDeVistaPorTipo = new()
        {
            { typeof(Ternero), "Terneros" },   // Jóvenes (0-12 meses)
            { typeof(Cebon),   "Cebones"  },   // Medios (13-48 meses)
            { typeof(Novillo), "Novillos" }    // Viejos (49+ meses)
        };
    }
}
