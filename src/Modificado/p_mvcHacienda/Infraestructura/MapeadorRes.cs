using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System.Globalization;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · Traduce entre una línea de Reses.txt y los datos de una <see cref="Res"/>.
    ///
    /// Traducción literal de PersistenciaService.cs:116-117 (escritura) y :367-379 (lectura).
    ///
    /// Detalle que hay que conservar y que resulta contraintuitivo: al CARGAR, la quinta
    /// columna —el tipo de res— se IGNORA. El subtipo concreto no lo decide el archivo
    /// sino el potrero donde entra la res, porque la carga delega en `Potrero.anadir_res`
    /// (PersistenciaService.cs:378). La columna se escribe pero no se lee. Cambiar eso
    /// alteraría qué animales sobreviven a un reinicio.
    ///
    /// ══════════════════════════════════════════════════════════════════════════════
    /// SC-2 · ADR-12 · LAS CUATRO COLUMNAS DEL CHIP SE ANEXAN AL FINAL
    ///
    /// El riesgo mejor documentado de toda la Fase 2 no es de código sino de datos: los
    /// `.txt` no tienen cabecera ni marca de versión, y los cargadores parsean por índice
    /// posicional detrás de una guarda de longitud. Si el formato cambia de manera
    /// incompatible, las líneas no cumplen la guarda y **se descartan sin lanzar
    /// excepción**: el histórico desaparece en silencio.
    ///
    /// La observación que habilita la solución: la guarda es `>=`, no `==`. El parser
    /// **ya tolera columnas de más** y las ignora. Por eso:
    ///
    ///   Sin chip (todo el histórico, y toda res nueva sin asignar):
    ///   Potrero_Cebones|Rayo|287|14|Cebon                          ← 5 columnas, idéntico a hoy
    ///
    ///   Con chip:
    ///   Potrero_Cebones|Rayo|287|14|Cebon|CHIP-0417|6.244200|-75.581200|2026-08-08
    ///
    /// REGLA, y aplica también a SC-1 y SC-3: columnas nuevas **solo al final**, nunca
    /// reordenar ni intercalar; y un registro de forma distinta va a un archivo nuevo, no
    /// a columnas vacías. El orden de las columnas queda congelado para siempre, porque el
    /// índice posicional es el contrato.
    ///
    /// Costo aceptado: el archivo queda HETEROGÉNEO —líneas de 5 y de 9 columnas
    /// conviviendo— y quien lo abra a mano no puede deducir el trazado mirando una sola
    /// línea. Se acepta porque la alternativa es reescribir datos del cliente, que es un
    /// cambio de salida no autorizado.
    /// ══════════════════════════════════════════════════════════════════════════════
    ///
    /// ADR-05 · Este mapeador es también el punto nº6 de los nueve puntos de cambio que
    /// hoy cuesta agregar un tipo de res: la reconstrucción de subtipos desde disco.
    /// </summary>
    public static class MapeadorRes
    {
        /// <summary>Número de columnas de una línea sin chip. La guarda histórica.</summary>
        private const int ColumnasBase = 5;

        /// <summary>Número de columnas de una línea con chip: las 5 de siempre + 4.</summary>
        private const int ColumnasConChip = 9;

        /// <summary>Formato de la última lectura, fijado por el ejemplo de ADR-12.</summary>
        private const string FormatoFechaLectura = "yyyy-MM-dd";

        public static string ALinea(Potrero potrero, Res res)
        {
            string tipoRes = res.GetType().Name;
            string linea = $"{potrero.Identificacion}|{res.Nombre}|{res.Peso}|{res.Edad}|{tipoRes}";

            // ADR-12 · Las cuatro columnas se escriben SOLO si la res tiene chip. Una res
            // sin chip produce exactamente la misma línea que producía el sistema original,
            // byte a byte. Esa es la propiedad que mantiene verdes los quince casos de
            // caracterización y que el caso 17 comprueba con un diff.
            if (res.Chip != null)
            {
                linea += "|" + string.Join("|",
                    res.Chip.Identificador,
                    res.Chip.Latitud.ToString("F6", CultureInfo.InvariantCulture),
                    res.Chip.Longitud.ToString("F6", CultureInfo.InvariantCulture),
                    res.Chip.UltimaLectura.ToString(FormatoFechaLectura, CultureInfo.InvariantCulture));
            }

            return linea;
        }

        /// <summary>
        /// Extrae los datos de una línea de Reses.txt. Devuelve false para las líneas que
        /// el sistema descarta hoy: en blanco o con menos de cinco columnas.
        ///
        /// El chip sale por separado porque no puede fijarse al construir la res: se
        /// conecta después, con `Res.AsignarChip` (ADR-11). `chip` vale `null` para las
        /// líneas de cinco columnas, que son todo el histórico.
        /// </summary>
        public static bool TryDeLinea(string linea, out string identificacionPotrero,
            out string nombre, out uint peso, out ushort edad, out Chip chip)
        {
            identificacionPotrero = null!;
            nombre = null!;
            peso = 0;
            edad = 0;
            chip = null!;

            if (string.IsNullOrWhiteSpace(linea)) return false;

            var partes = linea.Split('|');
            if (partes.Length < ColumnasBase) return false;

            identificacionPotrero = partes[0].Trim();
            nombre = partes[1];
            peso = uint.Parse(partes[2]);
            edad = ushort.Parse(partes[3]);
            // partes[4] (el tipo) se ignora deliberadamente: ver el comentario de la clase.

            // ADR-12 · Las columnas nuevas se consultan solo si están. Ninguna línea
            // histórica cambia de significado y ninguna se descarta.
            if (partes.Length >= ColumnasConChip)
            {
                string identificador = partes[5];
                bool latOk = double.TryParse(partes[6].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double latitud);
                bool lonOk = double.TryParse(partes[7].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double longitud);
                bool fechaOk = DateTime.TryParseExact(partes[8].Trim(), FormatoFechaLectura,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lectura);

                if (!string.IsNullOrWhiteSpace(identificador) && latOk && lonOk && fechaOk)
                {
                    chip = new Chip(identificador, latitud, longitud, lectura);
                }
                // Si las columnas del chip están corruptas, la res se carga SIN chip en
                // lugar de perderse. Es coherente con el criterio del resto del sistema:
                // una línea de res nunca se descarta si sus cinco columnas base son válidas.
            }

            return true;
        }

        /// <summary>
        /// Reconstruye un subtipo de <see cref="Res"/> a partir del discriminador que se
        /// persistió con <c>GetType().Name</c>. Lo usa <see cref="MapeadorVenta"/> para
        /// rehidratar la res de cada venta histórica.
        ///
        /// Traducción literal de PersistenciaService.cs:434-440, incluida la rama por
        /// defecto que convierte cualquier tipo desconocido en Ternero.
        ///
        /// ADR-05 · DI-6 · El switch sobre cadena mágica lo sustituye el REGISTRO DE
        /// FÁBRICAS resuelto en el composition root, que es el mismo que usa
        /// `Potrero.anadir_res`. Antes eran dos puntos de construcción independientes que
        /// había que mantener sincronizados a mano.
        ///
        /// ATENCIÓN · deuda D-4: esta reconstrucción NO valida el rango de admisión del
        /// potrero, así que una fila incoherente hace que el constructor del subtipo lance
        /// con el literal congelado de ese subtipo. Para Novillo ese literal dice
        /// "El ternero excedió la edad maxima" — nombra al animal equivocado. Se conserva
        /// a propósito porque termina en la consola de arranque.
        /// </summary>
        public static Res ReconstruirRes(IEnumerable<IFabricaRes> fabricas, string tipo,
            string nombre, uint peso, ushort edad)
        {
            var fabrica = fabricas.FirstOrDefault(f => f.TipoSoportado.Name == tipo)
                          ?? fabricas.First(f => f.TipoSoportado == typeof(Ternero));

            return fabrica.Crear(nombre, peso, edad);
        }
    }
}
