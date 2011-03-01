using System;
using System.Linq;
using System.Collections.Generic;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;
using RT.Util;

#pragma warning disable 1591

namespace RT.SqlChain.Schema
{
    public enum BasicType
    {
        VarText,
        VarBinary,
        Boolean,
        Autoincrement,
        Byte,
        Short,
        Int,
        Long,
        Double,
        DateTime,
    }

    public enum IndexKind
    {
        Normal,
        Unique,
        PrimaryKey,
    }

    public class TypeInfo
    {
        /// <summary>
        /// Specifies one of the fundamental underlying data types.
        /// </summary>
        public BasicType BasicType { get; set; }
        /// <summary>
        /// Specifies whether the type is nullable.
        /// </summary>
        public bool Nullable { get; set; }
        /// <summary>
        /// For VarText and VarBinary, specifies the length in characters/bytes, or null if the longest possible value is used.
        /// For all other types, always null.
        /// </summary>
        public int? Length { get; set; }

        public string ToCsTypeString()
        {
            switch (BasicType)
            {
                case BasicType.VarText: return "string";
                case BasicType.VarBinary: return "byte[]";
                case BasicType.Boolean: return "bool" + (Nullable ? "?" : "");
                case BasicType.Autoincrement: return "long" + (Nullable ? "?" : "");
                case BasicType.Byte: return "byte" + (Nullable ? "?" : "");
                case BasicType.Short: return "short" + (Nullable ? "?" : "");
                case BasicType.Int: return "int" + (Nullable ? "?" : "");
                case BasicType.Long: return "long" + (Nullable ? "?" : "");
                case BasicType.Double: return "double" + (Nullable ? "?" : "");
                case BasicType.DateTime: return "DateTime" + (Nullable ? "?" : "");
                default:
                    throw new Exception("Unknown BasicType");
            }
        }

        public bool IsForeignKeyCompatibleWith(TypeInfo other)
        {
            if (this.BasicType == BasicType.Autoincrement && other.BasicType != BasicType.Long && other.BasicType != BasicType.Autoincrement)
                return false;
            else if (other.BasicType == BasicType.Autoincrement && this.BasicType != BasicType.Long && this.BasicType != BasicType.Autoincrement)
                return false;
            else if (this.BasicType != other.BasicType && this.BasicType != BasicType.Autoincrement && other.BasicType != BasicType.Autoincrement)
                return false;
            // basic types are compatible
            else if (this.Length != other.Length)
                return false;
            // lengths are compatible
            else
                return true;
        }

        public void Validate()
        {
            if (BasicType != BasicType.VarText && BasicType != BasicType.VarBinary && Length != null)
                throw new SchemaValidationException("The Length property must be null for the basic type {0}".Fmt(BasicType));
        }

        public override string ToString()
        {
            return "Type: {0}, {1}{2}".Fmt(BasicType, Nullable ? "NULL" : "NOT NULL", Length == null ? "" : ", len={0}".Fmt(Length.Value));
        }
    }

    public class SchemaInfo
    {
        private List<TableInfo> _tables = new List<TableInfo>();

        /// <summary>
        /// Enumerates all tables in this schema in no particular order.
        /// </summary>
        public IEnumerable<TableInfo> Tables
        {
            get { return _tables.AsReadOnly(); }
        }

        /// <summary>
        /// Returns the specified table or throws an exception if it cannot be found.
        /// </summary>
        public TableInfo Table(string tableName)
        {
            var result = _tables.FirstOrDefault(t => t.Name.EqualsNoCase(tableName));
            if (result == null)
                throw new KeyNotFoundException("Table [{0}] does not exist in this schema.".Fmt(tableName));
            else
                return result;
        }

        public void AddTable(TableInfo table)
        {
            if (table.Schema != null)
                throw new InvalidOperationException("Table [{0}] is already a member of another schema.".Fmt(table.Name));
            if (_tables.Any(t => t.Name.Equals(table.Name)))
                throw new InvalidOperationException("A table named [{0}] already exists in this schema.".Fmt(table.Name));

            var existingIndexes = _tables.SelectMany(t => t.Indexes).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var index in table.Indexes)
                if (existingIndexes.ContainsKey(index.Name))
                    throw new InvalidOperationException("Index [{0}] on table [{1}] has the same name as an index on table [{2}]".Fmt(index.Name, table.Name, existingIndexes[index.Name].TableName));

            var existingForeignKeys = _tables.SelectMany(t => t.ForeignKeys).ToDictionary(fk => fk.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var foreignKey in table.ForeignKeys)
                if (existingForeignKeys.ContainsKey(foreignKey.Name))
                    throw new InvalidOperationException("Foreign key [{0}] on table [{1}] has the same name as a foreign key on table [{2}]".Fmt(foreignKey.Name, table.Name, existingForeignKeys[foreignKey.Name].TableName));

            table.Schema = this;
            _tables.Add(table);
        }

        public TableInfo RemoveTable(string tableName)
        {
            for (int i = 0; i < _tables.Count; i++)
                if (_tables[i].Name.EqualsNoCase(tableName))
                {
                    var tbl = _tables[i];
                    tbl.Schema = null;
                    _tables.RemoveAt(i);
                    return tbl;
                }
            return null;
        }

        /// <summary>
        /// Enumerates all the indexes defined in this schema, in no particular order.
        /// </summary>
        public IEnumerable<IndexInfo> Indexes
        {
            get
            {
                foreach (var table in _tables)
                    foreach (var index in table.Indexes)
                        yield return index;
            }
        }

        /// <summary>
        /// Enumerates all the foreign keys defined in this schema, in no particular order.
        /// </summary>
        public IEnumerable<ForeignKeyInfo> ForeignKeys
        {
            get
            {
                foreach (var table in _tables)
                    foreach (var key in table.ForeignKeys)
                        yield return key;
            }
        }

        public void XmlDeclassifyFixup(DbEngine supportedEngines)
        {
            foreach (var table in _tables)
                table.XmlDeclassifyFixup();

            var tables = _tables.ToArray();
            _tables.Clear();
            foreach (var table in tables)
                AddTable(table);

            Validate(supportedEngines);
        }

        public void Validate(DbEngine supportedEngines)
        {
            // All table names must be distinct
            var set = new HashSet<string>();
            foreach (var table in Tables)
                if (!set.Add(table.Name))
                    throw new SchemaValidationException("Duplicate table name: [{0}].".Fmt(table.Name));

            set.Clear();
            foreach (var fk in ForeignKeys)
            {
                // All foreign key names must be distinct
                if (!set.Add(fk.Name))
                    throw new SchemaValidationException("Duplicate foreign-key name: [{0}].".Fmt(fk.Name));
                
                // Foreign key columns must be the same type as the referenced columns
                foreach (var cols in fk.Columns.Zip(fk.ReferencedColumns, (c, refc) => new { Column = c, ReferencedColumn = refc }))
                    if (!cols.Column.Type.IsForeignKeyCompatibleWith(cols.ReferencedColumn.Type))
                        throw new SchemaValidationException("Foreign key [{0}] ([{1}] => [{2}]): columns [{3}] => [{4}] use types incompatible for foreign key purposes in some DBMSs ({5} vs {6}).".Fmt(fk.Name, fk.Table.Name, fk.ReferencedTable.Name, cols.Column.Name, cols.ReferencedColumn.Name, cols.Column.Type, cols.ReferencedColumn.Type));
            }

            set.Clear();
            foreach (var index in Indexes)
            {
                // All index names must be distinct
                if (!set.Add(index.Name))
                    throw new SchemaValidationException("Duplicate index name: [{0}].".Fmt(index.Name));
                
                // Indexes not allowed on certain column types
                foreach (var col in index.Columns)
                {
                    if (supportedEngines.HasFlag(DbEngine.SqlServer))
                    {
                        // MS SQL Server cannot index NVAR*(MAX) columns
                        if ((col.Type.BasicType == BasicType.VarText || col.Type.BasicType == BasicType.VarBinary) && col.Type.Length == null)
                            throw new SchemaValidationException("Index [{0}] on table [{1}] references column [{2}], which is of type {3} with maximum length. MS SQL Server cannot index such columns.".Fmt(index.Name, index.Table.Name, col.Name, col.Type.BasicType));
                    }
                }
            }
        }

        public override string ToString()
        {
            return "<SchemaInfo: {0} table(s)>".Fmt(_tables.Count);
        }
    }

    public class TableInfo
    {
        private List<ColumnInfo> _columns = new List<ColumnInfo>();
        private List<IndexInfo> _indexes = new List<IndexInfo>();
        private List<ForeignKeyInfo> _foreignKeys = new List<ForeignKeyInfo>();

        /// <summary>
        /// If the table is a member of a schema, this is a reference to that schema. Otherwise null.
        /// Set automatically by the <see cref="SchemaInfo"/> class when necessary.
        /// </summary>
        [XmlIgnore]
        public SchemaInfo Schema { get; set; }

        /// <summary>
        /// Gets/sets the name of this table.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the primary key index if one is defined for this table, or null otherwise.
        /// </summary>
        public IndexInfo PrimaryKey
        {
            get { return _indexes.FirstOrDefault(i => i.Kind == IndexKind.PrimaryKey); }
        }

        /// <summary>
        /// Enumerates all unique constraint indexes defined on this table.
        /// </summary>
        public IEnumerable<IndexInfo> UniqueConstraints
        {
            get { return _indexes.Where(i => i.Kind == IndexKind.Unique); }
        }

        /// <summary>
        /// Enumerates all columns of this table.
        /// </summary>
        public IEnumerable<ColumnInfo> Columns
        {
            get { return _columns.AsReadOnly(); }
        }

        public ColumnInfo Column(string columnName)
        {
            var result = _columns.FirstOrDefault(c => c.Name.EqualsNoCase(columnName));
            if (result == null)
                throw new KeyNotFoundException("Column [{0}] does not exist in table [{1}].".Fmt(columnName, Name));
            else
                return result;
        }

        public void AddColumn(ColumnInfo column)
        {
            if (column.Table != null)
                throw new InvalidOperationException("Column [{0}] is already a member of a table named [{1}].".Fmt(column.Name, column.Table.Name));
            if (_columns.Any(c => c.Name.EqualsNoCase(column.Name)))
                throw new InvalidOperationException("A column named [{0}] already exists in table [{1}].".Fmt(column.Name, Name));
            column.Table = this;
            _columns.Add(column);
        }

        public ColumnInfo RemoveColumn(string columnName)
        {
            for (int i = 0; i < _columns.Count; i++)
                if (_columns[i].Name.EqualsNoCase(columnName))
                {
                    var col = _columns[i];
                    col.Table = null;
                    _columns.RemoveAt(i);
                    return col;
                }
            return null;
        }

        /// <summary>
        /// Enumerates all indexes of this table (including PrimaryKey, Unique and other).
        /// </summary>
        public IEnumerable<IndexInfo> Indexes
        {
            get { return _indexes.AsReadOnly(); }
        }

        public void AddIndex(IndexInfo index)
        {
            if (index.Table != null)
                throw new InvalidOperationException("Index [{0}] is already a member of a table named [{1}].".Fmt(index.Name, index.Table.Name));
            // Check for name uniqueness within the table; adding this to a schema will check for overall uniqueness
            if (_indexes.Any(i => i.Name.EqualsNoCase(index.Name)))
                throw new InvalidOperationException("Index [{0}] has the same name as another index on table [{1}].".Fmt(index.Name, Name));
            index.Table = this;
            _indexes.Add(index);
        }

        /// <summary>Enumerates all foreign keys of this table.</summary>
        public IEnumerable<ForeignKeyInfo> ForeignKeys
        {
            get { return _foreignKeys.AsReadOnly(); }
        }

        public void AddForeignKey(ForeignKeyInfo foreignKey)
        {
            if (foreignKey.Table != null)
                throw new InvalidOperationException("Foreign key [{0}] is already a member of a table named [{1}].".Fmt(foreignKey.Name, foreignKey.Table.Name));
            // Check for name uniqueness within the table; adding this to a schema will check for overall uniqueness
            if (_foreignKeys.Any(fk => fk.Name.EqualsNoCase(foreignKey.Name)))
                throw new InvalidOperationException("Foreign key [{0}] has the same name as another foreign key on table [{1}].".Fmt(foreignKey.Name, Name));
            foreignKey.Table = this;
            _foreignKeys.Add(foreignKey);
        }

        public void XmlDeclassifyFixup()
        {
            var columns = _columns.ToArray();
            _columns.Clear();
            foreach (var column in columns)
                AddColumn(column);

            var indexes = _indexes.ToArray();
            _indexes.Clear();
            foreach (var index in indexes)
                AddIndex(index);

            var foreignKeys = _foreignKeys.ToArray();
            _foreignKeys.Clear();
            foreach (var foreignKey in foreignKeys)
                AddForeignKey(foreignKey);

            Validate();
        }

        public void Validate()
        {
            // Must have a name
            if (Name == null)
                throw new SchemaValidationException("Table name must not be null");
            // Must have at least one column
            if (_columns.Count == 0)
                throw new SchemaValidationException("{0} has no columns defined.".Fmt(this));
            // Column names must be distinct (within this table only)
            // TODO
            // Columns must validate
            foreach (var column in _columns)
                column.Validate();
            // Index names must be distinct (check this table; schema validation, if any, will check overall)
            // TODO
            // Indexes must validate
            foreach (var index in _indexes)
                index.Validate();
            // Foreign Key names must be distinct (check this table; schema validation, if any, will check overall)
            // TODO
            // Foreign Keys must validate
            foreach (var foreignKey in _foreignKeys)
                foreignKey.Validate();
            // Only a single primary key autoincrement allowed
            // TODO
        }

        public override string ToString()
        {
            return "<TableInfo: {0}, {1} column(s)>".Fmt(Name, _columns.Count());
        }
    }

    /// <summary>
    /// An experimental base class to share a few things that distinguish items that belong to a table.
    /// </summary>
    public class BelongsToTable
    {
        [XmlIgnore]
        private string _tableName;

        /// <summary>
        /// If this item is a member of a table, this is a reference to that table. Otherwise null.
        /// Set automatically by the <see cref="TableInfo"/> class when necessary.
        /// </summary>
        [XmlIgnore]
        public TableInfo Table { get; set; }

        public string TableName
        {
            get
            {
                if (Table != null)
                    return Table.Name;
                else
                    return _tableName;
            }
            set
            {
                if (Table != null)
                    throw new InvalidOperationException("Cannot set TableName directly because this item is currently associated with an actual TableInfo instance.");
                else
                    _tableName = value;
            }
        }

        /// <summary>The name of this item.</summary>
        public string Name { get; set; }

        /// <summary>
        /// Verifies that this class is consistent. Throws a <see cref="SchemaValidationException"/> if an
        /// inconsistency is found.
        /// </summary>
        public virtual void Validate()
        {
            // Must have a name
            if (Name == null)
                throw new SchemaValidationException("Name must not be null");
            // Must reference a table
            if (Table == null && _tableName == null)
                throw new SchemaValidationException("Both the Table and TableName of {0} are null.".Fmt(this));
        }
    }

    public class ColumnInfo : BelongsToTable
    {
        /// <summary>
        /// The type of this column.
        /// </summary>
        public TypeInfo Type { get; set; }

        public bool IsPartOfPrimaryKey
        {
            get
            {
                if (Table == null)
                    throw new InvalidOperationException("Cannot determine whether the column is part of a primary key because the column is not associated with a table.");
                else
                    return Table.PrimaryKey == null ? false : Table.PrimaryKey.ColumnNames.Any(cn => cn.EqualsNoCase(Name));
            }
        }

        /// <summary>
        /// Verifies that this class is consistent. Throws a <see cref="SchemaValidationException"/> if an
        /// inconsistency is found.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            Type.Validate();
        }

        public override string ToString()
        {
            if (Table == null)
                return "<ColumnInfo: {0}, {1} (unbound)>".Fmt(Name, Type);
            else
                return "<ColumnInfo: {0}.{1}, {2}>".Fmt(Table.Name, Name, Type);
        }
    }

    public class IndexInfo : BelongsToTable
    {
        public List<string> ColumnNames = new List<string>();
        public IndexKind Kind;

        public IEnumerable<ColumnInfo> Columns
        {
            get
            {
                if (Table == null)
                    throw new InvalidOperationException("Cannot enumerate index columns because the IndexInfo is not bound to a table. See also ColumnNames.");
                foreach (var colName in ColumnNames)
                    yield return Table.Column(colName);
            }
        }

        /// <summary>
        /// Verifies that this class is consistent. Throws a <see cref="SchemaValidationException"/> if an
        /// inconsistency is found.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            if (Table != null)
            {
                // All the columns must exist in the parent table
                foreach (var columnName in ColumnNames)
                    if (!Table.Columns.Any(c => c.Name.EqualsNoCase(columnName)))
                        throw new SchemaValidationException("{0} indexes a non-existent column [{1}]".Fmt(this, columnName));
            }
        }

        public override string ToString()
        {
            var on = "[{0}], column(s) {1}".Fmt(TableName, ColumnNames.JoinString(", ", "[", "]"));
            switch (Kind)
            {
                case IndexKind.Normal: return "<Index {0} on {1}>".Fmt(Name, on);
                case IndexKind.Unique: return "<Unique Index {0} on {1}>".Fmt(Name, on);
                case IndexKind.PrimaryKey: return "<Primary Key Index {0} on {1}>".Fmt(Name, on);
                default: throw new InternalErrorException("Unexpected IndexKind");
            }
        }
    }

    public class ForeignKeyInfo : BelongsToTable
    {
        public List<string> ColumnNames = new List<string>();
        public string ReferencedTableName;
        public List<string> ReferencedColumnNames = new List<string>();

        public IEnumerable<ColumnInfo> Columns
        {
            get
            {
                if (Table == null)
                    throw new InvalidOperationException("Cannot enumerate foreign key columns because the ForeignKeyInfo is not bound to a table. See also ColumnNames.");
                foreach (var col in ColumnNames)
                    yield return Table.Column(col);
            }
        }

        public IEnumerable<ColumnInfo> ReferencedColumns
        {
            get
            {
                if (Table == null)
                    throw new InvalidOperationException("Cannot enumerate foreign key referenced columns because the ForeignKeyInfo is not bound to a table. See also ReferencedColumnNames.");
                foreach (var col in ReferencedColumnNames)
                    yield return ReferencedTable.Column(col);
            }
        }

        public TableInfo ReferencedTable
        {
            get
            {
                return Schema.Table(ReferencedTableName);
            }
        }

        public SchemaInfo Schema
        {
            get
            {
                if (Table == null)
                    throw new InvalidOperationException("Cannot retrieve foreign key's containing schema because the ForeignKeyInfo is not bound to a table.");
                if (Table.Schema == null)
                    throw new InvalidOperationException("Cannot retrieve foreign key's containing schema because the ForeignKeyInfo's Table is not bound to a schema.");
                return Table.Schema;
            }
        }

        /// <summary>
        /// Verifies that this class is consistent. Throws a <see cref="SchemaValidationException"/> if an
        /// inconsistency is found.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            if (Table != null)
            {
                // All the columns must exist in the parent table
                foreach (var columnName in ColumnNames)
                    if (!Table.Columns.Any(c => c.Name.EqualsNoCase(columnName)))
                        throw new SchemaValidationException("{0} constrains a non-existent column [{1}]".Fmt(this, columnName));
                if (Table.Schema != null)
                {
                    // Referenced table must exist
                    var referencedTable = Table.Schema.Tables.FirstOrDefault(t => t.Name.EqualsNoCase(ReferencedTableName));
                    if (referencedTable == null)
                        throw new SchemaValidationException("{0} references a non-existent table [{1}]".Fmt(this, ReferencedTableName));
                    // All the referenced columns must exist in the referenced table
                    foreach (var referencedColumnName in ReferencedColumnNames)
                        if (!referencedTable.Columns.Any(c => c.Name.EqualsNoCase(referencedColumnName)))
                            throw new SchemaValidationException("{0} references a non-existent column [{1}]".Fmt(this, referencedColumnName));
                }
            }
        }

        public override string ToString()
        {
            return "<ForeignKey {0} ({2}) => {1} ({3})>".Fmt(TableName, ReferencedTableName, ColumnNames.JoinString(", "), ReferencedColumnNames.JoinString(", "));
        }
    }

    /// <summary>
    /// Thrown by various Validate methods to indicate that validation failed.
    /// </summary>
    public class SchemaValidationException : Exception
    {
        public SchemaValidationException(string message) : base(message) { }
    }
}
