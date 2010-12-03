using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Direct;
using NUnit.Framework;
using RT.SqlChain;
using RT.Util;

[assembly: Timeout(40000)]

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
                    return connInfoSqlite;

                case DbmsKind.SqlServer:
                    var connInfoSqlServer = new SqlServerConnectionInfo("LOCALHOST\\SQLEXPRESS", "SQLCHAIN_TEST_DB" + suffix);
                    connInfoSqlServer.Log = Console.Out;
                    return connInfoSqlServer;

                default: throw new InternalErrorException("hoffsho");
            }
        }

        private ConnectionInfo getConnInfoAndDeleteSchema(DbmsKind kind, string suffix)
        {
            var result = getConnInfo(kind, suffix);
            result.DeleteSchema();   // must succeed even if the schemas were properly deleted on last run.
            return result;
        }

        private TestDB createSchemaAndOpenConn(DbmsKind kind)
        {
            var connInfo = getConnInfoAndDeleteSchema(kind, null);
            TestDB.CreateSchema(connInfo);
            return new TestDB(connInfo);
        }

        private Exception exceptionof(Action action)
        {
            try { action(); return null; }
            catch (AssertionException) { throw; }
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
            bool wait = !args.Contains("--no-wait");
            bool notimes = args.Contains("--no-times");

            Console.OutputEncoding = Encoding.UTF8;
            NUnitDirect.RunTestsOnAssembly(Assembly.GetEntryAssembly(), notimes);

            if (wait)
            {
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
