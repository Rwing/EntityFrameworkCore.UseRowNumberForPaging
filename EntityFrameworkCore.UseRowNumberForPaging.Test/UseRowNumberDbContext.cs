using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.UseRowNumberForPaging;

public class UseRowNumberDbContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Author> Authors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\mssqllocaldb;Database=Blogging;Integrated Security=True", i => i.UseRowNumberForPaging());
    }
}

public class Blog
{
    public int BlogId { get; set; }
    public string Url { get; set; }
    public int Rating { get; set; }
    public virtual Author Author { get; set; }
}
public class Author
{
    public int AuthorId { get; set; }
    public string Name { get; set; }
    public DateOnly ContributingSince { get; set; }
    public virtual List<Blog> Blogs { get; set; }
}
