using Bib_Hacienda.Clases;
using System;
using static Bib_Hacienda.Clases.Viva;

namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// ADR-13 · Factory Method (P-01, Actividad 2) · Paquete de datos con el que se
    /// puede pedir la creación de CUALQUIER vacuna.
    ///
    /// Antes, "qué tipo es esto" se adivinaba mirando cuál de los dos parámetros
    /// opcionales llegaba con valor (VacunaService.cs:49). Esa inferencia es justo lo
    /// que <see cref="Bib_Hacienda.Contratos.IFabricaVacuna"/> reemplaza: el tipo ahora
    /// se declara explícitamente (la clave del registro), y esta clase solo transporta
    /// los datos. Cada fábrica concreta toma de aquí el campo que le corresponde
    /// (<see cref="PeriodoAplicacion"/> o <see cref="Atenuacion"/>) y valida su ausencia
    /// con el mismo mensaje congelado de siempre.
    /// </summary>
    public sealed class SolicitudVacuna
    {
        public SolicitudVacuna(string nombre, string lote, DateTime fechaVencimiento, DateTime fechaAplicacion,
            uint? periodoAplicacion, enum_l_atenuaciones? atenuacion)
        {
            Nombre = nombre;
            Lote = lote;
            FechaVencimiento = fechaVencimiento;
            FechaAplicacion = fechaAplicacion;
            PeriodoAplicacion = periodoAplicacion;
            Atenuacion = atenuacion;
        }

        public string Nombre { get; }
        public string Lote { get; }
        public DateTime FechaVencimiento { get; }
        public DateTime FechaAplicacion { get; }

        /// <summary>Específico de Bacteriana. Null si la solicitud es de otro tipo.</summary>
        public uint? PeriodoAplicacion { get; }

        /// <summary>Específico de Viva. Null si la solicitud es de otro tipo.</summary>
        public enum_l_atenuaciones? Atenuacion { get; }

        /// <summary>Copia con otro lote — la usa el bucle de creación por lotes de FabricaVacunas.</summary>
        public SolicitudVacuna ConLote(string loteNumerado) =>
            new SolicitudVacuna(Nombre, loteNumerado, FechaVencimiento, FechaAplicacion, PeriodoAplicacion, Atenuacion);
    }
}
