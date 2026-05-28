using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BillarBlackPool.Data;
using BillarBlackPool.Models;

namespace BillarBlackPool.Controllers
{
    [Authorize(Roles = "Administrador,Cajero")]
    public class ReservaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // LISTA RESERVAS
        // ============================================================
        public async Task<IActionResult> Index(string searchString, string estadoFilter, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var reservas = _context.Reservas
                .Include(r => r.Mesa)
                .Include(r => r.Cliente)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                reservas = reservas.Where(r =>
                    r.Cliente.Nombre.Contains(searchString) ||
                    r.Mesa.NumeroMesa.ToString().Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(estadoFilter))
            {
                reservas = reservas.Where(r => r.Estado == estadoFilter);
            }

            if (fechaDesde.HasValue)
            {
                reservas = reservas.Where(r => r.FechaReserva >= fechaDesde.Value.Date);
            }

            if (fechaHasta.HasValue)
            {
                reservas = reservas.Where(r => r.FechaReserva <= fechaHasta.Value.Date);
            }

            var lista = await reservas
                .OrderByDescending(r => r.FechaReserva)
                .ThenBy(r => r.HoraInicio)
                .ToListAsync();

            // Pasar filtros a la vista
            ViewBag.SearchString = searchString;
            ViewBag.EstadoFilter = estadoFilter;
            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");

            return View(lista);
        }

        // ============================================================
        // DETALLES
        // ============================================================
        public async Task<IActionResult> Details(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Mesa)
                .Include(r => r.Cliente)
                .FirstOrDefaultAsync(r => r.IdReserva == id);

            if (reserva == null)
                return NotFound();

            return View(reserva);
        }

        // ============================================================
        // CREATE GET
        // ============================================================
        public async Task<IActionResult> Create()
        {
            await CargarViewBags();

            // Inicializar valores por defecto
            var reserva = new Reserva
            {
                FechaReserva = DateTime.Now.Date,
                Estado = "Pendiente",
                NumeroPersonas = 1
            };

            return View(reserva);
        }

        // ============================================================
        // CREATE POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            // Validaciones personalizadas
            ValidarReserva(reserva);

            if (!ModelState.IsValid)
            {
                await CargarViewBags(reserva.IdMesa, reserva.IdCliente);
                return View(reserva);
            }

            // Asegurar que las fechas de juego están vacías al crear
            reserva.FechaHoraInicioJuego = null;
            reserva.FechaHoraFinJuego = null;

            try
            {
                _context.Add(reserva);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Reserva creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear la reserva: " + ex.Message);
                await CargarViewBags(reserva.IdMesa, reserva.IdCliente);
                return View(reserva);
            }
        }

        // ============================================================
        // EDIT GET
        // ============================================================
        public async Task<IActionResult> Edit(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return NotFound();

            // No permitir editar reservas completadas
            if (reserva.Estado == "Completada")
            {
                TempData["Error"] = "No se pueden editar reservas completadas.";
                return RedirectToAction(nameof(Index));
            }

            await CargarViewBags(reserva.IdMesa, reserva.IdCliente);
            return View(reserva);
        }

        // ============================================================
        // EDIT POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reserva reserva)
        {
            if (id != reserva.IdReserva)
                return NotFound();

            // Obtener la reserva original para mantener fechas de juego
            var reservaOriginal = await _context.Reservas.AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdReserva == id);

            if (reservaOriginal == null)
                return NotFound();

            // Mantener las fechas de juego originales
            reserva.FechaHoraInicioJuego = reservaOriginal.FechaHoraInicioJuego;
            reserva.FechaHoraFinJuego = reservaOriginal.FechaHoraFinJuego;

            ValidarReserva(reserva, id);

            if (!ModelState.IsValid)
            {
                await CargarViewBags(reserva.IdMesa, reserva.IdCliente);
                return View(reserva);
            }

            try
            {
                _context.Update(reserva);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Reserva actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservaExists(reserva.IdReserva))
                    return NotFound();
                else
                    throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar la reserva: " + ex.Message);
                await CargarViewBags(reserva.IdMesa, reserva.IdCliente);
                return View(reserva);
            }
        }

        // ============================================================
        // DELETE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva != null)
            {
                // No permitir eliminar reservas en juego o completadas
                if (reserva.Estado == "En Juego" || reserva.Estado == "Completada")
                {
                    TempData["Error"] = "No se pueden eliminar reservas en juego o completadas.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Reserva eliminada exitosamente.";
            }
            else
            {
                TempData["Error"] = "Reserva no encontrada.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // CAMBIAR ESTADO
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            var permitidos = new[] { "Pendiente", "Confirmada", "En Juego", "Completada", "Cancelada" };
            if (!permitidos.Contains(nuevoEstado))
            {
                TempData["Error"] = "Estado no válido.";
                return RedirectToAction(nameof(Index));
            }

            // Validar transiciones de estado
            if (reserva.Estado == "Completada")
            {
                TempData["Error"] = "No se puede cambiar el estado de una reserva completada.";
                return RedirectToAction(nameof(Index));
            }

            if (reserva.Estado == "Cancelada" && nuevoEstado != "Cancelada")
            {
                TempData["Error"] = "No se puede reactivar una reserva cancelada.";
                return RedirectToAction(nameof(Index));
            }

            // Actualizar fechas según el estado
            if (nuevoEstado == "En Juego" && reserva.FechaHoraInicioJuego == null)
            {
                reserva.FechaHoraInicioJuego = DateTime.Now;
            }

            if (nuevoEstado == "Completada")
            {
                if (reserva.FechaHoraInicioJuego == null)
                {
                    reserva.FechaHoraInicioJuego = DateTime.Now;
                }
                if (reserva.FechaHoraFinJuego == null)
                {
                    reserva.FechaHoraFinJuego = DateTime.Now;
                }
            }

            reserva.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Estado actualizado a '{nuevoEstado}' exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // INICIAR JUEGO
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> IniciarJuego(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (reserva.Estado != "Confirmada")
            {
                TempData["Error"] = "Solo se pueden iniciar reservas confirmadas.";
                return RedirectToAction(nameof(Index));
            }

            reserva.Estado = "En Juego";
            reserva.FechaHoraInicioJuego = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Juego iniciado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // FINALIZAR JUEGO
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> FinalizarJuego(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (reserva.Estado != "En Juego")
            {
                TempData["Error"] = "Solo se pueden finalizar juegos en curso.";
                return RedirectToAction(nameof(Index));
            }

            if (reserva.FechaHoraInicioJuego == null)
            {
                reserva.FechaHoraInicioJuego = DateTime.Now.AddHours(-1);
            }

            reserva.Estado = "Completada";
            reserva.FechaHoraFinJuego = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Juego finalizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // VER DISPONIBILIDAD (AJAX)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> VerDisponibilidad(DateTime fecha, string horaInicio, string horaFin)
        {
            if (!TimeSpan.TryParse(horaInicio, out TimeSpan inicio) ||
                !TimeSpan.TryParse(horaFin, out TimeSpan fin))
            {
                return Json(new { error = "Formato de hora inválido" });
            }

            var mesasOcupadas = await _context.Reservas
                .Where(r => r.FechaReserva.Date == fecha.Date &&
                            r.Estado != "Cancelada" &&
                            r.HoraInicio < fin &&
                            r.HoraFin > inicio)
                .Select(r => r.IdMesa)
                .ToListAsync();

            var mesasDisponibles = await _context.Mesas
                .Where(m => !mesasOcupadas.Contains(m.IdMesa))
                .OrderBy(m => m.NumeroMesa)
                .Select(m => new
                {
                    m.IdMesa,
                    NumeroMesa = m.NumeroMesa,
                    Nombre = "Mesa " + m.NumeroMesa
                })
                .ToListAsync();

            return Json(new
            {
                disponibles = mesasDisponibles,
                totalDisponibles = mesasDisponibles.Count
            });
        }

        // ============================================================
        // VALIDACIONES PERSONALIZADAS
        // ============================================================
        private void ValidarReserva(Reserva reserva, int? idEdit = null)
        {
            // Validar que la hora de fin sea mayor que la de inicio
            if (reserva.HoraFin <= reserva.HoraInicio)
            {
                ModelState.AddModelError("HoraFin", "La hora de fin debe ser mayor que la hora de inicio.");
            }

            // Validar que no se creen reservas en fechas pasadas
            if (idEdit == null && reserva.FechaReserva.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("FechaReserva", "No se pueden crear reservas en fechas pasadas.");
            }

            // Validar que la duración mínima sea de 30 minutos
            if ((reserva.HoraFin - reserva.HoraInicio).TotalMinutes < 30)
            {
                ModelState.AddModelError("HoraFin", "La reserva debe tener una duración mínima de 30 minutos.");
            }

            // Validar conflictos de horario
            var conflicto = _context.Reservas.Any(r =>
                r.IdMesa == reserva.IdMesa &&
                r.IdReserva != idEdit &&
                r.FechaReserva.Date == reserva.FechaReserva.Date &&
                (r.Estado == "Pendiente" || r.Estado == "Confirmada" || r.Estado == "En Juego") &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio);

            if (conflicto)
            {
                ModelState.AddModelError("", "La mesa ya está reservada en ese horario. Por favor, seleccione otro horario o mesa.");
            }
        }

        // ============================================================
        // VIEWBAGS
        // ============================================================
        private async Task CargarViewBags(int? mesa = null, int? cliente = null)
        {
            ViewBag.Mesas = new SelectList(
                await _context.Mesas
                    .OrderBy(m => m.NumeroMesa)
                    .Select(m => new { m.IdMesa, Nombre = "Mesa " + m.NumeroMesa })
                    .ToListAsync(),
                "IdMesa", "Nombre", mesa);

            ViewBag.Clientes = new SelectList(
                await _context.Clientes
                    .OrderBy(c => c.Nombre)
                    .ToListAsync(),
                "IdCliente", "Nombre", cliente);

            ViewBag.Estados = new SelectList(new[]
            {
                new { Value = "Pendiente", Text = "Pendiente" },
                new { Value = "Confirmada", Text = "Confirmada" },
                new { Value = "En Juego", Text = "En Juego" },
                new { Value = "Completada", Text = "Completada" },
                new { Value = "Cancelada", Text = "Cancelada" }
            }, "Value", "Text");
        }

        // ============================================================
        // HELPER
        // ============================================================
        private bool ReservaExists(int id)
        {
            return _context.Reservas.Any(e => e.IdReserva == id);
        }
    }
}