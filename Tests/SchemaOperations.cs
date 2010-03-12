using System;
using System.Data.Common;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using RT.SqlChain;
using RT.Util;
using RT.Util.Xml;
using System.Reflection;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void TestSchemaOperations([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            ConnectionInfo conninfo = null;
            switch (kind)
            {
                case DbmsKind.Sqlite:
                    var dbFilename = "SqlChainTestDB-closetest.db3";
                    conninfo = new SqliteConnectionInfo(Assembly.GetEntryAssembly() == null ? dbFilename : PathUtil.AppPathCombine(dbFilename));
                    break;
                case DbmsKind.SqlServer:
                    conninfo = new SqlServerConnectionInfo("LOCALHOST\\SQLEXPRESS", "SQLCHAIN_TEST_DB_CLOSETEST");
                    break;
            }
            conninfo.Log = Console.Out;

            // Test 1
            conninfo.DeleteSchema();
            Assert.IsFalse(conninfo.SchemaExists());

            // Test 2
            TestDB.CreateSchema(conninfo);
            Assert.IsTrue(conninfo.SchemaExists());

            // Roundtrip test
            using (var dbconn = conninfo.CreateConnection())
            {
                dbconn.Open();
                var retr = conninfo.CreateSchemaRetriever(dbconn);
                var xmlActual = XmlClassify.ObjectToXElement(retr.RetrieveSchema());
                var xmlExpected = XElement.Parse(TestDB.SchemaAsXml);
                Assert.IsTrue(XNode.DeepEquals(xmlActual, xmlExpected));
            }

            // Do something to exercise IQToolkit's use of this connection
            using (var conn = new TestDB(conninfo))
                conn.ExecuteInTransaction(txn => { var rows = txn.AllTypesNotNulls.ToArray(); });
            using (var conn = new TestDB(conninfo))
                conn.ExecuteInTransaction(txn => { var rows = txn.AllTypesNotNulls.ToArray(); });

            // Try to delete - this only succeeds if all connections were closed properly
            conninfo.DeleteSchema();
            Assert.IsFalse(conninfo.SchemaExists());

            // Double-check that opening a connection now fails
            Assert.IsInstanceOf<DbException>(exceptionof(() => conninfo.CreateConnection().Open()));
        }
    }
}
