using Microsoft.EntityFrameworkCore;
using WebFamilyCrud.Models;

namespace WebFamilyCrud.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
}
