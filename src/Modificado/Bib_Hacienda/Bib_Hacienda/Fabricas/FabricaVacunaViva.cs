using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System;
using static Bib_Hacienda.Clases.Viva;

namespace Bib_Hacienda.Fabricas
{
    /// <summary>
    /// ADR-13 · Factory Method — produce vacunas <see cref="Viva"/>.
    /// Sin estado, sellada y determinista, igual que las fábricas de res.
    /// </summary>
    public sealed class FabricaVacunaViva : IFabricaVacuna
    {
        public string Tipo => "Viva";

        public Type TipoSoportado => typeof(Viva);

        public string EtiquetaErrorIndividual => "viva";

        public string EtiquetaErrorLote => "lote vivo";

        public Vacuna Crear(SolicitudVacuna solicitud)
        {
            if (!solicitud.Atenuacion.HasValue)
                throw new ArgumentException("Falta el grado de atenuación para crear una vacuna viva", nameof(solicitud));

            return new Viva(solicitud.Nombre, solicitud.Lote, solicitud.FechaVencimiento,
                solicitud.FechaAplicacion, solicitud.Atenuacion.Value);
        }

        public string MensajeIndividual(SolicitudVacuna solicitud) =>
            $"Vacuna viva '{solicitud.Nombre}' del lote '{solicitud.Lote}' agregada al inventario con éxito. " +
            $"Grado de atenuación: {(int)solicitud.Atenuacion}.";

        public string MensajeLote(SolicitudVacuna solicitudBase, int creadas, uint cantidad) =>
            $"Lote de vacunas vivas creado con éxito:\n" +
            $"- Nombre: {solicitudBase.Nombre}\n" +
            $"- Cantidad creada: {creadas} de {cantidad}\n" +
            $"- Lotes: {solicitudBase.Lote}-001 a {solicitudBase.Lote}-{creadas:D3}\n" +
            $"- Grado de atenuación: {(int)solicitudBase.Atenuacion}";
    }
}
