using BillarBlackPool.Data;
using BillarBlackPool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillarBlackPool.Controllers
{
    // Solo el Administrador puede gestionar roles
    [Authorize(Roles = "Administrador")]
    public class RolController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RolController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // INDEX
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .Include(r => r.Usuarios)
                .OrderBy(r => r.NomRol)
                .ToListAsync();

            return View(roles);
        }

        // ============================================================
        // DETAILS
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var rol = await _context.Roles
                .Include(r => r.Usuarios)
                .FirstOrDefaultAsync(r => r.IdRol == id);

            if (rol == null) return NotFound();

            return View(rol);
        }

        // ============================================================
        // CREATE GET
        // ============================================================
        public IActionResult Create()
        {
            return View();
        }

        // ============================================================
        // CREATE POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rol rol)
        {
            if (!ModelState.IsValid)
                return View(rol);

            try
            {
                _context.Add(rol);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rol creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear el rol: {ex.Message}");
                return View(rol);
            }
        }

        // ============================================================
        // EDIT GET
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var rol = await _context.Roles.FindAsync(id);
            if (rol == null) return NotFound();

            return View(rol);
        }

        // ============================================================
        // EDIT POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Rol rol)
        {
            if (id != rol.IdRol) return NotFound();

            if (!ModelState.IsValid)
                return View(rol);

            try
            {
                _context.Update(rol);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rol actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Roles.Any(r => r.IdRol == rol.IdRol))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                return View(rol);
            }
        }

        // ============================================================
        // DELETE GET
        // ============================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var rol = await _context.Roles
                .FirstOrDefaultAsync(r => r.IdRol == id);

            if (rol == null) return NotFound();

            return View(rol);
        }

        // ============================================================
        // DELETE POST
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rol = await _context.Roles.FindAsync(id);
            if (rol == null) return NotFound();

            try
            {
                _context.Roles.Remove(rol);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rol eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}