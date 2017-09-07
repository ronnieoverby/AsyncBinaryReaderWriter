using Overby.Extensions.AsyncBinaryReaderWriter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Overby.Extensions.Tests
{
    public class WriterTests
    {
        public static IEnumerable<object[]> GetCompatCases()
        {
            // bool
            Func<AsyncBinaryWriter, object, Task> awriter = (w, v) => w.WriteAsync((bool)v);
            Action<BinaryWriter, object> swriter = (w, v) => w.Write((bool)v);
            Func<BinaryReader, object> reader = r => r.ReadBoolean();

            IEnumerable<object[]> makeTests<T>(params T[] values) =>
                values.Select(x => new object[] { x, awriter, swriter, reader }).ToArray();

            foreach (var test in makeTests(true, false))
                yield return test;

            // byte
            awriter = (w, v) => w.WriteAsync((byte)v);
            swriter = (w, v) => w.Write((byte)v);
            reader = r => r.ReadByte();

            foreach (var test in makeTests<byte>(byte.MaxValue, 0, 1))
                yield return test;

            // byte[]
            awriter = (w, v) => w.WriteAsync((byte[])v);
            swriter = (w, v) => w.Write((byte[])v);
            var rng = new Random();
            for (int i = 0; i < 10; i++)
            {
                reader = r => r.ReadBytes(i);
                var buffer = new byte[i];
                rng.NextBytes(buffer);
                yield return makeTests<byte[]>(buffer).Single();
            }

            // char 
            awriter = (w, v) => w.WriteAsync((char)v);
            swriter = (w, v) => w.Write((char)v);
            reader = r => r.ReadChar();

            foreach (var ch in "This is a test")
                yield return makeTests(ch).Single();

            // char[]
            awriter = (w, v) => w.WriteAsync((char[])v);
            swriter = (w, v) => w.Write((char[])v);
            for (int i = 0; i < 10; i++)
            {
                var s = Guid.NewGuid().ToString().Take(i).ToArray();
                reader = r => r.ReadChars(i);
                yield return makeTests<char[]>(s).Single();
            }

            //// decimal
            awriter = (w, v) => w.WriteAsync((decimal)v);
            swriter = (w, v) => w.Write((decimal)v);
            reader = r => r.ReadDecimal();
            foreach (var test in makeTests(decimal.MinValue, decimal.MinusOne, decimal.MaxValue, decimal.One, decimal.Zero))
                yield return test;

            // double
            awriter = (w, v) => w.WriteAsync((double)v);
            swriter = (w, v) => w.Write((double)v);
            reader = r => r.ReadDouble();
            foreach (var test in makeTests(double.MinValue, double.MaxValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, double.Epsilon))
                yield return test;

            // Int16
            awriter = (w, v) => w.WriteAsync((Int16)v);
            swriter = (w, v) => w.Write((Int16)v);
            reader = r => r.ReadInt16();
            foreach (var test in makeTests(short.MinValue, short.MaxValue, default(short), (short)1, (short)-1))
                yield return test;

            // Int32
            awriter = (w, v) => w.WriteAsync((int)v);
            swriter = (w, v) => w.Write((int)v);
            reader = r => r.ReadInt32();
            foreach (var test in makeTests(int.MaxValue, int.MinValue, 1, 0, -1))
                yield return test;

            // Int64
            awriter = (w, v) => w.WriteAsync((Int64)v);
            swriter = (w, v) => w.Write((Int64)v);
            reader = r => r.ReadInt64();
            foreach (var test in makeTests(long.MaxValue, long.MinValue, 1, 0, -1))
                yield return test;

            // sbyte
            awriter = (w, v) => w.WriteAsync((sbyte)v);
            swriter = (w, v) => w.Write((sbyte)v);
            reader = r => r.ReadSByte();
            foreach (var test in makeTests<sbyte>(sbyte.MaxValue, sbyte.MinValue, 1, 0, -1))
                yield return test;

            // single/float
            awriter = (w, v) => w.WriteAsync((float)v);
            swriter = (w, v) => w.Write((float)v);
            reader = r => r.ReadSingle();
            foreach (var test in makeTests(float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0, 1, -1))
                yield return test;

            // string
            foreach (var enc in new[] { Encoding.Default, Encoding.ASCII, Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF32, Encoding.UTF7, Encoding.UTF8, /*Encoding.GetEncoding(37)*/ })
            {
                awriter = (w, v) => w.WriteAsync((string)v);
                swriter = (w, v) => w.Write((string)v);
                reader = r => r.ReadString();

                var longString = new StringBuilder();
                for (int i = 0; i < 10000; i++)
                    longString.AppendLine(Guid.NewGuid().ToString());

                foreach (var test in makeTests("", "!", Guid.NewGuid().ToString(), longString.ToString()))
                    yield return test.Concat(new[] { enc }).ToArray();
            }

            // ushort / uint16
            awriter = (w, v) => w.WriteAsync((ushort)v);
            swriter = (w, v) => w.Write((ushort)v);
            reader = r => r.ReadUInt16();
            foreach (var test in makeTests<ushort>(ushort.MinValue, ushort.MaxValue, 1))
                yield return test;

            // uint / uint32
            awriter = (w, v) => w.WriteAsync((uint)v);
            swriter = (w, v) => w.Write((uint)v);
            reader = r => r.ReadUInt32();
            foreach (var test in makeTests<uint>(uint.MinValue, uint.MaxValue, 1))
                yield return test;

            // ulong / uint64
            awriter = (w, v) => w.WriteAsync((ulong)v);
            swriter = (w, v) => w.Write((ulong)v);
            reader = r => r.ReadUInt64();
            foreach (var test in makeTests<ulong>(ulong.MinValue, ulong.MaxValue, 1))
                yield return test;
        }

        [Theory, MemberData(nameof(GetCompatCases))]
        public async Task Test(object value, Func<AsyncBinaryWriter, object, Task> awriteFunc, Action<BinaryWriter, object> swriteFunc, Func<BinaryReader, object> readFunc, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var sstream = new MemoryStream();
            var swriter = new BinaryWriter(sstream, encoding);
            swriteFunc(swriter, value);
            swriter.Flush();
            sstream.Position = 0;

            var astream = new MemoryStream();
            var awriter = new AsyncBinaryWriter(astream, encoding);
            await awriteFunc(awriter, value);
            await awriter.FlushAsync();
            astream.Position = 0;

            // The following 2 assertions seem redundant. That's okay, though.

            // did AsyncBinaryWriter and BinaryWriter produce same bytes?
            Assert.Equal(sstream.ToArray(), astream.ToArray());

            // did AsyncBinaryWriter produce bytes that can be read by BinaryReader?
            var reader = new BinaryReader(astream, encoding);
            var result = readFunc(reader);
            Assert.Equal(value, result);
        }
    }
}