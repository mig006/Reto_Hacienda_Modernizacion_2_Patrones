using Bib_Hacienda.Contratos;
using System;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// P-02 (Actividad 1) · SC-1 · El campo que era `Res res` (Venta.cs:13,16 del Reto 1)
    /// pasa a ser <see cref="IArticuloVendible"/>: cualquier cosa que la hacienda venda,
    /// no solo un animal completo. Ver la ficha de Strategy en la Actividad 3.3 y B-10
    /// de la bitácora para la alternativa descartada (una jerarquía de Venta).
    /// </summary>
    public class Venta
    {
        private Potrero potrero;
        private DateTime fecha;
        private IArticuloVendible articulo;
        private uint monto;

        public Venta(Potrero potrero, DateTime fecha, IArticuloVendible articulo, uint monto)
        {
            this.Potrero = potrero;
            this.Fecha = fecha;
            this.Articulo = articulo;
            this.Monto = monto;
        }

        //Accesores
        public Potrero Potrero { get => potrero; set => potrero = value; }
        public DateTime Fecha { get => fecha; set => fecha = value; }
        public IArticuloVendible Articulo { get => articulo; set => articulo = value; }
        public uint Monto { get => monto; set => monto = value; }
    }
}
