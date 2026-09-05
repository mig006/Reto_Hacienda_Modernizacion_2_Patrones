using Bib_Hacienda.Clases;

namespace Bib_Hacienda.Valores
{
    /// <summary>
    /// Observer (Actividad 2 · P-03) · Lo que un <see cref="Contratos.IPublicadorEvento"/>
    /// puede necesitar para decidir si avisa y qué avisa.
    ///
    /// Es un contexto ÚNICO y compartido, no uno por publicador: quien da de alta una res
    /// construye un solo <c>ContextoAviso</c> con los cinco campos y se lo pasa a los
    /// cuatro avisos registrados; quien alimenta solo llena <see cref="Res"/> y aun así
    /// se lo pasa al mismo tipo de contexto. Cada publicador lee únicamente los campos
    /// que le corresponden e ignora el resto.
    ///
    /// Ese es el costo que la Actividad 2 declara al adoptar el patrón: un objeto que
    /// casi ningún consumidor llena por completo, a cambio de una única interfaz para
    /// los cinco avisos en vez de cinco firmas distintas.
    /// </summary>
    public sealed class ContextoAviso
    {
        public Potrero Potrero { get; init; }
        public ushort CantidadReses { get; init; }
        public Res Res { get; init; }
        public ushort ContadorBacterianas { get; init; }
        public ushort ContadorVivas { get; init; }
    }
}
