using BillarBlackPool.Data;
using BillarBlackPool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillarBlackPool.Controllers
{
    [Authorize]
    public class MesaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MesaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Index
        public async Task<IActionResult> Index(string searchString, string estadoFilter)
        {
            var mesas = _context.Mesas
                .Include(m => m.Consumos.Where(c => c.Estado == "Abierto"))
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                if (int.TryParse(searchString, out int numeroMesa))
                {
                    mesas = mesas.Where(m => m.NumeroMesa == numeroMesa);
                }
            }

            if (!string.IsNullOrEmpty(estadoFilter))
            {
                mesas = mesas.Where(m => m.Estado == estadoFilter);
            }

            return View(await mesas.OrderBy(m => m.NumeroMesa).ToListAsync());
        }

        // GET: Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var mesa = await _context.Mesas
                .Include(m => m.Consumos)
                    .ThenInclude(c => c.Usuario)
                .Include(m => m.Consumos)
                    .ThenInclude(c => c.Cliente)
                .Include(m => m.Reservas.OrderByDescending(r => r.FechaReserva).Take(10))
                    .ThenInclude(r => r.Cliente)
                .FirstOrDefaultAsync(m => m.IdMesa == id);

            if (mesa == null) return NotFound();

            return View(mesa);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Mesa mesa)
        {
            // ============ DEBUG ============
            Console.WriteLine("=== INICIO CREATE MESA ===");
            Console.WriteLine($"NumeroMesa recibido: {mesa?.NumeroMesa}");
            Console.WriteLine($"Estado recibido: {mesa?.Estado}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== ERRORES DE VALIDACIÓN ===");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
            }
            // ============================

            if (mesa.NumeroMesa <= 0)
            {
                ModelState.AddModelError("NumeroMesa", "El número de mesa debe ser mayor a 0.");
            }

            if (await _context.Mesas.AnyAsync(m => m.NumeroMesa == mesa.NumeroMesa))
            {
                ModelState.AddModelError("NumeroMesa", "Ya existe una mesa con este número.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== RETORNANDO VISTA CON ERRORES ===");
                return View(mesa);
            }

            try
            {
                mesa.Estado = "Disponible";
                _context.Add(mesa);
                await _context.SaveChangesAsync();

                Console.WriteLine($"=== MESA GUARDADA EXITOSAMENTE - ID: {mesa.IdMesa} ===");
                TempData["Success"] = "Mesa creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR AL GUARDAR: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                return View(mesa);
            }
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var mesa = await _context.Mesas.FindAsync(id);
            if (mesa == null) return NotFound();

            return View(mesa);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Mesa mesa)
        {
            // ============ DEBUG ============
            Console.WriteLine("=== INICIO EDIT MESA ===");
            Console.WriteLine($"ID: {id}");
            Console.WriteLine($"NumeroMesa: {mesa?.NumeroMesa}");
            Console.WriteLine($"Estado: {mesa?.Estado}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            // ============================

            if (id != mesa.IdMesa) return NotFound();

            if (mesa.NumeroMesa <= 0)
            {
                ModelState.AddModelError("NumeroMesa", "El número de mesa debe ser mayor a 0.");
            }

            if (await _context.Mesas.AnyAsync(m => m.NumeroMesa == mesa.NumeroMesa && m.IdMesa != id))
            {
                ModelState.AddModelError("NumeroMesa", "Ya existe otra mesa con este número.");
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                return View(mesa);
            }

            try
            {
                _context.Update(mesa);
                await _context.SaveChangesAsync();

                Console.WriteLine("=== MESA ACTUALIZADA EXITOSAMENTE ===");
                TempData["Success"] = "Mesa actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MesaExists(mesa.IdMesa))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR: {ex.Message} ===");
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                return View(mesa);
            }
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var mesa = await _context.Mesas
                .Include(m => m.Consumos)
                .Include(m => m.Reservas)
                .FirstOrDefaultAsync(m => m.IdMesa == id);

            if (mesa == null) return NotFound();

            if (mesa.Consumos.Any() || mesa.Reservas.Any())
            {
                TempData["Error"] = "No se puede eliminar la mesa porque tiene consumos o reservas registradas.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Mesas.Remove(mesa);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mesa eliminada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cambiar Estado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var mesa = await _context.Mesas.FindAsync(id);
            if (mesa == null) return NotFound();

            mesa.Estado = nuevoEstado;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Estado de la mesa cambiado a {nuevoEstado}.";
            return RedirectToAction(nameof(Index));
        }

        private bool MesaExists(int id)
        {
            return _context.Mesas.Any(e => e.IdMesa == id);
        }
    }
}