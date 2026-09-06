using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-04 · Reglas comerciales.
    ///
    /// Razón de cambio que la gobierna: la política comercial.
    /// Interlocutor que la solicita: el área comercial.
    ///
    /// Absorbe vender_res (Hacienda.cs:143). Era una sola operación en el Reto 1; SC-1
    /// añade la segunda que el comentario original ya anticipaba.
    ///
    /// ══ Strategy (Actividad 2 · P-02) ═══════════════════════════════════════════
    /// Qué le pasa al inventario al vender YA NO lo decide este método con un `if`:
    /// lo resuelve el registro de <see cref="IEfectoVenta"/>, exactamente como
    /// <see cref="FabricaVacunas"/> resuelve <see cref="IFabricaVacuna"/>. Un artículo
    /// vendible nuevo con un efecto de inventario distinto es una clase más y una línea
    /// en la raíz de composición — esta clase no se vuelve a tocar.
    /// </summary>
    public class ServicioVenta
    {
        private readonly Hacienda _hacienda;
        private readonly GestorPotreros _gestorPotreros;
        private readonly IReadOnlyCollection<IEfectoVenta> _efectos;

        public ServicioVenta(Hacienda hacienda, GestorPotreros gestorPotreros, IEnumerable<IEfectoVenta> efectos)
        {
            _hacienda = hacienda;
            _gestorPotreros = gestorPotreros;
            _efectos = efectos as IReadOnlyCollection<IEfectoVenta> ?? efectos.ToList();
        }

        //Metodo para vender res
        //
        // Comportamiento observable preservado tal cual: incluida la falta de guarda
        // entre `potrero.buscar_res(nombre)` y el `if (potrero == null)` de abajo, que
        // en el Reto 1 ya lanzaba NullReferenceException si el potrero no existía.
        public string vender_res(string id_potrero, string nombre, uint monto)
        {

            try
            {
                // Pedimos el potrero y la res
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                Res res = potrero.buscar_res(nombre);
                //Validar parámetros
                if (potrero == null) throw new ArgumentNullException(nameof(potrero));
                if (res == null) throw new ArgumentNullException(nameof(res));

                //Crear la venta
                Venta venta = new Venta(potrero, DateTime.Now, res, monto);
                //Agregar la venta a la lista de ventas
                _hacienda.RegistrarVenta(venta);
                //Aplicar el efecto de la venta sobre el inventario (Strategy · P-02)
                AplicarEfecto(potrero, res);
                return $"Venta de la res {res.Nombre} realizada con exito";
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo vender_res: " + er.Message);
            }

        }

        /// <summary>
        /// SC-1 · Vende un producto derivado (lácteo, carne o piel). No hay res que
        /// buscar ni que retirar: el efecto de inventario que corresponde —ninguno— lo
        /// resuelve <see cref="Bib_Hacienda.Estrategias.EfectoVentaSinEfecto"/> por el
        /// mismo registro que usa vender_res, no una rama distinta escrita a mano aquí.
        /// </summary>
        public string vender_producto(string id_potrero, ProductoDerivado producto, uint monto)
        {
            try
            {
                Potrero potrero = _gestorPotreros.buscar_potrero(id_potrero);
                if (potrero == null) throw new ArgumentNullException(nameof(potrero));
                if (producto == null) throw new ArgumentNullException(nameof(producto));

                Venta venta = new Venta(potrero, DateTime.Now, producto, monto);
                _hacienda.RegistrarVenta(venta);
                AplicarEfecto(potrero, producto);
                return $"Venta de {producto.Cantidad} unidad(es) de {producto.Nombre} realizada con éxito";
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el método vender_producto: " + er.Message);
            }
        }

        // Búsqueda por asignabilidad, no por coincidencia exacta de tipo: TipoSoportado
        // de EfectoVentaRetiroInventario es Res (la base), y lo que llega aquí es un
        // subtipo concreto (Ternero/Cebon/Novillo). Mismo criterio que ya usa
        // IFabricaRes.TipoPotreroSoportado en Potrero.anadir_res: un First() sobre el
        // registro, no un diccionario por tipo exacto.
        private void AplicarEfecto(Potrero potrero, IArticuloVendible articulo)
        {
            var tipoArticulo = articulo.GetType();
            _efectos.First(e => e.TipoSoportado.IsAssignableFrom(tipoArticulo)).Aplicar(_hacienda, potrero, articulo);
        }
    }
}
