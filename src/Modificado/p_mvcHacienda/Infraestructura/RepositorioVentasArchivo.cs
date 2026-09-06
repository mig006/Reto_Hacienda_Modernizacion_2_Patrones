using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · DI-2 · Implementación en archivos planos de <see cref="IRepositorioVentas"/>.
    /// Dueño exclusivo de Ventas.txt. Traducción de PersistenciaService.cs:133-172 y :389-452.
    /// </summary>
    public sealed class RepositorioVentasArchivo : IRepositorioVentas
    {
        private readonly string _directorioArchivos;
        private readonly IReadOnlyCollection<IFabricaRes> _fabricas;

        // Las fábricas se necesitan para rehidratar la res de cada venta histórica.
        public RepositorioVentasArchivo(string directorioArchivos, IEnumerable<IFabricaRes> fabricas)
        {
            _directorioArchivos = directorioArchivos;
            _fabricas = new List<IFabricaRes>(fabricas);
            if (!Directory.Exists(_directorioArchivos))
            {
                Directory.CreateDirectory(_directorioArchivos);
            }
        }

        private string Ruta(string archivo) => Path.Combine(_directorioArchivos, archivo);

        public List<Venta> CargarVentas(IReadOnlyList<Potrero> potreros)
        {
            try
            {
                string rutaArchivo = Ruta("Ventas.txt");
                if (!File.Exists(rutaArchivo)) return new List<Venta>();

                var ventas = new List<Venta>();
                foreach (var linea in File.ReadAllLines(rutaArchivo))
                {
                    if (MapeadorVenta.TryDeLinea(linea, potreros, _fabricas, out var venta))
                    {
                        ventas.Add(venta);
                    }
                }
                return ventas;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar ventas: {ex.Message}");
            }
        }

        public void GuardarVentas(IReadOnlyList<Venta> ventas)
        {
            try
            {
                var lineas = ventas.Select(MapeadorVenta.ALinea).ToList();
                File.WriteAllLines(Ruta("Ventas.txt"), lineas);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar ventas: {ex.Message}", ex);
            }
        }
    }
}
