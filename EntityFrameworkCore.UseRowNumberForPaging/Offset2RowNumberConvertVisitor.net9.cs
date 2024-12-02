#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.UseRowNumberForPaging;

internal class Offset2RowNumberConvertVisitor(
    Expression root,
    ISqlExpressionFactory sqlExpressionFactory,
    SqlAliasManager sqlAliasManager
) : ExpressionVisitor
{
    private readonly Expression root = root;
    private readonly ISqlExpressionFactory sqlExpressionFactory = sqlExpressionFactory;
    private readonly SqlAliasManager sqlAliasManager = sqlAliasManager;

    protected override Expression VisitExtension(Expression node) => node switch
    {
        ShapedQueryExpression shapedQueryExpression => shapedQueryExpression.Update(Visit(shapedQueryExpression.QueryExpression), Visit(shapedQueryExpression.ShaperExpression)),
        SelectExpression se => VisitSelect(se),
        _ => base.VisitExtension(node),
    };

    private SelectExpression VisitSelect(SelectExpression selectExpression)
    {
        // if we have no offset, we do not need to use ROW_NUMBER for offset calculations
        if (selectExpression.Offset == null)
        {
            return selectExpression;
        }
        var isRootQuery = selectExpression == root;

        // store offset, limit and orderings
        var oldOffset = selectExpression.Offset;
        var oldLimit = selectExpression.Limit;
        var oldOrderings = selectExpression.Orderings;

        // remove offset and limit by creating new select expression from old one
        // we can't use SelectExpression.Update because that breaks PushDownIntoSubquery
        var enhancedSelect = new SelectExpression(
            alias: null,
            tables: new(selectExpression.Tables),
            predicate: selectExpression.Predicate,
            groupBy: new(selectExpression.GroupBy),
            having: selectExpression.Having,
            projections: new(selectExpression.Projection),
            distinct: selectExpression.IsDistinct,
            orderings: isRootQuery ? [] : new(selectExpression.Orderings),
            offset: null,
            limit: null,
            tags: selectExpression.Tags,
            annotations: null,
            sqlAliasManager: sqlAliasManager,
            isMutable: true
        );
        // set up row_number expression
        var rowNumber = new RowNumberExpression([], isRootQuery ? [ new(new SqlFragmentExpression("(SELECT 1)"), true) ] : oldOrderings, oldOffset.TypeMapping);
        enhancedSelect.AddToProjection(rowNumber);
        enhancedSelect.PushdownIntoSubquery();

        // restore ordering to outer select after earlier removal
        if (isRootQuery)
        {
            foreach (var orderingClause in oldOrderings)
            {
                selectExpression.AppendOrdering(orderingClause);
            }
        }

        // generate subselect rownumber access expression
        var innerTable = enhancedSelect.Tables[0];
        var rowNumberColname = enhancedSelect.Projection[enhancedSelect.Projection.Count - 1].Alias;
        var rowNumberAlias = enhancedSelect.CreateColumnExpression(innerTable, rowNumberColname, typeof(int), null, false);

        // apply offset and limit
        var rowNumberGtOffset = sqlExpressionFactory.GreaterThan(rowNumberAlias, oldOffset);
        enhancedSelect.ApplyPredicate(rowNumberGtOffset);
        if (oldLimit != null)
        {
            if (oldOrderings.Count == 0)
            {
                var rowNumberLimiting = sqlExpressionFactory.LessThanOrEqual(rowNumberAlias, sqlExpressionFactory.Add(oldOffset, oldLimit));
                enhancedSelect.ApplyPredicate(rowNumberLimiting);
            }
            else
            {
                enhancedSelect.ApplyLimit(oldLimit);
            }
        }

        enhancedSelect.ApplyProjection(); // to make immutable
        var restoredProjections = enhancedSelect.Projection
            .Where(p => p.Alias != rowNumberColname)
            .ToList();
        var result = enhancedSelect.Update(
            enhancedSelect.Tables,
            enhancedSelect.Predicate,
            enhancedSelect.GroupBy,
            enhancedSelect.Having,
            restoredProjections,
            enhancedSelect.Orderings,
            enhancedSelect.Offset,
            enhancedSelect.Limit
        );

        // restore projection member binding lookup capabilities via reflection magic
        var projectionMapping = typeof(SelectExpression).GetField("_projectionMapping", BindingFlags.NonPublic | BindingFlags.Instance);
        projectionMapping.SetValue(result, projectionMapping.GetValue(selectExpression));
        return result;
    }
}
#endif