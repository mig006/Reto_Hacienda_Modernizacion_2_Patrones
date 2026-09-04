using System;

namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// SC-2 · ADR-11 · §5.6 · Chip de geolocalización conectado a una res.
    ///
    /// ══ Es un OBJETO DE VALOR, no una entidad ═══════════════════════════════════
    /// Un chip no tiene identidad propia dentro del dominio: no se consulta sin su res,
    /// no se transfiere entre animales durante su vida útil en este alcance, y dos chips
    /// con el mismo identificador y la misma lectura son indistinguibles y equivalentes.
    ///
    /// De ahí que sea INMUTABLE. Registrar una posición nueva **reemplaza** el objeto en
    /// lugar de mutarlo, lo que elimina de raíz la clase de fallo que este rediseño
    /// persigue en `Res.Edad` (§5.2): un estado que cambia bajo los pies de quien tiene
    /// la referencia.
    ///
    /// ══ Por qué NO hay jerarquía ════════════════════════════════════════════════
    /// Se consideró `Chip` como base de `ChipGPS` / `ChipRFID`, anticipando distintas
    /// tecnologías de rastreo. Se descartó porque **no hay comportamiento que varíe entre
    /// esos supuestos subtipos**: un chip guarda un identificador y una posición, y eso no
    /// cambia según la tecnología que la produjo. Sería herencia usada como etiqueta, y el
    /// diagnóstico de este mismo sistema ya muestra a dónde lleva eso (H-01, H-16).
    ///
    /// Si mañana un tipo de chip trae reglas propias —una periodicidad de lectura, un
    /// formato de identificador verificable—, la jerarquía se introduce entonces, con la
    /// evidencia delante. Declararlo es parte del argumento: la ausencia de jerarquía es
    /// una elección, no un olvido.
    ///
    /// ══ Por qué NO tiene repositorio propio ═════════════════════════════════════
    /// ADR-11 evaluó y descartó `Chip` como entidad con `IRepositorioChips`: añadiría un
    /// séptimo archivo y un punto de inconsistencia transaccional sin ningún caso de uso
    /// que lo pida. El chip se persiste con su res, en cuatro columnas anexadas al final
    /// de la línea (ADR-12). Queda anotado por si el negocio pide inventario de chips no
    /// asignados.
    /// </summary>
    public sealed class Chip
    {
        public Chip(string identificador, double latitud, double longitud, DateTime ultimaLectura)
        {
            Identificador = identificador;
            Latitud = latitud;
            Longitud = longitud;
            UltimaLectura = ultimaLectura;
        }

        public string Identificador { get; }

        public double Latitud { get; }

        public double Longitud { get; }

        public DateTime UltimaLectura { get; }
    }
}
