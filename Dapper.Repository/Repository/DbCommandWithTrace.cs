using Dapper.Repository.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Repository.Configs
{
    public class DbCommandWithTrace : TraceBase, IDisposable
    {
        protected IDbCommand instance = null;

        public DbCommandWithTrace(string cmdText, IDbConnection connection)
        {
            instance = connection.CreateCommand();
            instance.CommandText = cmdText;
        }

        public int CommandTimeout
        {
            get
            {
                return instance.CommandTimeout;
            }
            set
            {
                instance.CommandTimeout = value;
            }
        }

        public IDbConnection Connection
        {
            get
            {
                return instance.Connection;
            }
            set
            {
                instance.Connection = value;
            }
        }

        public string CommandText
        {
            get
            {
                return instance.CommandText;
            }
            set
            {
                instance.CommandText = value;
            }
        }

        public CommandType CommandType
        {
            get
            {
                return instance.CommandType;
            }
            set
            {
                instance.CommandType = value;
            }
        }

        public IDbCommand UnderlyingInstance
        {
            get
            {
                return instance;
            }
        }


        public IDataParameterCollection Parameters
        {
            get
            {
                return instance.Parameters;
            }
        }

        public int ExecuteNonQuery()
        {
            int result;

            Trace("Begin ExecuteNonQuery", "ExecuteNonQuery");

            try
            {
                result = instance.ExecuteNonQuery();
            }
            catch (DbException ex)
            {
                throw new DbExceptionWithTrace("DbException in DbCommandWithTrace.ExecuteNonQuery: " + ex.Message, ex, CommandText, Parameters, instance.Connection);
            }

            Trace("End ExecuteNonQuery, CommandText: {0}", "ExecuteNonQuery", CommandText);

            return result;

        }

        public void Dispose()
        {
            if (instance != null)
            {
                instance.Dispose();
                instance = null;
            }
        }
    }
}
