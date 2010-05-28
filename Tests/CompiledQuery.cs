using System;
using System.Linq;
using IQToolkit;
using NUnit.Framework;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void TestCompiledQuery_1arg_Explicit([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            Func<TestDB.WritableTransaction, IQueryable<string>> q46, q47, q48, q49;
            Func<TestDB.WritableTransaction, int> qDel47 = null;
            q46 = q47 = q48 = q49 = null;
            using (var conn = createSchemaAndOpenConn(kind))
            {
                conn.InTransaction(txn =>
                {
                    q46 = CompiledQuery.Create((TestDB.WritableTransaction txn2) => txn2.AllTypesNulls.Where(t => t.ColInt == 46).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile(txn.AllTypesNulls.Provider);
                    q47 = CompiledQuery.Create((TestDB.WritableTransaction txn2) => txn2.AllTypesNulls.Where(t => t.ColInt == 47).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile(txn.AllTypesNulls.Provider);
                    q48 = CompiledQuery.Create((TestDB.WritableTransaction txn2) => txn2.AllTypesNulls.Where(t => t.ColInt == 48).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile(txn.AllTypesNulls.Provider);
                    q49 = CompiledQuery.Create((TestDB.WritableTransaction txn2) => txn2.AllTypesNulls.Where(t => t.ColInt == 49).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile(txn.AllTypesNulls.Provider);
                    //qDel47 = CompiledQuery.Create((TestDB.WritableTransaction txn2) => txn2.AllTypesNulls.Delete(t => t.ColInt == 47)).Compile(txn.AllTypesNulls.Provider);

                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 46, ColVarTextMax = "forty six" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 1" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 2" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 48, ColVarTextMax = "forty eight" });
                });
            }

            using (var conn = new TestDB(getConnInfo(kind, null)))
            {
                conn.InTransaction(txnabc =>
                {
                    var vals46 = q46(txnabc).ToList();
                    var vals47 = q47(txnabc).ToList();
                    var vals48 = q48(txnabc).ToList();
                    var vals49 = q49(txnabc).ToList();
                    Assert.AreEqual(1, vals46.Count);
                    Assert.AreEqual(2, vals47.Count);
                    Assert.AreEqual(1, vals48.Count);
                    Assert.AreEqual(0, vals49.Count);
                    Assert.AreEqual("forty six", vals46[0]);
                    Assert.AreEqual("forty seven 1", vals47[0]);
                    Assert.AreEqual("forty seven 2", vals47[1]);
                    Assert.AreEqual("forty eight", vals48[0]);
#warning Bring back the failing compiled deletion test.
                    //qDel47(null); // FAILS!
                    //Assert.AreEqual(1, q46(null).Count());
                    //Assert.AreEqual(0, q47(null).Count());
                    //Assert.AreEqual(1, q48(null).Count());
                });
            }
        }

        //[Test]
        public void TestCompiledQuery_TwoConnections([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
#warning Implement test: two connections in parallel to different databases; two transactions at the same time. Verify that the same compiled query can be used on different connections and has the effect on the correct one.
        }

        [Test]
        public void TestCompiledQuery_0arg_Implicit([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            Func<IQueryable<string>> q46 = null, q47 = null, q48 = null, q49 = null;
            using (var conn = createSchemaAndOpenConn(kind))
            {
                conn.InTransaction(txn =>
                {
                    q46 = CompiledQuery.Create(() => txn.AllTypesNulls.Where(t => t.ColInt == 46).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile();
                    q47 = CompiledQuery.Create(() => txn.AllTypesNulls.Where(t => t.ColInt == 47).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile();
                    q48 = CompiledQuery.Create(() => txn.AllTypesNulls.Where(t => t.ColInt == 48).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile();
                    q49 = CompiledQuery.Create(() => txn.AllTypesNulls.Where(t => t.ColInt == 49).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile();
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 46, ColVarTextMax = "forty six" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 1" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 2" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 48, ColVarTextMax = "forty eight" });
                });

                conn.InTransaction(txn =>
                {
                    var vals46 = q46().ToList();
                    var vals47 = q47().ToList();
                    var vals48 = q48().ToList();
                    var vals49 = q49().ToList();
                    Assert.AreEqual(1, vals46.Count);
                    Assert.AreEqual(2, vals47.Count);
                    Assert.AreEqual(1, vals48.Count);
                    Assert.AreEqual(0, vals49.Count);
                    Assert.AreEqual("forty six", vals46[0]);
                    Assert.AreEqual("forty seven 1", vals47[0]);
                    Assert.AreEqual("forty seven 2", vals47[1]);
                    Assert.AreEqual("forty eight", vals48[0]);
                });
            }
        }

        [Test]
        public void TestCompiledQuery_1arg_Implicit([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            Func<int, IQueryable<string>> q = null;
            Func<int, int> qDel = null;

            using (var conn = createSchemaAndOpenConn(kind))
            {
                conn.InTransaction(txn =>
                {
                    q = CompiledQuery.Create((int val) => txn.AllTypesNulls.Where(t => t.ColInt == val).Select(r => r.ColVarTextMax).OrderBy(v => v)).Compile();
                    qDel = CompiledQuery.Create((int val) => txn.AllTypesNulls.Delete(t => t.ColInt == val)).Compile();
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 46, ColVarTextMax = "forty six" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 1" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 2" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 48, ColVarTextMax = "forty eight" });
                });

                conn.InTransaction(txn =>
                {
                    var vals46 = q(46).ToList();
                    var vals47 = q(47).ToList();
                    var vals48 = q(48).ToList();
                    var vals49 = q(49).ToList();
                    Assert.AreEqual(1, vals46.Count);
                    Assert.AreEqual(2, vals47.Count);
                    Assert.AreEqual(1, vals48.Count);
                    Assert.AreEqual(0, vals49.Count);
                    Assert.AreEqual("forty six", vals46[0]);
                    Assert.AreEqual("forty seven 1", vals47[0]);
                    Assert.AreEqual("forty seven 2", vals47[1]);
                    Assert.AreEqual("forty eight", vals48[0]);
                    qDel(47);
                    Assert.AreEqual(1, q(46).Count());
                    Assert.AreEqual(0, q(47).Count());
                    Assert.AreEqual(1, q(48).Count());
                });
            }
        }
    }
}
