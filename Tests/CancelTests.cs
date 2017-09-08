using Overby.Extensions.AsyncBinaryReaderWriter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tests;
using Xunit;

namespace Overby.Extensions.Tests
{
    public class CancelTests
    {
        public static IEnumerable<object[]> GetWriterCases()
        {
            object[] test(Func<AsyncBinaryWriter, CancellationToken, Task> f) =>
                new object[] { f };

            yield return test((w,t) => w.WriteAsync(true, t));
            yield return test((w,t) => w.WriteAsync(default(byte), t));
            yield return test((w,t) => w.WriteAsync(new byte[4], t));
            yield return test((w,t) => w.WriteAsync('a', t));
            yield return test((w,t) => w.WriteAsync("asdf".ToCharArray(), t));
            yield return test((w,t) => w.WriteAsync(123m, t));
            yield return test((w,t) => w.WriteAsync(1.0d, t));
            yield return test((w,t) => w.WriteAsync(1.0f, t));
            yield return test((w,t) => w.WriteAsync(default(short), t));
            yield return test((w,t) => w.WriteAsync(default(ushort), t));
            yield return test((w,t) => w.WriteAsync(default(long), t));
            yield return test((w,t) => w.WriteAsync(default(ulong), t));
            yield return test((w,t) => w.WriteAsync(default(uint), t));
            yield return test((w,t) => w.WriteAsync(default(int), t));
            yield return test((w,t) => w.WriteAsync(Guid.NewGuid().ToString(), t));
            yield return test((w,t) => w.WriteAsync(new byte[4], 0, 4, t));
            yield return test((w,t) => w.WriteAsync(new char[4], 0, 4, t));
        }

        public static IEnumerable<object[]> GetReaderCases()
        {
            object[] test(Func<AsyncBinaryReader, CancellationToken, Task> f) =>
                new object[] { f };

            yield return test((r, t) => r.Read7BitEncodedIntAsync(t));
            yield return test((r, t) => r.ReadAsync(t));
            yield return test((r, t) => r.ReadBooleanAsync(t));
            yield return test((r, t) => r.ReadByteAsync(t));
            yield return test((r, t) => r.ReadBytesAsync(10, t));
            yield return test((r, t) => r.ReadCharAsync(t));
            yield return test((r, t) => r.ReadCharsAsync(10, t));
            yield return test((r, t) => r.ReadDecimalAsync(t));
            yield return test((r, t) => r.ReadDoubleAsync(t));
            yield return test((r, t) => r.ReadInt16Async(t));
            yield return test((r, t) => r.ReadInt32Async(t));
            yield return test((r, t) => r.ReadInt64Async(t));
            yield return test((r, t) => r.ReadUInt16Async(t));
            yield return test((r, t) => r.ReadUInt32Async(t));
            yield return test((r, t) => r.ReadUInt64Async(t));
            yield return test((r, t) => r.ReadSByteAsync(t));
            yield return test((r, t) => r.ReadSingleAsync(t));
            yield return test((r, t) => r.ReadStringAsync(t));
        }

        [Theory, MemberData(nameof(GetReaderCases))]
        public async Task TestReaderCancellation(Func<AsyncBinaryReader, CancellationToken, Task> f)
        {
            using (var cts = new CancellationTokenSource())
            {
                var ct = cts.Token;
                var reader = new AsyncBinaryReader(new SlowStream());
                cts.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(() => f(reader, ct));
            }
        }

        [Theory, MemberData(nameof(GetWriterCases))]
        public async Task TestWriterCancellation(Func<AsyncBinaryWriter, CancellationToken, Task> f)
        {
            using (var cts = new CancellationTokenSource())
            {
                var ct = cts.Token;
                var writer = new AsyncBinaryWriter(new SlowStream());
                cts.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(() => f(writer, ct));                
            }
        }
    }
}