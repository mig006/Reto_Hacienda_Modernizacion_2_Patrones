using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;
using System;
using static Bib_Hacienda.Clases.Potrero;

namespace Bib_Hacienda.Fabricas
{
    /// <summary>
    /// ADR-05 · Fábrica de <see cref="Ternero"/>. Sin estado, sellada y determinista.
    ///
    /// ── ATENCIÓN · dos rangos que NO son el mismo ────────────────────────────────
    /// RangoSoportado es el rango de ADMISIÓN de un potrero, el que aplicaba
    /// Potrero.anadir_res (:49-86). Res.EdadPermitida es la INVARIANTE DEL TIPO, la que
    /// aplicaba el setter del subtipo. En el sistema original NO coinciden para Novillo,
    /// y la diferencia es alcanzable, así que se conservan por separado.
    /// Para Ternero ambos rangos coinciden: [0, 12].
    /// </summary>
    public sealed class FabricaTernero : IFabricaRes
    {
        public l_tipos_potreros TipoPotreroSoportado => l_tipos_potreros.ternero;

        public Type TipoSoportado => typeof(Ternero);

        // Potrero.cs:66-69 · edad_min_potrero = 0 (por defecto), edad_max_potrero = 12
        public RangoEdad RangoSoportado => new RangoEdad(0, ReglaRes.edad_max_ternero);

        public Res Crear(string nombre, uint peso, ushort edad) => new Ternero(nombre, peso, edad);
    }
}
