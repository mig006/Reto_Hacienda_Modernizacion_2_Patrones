namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// P-02 (Actividad 1) · Lo que puede figurar dentro de una <see cref="Clases.Venta"/>.
    ///
    /// Antes el campo era `Res res` a secas (Venta.cs:13,16), así que una venta SOLO
    /// podía ser un animal completo. SC-1 exige vender también leche, carne y piel, y
    /// eso no es un tercer subtipo de res: es una cosa de otra naturaleza que la
    /// hacienda también vende. De ahí la abstracción, no una jerarquía nueva de Venta
    /// (ver la ficha de Strategy en la Actividad 3.3 y B-10 de la bitácora).
    ///
    /// <see cref="Clases.Res"/> la implementa sin ganar miembros: Nombre ya existía.
    /// </summary>
    public interface IArticuloVendible
    {
        /// <summary>Nombre del artículo tal como se muestra al operario.</summary>
        string Nombre { get; }
    }
}
