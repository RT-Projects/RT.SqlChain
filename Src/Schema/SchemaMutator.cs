using System;
using System.Data.Common;
using System.IO;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.SqlChain.Schema
{
    public abstract class SchemaMutator
    {
        protected DbConnection Connection { get; private set; }
        protected IRetriever Retriever { get; private set; }

        /// <summary>
        /// If null, logging is disabled. Otherwise every SQL query executed is logged to this instance.
        /// </summary>
        public TextWriter Log { get; set; }
        /// <summary>
        /// Set to true to prevent the SQL queries from being executed. They will still be logged as long as
        /// <see cref="Log"/> is not null.
        /// </summary>
        public bool LogOnly { get; set; }

        public SchemaMutator(DbConnection connection, IRetriever retriever)
        {
            LogOnly = false;
            Connection = connection;
            Retriever = retriever;
        }

        protected int ExecuteSql(string sql)
        {
            if (Log != null)
            {
                Log.WriteLine(sql);
                Log.WriteLine();
            }
            if (LogOnly)
                return -1;
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        public virtual void CreateSchema(SchemaInfo schema)
        {
            foreach (var table in schema.Tables)
                CreateTable(table);
        }

        public abstract void CreateTable(TableInfo table);
    }

    public class SqliteSchemaMutator : SchemaMutator
    {
        public SqliteSchemaMutator(DbConnection connection) : base(connection, new SqliteRetriever(connection)) { }

        public string TypeToSqlString(TypeInfo type)
        {
            string nullable = type.Nullable ? "" : " NOT NULL";
            switch (type.BasicType)
            {
                case BasicType.FixText: return "NCHAR({0})".Fmt(type.Length.Value) + nullable;
                case BasicType.FixBinary: return "BINARY({0})".Fmt(type.Length.Value) + nullable;
                case BasicType.VarText: return "NVARCHAR" + (type.Length == null ? "" : "({0})".Fmt(type.Length.Value)) + nullable;
                case BasicType.VarBinary: return "VARBINARY" + (type.Length == null ? "" : "({0})".Fmt(type.Length.Value)) + nullable;
                case BasicType.Boolean: return "BOOLEAN" + nullable;
                case BasicType.Autoincrement: return "INTEGER" + nullable;
                case BasicType.Byte: return "TINYINT" + nullable;
                case BasicType.Short: return "SMALLINT" + nullable;
                case BasicType.Int: return "INT" + nullable;
                case BasicType.Long: return "INTEGER" + nullable;
                case BasicType.Double: return "FLOAT" + nullable;
                default:
                    throw new Exception("Unknown BasicType");
            }
        }

        public override void CreateTable(TableInfo table)
        {
            bool first;
            var sql = new StringBuilder();
            sql.AppendFormat("CREATE TABLE [{0}] (", table.Name);
            sql.AppendLine();
            var pk = table.PrimaryKey;
            // Columns
            first = true;
            foreach (var column in table.Columns)
            {
                if (!first)
                    sql.AppendLine(",");
                first = false;
                sql.AppendFormat("  [{0}] {1}", column.Name, TypeToSqlString(column.Type));
                if (column.IsPartOfPrimaryKey && pk != null && pk.ColumnNames.Count == 1)
                {
                    sql.Append(" CONSTRAINT [{0}] PRIMARY KEY".Fmt(pk.Name));
                    pk = null;
                    if (column.Type.BasicType == BasicType.Autoincrement)
                        sql.Append(" AUTOINCREMENT");
                }
            }
            // Primary key
            if (pk != null)
            {
                sql.AppendLine(",");
                sql.Append("  CONSTRAINT [{0}] PRIMARY KEY ({1})".Fmt(
                    pk.Name,
                    pk.ColumnNames.JoinString(", ")));
            }
            // Unique constraints
            foreach (var unique in table.UniqueConstraints)
            {
                sql.AppendLine(",");
                sql.Append("  CONSTRAINT [{0}] UNIQUE ({1})".Fmt(
                    unique.Name,
                    unique.ColumnNames.JoinString(", ")));
            }
            // Foreign keys
            foreach (var foreignKey in table.ForeignKeys)
            {
                sql.AppendLine(",");
                sql.Append("  CONSTRAINT [{0}] FOREIGN KEY ({1}) REFERENCES [{2}] ({3})".Fmt(
                    foreignKey.Name,
                    foreignKey.ColumnNames.JoinString(", "),
                    foreignKey.ReferencedTableName,
                    foreignKey.ReferencedColumnNames.JoinString(", ")));
            }
            sql.Append(")");

            ExecuteSql(sql.ToString());
        }
    }
}
