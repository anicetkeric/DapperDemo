namespace Dapper.Repository.SelectQueryBuilder
{
    public class SqlRelationship
    {
        public string Table1Name { get; set; }
        public string Table1ColumnName { get; set; }
        public string Table2Name { get; set; }
        public string Table2AsName { get; set; }
        public string Table2ColumnName { get; set; }
        public string JoinType { get; set; }
    }
}