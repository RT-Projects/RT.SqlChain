using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace RT.SqlChain.Schema
{
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

        protected override void transformTable(TableInfo table, List<Tuple<ColumnInfo, string>> newStructure)
        {
            var schema = table.Schema;

            var newTableName = "_new_table";
            var i = 1;
            while (schema.Tables.Any(t => t.Name.EqualsNoCase(newTableName)))
            {
                i++;
                newTableName = "_new_table_" + i;
            }

            ExecuteSql("BEGIN TRANSACTION");

            // Drop all the foreign-key constraints so that we can delete the table
            foreach (var foreignKey in table.ForeignKeys)
                ExecuteSql("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]".Fmt(table.Name, foreignKey.Name));

            foreach (var otherTable in schema.Tables.Where(t => t != table))
                foreach (var foreignKey in otherTable.ForeignKeys.Where(f => f.ReferencedTable == table))
                    ExecuteSql("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]".Fmt(otherTable.Name, foreignKey.Name));

            var sb = new StringBuilder();
            sb.AppendLine("CREATE TABLE [{0}] (".Fmt(newTableName));
            bool first = true;
            foreach (var struc in newStructure)
            {
                if (!first)
                    sb.AppendLine(",");
                sb.Append("    [{0}] {1}".Fmt(struc.E1.Name, TypeToSqlString(struc.E1.Type)));
                if (struc.E1.Type.BasicType == BasicType.Autoincrement)
                    sb.Append(" " + AutoincrementSuffix);
                first = false;
            }
            sb.AppendLine();
            sb.AppendLine(")");
            ExecuteSql(sb.ToString());

            ExecuteSql("SET IDENTITY_INSERT [{0}] ON".Fmt(newTableName));

            sb = new StringBuilder();
            sb.Append("INSERT INTO [{0}] (".Fmt(newTableName));
            sb.Append(newStructure.Select(struc => "[{0}]".Fmt(struc.E1.Name)).JoinString(", "));
            sb.AppendLine(")");
            sb.Append("SELECT ");
            sb.AppendLine(newStructure.Select(struc => struc.E2).JoinString(", "));
            sb.AppendLine("FROM [{0}] oldtable".Fmt(table.Name));
            ExecuteSql(sb.ToString());

            ExecuteSql("SET IDENTITY_INSERT [{0}] OFF".Fmt(newTableName));

            ExecuteSql("DROP TABLE [{0}]".Fmt(table.Name));

            ExecuteSql("sp_rename @objname='{0}', @newname='{1}', @objtype='OBJECT'".Fmt(newTableName, table.Name));

            // Re-create the primary key on this table
            if (table.PrimaryKey != null)
            {
                ExecuteSql("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] PRIMARY KEY ({2})".Fmt(
                    table.Name,
                    table.PrimaryKey.Name,
                    table.PrimaryKey.ColumnNames.JoinString(", ")));
            }

            // Now put all the foreign-key constraints back on the new table
            foreach (var foreignKey in table.ForeignKeys)
                ExecuteSql("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ({2}) REFERENCES [{3}] ({4})".Fmt(
                    table.Name,
                    foreignKey.Name,
                    foreignKey.ColumnNames.JoinString(", "),
                    foreignKey.ReferencedTableName,
                    foreignKey.ReferencedColumnNames.JoinString(", ")));

            foreach (var otherTable in schema.Tables.Where(t => t != table))
                foreach (var foreignKey in otherTable.ForeignKeys.Where(f => f.ReferencedTable == table))
                    ExecuteSql("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ({2}) REFERENCES [{3}] ({4})".Fmt(
                        otherTable.Name,
                        foreignKey.Name,
                        foreignKey.ColumnNames.JoinString(", "),
                        foreignKey.ReferencedTableName,
                        foreignKey.ReferencedColumnNames.JoinString(", ")));

            ExecuteSql("COMMIT TRANSACTION");
        }

        public override string SqlLength(string parameter) { return "len({0})".Fmt(parameter); }
    }
}
