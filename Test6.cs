BooleanExpression exp = f.Operator.ToUpper() switch
{
    "=" => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.Equals,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    },

    "!=" or "<>" => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.NotEqualTo,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    },

    ">" => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.GreaterThan,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    },

    ">=" => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.GreaterThanOrEqualTo,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    },

    "<" => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.LessThan,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    },

    "<=" => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.LessThanOrEqualTo,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    },

    "IN" => new BooleanExpressionSnippet
    {
        Script = $"{tableOrAlias}.{f.ColumnName} IN (SELECT [value] FROM OPENJSON(@{f.ColumnName}_Json))"
    },

    "NOT IN" => new BooleanExpressionSnippet
    {
        Script = $"{tableOrAlias}.{f.ColumnName} NOT IN (SELECT [value] FROM OPENJSON(@{f.ColumnName}_Json))"
    },

    _ => new BooleanComparisonExpression
    {
        ComparisonType = BooleanComparisonType.Equals,
        FirstExpression = column,
        SecondExpression = new StringLiteral { Value = f.Value }
    }
};
