using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591

namespace RT.SqlChain.Schema
{
    public class SqlServerSchemaRetriever : SchemaRetriever
    {
        public SqlServerSchemaRetriever(DbConnection connection)
            : base(connection)
        {
        }

        public override IEnumerable<TableInfo> RetrieveTables()
        {
            var tables = new List<TableInfo>();

            var sql = @"
                    SELECT *
                        FROM  information_schema.tables
                        WHERE table_type='BASE TABLE'
                        ORDER BY table_name
                ";

            foreach (var row in executeSqlAndGetResults(sql))
                tables.Add(RetrieveTable(row["TABLE_NAME"].ToString()));

            return tables.AsReadOnly();
        }

        public override IEnumerable<IndexInfo> RetrieveIndexes(string tableName)
        {
            var indexes = new List<IndexInfo>();

            var sql = @"
				    SELECT
				        i.name as IndexName,
				        t.name as TableName,
					    (SELECT c.name from sys.columns c where c.object_id=ic.object_id AND c.column_id=ic.column_id) as ColumnName,
					    is_primary_key as IsPrimaryKey,
					    is_unique_constraint as IsUniqueConstraint,
					    is_descending_key as IsDescendingKey
				    FROM
					    sys.index_columns ic
					    JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
					    JOIN sys.tables t ON ic.object_id = t.object_id
				    WHERE
				        t.type = 'U' -- user tables only
                        AND t.name = '{0}'
				    ORDER BY
                        IndexName, ic.key_ordinal
                ".Fmt(tableName);

            foreach (var row in executeSqlAndGetResults(sql))
            {
                var index = indexes.FirstOrDefault(i => i.Name.EqualsIgnoreCase(row["IndexName"].ToString()));
                if (index == null)
                {
                    index = new IndexInfo();
                    index.Name = row["IndexName"].ToString();
                    index.TableName = row["TableName"].ToString();
                    index.Kind = row["IsPrimaryKey"].ToString().EqualsIgnoreCase("true") ? IndexKind.PrimaryKey : row["IsUniqueConstraint"].ToString().EqualsIgnoreCase("true") ? IndexKind.Unique : IndexKind.Normal;
                    indexes.Add(index);
                }

                index.ColumnNames.Add(row["ColumnName"].ToString());
            }

            foreach (var index in indexes)
                index.Validate();

            return indexes.AsReadOnly();
        }

        public override IEnumerable<ForeignKeyInfo> RetrieveForeignKeys(string tableName)
        {
            var foreignKeys = new List<ForeignKeyInfo>();

            var sql = @"
                    SELECT * FROM (
                        SELECT
                            OBJECT_NAME(fkc.constraint_object_id) as ForeignKeyName,
                            OBJECT_NAME(fkc.parent_object_id) as ParentTable,
                            OBJECT_NAME(fkc.referenced_object_id) as ReferencedTable,
                            (SELECT cp.name FROM sys.columns cp WHERE cp.object_id=fkc.parent_object_id AND cp.column_id=fkc.parent_column_id) as ParentColumnName,
                            (SELECT cp.name FROM sys.columns cp WHERE cp.object_id=fkc.referenced_object_id AND cp.column_id=fkc.referenced_column_id) as ReferencedColumnName
                        FROM
                            sys.foreign_key_columns fkc
                        ) subquery
                    WHERE
                        ParentTable='{0}'
                    ORDER BY
                        ForeignKeyName
                ".Fmt(tableName);

            foreach (var row in executeSqlAndGetResults(sql))
            {
                var key = foreignKeys.FirstOrDefault(fk => fk.Name.EqualsIgnoreCase(row["ForeignKeyName"].ToString()));
                if (key == null)
                {
                    key = new ForeignKeyInfo();
                    key.Name = row["ForeignKeyName"].ToString();
                    key.TableName = row["ParentTable"].ToString();
                    key.ReferencedTableName = row["ReferencedTable"].ToString();
                    foreignKeys.Add(key);
                }
                key.ColumnNames.Add(row["ParentColumnName"].ToString());
                key.ReferencedColumnNames.Add(row["ReferencedColumnName"].ToString());
            }

            foreach (var key in foreignKeys)
                key.Validate();

            return foreignKeys.AsReadOnly();
        }

        public override IEnumerable<ColumnInfo> RetrieveColumns(string tableName)
        {
            var columns = new List<ColumnInfo>();

            var sql = @"
                    SELECT
                        t.name as TableName,
                        sc.name as ColumnName,
                        sc.is_nullable as IsNullable,
                        sc.is_identity as IsAutoincrement,
                        ic.ordinal_position as OrdinalPosition,
                        ic.column_default as DefaultValue,
                        ic.data_type as DataType,
                        ic.character_maximum_length as CharMaxLength
                    FROM
                        sys.columns sc
                        JOIN sys.tables t ON t.object_id = sc.object_id
                        JOIN information_schema.columns ic ON ic.table_name = t.name AND ic.column_name = sc.name
                    WHERE
                        t.type = 'U'
                        AND t.name = '{0}'
                    ORDER BY
                        TableName, OrdinalPosition
                ".Fmt(tableName);

            foreach (var row in executeSqlAndGetResults(sql))
                columns.Add(retrieveColumn(row));

            return columns;
        }

        private ColumnInfo retrieveColumn(DataRow row)
        {
            var col = new ColumnInfo();
            col.Name = row["ColumnName"].ToString();
            col.TableName = row["TableName"].ToString();
            col.Type = retrieveType(row);
            col.Validate();
            return col;
        }

        private TypeInfo retrieveType(DataRow row)
        {
            var result = retrieveTypeNonValidated(row);
            result.Validate();
            return result;
        }

        private TypeInfo retrieveTypeNonValidated(DataRow row)
        {
            var type = new TypeInfo();
            type.Nullable = row["IsNullable"].ToString().EqualsIgnoreCase("true");

            if (row["CharMaxLength"] != null && row["CharMaxLength"].GetType() != typeof(DBNull))
                type.Length = Convert.ToInt32(row["CharMaxLength"]);

            bool autoincrement = row["IsAutoincrement"].ToString().EqualsIgnoreCase("true");
            string sqlType = row["DataType"].ToString().ToLowerInvariant();

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
                case "varchar":
                case "nvarchar":
                    type.BasicType = BasicType.VarText;
                    if (type.Length == -1)
                        type.Length = null;
                    return type;

                case "varbinary":
                    type.BasicType = BasicType.VarBinary;
                    if (type.Length == -1)
                        type.Length = null;
                    return type;

                case "char":
                case "nchar":
                case "binary":
                    throw new Exception("SqlChain does not support fixed-width types, in particular, '{0}'.".Fmt(sqlType));

                case "bit":
                case "bool":
                case "boolean":
                    type.BasicType = BasicType.Boolean;
                    type.Length = null;
                    return type;

                case "tinyint":
                    type.BasicType = BasicType.Byte;
                    type.Length = null;
                    return type;

                case "smallint":
                    type.BasicType = BasicType.Short;
                    type.Length = null;
                    return type;

                case "int":
                    type.BasicType = BasicType.Int;
                    type.Length = null;
                    return type;

                case "bigint":
                    type.BasicType = BasicType.Long;
                    type.Length = null;
                    return type;

                case "float":
                    type.BasicType = BasicType.Double;
                    type.Length = null;
                    return type;

                case "datetime":
                    type.BasicType = BasicType.DateTime;
                    type.Length = null;
                    return type;

                default: throw new Exception("SQL Server schema retriever doesn't know how to convert SQL type \"{0}\".".Fmt(sqlType));
            }
        }
    }
}

