using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using RT.Util;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591

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
                    throw new InternalErrorException("BasicType {0} must be overridden in descendants.".Fmt(type.BasicType));
            }
        }

        protected abstract string AutoincrementSuffix { get; }

        public abstract void CreateSchema(SchemaInfo schema);

        public void TransformTable(TableInfo table, params TableTransform[] transforms)
        {
            if (transforms == null || transforms.Length == 0)
                return;

            var schema = table.Schema;
            if (schema == null)
                throw new InvalidOperationException("TransformTable can only operate on tables that belong to a schema.");

            foreach (var transform in transforms)
            {
                if (transform is MoveColumn && ((MoveColumn) transform).Column.Table != table)
                    throw new InvalidOperationException("TransformTable: The column specified in MoveColumn does not belong to the same table.");
                if (transform is RenameColumn && ((RenameColumn) transform).Column.Table != table)
                    throw new InvalidOperationException("TransformTable: The column specified in RenameColumn does not belong to the same table.");
                if (transform is DeleteColumn && ((DeleteColumn) transform).Column.Table != table)
                    throw new InvalidOperationException("TransformTable: The column specified in DeleteColumn does not belong to the same table.");
            }

            var newStructure = new List<RT.Util.ObsoleteTuple.Tuple<ColumnInfo, string>>(table.Columns.Select(col => RT.Util.ObsoleteTuple.Tuple.New(col, "oldtable.[{0}]".Fmt(col.Name))));

            foreach (var transform in transforms)
            {
                AddColumn add;
                MoveColumn move;
                RenameColumn rename;
                DeleteColumn delete;

                if ((add = transform as AddColumn) != null)
                {
                    int index = Math.Min(newStructure.Count, Math.Max(0, add.InsertAtIndex));
                    add.NewColumn.Table = table;
                    newStructure.Insert(index, RT.Util.ObsoleteTuple.Tuple.New(add.NewColumn, add.Populate ?? "NULL"));
                }
                else if ((move = transform as MoveColumn) != null)
                {
                    if (!newStructure.Any(tup => tup.E1 == move.Column))
                        throw new InvalidOperationException("TransformTable: The MoveColumn transformation refers to a column that doesn't exist or has been removed.");
                    var tuple = newStructure.First(tup => tup.E1 == move.Column);
                    newStructure.RemoveAt(newStructure.IndexOf(tup => tup.E1 == move.Column));
                    newStructure.Insert(Math.Min(newStructure.Count, Math.Max(0, move.NewIndex)), tuple);
                }
                else if ((rename = transform as RenameColumn) != null)
                {
                    if (!newStructure.Any(tup => tup.E1 == rename.Column))
                        throw new InvalidOperationException("TransformTable: The RenameColumn transformation refers to a column that doesn't exist or has been removed.");
                    var tuple = newStructure.First(tup => tup.E1 == rename.Column);
                    var index = newStructure.IndexOf(tup => tup.E1 == rename.Column);
                    newStructure[index] = RT.Util.ObsoleteTuple.Tuple.New(new ColumnInfo { Name = rename.NewName, Type = rename.Column.Type, Table = table }, tuple.E2);
                }
                else if ((delete = transform as DeleteColumn) != null)
                {
                    var index = newStructure.IndexOf(tup => tup.E1 == delete.Column);
                    newStructure.RemoveAt(index);
                }
            }

            foreach (var pair in newStructure.UniquePairs())
                if (pair.Item1.E1.Name == pair.Item2.E1.Name)
                    throw new InvalidOperationException(@"TransformTable: After applying the transformations, the table would have two columns named ""{0}"". Column names must be unique.".Fmt(pair.Item1.E1.Name));

            transformTable(table, newStructure);
        }

        protected abstract void transformTable(TableInfo table, List<RT.Util.ObsoleteTuple.Tuple<ColumnInfo, string>> newStructure);

        public abstract void CreateTable(TableInfo table);

        public abstract void RenameTable(TableInfo table, string newName);

        public abstract void DeleteTable(TableInfo table);

        protected void CreateTableInternal(TableInfo table, bool includeForeignKeys)
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

        public abstract string SqlLength(string parameter);
    }
}