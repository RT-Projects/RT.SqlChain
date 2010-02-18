using System;
using System.Reflection;
using NUnit.Direct;
using NUnit.Framework;
using RT.SqlChain;
using RT.Util;

namespace RT.SqlChainTests
{
    public enum DbmsKind { Sqlite, SqlServer }

    [TestFixture]
    public partial class Tests
    {
        private LoggerBase _log = new ConsoleLogger();

        private SqliteConnectionInfo _connSqlite;
        private SqlServerConnectionInfo _connSqlServer;

        private TestDB _dbSqlite;
        private TestDB _dbSqlServer;

        private ConnectionInfo getConnInfo(DbmsKind kind)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite: return _connSqlite;
                case DbmsKind.SqlServer: return _connSqlServer;
                default: throw new InternalError("hoffsho");
            }
        }

        private TestDB getConn(DbmsKind kind)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite: return _dbSqlite;
                case DbmsKind.SqlServer: return _dbSqlServer;
                default: throw new InternalError("fhsohsk");
            }
        }

        private Exception exceptionof(Action action)
        {
            try { action(); return null; }
            catch (Exception e) { return e; }
        }

        [TestFixtureSetUp]
        public void Init()
        {
            _log.Info("Init() ...");

            _connSqlite = new SqliteConnectionInfo(PathUtil.AppPathCombine("SqlChainTestDB.db3"));
            _connSqlite.Log = Console.Out;
            _connSqlite.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
            TestDB.CreateSchema(_connSqlite);
            _dbSqlite = new TestDB(_connSqlite);

            _connSqlServer = new SqlServerConnectionInfo("LOCALHOST\\SQLEXPRESS", "SQLCHAIN_TEST_DB");
            _connSqlServer.Log = Console.Out;
            _connSqlServer.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
            TestDB.CreateSchema(_connSqlServer);
            _dbSqlServer = new TestDB(_connSqlServer);

            _log.Info("Init() complete");
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            _log.Info("Cleanup() ...");

            _dbSqlite.Dispose();
            _dbSqlite = null;
            _connSqlite.DeleteSchema();

            _dbSqlServer.Dispose();
            _dbSqlServer = null;
            // _connSqlServer.DeleteSchema(); FAILS!!!

            _log.Info("Cleanup() complete");
        }
    }

    /// <summary>
    /// Tests TODO list:
    /// 
    /// AllTypesNull / AllTypesNotNull:
    /// - insertion of actual values [AllTypesNull]
    /// - insertion of nulls
    /// - update
    /// - delete
    /// - WHERE lookup by each type
    ///   - handling of nulls
    /// - JOIN by each type
    ///   - handling of nulls
    /// - failing doubles
    ///
    /// Autoincrement:
    /// - correctly autoincremented
    /// - value can be retrieved
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
    /// Transactions:
    /// - todo
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
