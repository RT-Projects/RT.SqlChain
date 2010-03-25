using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace RT.SqlChain.Schema
{
    /// <summary>Represents a change to the structure of a database table.</summary>
    public abstract class TableTransform { }

    /// <summary>Instructs a <see cref="SchemaMutator"/> to add a new column to an existing table.</summary>
    public class AddColumn : TableTransform
    {
        /// <summary>Contains information about the new column.</summary>
        public ColumnInfo NewColumn { get; set; }
        /// <summary>Specifies the index at which to insert the column. Use int.MaxValue to insert the column at the end.</summary>
        public int InsertAtIndex { get; set; }
        /// <summary>If non-null, the new column is populated with a constant or with information derived from other columns.
        /// This is expected to be a valid SQL expression which uses the table alias "oldtable" to refer to the old table columns and
        /// which returns the new value for this column. This is required for non-nullable column types.</summary>
        public string Populate { get; set; }
    }

    /// <summary>Instructs a <see cref="SchemaMutator"/> to change the order of the columns in a table.</summary>
    public class MoveColumn : TableTransform
    {
        /// <summary>Identifies the column to be moved.</summary>
        public ColumnInfo Column { get; set; }
        /// <summary>Specifies the new index at which the column should appear in the table.</summary>
        public int NewIndex { get; set; }
    }

    /// <summary>Instructs a <see cref="SchemaMutator"/> to rename an existing column in an existing table.</summary>
    public class RenameColumn : TableTransform
    {
        /// <summary>Identifies the column to be renamed.</summary>
        public ColumnInfo Column { get; set; }
        /// <summary>Specifies the new name for the column.</summary>
        public string NewName { get; set; }
    }

    /// <summary>Instructs a <see cref="SchemaMutator"/> to delete an existing column in an existing table.</summary>
    public class DeleteColumn : TableTransform
    {
        /// <summary>Identifies the column to be deleted.</summary>
        public ColumnInfo Column { get; set; }
    }
}
