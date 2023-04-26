using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591

namespace RT.SqlChain.Schema
{
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
                case BasicType.VarText: return "NVARCHAR" + (type.Length == null ? "" : "({0})".Fmt(type.Length.Value)) + nullable + " COLLATE ORDINALIGNORECASE";
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
            ExecuteSql("BEGIN TRANSACTION");
            foreach (var table in schema.Tables)
                createTable(table, false);
            ExecuteSql("COMMIT TRANSACTION");
        }

        private void createNormalIndex(IndexInfo index)
        {
            if (index.Kind != IndexKind.Normal)
                throw new InvalidOperationException("This method requires Index kind to be 'Normal'.");
            ExecuteSql("CREATE INDEX [{0}] ON [{1}] ({2})".Fmt(index.Name, index.TableName, index.Columns.Select(c => (c.Type.BasicType == BasicType.VarText ? "[{0}] COLLATE ORDINALIGNORECASE" : "[{0}]").Fmt(c.Name)).JoinString(", ")));
        }

        protected override void transformTable(TableInfo table, List<Tuple<ColumnInfo, string>> newStructure)
        {
            var schema = table.Schema;

            var newTableName = "_new_table";
            var i = 1;
            while (schema.Tables.Any(t => t.Name.EqualsIgnoreCase(newTableName)))
            {
                i++;
                newTableName = "_new_table_" + i;
            }

            ExecuteSql("BEGIN TRANSACTION");

            var sb = new StringBuilder();
            sb.AppendLine("CREATE TABLE [{0}] (".Fmt(newTableName));
            bool first = true;
            IndexInfo newPrimaryKey = null;
            if (table.PrimaryKey != null && table.PrimaryKey.ColumnNames.Count > 0)
            {
                newPrimaryKey = new IndexInfo
                {
                    ColumnNames = new List<string>(newStructure.Where(tup => tup.Item1.IsPartOfPrimaryKey).Select(tup => tup.Item1.Name)),
                    Kind = IndexKind.PrimaryKey,
                    Name = table.PrimaryKey.Name,
                    Table = table
                };
            }
            foreach (var struc in newStructure)
            {
                if (!first)
                    sb.AppendLine(",");
                sb.Append("    [{0}] {1}".Fmt(struc.Item1.Name, TypeToSqlString(struc.Item1.Type)));

                // Add primary key if it is single-column
                if (struc.Item1.IsPartOfPrimaryKey && newPrimaryKey != null && newPrimaryKey.ColumnNames.Count == 1)
                {
                    sb.Append(" CONSTRAINT [{0}] PRIMARY KEY".Fmt(newPrimaryKey.Name));
                    newPrimaryKey = null;
                    if (struc.Item1.Type.BasicType == BasicType.Autoincrement)
                        sb.Append(" " + AutoincrementSuffix);
                }
                first = false;
            }

            // Add primary key if it is multi-column
            if (newPrimaryKey != null)
            {
                sb.AppendLine(",");
                sb.Append("  CONSTRAINT [{0}] PRIMARY KEY ({1})".Fmt(
                    newPrimaryKey.Name,
                    newPrimaryKey.ColumnNames.JoinString(", ")));
            }

            // Add all the foreign-key constraints from the old table
            foreach (var foreignKey in table.ForeignKeys)
            {
                sb.AppendLine(",");
                sb.Append("    CONSTRAINT [{0}] FOREIGN KEY ({1}) REFERENCES [{2}] ({3})".Fmt(
                    foreignKey.Name,
                    foreignKey.ColumnNames.JoinString(", "),
                    foreignKey.ReferencedTableName,
                    foreignKey.ReferencedColumnNames.JoinString(", ")));
            }

            sb.AppendLine();
            sb.Append(")");
            ExecuteSql(sb.ToString());

            sb = new StringBuilder();
            sb.Append("INSERT INTO [{0}] (".Fmt(newTableName));
            sb.Append(newStructure.Select(struc => "[{0}]".Fmt(struc.Item1.Name)).JoinString(", "));
            sb.AppendLine(")");
            sb.Append("SELECT ");
            sb.AppendLine(newStructure.Select(struc => struc.Item2).JoinString(", "));
            sb.Append("FROM [{0}] oldtable".Fmt(table.Name));
            ExecuteSql(sb.ToString());

            // SQLite allows us to drop the table even when foreign-key constraints are pointing to it.
            ExecuteSql("DROP TABLE [{0}]".Fmt(table.Name));

            // Renaming the new table to the old name makes all the foreign-key constraints point to it automatically.
            ExecuteSql("ALTER TABLE [{0}] RENAME TO [{1}]".Fmt(newTableName, table.Name));

            ExecuteSql("COMMIT TRANSACTION");
        }

        public override string SqlLength(string parameter) { return "length({0})".Fmt(parameter); }

        public override void CreateTable(TableInfo table)
        {
            createTable(table, true);
        }

        private void createTable(TableInfo table, bool useTransaction)
        {
            if (useTransaction)
                ExecuteSql("BEGIN TRANSACTION");
            CreateTableInternal(table, true);
            foreach (var index in table.Indexes.Where(i => i.Kind == IndexKind.Normal))
                createNormalIndex(index);
            if (useTransaction)
                ExecuteSql("COMMIT TRANSACTION");
        }

        public override void RenameTable(TableInfo table, string newName)
        {
            ExecuteSql("ALTER TABLE [{0}] RENAME TO [{1}]".Fmt(table.Name, newName));
        }

        public override void DeleteTable(TableInfo table)
        {
            ExecuteSql("DROP TABLE [{0}]".Fmt(table.Name));
        }
    }
}
