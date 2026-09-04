using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · DI-3 · Implementación en archivos planos de
    /// <see cref="IRepositorioCatalogoVacunas"/>. Dueño exclusivo de Vacunas.txt.
    /// Traducción de PersistenciaService.cs:175-215 y :455-527.
    /// El mapeo congelado de D-1 vive en MapeadorVacuna, no aquí.
    /// </summary>
    public sealed class RepositorioCatalogoVacunasArchivo : IRepositorioCatalogoVacunas
    {
        private readonly string _directorioArchivos;

        public RepositorioCatalogoVacunasArchivo(string directorioArchivos)
        {
            _directorioArchivos = directorioArchivos;
            if (!Directory.Exists(_directorioArchivos))
            {
                Directory.CreateDirectory(_directorioArchivos);
            }
        }

        private string Ruta(string archivo) => Path.Combine(_directorioArchivos, archivo);

        public List<Vacuna> CargarVacunas()
        {
            try
            {
                string rutaArchivo = Ruta("Vacunas.txt");
                if (!File.Exists(rutaArchivo)) return new List<Vacuna>();

                var vacunas = new List<Vacuna>();
                foreach (var linea in File.ReadAllLines(rutaArchivo))
                {
                    if (MapeadorVacuna.TryDeLineaCatalogo(linea, out var vacuna))
                    {
                        vacunas.Add(vacuna);
                    }
                }
                return vacunas;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar vacunas: {ex.Message}");
            }
        }

        public void GuardarVacunas(List<Vacuna> vacunas)
        {
            try
            {
                var lineas = vacunas.Select(MapeadorVacuna.ALineaCatalogo).ToList();
                File.WriteAllLines(Ruta("Vacunas.txt"), lineas);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar vacunas: {ex.Message}", ex);
            }
        }
    }
}
