using Microsoft.EntityFrameworkCore;
using WeeCompanyAPI.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    //Set<Usuario>(): Método de EF Core que devuelve un DbSet configurado para la entidad.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
}

