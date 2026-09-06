using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;

namespace p_mvcHacienda.Composicion
{
    /// <summary>
    /// ADR-02 · Carga inicial del estado en memoria al arrancar la aplicación.
    ///
    /// Traducción literal de la fábrica que estaba embebida en Program.cs:33-74. El
    /// orden de carga es el mismo —potreros, reses, vacunas aplicadas, ventas, catálogo—
    /// y también lo son las dos salidas de consola, que son comportamiento observable
    /// (categoría 6 de ADRs.md §8.2).
    ///
    /// El try/catch que escribe en consola y continúa TAMBIÉN se conserva: si la carga
    /// falla, la aplicación arranca con datos incompletos y sin más aviso que esa línea.
    /// Está diagnosticado y es discutible, pero corregirlo cambiaría lo que el operario
    /// ve al arrancar.
    ///
    /// Es la vía por la que la deuda D-4 se hace visible: un Ventas.txt con una fila
    /// incoherente hace que MapeadorRes.ReconstruirRes lance con el literal congelado
    /// del subtipo, y ese texto termina aquí, en la consola.
    /// </summary>
    public static class CargadorInicial
    {
        public static Hacienda Cargar(IServiceProvider sp)
        {
            var hacienda = new Hacienda();

            var repositorioPotreros = sp.GetRequiredService<IRepositorioPotreros>();
            var repositorioVentas = sp.GetRequiredService<IRepositorioVentas>();
            var repositorioCatalogo = sp.GetRequiredService<IRepositorioCatalogoVacunas>();

            // Cargar datos al iniciar
            try
            {
                var potreros = repositorioPotreros.CargarPotreros();
                foreach (var potrero in potreros)
                {
                    hacienda.AgregarPotrero(potrero);
                }

                // Cargar reses en los potreros
                repositorioPotreros.CargarReses(hacienda.L_potreros);

                // Cargar vacunas aplicadas a las reses
                repositorioPotreros.CargarVacunasAplicadas(hacienda.L_potreros);

                var ventas = repositorioVentas.CargarVentas(hacienda.L_potreros);
                foreach (var venta in ventas)
                {
                    hacienda.RegistrarVenta(venta);
                }

                var vacunas = repositorioCatalogo.CargarVacunas();
                foreach (var vacuna in vacunas)
                {
                    hacienda.AgregarVacuna(vacuna);
                }

                Console.WriteLine($"Datos cargados: {potreros.Count} potreros, {ventas.Count} ventas, {vacunas.Count} vacunas");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar datos: {ex.Message}");
            }

            return hacienda;
        }
    }
}
