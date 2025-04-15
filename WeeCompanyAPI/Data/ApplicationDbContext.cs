// Importamos Entity Framework Core para manejar la base de datos
using Microsoft.EntityFrameworkCore;

// Importamos los modelos del dominio (Usuario y Reserva)
using WeeCompanyAPI.Models;

// Clase que representa el contexto de base de datos para EF Core
// Esta clase actúa como puente entre las entidades del modelo y la base de datos
public class ApplicationDbContext : DbContext
{
    // Constructor que recibe opciones (como la cadena de conexión)
    // Estas opciones se configuran en Program.cs y se pasan por inyección de dependencias
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // ===============================================
    // DbSet representa una tabla de la base de datos.
    // Cada propiedad aquí se traduce en una tabla.
    // ===============================================

    // Tabla de usuarios
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    // Tabla de reservas
    public DbSet<Reserva> Reservas => Set<Reserva>();
}
