using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591

namespace RT.SqlChain.Schema
{
    public abstract class SchemaRetriever
    {
        protected DbConnection Connection { get; private set; }

        public SchemaRetriever(DbConnection connection)
        {
            Connection = connection;
        }

#if DEBUG
        public void DebugDumpAllMetadata(string name)
        {
            var collections = Connection.GetSchema("MetaDataCollections").Rows.Cast<DataRow>().Select(r => r[0].ToString());
            foreach (var collection in collections)
            {
                try
                {
                    var sch = Connection.GetSchema(collection);
                    System.IO.File.WriteAllLines("C:/schema-{0}-{1}.csv".Fmt(name, collection),
                        new[] { sch.Columns.Cast<DataColumn>().Select(c => c.Caption).JoinString(",", "\"", "\"") }.Concat<string>(
                            sch.Rows.Cast<DataRow>().Select(r => r.ItemArray.Select(item => item.ToString()).JoinString(",", "\"", "\"")))
                        );
                }
                catch (InvalidOperationException) { }
            }
        }
#endif

        public virtual SchemaInfo RetrieveSchema()
        {
            var schema = new SchemaInfo();

            foreach (var table in RetrieveTables())
                schema.AddTable(table);

            schema.Validate();
            return schema;
        }

        public virtual TableInfo RetrieveTable(string tableName)
        {
            var table = new TableInfo();
            table.Name = tableName;

            foreach (var column in RetrieveColumns(tableName))
                table.AddColumn(column);

            foreach (var index in RetrieveIndexes(tableName))
                table.AddIndex(index);

            foreach (var fk in RetrieveForeignKeys(tableName))
                table.AddForeignKey(fk);

            table.Validate();
            return table;
        }

        public abstract IEnumerable<TableInfo> RetrieveTables();
        public abstract IEnumerable<ColumnInfo> RetrieveColumns(string tableName);
        public abstract IEnumerable<IndexInfo> RetrieveIndexes(string tableName);
        public abstract IEnumerable<ForeignKeyInfo> RetrieveForeignKeys(string tableName);
    }
}
