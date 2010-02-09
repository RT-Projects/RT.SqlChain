using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.SqlChain.Schema
{
    public class SqliteSchemaRetriever : SchemaRetriever
    {
        public SqliteSchemaRetriever(DbConnection connection)
            : base(connection)
        {
        }

        private IEnumerable<DataRow> getSchema(string schemaSetName)
        {
            return Connection.GetSchema(schemaSetName).Rows.Cast<DataRow>();
        }

        private IEnumerable<DataRow> getSchema(string schemaSetName, Func<DataRow, bool> filter)
        {
            return getSchema(schemaSetName).Where(filter);
        }

        public override IEnumerable<TableInfo> RetrieveTables()
        {
            var schTables = getSchema("Tables", row => row["TABLE_TYPE"].ToString().EqualsNoCase("table"));
            foreach (var schTable in schTables.OrderBy(r => r["TABLE_NAME"].ToString()))
                yield return RetrieveTable(schTable["TABLE_NAME"].ToString());
        }

        public override IEnumerable<IndexInfo> RetrieveIndexes(string tableName)
        {
            var schIndexes = getSchema("Indexes", row => row["TABLE_NAME"].ToString().EqualsNoCase(tableName));
            foreach (var schIndex in schIndexes.OrderBy(r => r["INDEX_NAME"].ToString()))
                yield return retrieveIndex(schIndex);
        }

        private IndexInfo retrieveIndex(DataRow schIndex)
        {
            var index = new IndexInfo();
            bool isPrimary = schIndex["PRIMARY_KEY"].ToString().EqualsNoCase("true");
            bool isUnique = schIndex["UNIQUE"].ToString().EqualsNoCase("true");

            index.TableName = schIndex["TABLE_NAME"].ToString();
            index.Name = schIndex["INDEX_NAME"].ToString();
            if (isPrimary && !isUnique)
                throw new InvalidOperationException("Index [{0}] on table [{1}] is marked as Primary Key but not Unique.".Fmt(index.TableName, index.Name));
            index.Kind = isPrimary ? IndexKind.PrimaryKey : isUnique ? IndexKind.Unique : IndexKind.Normal;

            var schIndexColumns = getSchema("IndexColumns", row => row["INDEX_NAME"].ToString().EqualsNoCase(index.Name))
                .OrderBy(row => Convert.ToInt32(row["ORDINAL_POSITION"]));
            foreach (var schIndexColumn in schIndexColumns)
                index.ColumnNames.Add(schIndexColumn["COLUMN_NAME"].ToString());

            index.Validate();
            return index;
        }

        public override IEnumerable<ForeignKeyInfo> RetrieveForeignKeys(string tableName)
        {
            var schFKs = getSchema("ForeignKeys", row => row["TABLE_NAME"].ToString().EqualsNoCase(tableName) && row["CONSTRAINT_TYPE"].ToString().EqualsNoCase("FOREIGN KEY"));
            var FKs = schFKs.GroupBy(row => row["CONSTRAINT_NAME"].ToString(), StringComparer.InvariantCultureIgnoreCase);

            foreach (var group in FKs.OrderBy(fk => fk.Key))
            {
                var allNames = group.Select(r => r["CONSTRAINT_NAME"].ToString()).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
                var allTableNames = group.Select(r => r["TABLE_NAME"].ToString()).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
                var allReferencedTableNames = group.Select(r => r["FKEY_TO_TABLE"].ToString()).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
                if (allNames.Count != 1 || allTableNames.Count != 1 || allReferencedTableNames.Count != 1)
                    throw new InternalError("Single ForeignKey group has multiple distinct names for the constraint or the referenced tables.");

                var foreignKey = new ForeignKeyInfo();
                var firstrow = group.First();
                foreignKey.Name = allNames.First();
                foreignKey.TableName = allTableNames.First();
                foreignKey.ReferencedTableName = allReferencedTableNames.First();

                foreach (var schFK in group.OrderBy(row => Convert.ToInt32(row["FKEY_FROM_ORDINAL_POSITION"])))
                {
                    foreignKey.ColumnNames.Add(schFK["FKEY_FROM_COLUMN"].ToString());
                    foreignKey.ReferencedColumnNames.Add(schFK["FKEY_TO_COLUMN"].ToString());
                }

                foreignKey.Validate();
                yield return foreignKey;
            }
        }

        public override IEnumerable<ColumnInfo> RetrieveColumns(string tableName)
        {
            var schColumns = getSchema("Columns", row => row["TABLE_NAME"].ToString().EqualsNoCase(tableName));
            foreach (DataRow schColumn in schColumns.OrderBy(r => Convert.ToInt32(r["ORDINAL_POSITION"])))
                yield return retrieveColumn(schColumn);
        }

        private ColumnInfo retrieveColumn(DataRow schColumn)
        {
            var col = new ColumnInfo();
            col.Name = schColumn["COLUMN_NAME"].ToString();
            col.TableName = schColumn["TABLE_NAME"].ToString();
            col.Type = retrieveType(schColumn);
            col.Validate();
            return col;
        }

        private TypeInfo retrieveType(DataRow schColumn)
        {
            var result = retrieveTypeNonValidated(schColumn);
            result.Validate();
            return result;
        }

        private TypeInfo retrieveTypeNonValidated(DataRow schColumn)
        {
            var type = new TypeInfo();
            type.Nullable = schColumn["IS_NULLABLE"].ToString().EqualsNoCase("true");

            int lengthDummy;
            if (int.TryParse(schColumn["CHARACTER_MAXIMUM_LENGTH"].ToString(), out lengthDummy))
                type.Length = lengthDummy;

            bool autoincrement = schColumn["AUTOINCREMENT"].ToString().EqualsNoCase("true");
            var sqlType = schColumn["DATA_TYPE"].ToString().ToLowerInvariant();

            Action assertNoLength = () => { if (type.Length != null) throw new NotSupportedException("Not supported: SQL type \"{0}\" with a length specified".Fmt(sqlType)); };
            Action assertHasLength = () => { if (type.Length == null) throw new NotSupportedException("Not supported: SQL type \"{0}\" with no length specified".Fmt(sqlType)); };

            if (autoincrement)
            {
                if (sqlType == "integer") // ie 64 bit in sqlite terms
                {
                    type.BasicType = BasicType.Autoincrement;
                    type.Length = null;
                    return type;
                }
                else
                    throw new NotSupportedException("Not supported: SQLite type \"{0}\" being autoincrement.".Fmt(sqlType));
            }

            switch (sqlType)
            {
                case "":
                case "varchar":
                case "nvarchar":
                    type.BasicType = BasicType.VarText;
                    if (type.Length == 2147483647)
                        type.Length = null;
                    return type;

                case "varbinary":
                    type.BasicType = BasicType.VarBinary;
                    if (type.Length == 2147483647)
                        type.Length = null;
                    return type;

                case "char":
                case "nchar":
                    type.BasicType = BasicType.FixText;
                    assertHasLength();
                    return type;

                case "binary":
                    type.BasicType = BasicType.FixBinary;
                    assertHasLength();
                    return type;

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
                case "integer":
                case "long":
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

                default: throw new Exception("{0} doesn't know how to convert SQL type \"{1}\".".Fmt(this.GetType().FullName, sqlType));
            }
        }
    }
}
