using Bib_Hacienda.Clases;
using System.Collections.Generic;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-02 · DI-3 · Contrato de persistencia del catálogo de vacunas disponibles.
    ///
    /// Alto nivel: FabricaVacunas y ServicioVacunacion.
    /// Bajo nivel: RepositorioCatalogoVacunasArchivo (Vacunas.txt).
    /// Composition root: RaizComposicion.Registrar. Ciclo de vida: Singleton.
    ///
    /// Es un agregado distinto del historial de vacunas aplicadas, que pertenece al
    /// potrero: el catálogo es inventario disponible y el historial es registro
    /// sanitario de un animal concreto.
    /// </summary>
    public interface IRepositorioCatalogoVacunas
    {
        List<Vacuna> CargarVacunas();

        void GuardarVacunas(IReadOnlyList<Vacuna> vacunas);
    }
}
