using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using Dapper;
using Dapper.Model;
using Dapper.Repository.Repository;
using Dapper.Repository.SelectQueryBuilder;

namespace Dapper.Repository.Extensions
{
    public static class DapperRepositoryExtensions
    {
        public static IEnumerable<T> GetList<T>(this IDbConnection connection, object parameterValues)
        {
            return GetList<T>(connection,
                new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetSelectQuery(GetSelectConfiguration<T>())
                , parameterValues, CommandType.Text);

        }

        public static IEnumerable<T> GetList<T>(this IDbConnection connection, SelectConfiguration selectConfiguration,
            object parameterValues)
        {
            return GetList<T>(connection,
                new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetSelectQuery(selectConfiguration),
                parameterValues, CommandType.Text);
        }

        public static IEnumerable<T> GetList<T>(this IDbConnection connection, string sqlQuery, object parameterValues,
            CommandType commandType = CommandType.Text)
        {
            return connection.Query<T>(sqlQuery, parameterValues, commandType: commandType);
        }

        public static IEnumerable<dynamic> GetList(this IDbConnection connection,
            SelectConfiguration selectConfiguration, object parameterValues)
        {
            var selectQuery = new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetSelectQuery(selectConfiguration);
            return connection.Query<dynamic>(selectQuery, parameterValues);
        }

        public static IEnumerable<dynamic> GetList(this IDbConnection connection, string sqlQuery,
            object parameterValues, CommandType commandType = CommandType.Text)
        {
            return connection.Query<dynamic>(sqlQuery, parameterValues, commandType: commandType);
        }

        private static T GetEntityExtension<T>(IDbConnection connection, object paramters, string selectQuery,
            CommandType commandType)
        {
            var entity = connection.Query<T, dynamic, T>(selectQuery, (e, dyna) =>
            {
                var entityExtension = e as EntityExtension;
                if (entityExtension != null) entityExtension.ExtensionFieldsObject = dyna;
                return e;
            }, paramters, commandType: commandType).FirstOrDefault();
            return entity;
        }

        public static T FindById<T>(this IDbConnection connection, object id, Tenant tenant = null)
        {
            var selectQuery =
                new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetSelectQuery(GetSelectConfiguration<T>());
            if (!selectQuery.ToUpper().Contains("WHERE"))
            {
                selectQuery = selectQuery + "Where Id=@Id";
            }
            return FindById<T>(connection, selectQuery, id, tenant: tenant);
        }

        public static T FindById<T>(this IDbConnection connection, SelectConfiguration configuration,
            object id, Tenant tenant = null)
        {
            var selectQuery = new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetSelectQuery(configuration);
            if (!selectQuery.ToUpper().Contains("WHERE"))
            {
                selectQuery = selectQuery + "Where Id=@Id";
            }
            return FindById<T>(connection, selectQuery, id, tenant: tenant);
        }

        public static T FindById<T>(this IDbConnection connection, string sqlQuery, object id,
            CommandType commandType = CommandType.Text, Tenant tenant = null)
        {
            var parameters = id?.ToDictionary() ?? new Dictionary<string, object>();
            if (sqlQuery.Contains("@TenantId"))
                parameters.Add("TenantId", tenant?.Id);

            return typeof(T).IsSubclassOf(typeof(EntityExtension))
                ? GetEntityExtension<T>(connection, parameters, sqlQuery, commandType)
                : connection.Query<T>(sqlQuery, parameters, commandType: commandType).FirstOrDefault();
        }

        public static dynamic FindById(this IDbConnection connection, SelectConfiguration selectConfiguration, object id)
        {
            var selectQuery = new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetSelectQuery(selectConfiguration);
            return FindById(connection, selectQuery, id);
        }

        public static dynamic FindById(this IDbConnection connection, string sqlQuery, object id,
            CommandType commandType = CommandType.Text)
        {
            return connection.Query(sqlQuery, new { Id = id }, commandType: commandType).FirstOrDefault();
        }

        public static Result<SpTransactionMessage> ExecuteQuery(this IDbConnection connection, string sqlQuery,
            object paramterValues,
            CommandType commandType, IDbTransaction transaction = null)
        {
            var result =
                connection.Query<SpTransactionMessage>(sqlQuery, paramterValues, commandType: commandType, transaction: transaction)
                    .FirstOrDefault();

            if ((result != null && result.Success) || result == null)
            {
                return Result.Ok(result ?? new SpTransactionMessage() { Success = true });
            }

            return Result.Fail(result, result.Message);
        }

        public static T ExecuteQuery<T>(this IDbConnection connection, string sqlQuery, object paramterValues,
            CommandType commandType)
        {
            return connection.Query<T>(sqlQuery, paramterValues, commandType: commandType).FirstOrDefault();
        }

        public static Result<SpTransactionMessage> InsertExtentionTable<T>(this IDbConnection connection, IDictionary<string, object> parameterValues, IDbTransaction transaction = null)
        {
            if (parameterValues == null)
                throw new ArgumentException("Invalid extention table values");
            var insertConfiguration = GetInsertConfiguration<T>(parameterValues);
            var insertQuery = new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetInsertQuery(insertConfiguration);
            return ExecuteQuery(connection, insertQuery, parameterValues, CommandType.Text, transaction);
        }


        public static Result<SpTransactionMessage> UpdateExtentionTable<T>(this IDbConnection connection, IDictionary<string, object> parameterValues, IDbTransaction transaction = null)
        {
            if (parameterValues == null)
                throw new ArgumentException("Invalid extention table values");
            var updateConfiguration = GetUpdateConfiguration<T>(parameterValues);
            var updateQuery = new QueryBuilder.SelectQueryBuilder.QueryBuilder().GetUpdateQuery(updateConfiguration);
            return ExecuteQuery(connection, updateQuery, parameterValues, CommandType.Text, transaction);
        }

        public static Result<SpTransactionMessage> InsertEntityAndExtensionFields<T>(this IDbConnection connection, T t)
            where T : EntityExtension
        {

            using (var cn = connection)
            {
                cn.Open();
                using (var transaction = cn.BeginTransaction())
                {
                    try
                    {
                        // multiple operations involving cn and tran here
                        cn.Insert<T>(t, transaction);
                        if (t.ExtensionFieldsObject != null ||
                            (t.ExtensionFieldsObject as IDictionary<string, object>) != null &&
                            ((IDictionary<string, object>)t.ExtensionFieldsObject).Count > 0)
                        {
                            cn.InsertExtentionTable<T>(t.ExtensionFieldsObject as IDictionary<string, object>,
                                transaction);

                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            return null;
        }

        private static string GetTableName(string tableName, Tenant tenant)
        {
            return string.IsNullOrEmpty(tenant?.Name) ? tableName : tableName + "Extension" + tenant.Name;
        }

        const string TenantString = "Tenant";

        private static IEnumerable<PropertyInfo> GetPrimitiveTypeProperties<T>()
        {
            var results = new List<PropertyInfo>();
            var properties = typeof(T).GetProperties();
            properties.ToList().ForEach(p =>
            {
                if (IsPrimitive(p.PropertyType))
                    results.Add(p);
            });
            return results;
        }

        private static bool IsPrimitive(Type t)
        {
            //// quite understand what your definition of primitive type is
            //return new[]
            //{
            //    typeof(string),
            //    typeof(char),
            //    typeof(byte),
            //    typeof(sbyte),
            //    typeof(ushort),
            //    typeof(short),
            //    typeof(uint),
            //    typeof(int),
            //    typeof(ulong),
            //    typeof(long),
            //    typeof(float),
            //    typeof(double),
            //    typeof(decimal),
            //    typeof(DateTime),
            //}.Contains(t);
            return t?.Namespace?.StartsWith("System") ?? false;
        }

        //public static InsertConfiguration GetInsertConfiguration<T>(IDictionary<string, object> parameterValues)
        //{
        //    var insertConfiguration = new InsertConfiguration();
        //    var columns = new List<string>();
        //    Tenant tenant = null;
        //    parameterValues.ForEach(p =>
        //    {
        //        if (p.Key.Equals(TenantString))
        //            tenant = p.Value as Tenant;
        //        else
        //            columns.Add(p.Key);
        //    });
        //    insertConfiguration.Columns = columns;
        //    insertConfiguration.TableName = GetTableName(typeof(T).Name, tenant);
        //    return insertConfiguration;
        //}

        public static UpdateConfiguration GetUpdateConfiguration<T>(IDictionary<string, object> parameterValues)
        {
            var properties = FilterProperties<T>(typeof(T).GetProperties());
            var updateConfiguration = new UpdateConfiguration();
            var columns = new List<string>();
            Tenant tenant = null;
            parameterValues.ForEach(p =>
            {
                if (p.Key.Equals(TenantString))
                    tenant = p.Value as Tenant;
                else
                {
                    if (p.Key == "Id")
                        updateConfiguration.KeyColumnName = p.Key;
                    columns.Add(p.Key);
                }
            });
            var property = properties
                .FirstOrDefault(p => p.GetCustomAttributes(false)
                    .Any(a => a is KeyAttribute));
            if (property != null)
                updateConfiguration.KeyColumnName = property.Name;

            updateConfiguration.Columns = columns;
            updateConfiguration.TableName = GetTableName(typeof(T).Name, tenant);
            return updateConfiguration;
        }


        public static SelectConfiguration GetSelectConfiguration<T>()
        {
            var properties = FilterProperties<T>(typeof(T).GetProperties());

            var selectConfiguration = new SelectConfiguration();
            var columns = new List<SqlColumn>();
            PropertyInfo idProperty = null;
            properties.ForEach(p =>
            {
                if (p.Name == "Id")
                    idProperty = p;
                columns.Add(new SqlColumn { AsName = p.Name, Name = p.Name, TableName = typeof(T).Name });
            });
            var property = properties
                .FirstOrDefault(p => p.GetCustomAttributes(false)
                    .Any(a => a is KeyAttribute));
            selectConfiguration.MainTableName = typeof(T).Name;
            selectConfiguration.Columns = columns.GetExtensionColumns<T>(selectConfiguration.MainTableName);
            selectConfiguration.IsPaging = false;

            if (property == null)
            {
                if (idProperty != null)
                    selectConfiguration.KeyColumnName = idProperty.Name;
            }
            else
                selectConfiguration.KeyColumnName = property.Name;

            selectConfiguration.Relationships = new List<SqlRelationship>();
            selectConfiguration.Relationships.AddRange(GetRelationships<T>(selectConfiguration.MainTableName,
                selectConfiguration.KeyColumnName));
            return selectConfiguration;
        }

        private static PropertyInfo[] FilterProperties<T>(PropertyInfo[] properties)
        {
            // if (!typeof(T).IsSubclassOf(typeof(EntityExtension))) return properties;

            var filterList = new List<string> { "EXTENSIONFIELDS", "EXTENSIONFIELDSOBJECT", "COLUMNS", "RELATIONSHIPS", "CACHEKEY" };
            return properties.Where(p => !filterList.Contains(p.Name.ToUpper())).ToArray();
        }

        private static IEnumerable<SqlRelationship> GetRelationships<T>(string mainTableName, string keyColumnName)
        {
            if (!typeof(T).IsSubclassOf(typeof(EntityExtension))) return Enumerable.Empty<SqlRelationship>();

            return new List<SqlRelationship>
            {
                new SqlRelationship
                {
                    JoinType = "Inner Join",
                    Table1ColumnName = keyColumnName,
                    Table1Name = mainTableName,
                    Table2Name = "Dept_" + typeof (T).Name,
                    Table2ColumnName = $"{mainTableName}{keyColumnName}",
                    Table2AsName = string.Empty
                }
            };
        }

        private static List<SqlColumn> GetExtensionColumns<T>(this List<SqlColumn> columns, string mainTableName)
        {
            if (!typeof(T).IsSubclassOf(typeof(EntityExtension))) return columns;

            columns.Add(new SqlColumn { Name = $"*", TableName = $"Dept_{mainTableName}" });
            return columns;
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            var properties = TypeDescriptor.GetProperties(obj);
            foreach (PropertyDescriptor property in properties)
            {
                result.Add(property.Name, property.GetValue(obj));
            }
            return result;
        }
    }
}
