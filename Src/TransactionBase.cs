using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IQToolkit.Data;
using IQToolkit.Data.Common;

namespace RT.SqlChain
{
    /// <summary>Provides abstract functionality relating to read-only transactions.</summary>
    public abstract class ReadableTransactionBase
    {
        /// <summary>Gets the underlying database connection, which exposes numerous useful methods.</summary>
        protected DbEntityProvider DbProvider { get; set; }
    }

    /// <summary>Provides abstract functionality relating to non-read-only transactions.</summary>
    public abstract class WritableTransactionBase : ReadableTransactionBase
    {
        /// <summary>Executes the specified SQL command.</summary>
        public int ExecuteSql(string sql)
        {
            return DbProvider.ExecuteCommand(sql);
        }

        /// <summary>Executes the specified SQL command with the specified parameter value.</summary>
        public int ExecuteSql<T0>(string sql, T0 p0)
        {
            var cmd = new QueryCommand(sql, new QueryParameter[] { new QueryParameter("p0", typeof(T0), null) });
            return new DbEntityProvider.Executor(DbProvider).ExecuteCommand(cmd, new object[] { p0 });
        }

        /// <summary>Executes the specified SQL command with the specified parameter values.</summary>
        public int ExecuteSql<T0, T1>(string sql, T0 p0, T1 p1)
        {
            var cmd = new QueryCommand(sql, new QueryParameter[] { new QueryParameter("p0", typeof(T0), null), new QueryParameter("p1", typeof(T1), null) });
            return new DbEntityProvider.Executor(DbProvider).ExecuteCommand(cmd, new object[] { p0, p1 });
        }
    }
}
