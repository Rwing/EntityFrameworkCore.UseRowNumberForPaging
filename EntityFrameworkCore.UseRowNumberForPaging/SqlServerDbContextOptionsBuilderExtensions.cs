using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.UseRowNumberForPaging
{
    public static class SqlServerDbContextOptionsBuilderExtensions
    {

        public static SqlServerDbContextOptionsBuilder UseRowNumberForPaging(this SqlServerDbContextOptionsBuilder optionsBuilder)
        {
            ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder
                .ReplaceService<IQueryTranslationPostprocessorFactory, SqlServer2008QueryTranslationPostprocessorFactory>();
            return optionsBuilder;
        }
    }
}
