using System.Collections.Generic;

namespace Bib_Hacienda.Clases
{
    /// <summary>
    /// ADR-04 · Raíz de agregados en memoria.
    ///
    /// ══ De 558 líneas a 30 ═══════════════════════════════════════════════════════
    /// Esta clase reunía SEIS responsabilidades —gestión de potreros, gestión de reses,
    /// proceso de venta, fábrica de vacunas con cuatro sobrecargas duplicadas, reglas de
    /// vacunación y orquestación de eventos—, implementaba tres interfaces a la vez
    /// (Hacienda.cs:16) y era el segundo nodo más acoplado del mapa de dependencias, con
    /// grado 14 (H-03). Ninguna operación del sistema podía completarse sin atravesarla.
    ///
    /// Sus once métodos públicos se repartieron en cinco servicios de dominio, uno por
    /// RAZÓN DE CAMBIO y por INTERLOCUTOR que la solicita:
    ///
    ///   GestorPotreros      · reglas de creación y unicidad  · administrador de la finca
    ///   GestorReses         · reglas de ingreso y engorde    · operario de campo
    ///   ServicioVenta       · reglas comerciales             · área comercial
    ///   FabricaVacunas      · creación de inventario         · veterinario
    ///   ServicioVacunacion  · esquema y límites              · veterinario
    ///
    /// Se evaluaron y descartaron dos fronteras alternativas:
    ///   · POR ENTIDAD (ServicioPotrero, ServicioRes, …): aplicar_vacuna toca Res, Vacuna
    ///     y Potrero a la vez, así que el método no tendría dueño y reaparecería el
    ///     acoplamiento cruzado que se quiere eliminar.
    ///   · POR TAMAÑO (bloques de ~100 líneas): produce fragmentos sin significado de
    ///     negocio y no reduce las razones de cambio, que es el criterio real de SRP.
    ///
    /// ══ Qué queda ═══════════════════════════════════════════════════════════════
    /// Solo las tres colecciones. Hacienda ya no tiene política: tiene ESTADO. Y por eso
    /// no aparece en ningún ADR como "servicio": es el agregado compartido en memoria,
    /// registrado como Singleton, cuyo ciclo de vida no se toca porque cambiarlo sí
    /// alteraría el comportamiento observable.
    ///
    /// Se evaluó y descartó eliminar el tipo repartiendo las tres colecciones entre los
    /// repositorios: máxima separación, pero cambia el ciclo de vida del estado
    /// compartido del que depende la conducta observada. Riesgo alto de alterar salidas.
    /// </summary>
    public class Hacienda
    {
        //Atributos
        private List<Potrero> l_potreros;
        private List<Venta> l_ventas;
        private List<Vacuna> l_vacunas;

        // Vistas de SOLO LECTURA. La raíz de agregados es la ÚNICA que muta sus
        // colecciones, a través de sus métodos de comportamiento. Antes exponía las List
        // mutables y los servicios hacían .Add/.Remove desde fuera.
        public IReadOnlyList<Potrero> L_potreros => l_potreros;
        public IReadOnlyList<Venta> L_ventas => l_ventas;
        public IReadOnlyList<Vacuna> L_vacunas => l_vacunas;

        public void AgregarPotrero(Potrero potrero) => l_potreros.Add(potrero);
        public void RegistrarVenta(Venta venta) => l_ventas.Add(venta);
        public void AgregarVacuna(Vacuna vacuna) => l_vacunas.Add(vacuna);
        public void RemoverVacuna(Vacuna vacuna) => l_vacunas.Remove(vacuna);

        //Constructor vacío
        public Hacienda()
        {
            l_potreros = new List<Potrero>();
            l_ventas = new List<Venta>();
            l_vacunas = new List<Vacuna>();
        }
    }
}
