using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using p_mvcHacienda.Models;

namespace p_mvcHacienda.Controllers
{
    public class AccountController : Controller
    {
        private readonly Servicios.UsuarioService _usuarioService;

        public AccountController(Servicios.UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var (success, claims) = await _usuarioService.ValidateUserAsync(model.Username, model.Password);

                if (success)
                {
                    await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(new ClaimsIdentity(claims, "CookieAuth")));
                    
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
