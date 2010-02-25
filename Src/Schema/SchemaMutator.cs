using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.SqlChain.Schema
{
    public abstract class SchemaMutator
    {
        protected DbConnection Connection { get; private set; }
        protected SchemaRetriever Retriever { get; private set; }

        /// <summary>
        /// If null, logging is disabled. Otherwise every SQL query executed is logged to this instance.
        /// </summary>
        public TextWriter Log { get; set; }

        /// <param name="connection">May be null - in which case no actual schema changes will be made</param>
        /// <param name="retriever">May be null - in which case certain operations only on certain DBMSs only will result in <see cref="NullReferenceException"/>.</param>
        public SchemaMutator(DbConnection connection, SchemaRetriever retriever)
        {
            Connection = connection;
            Retriever = retriever;
        }

        protected int ExecuteSql(string sql)
        {
            if (Log != null)
            {
                Log.Write(sql);
                Log.WriteLine(";");
                Log.WriteLine();
            }
            if (Connection == null)
                return -1;
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        protected virtual string TypeToSqlString(TypeInfo type)
        {
            string nullable = type.Nullable ? "" : " NOT NULL";
            switch (type.BasicType)
            {
                case BasicType.Boolean: return "BIT" + nullable;
                case BasicType.Byte: return "TINYINT" + nullable;
                case BasicType.Short: return "SMALLINT" + nullable;
                case BasicType.Int: return "INT" + nullable;
                case BasicType.Double: return "FLOAT" + nullable;
                case BasicType.DateTime: return "DATETIME" + nullable;
                default:
                    throw new InternalError("BasicType {0} must be overridden in descendants.".Fmt(type.BasicType));
            }
        }

        protected abstract string AutoincrementSuffix { get; }

        public abstract void CreateSchema(SchemaInfo schema);

        protected void CreateTable(TableInfo table, bool includeForeignKeys)
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
                        sql.Append(" " + AutoincrementSuffix);
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
            if (includeForeignKeys)
            {
                foreach (var foreignKey in table.ForeignKeys)
                {
                    sql.AppendLine(",");
                    sql.Append("  CONSTRAINT [{0}] FOREIGN KEY ({1}) REFERENCES [{2}] ({3})".Fmt(
                        foreignKey.Name,
                        foreignKey.ColumnNames.JoinString(", "),
                        foreignKey.ReferencedTableName,
                        foreignKey.ReferencedColumnNames.JoinString(", ")));
                }
            }
            sql.Append(")");

            ExecuteSql(sql.ToString());
        }
    }

    public class SqliteSchemaMutator : SchemaMutator
    {
        public SqliteSchemaMutator(DbConnection connection, bool readOnly)
            : base(readOnly ? null : connection, new SqliteSchemaRetriever(connection))
        {
        }

        protected override string TypeToSqlString(TypeInfo type)
        {
            string nullable = type.Nullable ? "" : " NOT NULL";
            switch (type.BasicType)
            {
                case BasicType.VarText: return "NVARCHAR" + (type.Length == null ? "" : "({0})".Fmt(type.Length.Value)) + nullable + " COLLATE INVARIANTCULTUREIGNORECASE";
                case BasicType.VarBinary: return "VARBINARY" + (type.Length == null ? "" : "({0})".Fmt(type.Length.Value)) + nullable;
                case BasicType.Autoincrement: return "INTEGER" + nullable;
                case BasicType.Long: return "INTEGER" + nullable;
                default: return base.TypeToSqlString(type);
            }
        }

        protected override string AutoincrementSuffix
        {
            get { return "AUTOINCREMENT"; }
        }

        public override void CreateSchema(SchemaInfo schema)
        {
            foreach (var table in schema.Tables)
                CreateTable(table, true);
            foreach (var index in schema.Indexes.Where(i => i.Kind == IndexKind.Normal))
                createNormalIndex(index);
        }

        private void createNormalIndex(IndexInfo index)
        {
            if (index.Kind != IndexKind.Normal)
                throw new InvalidOperationException("This method requires Index kind to be 'Normal'.");
            ExecuteSql("CREATE INDEX [{0}] ON [{1}] ({2})".Fmt(index.Name, index.TableName, index.Columns.Select(c => (c.Type.BasicType == BasicType.VarText ? "[{0}] COLLATE INVARIANTCULTUREIGNORECASE" : "[{0}]").Fmt(c.Name)).JoinString(", ")));
        }
    }

    public class SqlServerSchemaMutator : SchemaMutator
    {
        public SqlServerSchemaMutator(DbConnection connection, bool readOnly)
            : base(readOnly ? null : connection, new SqlServerSchemaRetriever(connection))
        {
        }

        protected override string TypeToSqlString(TypeInfo type)
        {
            string nullable = type.Nullable ? "" : " NOT NULL";
            switch (type.BasicType)
            {
                case BasicType.VarText: return "NVARCHAR" + (type.Length == null ? "(MAX)" : "({0})".Fmt(type.Length.Value)) + nullable;
                case BasicType.VarBinary: return "VARBINARY" + (type.Length == null ? "(MAX)" : "({0})".Fmt(type.Length.Value)) + nullable;
                case BasicType.Autoincrement: return "BIGINT" + nullable;
                case BasicType.Long: return "BIGINT" + nullable;
                default: return base.TypeToSqlString(type);
            }
        }

        protected override string AutoincrementSuffix
        {
            get { return "IDENTITY(1,1)"; }
        }

        public override void CreateSchema(SchemaInfo schema)
        {
            foreach (var table in schema.Tables)
                CreateTable(table, false);
            foreach (var table in schema.Tables)
                foreach (var foreignKey in table.ForeignKeys)
                    ExecuteSql(@"ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ({2}) REFERENCES [{3}] ({4})".Fmt(
                        table.Name,
                        foreignKey.Name,
                        foreignKey.ColumnNames.JoinString(", "),
                        foreignKey.ReferencedTableName,
                        foreignKey.ReferencedColumnNames.JoinString(", ")
                    ));
            foreach (var index in schema.Indexes.Where(i => i.Kind == IndexKind.Normal))
                createNormalIndex(index);
        }

        private void createNormalIndex(IndexInfo index)
        {
            if (index.Kind != IndexKind.Normal)
                throw new InvalidOperationException("This method requires Index kind to be 'Normal'.");
            ExecuteSql("CREATE INDEX [{0}] ON [{1}] ({2})".Fmt(index.Name, index.TableName, index.ColumnNames.JoinString("[", "]", ", ")));
        }
    }
}