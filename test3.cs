    public class BaseDynamicFilter
    {
        public string TableName { get; set; } = "";
        public List<DynamicFilter> SubFilters { get; set; } = null;
    }

    public class DynamicFilter
    {
        public string ColumnName { get; set; } = "";
        public string Operator { get; set; } = "=";
        public string Value { get; set; } = "";
        public List<DynamicFilter> SubFilters { get; set; } = null;
        public string? CombineWithSubFilters { get; set; } = "And";

        public string? CombineWith { get; set; } = "And";
    }
