using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class SlowStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        long _len = long.MaxValue;
        public override long Length => _len;

        public override long Position { get; set; }

        public override void Flush()
        {
            GoSlow();
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await GoSlowAsync(cancellationToken);
            await base.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            GoSlowAsync(cancellationToken);

        void GoSlow() => GoSlowAsync(default(CancellationToken)).Wait();

        Task GoSlowAsync(CancellationToken ct) =>
            Task.Delay(5000, ct);

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, default(CancellationToken)).Result;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            await GoSlowAsync(ct);
            Position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            _len = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            GoSlow();
        }

        public override Task WriteAsync(byte[] b, int o, int c, CancellationToken ct) =>
            GoSlowAsync(ct);
    }
}
