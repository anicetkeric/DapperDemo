using System.Collections.Generic;

namespace Dapper.Repository.SelectQueryBuilder
{
    public class SqlWhereCondition
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Comparison { get; set; }
        public int Order { get; set; }
        public string ConditionalOperator { get; set; }
        public object Value { get; set; }
        public List<SqlWhereCondition> InnerGridWhereConditions { get; set; }
    }
}
