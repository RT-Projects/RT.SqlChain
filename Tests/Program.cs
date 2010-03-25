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

        private ConnectionInfo getConnInfo(DbmsKind kind, string suffix)
        {
            switch (kind)
            {
                case DbmsKind.Sqlite:
                    var dbFilename = "SqlChainTestDB" + suffix + ".db3";
                    var connInfoSqlite = new SqliteConnectionInfo(Assembly.GetEntryAssembly() == null ? dbFilename : PathUtil.AppPathCombine(dbFilename));
                    connInfoSqlite.Log = Console.Out;
                    connInfoSqlite.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
                    return connInfoSqlite;

                case DbmsKind.SqlServer:
                    var connInfoSqlServer = new SqlServerConnectionInfo("LOCALHOST\\SQLEXPRESS", "SQLCHAIN_TEST_DB" + suffix);
                    connInfoSqlServer.Log = Console.Out;
                    connInfoSqlServer.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
                    return connInfoSqlServer;

                default: throw new InternalError("hoffsho");
            }
        }

        private TestDB createConnAndSchema(DbmsKind kind)
        {
            var connInfo = getConnInfo(kind, null);
            TestDB.CreateSchema(connInfo);
            return new TestDB(connInfo);
        }

        private Exception exceptionof(Action action)
        {
            try { action(); return null; }
            catch (Exception e) { return e; }
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
