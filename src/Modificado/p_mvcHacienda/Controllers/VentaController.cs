using Bib_Hacienda.Clases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using p_mvcHacienda.Servicios;

namespace p_mvcHacienda.Controllers
{
    public class VentaController : Controller
    {
        private readonly VentaService _ventaService;
        private readonly PotreroService _potreroService;

        public VentaController(VentaService ventaService, PotreroService potreroService)
        {
            _ventaService = ventaService;
            _potreroService = potreroService;
        }

        // GET: VentaController
        public ActionResult Index()
        {
            var ventas = _ventaService.ObtenerTodasLasVentas();
            var estadisticas = _ventaService.ObtenerEstadisticas();

            ViewBag.Estadisticas = estadisticas;

            return View(ventas);
        }

        // GET: Venta/VenderProducto - SC-1 · Mostrar formulario de venta de derivados
        [HttpGet]
        public ActionResult VenderProducto()
        {
            ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
            return View();
        }

        // POST: Venta/VenderProducto - SC-1 · Procesar la venta de un derivado
        //
        // Funcionalidad NUEVA y autorizada: SÍ consulta resultado.Exito, igual que
        // ResController.AsignarChip (SC-2, Reto 1) y por la misma razón: no hay
        // comportamiento previo que preservar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VenderProducto(string potreroId, TipoProducto tipoProducto, uint cantidad, uint monto)
        {
            if (string.IsNullOrWhiteSpace(potreroId) || cantidad <= 0 || monto <= 0)
            {
                ViewBag.Mensaje = "Potrero, cantidad y monto son requeridos, y deben ser mayores a 0";
                ViewBag.TipoMensaje = "danger";
                ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
                return View();
            }

            var resultado = _ventaService.VenderProducto(potreroId, tipoProducto, cantidad, monto);

            if (resultado.Exito)
            {
                TempData["Mensaje"] = resultado.Mensaje;
                TempData["TipoMensaje"] = "success";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Mensaje = resultado.Mensaje;
            ViewBag.TipoMensaje = "danger";
            ViewBag.Potreros = _potreroService.ObtenerTodosLosPotreros();
            return View();
        }

        // GET: VentaController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: VentaController/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: VentaController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // GET: VentaController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: VentaController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // POST: VentaController/Edit/5
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // POST: VentaController/Delete/5
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
