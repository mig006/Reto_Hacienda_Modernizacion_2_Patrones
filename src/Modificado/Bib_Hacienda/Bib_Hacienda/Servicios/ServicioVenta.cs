using Bib_Hacienda.Clases;
using System;
using System.Linq;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-04 · Reglas comerciales.
    ///
    /// Razón de cambio que la gobierna: la política comercial.
    /// Interlocutor que la solicita: el área comercial.
    ///
    /// Absorbe vender_res (Hacienda.cs:143). Es una sola operación hoy, pero es
    /// precisamente el punto donde aterrizaría SC-1 (vender derivados del ganado), así
    /// que tener su propio tipo no es sobre-ingeniería: es dónde va a crecer el sistema.
    /// </summary>
    public class ServicioVenta
    {
        private readonly Hacienda _hacienda;
        private readonly GestorPotreros _gestorPotreros;

        public ServicioVenta(Hacienda hacienda, GestorPotreros gestorPotreros)
        {
            _hacienda = hacienda;
            _gestorPotreros = gestorPotreros;
        }

        //Metodo para vender res
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
                _hacienda.L_ventas.Add(venta);
                //Remover la res del potrero
                _hacienda.L_potreros.Where(p => p == potrero).FirstOrDefault().L_reses.Remove(res);
                return $"Venta de la res {res.Nombre} realizada con exito";
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el metodo vender_res: " + er.Message);
            }

        }
    }
}
