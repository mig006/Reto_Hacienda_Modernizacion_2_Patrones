using Bib_Hacienda.Clases;
using static Bib_Hacienda.Clases.Potrero;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · Traduce entre una línea de Potreros.txt y un <see cref="Potrero"/>.
    ///
    /// Traducción literal de PersistenciaService.cs:82 (escritura) y :323-333 (lectura).
    /// El formato del archivo no cambia (ADR-09): sigue siendo texto plano separado
    /// por '|', sin cabecera ni marca de versión.
    /// </summary>
    public static class MapeadorPotrero
    {
        public static string ALinea(Potrero potrero) => $"{potrero.Identificacion}|{potrero.Tipo_potrero}";

        /// <summary>
        /// Devuelve false para las líneas que el sistema descarta hoy: en blanco o con
        /// menos de dos columnas (PersistenciaService.cs:321,324).
        /// Un tipo de potrero desconocido SÍ lanza, igual que hoy: Enum.Parse no perdona.
        /// </summary>
        public static bool TryDeLinea(string linea, out Potrero potrero)
        {
            potrero = null!;
            if (string.IsNullOrWhiteSpace(linea)) return false;

            var partes = linea.Split('|');
            if (partes.Length < 2) return false;

            string identificacion = partes[0].Trim();   // normalizar
            l_tipos_potreros tipo = Enum.Parse<l_tipos_potreros>(partes[1]);
            potrero = new Potrero(identificacion, tipo);
            return true;
        }
    }
}
