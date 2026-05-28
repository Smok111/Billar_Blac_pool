using BillarBlackPool.Data;
using BillarBlackPool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillarBlackPool.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CategoriaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var categorias = _context.CategoriasProductos
                .Include(c => c.Productos)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                categorias = categorias.Where(c =>
                    c.Nombre.Contains(searchString));
            }

            return View(await categorias
                .OrderBy(c => c.Nombre)
                .ToListAsync());
        }

        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.CategoriasProductos
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

     
        public IActionResult Create()
        {
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoriaProducto categoria)
        {
           
            Console.WriteLine($"Nombre recibido: {categoria?.Nombre}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            if (string.IsNullOrWhiteSpace(categoria?.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            }

            if (!ModelState.IsValid)
            {
                
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                return View(categoria);
            }

            _context.Add(categoria);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Categoría guardada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

    
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.CategoriasProductos.FindAsync(id);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoriaProducto categoria)
        {
            if (id != categoria.IdCategoria)
                return NotFound();

            if (!ModelState.IsValid)
                return View(categoria);

            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Categoría actualizada exitosamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExists(categoria.IdCategoria))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _context.CategoriasProductos
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null)
                return NotFound();

            if (categoria.Productos.Any())
            {
                TempData["Error"] = "No se puede eliminar esta categoría porque tiene productos asignados.";
                return RedirectToAction(nameof(Index));
            }

            _context.CategoriasProductos.Remove(categoria);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Categoría eliminada exitosamente.";

            return RedirectToAction(nameof(Index));
        }

        private bool CategoriaExists(int id)
        {
            return _context.CategoriasProductos.Any(e => e.IdCategoria == id);
        }
    }
}
