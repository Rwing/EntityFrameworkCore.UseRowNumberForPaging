using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.UseRowNumberForPaging
{
    public class SqlServer2008QueryTranslationPostprocessorFactory : IQueryTranslationPostprocessorFactory
    {
        private readonly QueryTranslationPostprocessorDependencies _dependencies;
        private readonly RelationalQueryTranslationPostprocessorDependencies _relationalDependencies;
        public SqlServer2008QueryTranslationPostprocessorFactory(QueryTranslationPostprocessorDependencies dependencies, RelationalQueryTranslationPostprocessorDependencies relationalDependencies)
        {
            _dependencies = dependencies;
            _relationalDependencies = relationalDependencies;
        }

        public virtual QueryTranslationPostprocessor Create(QueryCompilationContext queryCompilationContext)
            => new SqlServer2008QueryTranslationPostprocessor(
                _dependencies,
                _relationalDependencies,
#if NET9_0_OR_GREATER
                (RelationalQueryCompilationContext)queryCompilationContext
#else
                queryCompilationContext
#endif
            );
        public class SqlServer2008QueryTranslationPostprocessor : RelationalQueryTranslationPostprocessor
        {
            public SqlServer2008QueryTranslationPostprocessor(
                QueryTranslationPostprocessorDependencies dependencies,
                RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
#if NET9_0_OR_GREATER
                RelationalQueryCompilationContext queryCompilationContext
#else
                QueryCompilationContext queryCompilationContext
#endif
            )
            : base(dependencies, relationalDependencies, queryCompilationContext)
            {}

            public override Expression Process(Expression query)
            {
                query = base.Process(query);
#if NET9_0_OR_GREATER
                query = new Offset2RowNumberConvertVisitor(query, RelationalDependencies.SqlExpressionFactory, RelationalQueryCompilationContext.SqlAliasManager).Visit(query);
#else
                query = new Offset2RowNumberConvertVisitor(query, RelationalDependencies.SqlExpressionFactory).Visit(query);
#endif
                return query;
            }
        }
    }
}