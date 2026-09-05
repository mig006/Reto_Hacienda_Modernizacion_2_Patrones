using Bib_Hacienda.Clases;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-02 · H-19 · Servicio de aplicación de ventas.
    ///
    /// Antes recibía PersistenciaService por constructor (VentaService.cs:9,11-15) y
    /// ninguno de sus métodos lo usaba: una dependencia de 643 líneas cargada para nada.
    /// El Reto 1 lo dejó solo de consulta porque vender una res es una operación SOBRE
    /// la res —bajaba a ResService, junto con AlimentarRes—.
    ///
    /// SC-1 rompe esa simetría a propósito: vender un producto derivado no toca ninguna
    /// res ni ningún potrero (Strategy P-02, <c>EfectoVentaSinEfecto</c>), así que no
    /// tiene nada que hacer en ResService. Es una operación sobre el AGREGADO Venta, y
    /// este es el servicio de aplicación de ese agregado. Por eso gana aquí —no en
    /// ResService— las dos dependencias que necesita para escribir.
    /// </summary>
    public class VentaService
    {
        // Atributos
        private readonly Hacienda _hacienda;
        private readonly ServicioVenta _servicioVenta;
        private readonly GuardadoValidado _guardado;

        public VentaService(Hacienda hacienda, ServicioVenta servicioVenta, GuardadoValidado guardado)
        {
            _hacienda = hacienda;
            _servicioVenta = servicioVenta;
            _guardado = guardado;
        }

        /// <summary>
        /// SC-1 · Vende un producto derivado (lácteo, carne o piel) y persiste.
        /// Funcionalidad NUEVA y autorizada: no hay comportamiento previo que preservar,
        /// así que el controlador SÍ puede consultar <c>Exito</c> para decidir color y
        /// navegación, igual que ResService.AsignarChip (SC-2, Reto 1).
        /// </summary>
        public ResultadoOperacion VenderProducto(string potreroId, TipoProducto tipo, uint cantidad, uint monto)
        {
            try
            {
                var producto = new ProductoDerivado(tipo, cantidad);
                string mensaje = _servicioVenta.vender_producto(potreroId, producto, monto);
                _guardado.GuardarVentas(_hacienda.L_ventas);
                return ResultadoOperacion.Exitoso(mensaje);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Fallido(ex.Message);
            }
        }

        // Obtener todas las ventas
        public List<Venta> ObtenerTodasLasVentas()
        {
            // Ordenar las ventas por fecha descendente
            return _hacienda.L_ventas.OrderByDescending(v => v.Fecha).ToList();
        }

        // Obtener ventas por potrero
        public List<Venta> ObtenerVentasPorPotrero(string potreroId)
        {
            // Filtrar ventas por el ID del potrero
            return _hacienda.L_ventas
                .Where(v => v.Potrero.Identificacion == potreroId)
                .OrderByDescending(v => v.Fecha)
                .ToList();
        }

        // Obtener ventas por rango de fechas
        public List<Venta> ObtenerVentasPorFechas(DateTime fechaInicio, DateTime fechaFin)
        {
            // Filtrar ventas dentro del rango de fechas
            return _hacienda.L_ventas
                .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin)
                .OrderByDescending(v => v.Fecha)
                .ToList();
        }

        // Obtener estadísticas de ventas
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            // Calcular estadísticas básicas de ventas
            var ventas = _hacienda.L_ventas;

            // Retornar un diccionario con las estadísticas
            return new Dictionary<string, object>
            {
                { "TotalVentas", ventas.Count },
                { "MontoTotal", ventas.Sum(v => v.Monto) },
                { "PromedioVenta", ventas.Any() ? ventas.Average(v => v.Monto) : 0 },
                { "VentasEsteMes", ventas.Count(v => v.Fecha.Month == DateTime.Now.Month && v.Fecha.Year == DateTime.Now.Year) },
                { "MontoEsteMes", ventas.Where(v => v.Fecha.Month == DateTime.Now.Month && v.Fecha.Year == DateTime.Now.Year).Sum(v => v.Monto) }
            };
        }
    }
}
