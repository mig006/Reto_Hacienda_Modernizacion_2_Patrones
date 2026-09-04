using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;
using System;

namespace Bib_Hacienda.Fabricas
{
    /// <summary>
    /// ADR-13 · Factory Method — produce vacunas <see cref="Bacteriana"/>.
    /// Sin estado, sellada y determinista, igual que las fábricas de res.
    /// </summary>
    public sealed class FabricaVacunaBacteriana : IFabricaVacuna
    {
        public string Tipo => "Bacteriana";

        public Type TipoSoportado => typeof(Bacteriana);

        public string EtiquetaErrorIndividual => "bacteriana";

        public string EtiquetaErrorLote => "lote bacteriano";

        public Vacuna Crear(SolicitudVacuna solicitud)
        {
            if (!solicitud.PeriodoAplicacion.HasValue)
                throw new ArgumentException("Falta el período de aplicación para crear una vacuna bacteriana", nameof(solicitud));

            return new Bacteriana(solicitud.Nombre, solicitud.Lote, solicitud.FechaVencimiento,
                solicitud.FechaAplicacion, solicitud.PeriodoAplicacion.Value);
        }

        public string MensajeIndividual(SolicitudVacuna solicitud) =>
            $"Vacuna bacteriana '{solicitud.Nombre}' del lote '{solicitud.Lote}' agregada al inventario con éxito. " +
            $"Período de aplicación: {solicitud.PeriodoAplicacion} semanas.";

        /// <summary>
        /// DEUDA D-5 · La segunda línea NO lleva el prefijo $, de modo que la pantalla
        /// muestra literalmente "- Nombre: {nombre}". Traducción literal de
        /// FabricaVacunas.CrearLoteBacteriano original (ver ADRs.md §8.3): al mover el
        /// mensaje aquí hay que escribirlo roto a propósito, porque corregirlo es un
        /// cambio de comportamiento observable no autorizado.
        /// </summary>
        public string MensajeLote(SolicitudVacuna solicitudBase, int creadas, uint cantidad) =>
            $"Lote de vacunas bacterianas creado con éxito:\n" +
            "- Nombre: {nombre}\n" +
            $"- Cantidad creada: {creadas} de {cantidad}\n" +
            $"- Lotes: {solicitudBase.Lote}-001 a {solicitudBase.Lote}-{creadas:D3}\n" +
            $"- Período de aplicación: {solicitudBase.PeriodoAplicacion} semanas";
    }
}
