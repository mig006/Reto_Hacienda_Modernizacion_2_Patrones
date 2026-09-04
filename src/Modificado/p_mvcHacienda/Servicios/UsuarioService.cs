using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System.Security.Claims;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-02 · H-05 · Servicio de aplicación de usuarios.
    ///
    /// ── El campo dejó de ser static ───────────────────────────────────────────────
    /// Antes era <c>private static List&lt;Usuario&gt; _usuarios</c> (UsuarioService.cs:9)
    /// ADEMÁS de estar registrado como Singleton (Program.cs:81). El diagnóstico
    /// concluyó que compartir el catálogo es intencional y que el problema era el
    /// mecanismo: el static duplicaba lo que el contenedor ya garantizaba y dejaba ese
    /// estado fuera de su control.
    ///
    /// Ahora es campo de instancia y el alcance lo decide el composition root, no la
    /// palabra clave. El comportamiento observable no cambia: sigue habiendo una sola
    /// lista compartida, porque el registro sigue siendo Singleton.
    ///
    /// DEUDA CONSCIENTE declarada: la sincronización frente a escrituras concurrentes
    /// no se resuelve. Hacerlo bien exige decidir la política transaccional de todos
    /// los repositorios, que está fuera del alcance de esta fase (ADRs.md §6.3.4).
    /// </summary>
    public class UsuarioService
    {
        // Atributos
        private List<Usuario> _usuarios = new List<Usuario>();
        private readonly IRepositorioUsuarios _repositorio;

        // Constructor
        public UsuarioService(IRepositorioUsuarios repositorio)
        {
            _repositorio = repositorio;
        }

        // Cargar usuarios desde persistencia
        public void CargarUsuarios()
        {
            _usuarios = _repositorio.CargarUsuarios();
        }

        // Crear un nuevo usuario
        public ResultadoOperacion CrearUsuario(string nombre, string contrasena)
        {
            try
            {      // Validaciones básicas
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre del usuario no puede estar vacío");
                }

                if (string.IsNullOrWhiteSpace(contrasena))
                {
                    // Validar que la contraseña no esté vacía
                    throw new ArgumentException("La contraseña no puede estar vacía");
                }

                if (_usuarios.Any(u => u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                {
                    // Verificar si ya existe un usuario con el mismo nombre
                    throw new InvalidOperationException($"Ya existe un usuario con el nombre '{nombre}'");
                }

                // Crear y agregar el nuevo usuario
                var nuevoUsuario = new Usuario(nombre, contrasena);
                _usuarios.Add(nuevoUsuario);
                _repositorio.GuardarUsuarios(_usuarios);

                // DEUDA D-2 · Este texto NO contiene el carácter que UsuarioController.cs:50
                // busca con Contains("✅"), de modo que un alta correcta se muestra en rojo
                // y la pantalla se queda en el formulario. El usuario SÍ queda creado y
                // persistido. Corregirlo cambiaría color y navegación: se conserva.
                return ResultadoOperacion.Exitoso($"Usuario '{nombre}' creado exitosamente");

            }
            catch (Exception ex)
            {
                // ADR-08a · BLINDAJE: devuelve, no lanza. Igual que el original.
                return ResultadoOperacion.Fallido($"{ex.Message}");
            }
        }

        // Autenticar usuario
        public bool AutenticarUsuario(string nombre, string contrasena)
        {
            // Verificar si existe un usuario con el nombre y contraseña proporcionados
            return _usuarios.Any(u => u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
            u.Contrasena == contrasena);
        }

        // Obtener todos los usuarios
        public List<Usuario> ObtenerTodosLosUsuarios()
        {
            // Retornar la lista de usuarios ordenada por nombre
            return _usuarios.OrderBy(u => u.Nombre).ToList();
        }

        // Buscar usuario por nombre
        public Usuario? BuscarUsuario(string nombre)
        {
            // Retornar el usuario que coincida con el nombre proporcionado
            return _usuarios.FirstOrDefault(u => u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        }

        // Obtener estadísticas
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            // Retornar un diccionario con el total de usuarios
            return new Dictionary<string, object>
            {
                {"TotalUsuarios", _usuarios.Count}
            };
        }

        public async Task<(bool, IEnumerable<Claim>)> ValidateUserAsync(string username, string password)
        {
            var user = _usuarios.FirstOrDefault(u => u.Nombre.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Contrasena == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Nombre),
                    // Puedes agregar más claims si tienes roles u otra información
                };
                return (true, claims);
            }

            return (false, null);
        }
    }
}
