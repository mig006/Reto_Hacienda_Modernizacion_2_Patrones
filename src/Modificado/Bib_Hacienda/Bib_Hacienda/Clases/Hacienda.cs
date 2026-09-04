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

        //Accesores públicos para los servicios (get público, set privado)
        public List<Potrero> L_potreros
        {
            get => l_potreros;
            private set => l_potreros = value;
        }

        public List<Venta> L_ventas
        {
            get => l_ventas;
            private set => l_ventas = value;
        }

        public List<Vacuna> L_vacunas
        {
            get => l_vacunas;
            private set => l_vacunas = value;
        }

        //Constructor vacío
        public Hacienda()
        {
            l_potreros = new List<Potrero>();
            l_ventas = new List<Venta>();
            l_vacunas = new List<Vacuna>();
        }
    }
}
