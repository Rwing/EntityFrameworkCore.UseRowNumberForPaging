using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace EntityFrameworkCore.UseRowNumberForPaging.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            using (var dbContext = new UseRowNumberDbContext())
            {
                var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).Skip(0).Take(10).ToQueryString();
               rawSql.ShouldContain("ROW_NUMBER");
            }
        }
        
        [Fact]
        public void Test2()
        {
            using (var dbContext = new NotUseRowNumberDbContext())
            {
                var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).Skip(0).Take(10).ToQueryString();
                rawSql.ShouldContain("OFFSET");
                rawSql.ShouldNotContain("ROW_NUMBER");
            }
        }
    }
}
