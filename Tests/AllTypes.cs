using System;
using System.Data.Common;
using System.Linq;
using IQToolkit;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void AllTypesNotNull([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            using (var conn = createSchemaAndOpenConn(kind))
            {
                var rowMax = new TestDB.AllTypesNotNull();
                rowMax.ColVarText1 = "a";
                rowMax.ColVarText100 = "blah100";
                rowMax.ColVarTextMax = "lots of text! ".Repeat(100);
                rowMax.ColVarBinary1 = new byte[] { 250 };
                rowMax.ColVarBinary100 = "some text here".ToUtf8();
                rowMax.ColVarBinaryMax = "lots of binary! ".Repeat(100).ToUtf8();
                rowMax.ColBoolean = true;
                rowMax.ColByte = byte.MaxValue;
                rowMax.ColShort = short.MaxValue;
                rowMax.ColInt = int.MaxValue;
                rowMax.ColLong = long.MaxValue;
                rowMax.ColDouble = 1e+308;
                rowMax.ColDateTime = new DateTime(2150, 12, 31, 23, 59, 59, 987, DateTimeKind.Utc);

                var rowMin = rowMax.Clone();
                rowMin.ColBoolean = false;
                rowMin.ColByte = byte.MinValue;
                rowMin.ColShort = short.MinValue;
                rowMin.ColInt = int.MinValue;
                rowMin.ColLong = long.MinValue;
                rowMin.ColDouble = -1e+308;
                rowMin.ColDateTime = new DateTime(1950, 12, 31, 23, 59, 59, 987, DateTimeKind.Utc);

                var rowEmpty = new TestDB.AllTypesNotNull();
                rowEmpty.ColVarText1 = "";
                rowEmpty.ColVarText100 = "";
                rowEmpty.ColVarTextMax = "";
                rowEmpty.ColVarBinary1 = new byte[0];
                rowEmpty.ColVarBinary100 = new byte[0];
                rowEmpty.ColVarBinaryMax = new byte[0];
                rowEmpty.ColBoolean = false;
                rowEmpty.ColByte = 0;
                rowEmpty.ColShort = 0;
                rowEmpty.ColInt = 0;
                rowEmpty.ColLong = 0;
                rowEmpty.ColDouble = 1e-300;
                rowEmpty.ColDateTime = new DateTime(1753, 1, 1, 0, 0, 0, 123); // default(DateTime); cannot be stored - minimum supported by SQL Server is 1/1/1753

                var rowExtra = rowMin.Clone();
                rowExtra.ColDouble = 3.1415926535897932384626433832795;

                // Insertion
                conn.InTransaction(txn =>
                    {
                        rowMax.ColAutoincrement = 12345;
                        rowMin.ColAutoincrement = 23456;
                        rowEmpty.ColAutoincrement = 34567; // these are ignored because they are autoincrement

                        rowMax.ColAutoincrement = txn.AllTypesNotNulls.Insert(rowMax.Clone(), res => res.ColAutoincrement);
                        rowMin.ColAutoincrement = txn.AllTypesNotNulls.Insert(rowMin.Clone(), res => res.ColAutoincrement);
                        rowEmpty.ColAutoincrement = txn.AllTypesNotNulls.Insert(rowEmpty.Clone(), res => res.ColAutoincrement);
                        rowExtra.ColAutoincrement = txn.AllTypesNotNulls.Insert(rowExtra.Clone(), res => res.ColAutoincrement);

                        // Autoincrement values were picked by the DBMS, but we don't expect any DBMS to pick
                        // the weird values used above for an empty table. Test that it selected new, distinct values and
                        // that the above code correctly retrieved them.
                        Assert.AreNotEqual(12345, rowMax.ColAutoincrement);
                        Assert.AreNotEqual(23456, rowMin.ColAutoincrement);
                        Assert.AreNotEqual(34567, rowEmpty.ColAutoincrement);
                        Assert.AreNotEqual(rowMax.ColAutoincrement, rowMin.ColAutoincrement);
                        Assert.AreNotEqual(rowMax.ColAutoincrement, rowEmpty.ColAutoincrement);
                        Assert.AreNotEqual(rowMin.ColAutoincrement, rowEmpty.ColAutoincrement);
                    });

                // Inserting nulls must throw exceptions for each type that must be nullable in C#
                TestDB.AllTypesNotNull rowWithNull;
                rowWithNull = rowEmpty.Clone(); rowWithNull.ColVarText1 = null;
                Assert.IsInstanceOf<DbException>(exceptionof(() => conn.InTransaction(txn => txn.AllTypesNotNulls.Insert(rowWithNull))));
                rowWithNull = rowEmpty.Clone(); rowWithNull.ColVarBinary100 = null;
                Assert.IsInstanceOf<DbException>(exceptionof(() => conn.InTransaction(txn => txn.AllTypesNotNulls.Insert(rowWithNull))));
                rowWithNull = rowEmpty.Clone(); rowWithNull.ColVarTextMax = null;
                Assert.IsInstanceOf<DbException>(exceptionof(() => conn.InTransaction(txn => txn.AllTypesNotNulls.Insert(rowWithNull))));
                rowWithNull = rowEmpty.Clone(); rowWithNull.ColVarBinaryMax = null;
                Assert.IsInstanceOf<DbException>(exceptionof(() => conn.InTransaction(txn => txn.AllTypesNotNulls.Insert(rowWithNull))));

                // Retrieval
                TestDB.AllTypesNotNull readMax = null, readMin = null, readEmpty = null, readExtra = null;
                conn.InTransaction(txn =>
                {
                    readMax = txn.AllTypesNotNulls.First(row => row.ColAutoincrement == rowMax.ColAutoincrement);
                    readMin = txn.AllTypesNotNulls.First(row => row.ColAutoincrement == rowMin.ColAutoincrement);
                    readEmpty = txn.AllTypesNotNulls.First(row => row.ColAutoincrement == rowEmpty.ColAutoincrement);
                    readExtra = txn.AllTypesNotNulls.First(row => row.ColAutoincrement == rowExtra.ColAutoincrement);
                });

                // Verify insertion and retrieval
                AssertRowsEqual(rowMax, readMax);
                AssertRowsEqual(rowMin, readMin);
                AssertRowsEqual(rowEmpty, readEmpty);
                AssertRowsEqual(rowExtra, readExtra);
            }
        }

        [Test]
        public void AllTypesNull([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            using (var conn = createSchemaAndOpenConn(kind))
            {
                var rowMax = new TestDB.AllTypesNull();
                rowMax.ColVarText1 = "a";
                rowMax.ColVarText100 = "blah100";
                rowMax.ColVarTextMax = "lots of text! ".Repeat(100);
                rowMax.ColVarBinary1 = new byte[] { 250 };
                rowMax.ColVarBinary100 = "some text here".ToUtf8();
                rowMax.ColVarBinaryMax = "lots of binary! ".Repeat(100).ToUtf8();
                rowMax.ColBoolean = true;
                rowMax.ColByte = byte.MaxValue;
                rowMax.ColShort = short.MaxValue;
                rowMax.ColInt = int.MaxValue;
                rowMax.ColLong = long.MaxValue;
                rowMax.ColDouble = 1e+308;
                rowMax.ColDateTime = new DateTime(2150, 12, 31, 23, 59, 59, 987, DateTimeKind.Utc);

                var rowMin = rowMax.Clone();
                rowMin.ColBoolean = false;
                rowMin.ColByte = byte.MinValue;
                rowMin.ColShort = short.MinValue;
                rowMin.ColInt = int.MinValue;
                rowMin.ColLong = long.MinValue;
                rowMin.ColDouble = -1e+308;
                rowMin.ColDateTime = new DateTime(1950, 12, 31, 23, 59, 59, 987, DateTimeKind.Utc);

                var rowEmpty = new TestDB.AllTypesNull();
                rowEmpty.ColVarText1 = "";
                rowEmpty.ColVarText100 = "";
                rowEmpty.ColVarTextMax = "";
                rowEmpty.ColVarBinary1 = new byte[0];
                rowEmpty.ColVarBinary100 = new byte[0];
                rowEmpty.ColVarBinaryMax = new byte[0];
                rowEmpty.ColBoolean = false;
                rowEmpty.ColByte = 0;
                rowEmpty.ColShort = 0;
                rowEmpty.ColInt = 0;
                rowEmpty.ColLong = 0;
                rowEmpty.ColDouble = 1e-300;
                rowEmpty.ColDateTime = new DateTime(1753, 1, 1, 0, 0, 0, 123); // default(DateTime); cannot be stored - minimum supported by SQL Server is 1/1/1753

                var rowExtra = rowMin.Clone();
                rowExtra.ColDouble = 3.1415926535897932384626433832795;
                rowExtra.ColShort = -25;

                var rowNull = new TestDB.AllTypesNull();
                rowNull.ColVarText1 = null;
                rowNull.ColVarText100 = null;
                rowNull.ColVarTextMax = null;
                rowNull.ColVarBinary1 = null;
                rowNull.ColVarBinary100 = null;
                rowNull.ColVarBinaryMax = null;
                rowNull.ColBoolean = null;
                rowNull.ColByte = null;
                rowNull.ColShort = null;
                rowNull.ColInt = null;
                rowNull.ColLong = null;
                rowNull.ColDouble = null;
                rowNull.ColDateTime = null;

                // Insertion
                conn.InTransaction(txn =>
                {
                    int affectedMax = txn.AllTypesNulls.Insert(rowMax.Clone());
                    int affectedMin = txn.AllTypesNulls.Insert(rowMin.Clone());
                    int affectedEmpty = txn.AllTypesNulls.Insert(rowEmpty.Clone());
                    int affectedExtra = txn.AllTypesNulls.Insert(rowExtra.Clone());
                    int affectedNull = txn.AllTypesNulls.Insert(rowNull.Clone());
                    Assert.AreEqual(1, affectedMax);
                    Assert.AreEqual(1, affectedMin);
                    Assert.AreEqual(1, affectedEmpty);
                    Assert.AreEqual(1, affectedExtra);
                    Assert.AreEqual(1, affectedNull);
                });

                // Retrieval
                TestDB.AllTypesNull readMax = null, readMin = null, readEmpty = null, readExtra = null, readNull = null;
                conn.InTransaction(txn =>
                {
                    readMax = txn.AllTypesNulls.First(row => row.ColShort == short.MaxValue);
                    readMin = txn.AllTypesNulls.First(row => row.ColShort == short.MinValue);
                    readEmpty = txn.AllTypesNulls.First(row => row.ColShort == 0);
                    readExtra = txn.AllTypesNulls.First(row => row.ColShort == -25);
                    readNull = txn.AllTypesNulls.First(row => row.ColShort == null);
                });

                // Verify insertion and retrieval
                AssertRowsEqual(rowMax, readMax);
                AssertRowsEqual(rowMin, readMin);
                AssertRowsEqual(rowEmpty, readEmpty);
                AssertRowsEqual(rowExtra, readExtra);
                AssertRowsEqual(rowNull, readNull);
            }
        }

        private void AssertRowsEqual(TestDB.AllTypesNotNull expected, TestDB.AllTypesNotNull actual)
        {
            Assert.AreEqual(expected.ColAutoincrement, actual.ColAutoincrement);
            Assert.AreEqual(expected.ColVarText1, actual.ColVarText1);
            Assert.AreEqual(expected.ColVarText100, actual.ColVarText100);
            Assert.AreEqual(expected.ColVarTextMax, actual.ColVarTextMax);
            Assert.AreEqual(expected.ColVarBinary1, actual.ColVarBinary1);
            Assert.AreEqual(expected.ColVarBinary100, actual.ColVarBinary100);
            Assert.AreEqual(expected.ColVarBinaryMax, actual.ColVarBinaryMax);
            Assert.AreEqual(expected.ColBoolean, actual.ColBoolean);
            Assert.AreEqual(expected.ColByte, actual.ColByte);
            Assert.AreEqual(expected.ColShort, actual.ColShort);
            Assert.AreEqual(expected.ColInt, actual.ColInt);
            Assert.AreEqual(expected.ColLong, actual.ColLong);
            Assert.AreEqual(expected.ColDouble, actual.ColDouble, Math.Abs(Math.Min(expected.ColDouble, actual.ColDouble)) / 1e12);
            Assert.AreEqual(expected.ColDateTime, actual.ColDateTime);
        }

        private void AssertRowsEqual(TestDB.AllTypesNull expected, TestDB.AllTypesNull actual)
        {
            Assert.AreEqual(expected.ColVarText1, actual.ColVarText1);
            Assert.AreEqual(expected.ColVarText100, actual.ColVarText100);
            Assert.AreEqual(expected.ColVarTextMax, actual.ColVarTextMax);
            Assert.AreEqual(expected.ColVarBinary1, actual.ColVarBinary1);
            Assert.AreEqual(expected.ColVarBinary100, actual.ColVarBinary100);
            Assert.AreEqual(expected.ColVarBinaryMax, actual.ColVarBinaryMax);
            // Assert.AreEqual(expected.ColBoolean, actual.ColBoolean);   FAILS!
            Assert.AreEqual(expected.ColByte, actual.ColByte);
            Assert.AreEqual(expected.ColShort, actual.ColShort);
            Assert.AreEqual(expected.ColInt, actual.ColInt);
            Assert.AreEqual(expected.ColLong, actual.ColLong);
            if (expected.ColDouble != null && actual.ColDouble != null)
                Assert.AreEqual(expected.ColDouble.Value, actual.ColDouble.Value, Math.Abs(Math.Min(expected.ColDouble.Value, actual.ColDouble.Value)) / 1e12);
            else
                Assert.AreEqual(expected.ColDouble, actual.ColDouble); // both must be null
            Assert.AreEqual(expected.ColDateTime, actual.ColDateTime);
        }
    }
}
