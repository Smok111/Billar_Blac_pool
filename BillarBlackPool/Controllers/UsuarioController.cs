using BillarBlackPool.Data;
using BillarBlackPool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BillarBlackPool.Controllers
{
    [Authorize(Roles = "Administrador")] // Solo el Administrador puede acceder
    public class UsuarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // INDEX - Lista de todos los usuarios
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.NomUsuario)
                .ToListAsync();

            return View(usuarios);
        }

        // ============================================================
        // DETAILS - Ver detalles de un usuario
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // ============================================================
        // CREATE GET - Formulario para crear usuario
        // ============================================================
        public IActionResult Create()
        {
            // Cargar TODOS los roles sin restricciones
            ViewBag.Roles = new SelectList(
                _context.Roles.OrderBy(r => r.NomRol),
                "IdRol",
                "NomRol"
            );
            return View();
        }

        // ============================================================
        // CREATE POST - Guardar nuevo usuario
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            if (!ModelState.IsValid)
            {
                // Recargar todos los roles si hay error
                ViewBag.Roles = new SelectList(
                    _context.Roles.OrderBy(r => r.NomRol),
                    "IdRol",
                    "NomRol"
                );
                return View(usuario);
            }

            // Validar que el rol exista
            var rolSeleccionado = await _context.Roles.FindAsync(usuario.IdRol);
            if (rolSeleccionado == null)
            {
                ModelState.AddModelError("IdRol", "El rol seleccionado no existe.");
                ViewBag.Roles = new SelectList(
                    _context.Roles.OrderBy(r => r.NomRol),
                    "IdRol",
                    "NomRol"
                );
                return View(usuario);
            }

            // Validar que el correo no exista
            if (await _context.Usuarios.AnyAsync(u => u.Correo == usuario.Correo))
            {
                ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                ViewBag.Roles = new SelectList(
                    _context.Roles.OrderBy(r => r.NomRol),
                    "IdRol",
                    "NomRol"
                );
                return View(usuario);
            }

            try
            {
                // IMPORTANTE: En producción, usa BCrypt o similar para hashear la contraseña
                // Por ahora se guarda en texto plano (NO RECOMENDADO)

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Usuario {usuario.NomUsuario} creado correctamente con rol {rolSeleccionado.NomRol}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear usuario: {ex.Message}");
                ViewBag.Roles = new SelectList(
                    _context.Roles.OrderBy(r => r.NomRol),
                    "IdRol",
                    "NomRol"
                );
                return View(usuario);
            }
        }

        // ============================================================
        // EDIT GET - Formulario para editar usuario
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Cargar TODOS los roles (sin restricciones)
            ViewBag.Roles = new SelectList(
                _context.Roles.OrderBy(r => r.NomRol),
                "IdRol",
                "NomRol",
                usuario.IdRol
            );
            return View(usuario);
        }

        // ============================================================
        // EDIT POST - Guardar cambios del usuario
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario)
        {
            if (id != usuario.IdUsuario) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(
                    _context.Roles.OrderBy(r => r.NomRol),
                    "IdRol",
                    "NomRol",
                    usuario.IdRol
                );
                return View(usuario);
            }

            try
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Usuario actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Usuarios.Any(u => u.IdUsuario == usuario.IdUsuario))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                ViewBag.Roles = new SelectList(
                    _context.Roles.OrderBy(r => r.NomRol),
                    "IdRol",
                    "NomRol",
                    usuario.IdRol
                );
                return View(usuario);
            }
        }

        // ============================================================
        // DELETE GET - Confirmación para eliminar
        // ============================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // ============================================================
        // DELETE POST - Eliminar usuario
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null) return NotFound();

            try
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Usuario eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}