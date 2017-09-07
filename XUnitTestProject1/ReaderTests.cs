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
    public class ReaderTests
    {
        public static IEnumerable<object[]> GetCompatCases()
        {
            // bool
            Action<BinaryWriter, object> writer = (w, v) => w.Write((bool)v);
            Func<AsyncBinaryReader, Task<object>> reader = async r => await r.ReadBooleanAsync();

            IEnumerable<object[]> makeTests<T>(params T[] values) =>
                values.Select(x => new object[] { x, writer, reader }).ToArray();

            foreach (var test in makeTests(true, false))
                yield return test;

            // byte
            writer = (w, v) => w.Write((byte)v);
            reader = async r => await r.ReadByteAsync();

            foreach (var test in makeTests<byte>(byte.MaxValue, 0, 1))
                yield return test;

            // byte[]
            writer = (w, v) => w.Write((byte[])v);
            var rng = new Random();
            for (int i = 0; i < 10; i++)
            {
                reader = async r => await r.ReadBytesAsync(i);
                var buffer = new byte[i];
                rng.NextBytes(buffer);
                yield return makeTests<byte[]>(buffer).Single();
            }

            // char 
            writer = (w, v) => w.Write((char)v);
            reader = async r => await r.ReadCharAsync();

            foreach (var ch in "This is a test")
                yield return makeTests(ch).Single();

            // char[]
            writer = (w, v) => w.Write((char[])v);
            for (int i = 0; i < 10; i++)
            {
                var s = Guid.NewGuid().ToString().Take(i).ToArray();
                reader = async r => await r.ReadCharsAsync(i);
                yield return makeTests<char[]>(s).Single();
            }

            // decimal
            writer = (w, v) => w.Write((decimal)v);
            reader = async r => await r.ReadDecimalAsync();
            foreach (var test in makeTests(decimal.MinValue, decimal.MinusOne, decimal.MaxValue, decimal.One, decimal.Zero))
                yield return test;

            // double
            writer = (w, v) => w.Write((double)v);
            reader = async r => await r.ReadDoubleAsync();
            foreach (var test in makeTests(double.MinValue, double.MaxValue, double.NaN, double.NegativeInfinity, double.PositiveInfinity, double.Epsilon))
                yield return test;

            // Int16
            writer = (w, v) => w.Write((Int16)v);
            reader = async r => await r.ReadInt16Async();
            foreach (var test in makeTests(short.MinValue, short.MaxValue, default(short), (short)1, (short)-1))
                yield return test;

            // Int32
            writer = (w, v) => w.Write((int)v);
            reader = async r => await r.ReadInt32Async();
            foreach (var test in makeTests(int.MaxValue, int.MinValue, 1, 0, -1))
                yield return test;

            // Int64
            writer = (w, v) => w.Write((Int64)v);
            reader = async r => await r.ReadInt64Async();
            foreach (var test in makeTests(long.MaxValue, long.MinValue, 1, 0, -1))
                yield return test;

            // sbyte
            writer = (w, v) => w.Write((sbyte)v);
            reader = async r => await r.ReadSByteAsync();
            foreach (var test in makeTests<sbyte>(sbyte.MaxValue, sbyte.MinValue, 1, 0, -1))
                yield return test;

            // single/float
            writer = (w, v) => w.Write((float)v);
            reader = async r => await r.ReadSingleAsync();
            foreach (var test in makeTests(float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0, 1, -1))
                yield return test;

            // string
            foreach (var enc in new[] { Encoding.Default, Encoding.ASCII, Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF32, Encoding.UTF7, Encoding.UTF8, /*Encoding.GetEncoding(37)*/ })
            {
                writer = (w, v) => w.Write((string)v);
                reader = async r => await r.ReadStringAsync();
                foreach (var test in makeTests("Ronnie Overby", Guid.NewGuid().ToString()))
                    yield return test.Concat(new[] { enc }).ToArray();
            }

            // ushort / uint16
            writer = (w, v) => w.Write((ushort)v);
            reader = async r => await r.ReadUInt16Async();
            foreach (var test in makeTests<ushort>(ushort.MinValue, ushort.MaxValue, 1))
                yield return test;

            // uint / uint32
            writer = (w, v) => w.Write((uint)v);
            reader = async r => await r.ReadUInt32Async();
            foreach (var test in makeTests<uint>(uint.MinValue, uint.MaxValue, 1))
                yield return test;

            // ulong / uint64
            writer = (w, v) => w.Write((ulong)v);
            reader = async r => await r.ReadUInt64Async();
            foreach (var test in makeTests<ulong>(ulong.MinValue, ulong.MaxValue, 1))
                yield return test;
        }

        [Theory, MemberData(nameof(GetCompatCases))]
        public async Task Test(object value, Action<BinaryWriter, object> writeFunc, Func<AsyncBinaryReader, Task<object>> readFunc, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream, encoding);
            writeFunc(writer, value);
            writer.Flush();
            stream.Position = 0;

            var reader = new AsyncBinaryReader(stream, encoding);
            var result = await readFunc(reader);
            Assert.Equal(value, result);
        }
    }
}