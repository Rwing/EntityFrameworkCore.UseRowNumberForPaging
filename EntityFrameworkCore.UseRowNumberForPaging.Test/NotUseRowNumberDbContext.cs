using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.UseRowNumberForPaging;

public class NotUseRowNumberDbContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Author> Authors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\mssqllocaldb;Database=Blogging;Integrated Security=True");
    }
}
