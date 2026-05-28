using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillarBlackPool.Data;
using BillarBlackPool.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BillarBlackPool.Controllers
{
    public class AccesoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccesoController(ApplicationDbContext context)
        {
            _context = context;
        }

    
        public IActionResult Login()
        {
      
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Mesa");
            }
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                var usuarioEncontrado = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Correo == modelo.Correo && u.Password == modelo.Password);

                if (usuarioEncontrado != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, $"{usuarioEncontrado.NomUsuario} {usuarioEncontrado.ApeUsuario}"),
                        new Claim(ClaimTypes.Role, usuarioEncontrado.Rol.NomRol)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                    });

                    TempData["Success"] = $"¡Bienvenido, {usuarioEncontrado.NomUsuario}!";

                    return RedirectToAction("Index", "Mesa");
                }

                ModelState.AddModelError(string.Empty, "Correo o Contraseña inválidos.");
            }

            return View(modelo);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "¡Sesión cerrada con éxito!";

            return RedirectToAction("Index", "Home");
        }
    }
}