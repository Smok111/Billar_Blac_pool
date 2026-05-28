using BillarBlackPool.Data;
using BillarBlackPool.Models;
using BillarBlackPool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BillarBlackPool.Controllers
{
    [Authorize]
    public class ConsumoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConsumoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static void RecalcularTotales(Consumo consumo)
        {
            var calculo = BillarTarifas.Calcular(consumo.FechaInicio, consumo.FechaFin);

            consumo.MinutosJugados = calculo.Minutos;
            consumo.PrecioHora = calculo.PrecioHora;
            consumo.PrecioMediaHora = calculo.PrecioMediaHora;
            consumo.PrecioLibrePorMinuto = calculo.PrecioLibrePorMinuto;
            consumo.CostoMesa = calculo.Importe;
            consumo.Total = Math.Round(consumo.CostoMesa + consumo.TotalProductos, 2, MidpointRounding.AwayFromZero);
        }

        private async Task<decimal> CalcularTotalProductosAsync(int idConsumo)
        {
            return await _context.ConsumoDetalles
                .Where(d => d.IdConsumo == idConsumo)
                .SumAsync(d => (decimal?)d.Cantidad * d.PrecioUnitario) ?? 0m;
        }

        private async Task CargarListasAsync(int? selectedMesa = null, int? selectedUsuario = null, int? selectedCliente = null)
        {
            ViewBag.Mesas = await _context.Mesas
                .OrderBy(m => m.NumeroMesa)
                .ToListAsync();

            ViewBag.Usuarios = await _context.Usuarios
                .OrderBy(u => u.NomUsuario)
                .ToListAsync();

            ViewBag.Clientes = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.SelectedMesa = selectedMesa;
            ViewBag.SelectedUsuario = selectedUsuario;
            ViewBag.SelectedCliente = selectedCliente;
        }

        public async Task<IActionResult> Index(string searchString, string estadoFilter)
        {
            var consumos = _context.Consumos
                .Include(c => c.Mesa)
                .Include(c => c.Usuario)
                .Include(c => c.Cliente)
                .Include(c => c.Detalles)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                if (int.TryParse(searchString, out int numeroMesa))
                {
                    consumos = consumos.Where(c => c.Mesa != null && c.Mesa.NumeroMesa == numeroMesa);
                }
                else
                {
                    consumos = consumos.Where(c =>
                        c.Cliente != null && c.Cliente.Nombre.Contains(searchString) ||
                        c.Usuario != null && (c.Usuario.NomUsuario.Contains(searchString) ||
                        c.Usuario.ApeUsuario.Contains(searchString)));
                }
            }

            if (!string.IsNullOrEmpty(estadoFilter))
                consumos = consumos.Where(c => c.Estado == estadoFilter);

            var lista = await consumos.OrderByDescending(c => c.FechaInicio).ToListAsync();

            foreach (var consumo in lista)
            {
                consumo.TotalProductos = await CalcularTotalProductosAsync(consumo.IdConsumo);
                RecalcularTotales(consumo);
            }

            return View(lista);
        }

        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Consumo consumo)
        {
            var mesa = await _context.Mesas.FindAsync(consumo.IdMesa);
            if (mesa == null)
            {
                ModelState.AddModelError("IdMesa", "La mesa seleccionada no existe.");
            }
            else if (mesa.Estado != "Disponible")
            {
                ModelState.AddModelError("IdMesa", "La mesa seleccionada no está disponible.");
            }

            if (!await _context.Usuarios.AnyAsync(u => u.IdUsuario == consumo.IdUsuario))
            {
                ModelState.AddModelError("IdUsuario", "El usuario seleccionado no existe.");
            }

            if (!consumo.IdCliente.HasValue || !await _context.Clientes.AnyAsync(c => c.IdCliente == consumo.IdCliente.Value))
            {
                ModelState.AddModelError("IdCliente", "El cliente seleccionado no existe.");
            }
            else
            {
                var consumoExistente = await _context.Consumos
                    .AnyAsync(c => c.IdCliente == consumo.IdCliente.Value && c.Estado == "Abierto");

                if (consumoExistente)
                {
                    ModelState.AddModelError("IdCliente", "Este cliente ya tiene un consumo abierto. Debe cerrarlo antes de crear uno nuevo.");
                }
            }

            if (string.IsNullOrWhiteSpace(consumo.TipoCobro))
            {
                consumo.TipoCobro = "Libre";
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(consumo.IdMesa, consumo.IdUsuario, consumo.IdCliente);
                return View(consumo);
            }

            consumo.FechaInicio = DateTime.Now;
            consumo.FechaFin = null;
            consumo.Estado = "Abierto";
            consumo.TotalProductos = 0m;
            RecalcularTotales(consumo);

            _context.Add(consumo);

            if (mesa != null)
            {
                mesa.Estado = "Ocupada";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Consumo creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var consumo = await _context.Consumos.FindAsync(id);
            if (consumo == null) return NotFound();

            await CargarListasAsync(consumo.IdMesa, consumo.IdUsuario, consumo.IdCliente);
            return View(consumo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Consumo consumo)
        {
            if (id != consumo.IdConsumo) return NotFound();

            var original = await _context.Consumos.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdConsumo == id);

            if (original == null) return NotFound();

            if (!await _context.Usuarios.AnyAsync(u => u.IdUsuario == consumo.IdUsuario))
            {
                ModelState.AddModelError("IdUsuario", "El usuario seleccionado no existe.");
            }

            if (!consumo.IdCliente.HasValue || !await _context.Clientes.AnyAsync(c => c.IdCliente == consumo.IdCliente.Value))
            {
                ModelState.AddModelError("IdCliente", "El cliente seleccionado no existe.");
            }
            else
            {
                var consumoExistente = await _context.Consumos
                    .AnyAsync(c => c.IdCliente == consumo.IdCliente.Value
                                && c.Estado == "Abierto"
                                && c.IdConsumo != consumo.IdConsumo);

                if (consumoExistente)
                {
                    ModelState.AddModelError("IdCliente", "Este cliente ya tiene otro consumo abierto. Debe cerrarlo antes de asignarle este consumo.");
                }
            }

            var mesaNueva = await _context.Mesas.FindAsync(consumo.IdMesa);
            if (mesaNueva == null)
            {
                ModelState.AddModelError("IdMesa", "La mesa seleccionada no existe.");
            }
            else if (original.IdMesa != consumo.IdMesa && mesaNueva.Estado != "Disponible" && consumo.Estado == "Abierto")
            {
                ModelState.AddModelError("IdMesa", $"La mesa {mesaNueva.NumeroMesa} no está disponible.");
            }

            if (string.IsNullOrWhiteSpace(consumo.TipoCobro))
            {
                consumo.TipoCobro = "Libre";
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(consumo.IdMesa, consumo.IdUsuario, consumo.IdCliente);
                return View(consumo);
            }

            if (original.IdMesa != consumo.IdMesa)
            {
                var mesaAnterior = await _context.Mesas.FindAsync(original.IdMesa);
                if (mesaAnterior != null)
                    mesaAnterior.Estado = "Disponible";

                if (mesaNueva != null && consumo.Estado == "Abierto")
                    mesaNueva.Estado = "Ocupada";
            }

            if (original.Estado == "Abierto" && consumo.Estado == "Cerrado")
            {
                consumo.FechaFin = DateTime.Now;

                var mesaActual = await _context.Mesas.FindAsync(consumo.IdMesa);
                if (mesaActual != null)
                    mesaActual.Estado = "Disponible";
            }
            else if (consumo.Estado == "Abierto")
            {
                consumo.FechaFin = null;
            }

            consumo.TotalProductos = await CalcularTotalProductosAsync(consumo.IdConsumo);
            RecalcularTotales(consumo);

            _context.Update(consumo);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Consumo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var consumo = await _context.Consumos.Include(c => c.Mesa)
                .FirstOrDefaultAsync(c => c.IdConsumo == id);
            if (consumo == null) return NotFound();

            if (consumo.Estado == "Abierto" && consumo.Mesa != null)
                consumo.Mesa.Estado = "Disponible";

            _context.Consumos.Remove(consumo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Consumo eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id)
        {
            var consumo = await _context.Consumos.Include(c => c.Mesa)
                .FirstOrDefaultAsync(c => c.IdConsumo == id);
            if (consumo == null) return NotFound();

            if (consumo.Estado == "Cerrado")
            {
                TempData["Error"] = "El consumo ya está cerrado.";
                return RedirectToAction(nameof(Index));
            }

            consumo.Estado = "Cerrado";
            consumo.FechaFin = DateTime.Now;
            consumo.TotalProductos = await CalcularTotalProductosAsync(consumo.IdConsumo);
            RecalcularTotales(consumo);

            if (consumo.Mesa != null)
                consumo.Mesa.Estado = "Disponible";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Consumo cerrado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}