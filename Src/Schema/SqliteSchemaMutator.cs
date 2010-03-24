using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

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
}
