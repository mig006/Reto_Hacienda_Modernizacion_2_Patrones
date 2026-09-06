using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using System;
using System.Linq;

namespace Bib_Hacienda.Estrategias
{
    /// <summary>
    /// Strategy (Actividad 2 · P-02) · Vender una res la retira del potrero.
    ///
    /// Traducción literal de ServicioVenta.cs:46 del Reto 1
    /// (<c>_hacienda.L_potreros.Where(p => p == potrero).FirstOrDefault().L_reses.Remove(res)</c>),
    /// sin guarda añadida: si el potrero no aparece en la lista, sigue lanzando
    /// NullReferenceException exactamente como antes. No es una omisión, es preservar
    /// el comportamiento observable — endurecerla sería un cambio no autorizado.
    /// </summary>
    public sealed class EfectoVentaRetiroInventario : IEfectoVenta
    {
        public Type TipoSoportado => typeof(Res);

        public void Aplicar(Hacienda hacienda, Potrero potrero, IArticuloVendible articulo)
        {
            var res = (Res)articulo;
            hacienda.L_potreros.Where(p => p == potrero).FirstOrDefault().RetirarRes(res);
        }
    }
}
