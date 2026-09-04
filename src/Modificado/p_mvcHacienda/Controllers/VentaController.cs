using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using p_mvcHacienda.Servicios;

namespace p_mvcHacienda.Controllers
{
    public class VentaController : Controller
    {
        private readonly VentaService _ventaService;

        public VentaController(VentaService ventaService)
        {
            _ventaService = ventaService;
        }

        // GET: VentaController
        public ActionResult Index()
        {
            var ventas = _ventaService.ObtenerTodasLasVentas();
            var estadisticas = _ventaService.ObtenerEstadisticas();

            ViewBag.Estadisticas = estadisticas;

            return View(ventas);
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
