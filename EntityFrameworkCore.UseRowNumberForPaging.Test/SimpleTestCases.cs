using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace EntityFrameworkCore.UseRowNumberForPaging.Test;

public class SimpleTestCases
{
    [Fact]
    public void With_TrivialOk()
    {
        using (var dbContext = new UseRowNumberDbContext())
        {
            var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).Skip(0).Take(10).ToQueryString();
            rawSql.ShouldContain("ROW_NUMBER");
        }
    }

    [Fact]
    public void Without_TrivialOk()
    {
        using (var dbContext = new NotUseRowNumberDbContext())
        {
            var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).Skip(0).Take(10).ToQueryString();
            rawSql.ShouldContain("OFFSET");
            rawSql.ShouldNotContain("ROW_NUMBER");
        }
    }

    [Fact]
    public void With_NoSkipClause_OrderDesc_NoRowNumber()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).OrderByDescending(o => o.Rating).Take(20).ToQueryString();
        rawSql.ShouldNotContain("ROW_NUMBER");
        rawSql.ShouldContain("TOP");
        rawSql.ShouldContain("ORDER BY");
    }

    [Fact]
    public void With_OrderDesc_UsesRowNumber()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).OrderByDescending(o => o.Rating).Skip(20).Take(20).ToQueryString();
        rawSql.ShouldContain("ROW_NUMBER");
        rawSql.ShouldContain("ORDER BY");
        rawSql.ShouldContain("TOP");
    }

    [Fact]
    public void With_Order_SplitQuery_UsesRowNumber()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs.Include(b => b.Author).Where(i => i.BlogId > 1)
            .OrderBy(a => a.Author.ContributingSince)
            .OrderByDescending(o => o.Rating)
            .Skip(30).Take(15)
            .AsSplitQuery().ToQueryString();
        rawSql.ShouldContain("ROW_NUMBER");
        rawSql.ShouldContain("ORDER BY");
        rawSql.ShouldContain("TOP");
    }
}
