using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · DI-4 · Implementación en archivos planos de <see cref="IRepositorioUsuarios"/>.
    /// Dueño exclusivo de Usuarios.txt. Traducción de PersistenciaService.cs:275-297 y :602-638.
    ///
    /// Dos asimetrías respecto de los otros tres repositorios que se conservan tal cual:
    ///   · GuardarUsuarios valida aquí dentro y devuelve su propio literal, porque nunca
    ///     pasó por el mecanismo de validadores (PersistenciaService.cs:280-286);
    ///   · CargarUsuarios NO relanza: escribe en consola y devuelve lista vacía (:635-637).
    ///
    /// Riesgo de seguridad conocido y declarado en ADRs.md §9.2.1: las contraseñas se
    /// guardan en texto plano. Corregirlo cambiaría el formato de Usuarios.txt y el
    /// flujo de login, que es comportamiento observable, así que queda fuera de alcance.
    /// </summary>
    public sealed class RepositorioUsuariosArchivo : IRepositorioUsuarios
    {
        private readonly string _directorioArchivos;

        public RepositorioUsuariosArchivo(string directorioArchivos)
        {
            _directorioArchivos = directorioArchivos;
            if (!Directory.Exists(_directorioArchivos))
            {
                Directory.CreateDirectory(_directorioArchivos);
            }
        }

        private string Ruta(string archivo) => Path.Combine(_directorioArchivos, archivo);

        public List<Usuario> CargarUsuarios()
        {
            try
            {
                string rutaArchivo = Ruta("Usuarios.txt");
                if (!File.Exists(rutaArchivo)) return new List<Usuario>();

                var usuarios = new List<Usuario>();
                foreach (var linea in File.ReadAllLines(rutaArchivo))
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split('|');
                    if (partes.Length >= 2)
                    {
                        usuarios.Add(new Usuario(partes[0], partes[1]));
                    }
                }
                return usuarios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar usuarios: {ex.Message}");
                return new List<Usuario>();
            }
        }

        public string GuardarUsuarios(List<Usuario> usuarios)
        {
            try
            {
                foreach (var usuario in usuarios)
                {
                    if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Contrasena))
                    {
                        return TextoUsuarioIncompleto;
                    }
                }

                var lineas = usuarios.Select(u => $"{u.Nombre}|{u.Contrasena}");
                File.WriteAllLines(Ruta("Usuarios.txt"), lineas);

                return TextoGuardadoExitoso;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar usuarios: {ex.Message}", ex);
            }
        }

        /// <summary>Literal congelado de PersistenciaService.cs:291.</summary>
        public const string TextoGuardadoExitoso = "Guardado exitosamente";

        /// <summary>
        /// Literal congelado de PersistenciaService.cs:284. En el archivo original está
        /// escrito en ISO-8859 sin BOM, de modo que el compilador lo lee como UTF-8 y la
        /// 'ñ' de "contraseña" se convierte en el carácter de reemplazo U+FFFD. Lo que
        /// el programa emite hoy es literalmente lo que aparece aquí, no la palabra bien
        /// escrita: corregirlo sería cambiar una salida (ADRs.md §8.5).
        /// </summary>
        public const string TextoUsuarioIncompleto = "Error: Usuario debe tener nombre y contrase�a";
    }
}
