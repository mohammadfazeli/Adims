private BooleanExpression? GetFilterBooleanExpression(TableReference tableRef, DynamicFilter? filter = null)
{
    if (tableRef is not NamedTableReference namedTable) return null;

    var tableName = namedTable.SchemaObject.BaseIdentifier.Value;
    var tableAlias = namedTable.Alias?.Value;

    var filters = filter?.SubFilters ?? _filterService.GetFiltersForTable(tableName);
    if (filters.Count == 0) return null;

    BooleanExpression combined = null!;
    foreach (var f in filters)
    {
        var column = new ColumnReferenceExpression
        {
            MultiPartIdentifier = new MultiPartIdentifier()
        };

        if (!string.IsNullOrEmpty(tableAlias))
            column.MultiPartIdentifier.Identifiers.Add(new Identifier { Value = tableAlias });
        else
            column.MultiPartIdentifier.Identifiers.Add(new Identifier { Value = tableName });

        column.MultiPartIdentifier.Identifiers.Add(new Identifier { Value = f.ColumnName });

        BooleanExpression exp = f.Operator.ToUpper() switch
        {
            "IN" => new InPredicate
            {
                Expression = column,
                Values = { new IntegerLiteral { Value = f.Value } }
            },
            _ => new BooleanComparisonExpression
            {
                ComparisonType = BooleanComparisonType.Equals,
                FirstExpression = column,
                SecondExpression = new StringLiteral { Value = f.Value }
            }
        };

        // اگر SubFilters دارد، بازگشتی اعمال می‌کنیم
        if (f.SubFilters != null && f.SubFilters.Count > 0)
        {
            var subExp = GetFilterBooleanExpression(tableRef, f);
            if (subExp != null)
                exp = new BooleanBinaryExpression
                {
                    BinaryExpressionType = f.CombineWith?.ToUpper() == "OR" 
                        ? BooleanBinaryExpressionType.Or 
                        : BooleanBinaryExpressionType.And,
                    FirstExpression = exp,
                    SecondExpression = subExp
                };
        }

        combined = combined == null ? exp : new BooleanBinaryExpression
        {
            BinaryExpressionType = f.CombineWith?.ToUpper() == "OR" 
                ? BooleanBinaryExpressionType.Or 
                : BooleanBinaryExpressionType.And,
            FirstExpression = combined,
            SecondExpression = exp
        };
    }

    return combined;
}
