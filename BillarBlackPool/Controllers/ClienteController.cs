using BillarBlackPool.Data;
using BillarBlackPool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillarBlackPool.Controllers
{
    [Authorize(Roles = "Administrador,Cajero")]
    public class ClienteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClienteController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var clientes = _context.Clientes.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                clientes = clientes.Where(c =>
                    c.Nombre.Contains(searchString) ||
                    c.Telefono.Contains(searchString) ||
                    (c.Email != null && c.Email.Contains(searchString)));
            }

            return View(await clientes
                .OrderBy(c => c.Nombre)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var cliente = await _context.Clientes.Include(c => c.Reservas)
                    .ThenInclude(r => r.Mesa)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }


        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            // Validaciones personalizadas
            if (string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            }

            // Validar que el teléfono empiece con 9
            if (string.IsNullOrWhiteSpace(cliente.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono es obligatorio.");
            }
            else if (!cliente.Telefono.StartsWith("9"))
            {
                ModelState.AddModelError("Telefono", "El teléfono debe empezar con el número 9.");
            }

            // Verificar si el email ya existe (si se proporcionó)
            if (!string.IsNullOrWhiteSpace(cliente.Email))
            {
                if (await _context.Clientes.AnyAsync(c => c.Email == cliente.Email))
                {
                    ModelState.AddModelError("Email", "Ya existe un cliente con este email.");
                }
            }

            if (!ModelState.IsValid)
                return View(cliente);

            try
            {
                cliente.FechaRegistro = DateTime.UtcNow;
                _context.Add(cliente);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cliente registrado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                return View(cliente);
            }
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cliente cliente)
        {
            if (id != cliente.IdCliente)
                return NotFound();

            
            if (string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(cliente.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono es obligatorio.");
            }
            else if (!cliente.Telefono.StartsWith("9"))
            {
                ModelState.AddModelError("Telefono", "El teléfono debe empezar con el número 9.");
            }

           
            if (!string.IsNullOrWhiteSpace(cliente.Email))
            {
                if (await _context.Clientes
                    .AnyAsync(c => c.Email == cliente.Email && c.IdCliente != id))
                {
                    ModelState.AddModelError("Email", "Ya existe otro cliente con este email.");
                }
            }

            if (!ModelState.IsValid)
                return View(cliente);

            try
            {
                
                var clienteOriginal = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdCliente == id);

                if (clienteOriginal != null)
                    cliente.FechaRegistro = clienteOriginal.FechaRegistro;

                _context.Update(cliente);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cliente actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteExists(cliente.IdCliente))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                return View(cliente);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null)
                return NotFound();

            
            if (cliente.Reservas != null && cliente.Reservas.Any())
            {
                TempData["Error"] = "No se puede eliminar el cliente porque tiene reservas registradas.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cliente eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

 
        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.IdCliente == id);
        }
    }
}