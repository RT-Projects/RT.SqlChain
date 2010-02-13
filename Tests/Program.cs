using System;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Direct;
using NUnit.Framework;
using RT.SqlChain;
using RT.Util;
using RT.Util.Xml;

namespace SqlChainTests
{
    public enum DbmsKind { Sqlite, SqlServer }

    [TestFixture]
    public static partial class Tests
    {
        private static LoggerBase _log = new ConsoleLogger();

        private static SqliteConnectionInfo _connSqlite;
        private static SqlServerConnectionInfo _connSqlServer;

        private static TestDB _dbSqlite;
        private static TestDB _dbSqlServer;

        private static ConnectionInfo getConnInfo(DbmsKind kind)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite: return _connSqlite;
                case DbmsKind.SqlServer: return _connSqlServer;
                default: throw new InternalError("hoffsho");
            }
        }

        private static TestDB getConn(DbmsKind kind)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite: return _dbSqlite;
                case DbmsKind.SqlServer: return _dbSqlServer;
                default: throw new InternalError("fhsohsk");
            }
        }

        [TestFixtureSetUp]
        public static void Init()
        {
            _log.Info("Init() ...");

            _connSqlite = new SqliteConnectionInfo(PathUtil.AppPathCombine("SqlChainTestDB.db3"));
            _connSqlServer = new SqlServerConnectionInfo("LOCALHOST", "SQLCHAIN_TEST_DB");

            _connSqlite.Log = _connSqlServer.Log = Console.Out;

            // These deletions must succeed even if the schemas were properly deleted on last run.
            _connSqlite.DeleteSchema();
            _connSqlServer.DeleteSchema();

            TestDB.CreateSchema(_connSqlite);
            TestDB.CreateSchema(_connSqlServer);

            _dbSqlite = new TestDB(_connSqlite);
            _dbSqlServer = new TestDB(_connSqlServer);

            _log.Info("Init() complete");
        }

        [TestFixtureTearDown]
        public static void Cleanup()
        {
            _log.Info("Cleanup() ...");

            _dbSqlite.Dispose();
            _dbSqlite = null;
            _connSqlite.DeleteSchema();

            _dbSqlServer.Dispose();
            _dbSqlServer.Dispose();
            _dbSqlServer = null;
            _connSqlServer.DeleteSchema();

            _log.Info("Cleanup() complete");
        }

        [Test]
        public static void RoundtripTest([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            var conninfo = getConnInfo(kind);
            using (var dbconn = conninfo.CreateConnection())
            {
                dbconn.Open();
                var retr = conninfo.CreateSchemaRetriever(dbconn);
                var xmlActual = XmlClassify.ObjectToXElement(retr.RetrieveSchema());
                var xmlExpected = XElement.Parse(TestDB.SchemaAsXml);
                Assert.IsTrue(XNode.DeepEquals(xmlActual, xmlExpected));
            }
        }
    }

    /// <summary>
    /// Tests TODO list:
    /// 
    /// AllTypesNull / AllTypesNotNull:
    /// - insertion of actual values
    /// - insertion of nulls
    /// - update
    /// - delete
    /// - WHERE lookup by each type
    ///   - handling of nulls
    /// - JOIN by each type
    ///   - handling of nulls
    /// 
    /// PK/FK as follows:
    /// - Non-PK table (unique index + foreign key)
    /// - Autoincrement PK table
    /// - String PK table
    /// - Multicolumn PK table (three: int, string, date?)
    /// For each of these:
    /// - Schema creation (implicit in Setup)
    /// - That the PK / unique constraint is enforced (abort mode)
    /// - That the FK constraint is enforced (abort mode)
    /// - That cascading updates/deletes work?
    /// 
    /// Schema delete:
    /// - That the scema has disappeared
    /// 
    /// -------------------------------------
    /// For features not yet implemented:
    /// 
    /// Schema upgrades:
    /// - each function exposed by Mutator in various corner-cases
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly());
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
