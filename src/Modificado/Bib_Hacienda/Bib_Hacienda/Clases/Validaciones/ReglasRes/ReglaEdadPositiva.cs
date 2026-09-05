using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Clases.Validaciones.ReglasRes
{
    /// <summary>
    /// Composite (P-05, Actividad 2) · Cuarta regla de ValidarRes.cs:19. Se apoya en
    /// que <see cref="ReglaResNoNula"/> ya corrió primero: aquí <c>res</c> nunca es null.
    ///
    /// Se conserva tal cual, incluida la razón por la que esta condición SÍ es
    /// alcanzable: el formulario de alta admite edad 0 para potreros de ternero
    /// (Views/Res/Create.cshtml, min="0"), así que una res recién creada puede llegar
    /// aquí con Edad == 0 y esta regla es la que la corta antes de escribir el archivo.
    /// </summary>
    public sealed class ReglaEdadPositiva : IValidador<Res>
    {
        public ResultadoValidacion Validar(Res res)
            => res.Edad <= 0 ? ResultadoValidacion.Invalido() : ResultadoValidacion.Valido();
    }
}
