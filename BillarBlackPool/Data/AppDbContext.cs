using Microsoft.EntityFrameworkCore;
using BillarBlackPool.Models;

namespace BillarBlackPool.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ============================================================
        // DBSETS - TABLAS DE LA BASE DE DATOS
        // ============================================================
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<CategoriaProducto> CategoriasProductos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Consumo> Consumos { get; set; }
        public DbSet<ConsumoDetalle> ConsumoDetalles { get; set; }

        // ============================================================
        // CONFIGURACIÓN DEL MODELO
        // ============================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario -> Rol
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.IdRol)
                .OnDelete(DeleteBehavior.Restrict);

            // Usuario -> Consumo
            modelBuilder.Entity<Consumo>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Consumos)
                .HasForeignKey(c => c.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Cliente -> Reserva
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Cliente)
                .WithMany(c => c.Reservas)
                .HasForeignKey(r => r.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            // Mesa -> Reserva
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Mesa)
                .WithMany(m => m.Reservas)
                .HasForeignKey(r => r.IdMesa)
                .OnDelete(DeleteBehavior.Restrict);

            // Mesa -> Consumo
            modelBuilder.Entity<Consumo>()
                .HasOne(c => c.Mesa)
                .WithMany(m => m.Consumos)
                .HasForeignKey(c => c.IdMesa)
                .OnDelete(DeleteBehavior.Restrict);

            // Categoría -> Producto
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.CategoriaProducto)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            // Consumo -> Detalle
            modelBuilder.Entity<ConsumoDetalle>()
                .HasOne(cd => cd.Consumo)
                .WithMany(c => c.Detalles)
                .HasForeignKey(cd => cd.IdConsumo)
                .OnDelete(DeleteBehavior.Cascade);

            // Producto -> Detalle
            modelBuilder.Entity<ConsumoDetalle>()
                .HasOne(cd => cd.Producto)
                .WithMany(p => p.Detalles)
                .HasForeignKey(cd => cd.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Precio decimal
            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Consumo>()
                .Property(c => c.PrecioHora)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Consumo>()
                .Property(c => c.PrecioMediaHora)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Consumo>()
                .Property(c => c.PrecioLibrePorMinuto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Consumo>()
                .Property(c => c.CostoMesa)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Consumo>()
                .Property(c => c.TotalProductos)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Consumo>()
                .Property(c => c.Total)
                .HasPrecision(18, 2);

            // Índices únicos
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();
            modelBuilder.Entity<Mesa>().HasIndex(m => m.NumeroMesa).IsUnique();
            modelBuilder.Entity<Rol>().HasIndex(r => r.NomRol).IsUnique();

            // Seed
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { IdRol = 1, NomRol = "Administrador", DesRol = "Acceso total al sistema." },
                new Rol { IdRol = 2, NomRol = "Cajero", DesRol = "Gestiona mesas y reservas." },
                new Rol { IdRol = 3, NomRol = "Mesero", DesRol = "Gestión operativa básica." }
            );

            // Usuarios
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario { IdUsuario = 1, NomUsuario = "Admin", ApeUsuario = "Sistema", Correo = "admin@billar.com", Password = "admin123", IdRol = 1 }
            );

            // Mesas
            modelBuilder.Entity<Mesa>().HasData(
                new Mesa { IdMesa = 1, NumeroMesa = 1, Estado = "Disponible" },
                new Mesa { IdMesa = 2, NumeroMesa = 2, Estado = "Disponible" },
                new Mesa { IdMesa = 3, NumeroMesa = 3, Estado = "Disponible" },
                new Mesa { IdMesa = 4, NumeroMesa = 4, Estado = "Disponible" },
                new Mesa { IdMesa = 5, NumeroMesa = 5, Estado = "Disponible" }
            );
        }

        public static async Task SeedDefaultCatalogDataAsync(ApplicationDbContext context)
        {
            if (context is null)
            {
                return;
            }

            var categories = new[]
            {
                "Bebidas",
                "Snacks",
                "Licores",
                "Cigarros",
                "Galletas"
            };

            var existingCategories = await context.CategoriasProductos
                .AsNoTracking()
                .Select(c => c.Nombre)
                .ToListAsync();

            var existingCategoryNames = new HashSet<string>(
                existingCategories.Where(n => !string.IsNullOrWhiteSpace(n))!,
                StringComparer.OrdinalIgnoreCase);

            foreach (var categoryName in categories)
            {
                if (!existingCategoryNames.Contains(categoryName))
                {
                    context.CategoriasProductos.Add(new CategoriaProducto
                    {
                        Nombre = categoryName
                    });
                }
            }

            await context.SaveChangesAsync();

            var categoryLookup = await context.CategoriasProductos
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Nombre ?? string.Empty, c => c.IdCategoria, StringComparer.OrdinalIgnoreCase);

            var products = new[]
            {
                // Bebidas
                ("Coca Cola 500ml", 4.00m, "Bebidas"),
                ("Inca Kola 500ml", 4.00m, "Bebidas"),
                ("Pepsi 500ml", 3.50m, "Bebidas"),
                ("Sprite 500ml", 3.50m, "Bebidas"),
                ("Fanta 500ml", 3.50m, "Bebidas"),
                ("Agua San Luis", 2.50m, "Bebidas"),
                ("Sporade", 4.50m, "Bebidas"),
                ("Gatorade", 5.00m, "Bebidas"),
                ("Volt", 4.00m, "Bebidas"),
                ("Red Bull", 8.00m, "Bebidas"),
                ("Café", 4.00m, "Bebidas"),

                // Snacks
                ("Papas Lays", 4.00m, "Snacks"),
                ("Doritos", 4.50m, "Snacks"),
                ("Chizitos", 2.50m, "Snacks"),
                ("Cheetos", 4.00m, "Snacks"),
                ("Pringles", 10.00m, "Snacks"),
                ("Mani salado", 4.00m, "Snacks"),
                ("Chifles", 5.00m, "Snacks"),
                ("Nachos", 8.00m, "Snacks"),
                ("Takis", 8.00m, "Snacks"),

                // Licores
                ("Cristal Personal", 8.00m, "Licores"),
                ("Pilsen", 8.00m, "Licores"),
                ("Cusqueña", 10.00m, "Licores"),
                ("Corona", 12.00m, "Licores"),
                ("Heineken", 11.00m, "Licores"),
                ("Smirnoff Ice", 12.00m, "Licores"),
                ("Jack Daniel's (vaso)", 18.00m, "Licores"),
                ("Johnnie Walker (vaso)", 20.00m, "Licores"),
                ("Ron Cartavio (vaso)", 15.00m, "Licores"),
                ("Vodka Smirnoff (vaso)", 15.00m, "Licores"),
                ("Jägermeister Shot", 12.00m, "Licores"),

                // Cigarros
                ("Marlboro Rojo", 1.50m, "Cigarros"),
                ("Marlboro Gold", 1.50m, "Cigarros"),
                ("Lucky Strike", 1.50m, "Cigarros"),
                ("Hamilton", 1.00m, "Cigarros"),
                ("Winston", 1.20m, "Cigarros"),
                ("Pall Mall", 1.20m, "Cigarros"),
                ("Chesterfield", 1.20m, "Cigarros"),

                // Galletas
                ("Oreo", 2.50m, "Galletas"),
                ("Casino", 1.50m, "Galletas"),
                ("Morochas", 2.00m, "Galletas"),
                ("Tentación", 2.50m, "Galletas"),
                ("Ritz", 3.00m, "Galletas"),
                ("Charada", 1.50m, "Galletas"),
                ("Soda Field", 2.00m, "Galletas"),
                ("Chokis", 2.50m, "Galletas"),
                ("Pícaras", 2.00m, "Galletas"),
                ("Cua Cua", 1.50m, "Galletas")
            };

            var existingProducts = new HashSet<string>(
                await context.Productos.AsNoTracking().Select(p => p.Nombre).ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var (nombre, precio, categoriaNombre) in products)
            {
                if (existingProducts.Contains(nombre))
                {
                    continue;
                }

                if (!categoryLookup.TryGetValue(categoriaNombre, out var categoriaId))
                {
                    continue;
                }

                context.Productos.Add(new Producto
                {
                    Nombre = nombre,
                    Precio = precio,
                    IdCategoria = categoriaId
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
