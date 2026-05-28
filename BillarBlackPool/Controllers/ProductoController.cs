using BillarBlackPool.Data;
using BillarBlackPool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.RegularExpressions;

namespace BillarBlackPool.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductoController(ApplicationDbContext context)
        {
            _context = context;
        }


        private bool ContieneCaracteresEspeciales(string texto)
        {
           
            return !Regex.IsMatch(texto, @"^[a-zA-Z0-9\s]+$");
        }

        // ============================================================
        // INDEX - Lista todos los productos
        // ============================================================
        public async Task<IActionResult> Index(string searchString, int? categoriaFilter)
        {
            var productos = _context.Productos
                .Include(p => p.CategoriaProducto)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                productos = productos.Where(p => p.Nombre.Contains(searchString));

            if (categoriaFilter.HasValue)
                productos = productos.Where(p => p.IdCategoria == categoriaFilter);

            ViewBag.Categorias = await _context.CategoriasProductos
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(await productos.OrderBy(p => p.Nombre).ToListAsync());
        }

        // ============================================================
        // DETAILS - Detalles de un producto
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos
                .Include(p => p.CategoriaProducto)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Consumo)
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // ============================================================
        // CREATE - Formulario de nuevo producto
        // ============================================================
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = new SelectList(
                await _context.CategoriasProductos
                    .OrderBy(c => c.Nombre)
                    .ToListAsync(),
                "IdCategoria",
                "Nombre");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile? ImagenFile)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            else if (ContieneCaracteresEspeciales(producto.Nombre))
                ModelState.AddModelError("Nombre", "El nombre no debe contener caracteres especiales.");

            if (producto.Precio <= 0)
                ModelState.AddModelError("Precio", "El precio debe ser mayor a 0.");

            if (!await _context.CategoriasProductos.AnyAsync(c => c.IdCategoria == producto.IdCategoria))
                ModelState.AddModelError("IdCategoria", "La categoría seleccionada no existe.");

            if (await _context.Productos.AnyAsync(p => p.Nombre == producto.Nombre))
                ModelState.AddModelError("Nombre", "Ya existe un producto con este nombre.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(
                    await _context.CategoriasProductos.ToListAsync(),
                    "IdCategoria",
                    "Nombre");
                return View(producto);
            }

            // Guardar imagen
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImagenFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/productos", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ImagenFile.CopyToAsync(stream);

                producto.ImagenUrl = "/images/productos/" + fileName;
            }

            try
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                ViewBag.Categorias = new SelectList(
                    await _context.CategoriasProductos.ToListAsync(),
                    "IdCategoria",
                    "Nombre");
                return View(producto);
            }
        }

        // ============================================================
        // EDIT - Formulario de edición
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            ViewBag.Categorias = new SelectList(
                await _context.CategoriasProductos.OrderBy(c => c.Nombre).ToListAsync(),
                "IdCategoria",
                "Nombre",
                producto.IdCategoria);

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile? ImagenFile)
        {
            if (id != producto.IdProducto)
                return NotFound();

            // Validaciones
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            else if (ContieneCaracteresEspeciales(producto.Nombre))
                ModelState.AddModelError("Nombre", "El nombre no debe contener caracteres especiales.");

            if (producto.Precio <= 0)
                ModelState.AddModelError("Precio", "El precio debe ser mayor a 0.");

            if (!await _context.CategoriasProductos.AnyAsync(c => c.IdCategoria == producto.IdCategoria))
                ModelState.AddModelError("IdCategoria", "La categoría seleccionada no existe.");

            if (await _context.Productos.AnyAsync(p => p.Nombre == producto.Nombre && p.IdProducto != id))
                ModelState.AddModelError("Nombre", "Ya existe otro producto con este nombre.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(
                    await _context.CategoriasProductos.ToListAsync(),
                    "IdCategoria",
                    "Nombre",
                    producto.IdCategoria);
                return View(producto);
            }

            // Guardar nueva imagen si se sube
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImagenFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/productos", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ImagenFile.CopyToAsync(stream);

                producto.ImagenUrl = "/images/productos/" + fileName;
            }

            try
            {
                _context.Update(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(producto.IdProducto))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                ViewBag.Categorias = new SelectList(
                    await _context.CategoriasProductos.ToListAsync(),
                    "IdCategoria",
                    "Nombre",
                    producto.IdCategoria);
                return View(producto);
            }
        }

        // ============================================================
        // DELETE - Eliminar producto
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto == null)
                return NotFound();

            if (producto.Detalles.Any())
            {
                TempData["Error"] = "No se puede eliminar el producto porque está en consumos registrados.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // HELPER - Verificar existencia
        // ============================================================
        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.IdProducto == id);
        }
    }
}