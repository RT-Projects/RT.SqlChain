using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591

namespace RT.SqlChain.Schema
{
    public class PostgresSchemaRetriever : SchemaRetriever
    {
        public PostgresSchemaRetriever(DbConnection conn)
            : base(conn)
        {
        }

        public override IEnumerable<TableInfo> RetrieveTables()
        {
            var tables = new List<TableInfo>();

            foreach (var row in executeSqlAndGetResults("SELECT * FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"))
                tables.Add(RetrieveTable(row["table_name"].ToString()));

            return tables.AsReadOnly();
        }

        public override IEnumerable<ColumnInfo> RetrieveColumns(string tableName)
        {
            var sql = @"
                SELECT *
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{0}'
            ".Fmt(tableName);

            foreach (var row in executeSqlAndGetResults(sql))
            {
                var col = new ColumnInfo();
                col.Name = row["ColumnName"].ToString();
                col.TableName = row["TableName"].ToString();
                col.Type = retrieveType(row);
                col.Type.Validate();
                col.Validate();
                yield return col;
            }
        }

        private TypeInfo retrieveType(DataRow row)
        {
            var type = new TypeInfo();
            type.Nullable = row["is_nullable"].ToString().EqualsIgnoreCase("yes");

            if (row["character_maximum_length"] != null && row["character_maximum_length"].GetType() != typeof(DBNull))
                type.Length = Convert.ToInt32(row["character_maximum_length"]);

            var def = row["column_default"].ToString().ToLowerInvariant();
            bool autoincrement = def.StartsWith("nextval(") && def.EndsWith("::regclass)");
            string sqlType = row["data_type"].ToString().ToLowerInvariant();

            Action assertNoLength = () => { if (type.Length != null) throw new NotSupportedException("Not supported: SQL type \"{0}\" with a length specified".Fmt(sqlType)); };
            Action assertHasLength = () => { if (type.Length == null) throw new NotSupportedException("Not supported: SQL type \"{0}\" with no length specified".Fmt(sqlType)); };

            if (autoincrement)
            {
                if (sqlType == "bigint")
                {
                    type.BasicType = BasicType.Autoincrement;
                    type.Length = null;
                    return type;
                }
                else
                    throw new NotSupportedException("Not supported: SQL type \"{0}\" being autoincrement.".Fmt(sqlType));
            }

            switch (sqlType)
            {
                case "text":
                case "character varying":
                    type.BasicType = BasicType.VarText;
                    if (type.Length == -1)
                        type.Length = null;
                    return type;

                case "bytea":
                    type.BasicType = BasicType.VarBinary;
                    if (type.Length == -1)
                        type.Length = null;
                    return type;

                case "character":
                case "bit":
                    throw new Exception("SqlChain does not support fixed-width types, in particular, '{0}'.".Fmt(sqlType));

                case "boolean":
                    type.BasicType = BasicType.Boolean;
                    type.Length = null;
                    return type;

                case "smallint":
                    type.BasicType = BasicType.Short;
                    type.Length = null;
                    return type;

                case "integer":
                    type.BasicType = BasicType.Int;
                    type.Length = null;
                    return type;

                case "bigint":
                    type.BasicType = BasicType.Long;
                    type.Length = null;
                    return type;

                case "double precision":
                    type.BasicType = BasicType.Double;
                    type.Length = null;
                    return type;

                case "timestamp with time zone":
                    type.BasicType = BasicType.DateTime;
                    type.Length = null;
                    return type;

                default: throw new Exception("SQL Server schema retriever doesn't know how to convert SQL type \"{0}\".".Fmt(sqlType));
            }
        }

        public override IEnumerable<IndexInfo> RetrieveIndexes(string tableName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ForeignKeyInfo> RetrieveForeignKeys(string tableName)
        {
            throw new NotImplementedException();
        }
    }
}
