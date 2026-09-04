using Bib_Hacienda.Clases;
using System.Collections.Generic;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-02 · DI-2 · Contrato de persistencia del agregado <see cref="Venta"/>.
    ///
    /// Alto nivel: ServicioVenta. Bajo nivel: RepositorioVentasArchivo (Ventas.txt).
    /// Composition root: RaizComposicion.Registrar. Ciclo de vida: Singleton.
    ///
    /// CargarVentas recibe los potreros ya cargados porque cada venta histórica
    /// referencia un potrero; si no lo encuentra, reconstruye uno suelto. Ese
    /// comportamiento se conserva literalmente (PersistenciaService.cs:428-432).
    /// </summary>
    public interface IRepositorioVentas
    {
        List<Venta> CargarVentas(List<Potrero> potreros);

        void GuardarVentas(List<Venta> ventas);
    }
}
