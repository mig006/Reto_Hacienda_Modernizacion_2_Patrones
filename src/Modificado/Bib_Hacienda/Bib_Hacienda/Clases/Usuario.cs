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

        public string Nombre { get => nombre; private set => nombre = value; }
        // Getter conservado SOLO para la persistencia (RepositorioUsuariosArchivo escribe
        // "Nombre|Contrasena", formato de archivo congelado). Sin setter público: el usuario
        // es inmutable tras construirse.
        public string Contrasena { get => contrasena; private set => contrasena = value; }
    }
}
