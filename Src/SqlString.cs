using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using IQToolkit;

namespace RT.SqlChain
{
    /// <summary>
    /// Helps construct SQL query strings for various DBMSs.
    /// </summary>
    public abstract class SqlStringManipulator
    {
        /// <summary>
        /// Constructs an SQL query string from the various pieces passed in by concatenating them all together, separated with spaces.
        /// Strings are concatenated as-is; numbers and datetimes are converted to corresponding literals; null values are converted to
        /// the "NULL" literal, and column and table references are converted to an escaped string representing their name.
        /// Use <see cref="MakeStringLiteral"/> in order to add a string literal to the query string.
        /// </summary>
        public string MakeSql(params object[] values)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var value in values)
            {
                if (first)
                    first = false;
                else
                    sb.Append(' ');

                if (value == null)
                    sb.Append("NULL");
                else if (value is string)
                    sb.Append(value as string);
                else if (value is StaticTableInfo)
                    sb.Append(MakeTableName(((StaticTableInfo) value).SqlName));
                else if (value is StaticColumnInfo)
                    sb.Append(MakeTableName(((StaticColumnInfo) value).Table.SqlName) + "." + MakeColumnName(((StaticColumnInfo) value).SqlName));
                else if (value is float)
                    sb.Append(MakeNumericLiteral((float) value));
                else if (value is double)
                    sb.Append(MakeNumericLiteral((double) value));
                else if (value is DateTime)
                    sb.Append(MakeDatetimeLiteral((DateTime) value));
                else
                    sb.Append(value.ToString());
            }
            return sb.ToString();
        }

        /// <summary>Takes a table name and creates a "table name literal", escaping and surrounding as appropriate.</summary>
        public virtual string MakeTableName(string name)
        {
            return "[" + name.Replace(@"\", @"\\").Replace(@"]", @"\]") + "]";
        }

        /// <summary>Takes a column name and creates a "column name literal", escaping and surrounding as appropriate.</summary>
        public virtual string MakeColumnName(string name)
        {
            return "[" + name.Replace(@"\", @"\\").Replace(@"]", @"\]") + "]";
        }

        /// <summary>Takes a string value and creates a string literal, escaping and surrounding as appropriate.</summary>
        public virtual string MakeStringLiteral(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        /// <summary>Takes a number and creates a numeric literal, converting the number to a string as appropriate.</summary>
        public virtual string MakeNumericLiteral(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>Takes a number and creates a date/time literal, converting and surrounding as appropriate.</summary>
        public virtual string MakeDatetimeLiteral(DateTime value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Helps construct SQL query strings for Sqlite.
    /// </summary>
    public class SqliteStringManipulator : SqlStringManipulator
    {
    }

    /// <summary>
    /// Helps construct SQL query strings for SQL Server.
    /// </summary>
    public class SqlServerStringManipulator : SqlStringManipulator
    {
        public override string MakeDatetimeLiteral(DateTime value)
        {
            return "{ts '{0:0000}-{1:00}-{2:00} {3:00}:{4:00}:{5:00.000}'}".Fmt(value.Year, value.Month, value.Day, value.Hour, value.Minute, (double) value.Second + (double) value.Millisecond / 1000);
        }
    }
}
