using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System;
using System.Collections.Generic;
using System.Linq;
using static Bib_Hacienda.Clases.Viva;

namespace Bib_Hacienda.Servicios
{
    /// <summary>
    /// ADR-04 · ADR-13 (Factory Method, P-01, Actividad 2) · Reglas de creación de
    /// inventario sanitario.
    ///
    /// Razón de cambio que la gobierna: la política de inventario de vacunas.
    /// Interlocutor que la solicita: el veterinario.
    ///
    /// ══ Qué cambió respecto al Reto 1 ═══════════════════════════════════════════
    /// Los cuatro métodos fijos (CrearBacteriana, CrearViva, CrearLoteBacteriano,
    /// CrearLoteVivo) se reemplazan por dos métodos ÚNICOS —Crear y CrearLote— que
    /// reciben el tipo como dato y lo resuelven contra un registro de
    /// <see cref="IFabricaVacuna"/>, exactamente como <c>RaizComposicion</c> ya resuelve
    /// <see cref="IFabricaRes"/>. Un tercer tipo de vacuna deja de tocar esta clase: solo
    /// exige una implementación nueva de IFabricaVacuna y una línea en la raíz de
    /// composición.
    ///
    /// ══ Lo que sigue siendo responsabilidad de ESTA clase ════════════════════════
    /// La validación común (nombre, lote, fechas, no-duplicado, cantidad) y la mutación
    /// del inventario de Hacienda. Eso NO varía por tipo, así que no pertenece a las
    /// fábricas concretas — vive aquí, escrito una sola vez, igual que en el Reto 1.
    /// Lo que SÍ varía por tipo —construir el objeto y redactar el mensaje— se delegó.
    ///
    /// ══ El "else" que se conserva a propósito ════════════════════════════════════
    /// VacunaController.cs solo ofrece dos valores de tipoVacuna ("Bacteriana"/"Viva"),
    /// pero el if/else original (:95) trataba CUALQUIER valor distinto de "Bacteriana"
    /// como Viva, sin comparar contra "Viva" explícitamente. ObtenerFabrica reproduce
    /// ese mismo comportamiento por defecto: si el tipo pedido no está registrado, cae
    /// en la fábrica de Viva. No es un descuido — es la regla original, documentada en
    /// vez de repetida en tres sitios.
    /// </summary>
    public class FabricaVacunas
    {
        private readonly Hacienda _hacienda;
        private readonly IReadOnlyDictionary<string, IFabricaVacuna> _fabricasPorTipo;
        private readonly IFabricaVacuna _fabricaPorDefecto;

        public FabricaVacunas(Hacienda hacienda, IEnumerable<IFabricaVacuna> fabricas)
        {
            _hacienda = hacienda;

            var lista = fabricas as IReadOnlyCollection<IFabricaVacuna> ?? fabricas.ToList();
            var registro = lista.ToDictionary(f => f.Tipo, f => f, StringComparer.Ordinal);
            _fabricasPorTipo = registro;

            // Ver "el else que se conserva a propósito" en la documentación de la clase.
            _fabricaPorDefecto = registro.TryGetValue("Viva", out var viva) ? viva : lista.First();
        }

        /// <summary>Crea y añade al inventario una vacuna individual del tipo indicado.</summary>
        public string Crear(string tipoVacuna, string nombre, string lote, DateTime fecha_vencimiento,
            DateTime fecha_aplicacion, uint? periodo_aplicacion, enum_l_atenuaciones? grado_atenuacion)
        {
            var fabrica = ObtenerFabrica(tipoVacuna);
            try
            {
                ValidarNombre(nombre);

                if (string.IsNullOrWhiteSpace(lote))
                    throw new ArgumentException("El lote de la vacuna no puede estar vacío", nameof(lote));

                ValidarFechas(fecha_vencimiento, fecha_aplicacion);
                ValidarLoteNoDuplicado(lote);

                var solicitud = new SolicitudVacuna(nombre, lote, fecha_vencimiento, fecha_aplicacion,
                    periodo_aplicacion, grado_atenuacion);

                Vacuna nueva_vacuna = fabrica.Crear(solicitud);
                _hacienda.L_vacunas.Add(nueva_vacuna);

                return fabrica.MensajeIndividual(solicitud);
            }
            catch (Exception er)
            {
                throw new Exception($"Error inesperado en el método crear_vacuna ({fabrica.EtiquetaErrorIndividual}): " + er.Message);
            }
        }

        /// <summary>Crea un lote de vacunas numeradas del tipo indicado.</summary>
        public string CrearLote(string tipoVacuna, string nombre, string lote_base, DateTime fecha_vencimiento,
            DateTime fecha_aplicacion, uint? periodo_aplicacion, enum_l_atenuaciones? grado_atenuacion, uint cantidad)
        {
            var fabrica = ObtenerFabrica(tipoVacuna);
            try
            {
                ValidarCantidad(cantidad);
                ValidarNombre(nombre);

                if (string.IsNullOrWhiteSpace(lote_base))
                    throw new ArgumentException("El lote base no puede estar vacío", nameof(lote_base));

                ValidarFechas(fecha_vencimiento, fecha_aplicacion);

                var solicitudBase = new SolicitudVacuna(nombre, lote_base, fecha_vencimiento, fecha_aplicacion,
                    periodo_aplicacion, grado_atenuacion);

                int vacunas_creadas = CrearLoteInterno(solicitudBase, cantidad, fabrica);

                return fabrica.MensajeLote(solicitudBase, vacunas_creadas, cantidad);
            }
            catch (Exception er)
            {
                throw new Exception($"Error inesperado en el método crear_vacuna ({fabrica.EtiquetaErrorLote}): " + er.Message);
            }
        }

        private IFabricaVacuna ObtenerFabrica(string tipoVacuna) =>
            _fabricasPorTipo.TryGetValue(tipoVacuna ?? string.Empty, out var fabrica) ? fabrica : _fabricaPorDefecto;

        // ── Bloque de validación común, escrito una sola vez ─────────────────────

        private static void ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la vacuna no puede estar vacío", nameof(nombre));
        }

        private static void ValidarFechas(DateTime fecha_vencimiento, DateTime fecha_aplicacion)
        {
            if (fecha_vencimiento <= fecha_aplicacion)
                throw new Exception("La fecha de vencimiento debe ser posterior a la fecha de aplicación");
        }

        private static void ValidarCantidad(uint cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a 0", nameof(cantidad));

            if (cantidad > 100)
                throw new ArgumentException("No se pueden crear más de 100 vacunas en un solo lote", nameof(cantidad));
        }

        private void ValidarLoteNoDuplicado(string lote)
        {
            if (_hacienda.L_vacunas.Any(v => v.Lote.Equals(lote, StringComparison.OrdinalIgnoreCase)))
                throw new Exception($"Ya existe una vacuna con el lote '{lote}' en el inventario");
        }

        /// <summary>
        /// Bucle de creación por lotes, común a cualquier tipo. Los lotes ya existentes
        /// se saltan EN SILENCIO, igual que en el Reto 1, y si no se creó ninguna se lanza.
        /// </summary>
        private int CrearLoteInterno(SolicitudVacuna solicitudBase, uint cantidad, IFabricaVacuna fabrica)
        {
            int vacunas_creadas = 0;

            for (int i = 1; i <= cantidad; i++)
            {
                string lote_numerado = $"{solicitudBase.Lote}-{i:D3}";

                if (_hacienda.L_vacunas.Any(v => v.Lote.Equals(lote_numerado, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _hacienda.L_vacunas.Add(fabrica.Crear(solicitudBase.ConLote(lote_numerado)));
                vacunas_creadas++;
            }

            if (vacunas_creadas == 0)
                throw new Exception($"No se pudo crear ninguna vacuna. Todos los lotes ya existen en el inventario");

            return vacunas_creadas;
        }
    }
}
