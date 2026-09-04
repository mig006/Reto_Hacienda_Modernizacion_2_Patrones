using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Fabricas;
using Bib_Hacienda.Valores;
using System.Collections.Generic;
using System.Globalization;
using static Bib_Hacienda.Clases.Viva;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 y ADR-06 · Traduce entre líneas de Vacunas.txt / VacunasAplicadas.txt y
    /// objetos <see cref="Vacuna"/>.
    ///
    /// ══════════════════════════════════════════════════════════════════════════════
    /// MAPEO CONGELADO · DEUDA D-1 (ADRs.md §8.3)
    ///
    /// Este mapeador reproduce a propósito un defecto activo. Al guardar una vacuna
    /// VIVA escribe 0 en la columna de período, y al cargarla reconstruye siempre
    /// Atenuacion10 fijo. Es decir: EL GRADO DE ATENUACIÓN DE TODA VACUNA VIVA SE
    /// PIERDE EN CADA REINICIO, tanto en el catálogo como en el historial sanitario.
    ///
    /// Traducción literal de PersistenciaService.cs:201, :256 (escritura) y :514, :586
    /// (lectura).
    ///
    /// NO ES UN DESCUIDO. ADR-06 evaluó corregirlo y lo descartó por restricción del
    /// cliente, no por criterio técnico: cambiaría el contenido de dos .txt y el estado
    /// en memoria tras reiniciar, y el enunciado solo autoriza como excepción las tres
    /// solicitudes de cambio.
    ///
    /// Lo que ADR-06 SÍ hizo es exponer Viva.Periodo_atenuacion, que es invisible
    /// —ninguna vista muestra la atenuación— y cierra la violación de LSP en la
    /// jerarquía. Con eso, el defecto deja de ser «Viva esconde estado» y pasa a ser
    /// «el mapeador decide no leerlo»: dos ramas localizables en un solo archivo.
    ///
    /// PARCHE, cuando la líder técnica lo autorice: sustituir PeriodoDe() por el valor
    /// real de cada subtipo y, al cargar, reconstruir con el valor leído en lugar de
    /// Atenuacion10. Migración de datos históricos: interpretar 0 como Atenuacion10,
    /// que es lo que el sistema hace hoy.
    /// ══════════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class MapeadorVacuna
    {
        // ADR-13 (Factory Method, P-01) · Mismo registro que resuelve FabricaVacunas,
        // instanciado aquí porque el mapeador es infraestructura pura sin contenedor de
        // DI. Las dos fábricas son sin estado y deterministas (igual que FabricaTernero,
        // etc.), así que instanciarlas directamente no introduce ningún costo ni estado
        // compartido problemático. Reemplaza el `new Bacteriana(...)` / `new Viva(...)`
        // que antes se escribía por separado en cada una de las dos ramas de abajo.
        private static readonly IReadOnlyDictionary<string, IFabricaVacuna> FabricasPorTipo =
            new Dictionary<string, IFabricaVacuna>
            {
                ["Bacteriana"] = new FabricaVacunaBacteriana(),
                ["Viva"] = new FabricaVacunaViva(),
            };

        // --- Catálogo (Vacunas.txt) ---------------------------------------

        public static string ALineaCatalogo(Vacuna vacuna)
        {
            string fechaVenc = vacuna.Fecha_vencimiento.ToString("yyyy-MM-dd");
            string fechaAplic = vacuna.Fecha_aplicacion.ToString("yyyy-MM-dd");
            string tipo = vacuna.GetType().Name;
            uint periodo = PeriodoDe(vacuna);

            return $"{vacuna.Nombre}|{vacuna.Lote}|{fechaVenc}|{fechaAplic}|{tipo}|{periodo}";
        }

        /// <summary>
        /// Traducción de PersistenciaService.cs:479-518.
        ///
        /// Las bacterianas con período fuera de [2,4] se OMITEN en silencio, igual que
        /// hoy (:498-502): la línea se pierde sin aviso. Las vivas nunca se omiten
        /// porque su columna de período no se consulta.
        /// </summary>
        public static bool TryDeLineaCatalogo(string linea, out Vacuna vacuna)
        {
            vacuna = null!;
            if (string.IsNullOrWhiteSpace(linea)) return false;

            var partes = linea.Split('|');
            if (partes.Length < 6) return false;

            string nombre = partes[0];
            string lote = partes[1];
            if (!DateTime.TryParseExact(partes[2].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaVenc)) return false;
            if (!DateTime.TryParseExact(partes[3].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaAplic)) return false;
            string tipo = partes[4].Trim();

            // El match sigue siendo case-insensitive, igual que en el Reto 1: cualquier
            // valor que no sea "Bacteriana" (con ese criterio) cae en Viva por defecto.
            bool esBacteriana = tipo.Equals("Bacteriana", StringComparison.OrdinalIgnoreCase);
            var fabrica = FabricasPorTipo[esBacteriana ? "Bacteriana" : "Viva"];

            uint? periodoParaSolicitud = null;
            enum_l_atenuaciones? atenuacionParaSolicitud = null;

            if (esBacteriana)
            {
                if (!uint.TryParse(partes[5].Trim(), out uint periodo) || periodo < 2 || periodo > 4)
                {
                    return false;   // omitir y seguir
                }
                periodoParaSolicitud = periodo;
            }
            else
            {
                // D-1 · el grado leído del archivo se descarta y se fija Atenuacion10.
                atenuacionParaSolicitud = enum_l_atenuaciones.Atenuacion10;
            }

            try
            {
                vacuna = fabrica.Crear(new SolicitudVacuna(nombre, lote, fechaVenc, fechaAplic,
                    periodoParaSolicitud, atenuacionParaSolicitud));
            }
            catch
            {
                return false;   // por si el constructor valida otras reglas
            }

            return true;
        }

        // --- Historial aplicado (VacunasAplicadas.txt) ---------------------

        public static string ALineaAplicada(Potrero potrero, Res res, Vacuna vacuna)
        {
            string fechaVenc = vacuna.Fecha_vencimiento.ToString("yyyy-MM-dd");
            string fechaAplic = vacuna.Fecha_aplicacion.ToString("yyyy-MM-dd");
            string tipo = vacuna.GetType().Name;
            uint periodo = PeriodoDe(vacuna);

            return $"{potrero.Identificacion}|{res.Nombre}|{vacuna.Nombre}|{vacuna.Lote}|{fechaVenc}|{fechaAplic}|{tipo}|{periodo}";
        }

        /// <summary>
        /// Traducción de PersistenciaService.cs:555-587.
        ///
        /// Dos asimetrías respecto del catálogo que NO son erratas de transcripción y
        /// hay que conservar:
        ///   · el tipo se lee SIN Trim y se compara de forma sensible a mayúsculas
        ///     (<c>tipo == "Bacteriana"</c>, :580), no con OrdinalIgnoreCase;
        ///   · el período NO se acota a [2,4], de modo que un valor fuera de rango
        ///     hace lanzar al constructor de Bacteriana en lugar de omitir la línea.
        /// </summary>
        public static bool TryDeLineaAplicada(string linea, out string identificacionPotrero,
            out string nombreRes, out Vacuna vacuna)
        {
            identificacionPotrero = null!;
            nombreRes = null!;
            vacuna = null!;

            if (string.IsNullOrWhiteSpace(linea)) return false;

            var partes = linea.Split('|');
            if (partes.Length < 8) return false;

            identificacionPotrero = partes[0].Trim();
            nombreRes = partes[1];
            string nombreVacuna = partes[2];
            string lote = partes[3];
            if (!DateTime.TryParseExact(partes[4].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaVenc)) return false;
            if (!DateTime.TryParseExact(partes[5].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaAplic)) return false;
            string tipo = partes[6];
            uint periodo = uint.TryParse(partes[7].Trim(), out var per) ? per : 0u;

            // Asimetría con el catálogo, conservada a propósito (ver comentario de la
            // clase): aquí la comparación ES sensible a mayúsculas y no hay Trim.
            bool esBacteriana = tipo == "Bacteriana";
            var fabrica = FabricasPorTipo[esBacteriana ? "Bacteriana" : "Viva"];

            var solicitud = esBacteriana
                ? new SolicitudVacuna(nombreVacuna, lote, fechaVenc, fechaAplic, periodo, null)
                // D-1 · otra vez: el grado real no se lee, se fija Atenuacion10.
                : new SolicitudVacuna(nombreVacuna, lote, fechaVenc, fechaAplic, null, enum_l_atenuaciones.Atenuacion10);

            vacuna = fabrica.Crear(solicitud);

            return true;
        }

        /// <summary>
        /// D-1 · La rama que congela el defecto. Devuelve el período real si la vacuna
        /// es bacteriana y 0 si es viva, descartando su grado de atenuación.
        /// Traducción literal de PersistenciaService.cs:201 y :256.
        /// </summary>
        private static uint PeriodoDe(Vacuna vacuna)
            => vacuna is Bacteriana bacteriana ? bacteriana.Periodo_aplicacion : 0;
    }
}
