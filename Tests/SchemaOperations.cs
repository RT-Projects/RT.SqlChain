using System;
using System.Data.Common;
using System.Linq;
using System.Xml.Linq;
using IQToolkit;
using NUnit.Framework;
using RT.SqlChain;
using RT.Util.Xml;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void TestSchemaOperations([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            ConnectionInfo conninfo = getConnInfoAndDeleteSchema(kind, "closetest");
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
                var retr = conninfo.CreateSchemaRetriever(dbconn);
                var xmlActual = XmlClassify.ObjectToXElement(retr.RetrieveSchema());
                var xmlExpected = XElement.Parse(TestDB.SchemaAsXml);
                Assert.IsTrue(XNode.DeepEquals(xmlActual, xmlExpected));
            }

            // Do something to exercise IQToolkit's use of this connection
            using (var conn = new TestDB(conninfo))
                conn.InTransaction(txn => { var rows = txn.AllTypesNotNulls.ToArray(); });
            using (var conn = new TestDB(conninfo))
                conn.InTransaction(txn => { var rows = txn.AllTypesNotNulls.ToArray(); });
            using (var conn = new TestDB(conninfo))
                conn.InTransaction(txn => { txn.AllTypesNulls.Batch(new[] { new TestDB.AllTypesNull { ColInt = 47 } }, (table, entry) => table.Insert(entry)); });

            // Try to delete - this only succeeds if all connections were closed properly
            conninfo.DeleteSchema();
            Assert.IsFalse(conninfo.SchemaExists());

            // Double-check that opening a connection now fails
            Assert.IsInstanceOf<DbException>(exceptionof(() => conninfo.CreateConnection()));
        }
    }
}
