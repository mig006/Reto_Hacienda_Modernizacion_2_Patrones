using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bib_Hacienda.Clases
{
    public class Venta
    {
        private Potrero potrero;
        private DateTime fecha;
        private Res res;
        private uint monto;

        public Venta(Potrero potrero, DateTime fecha, Res res, uint monto)
        {
            this.Potrero = potrero;
            this.Fecha = fecha;
            this.Res = res;
            this.Monto = monto;
        }

        //Accesores
        public Potrero Potrero { get => potrero; set => potrero = value; }
        public DateTime Fecha { get => fecha; set => fecha = value; }
        public Res Res { get => res; set => res = value; }
        public uint Monto { get => monto; set => monto = value; }
    }
}
