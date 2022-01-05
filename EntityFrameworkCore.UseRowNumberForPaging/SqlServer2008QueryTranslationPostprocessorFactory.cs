using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

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
				queryCompilationContext);

		public class SqlServer2008QueryTranslationPostprocessor : RelationalQueryTranslationPostprocessor
		{

			public SqlServer2008QueryTranslationPostprocessor(QueryTranslationPostprocessorDependencies dependencies, RelationalQueryTranslationPostprocessorDependencies relationalDependencies, QueryCompilationContext queryCompilationContext)
				: base(dependencies, relationalDependencies, queryCompilationContext)
			{
			}

			public override Expression Process(Expression query)
			{
				query = base.Process(query);
				query = new Offset2RowNumberConvertVisitor(query, RelationalDependencies.SqlExpressionFactory).Visit(query);
				return query;
			}

			private class Offset2RowNumberConvertVisitor : ExpressionVisitor
			{
				private static readonly MethodInfo GenerateOuterColumnAccessorMethodInfo;
				private static readonly Type GenerateOuterColumnAccessorDelegateType;
				private static readonly FieldInfo TableReferencesFieldInfo;

				static Offset2RowNumberConvertVisitor()
				{
					var tableReferenceExpressionType = typeof(SelectExpression).GetNestedType("TableReferenceExpression", BindingFlags.NonPublic);
					System.Diagnostics.Debug.Assert(tableReferenceExpressionType != null);
					var parameterTypes = new Type[] { tableReferenceExpressionType, typeof(SqlExpression), typeof(string), typeof(bool) };
					GenerateOuterColumnAccessorMethodInfo = typeof(SelectExpression).GetMethod("GenerateOuterColumn", BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
					if ((GenerateOuterColumnAccessorMethodInfo?.ReturnType.IsAssignableTo(typeof(ColumnExpression))).GetValueOrDefault() == false)
						throw new InvalidOperationException("SelectExpression.GenerateOuterColumn() is not found");
					GenerateOuterColumnAccessorDelegateType = Expression.GetDelegateType(GenerateOuterColumnAccessorMethodInfo.GetParameters().Select(p => p.ParameterType).Append(GenerateOuterColumnAccessorMethodInfo.ReturnType).ToArray());

					TableReferencesFieldInfo = typeof(SelectExpression).GetField("_tableReferences", BindingFlags.NonPublic | BindingFlags.Instance);
				}

				private readonly Expression root;
				private readonly ISqlExpressionFactory sqlExpressionFactory;

				public Offset2RowNumberConvertVisitor(Expression root, ISqlExpressionFactory sqlExpressionFactory)
				{
					this.root = root;
					this.sqlExpressionFactory = sqlExpressionFactory;
				}

				protected override Expression VisitExtension(Expression node)
				{
					if (node is ShapedQueryExpression shapedQueryExpression)
					{
						return shapedQueryExpression.Update(Visit(shapedQueryExpression.QueryExpression), shapedQueryExpression.ShaperExpression);
					}
					if (node is SelectExpression se)
						node = VisitSelect(se);
					return base.VisitExtension(node);
				}

				private Expression VisitSelect(SelectExpression selectExpression)
				{
					var oldOffset = selectExpression.Offset;
					if (oldOffset == null)
						return selectExpression;

					var oldLimit = selectExpression.Limit;
					var oldOrderings = selectExpression.Orderings;
					//order by in subQuery without TOP N is invalid.
					var newOrderings = oldOrderings.Count > 0 && (oldLimit != null || selectExpression == root)
						? oldOrderings.ToList()
						: new List<OrderingExpression>();
					selectExpression = selectExpression.Update(selectExpression.Projection.ToList(),
															   selectExpression.Tables.ToList(),
															   selectExpression.Predicate,
															   selectExpression.GroupBy.ToList(),
															   selectExpression.Having,
															   orderings: newOrderings,
															   limit: null,
															   offset: null,
															   selectExpression.IsDistinct,
															   selectExpression.Alias);
					var rowOrderings = oldOrderings.Count != 0 ? oldOrderings
						: new[] { new OrderingExpression(new SqlFragmentExpression("(SELECT 1)"), true) };
					selectExpression.PushdownIntoSubquery();
					var subQuery = (SelectExpression)selectExpression.Tables[0];
					var projection = new RowNumberExpression(Array.Empty<SqlExpression>(), rowOrderings, oldOffset.TypeMapping);
					var generateOuterColumnAccessorDelegate = GenerateOuterColumnAccessorMethodInfo.CreateDelegate(GenerateOuterColumnAccessorDelegateType, subQuery);
					var tableReferences = (IEnumerable<object>)TableReferencesFieldInfo.GetValue(selectExpression);
					var tableReferenceExpression = tableReferences.Single();
					var left = (ColumnExpression)generateOuterColumnAccessorDelegate.DynamicInvoke(tableReferenceExpression, projection, "row", true);
					selectExpression.ApplyPredicate(sqlExpressionFactory.GreaterThan(left, oldOffset));
					if (oldLimit != null)
					{
						if (oldOrderings.Count == 0)
						{
							selectExpression.ApplyPredicate(sqlExpressionFactory.LessThanOrEqual(left, sqlExpressionFactory.Add(oldOffset, oldLimit)));
						}
						else
						{
							//the above one not working when used as subQuery with orderBy
							selectExpression.ApplyLimit(oldLimit);
						}
					}
					return selectExpression;
				}
			}
		}
	}
}
