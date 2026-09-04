using Bib_Hacienda.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bib_Hacienda.Clases
{
    //Clase que implementa la autenticación y autorización de usuarios
    public class Autenticacion : IAutenticacion
    {
        //Lista de usuarios registrados en el sistema
        private List<Usuario> usuarios_registrados;

        //Accesor lista de usuarios
        public List<Usuario> Usuarios_registrados { get => usuarios_registrados; set => usuarios_registrados = value; }

        public Autenticacion()
        {
            Usuarios_registrados = new List<Usuario>();
            //Agregar usuarios por defecto
            Usuarios_registrados.Add(new Usuario("admin", "admin123"));
            Usuarios_registrados.Add(new Usuario("empleado", "emp456"));
            Usuarios_registrados.Add(new Usuario("visitante", "visit789"));
        }

        //Método para crear nuevos usuarios en el sistema
        public string crear_usuario(string nombre, string contrasena)
        {
            try
            {
                //Validar que el nombre no esté vacío o nulo
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre del usuario no puede estar vacío", nameof(nombre));
                }

                if (string.IsNullOrWhiteSpace(contrasena))
                {
                    throw new ArgumentException("La contraseña no puede estar vacía", nameof(contrasena));
                }

                //Verificar si ya existe un usuario con el mismo nombre
                if (Usuarios_registrados.Any(u => u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Ya existe un usuario con el nombre '{nombre}'.");
                }

                //Crear nuevo usuario
                Usuario nuevo_usuario = new Usuario(nombre, contrasena);
                Usuarios_registrados.Add(nuevo_usuario);

                return $"Usuario '{nombre}' creado exitosamente en el sistema.";
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el método crear_usuario: " + er.Message);
            }
        }

        //Método para listar todos los usuarios registrados (útil para debugging o admin)
        public List<Usuario> listar_usuarios()
        {
            return new List<Usuario>(Usuarios_registrados);
        }

        //Método para validar credenciales (útil para login)
        public bool ValidarCredenciales(string nombre, string contrasena)
        {
            return Usuarios_registrados.Any(u =>
                u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                u.Contrasena == contrasena);
        }

        //Método para buscar usuario por nombre
        public Usuario buscar_usuario(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre de búsqueda no puede estar vacío.");
                }

                Usuario usuario = Usuarios_registrados.FirstOrDefault(u =>
                    u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

                if (usuario == null)
                {
                    throw new Exception($"No se encontró el usuario '{nombre}'.");
                }

                return usuario;
            }
            catch (Exception er)
            {
                throw new Exception("Error inesperado en el método buscar_usuario: " + er.Message);
            }
        }

        //Autoriza la ejecución de una operación para un usuario específico
        //Lanza excepción con el resultado (éxito o denegación)
        public void AutorizarOperacion(Usuario usuario, string operacion)
        {
            if (usuario == null)
            {
                throw new Exception("✗ Usuario no autenticado. Debe iniciar sesión para realizar operaciones");
            }

            //Buscar si el usuario está registrado
            Usuario usuarioRegistrado = Usuarios_registrados.FirstOrDefault(u =>
                    u.Nombre == usuario.Nombre && u.Contrasena == usuario.Contrasena);

            if (usuarioRegistrado == null)
            {
                throw new Exception($"✗ Usuario '{usuario.Nombre}' no está registrado en el sistema");
            }

            //Verificar permisos según el rol del usuario
            bool tienePermiso = false;

            if (usuario.Nombre == "admin")
            {
                //Admin tiene todos los permisos
                tienePermiso = true;
            }
            else if (usuario.Nombre == "empleado")
            {
                //Empleado puede hacer todo excepto eliminar usuarios
                tienePermiso = !operacion.Contains("Eliminar");
            }
            else if (usuario.Nombre == "visitante")
            {
                //Visitante solo puede consultar
                tienePermiso = operacion.Contains("Consultar") || operacion.Contains("Listar");
            }

            //Lanzar excepción con el resultado
            if (tienePermiso)
            {
                throw new Exception($"✓ Usuario '{usuario.Nombre}' autorizado para ejecutar: {operacion}");
            }
            else
            {
                throw new Exception($"✗ Acceso DENEGADO. Usuario '{usuario.Nombre}' NO tiene permisos para: {operacion}");
            }
        }
    }
}
