using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IQToolkit;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void Collations([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            using (var conn = createConnAndSchema(kind))
            {
                Assert.AreEqual(0, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count()));

                conn.InTransaction(txn => txn.AllTypesNulls.Insert(new TestDB.AllTypesNull
                {
                    ColVarText1 = "a",
                    ColVarText100 = "AbCdE",
                    ColVarTextMax = "ABCDEF"
                }));

                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count()));

                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count(atn => atn.ColVarText1 == "A")));
                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count(atn => atn.ColVarText100 == "ABCDE")));
                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count(atn => atn.ColVarTextMax == "ABCDEF")));

                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count(atn => atn.ColVarText1 == "a")));
                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count(atn => atn.ColVarText100 == "abcde")));
                Assert.AreEqual(1, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count(atn => atn.ColVarTextMax == "abcdef")));

                Assert.DoesNotThrow(() => conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.First()));
                Assert.DoesNotThrow(() => conn.InTransaction(txn => txn.AllTypesNulls.Delete(atn => true)));
                Assert.AreEqual(0, conn.InReadOnlyTransaction(txn => txn.AllTypesNulls.Count()));
            }
        }
    }
}
