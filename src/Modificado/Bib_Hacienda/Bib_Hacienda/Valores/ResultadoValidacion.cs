using System;

namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// ADR-03 · Resultado de validar una entidad.
    ///
    /// Sustituye a <c>HttpContext.Items["ResultadoValidacion"]</c> (H-14). Antes, el
    /// resultado de la validación se depositaba en un diccionario global desde
    /// InterceptorValidarInformacion.cs:48,53 y se leía en once puntos distintos de
    /// PersistenciaService. Fuera de una petición HTTP ese diccionario era nulo y el
    /// dato se perdía sin aviso.
    ///
    /// Ahora viaja por el valor de retorno: quien valida se entera de si validó.
    ///
    /// CONTRATO DE TEXTO CONGELADO (ADR-02). Los dos mensajes son exactamente los que
    /// producía el interceptor, porque terminan en la pantalla del operario
    /// concatenados por los servicios como $"{resultado}. {validado}".
    /// </summary>
    public sealed class ResultadoValidacion
    {
        /// <summary>Literal de InterceptorValidarInformacion.cs:48. No se cambia.</summary>
        public const string TextoValido = "Datos válidos. Guardado exitoso en BD";

        /// <summary>Literal de InterceptorValidarInformacion.cs:53. No se cambia.</summary>
        public const string TextoInvalido = "Datos inválidos. NO se guardó en BD";

        private ResultadoValidacion(bool esValido, string mensaje)
        {
            EsValido = esValido;
            Mensaje = mensaje;
        }

        public bool EsValido { get; }

        public string Mensaje { get; }

        public static ResultadoValidacion Valido() => new ResultadoValidacion(true, TextoValido);

        public static ResultadoValidacion Invalido() => new ResultadoValidacion(false, TextoInvalido);
    }
}
