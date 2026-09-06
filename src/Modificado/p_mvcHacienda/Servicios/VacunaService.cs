using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using static Bib_Hacienda.Clases.Viva;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-02 · Servicio de aplicación de vacunas.
    ///
    /// Depende de <see cref="IRepositorioCatalogoVacunas"/> —un contrato del dominio—
    /// para la carga perezosa del catálogo, y de <see cref="GuardadoValidado"/> para la
    /// secuencia validar→persistir. Antes dependía de PersistenciaService, la clase de
    /// 643 líneas (H-08).
    /// </summary>
    public class VacunaService
    {
        // Atributos
        private readonly Hacienda _hacienda;
        private readonly FabricaVacunas _fabricaVacunas;
        private readonly ServicioVacunacion _servicioVacunacion;
        private readonly GestorPotreros _gestorPotreros;
        private readonly GuardadoValidado _guardado;
        private readonly IRepositorioCatalogoVacunas _catalogo;

        // Constructor
        public VacunaService(Hacienda hacienda, FabricaVacunas fabricaVacunas,
            ServicioVacunacion servicioVacunacion, GestorPotreros gestorPotreros,
            GuardadoValidado guardado, IRepositorioCatalogoVacunas catalogo)
        {
            _hacienda = hacienda;
            _fabricaVacunas = fabricaVacunas;
            _servicioVacunacion = servicioVacunacion;
            _gestorPotreros = gestorPotreros;
            _guardado = guardado;
            _catalogo = catalogo;
        }

        // Crear vacuna viva o bacteriana
        //
        // ADR-13 (Factory Method, P-01) · El tipo ya no se ADIVINA mirando cuál de los
        // dos parámetros opcionales llegó con valor: llega explícito en tipoVacuna,
        // igual que ya lo sabía el formulario (VacunaController.cs). Lo que queda de la
        // validación original es la regla de negocio "el par debe ser consistente con
        // el tipo declarado", que es una guarda de datos, no una decisión de qué
        // fábrica invocar — esa la resuelve FabricaVacunas contra su registro.
        public ResultadoOperacion CrearVacuna(string tipoVacuna, string nombre, string lote, DateTime fechaVencimiento, DateTime fechaAplicacion, uint? periodoAplicacion, enum_l_atenuaciones? atenuacion)
        {
            try
            {
                bool combinacionValida =
                    (periodoAplicacion.HasValue && !atenuacion.HasValue) ||
                    (!periodoAplicacion.HasValue && atenuacion.HasValue);

                if (!combinacionValida)
                {
                    return ResultadoOperacion.Fallido("Error: parámetros inválidos para crear la vacuna (revise tipo, período o atenuación)");
                }

                string resultadoDominio = _fabricaVacunas.Crear(tipoVacuna, nombre, lote, fechaVencimiento, fechaAplicacion, periodoAplicacion, atenuacion);

                string validado = GuardadoValidado.Texto(_guardado.GuardarVacunas(_hacienda.L_vacunas));

                // Mensaje compuesto: evento de dominio + mensaje del guardado
                return ResultadoOperacion.Exitoso($"{resultadoDominio}. {validado}");
            }

            catch (Exception ex)
            {
                // ADR-08a · BLINDAJE: este servicio DEVUELVE el texto del error en lugar de
                // lanzarlo. PotreroService hace lo contrario. La asimetría es del sistema
                // original y se conserva: determina a qué rama entra el controlador y, con
                // ella, el color de la alerta y la navegación.
                //
                // Aquí Exito SÍ lleva información real. Es el punto donde el parche de
                // ADR-08b —consultar Exito en lugar de olfatear la cadena— arreglaría D-3.
                return ResultadoOperacion.Fallido($"{ex.Message}");
            }
        }

        // Aplicar vacuna a una res
        public ResultadoOperacion AplicarVacuna(string potreroId, string nombreRes, string loteVacuna)
        {
            try
            {
                // Asegurar catálogo cargado
                if (_hacienda.L_vacunas.Count ==0)
                {
                    var cargadas = _catalogo.CargarVacunas();
                    foreach (var v in cargadas) _hacienda.AgregarVacuna(v);
                }

                // Buscar la vacuna por su lote
                var vacuna = _hacienda.L_vacunas.FirstOrDefault(v => v.Lote == loteVacuna);
                if (vacuna == null)
                {
                    throw new Exception($"No se encontró una vacuna con el lote '{loteVacuna}'");
                }

                // Aplicar la vacuna desde la hacienda (dispara eventos del dominio)
                string resultadoDominio = _servicioVacunacion.aplicar_vacuna(vacuna, nombreRes, potreroId);

                // Remover del inventario disponible si aún existe (evitar duplicidad)
                var existente = _hacienda.L_vacunas.FirstOrDefault(v => v.Lote == loteVacuna);
                if (existente != null)
                {
                    _hacienda.RemoverVacuna(existente);
                }

                // Persistir cambios.
                //
                // EL ORDEN DE LAS CUATRO ESCRITURAS ES COMPORTAMIENTO OBSERVABLE (ADR-02):
                // aplicadas → disponibles → potreros → reses. Si el proceso se interrumpe
                // a mitad, el estado en disco debe quedar igual que hoy.
                //
                // 'ultima' encadena el resultado de validación dentro de esta operación,
                // que es lo que antes hacía HttpContext.Items["ResultadoValidacion"] a lo
                // largo de una petición (H-14).
                ResultadoValidacion? ultima = null;

                ultima = _guardado.GuardarVacunasAplicadas(_hacienda.L_potreros) ?? ultima;
                var validadoAplicadas = GuardadoValidado.Texto(ultima);

                ultima = _guardado.GuardarVacunas(_hacienda.L_vacunas) ?? ultima;
                var validadoDisponibles = GuardadoValidado.Texto(ultima);

                ultima = _guardado.GuardarPotreros(_hacienda.L_potreros) ?? ultima;
                ultima = _guardado.GuardarReses(_hacienda.L_potreros) ?? ultima;

                // Consolidar mensajes de guardado para evitar duplicados
                var validado = ConsolidarValidaciones(validadoAplicadas, validadoDisponibles);

                // Mensaje final sin repeticiones adicionales.
                // ADR-08a · El post-procesado (ConsolidarValidaciones + AsegurarPuntoFinal)
                // queda DENTRO de Mensaje: perderlo cambiaría el texto en pantalla.
                return ResultadoOperacion.Exitoso(AsegurarPuntoFinal($"{resultadoDominio}. {validado}".Trim()));
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Fallido($"{ex.Message}");
            }
        }

        // Obtener todas las vacunas disponibles
        public List<Vacuna> ObtenerVacunasDisponibles()
        {
            // Lazy-load desde archivo si el catálogo está vacío
            if (_hacienda.L_vacunas.Count ==0)
            {
                var cargadas = _catalogo.CargarVacunas();
                foreach (var v in cargadas) _hacienda.AgregarVacuna(v);
            }
            return _hacienda.L_vacunas.OrderBy(v => v.Nombre).ToList();
        }

        // Obtener vacunas aplicadas a una res
        public List<Vacuna> ObtenerVacunasAplicadas(string potreroId, string nombreRes)
        {
            try
            {
                // Buscar el potrero por su identificación
                var potrero = _gestorPotreros.buscar_potrero(potreroId);
                var res = potrero.buscar_res(nombreRes);
                return res.L_vacunas_aplicadas.ToList();
            }
            catch
            {
                return new List<Vacuna>();
            }
        }

        // Obtener estadísticas de vacunas
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            // Asegurar catálogo cargado
            if (_hacienda.L_vacunas.Count ==0)
            {
                var cargadas = _catalogo.CargarVacunas();
                foreach (var v in cargadas) _hacienda.AgregarVacuna(v);
            }

            var vacunas = _hacienda.L_vacunas;
            return new Dictionary<string, object>
            {
                { "TotalVacunas", vacunas.Count },
                { "Bacterianas", vacunas.Count(v => v is Bacteriana) },
                { "Vivas", vacunas.Count(v => v is Viva) },
                { "Vencidas", vacunas.Count(v => v.Fecha_vencimiento < DateTime.Now) },
                { "Vigentes", vacunas.Count(v => v.Fecha_vencimiento >= DateTime.Now) }
            };
        }

        // Consolidar mensajes de validación: evita duplicados y normaliza
        private string ConsolidarValidaciones(string a, string b)
        {
            a = (a ?? string.Empty).Trim();
            b = (b ?? string.Empty).Trim();
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return a;
            if (a.Contains(b, StringComparison.OrdinalIgnoreCase)) return a;
            if (b.Contains(a, StringComparison.OrdinalIgnoreCase)) return b;
            // Si son distintos, priorizar el primero (aplicadas) para evitar repetición
            return a.Length >0 ? a : b;
        }

        private string AsegurarPuntoFinal(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje)) return mensaje;
            return mensaje.EndsWith(".") ? mensaje : mensaje + ".";
        }
    }
}
