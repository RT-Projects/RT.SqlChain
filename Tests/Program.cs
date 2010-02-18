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

        private SqliteConnectionInfo _conninfoSqlite;
        private SqlServerConnectionInfo _conninfoSqlServer;

        private ConnectionInfo getConnInfo(DbmsKind kind)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite: return _conninfoSqlite;
                case DbmsKind.SqlServer: return _conninfoSqlServer;
                default: throw new InternalError("hoffsho");
            }
        }

        private TestDB createConn(DbmsKind kind)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite: return new TestDB(_conninfoSqlite);
                case DbmsKind.SqlServer: return new TestDB(_conninfoSqlServer);
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

            _conninfoSqlite = new SqliteConnectionInfo(PathUtil.AppPathCombine("SqlChainTestDB.db3"));
            _conninfoSqlite.Log = Console.Out;
            _conninfoSqlite.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
            TestDB.CreateSchema(_conninfoSqlite);

            _conninfoSqlServer = new SqlServerConnectionInfo("LOCALHOST\\SQLEXPRESS", "SQLCHAIN_TEST_DB");
            _conninfoSqlServer.Log = Console.Out;
            _conninfoSqlServer.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
            TestDB.CreateSchema(_conninfoSqlServer);

            _log.Info("Init() complete");
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            _log.Info("Cleanup() ...");

            _conninfoSqlite.DeleteSchema();
             _conninfoSqlServer.DeleteSchema();

            _log.Info("Cleanup() complete");
        }
    }

    /// <summary>
    /// Tests TODO list:
    /// 
    /// AllTypesNull / AllTypesNotNull:
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
