using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void Transactions([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            using (var conn = createSchemaAndOpenConn(kind))
            {
                Assert.AreEqual(0, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count()));


                Assert.IsInstanceOf<InvalidOperationException>(exceptionof(() =>
                {
                    conn.InTransaction(outerTxn =>
                    {
                        Assert.AreEqual(0, outerTxn.AllTypesNulls.Count());
                        outerTxn.AllTypesNulls.Insert(new TestDB.AllTypesNull());
                        Assert.AreEqual(1, outerTxn.AllTypesNulls.Count());

                        // This should throw an InvalidOperationException
                        conn.InTransaction(innerTxn =>
                        {
                            // This code should never run
                            Assert.Fail();
                        });
                    });
                }));

                // Transaction should have been rolled back
                Assert.AreEqual(0, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count()));


                Assert.IsInstanceOf<InvalidOperationException>(exceptionof(() =>
                {
                    conn.InTransaction(outerTxn =>
                    {
                        Assert.AreEqual(0, outerTxn.AllTypesNulls.Count());
                        outerTxn.AllTypesNulls.Insert(new TestDB.AllTypesNull());
                        Assert.AreEqual(1, outerTxn.AllTypesNulls.Count());

                        // This should throw an InvalidOperationException
                        conn.InReadOnlyTransaction(innerTxn =>
                        {
                            // This code should never run
                            Assert.Fail();
                        });
                    });
                }));

                // Transaction should have been rolled back
                Assert.AreEqual(0, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count()));


                Assert.IsInstanceOf<InvalidOperationException>(exceptionof(() =>
                {
                    conn.InReadOnlyTransaction(outerTxn =>
                    {
                        // This should throw an InvalidOperationException
                        conn.InTransaction(innerTxn =>
                        {
                            // This code should never run
                            Assert.Fail();
                        });
                    });
                }));


                // This should be rolled back
                try
                {
                    conn.InTransaction(txn =>
                    {
                        txn.AllTypesNulls.Insert(new TestDB.AllTypesNull());
                        Assert.AreEqual(1, txn.AllTypesNulls.Count());
                        throw new Exception();
                    });
                    Assert.Fail();
                }
                catch
                {
                }

                // This should succeed and be committed
                conn.InTransaction(txn =>
                {
                    Assert.AreEqual(0, txn.AllTypesNulls.Count());
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull());
                    Assert.AreEqual(1, txn.AllTypesNulls.Count());
                });

                conn.InReadOnlyTransaction(outerTxn =>
                {
                    Assert.AreEqual(1, outerTxn.AllTypesNulls.Count());
                    // ReadOnly inside ReadOnly should succeed
                    conn.InReadOnlyTransaction(innerTxn =>
                    {
                        Assert.AreEqual(1, innerTxn.AllTypesNulls.Count());
                    });
                });
            }
        }
    }
}
