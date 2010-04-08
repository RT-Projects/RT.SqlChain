using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using RT.SqlChain;
using RT.SqlChain.Schema;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.SqlChainTests
{
    /// <summary>
    /// Represents a single connection to the TestDB database and
    /// provides properties and methods to query and modify this database. See
    /// Remarks for general suggestions on using this class.
    /// </summary>
    /// <remarks>
    /// <para>General database utility methods should accept a <see cref="TestDB.Transaction"/>
    /// object, and make changes through that. On failure such methods should throw an exception; on success just leave normally.</para>
    /// <para>Other methods that require database access should call <see cref="ExecuteInTransaction"/> and process any exceptions as
    /// appropriate for the situation. An exception always means that the transaction did not commit.</para>
    /// <para>A method that catches exceptions thrown by another method taking a <see cref="TestDB.Transaction"/>
    /// object should, as a general rule, rethrow the caught or a new exception, as otherwise the transaction would commit.</para>
    /// </remarks>
    sealed partial class TestDB : IDisposable
    {
        private int _creatingThreadId;

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
            _creatingThreadId = Thread.CurrentThread.ManagedThreadId;
            ConnectionInfo = connInfo;
            dbProvider = connInfo.CreateEntityProvider(connInfo.CreateUnopenedConnection(), typeof(Transaction));
            Log = connInfo.Log;
            dbProvider.StartUsingConnection();
            connInfo.PrepareConnectionForFurtherUse(dbProvider.Connection);
        }

        /// <summary>
        /// Call this method explicitly or via a "using" clause to close this connection to the TestDB
        /// database.
        /// </summary>
        public void Dispose()
        {
            checkThread(); // nobody should call Dispose on another thread; the GC never calls this either.
            if (dbProvider != null)
            {
                dbProvider.StopUsingConnection();
                dbProvider.Connection.Dispose(); // since we created it in the constructor
                dbProvider = null;
            }
        }

        /// <summary>
        /// Gets or sets a class used for logging all executed queries. Set to null to disable logging. Note that
        /// this property is automatically initialized to <see cref="RT.SqlChain.ConnectionInfo.Log"/> when TestDB is
        /// instantiated.
        /// </summary>
        public TextWriter Log
        {
            // no checkThread here: this is for debugging purposes only and it might be useful to set this from another thread
            // even if that might cause an occasional threading bug
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
            var xml = XElement.Parse(SchemaAsXml);
            var schema = XmlClassify.ObjectFromXElement<SchemaInfo>(xml);
            schema.XmlDeclassifyFixup();
            using (var conn = connectionInfo.CreateConnectionForSchemaCreation())
            {
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
            var xml = XElement.Parse(SchemaAsXml);
            var schema = XmlClassify.ObjectFromXElement<SchemaInfo>(xml);
            schema.XmlDeclassifyFixup();
            using (var conn = connectionInfo.CreateConnectionForSchemaCreation())
            {
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

        /// <summary>
        /// Completely erases the schema represented by the <paramref name="connectionInfo"/> class.
        /// This method succeeds if and only if the schema no longer exists on return. It will likely fail if
        /// there exist connections to the schema.
        /// </summary>
        public static void DeleteSchema(ConnectionInfo connectionInfo)
        {
            connectionInfo.DeleteSchema();
        }

        #region Record types (one for each table)

        /// <summary>
        /// Represents a single row of the AllTypesNotNull table. The instances of this class are
        /// in no way "connected" to the table, and simply hold the row data.
        /// </summary>
        public partial class AllTypesNotNull : ICloneable
        {
            /// <summary>Represents the ColAutoincrement column in the AllTypesNotNull table. (Type: Autoincrement, NOT NULL)</summary>
            public long ColAutoincrement { get; set; }
            /// <summary>Represents the ColVarText1 column in the AllTypesNotNull table. (Type: VarText, NOT NULL, len=1)</summary>
            public string ColVarText1 { get; set; }
            /// <summary>Represents the ColVarText100 column in the AllTypesNotNull table. (Type: VarText, NOT NULL, len=100)</summary>
            public string ColVarText100 { get; set; }
            /// <summary>Represents the ColVarTextMax column in the AllTypesNotNull table. (Type: VarText, NOT NULL)</summary>
            public string ColVarTextMax { get; set; }
            /// <summary>Represents the ColVarBinary1 column in the AllTypesNotNull table. (Type: VarBinary, NOT NULL, len=1)</summary>
            public byte[] ColVarBinary1 { get; set; }
            /// <summary>Represents the ColVarBinary100 column in the AllTypesNotNull table. (Type: VarBinary, NOT NULL, len=100)</summary>
            public byte[] ColVarBinary100 { get; set; }
            /// <summary>Represents the ColVarBinaryMax column in the AllTypesNotNull table. (Type: VarBinary, NOT NULL)</summary>
            public byte[] ColVarBinaryMax { get; set; }
            /// <summary>Represents the ColBoolean column in the AllTypesNotNull table. (Type: Boolean, NOT NULL)</summary>
            public bool ColBoolean { get; set; }
            /// <summary>Represents the ColByte column in the AllTypesNotNull table. (Type: Byte, NOT NULL)</summary>
            public byte ColByte { get; set; }
            /// <summary>Represents the ColShort column in the AllTypesNotNull table. (Type: Short, NOT NULL)</summary>
            public short ColShort { get; set; }
            /// <summary>Represents the ColInt column in the AllTypesNotNull table. (Type: Int, NOT NULL)</summary>
            public int ColInt { get; set; }
            /// <summary>Represents the ColLong column in the AllTypesNotNull table. (Type: Long, NOT NULL)</summary>
            public long ColLong { get; set; }
            /// <summary>Represents the ColDouble column in the AllTypesNotNull table. (Type: Double, NOT NULL)</summary>
            public double ColDouble { get; set; }
            /// <summary>Represents the ColDateTime column in the AllTypesNotNull table. (Type: DateTime, NOT NULL)</summary>
            public DateTime ColDateTime { get; set; }

            /// <summary>Implement this partial method to define a custom <see cref="ToString"/> conversion.</summary>
            partial void ToStringCustom(ref string result);

            /// <summary>Returns a string representation of this object.</summary>
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

            object ICloneable.Clone()
            {
                return Clone();
            }

            /// <summary>
            /// Returns a shallow clone of this row.
            /// </summary>
            public AllTypesNotNull Clone()
            {
                return (AllTypesNotNull) MemberwiseClone();
            }
        }

        /// <summary>
        /// Represents a single row of the AllTypesNull table. The instances of this class are
        /// in no way "connected" to the table, and simply hold the row data.
        /// </summary>
        public partial class AllTypesNull : ICloneable
        {
            /// <summary>Represents the ColVarText1 column in the AllTypesNull table. (Type: VarText, NULL, len=1)</summary>
            public string ColVarText1 { get; set; }
            /// <summary>Represents the ColVarText100 column in the AllTypesNull table. (Type: VarText, NULL, len=100)</summary>
            public string ColVarText100 { get; set; }
            /// <summary>Represents the ColVarTextMax column in the AllTypesNull table. (Type: VarText, NULL)</summary>
            public string ColVarTextMax { get; set; }
            /// <summary>Represents the ColVarBinary1 column in the AllTypesNull table. (Type: VarBinary, NULL, len=1)</summary>
            public byte[] ColVarBinary1 { get; set; }
            /// <summary>Represents the ColVarBinary100 column in the AllTypesNull table. (Type: VarBinary, NULL, len=100)</summary>
            public byte[] ColVarBinary100 { get; set; }
            /// <summary>Represents the ColVarBinaryMax column in the AllTypesNull table. (Type: VarBinary, NULL)</summary>
            public byte[] ColVarBinaryMax { get; set; }
            /// <summary>Represents the ColBoolean column in the AllTypesNull table. (Type: Boolean, NULL)</summary>
            public bool? ColBoolean { get; set; }
            /// <summary>Represents the ColByte column in the AllTypesNull table. (Type: Byte, NULL)</summary>
            public byte? ColByte { get; set; }
            /// <summary>Represents the ColShort column in the AllTypesNull table. (Type: Short, NULL)</summary>
            public short? ColShort { get; set; }
            /// <summary>Represents the ColInt column in the AllTypesNull table. (Type: Int, NULL)</summary>
            public int? ColInt { get; set; }
            /// <summary>Represents the ColLong column in the AllTypesNull table. (Type: Long, NULL)</summary>
            public long? ColLong { get; set; }
            /// <summary>Represents the ColDouble column in the AllTypesNull table. (Type: Double, NULL)</summary>
            public double? ColDouble { get; set; }
            /// <summary>Represents the ColDateTime column in the AllTypesNull table. (Type: DateTime, NULL)</summary>
            public DateTime? ColDateTime { get; set; }

            /// <summary>Implement this partial method to define a custom <see cref="ToString"/> conversion.</summary>
            partial void ToStringCustom(ref string result);

            /// <summary>Returns a string representation of this object.</summary>
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

            object ICloneable.Clone()
            {
                return Clone();
            }

            /// <summary>
            /// Returns a shallow clone of this row.
            /// </summary>
            public AllTypesNull Clone()
            {
                return (AllTypesNull) MemberwiseClone();
            }
        }

        #endregion

        /// <summary>
        /// Verifies that the current thread is the same as the thread that created this instance. Throws an
        /// exception if this condition is not met.
        /// </summary>
        private void checkThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _creatingThreadId)
                throw new InvalidOperationException("Detected member access from a thread different than the creating thread. The TestDB class is not thread-safe and such use is not permitted.");
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
        public void ExecuteInTransaction(Action<Transaction> action)
        {
            checkThread();
            executeInTransaction<bool>(action);
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
            checkThread();
            return executeInTransaction<TResult>(func);
        }

        private Transaction _currentTransaction;
        private int _nestedTransactions = 0;

        private TResult executeInTransaction<TResult>(Delegate method)
        {
            _nestedTransactions++;
            if (_nestedTransactions == 1)
            {
                dbProvider.Transaction = dbProvider.Connection.BeginTransaction();
                _currentTransaction = new Transaction(dbProvider);
            }

            bool success = false;
            try
            {
                TResult result;
                var action = method as Action<Transaction>;
                if (action != null)
                {
                    action(_currentTransaction);
                    result = default(TResult);
                }
                else
                {
                    var func = method as Func<Transaction, TResult>;
                    result = func(_currentTransaction);
                }

                success = true;
                return result;
            }
            finally
            {
                _nestedTransactions--;
                if (_nestedTransactions == 0)
                {
                    try
                    {
                        if (success)
                            dbProvider.Transaction.Commit(); // can throw deferred constraint violations
                        else
                            dbProvider.Transaction.Rollback(); // can probably also throw - disk failure, connection timeout, etc
                    }
                    finally
                    {
                        dbProvider.Transaction.Dispose();
                        dbProvider.Transaction = null;
                        _currentTransaction = null;
                    }
                }
            }
        }

        /// <summary>
        /// Represents a transaction within a <see cref="DB"/> database connection. Do not instantiate this class directly. Instead, use <see cref="DB.ExecuteInTransaction"/>.
        /// </summary>
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
            [Table(Name = "AllTypesNotNull")]
            [Column(Member = "ColAutoincrement", Name = "ColAutoincrement", IsPrimaryKey = true, IsGenerated = true)]
            [Column(Member = "ColVarText1", Name = "ColVarText1")]
            [Column(Member = "ColVarText100", Name = "ColVarText100")]
            [Column(Member = "ColVarTextMax", Name = "ColVarTextMax")]
            [Column(Member = "ColVarBinary1", Name = "ColVarBinary1")]
            [Column(Member = "ColVarBinary100", Name = "ColVarBinary100")]
            [Column(Member = "ColVarBinaryMax", Name = "ColVarBinaryMax")]
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
            [Table(Name = "AllTypesNull")]
            [Column(Member = "ColVarText1", Name = "ColVarText1")]
            [Column(Member = "ColVarText100", Name = "ColVarText100")]
            [Column(Member = "ColVarTextMax", Name = "ColVarTextMax")]
            [Column(Member = "ColVarBinary1", Name = "ColVarBinary1")]
            [Column(Member = "ColVarBinary100", Name = "ColVarBinary100")]
            [Column(Member = "ColVarBinaryMax", Name = "ColVarBinaryMax")]
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
            public int ExecuteSql(string sql)
            {
                return DbProvider.ExecuteCommand(sql);
            }

            /// <summary>Executes the specified SQL command with the specified parameter value.</summary>
            public int ExecuteSql<T0>(string sql, T0 p0)
            {
                var cmd = new QueryCommand(sql, new QueryParameter[] { new QueryParameter("p0", typeof(T0), null) });
                return new DbEntityProvider.Executor(DbProvider).ExecuteCommand(cmd, new object[] { p0 });
            }

            /// <summary>Executes the specified SQL command with the specified parameter values.</summary>
            public int ExecuteSql<T0, T1>(string sql, T0 p0, T1 p1)
            {
                var cmd = new QueryCommand(sql, new QueryParameter[] { new QueryParameter("p0", typeof(T0), null), new QueryParameter("p1", typeof(T1), null) });
                return new DbEntityProvider.Executor(DbProvider).ExecuteCommand(cmd, new object[] { p0, p1 });
            }
        }

        /// <summary>
        /// Gets the schema of this database connection as an XML string with no XML declaration
        /// (parseable by <see cref="XElement"/> but not <see cref="XDocument"/>).
        /// </summary>
        public const string SchemaAsXml = @"
<item>
  <tables>
    <item>
      <columns>
        <item>
          <Name>ColAutoincrement</Name>
          <Type>
            <BasicType>Autoincrement</BasicType>
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
