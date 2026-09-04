using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Reglas;
using Bib_Hacienda.Valores;
using System;
using static Bib_Hacienda.Clases.Potrero;

namespace Bib_Hacienda.Fabricas
{
    /// <summary>
    /// ADR-05 · Fábrica de <see cref="Novillo"/>. Sin estado, sellada y determinista.
    ///
    /// ── ATENCIÓN · dos rangos que NO son el mismo ────────────────────────────────
    /// RangoSoportado es el rango de ADMISIÓN de un potrero, el que aplicaba
    /// Potrero.anadir_res (:49-86). Res.EdadPermitida es la INVARIANTE DEL TIPO, la que
    /// aplicaba el setter del subtipo. En el sistema original NO coinciden para Novillo,
    /// y la diferencia es alcanzable, así que se conservan por separado.
    ///
    /// ── AQUÍ ES DONDE LOS DOS RANGOS DIFIEREN, Y ES UN HALLAZGO PROPIO ───────────
    /// Potrero.anadir_res declaraba `byte edad_max_potrero = 255` y, en la rama de
    /// novillo, NUNCA lo reasignaba (Potrero.cs:50, :78-82): solo fijaba el mínimo en 49.
    /// De modo que el potrero admitía [49, 255] mientras que el setter de Novillo
    /// aceptaba cualquier edad mayor que 48, hasta 65535.
    ///
    /// La diferencia es ALCANZABLE: dar de alta un novillo de 300 meses por el formulario
    /// se rechaza, pero reconstruir uno de 300 meses desde Ventas.txt funciona. Es
    /// exactamente el tipo de regla duplicada y divergente que ADR-05 denuncia, y por eso
    /// los dos rangos se conservan tal cual en lugar de "unificarlos": unificarlos
    /// cambiaría el comportamiento observable en uno de los dos caminos.
    /// </summary>
    public sealed class FabricaNovillo : IFabricaRes
    {
        /// <summary>Tope de admisión del potrero: el valor por defecto de un byte.</summary>
        private const ushort TopeDeAdmisionDelPotrero = 255;

        public l_tipos_potreros TipoPotreroSoportado => l_tipos_potreros.novillo;

        public Type TipoSoportado => typeof(Novillo);

        // Potrero.cs:78-82 · edad_min_potrero = 48 + 1 = 49; edad_max_potrero se queda en 255.
        public RangoEdad RangoSoportado
            => new RangoEdad((ushort)(ReglaRes.edad_max_cebon + 1), TopeDeAdmisionDelPotrero);

        public Res Crear(string nombre, uint peso, ushort edad) => new Novillo(nombre, peso, edad);
    }
}
