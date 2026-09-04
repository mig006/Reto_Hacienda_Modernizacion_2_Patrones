using System;

namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// ADR-05 · Rango de edad admisible para un subtipo de res, en meses.
    ///
    /// Existe para convertir lo que antes era una PRECONDICIÓN FORTALECIDA —cada
    /// subtipo restringía por su cuenta el setter de Res.Edad y lanzaba— en una
    /// INVARIANTE DE CLASE consultable: el rango forma parte del contrato, de modo que
    /// el llamador puede preguntarlo antes de invocar en lugar de descubrirlo por
    /// excepción. Ese es exactamente el error que la jerarquía Res tenía y que
    /// IFabricaRes evita por construcción.
    /// </summary>
    public sealed class RangoEdad
    {
        public RangoEdad(ushort minimo, ushort maximo)
        {
            Minimo = minimo;
            Maximo = maximo;
        }

        public ushort Minimo { get; }

        public ushort Maximo { get; }

        public bool Contiene(ushort edad) => edad >= Minimo && edad <= Maximo;
    }
}
