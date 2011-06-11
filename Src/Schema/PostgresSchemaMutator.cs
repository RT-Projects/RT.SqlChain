using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

#pragma warning disable 1591

namespace RT.SqlChain.Schema
{
    public class PostgresSchemaMutator : SchemaMutator
    {
        public PostgresSchemaMutator(DbConnection connection, bool readOnly)
            : base(readOnly ? null : connection, new PostgresSchemaRetriever(connection))
        {
        }

        protected override string AutoincrementSuffix
        {
            get { throw new NotImplementedException(); }
        }

        public override void CreateSchema(SchemaInfo schema)
        {
            throw new NotImplementedException();
        }

        protected override void transformTable(TableInfo table, List<Tuple<ColumnInfo, string>> newStructure)
        {
            throw new NotImplementedException();
        }

        public override void CreateTable(TableInfo table)
        {
            throw new NotImplementedException();
        }

        public override void RenameTable(TableInfo table, string newName)
        {
            throw new NotImplementedException();
        }

        public override void DeleteTable(TableInfo table)
        {
            throw new NotImplementedException();
        }

        public override string SqlLength(string parameter)
        {
            throw new NotImplementedException();
        }
    }
}
