using System;
using System.Reflection;
using NUnit.Framework;
using RT.Util;

namespace SqlChainTests
{
    [TestFixture]
    public partial class Tests
    {
        [TestFixtureSetUp]
        public void Init()
        {
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
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
            if (false)
            {
                Testing.GenerateTestingCode(@"..\..\main\common\SqlChain\Tests\Program.cs", "Run Tests", Assembly.GetExecutingAssembly().GetExportedTypes(),
                    typeof(TestFixtureAttribute), typeof(TestFixtureSetUpAttribute), typeof(TestAttribute), typeof(TestFixtureTearDownAttribute));
            }
            else
            {
                #region Run Tests
                #endregion

                Console.WriteLine("");
                Console.WriteLine("Tests passed; press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}
