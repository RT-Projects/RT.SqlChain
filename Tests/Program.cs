using System;
using System.Reflection;
using NUnit.Direct;
using NUnit.Framework;
using RT.SqlChain;
using RT.Util;

namespace SqlChainTests
{
    [TestFixture]
    public partial class Tests
    {
        //SqliteConnectionInfo _connSqlite;
        //SqlServerConnectionInfo _connSqlServer;

        //TestDB _dbSqlite;
        //TestDB _dbSqlServer;

        [TestFixtureSetUp]
        public void Init()
        {
            //_connSqlite = new SqliteConnectionInfo(PathUtil.AppPathCombine("SqlChainTestDB.db3"));
            //_connSqlServer = new SqlServerConnectionInfo("LOCALHOST", "SQLCHAIN_TEST_DB");

            //// These deletions must succeed even if the schemas were properly deleted on last run.
            //_connSqlite.DeleteSchema();
            //_connSqlServer.DeleteSchema();

            //TestDB.CreateSchema(_connSqlite);
            //TestDB.CreateSchema(_connSqlServer);

            //_dbSqlite = new TestDB(_connSqlite);
            //_dbSqlServer = new TestDB(_connSqlServer);
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            //_connSqlite.DeleteSchema();
            //_connSqlServer.DeleteSchema();
        }

        [Test]
        public void SuccessfulTest()
        {
            // this is just a test that always succeeds, to aid TeamCity setup until actual tests become available
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
    /// Creation + Retrieval:
    /// - That the schema roundtrips correctly (retrieved XML equals source XML)
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
