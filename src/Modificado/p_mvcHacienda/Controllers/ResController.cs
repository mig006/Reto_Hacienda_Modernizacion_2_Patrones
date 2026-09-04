using Microsoft.AspNetCore.Mvc;
using p_mvcHacienda.Servicios;

namespace p_mvcHacienda.Controllers
{
    /// <summary>
    /// ADR-02 · El controlador pierde Hacienda y PersistenciaService del constructor
    /// (antes: ResController.cs:17). Alimentar y Vender, que llamaban directamente al
    /// dominio y al disco desde aquí, pasaron a ResService.
    /// </summary>
    public class ResController : Controller
    {
        // Atributos
        private readonly ResService _resService;
        private readonly PotreroService _potreroService;

        //Constructor con inyección de dependencias
        public ResController(ResService resService, PotreroService potreroService)
        {
            _resService = resService;
            _potreroService = potreroService;
        }

        // GET: Res/Index - Listar todas las reses
        [HttpGet]
        public ActionResult Index()
        {
            var resesConPotrero = _resService.ObtenerTodasLasReses();
            var estadisticas = _resService.ObtenerEstadisticas();

            ViewBag.Estadisticas = estadisticas;

            return View(resesConPotrero);
        }

        // Ver vacunas aplicadas por res
        [HttpGet]
        public ActionResult DetalleVacunas(string potreroId, string nombreRes)
        {
            try
            {
                // BuscarResEstricta, no BuscarRes: el mensaje de error de la búsqueda se
                // muestra al operario y es comportamiento observable.
                var res = _resService.BuscarResEstricta(potreroId, nombreRes);
                if (res == null)
                {
                    TempData["Mensaje"] = "Res no encontrada";
                    TempData["TipoMensaje"] = "danger";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.PotreroId = potreroId;
                ViewBag.NombreRes = nombreRes;
                return View(res.L_vacunas_aplicadas);
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = ex.Message;
                TempData["TipoMensaje"] = "danger";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Res/Create - Mostrar formulario de creación
        public ActionResult Create()
        {
            ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
            return View();
        }

        // POST: Res/Create - Procesar creación de res
        [HttpPost]
        public ActionResult Create(string potreroId, string nombre, ushort edad, uint peso)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(potreroId) || string.IsNullOrWhiteSpace(nombre))
                {
                    ViewBag.Mensaje = "Todos los campos son requeridos";
                    ViewBag.TipoMensaje = "danger";
                    ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
                    return View();
                }

                // Usar PotreroService para agregar y guardar, y obtener mensaje compuesto
                var resultado = _potreroService.AgregarRes(potreroId, nombre, edad, peso);

                    // ADR-08a · BLINDAJE: "success" fijo, como en el original.
                    TempData["Mensaje"] = resultado.Mensaje;
                    TempData["TipoMensaje"] = "success";
                    return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"{ex.Message}";
                ViewBag.TipoMensaje = "danger";
            }

            ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
            return View();
        }

        // GET: Res/AsignarChip - SC-2 · Mostrar formulario de conexión de chip
        [HttpGet]
        public ActionResult AsignarChip()
        {
            ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
            ViewBag.Reses = _resService.ObtenerTodasLasReses();
            return View();
        }

        // POST: Res/AsignarChip - SC-2 · Procesar la conexión del chip
        //
        // A diferencia de UsuarioController y VacunaController, esta acción SÍ consulta
        // resultado.Exito para decidir color y navegación. Puede hacerlo porque es
        // funcionalidad NUEVA y autorizada: no hay comportamiento previo que preservar.
        // Es, de paso, cómo se verá el sistema entero cuando se autorice el parche de
        // ADR-08b.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarChip(string potreroId, string nombreRes,
            string identificador, string latitud, string longitud)
        {
            if (string.IsNullOrWhiteSpace(potreroId) || string.IsNullOrWhiteSpace(nombreRes)
                || string.IsNullOrWhiteSpace(identificador))
            {
                ViewBag.Mensaje = "Todos los campos son requeridos";
                ViewBag.TipoMensaje = "danger";
                ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
                ViewBag.Reses = _resService.ObtenerTodasLasReses();
                return View();
            }

            if (!double.TryParse(latitud, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(longitud, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                ViewBag.Mensaje = "Latitud y longitud deben ser números (use punto decimal)";
                ViewBag.TipoMensaje = "danger";
                ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
                ViewBag.Reses = _resService.ObtenerTodasLasReses();
                return View();
            }

            var resultado = _resService.AsignarChip(potreroId, nombreRes, identificador, lat, lon);

            if (resultado.Exito)
            {
                TempData["Mensaje"] = resultado.Mensaje;
                TempData["TipoMensaje"] = "success";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Mensaje = resultado.Mensaje;
            ViewBag.TipoMensaje = "danger";
            ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
            ViewBag.Reses = _resService.ObtenerTodasLasReses();
            return View();
        }

        // POST: Res/Alimentar - Alimentar una res
        public ActionResult Alimentar(string potreroId, string nombreRes, uint cantidadAlimento)
        {
            try
            {
                // Validar cantidad de alimento
                var resultado = _resService.AlimentarRes(potreroId, nombreRes, cantidadAlimento);

                string mensajeAlimento = cantidadAlimento == 1 ? "vez" : "veces";
                TempData["Mensaje"] = resultado.Mensaje;
                TempData["TipoMensaje"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"{ex.Message}";
                TempData["TipoMensaje"] = "danger";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Res/Vender - Vender una res (validando overflow de monto)
        public ActionResult Vender(string potreroId, string nombreRes, string monto)
        {
            try
            {
                // Validar y convertir monto de forma segura
                if (string.IsNullOrWhiteSpace(monto))
                {
                    TempData["Mensaje"] = "El monto es requerido";
                    TempData["TipoMensaje"] = "danger";
                    return RedirectToAction(nameof(Index));
                }

                // Intentar convertir a decimal primero
                if (!decimal.TryParse(monto, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var montoDec))
                {
                    TempData["Mensaje"] = "Monto inválido";
                    TempData["TipoMensaje"] = "danger";
                    return RedirectToAction(nameof(Index));
                }

                // Validar límites de uint
                if (montoDec < 0 || montoDec > uint.MaxValue)
                {
                    TempData["Mensaje"] = $"El monto excede el máximo permitido ({uint.MaxValue})";
                    TempData["TipoMensaje"] = "danger";
                    return RedirectToAction(nameof(Index));
                }

                var montoUint = (uint)montoDec;

                // Vende la res y envía el mensaje
                var resultado = _resService.VenderRes(potreroId, nombreRes, montoUint);

                    TempData["Mensaje"] = resultado.Mensaje;
                    TempData["TipoMensaje"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"{ex.Message}";
                TempData["TipoMensaje"] = "danger";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
