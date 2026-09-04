using System.Text;

namespace Caracterizacion.Rediseno
{
    /// <summary>
    /// Punto de entrada del arnés de la sistema REDISEÑADO.
    ///
    /// Ejecuta los quince casos de ADRs.md §8.6 contra el sistema original y deja
    /// en 04-evidencia/ la salida y una copia de los seis .txt resultantes. Nada
    /// de esto toca los datos del repositorio: se trabaja sobre copias en un directorio
    /// temporal, de modo que el arnés es idempotente.
    /// </summary>
    public static class PuntoDeEntrada
    {
        private const string NombreSalida = "salida-rediseñada.txt";
        private const string NombreDatos = "datos-rediseñado";
        private const string NombreSalidaSC2 = "salida-sc2.txt";
        private const string NombreDatosSC2 = "datos-sc2";

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string raizReto = EncontrarRaizDelReto();
            string semilla = Path.Combine(raizReto, "03-src", "Modificado", "p_mvcHacienda", "Datos");
            string evidencia = Path.Combine(raizReto, "04-evidencia");
            string trabajo = Path.Combine(Path.GetTempPath(), "caracterizacion-rediseno");

            if (Directory.Exists(trabajo)) Directory.Delete(trabajo, recursive: true);

            var registro = new Registro();
            // El encabezado NO menciona qué versión lo produjo: los dos arneses escriben
            // exactamente el mismo texto para que el diff entre ambas salidas sea vacío.
            // Qué versión generó cada archivo lo dice su nombre.
            registro.Linea("CARACTERIZACIÓN · casos de 02-diseno/ADRs.md §8.6");
            registro.Linea("Categorías observables verificadas: 1 texto · 2 tipo de alerta · 3 navegación · 4 archivos · 5 listados · 6 consola");

            // --- Fixture F1: datos históricos reales ---
            string raizF1 = Path.Combine(trabajo, "f1");
            CopiarDirectorio(semilla, Path.Combine(raizF1, "Datos"));
            new Escenarios(registro, raizF1).CargaDeArranque();

            // --- Fixture F2: escenarios guionizados sobre datos vacíos ---
            string raizF2 = Path.Combine(trabajo, "f2");
            Directory.CreateDirectory(Path.Combine(raizF2, "Datos"));
            new Escenarios(registro, raizF2).Ejecutar();

            // --- Fixture F3: SC-2 · casos 16 a 18 ---
            //
            // Van a su PROPIO archivo porque no se comparan contra la línea base: el
            // sistema original no tiene chips. Mantenerlos fuera de salida-rediseñada.txt
            // es lo que deja intacto el diff que prueba la preservación (§8.1).
            var registroSC2 = new Registro();
            registroSC2.Linea("SC-2 · casos 16 a 18 de 02-diseno/ADRs.md §8.6");
            registroSC2.Linea("Solo del sistema rediseñado: verifican ADR-11 y ADR-12.");

            string raizF3 = Path.Combine(trabajo, "f3");
            CopiarDirectorio(semilla, Path.Combine(raizF3, "Datos"));
            string resesDelOriginal = Path.Combine(evidencia, "datos-original", "f1-historicos", "Reses.txt");
            new Escenarios(registroSC2, raizF3).EjecutarSC2(resesDelOriginal);

            // --- Volcado de evidencia ---
            Directory.CreateDirectory(evidencia);
            string rutaSalida = Path.Combine(evidencia, NombreSalida);
            File.WriteAllText(rutaSalida, registro.ToString(), new UTF8Encoding(false));

            string destinoDatos = Path.Combine(evidencia, NombreDatos);
            if (Directory.Exists(destinoDatos)) Directory.Delete(destinoDatos, recursive: true);
            CopiarDatosNormalizados(Path.Combine(raizF1, "Datos"), Path.Combine(destinoDatos, "f1-historicos"));
            CopiarDatosNormalizados(Path.Combine(raizF2, "Datos"), Path.Combine(destinoDatos, "f2-escenarios"));

            string rutaSalidaSC2 = Path.Combine(evidencia, NombreSalidaSC2);
            File.WriteAllText(rutaSalidaSC2, registroSC2.ToString(), new UTF8Encoding(false));

            string destinoSC2 = Path.Combine(evidencia, NombreDatosSC2);
            if (Directory.Exists(destinoSC2)) Directory.Delete(destinoSC2, recursive: true);
            CopiarDatosNormalizados(Path.Combine(raizF3, "Datos"), destinoSC2);

            Console.WriteLine($"Salida escrita en  : {rutaSalida}");
            Console.WriteLine($"Salida SC-2 en     : {rutaSalidaSC2}");
            Console.WriteLine($"Datos copiados en  : {destinoDatos}");
            return 0;
        }

        /// <summary>Sube por el árbol hasta encontrar la carpeta que contiene el enunciado.</summary>
        private static string EncontrarRaizDelReto()
        {
            var directorio = new DirectoryInfo(AppContext.BaseDirectory);
            while (directorio != null)
            {
                if (File.Exists(Path.Combine(directorio.FullName, "Reto_Modernizacion_Arquitectonica_SOLID_Enunciado_y_Rubrica.md")))
                {
                    return directorio.FullName;
                }
                directorio = directorio.Parent;
            }
            throw new InvalidOperationException(
                "No se encontró la raíz del reto (el directorio que contiene el enunciado).");
        }

        private static void CopiarDirectorio(string origen, string destino)
        {
            Directory.CreateDirectory(destino);
            foreach (string archivo in Directory.GetFiles(origen))
            {
                File.Copy(archivo, Path.Combine(destino, Path.GetFileName(archivo)), overwrite: true);
            }
        }

        /// <summary>
        /// Copia los .txt aplicando la misma normalización de fechas que la salida de
        /// texto, para que el diff entre las dos versiones no dependa del día en que
        /// se ejecutó cada una.
        /// </summary>
        private static void CopiarDatosNormalizados(string origen, string destino)
        {
            Directory.CreateDirectory(destino);
            foreach (string archivo in Directory.GetFiles(origen, "*.txt").OrderBy(a => a))
            {
                string contenido = Normalizador.Normalizar(File.ReadAllText(archivo));
                File.WriteAllText(Path.Combine(destino, Path.GetFileName(archivo)), contenido, new UTF8Encoding(false));
            }
        }
    }
}
