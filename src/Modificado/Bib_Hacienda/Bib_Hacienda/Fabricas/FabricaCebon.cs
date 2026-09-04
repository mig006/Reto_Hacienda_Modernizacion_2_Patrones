using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;
using System;
using static Bib_Hacienda.Clases.Potrero;

namespace Bib_Hacienda.Fabricas
{
    /// <summary>
    /// ADR-05 · Fábrica de <see cref="Cebon"/>. Sin estado, sellada y determinista.
    ///
    /// ── ATENCIÓN · dos rangos que NO son el mismo ────────────────────────────────
    /// RangoSoportado es el rango de ADMISIÓN de un potrero, el que aplicaba
    /// Potrero.anadir_res (:49-86). Res.EdadPermitida es la INVARIANTE DEL TIPO, la que
    /// aplicaba el setter del subtipo. En el sistema original NO coinciden para Novillo,
    /// y la diferencia es alcanzable, así que se conservan por separado.
    /// Para Cebon ambos rangos coinciden: [13, 48].
    /// </summary>
    public sealed class FabricaCebon : IFabricaRes
    {
        public l_tipos_potreros TipoPotreroSoportado => l_tipos_potreros.cebon;

        public Type TipoSoportado => typeof(Cebon);

        // Potrero.cs:71-76 · edad_min_potrero = 12 + 1 = 13, edad_max_potrero = 48
        public RangoEdad RangoSoportado
            => new RangoEdad((ushort)(ReglaRes.edad_max_ternero + 1), ReglaRes.edad_max_cebon);

        public Res Crear(string nombre, uint peso, ushort edad) => new Cebon(nombre, peso, edad);
    }
}
