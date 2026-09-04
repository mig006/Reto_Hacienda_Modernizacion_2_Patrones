using Bib_Hacienda.Clases;
using Bib_Hacienda.Valores;
using System;
using static Bib_Hacienda.Clases.Potrero;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-05 · DI-6 · Contrato de construcción de reses.
    ///
    /// Sustituye a los DOS puntos de construcción por cadena mágica que había:
    ///   · el switch sobre string de Potrero.cs:88-102, alimentado por otro switch
    ///     sobre el tipo de potrero (:62-83);
    ///   · el switch de reconstrucción desde disco de PersistenciaService.cs:434-440.
    ///
    /// Ambos cambiaban por la MISMA razón —aparece un tipo de res nuevo— y por eso van
    /// juntos aquí, no separados.
    ///
    /// ── Por qué una jerarquía y no un switch ─────────────────────────────────────
    /// Es implementación de interfaz, no herencia de clase: no hay estado ni
    /// comportamiento heredado, solo un contrato. Esa es precisamente la razón de
    /// preferirlo aquí — no se hereda nada, así que no hay contrato de superclase que un
    /// subtipo pueda debilitar. VEREDICTO LSP: PASA POR CONSTRUCCIÓN.
    ///
    /// Alternativa evaluada: un Dictionary&lt;l_tipos_potreros, Func&lt;...&gt;&gt; en el
    /// composition root. Más compacta, pero el enunciado exige correspondencia uno a uno
    /// entre diagrama y código, y tres lambdas anónimas no son representables como
    /// elementos del diagrama. Descartada.
    ///
    /// ── El detalle que corrige el error de la jerarquía Res ──────────────────────
    /// El contrato EXPONE RangoSoportado, de modo que el cliente puede consultar la
    /// precondición antes de invocar en lugar de descubrirla por excepción. Eso es
    /// justamente lo que la jerarquía Res no permitía hacer.
    /// </summary>
    public interface IFabricaRes
    {
        /// <summary>Tipo de potrero al que corresponde este subtipo de res.</summary>
        l_tipos_potreros TipoPotreroSoportado { get; }

        /// <summary>
        /// Tipo concreto que produce. Su Name es el discriminador que se persiste
        /// (GetType().Name) y por el que se reconstruye desde disco.
        /// </summary>
        Type TipoSoportado { get; }

        /// <summary>Precondición consultable: rango de edad que esta fábrica acepta.</summary>
        RangoEdad RangoSoportado { get; }

        /// <summary>
        /// Postcondición: devuelve una Res no nula del tipo declarado en TipoSoportado.
        /// Lanza Exception con el literal congelado del subtipo si la edad queda fuera
        /// del rango, igual que hoy.
        /// </summary>
        Res Crear(string nombre, uint peso, ushort edad);
    }
}
