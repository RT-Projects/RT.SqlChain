using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
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
    }
}
