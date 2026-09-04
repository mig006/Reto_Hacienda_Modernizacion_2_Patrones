using Bib_Hacienda.Valores;

namespace Bib_Hacienda.Contratos
{
    /// <summary>
    /// ADR-03 · Contrato de validación segregado por el tipo validado.
    ///
    /// Sustituye a <c>IValidarInformacion</c> y a la clase abstracta <c>Validacion</c>,
    /// que declaraban cuatro métodos de los que ningún implementador cumplía más de
    /// uno: doce excepciones de método no implementado en total (H-01), más el bloque
    /// catch de InterceptorValidarInformacion.cs:58-69 escrito para absorberlas.
    ///
    /// La segregación es real: quien pide <c>IValidador&lt;Res&gt;</c> no ve nada de
    /// <c>Venta</c>. Y ningún implementador puede quedarse con un método sin implementar,
    /// porque el contrato ya no declara métodos que no le corresponden.
    ///
    /// Se eligió el genérico frente a cuatro interfaces con nombre propio
    /// (IValidadorDeRes, IValidadorDePotrero, …): segregan igual de bien y evitan
    /// cuatro declaraciones idénticas salvo el tipo. El costo aceptado es que el
    /// diagrama UML debe representar el parámetro de tipo.
    /// </summary>
    public interface IValidador<T>
    {
        ResultadoValidacion Validar(T entidad);
    }
}
