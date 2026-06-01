using BillarBlackPool.Data;
using BillarBlackPool.Models;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using System.IO;
using System.Linq;

namespace BillarBlackPool.Controllers
{
    [Authorize(Roles = "Administrador,Cajero")]
    public class ConsumoDetalleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConsumoDetalleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string buscar)
        {
            var query = _context.ConsumoDetalles
                .Include(d => d.Producto)
                .Include(d => d.Consumo)
                    .ThenInclude(c => c.Mesa)
                .OrderByDescending(d => d.FechaRegistro)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(d =>
                    d.Producto.Nombre.Contains(buscar) ||
                    d.IdConsumo.ToString().Contains(buscar)
                );
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await CargarDatosVista();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdConsumo,IdProducto,Cantidad")] ConsumoDetalle detalle)
        {
            var consumo = await _context.Consumos
                .Include(c => c.Mesa)
                .FirstOrDefaultAsync(c => c.IdConsumo == detalle.IdConsumo);

            if (consumo == null)
            {
                ModelState.AddModelError("IdConsumo", "El consumo no existe.");
            }
            else if (consumo.Estado != "Abierto")
            {
                ModelState.AddModelError("IdConsumo", "No se pueden agregar productos a un consumo cerrado.");
            }

            var producto = await _context.Productos.FindAsync(detalle.IdProducto);
            if (producto == null)
                ModelState.AddModelError("IdProducto", "El producto no existe.");
            else
                detalle.PrecioUnitario = producto.Precio;

            if (ModelState.IsValid)
            {
                detalle.FechaRegistro = DateTime.UtcNow;
                _context.Add(detalle);
                await _context.SaveChangesAsync();
                await RecalcularTotalesConsumoAsync(detalle.IdConsumo);
                TempData["Success"] = "Detalle de consumo creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarDatosVista(detalle.IdConsumo);
            return View(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMultiple([FromBody] List<ConsumoDetalle> productos)
        {
            try
            {
                if (productos == null || productos.Count == 0)
                    return Json(new { success = false, message = "No se recibieron productos" });

                var idConsumo = productos.First().IdConsumo;

                var consumo = await _context.Consumos
                    .Include(c => c.Mesa)
                    .FirstOrDefaultAsync(c => c.IdConsumo == idConsumo);

                if (consumo == null)
                    return Json(new { success = false, message = "El consumo especificado no existe" });

                if (consumo.Estado != "Abierto")
                    return Json(new { success = false, message = "No se pueden agregar productos a un consumo cerrado" });

                foreach (var detalle in productos)
                {
                    if (detalle.Cantidad <= 0)
                        return Json(new { success = false, message = "Las cantidades deben ser mayores a cero" });

                    var producto = await _context.Productos.FindAsync(detalle.IdProducto);
                    if (producto == null)
                        return Json(new { success = false, message = $"El producto {detalle.IdProducto} no existe" });

                    detalle.PrecioUnitario = producto.Precio;
                    detalle.FechaRegistro = DateTime.UtcNow;
                    detalle.IdDetalle = 0;

                    _context.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await RecalcularTotalesConsumoAsync(idConsumo);

                return Json(new { success = true, message = "Productos guardados exitosamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> EditConsumo(int? idConsumo)
        {
            if (idConsumo == null) return NotFound();

            var consumo = await _context.Consumos.FindAsync(idConsumo);
            if (consumo == null)
            {
                TempData["Error"] = "El consumo no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (consumo.Estado != "Abierto")
            {
                TempData["Warning"] = "No puedes editar un consumo cerrado.";
                return RedirectToAction(nameof(Index));
            }

            var detalles = await _context.ConsumoDetalles
                .Include(d => d.Producto)
                .Where(d => d.IdConsumo == idConsumo)
                .ToListAsync();

            if (!detalles.Any())
            {
                TempData["Info"] = "Este consumo no tiene detalles.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProductosConPrecio = await _context.Productos
                .Select(p => new { p.IdProducto, p.Nombre, p.Precio })
                .ToListAsync();

            ViewBag.IdConsumo = idConsumo.Value;

            return View(detalles);
        }

        [HttpPost]
        public async Task<IActionResult> EditConsumo([FromBody] List<ConsumoDetalle> detallesActualizados)
        {
            try
            {
                if (detallesActualizados == null || !detallesActualizados.Any())
                    return Json(new { success = false, message = "No se enviaron detalles" });

                var idConsumo = detallesActualizados.First().IdConsumo;

                var consumo = await _context.Consumos.FindAsync(idConsumo);
                if (consumo == null)
                    return Json(new { success = false, message = "El consumo no existe" });

                if (consumo.Estado != "Abierto")
                    return Json(new { success = false, message = "No se puede editar un consumo cerrado" });

                var detallesExistentes = await _context.ConsumoDetalles
                    .Where(d => d.IdConsumo == idConsumo)
                    .ToListAsync();

                var idsActualizados = detallesActualizados.Select(d => d.IdDetalle);

                foreach (var nuevo in detallesActualizados)
                {
                    var producto = await _context.Productos.FindAsync(nuevo.IdProducto);
                    if (producto == null)
                        return Json(new { success = false, message = $"Producto {nuevo.IdProducto} no existe" });

                    nuevo.PrecioUnitario = producto.Precio;
                    nuevo.FechaRegistro = DateTime.Now;

                    if (nuevo.IdDetalle > 0)
                    {
                        var existente = detallesExistentes.FirstOrDefault(d => d.IdDetalle == nuevo.IdDetalle);
                        if (existente != null)
                        {
                            existente.IdProducto = nuevo.IdProducto;
                            existente.Cantidad = nuevo.Cantidad;
                            existente.PrecioUnitario = nuevo.PrecioUnitario;
                            existente.FechaRegistro = nuevo.FechaRegistro;
                            _context.Update(existente);
                        }
                    }
                    else
                    {
                        _context.Add(nuevo);
                    }
                }

                var detallesAEliminar = detallesExistentes
                    .Where(d => !idsActualizados.Contains(d.IdDetalle));

                _context.RemoveRange(detallesAEliminar);

                await _context.SaveChangesAsync();
                await RecalcularTotalesConsumoAsync(idConsumo);

                return Json(new { success = true, message = "Cambios guardados correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var detalle = await _context.ConsumoDetalles
                .Include(d => d.Consumo)
                .FirstOrDefaultAsync(d => d.IdDetalle == id);

            if (detalle == null) return NotFound();

            if (detalle.Consumo?.Estado != "Abierto")
            {
                TempData["Warning"] = "No puedes editar detalles de un consumo cerrado.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProductosConPrecio = await _context.Productos
                .Select(p => new { p.IdProducto, p.Nombre, p.Precio })
                .ToListAsync();

            return View(detalle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConsumoDetalle detalle)
        {
            if (id != detalle.IdDetalle) return NotFound();

            var consumo = await _context.Consumos.FindAsync(detalle.IdConsumo);
            if (consumo?.Estado != "Abierto")
            {
                ModelState.AddModelError("", "No se puede editar un detalle de un consumo cerrado.");
            }

            var producto = await _context.Productos.FindAsync(detalle.IdProducto);
            if (producto == null)
            {
                ModelState.AddModelError("IdProducto", "Producto no válido.");
            }
            else
            {
                detalle.PrecioUnitario = producto.Precio;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProductosConPrecio = await _context.Productos.ToListAsync();
                return View(detalle);
            }

            detalle.FechaRegistro = DateTime.Now;

            try
            {
                _context.Update(detalle);
                await _context.SaveChangesAsync();
                await RecalcularTotalesConsumoAsync(detalle.IdConsumo);
                TempData["Success"] = "Detalle actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var detalle = await _context.ConsumoDetalles
                .Include(d => d.Producto)
                .Include(d => d.Consumo)
                .FirstOrDefaultAsync(d => d.IdDetalle == id);

            if (detalle == null) return NotFound();

            if (detalle.Consumo?.Estado != "Abierto")
            {
                TempData["Warning"] = "No puedes eliminar detalles de un consumo cerrado.";
                return RedirectToAction(nameof(Index));
            }

            return View(detalle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalle = await _context.ConsumoDetalles
                .Include(d => d.Consumo)
                .FirstOrDefaultAsync(d => d.IdDetalle == id);

            if (detalle != null)
            {
                if (detalle.Consumo?.Estado != "Abierto")
                {
                    TempData["Error"] = "No se puede eliminar un detalle de un consumo cerrado.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Remove(detalle);
                await _context.SaveChangesAsync();
                await RecalcularTotalesConsumoAsync(detalle.Consumo!.IdConsumo);
                TempData["Success"] = "Detalle eliminado.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportarPDF(int id)
        {
            try
            {
                var detalles = await _context.ConsumoDetalles
                    .Include(d => d.Producto)
                    .Include(d => d.Consumo)
                        .ThenInclude(c => c.Mesa)
                    .Where(d => d.IdConsumo == id)
                    .ToListAsync();

                if (!detalles.Any())
                {
                    TempData["Error"] = "No hay detalles para este consumo.";
                    return RedirectToAction(nameof(Index));
                }

                var primerDetalle = detalles.First();
                if (primerDetalle.Consumo == null)
                {
                    TempData["Error"] = "Error: El consumo no está cargado correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                var ms = new MemoryStream();
                var writer = new PdfWriter(ms);
                writer.SetCloseStream(false);
                var pdf = new PdfDocument(writer);
                var doc = new Document(pdf);

                PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                doc.SetFont(regularFont);

                doc.Add(new Paragraph($"Consumo #{id}")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER));

                string numeroMesa = primerDetalle.Consumo.Mesa?.NumeroMesa.ToString() ?? "N/A";
                doc.Add(new Paragraph($"Mesa: {numeroMesa}")
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER));

                doc.Add(new Paragraph($"Fecha: {primerDetalle.FechaRegistro:dd/MM/yyyy HH:mm}")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER));

                doc.Add(new Paragraph("\n"));

                Table table = new Table(4).SetWidth(UnitValue.CreatePercentValue(100));

                table.AddHeaderCell(new Cell().Add(new Paragraph("Producto").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Cantidad").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Precio").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Subtotal").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                foreach (var d in detalles)
                {
                    string nombreProducto = d.Producto?.Nombre ?? "Producto desconocido";
                    table.AddCell(new Paragraph(nombreProducto));
                    table.AddCell(new Paragraph(d.Cantidad.ToString())
                        .SetTextAlignment(TextAlignment.RIGHT));
                    table.AddCell(new Paragraph($"S/ {d.PrecioUnitario:N2}")
                        .SetTextAlignment(TextAlignment.RIGHT));
                    table.AddCell(new Paragraph($"S/ {d.SubTotal:N2}")
                        .SetTextAlignment(TextAlignment.RIGHT));
                }

                decimal totalProductos = detalles.Sum(x => x.SubTotal);
                decimal totalMesa = primerDetalle.Consumo.CostoMesa;
                decimal totalFinal = totalProductos + totalMesa;

                table.AddCell(new Cell(1, 2).Add(new Paragraph(""))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                table.AddCell(new Cell(1, 2)
                    .Add(new Paragraph($"TOTAL: S/ {totalFinal:N2}").SetFont(boldFont).SetFontSize(14))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBackgroundColor(ColorConstants.YELLOW));

                doc.Add(table);
                doc.Close();

                byte[] pdfBytes = ms.ToArray();
                ms.Close();

                return File(pdfBytes, "application/pdf", $"Consumo_{id}.pdf");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar PDF: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> ExportarTodosPDF()
        {
            var detalles = await _context.ConsumoDetalles
                .Include(d => d.Producto)
                .Include(d => d.Consumo)
                    .ThenInclude(c => c.Mesa)
                .OrderByDescending(d => d.FechaRegistro)
                .ToListAsync();

            return new ViewAsPdf("PDFView", detalles)
            {
                FileName = $"Todos_Consumos_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4
            };
        }

        private async Task CargarDatosVista(int? idConsumo = null)
        {
            var consumosAbiertos = await _context.Consumos
                .Include(c => c.Mesa)
                .Where(c => c.Estado != null && c.Estado == "Abierto")
                .OrderBy(c => c.Mesa!.NumeroMesa)
                .ToListAsync();

            ViewData["IdConsumo"] = new SelectList(
                consumosAbiertos.Select(c => new
                {
                    c.IdConsumo,
                    Texto = c.Mesa != null ? $"Mesa {c.Mesa.NumeroMesa} - Consumo #{c.IdConsumo}" : $"Consumo #{c.IdConsumo}"
                }),
                "IdConsumo",
                "Texto",
                idConsumo
            );

            ViewBag.ProductosConPrecio = await _context.Productos
                .Select(p => new { p.IdProducto, p.Nombre, p.Precio })
                .ToListAsync();
        }

        private async Task RecalcularTotalesConsumoAsync(int idConsumo)
        {
            var consumo = await _context.Consumos.FirstOrDefaultAsync(c => c.IdConsumo == idConsumo);
            if (consumo == null)
            {
                return;
            }

            // EF Core no puede traducir propiedades [NotMapped] en SQL (SubTotal), sumar usando la expresión enviada a la base de datos
            var totalProductos = await _context.ConsumoDetalles
                .Where(d => d.IdConsumo == idConsumo)
                .SumAsync(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0m;

            consumo.TotalProductos = totalProductos;
            consumo.Total = Math.Round(consumo.TotalProductos + consumo.CostoMesa, 2, MidpointRounding.AwayFromZero);

            await _context.SaveChangesAsync();
        }
    }
}