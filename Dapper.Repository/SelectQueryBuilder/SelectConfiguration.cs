using System.Collections.Generic;

namespace Dapper.Repository.SelectQueryBuilder
{
    public class SelectConfiguration
    {
        public List<SqlColumn> Columns { get; set; }
        public string MainTableName { get; set; }
        public List<SqlRelationship> Relationships { get; set; }
        public string KeyColumnName { get; set; }
        public List<SqlWhereCondition> WhereConditions { get; set; }
        public List<SqlSortColumn> SortColumns { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool IsPaging { get; set; }
    }
}