using Castle.DynamicProxy;
using Bib_Hacienda.Clases;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p_mvcHacienda.Aspectos
{
    //Interceptor que valida autenticación antes de ejecutar operaciones
    public class InterceptorAutenticacion : IInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        //Constructor que recibe el contexto HTTP
        public InterceptorAutenticacion(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Intercept(IInvocation invocation)
        {
            string operacion = invocation.Method.Name; // aplicar_vacuna, vender_res, etc.
            string nombreUsuario = "Desconocido";

            // Intentar obtener el usuario del primer argumento si existe
            if (invocation.Arguments.Length > 0)
            {
                var primerArgumento = invocation.Arguments[0];
                if (primerArgumento is Usuario usuario)
                {
                    nombreUsuario = usuario.Nombre;
                }
            }

            try
            {
                // Guardar contexto en HttpContext.Items para uso posterior
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Items["OperacionSolicitada"] = operacion;
                    _httpContextAccessor.HttpContext.Items["UsuarioActual"] = nombreUsuario;
                }

                // Ejecutar el método original (lanzará la excepción desde Autenticacion)
                // La excepción se propagará al Controller para que la capture
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                // Capturar el resultado de la autorización (exitosa o denegada)
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Items["ResultadoAutenticacion"] = ex.Message;

                    // Determinar si fue exitoso o denegado según el mensaje
                    bool esExitoso = ex.Message.Contains("✓");
                    _httpContextAccessor.HttpContext.Items["AutenticacionExitosa"] = esExitoso;
                    _httpContextAccessor.HttpContext.Items["TipoMensaje"] = esExitoso ? "success" : "error";
                }

                // Re-lanzar para que el Controller maneje la excepción
                throw;
            }
        }
    }
}
