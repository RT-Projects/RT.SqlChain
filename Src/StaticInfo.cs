using System;

namespace RT.SqlChain
{
    /// <summary>Describes a SqlChain table, for use by a SqlChain client at run-time.</summary>
    public class StaticTableInfo
    {
        /// <summary>Exact name of the table in the DBMS. Once appropriately escaped, may be used in SQL query strings to refer to this table.</summary>
        public string SqlName { get; private set; }

        /// <summary>Constructor.</summary>
        public StaticTableInfo(string sqlName)
        {
            SqlName = sqlName;
        }
    }

    /// <summary>Describes a SqlChain column, for use by a SqlChain client at run-time.</summary>
    public class StaticColumnInfo
    {
        /// <summary>Information about the table that this field is on.</summary>
        public StaticTableInfo Table { get; private set; }
        /// <summary>User-friendly name of this column.</summary>
        public string Name { get; private set; }
        /// <summary>Exact name of this column in the DBMS. Once appropriately escaped, may be used in SQL query strings to refer to this column.</summary>
        public string SqlName { get; private set; }
        /// <summary>True if this field is part of the table's primary key.</summary>
        public bool IsPartOfPrimaryKey { get; private set; }
        /// <summary>True if this field is an autoincrement field.</summary>
        public bool IsAutoIncrement { get; private set; }

        /// <summary>Constructor.</summary>
        public StaticColumnInfo(StaticTableInfo table, string name, string sqlName, bool isPartOfPrimaryKey, bool isAutoIncrement)
        {
            Table = table;
            Name = name;
            SqlName = sqlName;
            IsPartOfPrimaryKey = isPartOfPrimaryKey;
            IsAutoIncrement = isAutoIncrement;
        }
    }
}
