using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dapper.Repository.SelectQueryBuilder
{
    public class QueryBuilder
    {
        #region Select Query

        public string GetSelectQuery(SelectConfiguration selectConfiguration)
        {

            if (selectConfiguration == null)
                throw new ArgumentException("Invalid table parameter values supplied");
            if (string.IsNullOrEmpty(selectConfiguration.KeyColumnName))
                throw new ArgumentException("Key column is required");
            if (string.IsNullOrEmpty(selectConfiguration.MainTableName))
                throw new ArgumentException("Main table name is required");
            if (selectConfiguration.Columns == null || !selectConfiguration.Columns.Any())
                throw new ArgumentException("Atleast one column is required");

            if (selectConfiguration?.IsPaging ?? false)
            {
                if (selectConfiguration.PageNumber <= 0)
                    throw new ArgumentException("Invalid pagenumber is supplied");
                if (selectConfiguration.PageSize <= 0)
                    throw new ArgumentException("Invalid pagesize is supplied");

                var pagingSqlArray = GetPagingSql(selectConfiguration);
                return $@"SELECT {pagingSqlArray.First()} 
                            {GetTableColumns(selectConfiguration)} FROM 
                            {selectConfiguration.MainTableName} 
                            {GetTableRelationships(selectConfiguration)}
                            {GetWhereCondition(selectConfiguration.WhereConditions)}
                            {pagingSqlArray.ElementAt(1)}";
            }
            return $"SELECT {GetTableColumns(selectConfiguration)} FROM {selectConfiguration.MainTableName} {GetTableRelationships(selectConfiguration)}";
        }

        private static IReadOnlyCollection<string> GetPagingSql(SelectConfiguration selectConfiguration)
        {
            var result = new List<string>
            {
                "COUNT(1) OVER() ROW_COUNT, ",

                $@"{GetSortSql(selectConfiguration)}
                        OFFSET {(selectConfiguration.PageNumber - 1) * selectConfiguration.PageSize} ROWS
                        FETCH NEXT {selectConfiguration.PageSize} ROWS ONLY;"
            };
            return result;
        }

        private static string GetSortSql(SelectConfiguration selectConfiguration)
        {
            if (selectConfiguration?.SortColumns?.Any() ?? false)
            {
                var orderByColumnsBuilder = new StringBuilder();
                selectConfiguration.SortColumns.ForEach(s =>
                {
                    orderByColumnsBuilder.Append($"{s.Name} {s.SortOrder}, ");
                });
                return orderByColumnsBuilder.ToString().TrimEnd(new char[] {',', ' '});
            }
            return $"ORDER BY {selectConfiguration?.KeyColumnName} ";
        }


        private static string GetTableColumns(SelectConfiguration selectConfiguration)
        {
            if (!(selectConfiguration?.Columns?.Any() ?? false)) return string.Empty;

            var result = new StringBuilder();
            selectConfiguration.Columns.ForEach(t =>
            {
                result.Append(
                    $", {t.TableName}.{t.Name}{(string.IsNullOrEmpty(t.AsName) ? string.Empty : " AS " + t.AsName)} ");
            });
            return result.ToString().TrimStart(new char[] {','});
        }

        private static string GetTableRelationships(SelectConfiguration selectConfiguration)
        {
            if (!(selectConfiguration?.Relationships?.Any() ?? false)) return string.Empty;

            var result = new StringBuilder();
            selectConfiguration.Relationships.ForEach(t =>
            {
                result.Append($"{t.JoinType} {t.Table2Name} {(string.IsNullOrEmpty(t.Table2AsName)? string.Empty : "AS " + t.Table2AsName)} ON {t.Table1Name}.{t.Table1ColumnName} = {(string.IsNullOrEmpty(t.Table2AsName) ? t.Table2Name : t.Table2AsName)}.{t.Table2ColumnName} " + Environment.NewLine);
            });
            return result.ToString();
        }

        private static string GetWhereCondition(List<SqlWhereCondition> conditions)
        {
            if (conditions != null && conditions.Any())
            {
                return InnerWhereCondition(conditions).TrimStart();
            }
            return string.Empty;
        }

        private static string InnerWhereCondition(List<SqlWhereCondition> conditions)
        {
            if (conditions != null && conditions.Any())
            {
                var builder = new StringBuilder();
                var index = 0;
                conditions.ForEach(c =>
                {
                    if (index == 0)
                        builder.Append($"{c.ConditionalOperator} (");

                    builder.Append($"{WhereCondition(c)} {InnerWhereCondition(c.InnerGridWhereConditions)}");
                });
                builder.Append(") ");

                return builder.ToString();
            }
            return string.Empty;
        }

        private static string WhereCondition(SqlWhereCondition condition)
        {
            return $"{condition.ConditionalOperator} {(string.IsNullOrEmpty(condition.TableName) ? string.Empty : condition.TableName + ".")}{condition.ColumnName} = @{condition.ColumnName}" + Environment.NewLine;
        }

        #endregion

        #region Insert Query
        
        public string GetInsertQuery(InsertConfiguration insertConfiguration)
        {
            return
                $@"INSERT INTO {insertConfiguration.TableName} ({GetInsertColumns(insertConfiguration.Columns)})
                                VALUES ({GetInsertColumnParameters(insertConfiguration.Columns)}";
        }

        private static string GetInsertColumns(IEnumerable<string> columns) => string.Join(", ", columns);

        private static string GetInsertColumnParameters(IEnumerable<string> columns) => string.Join(", @", columns);

        #endregion

        #region Update Query

        public string GetUpdateQuery(UpdateConfiguration updateConfiguration)
        {
            return
                   $@"UPDATE {updateConfiguration.TableName} {GetUpdateColumnsAndParamters(updateConfiguration.Columns)}
                           WHERE {GetUpdateWhereCondition(updateConfiguration.WhereConditions)}";
        }

        private static string GetUpdateColumnsAndParamters(IEnumerable<string> columns) => string.Join(", ", columns);

        private static string GetUpdateWhereCondition(IEnumerable<WhereCondition> whereConditions) => string.Join(",", whereConditions.Select(s => $"{s.ColumnId} = @{s.ColumnId.Replace("-", "_")}"));

        #endregion

    }

    public class InsertConfiguration
    {
        public string TableName { get; set; }
        public IEnumerable<string> Columns { get; set; }
    }

    public class UpdateConfiguration
    {
        public string TableName { get; set; }
        public IEnumerable<string> Columns { get; set; }
        public string KeyColumnName { get; set; }
        public IEnumerable<WhereCondition> WhereConditions { get; set; }
    }
}