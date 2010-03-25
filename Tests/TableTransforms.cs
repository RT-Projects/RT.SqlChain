using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using RT.SqlChain;
using RT.SqlChain.Schema;
using RT.Util;
using RT.Util.Xml;

namespace RT.SqlChainTests
{
    public partial class Tests
    {
        [Test]
        public void TestTableTransforms([Values(DbmsKind.Sqlite, DbmsKind.SqlServer)] DbmsKind kind)
        {
            ConnectionInfo conninfo = getConnInfo(kind, null);
            conninfo.Log = Console.Out;

            TestDB.CreateSchema(conninfo);
            Assert.IsTrue(conninfo.SchemaExists());

            var bytesFrom0To100 = Enumerable.Range(0, 100).Select(b => (byte) b).ToArray();

            using (var testdb = new TestDB(conninfo))
                testdb.ExecuteInTransaction(tr =>
                {
                    tr.AllTypesNotNulls.Insert(new TestDB.AllTypesNotNull
                    {
                        ColBoolean = true,
                        ColByte = 1,
                        ColDateTime = DateTime.Parse("2000-01-01 01:47"),
                        ColDouble = 4.7d,
                        ColInt = 47,
                        ColLong = 42,
                        ColShort = 2,
                        ColVarBinary1 = new byte[] { 3 },
                        ColVarBinary100 = bytesFrom0To100,
                        ColVarBinaryMax = new byte[] { 5, 55 },
                        ColVarText1 = "a",
                        ColVarText100 = "The quick brown fox jumps over the lazy dog.",
                        ColVarTextMax = "Jackdaws love my big sphinx of quartz."
                    });
                });

            using (var conn = conninfo.CreateConnection())
            {
                conn.Open();
                var retriever = conninfo.CreateSchemaRetriever(conn);
                var schema = retriever.RetrieveSchema();
                var mutator = conninfo.CreateSchemaMutator(conn, false);
                var table = schema.Table("AllTypesNotNull");

                mutator.TransformTable(table,
                    new AddColumn { InsertAtIndex = 2, NewColumn = new ColumnInfo { Name = "VarText1Length", Type = new TypeInfo { BasicType = BasicType.Int, Nullable = false } }, Populate = mutator.SqlLength("oldtable.ColVarText1") },
                    new AddColumn { InsertAtIndex = 4, NewColumn = new ColumnInfo { Name = "VarText100Length", Type = new TypeInfo { BasicType = BasicType.Int, Nullable = false } }, Populate = mutator.SqlLength("oldtable.ColVarText100") },
                    new AddColumn { InsertAtIndex = 6, NewColumn = new ColumnInfo { Name = "VarTextMaxLength", Type = new TypeInfo { BasicType = BasicType.Int, Nullable = false } }, Populate = mutator.SqlLength("oldtable.ColVarTextMax") },
                    new AddColumn { InsertAtIndex = 8, NewColumn = new ColumnInfo { Name = "VarBin1Length", Type = new TypeInfo { BasicType = BasicType.Int, Nullable = false } }, Populate = mutator.SqlLength("oldtable.ColVarBinary1") },
                    new AddColumn { InsertAtIndex = 10, NewColumn = new ColumnInfo { Name = "VarBin100Length", Type = new TypeInfo { BasicType = BasicType.Int, Nullable = false } }, Populate = mutator.SqlLength("oldtable.ColVarBinary100") },
                    new AddColumn { InsertAtIndex = 12, NewColumn = new ColumnInfo { Name = "VarBinMaxLength", Type = new TypeInfo { BasicType = BasicType.Int, Nullable = false } }, Populate = mutator.SqlLength("oldtable.ColVarBinaryMax") },
                    new DeleteColumn { Column = table.Column("ColDouble") },
                    new MoveColumn { Column = table.Column("ColBoolean"), NewIndex = int.MaxValue },
                    new MoveColumn { Column = table.Column("ColShort"), NewIndex = 0 },
                    new RenameColumn { Column = table.Column("ColInt"), NewName = "FortySeven" }
                );
            }

            using (var conn = conninfo.CreateConnection())
            {
                conn.Open();
                var retriever = conninfo.CreateSchemaRetriever(conn);
                var newSchema = retriever.RetrieveSchema();
                var cols = newSchema.Table("AllTypesNotNull").Columns.ToArray();
                Assert.AreEqual(19, cols.Length);

                Assert.AreEqual("ColShort", cols[0].Name);
                Assert.AreEqual("ColAutoincrement", cols[1].Name);
                Assert.AreEqual("ColVarText1", cols[2].Name);
                Assert.AreEqual("VarText1Length", cols[3].Name);
                Assert.AreEqual("ColVarText100", cols[4].Name);
                Assert.AreEqual("VarText100Length", cols[5].Name);
                Assert.AreEqual("ColVarTextMax", cols[6].Name);
                Assert.AreEqual("VarTextMaxLength", cols[7].Name);
                Assert.AreEqual("ColVarBinary1", cols[8].Name);
                Assert.AreEqual("VarBin1Length", cols[9].Name);
                Assert.AreEqual("ColVarBinary100", cols[10].Name);
                Assert.AreEqual("VarBin100Length", cols[11].Name);
                Assert.AreEqual("ColVarBinaryMax", cols[12].Name);
                Assert.AreEqual("VarBinMaxLength", cols[13].Name);
                Assert.AreEqual("ColByte", cols[14].Name);
                Assert.AreEqual("FortySeven", cols[15].Name);
                Assert.AreEqual("ColLong", cols[16].Name);
                Assert.AreEqual("ColDateTime", cols[17].Name);
                Assert.AreEqual("ColBoolean", cols[18].Name);

                Assert.AreEqual(BasicType.Short, cols[0].Type.BasicType);
                Assert.AreEqual(BasicType.Autoincrement, cols[1].Type.BasicType);
                Assert.AreEqual(BasicType.VarText, cols[2].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[3].Type.BasicType);
                Assert.AreEqual(BasicType.VarText, cols[4].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[5].Type.BasicType);
                Assert.AreEqual(BasicType.VarText, cols[6].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[7].Type.BasicType);
                Assert.AreEqual(BasicType.VarBinary, cols[8].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[9].Type.BasicType);
                Assert.AreEqual(BasicType.VarBinary, cols[10].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[11].Type.BasicType);
                Assert.AreEqual(BasicType.VarBinary, cols[12].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[13].Type.BasicType);
                Assert.AreEqual(BasicType.Byte, cols[14].Type.BasicType);
                Assert.AreEqual(BasicType.Int, cols[15].Type.BasicType);
                Assert.AreEqual(BasicType.Long, cols[16].Type.BasicType);
                Assert.AreEqual(BasicType.DateTime, cols[17].Type.BasicType);
                Assert.AreEqual(BasicType.Boolean, cols[18].Type.BasicType);

                for (int i = 0; i < 19; i++)
                    Assert.IsFalse(cols[i].Type.Nullable);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM AllTypesNotNull";
                    using (var reader = cmd.ExecuteReader())
                    {
                        Assert.IsTrue(reader.Read());

                        Assert.AreEqual(2, reader.GetValue(0));
                        // skip ColAutoIncrement
                        Assert.AreEqual("a", reader.GetValue(2));
                        Assert.AreEqual("a".Length, reader.GetValue(3));
                        Assert.AreEqual("The quick brown fox jumps over the lazy dog.", reader.GetValue(4));
                        Assert.AreEqual("The quick brown fox jumps over the lazy dog.".Length, reader.GetValue(5));
                        Assert.AreEqual("Jackdaws love my big sphinx of quartz.", reader.GetValue(6));
                        Assert.AreEqual("Jackdaws love my big sphinx of quartz.".Length, reader.GetValue(7));
                        Assert.AreEqual(new byte[] { 3 }, reader.GetValue(8));
                        Assert.AreEqual(1, reader.GetValue(9));
                        Assert.AreEqual(bytesFrom0To100, reader.GetValue(10));
                        Assert.AreEqual(100, reader.GetValue(11));
                        Assert.AreEqual(new byte[] { 5, 55 }, reader.GetValue(12));
                        Assert.AreEqual(2, reader.GetValue(13));
                        Assert.AreEqual(1, reader.GetValue(14));
                        Assert.AreEqual(47, reader.GetValue(15));
                        Assert.AreEqual(42, reader.GetValue(16));
                        Assert.AreEqual(DateTime.Parse("2000-01-01 01:47"), reader.GetValue(17));
                        Assert.AreEqual(true, reader.GetValue(18));
                        reader.Close();
                    }
                }
            }
        }
    }
}
