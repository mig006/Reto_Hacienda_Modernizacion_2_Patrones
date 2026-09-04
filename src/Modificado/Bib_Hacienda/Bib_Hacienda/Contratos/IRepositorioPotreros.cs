using Bib_Hacienda.Clases;
using System.Collections.Generic;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-02 · DI-1 · Contrato de persistencia del agregado <see cref="Potrero"/>.
    ///
    /// Módulo de alto nivel: GestorPotreros y GestorReses (reglas ganaderas).
    /// Módulo de bajo nivel: RepositorioPotrerosArchivo (E/S sobre Potreros.txt,
    /// Reses.txt y VacunasAplicadas.txt).
    /// Abstracción que los desacopla: esta interfaz, declarada en el dominio.
    /// Composition root: RaizComposicion.Registrar. Ciclo de vida: Singleton.
    ///
    /// El potrero es la RAÍZ DEL AGREGADO: arrastra sus reses y sus vacunas aplicadas.
    /// Esa frontera no es arbitraria: es exactamente como el sistema ya cargaba los
    /// datos al arrancar (Program.cs:41-51). La alternativa evaluada y descartada era
    /// un IRepositorio&lt;T&gt; genérico único, que habría obligado a métodos que
    /// algunos implementadores no podrían cumplir — el mismo error de IValidarInformacion.
    ///
    /// La interfaz declara métodos separados por archivo porque el sistema los invoca
    /// por separado y en un orden que es comportamiento observable (ADR-02: el orden
    /// de las cuatro escrituras de VacunaService.cs:83-86 se conserva).
    /// </summary>
    public interface IRepositorioPotreros
    {
        List<Potrero> CargarPotreros();

        /// <summary>Rehidrata las reses dentro de los potreros recibidos.</summary>
        void CargarReses(List<Potrero> potreros);

        /// <summary>Rehidrata las vacunas aplicadas dentro de las reses de esos potreros.</summary>
        void CargarVacunasAplicadas(List<Potrero> potreros);

        void GuardarPotreros(List<Potrero> potreros);

        void GuardarReses(List<Potrero> potreros);

        void GuardarVacunasAplicadas(List<Potrero> potreros);
    }
}
