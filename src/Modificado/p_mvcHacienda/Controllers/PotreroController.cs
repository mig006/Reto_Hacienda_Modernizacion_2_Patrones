using Microsoft.AspNetCore.Mvc;
using p_mvcHacienda.Servicios;
using static Bib_Hacienda.Clases.Potrero;

namespace p_mvcHacienda.Controllers
{
    /// <summary>
    /// ADR-02 · El controlador pierde Hacienda y PersistenciaService del constructor
    /// (antes: PotreroController.cs:16). Ahora solo conoce su servicio de aplicación.
    /// </summary>
    public class PotreroController : Controller
    {
        //Atributos
        private readonly PotreroService _potreroService;

        //Inyección de dependencias del servicio
        public PotreroController(PotreroService potreroService)
        {
            _potreroService = potreroService;
        }

        // GET
        [HttpGet]

        //Mostrar la lista de potreros y estadisticas
        public ActionResult Index()
        {
            var potreros = _potreroService.ObtenerTodosLosPotreros();
            var estadisticas = _potreroService.ObtenerEstadisticas();

            ViewBag.Estadisticas = estadisticas;

            return View(potreros);
        }


        // GET: Potrero/Create - Mostrar formulario de creación
        public ActionResult Create()
        {
            return View();
        }

        //Detalles de un potrero
        public ActionResult Details(string id)
        {
            var potrero = _potreroService.ObtenerPotreroPorIdentificacion(id);

            if (potrero == null)
            {
                TempData["Mensaje"] = "Potrero no encontrado";
                TempData["TipoMensaje"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            return View(potrero);
        }

        // POST:
        [HttpPost]

        // Procesar creación de potrero
        public ActionResult Create(string identificacion, l_tipos_potreros tipo)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(identificacion))
                {
                    ViewBag.Mensaje = "La identificación no puede estar vacía";
                    ViewBag.TipoMensaje = "danger";
                    return View();
                }

                // Llamar al servicio para crear potrero (persiste internamente)
                var exitoso = _potreroService.CrearPotrero(identificacion, tipo);

                // ADR-02 · Aquí había un segundo GuardarPotreros "por seguridad"
                // (PotreroController.cs:76-79) que repetía el guardado que el servicio
                // acababa de hacer. Se eliminó tras verificar que su retorno se descartaba
                // y que el archivo resultante era idéntico.

                // ADR-08a · BLINDAJE: este controlador NUNCA consultó el resultado para
                // decidir el color; si no hubo excepción, ponía "success" fijo. Sigue igual.
                // Consultar exitoso.Exito aquí cambiaría el color de la alerta.
                TempData["Mensaje"] = exitoso.Mensaje;
                TempData["TipoMensaje"] = "success";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"{ex.Message}";
                ViewBag.TipoMensaje = "danger";
            }

            return View();
        }
    }
}
