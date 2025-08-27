using Microsoft.EntityFrameworkCore.Diagnostics; using System.Data.Common; using Microsoft.SqlServer.TransactSql.ScriptDom; using System.IO; using System.Text; using System.Collections.Generic;

namespace DynamicRls { public class DynamicFilter { public string ColumnName { get; set; } = ""; public string Operator { get; set; } = "="; public string Value { get; set; } = ""; }

public interface IFilterService
{
    List<DynamicFilter> GetFiltersForTable(string tableName);
}

public class DynamicFilterVisitor : TSqlFragmentVisitor
{
    private readonly IFilterService _filterService;

    public DynamicFilterVisitor(IFilterService filterService)
    {
        _filterService = filterService;
    }

    public override void Visit(QuerySpecification node)
    {
        ApplyFiltersToTableReferences(node.FromClause?.TableReferences, ref node.WhereClause);
        base.Visit(node);
    }

    public override void Visit(SelectStatement node)
    {
        base.Visit(node);
    }

    private void ApplyFiltersToTableReferences(IList<TableReference>? tableRefs, ref WhereClause? whereClause)
    {
        if (tableRefs == null) return;

        foreach (var tableRef in tableRefs)
        {
            ApplyFiltersToTableReference(tableRef, ref whereClause);
        }
    }

    private void ApplyFiltersToTableReference(TableReference tableRef, ref WhereClause? whereClause)
    {
        switch (tableRef)
        {
            case NamedTableReference namedTable:
                AddFiltersToNamedTable(namedTable, ref whereClause);
                break;

            case QualifiedJoin join:
                ApplyFiltersToTableReference(join.FirstTableReference, ref whereClause);
                ApplyFiltersToTableReference(join.SecondTableReference, ref whereClause);

                var joinFilters = GetFilterBooleanExpression(join.SecondTableReference);
                if (joinFilters != null)
                {
                    if (join.SearchCondition == null)
                        join.SearchCondition = joinFilters;
                    else
                        join.SearchCondition = new BooleanBinaryExpression
                        {
                            BinaryExpressionType = BooleanBinaryExpressionType.And,
                            FirstExpression = join.SearchCondition,
                            SecondExpression = joinFilters
                        };
                }
                break;
        }
    }

    private void AddFiltersToNamedTable(NamedTableReference table, ref WhereClause? whereClause)
    {
        var filterExpr = GetFilterBooleanExpression(table);
        if (filterExpr == null) return;

        if (whereClause == null)
            whereClause = new WhereClause { SearchCondition = filterExpr };
        else
            whereClause.SearchCondition = new BooleanBinaryExpression
            {
                BinaryExpressionType = BooleanBinaryExpressionType.And,
                FirstExpression = whereClause.SearchCondition,
                SecondExpression = filterExpr
            };
    }

    private BooleanExpression? GetFilterBooleanExpression(TableReference tableRef)
    {
        if (tableRef is not NamedTableReference namedTable) return null;

        var tableName = namedTable.SchemaObject.BaseIdentifier.Value;
        var filters = _filterService.GetFiltersForTable(tableName);
        if (filters.Count == 0) return null;

        BooleanExpression combined = null!;
        foreach (var filter in filters)
        {
            BooleanExpression exp = filter.Operator.ToUpper() switch
            {
                "IN" => new InPredicate
                {
                    Expression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers = { new Identifier { Value = filter.ColumnName } }
                        }
                    },
                    Values = { new IntegerLiteral { Value = filter.Value } }
                },
                _ => new BooleanComparisonExpression
                {
                    ComparisonType = BooleanComparisonType.Equals,
                    FirstExpression = new ColumnReferenceExpression
                    {
                        MultiPartIdentifier = new MultiPartIdentifier
                        {
                            Identifiers = { new Identifier { Value = filter.ColumnName } }
                        }
                    },
                    SecondExpression = new StringLiteral { Value = filter.Value }
                }
            };

            combined = combined == null ? exp : new BooleanBinaryExpression
            {
                BinaryExpressionType = BooleanBinaryExpressionType.And,
                FirstExpression = combined,
                SecondExpression = exp
            };
        }

        return combined;
    }
}

public class DynamicRlsInterceptor : DbCommandInterceptor
{
    private readonly IFilterService _filterService;

    public DynamicRlsInterceptor(IFilterService filterService)
    {
        _filterService = filterService;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        TSql150Parser parser = new TSql150Parser(false);
        TSqlFragment fragment;
        IList<ParseError> errors;

        using (var reader = new StringReader(command.CommandText))
        {
            fragment = parser.Parse(reader, out errors);
        }

        if (errors != null && errors.Count > 0)
            return base.ReaderExecuting(command, eventData, result);

        var visitor = new DynamicFilterVisitor(_filterService);
        fragment.Accept(visitor);

        var sb = new StringBuilder();
        var generator = new Sql150ScriptGenerator();
        generator.GenerateScript(fragment, sb);
        command.CommandText = sb.ToString();

        return base.ReaderExecuting(command, eventData, result);
    }
}

}

