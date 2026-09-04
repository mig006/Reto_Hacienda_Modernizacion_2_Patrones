using Bib_Hacienda.Clases;
using System.Collections.Generic;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-02 · DI-4 · Contrato de persistencia del agregado <see cref="Usuario"/>.
    ///
    /// Alto nivel: UsuarioService. Bajo nivel: RepositorioUsuariosArchivo (Usuarios.txt).
    /// Composition root: RaizComposicion.Registrar. Ciclo de vida: Singleton.
    ///
    /// GuardarUsuarios NO valida con el mismo mecanismo que el resto: la comprobación
    /// de nombre y contraseña vive dentro del repositorio porque así estaba en
    /// PersistenciaService.cs:280-286, y su mensaje de error es un literal propio.
    /// Se conserva tal cual (contrato de texto congelado de ADR-02).
    /// </summary>
    public interface IRepositorioUsuarios
    {
        List<Usuario> CargarUsuarios();

        /// <summary>
        /// Devuelve el mismo texto que devolvía PersistenciaService.GuardarUsuarios:
        /// "Guardado exitosamente" o el literal de error de datos incompletos.
        /// El llamador lo descarta hoy, y se conserva por si dejara de hacerlo.
        /// </summary>
        string GuardarUsuarios(List<Usuario> usuarios);
    }
}
