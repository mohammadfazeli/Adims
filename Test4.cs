private BooleanExpression? GetFilterBooleanExpression(
    TableReference tableRef,
    BaseDynamicFilter baseFilter)
{
    if (tableRef is not NamedTableReference namedTable) return null;

    var tableName = namedTable.SchemaObject.BaseIdentifier.Value;
    var tableAlias = namedTable.Alias?.Value;

    // فقط اگر جدول درست باشه ادامه میدیم
    if (!string.Equals(tableName, baseFilter.TableName, StringComparison.OrdinalIgnoreCase))
        return null;

    return CombineFilters(tableAlias ?? tableName, baseFilter.SubFilters, "AND");
}

private BooleanExpression? CombineFilters(
    string tableOrAlias,
    List<DynamicFilter>? filters,
    string defaultCombine = "AND")
{
    if (filters == null || filters.Count == 0) return null;

    BooleanExpression? combined = null;

    foreach (var f in filters)
    {
        // ستون
        var column = new ColumnReferenceExpression
        {
            MultiPartIdentifier = new MultiPartIdentifier(
                new[]
                {
                    new Identifier { Value = tableOrAlias },
                    new Identifier { Value = f.ColumnName }
                })
        };

        // شرط اصلی
        BooleanExpression exp = f.Operator.ToUpper() switch
        {
            "=" => new BooleanComparisonExpression
            {
                ComparisonType = BooleanComparisonType.Equals,
                FirstExpression = column,
                SecondExpression = new StringLiteral { Value = f.Value }
            },
            ">" => new BooleanComparisonExpression
            {
                ComparisonType = BooleanComparisonType.GreaterThan,
                FirstExpression = column,
                SecondExpression = new StringLiteral { Value = f.Value }
            },
            "<" => new BooleanComparisonExpression
            {
                ComparisonType = BooleanComparisonType.LessThan,
                FirstExpression = column,
                SecondExpression = new StringLiteral { Value = f.Value }
            },
            "IN" => new InPredicate
            {
                Expression = column,
                Values = f.Value.Split(',')
                                .Select(v => new StringLiteral { Value = v.Trim() })
                                .Cast<ScalarExpression>()
                                .ToList()
            },
            _ => new BooleanComparisonExpression
            {
                ComparisonType = BooleanComparisonType.Equals,
                FirstExpression = column,
                SecondExpression = new StringLiteral { Value = f.Value }
            }
        };

        // اگر SubFilters دارد
        if (f.SubFilters != null && f.SubFilters.Count > 0)
        {
            var subExp = CombineFilters(tableOrAlias, f.SubFilters, f.CombineWithSubFilters ?? "AND");

            if (subExp != null)
            {
                // ترکیب Parent + SubFilters داخل پرانتز
                exp = new BooleanParenthesisExpression
                {
                    Expression = new BooleanBinaryExpression
                    {
                        BinaryExpressionType = f.CombineWithSubFilters?.ToUpper() == "OR"
                            ? BooleanBinaryExpressionType.Or
                            : BooleanBinaryExpressionType.And,
                        FirstExpression = exp,
                        SecondExpression = subExp
                    }
                };
            }
        }

        // ترکیب با بقیه
        if (combined == null)
        {
            combined = exp;
        }
        else
        {
            combined = new BooleanBinaryExpression
            {
                BinaryExpressionType = f.CombineWith?.ToUpper() == "OR"
                    ? BooleanBinaryExpressionType.Or
                    : BooleanBinaryExpressionType.And,
                FirstExpression = combined,
                SecondExpression = exp
            };

            // کل گروه داخل پرانتز
            combined = new BooleanParenthesisExpression { Expression = combined };
        }
    }

    return combined;
}
