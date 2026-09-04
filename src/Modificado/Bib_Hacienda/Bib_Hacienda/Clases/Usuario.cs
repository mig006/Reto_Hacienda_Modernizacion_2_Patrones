using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bib_Hacienda.Clases
{
    public class Usuario
    {
        private string nombre;
        private string contrasena;

        public Usuario(string nombre, string contrasena)
        {
            this.Nombre = nombre;
            this.Contrasena = contrasena;
        }

        public string Nombre { get => nombre; set => nombre = value; }
        public string Contrasena { get => contrasena; set => contrasena = value; }
    }
}
