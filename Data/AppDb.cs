using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> o) : base(o) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<FechaEspecial> FechasEspeciales => Set<FechaEspecial>();
    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaIngrediente> RecetaIngredientes => Set<RecetaIngrediente>();
    public DbSet<Semana> Semanas => Set<Semana>();
    public DbSet<SemanaReceta> SemanaRecetas => Set<SemanaReceta>();
    public DbSet<ListaItem> ListaItems => Set<ListaItem>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<MesInicializado> MesesInicializados => Set<MesInicializado>();
    public DbSet<Config> Configs => Set<Config>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Mensaje>().HasIndex(m => new { m.Mes, m.Dia });
        b.Entity<Ingrediente>().HasIndex(i => i.Nombre).IsUnique();
        b.Entity<Semana>().HasIndex(s => s.LunesInicio).IsUnique();
        b.Entity<MesInicializado>().HasIndex(m => new { m.Anio, m.Mes }).IsUnique();
        b.Entity<Gasto>().HasIndex(g => g.Fecha);
        b.Entity<RecetaIngrediente>().HasOne(ri => ri.Receta).WithMany(r => r.Ingredientes).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RecetaIngrediente>().HasOne(ri => ri.Ingrediente).WithMany(i => i.Recetas).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SemanaReceta>().HasOne(sr => sr.Semana).WithMany(s => s.Recetas).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SemanaReceta>().HasOne(sr => sr.Receta).WithMany().OnDelete(DeleteBehavior.Cascade);
        b.Entity<ListaItem>().HasOne(l => l.Semana).WithMany(s => s.Lista).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Gasto>().Property(g => g.Importe).HasConversion<double>();
        b.Entity<RecetaIngrediente>().Property(g => g.Cantidad).HasConversion<double?>();
        b.Entity<ListaItem>().Property(g => g.Cantidad).HasConversion<double?>();
    }
}
