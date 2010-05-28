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
            var q46 = QueryCompiler.Compile<TestDB.WritableTransaction, IQueryable<string>>((_txn) => _txn.AllTypesNulls.Where(t => t.ColInt == 46).Select(r => r.ColVarTextMax).OrderBy(v => v));
            var q47 = QueryCompiler.Compile<TestDB.WritableTransaction, IQueryable<string>>((_txn) => _txn.AllTypesNulls.Where(t => t.ColInt == 47).Select(r => r.ColVarTextMax).OrderBy(v => v));
            var q48 = QueryCompiler.Compile<TestDB.WritableTransaction, IQueryable<string>>((_txn) => _txn.AllTypesNulls.Where(t => t.ColInt == 48).Select(r => r.ColVarTextMax).OrderBy(v => v));
            var q49 = QueryCompiler.Compile<TestDB.WritableTransaction, IQueryable<string>>((_txn) => _txn.AllTypesNulls.Where(t => t.ColInt == 49).Select(r => r.ColVarTextMax).OrderBy(v => v));
            var qDel47 = QueryCompiler.Compile<TestDB.WritableTransaction, int>((_txn) => _txn.AllTypesNulls.Delete(t => t.ColInt == 47));

            using (var conn = createSchemaAndOpenConn(kind))
            {
                conn.InTransaction(txn =>
                {
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 46, ColVarTextMax = "forty six" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 1" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 47, ColVarTextMax = "forty seven 2" });
                    txn.AllTypesNulls.Insert(new TestDB.AllTypesNull { ColInt = 48, ColVarTextMax = "forty eight" });
                });
            }

            using (var conn = new TestDB(getConnInfo(kind, null)))
            {
                conn.InTransaction(txn =>
                {
                    var vals46 = q46(txn).ToList();
                    var vals47 = q47(txn).ToList();
                    var vals48 = q48(txn).ToList();
                    var vals49 = q49(txn).ToList();
                    Assert.AreEqual(1, vals46.Count);
                    Assert.AreEqual(2, vals47.Count);
                    Assert.AreEqual(1, vals48.Count);
                    Assert.AreEqual(0, vals49.Count);
                    Assert.AreEqual("forty six", vals46[0]);
                    Assert.AreEqual("forty seven 1", vals47[0]);
                    Assert.AreEqual("forty seven 2", vals47[1]);
                    Assert.AreEqual("forty eight", vals48[0]);
                    qDel47(txn); // FAILS!
                    Assert.AreEqual(1, q46(txn).Count());
                    Assert.AreEqual(0, q47(txn).Count());
                    Assert.AreEqual(1, q48(txn).Count());
                });
            }
        }

        [Test]
        public void TestCompiledQuery_0arg_Implicit([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            using (var conn = createSchemaAndOpenConn(kind))
            {
                Func<IQueryable<string>> q46 = null, q47 = null, q48 = null, q49 = null;
                conn.InTransaction(txn =>
                {
                    q46 = QueryCompiler.Compile<IQueryable<string>>(() => txn.AllTypesNulls.Where(t => t.ColInt == 46).Select(r => r.ColVarTextMax).OrderBy(v => v));
                    q47 = QueryCompiler.Compile<IQueryable<string>>(() => txn.AllTypesNulls.Where(t => t.ColInt == 47).Select(r => r.ColVarTextMax).OrderBy(v => v));
                    q48 = QueryCompiler.Compile<IQueryable<string>>(() => txn.AllTypesNulls.Where(t => t.ColInt == 48).Select(r => r.ColVarTextMax).OrderBy(v => v));
                    q49 = QueryCompiler.Compile<IQueryable<string>>(() => txn.AllTypesNulls.Where(t => t.ColInt == 49).Select(r => r.ColVarTextMax).OrderBy(v => v));
                    // Note: the implicit "txn" used in the above queries breaks if we close this connection and open another one
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
            using (var conn = createSchemaAndOpenConn(kind))
            {
                Func<int, IQueryable<string>> q = null;
                Func<int, int> qDel = null;

                conn.InTransaction(txn =>
                {
                    q = QueryCompiler.Compile<int, IQueryable<string>>((val) => txn.AllTypesNulls.Where(t => t.ColInt == val).Select(r => r.ColVarTextMax).OrderBy(v => v));
                    qDel = QueryCompiler.Compile<int, int>((val) => txn.AllTypesNulls.Delete(t => t.ColInt == val));
                    // Note: the implicit "txn" used in the above queries breaks if we close this connection and open another one
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
