using Bib_Hacienda.Clases;
using System;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// Strategy (Actividad 2 · P-02) · Qué le pasa al inventario cuando se concreta
    /// una venta.
    ///
    /// ══ Qué sustituye ═══════════════════════════════════════════════════════════
    /// ServicioVenta.vender_res decidía a mano, en el propio método, que vender SIEMPRE
    /// retira la res del potrero (ServicioVenta.cs:46). Esa decisión estaba bien
    /// mientras solo existiera una forma de vender. SC-1 introduce una segunda —vender
    /// leche, carne o piel— que NO debe tocar el inventario, y meter un
    /// `if (articulo is Res) … else …` dentro de ServicioVenta sería exactamente el
    /// antipatrón que el enunciado marca: mover el punto de modificación, no eliminarlo.
    ///
    /// ══ Cómo se resuelve, igual que IFabricaVacuna ═══════════════════════════════
    /// ServicioVenta recibe un registro de estas estrategias, indexado por
    /// <see cref="TipoSoportado"/>, y punto: un artículo vendible nuevo con un efecto
    /// distinto es una clase más y una línea en la raíz de composición, no una rama
    /// nueva en un condicional existente.
    /// </summary>
    public interface IEfectoVenta
    {
        /// <summary>Tipo concreto de <see cref="IArticuloVendible"/> que maneja esta estrategia.</summary>
        Type TipoSoportado { get; }

        /// <summary>Aplica sobre el inventario el efecto que corresponde a vender este artículo.</summary>
        void Aplicar(Hacienda hacienda, Potrero potrero, IArticuloVendible articulo);
    }
}
