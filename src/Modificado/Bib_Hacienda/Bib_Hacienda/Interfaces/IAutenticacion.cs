using Bib_Hacienda.Clases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bib_Hacienda.Interfaces
{
    //Interfaz para autenticar y autorizar operaciones de usuarios
    public interface IAutenticacion
    {
        //Autoriza la ejecución de una operación para un usuario
        void AutorizarOperacion(Usuario usuario, string operacion);
  }
}
