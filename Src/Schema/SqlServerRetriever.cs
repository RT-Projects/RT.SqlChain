using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using RT.Util.ExtensionMethods;
using RT.SqlChain;
using System.Data.Common;

namespace RT.SqlChain.Schema
{
    public class SqlServerRetriever : IRetriever
    {
        private DbConnection _connection;

        public SqlServerRetriever(DbConnection connection)
        {
            _connection = connection;
        }

        public SchemaInfo RetrieveSchema()
        {
            throw new NotImplementedException();
        }

        //        private IDataReader GetReader(string sql)
//        {
//            SqlConnection conn = new SqlConnection(_params.ConnectionString);
//            SqlCommand cmd = new SqlCommand(sql, conn);
//            conn.Open();
//            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
//        }

//        private SqlCommand GetCommand(string sql)
//        {
//            SqlConnection conn = new SqlConnection(_params.ConnectionString);
//            SqlCommand cmd = new SqlCommand(sql, conn);
//            conn.Open();
//            return cmd;
//        }

//        public List<TableInfo> RetrieveTables()
//        {
//            var result = new List<TableInfo>();

//            var sql =
//                @"SELECT *
//                FROM  INFORMATION_SCHEMA.TABLES
//                WHERE TABLE_TYPE='BASE TABLE'";

//            //pull the tables in a reader
//            using (IDataReader rdr = GetReader(sql))
//            {
//                while (rdr.Read())
//                {
//                    Table tbl = new Table();
//                    tbl.SqlName = rdr["TABLE_NAME"].ToString();
//                    tbl.Schema = rdr["TABLE_SCHEMA"].ToString();
//                    tbl.Columns = retrieveTableColumns(tbl);
//                    tbl.SingularName = Inflector.MakeSingular(DsgUtil.CleanUp(tbl.SqlName, CleanUpSubject.Table));
//                    tbl.PluralName = Inflector.MakePlural(tbl.SingularName);

//                    //set the PK for the columns
//                    foreach (var pk in retrievePrimaryKeys(tbl.SqlName))
//                        foreach (var col in tbl.Columns.Where(c => c.SqlName.ToLower().Trim() == pk.ToLower().Trim()))
//                            col.IsPrimaryKey = true;

//                    tbl.ForeignKeyTables = retrieveForeignKeyTables(tbl.SqlName);

//                    result.Add(tbl);
//                }
//            }

//            foreach (Table tbl in result)
//            {
//                //loop the FK tables and see if there's a match for our FK columns
//                foreach (Column col in tbl.Columns)
//                {
//                    col.IsForeignKey = tbl.ForeignKeyTables.Any(
//                        x => x.ThisColumn.Equals(col.SqlName, StringComparison.InvariantCultureIgnoreCase)
//                    );
//                }
//            }
//            return result;
//        }

//        private List<ColumnInfo> retrieveTableColumns(TableInfo tbl)
//        {
//            var result = new List<Column>();
//            var sql =
//                @"SELECT
//                    TABLE_CATALOG AS [Database],
//                    TABLE_SCHEMA AS Owner,
//                    TABLE_NAME AS TableName,
//                    COLUMN_NAME AS ColumnName,
//                    ORDINAL_POSITION AS OrdinalPosition,
//                    COLUMN_DEFAULT AS DefaultSetting,
//                    IS_NULLABLE AS IsNullable, DATA_TYPE AS DataType,
//                    CHARACTER_MAXIMUM_LENGTH AS MaxLength,
//                    DATETIME_PRECISION AS DatePrecision,
//                    COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') AS IsIdentity,
//                    COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') as IsComputed
//                FROM  INFORMATION_SCHEMA.COLUMNS
//                WHERE TABLE_NAME=@tableName
//                ORDER BY OrdinalPosition ASC";
//            var cmd = GetCommand(sql);
//            cmd.Parameters.AddWithValue("@tableName", tbl.SqlName);

//            using (IDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
//            {
//                while (rdr.Read())
//                {
//                    Column col = new Column();
//                    col.SqlName = rdr["ColumnName"].ToString();
//                    col.CleanName = DsgUtil.CleanUp(col.SqlName, CleanUpSubject.Column);
//                    col.SqlTypeString = rdr["DataType"].ToString();
//                    col.ClrType = GetClrType(col.SqlTypeString);
//                    col.DbType = GetDbType(col.SqlTypeString);
//                    col.AutoIncrement = rdr["IsIdentity"].ToString() == "1";
//                    col.IsNullable = rdr["IsNullable"].ToString() == "YES";
//                    int.TryParse(rdr["MaxLength"].ToString(), out col.MaxLength);

//                    result.Add(col);
//                }

//            }

//            return result;
//        }

//        private List<string> retrievePrimaryKeys(string table)
//        {
//            DataTable pkTable = new DataTable();
//            string sql = @"SELECT KCU.COLUMN_NAME as ColumnName
//                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU
//                JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
//                ON KCU.CONSTRAINT_NAME=TC.CONSTRAINT_NAME
//                WHERE TC.CONSTRAINT_TYPE='PRIMARY KEY'
//		        AND KCU.TABLE_NAME=@tableName";

//            var pks = new List<string>();

//            using (var cmd = GetCommand(sql))
//            {
//                cmd.Parameters.AddWithValue("@tableName", table);
//                using (IDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
//                    while (rdr.Read())
//                        pks.Add((string) rdr["ColumnName"]);
//            }
//            return pks;
//        }

//        private List<ForeignKeyTable> retrieveForeignKeyTables(string tableName)
//        {
//            //this is a "bi-directional" scheme
//            //which pulls both 1-many and many-1

//            var result = new List<ForeignKeyTable>();
//            var sql =
//                @"SELECT
//                    ThisTable  = FK.TABLE_NAME,
//                    ThisColumn = CU.COLUMN_NAME,
//                    OtherTable  = PK.TABLE_NAME,
//                    OtherColumn = PT.COLUMN_NAME,
//                    Constraint_Name = C.CONSTRAINT_NAME,
//                    Owner = FK.TABLE_SCHEMA
//                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C
//                INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
//                INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
//                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
//                INNER JOIN
//                    (
//                        SELECT i1.TABLE_NAME, i2.COLUMN_NAME
//                        FROM  INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
//                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
//                        WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
//                    )
//                PT ON PT.TABLE_NAME = PK.TABLE_NAME
//                WHERE FK.Table_NAME=@tableName OR PK.Table_NAME=@tableName";
//            var cmd = GetCommand(sql);
//            cmd.Parameters.AddWithValue("@tableName", tableName);
//            using (IDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
//            {
//                while (rdr.Read())
//                {
//                    ForeignKeyTable fk = new ForeignKeyTable();
//                    string thisTable = rdr["ThisTable"].ToString();

//                    if (tableName.ToLower() == thisTable.ToLower())
//                    {
//                        fk.ThisTable = rdr["ThisTable"].ToString();
//                        fk.ThisColumn = rdr["ThisColumn"].ToString();
//                        fk.OtherTable = rdr["OtherTable"].ToString();
//                        fk.OtherColumn = rdr["OtherColumn"].ToString();

//                    }
//                    else
//                    {
//                        fk.ThisTable = rdr["OtherTable"].ToString();
//                        fk.ThisColumn = rdr["OtherColumn"].ToString();
//                        fk.OtherTable = rdr["ThisTable"].ToString();
//                        fk.OtherColumn = rdr["ThisColumn"].ToString();

//                    }

//                    fk.OtherClass = Inflector.MakeSingular(DsgUtil.CleanUp(fk.OtherTable, CleanUpSubject.Table));
//                    fk.OtherQueryable = Inflector.MakePlural(fk.OtherClass);

//                    result.Add(fk);
//                }
//            }
//            return result;

//        }

//        private Type GetClrType(string sqlTypeString)
//        {
//            return DsgUtil.ConvertDbTypeToClrType(GetDbType(sqlTypeString));
//        }

//        private DbType GetDbType(string sqlTypeString)
//        {
//            switch (sqlTypeString)
//            {
//                case "char": return DbType.AnsiStringFixedLength;
//                case "varchar": return DbType.AnsiString;
//                case "text": return DbType.AnsiString;

//                case "nchar": return DbType.String;
//                case "nvarchar": return DbType.String;
//                case "ntext": return DbType.String;
//                case "sql_variant": return DbType.String;
//                case "sysname": return DbType.String;

//                case "xml": return DbType.Xml;

//                case "bit": return DbType.Boolean;
//                case "tinyint": return DbType.Byte;
//                case "smallint": return DbType.Int16;
//                case "int": return DbType.Int32;
//                case "bigint": return DbType.Int64;

//                case "real": return DbType.Single;
//                case "float": return DbType.Double;
//                case "decimal": return DbType.Decimal;
//                case "numeric": return DbType.VarNumeric;
//                case "money": return DbType.Currency;
//                case "smallmoney": return DbType.Currency;

//                case "datetime": return DbType.DateTime;
//                case "smalldatetime": return DbType.DateTime;
//                case "timestamp": return DbType.Binary;

//                case "uniqueidentifier": return DbType.Guid;

//                case "binary": return DbType.Binary;
//                case "varbinary": return DbType.Binary;
//                case "image": return DbType.Binary;

//                default: throw new Exception("SqlServerRetriever doesn't know how to convert SQL type \"{0}\" to an ADO.NET type.".Fmt(sqlTypeString));
//            }
//        }
    }
}

