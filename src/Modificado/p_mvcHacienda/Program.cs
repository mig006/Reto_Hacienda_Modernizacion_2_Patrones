using p_mvcHacienda.Composicion;

namespace p_mvcHacienda
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // --- Configuracion de Autenticacion por Cookies ---
            builder.Services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = "HaciendaSoft.Auth";
                    options.LoginPath = "/Account/Login"; // Pagina de login
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Duracion de la sesion
                });

            // Agregar HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // ADR-01, ADR-02 · Todo el grafo de objetos se compone en un único lugar.
            //
            // Antes, este método contenía 50 líneas de registros de clases concretas y la
            // carga de los cinco archivos de datos embebida dentro de una lambda del
            // contenedor (Program.cs:30-87). Ahora es una sola llamada: qué implementa
            // qué se decide en RaizComposicion, y cómo se carga el estado inicial, en
            // CargadorInicial.
            RaizComposicion.Registrar(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // --- Habilitar Autenticacion y Autorizacion ---
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
