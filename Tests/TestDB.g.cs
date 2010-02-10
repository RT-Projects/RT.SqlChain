using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using RT.SqlChain;
using RT.SqlChain.Schema;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace SqlChainTests
{
    /// <summary>
    /// Represents a single connection to the TestDB database and
    /// provides properties and methods to query and modify this database. See
    /// Remarks for general suggestions on using this class.
    /// </summary>
    /// <remarks>
    /// <para>General database utility methods should accept a <see cref="TestDB.Transaction"/>
    /// object, and make changes through that. On failure such methods should throw an exception; on success just leave normally.</para>
    /// <para>Other methods that require database access should call ExecuteInTransaction and process any exceptions as
    /// appropriate for the situation. An exception always means that the transaction did not commit.</para>
    /// <para>A method catching exceptions thrown by another method taking a <see cref="TestDB.Transaction"/>
    /// object should not let the transaction commit as a general rule, by rethrowing the caught or a new exception.</para>
    /// </remarks>
    sealed partial class TestDB : IDisposable
    {
        /// <summary>
        /// Holds the underlying IQToolkit database connection. This is private to ensure that it is only possible to
        /// alter the database through a supported <see cref="Transaction"/> object.
        /// </summary>
        private DbEntityProvider dbProvider { get; set; }

        /// <summary>
        /// Gets the <see cref="ConnectionInfo"/> instance used to instantiate this database connection.
        /// </summary>
        public ConnectionInfo ConnectionInfo { get; private set; }

        /// <summary>
        /// Creates a new connection to the TestDB database. The connection is
        /// created immediately, and is held open until <see cref="Dispose"/> is called. IMPORTANT:
        /// this connection object is not thread-safe and must only be used on one thread.
        /// </summary>
        public TestDB(ConnectionInfo connInfo)
        {
            ConnectionInfo = connInfo;
            dbProvider = connInfo.CreateEntityProvider(connInfo.CreateConnection(), typeof(Transaction));
            dbProvider.StartUsingConnection();
        }

        /// <summary>
        /// Call this method explicitly or via a "using" clause to close this connection to the TestDB
        /// database.
        /// </summary>
        public void Dispose()
        {
            if (dbProvider != null)
            {
                dbProvider.StopUsingConnection();
                dbProvider = null;
            }
        }

        /// <summary>
        /// Gets or sets a class used for logging all executed queries. Set to null to disable logging.
        /// </summary>
        public TextWriter Log
        {
            get { return dbProvider.Log; }
            set { dbProvider.Log = value; }
        }

        /// <summary>
        /// Creates the entire schema from scratch using this connection. This method will fail if any of the
        /// objects that need to be created already exist. No other activity must occur on the database while
        /// this method is running.
        /// </summary>
        public static void CreateSchema(ConnectionInfo connectionInfo)
        {
            var xml = XElement.Parse(_schemaAsXml);
            var schema = XmlClassify.ObjectFromXElement<SchemaInfo>(xml);
            schema.XmlDeclassifyFixup();
            using (var conn = connectionInfo.CreateConnectionForSchemaCreation())
            {
                conn.Open();
                var mutator = connectionInfo.CreateSchemaMutator(conn, false);
                mutator.CreateSchema(schema);
            }
        }

        /// <summary>
        /// Returns the SQL that would be run to create the entire schema from scratch. This method
        /// could potentially have some side-effects; it is intended for debugging purposes only and
        /// should not be used in production code.
        /// </summary>
        public static string CreateSchemaSqlOnly(ConnectionInfo connectionInfo)
        {
            var xml = XElement.Parse(_schemaAsXml);
            var schema = XmlClassify.ObjectFromXElement<SchemaInfo>(xml);
            schema.XmlDeclassifyFixup();
            using (var conn = connectionInfo.CreateConnectionForSchemaCreation())
            {
                conn.Open();
                var mutator = connectionInfo.CreateSchemaMutator(conn, true);
                using (var sql = new MemoryStream())
                {
                    using (var log = new StreamWriter(sql))
                    {
                        mutator.Log = log;
                        mutator.CreateSchema(schema);
                    }
                    return sql.ToArray().FromUtf8();
                }
            }
        }

        #region Record types (one for each table)

        /// <summary>
        /// Represents a single row of the AllTypesNotNulls table. The instances of this class are
        /// in no way "connected" to the table, and simply hold the row data.
        /// </summary>
        public partial class AllTypesNotNull
        {
            public long ColAutoincrement { get; set; }
            public string ColVarText1 { get; set; }
            public string ColVarText100 { get; set; }
            public string ColVarTextMax { get; set; }
            public byte[] ColVarBinary1 { get; set; }
            public byte[] ColVarBinary100 { get; set; }
            public byte[] ColVarBinaryMax { get; set; }
            public string ColFixText5 { get; set; }
            public byte[] ColFixBinary5 { get; set; }
            public bool ColBoolean { get; set; }
            public byte ColByte { get; set; }
            public short ColShort { get; set; }
            public int ColInt { get; set; }
            public long ColLong { get; set; }
            public double ColDouble { get; set; }
            public DateTime ColDateTime { get; set; }

            /// <summary>
            /// Implement this partial method to define a custom <see cref="ToString"/> conversion.
            /// </summary>
            partial void ToStringCustom(ref string result);

            public override string ToString()
            {
                string custom = null;
                ToStringCustom(ref custom);
                if (custom != null)
                    return custom;

                var result = new StringBuilder();
                result.Append("<AllTypesNotNull");
                result.Append(" ColAutoincrement='"); result.Append(ColAutoincrement); result.Append('\'');
                result.Append(" ColVarText1='"); result.Append(ColVarText1); result.Append('\'');
                result.Append(" ColVarText100='"); result.Append(ColVarText100); result.Append('\'');
                result.Append(" ColVarTextMax='"); result.Append(ColVarTextMax); result.Append('\'');
                result.Append(" ColVarBinary1='"); result.Append(ColVarBinary1); result.Append('\'');
                result.Append(" ColVarBinary100='"); result.Append(ColVarBinary100); result.Append('\'');
                result.Append(" ColVarBinaryMax='"); result.Append(ColVarBinaryMax); result.Append('\'');
                result.Append(" ColFixText5='"); result.Append(ColFixText5); result.Append('\'');
                result.Append(" ColFixBinary5='"); result.Append(ColFixBinary5); result.Append('\'');
                result.Append(" ColBoolean='"); result.Append(ColBoolean); result.Append('\'');
                result.Append(" ColByte='"); result.Append(ColByte); result.Append('\'');
                result.Append(" ColShort='"); result.Append(ColShort); result.Append('\'');
                result.Append(" ColInt='"); result.Append(ColInt); result.Append('\'');
                result.Append(" ColLong='"); result.Append(ColLong); result.Append('\'');
                result.Append(" ColDouble='"); result.Append(ColDouble); result.Append('\'');
                result.Append(" ColDateTime='"); result.Append(ColDateTime); result.Append('\'');
                result.Append('>');
                return result.ToString();
            }
        }

        /// <summary>
        /// Represents a single row of the AllTypesNulls table. The instances of this class are
        /// in no way "connected" to the table, and simply hold the row data.
        /// </summary>
        public partial class AllTypesNull
        {
            public string ColVarText1 { get; set; }
            public string ColVarText100 { get; set; }
            public string ColVarTextMax { get; set; }
            public byte[] ColVarBinary1 { get; set; }
            public byte[] ColVarBinary100 { get; set; }
            public byte[] ColVarBinaryMax { get; set; }
            public string ColFixText5 { get; set; }
            public byte[] ColFixBinary5 { get; set; }
            public bool? ColBoolean { get; set; }
            public byte? ColByte { get; set; }
            public short? ColShort { get; set; }
            public int? ColInt { get; set; }
            public long? ColLong { get; set; }
            public double? ColDouble { get; set; }
            public DateTime? ColDateTime { get; set; }

            /// <summary>
            /// Implement this partial method to define a custom <see cref="ToString"/> conversion.
            /// </summary>
            partial void ToStringCustom(ref string result);

            public override string ToString()
            {
                string custom = null;
                ToStringCustom(ref custom);
                if (custom != null)
                    return custom;

                var result = new StringBuilder();
                result.Append("<AllTypesNull");
                result.Append(" ColVarText1='"); result.Append(ColVarText1); result.Append('\'');
                result.Append(" ColVarText100='"); result.Append(ColVarText100); result.Append('\'');
                result.Append(" ColVarTextMax='"); result.Append(ColVarTextMax); result.Append('\'');
                result.Append(" ColVarBinary1='"); result.Append(ColVarBinary1); result.Append('\'');
                result.Append(" ColVarBinary100='"); result.Append(ColVarBinary100); result.Append('\'');
                result.Append(" ColVarBinaryMax='"); result.Append(ColVarBinaryMax); result.Append('\'');
                result.Append(" ColFixText5='"); result.Append(ColFixText5); result.Append('\'');
                result.Append(" ColFixBinary5='"); result.Append(ColFixBinary5); result.Append('\'');
                result.Append(" ColBoolean='"); result.Append(ColBoolean); result.Append('\'');
                result.Append(" ColByte='"); result.Append(ColByte); result.Append('\'');
                result.Append(" ColShort='"); result.Append(ColShort); result.Append('\'');
                result.Append(" ColInt='"); result.Append(ColInt); result.Append('\'');
                result.Append(" ColLong='"); result.Append(ColLong); result.Append('\'');
                result.Append(" ColDouble='"); result.Append(ColDouble); result.Append('\'');
                result.Append(" ColDateTime='"); result.Append(ColDateTime); result.Append('\'');
                result.Append('>');
                return result.ToString();
            }
        }

        #endregion

        /// <summary>
        /// Executes the specified code using a new database transaction. There is no way to make changes
        /// to the database other than through a method like this one. See Remarks for more info.
        /// </summary>
        /// <remarks>
        /// <para>See Remarks on the connection class (<see cref="TestDB"/>) for general suggestions.</para>
        /// <para>To commit the transaction, let <paramref name="action"/> return normally.</para>
        /// <para>To rollback the transaction, let <paramref name="action"/> throw an exception. This exception will
        /// NOT be caught by <see cref="ExecuteInTransaction"/>, so make sure you handle any exceptions escaping this call as appropriate.</para>
        /// </remarks>
        public void ExecuteInTransaction(Action<Transaction> action)
        {
            if (dbProvider.Transaction != null)
                throw new InvalidOperationException("Another transaction is already active; cannot have more than one active transaction on the same connection.");
            try
            {
                dbProvider.Transaction = dbProvider.Connection.BeginTransaction();
                action(new Transaction(dbProvider));
                dbProvider.Transaction.Commit();
                dbProvider.Transaction = null;
            }
            finally
            {
                if (dbProvider.Transaction != null)
                {
                    dbProvider.Transaction.Rollback();
                    dbProvider.Transaction = null;
                }
            }
        }

        /// <summary>
        /// Executes the specified code using a new database transaction. There is no way to make changes
        /// to the database other than through a method like this one. See Remarks for more info.
        /// </summary>
        /// <remarks>
        /// <para>See Remarks on the connection class (<see cref="TestDB"/>) for general suggestions.</para>
        /// <para>To commit the transaction, let <paramref name="action"/> return normally.</para>
        /// <para>To rollback the transaction, let <paramref name="action"/> throw an exception. This exception will
        /// NOT be caught by <see cref="ExecuteInTransaction"/>, so make sure you handle any exceptions escaping this call as appropriate.</para>
        /// </remarks>
        public TResult ExecuteInTransaction<TResult>(Func<Transaction, TResult> func)
        {
            if (dbProvider.Transaction != null)
                throw new InvalidOperationException("Another transaction is already active; cannot have more than one active transaction on the same connection.");
            try
            {
                dbProvider.Transaction = dbProvider.Connection.BeginTransaction();
                var result = func(new Transaction(dbProvider));
                dbProvider.Transaction.Commit();
                dbProvider.Transaction = null;
                return result;
            }
            finally
            {
                if (dbProvider.Transaction != null)
                {
                    dbProvider.Transaction.Rollback();
                    dbProvider.Transaction = null;
                }
            }
        }

        public sealed partial class Transaction
        {
            /// <summary>
            /// Gets the underlying database connection, which exposes numerous useful methods.
            /// </summary>
            public DbEntityProvider DbProvider { get; private set; }

            /// <summary>
            /// This constructor is not intended to be used by clients of this class.
            /// </summary>
            public Transaction(DbEntityProvider dbProvider)
            {
                DbProvider = dbProvider;
            }

            /// <summary>
            /// Provides methods to query and modify the AllTypesNotNulls table of the TestDB database.
            /// </summary>
            [Table(Name ="AllTypesNotNull")]
            [Column(Member = "ColAutoincrement", Name = "ColAutoincrement", IsPrimaryKey = true)]
            [Column(Member = "ColVarText1", Name = "ColVarText1")]
            [Column(Member = "ColVarText100", Name = "ColVarText100")]
            [Column(Member = "ColVarTextMax", Name = "ColVarTextMax")]
            [Column(Member = "ColVarBinary1", Name = "ColVarBinary1")]
            [Column(Member = "ColVarBinary100", Name = "ColVarBinary100")]
            [Column(Member = "ColVarBinaryMax", Name = "ColVarBinaryMax")]
            [Column(Member = "ColFixText5", Name = "ColFixText5")]
            [Column(Member = "ColFixBinary5", Name = "ColFixBinary5")]
            [Column(Member = "ColBoolean", Name = "ColBoolean")]
            [Column(Member = "ColByte", Name = "ColByte")]
            [Column(Member = "ColShort", Name = "ColShort")]
            [Column(Member = "ColInt", Name = "ColInt")]
            [Column(Member = "ColLong", Name = "ColLong")]
            [Column(Member = "ColDouble", Name = "ColDouble")]
            [Column(Member = "ColDateTime", Name = "ColDateTime")]
            public IEntityTable<AllTypesNotNull> AllTypesNotNulls
            {
                get { return DbProvider.GetTable<AllTypesNotNull>("AllTypesNotNulls"); }
            }

            /// <summary>
            /// Provides methods to query and modify the AllTypesNulls table of the TestDB database.
            /// </summary>
            [Table(Name ="AllTypesNull")]
            [Column(Member = "ColVarText1", Name = "ColVarText1")]
            [Column(Member = "ColVarText100", Name = "ColVarText100")]
            [Column(Member = "ColVarTextMax", Name = "ColVarTextMax")]
            [Column(Member = "ColVarBinary1", Name = "ColVarBinary1")]
            [Column(Member = "ColVarBinary100", Name = "ColVarBinary100")]
            [Column(Member = "ColVarBinaryMax", Name = "ColVarBinaryMax")]
            [Column(Member = "ColFixText5", Name = "ColFixText5")]
            [Column(Member = "ColFixBinary5", Name = "ColFixBinary5")]
            [Column(Member = "ColBoolean", Name = "ColBoolean")]
            [Column(Member = "ColByte", Name = "ColByte")]
            [Column(Member = "ColShort", Name = "ColShort")]
            [Column(Member = "ColInt", Name = "ColInt")]
            [Column(Member = "ColLong", Name = "ColLong")]
            [Column(Member = "ColDouble", Name = "ColDouble")]
            [Column(Member = "ColDateTime", Name = "ColDateTime")]
            public IEntityTable<AllTypesNull> AllTypesNulls
            {
                get { return DbProvider.GetTable<AllTypesNull>("AllTypesNulls"); }
            }

            /// <summary>Executes the specified SQL command.</summary>
            public int ExecuteCommand(string sql)
            {
                return DbProvider.ExecuteCommand(sql);
            }

            /// <summary>Executes the specified SQL command with the specified parameter values.</summary>
            public int ExecuteCommand(string sql, params object[] paramValues)
            {
                var cmd = new QueryCommand(sql, new QueryParameter[0]);
                return DbProvider.ExecuteCommand(cmd, paramValues);
            }
        }

        private static string _schemaAsXml = @"
<item>
  <tables>
    <item>
      <columns>
        <item>
          <Name>ColAutoincrement</Name>
          <Type>
            <BasicType>Long</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColVarText1</Name>
          <Type>
            <BasicType>VarText</BasicType>
            <Nullable>False</Nullable>
            <Length>1</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarText100</Name>
          <Type>
            <BasicType>VarText</BasicType>
            <Nullable>False</Nullable>
            <Length>100</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarTextMax</Name>
          <Type>
            <BasicType>VarText</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColVarBinary1</Name>
          <Type>
            <BasicType>VarBinary</BasicType>
            <Nullable>False</Nullable>
            <Length>1</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarBinary100</Name>
          <Type>
            <BasicType>VarBinary</BasicType>
            <Nullable>False</Nullable>
            <Length>100</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarBinaryMax</Name>
          <Type>
            <BasicType>VarBinary</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColFixText5</Name>
          <Type>
            <BasicType>FixText</BasicType>
            <Nullable>False</Nullable>
            <Length>5</Length>
          </Type>
        </item>
        <item>
          <Name>ColFixBinary5</Name>
          <Type>
            <BasicType>FixBinary</BasicType>
            <Nullable>False</Nullable>
            <Length>5</Length>
          </Type>
        </item>
        <item>
          <Name>ColBoolean</Name>
          <Type>
            <BasicType>Boolean</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColByte</Name>
          <Type>
            <BasicType>Byte</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColShort</Name>
          <Type>
            <BasicType>Short</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColInt</Name>
          <Type>
            <BasicType>Int</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColLong</Name>
          <Type>
            <BasicType>Long</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColDouble</Name>
          <Type>
            <BasicType>Double</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColDateTime</Name>
          <Type>
            <BasicType>DateTime</BasicType>
            <Nullable>False</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
      </columns>
      <indexes>
        <item>
          <Name>sqlite_master_PK_AllTypesNotNull</Name>
          <ColumnNames>
            <item>ColAutoincrement</item>
          </ColumnNames>
          <Kind>PrimaryKey</Kind>
        </item>
      </indexes>
      <foreignKeys />
      <Name>AllTypesNotNull</Name>
    </item>
    <item>
      <columns>
        <item>
          <Name>ColVarText1</Name>
          <Type>
            <BasicType>VarText</BasicType>
            <Nullable>True</Nullable>
            <Length>1</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarText100</Name>
          <Type>
            <BasicType>VarText</BasicType>
            <Nullable>True</Nullable>
            <Length>100</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarTextMax</Name>
          <Type>
            <BasicType>VarText</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColVarBinary1</Name>
          <Type>
            <BasicType>VarBinary</BasicType>
            <Nullable>True</Nullable>
            <Length>1</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarBinary100</Name>
          <Type>
            <BasicType>VarBinary</BasicType>
            <Nullable>True</Nullable>
            <Length>100</Length>
          </Type>
        </item>
        <item>
          <Name>ColVarBinaryMax</Name>
          <Type>
            <BasicType>VarBinary</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColFixText5</Name>
          <Type>
            <BasicType>FixText</BasicType>
            <Nullable>True</Nullable>
            <Length>5</Length>
          </Type>
        </item>
        <item>
          <Name>ColFixBinary5</Name>
          <Type>
            <BasicType>FixBinary</BasicType>
            <Nullable>True</Nullable>
            <Length>5</Length>
          </Type>
        </item>
        <item>
          <Name>ColBoolean</Name>
          <Type>
            <BasicType>Boolean</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColByte</Name>
          <Type>
            <BasicType>Byte</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColShort</Name>
          <Type>
            <BasicType>Short</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColInt</Name>
          <Type>
            <BasicType>Int</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColLong</Name>
          <Type>
            <BasicType>Long</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColDouble</Name>
          <Type>
            <BasicType>Double</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
        <item>
          <Name>ColDateTime</Name>
          <Type>
            <BasicType>DateTime</BasicType>
            <Nullable>True</Nullable>
            <Length null=""1"" />
          </Type>
        </item>
      </columns>
      <indexes />
      <foreignKeys />
      <Name>AllTypesNull</Name>
    </item>
  </tables>
</item>";
    }
}
