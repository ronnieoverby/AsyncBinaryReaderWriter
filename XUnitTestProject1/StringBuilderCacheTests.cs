using Overby.Extensions.AsyncBinaryReaderWriter;
using System.Text;
using Xunit;

namespace CoreTests
{
    public class StringBuilderCacheTests
    {
        [Fact]
        public void CanAcquire()
        {
            var sb = StringBuilderCache.Acquire();
            Assert.NotNull(sb);
        }

        [Fact]
        public void CanRelease()
        {
            var sb = StringBuilderCache.Acquire();
            StringBuilderCache.Release(sb);
        }

        [Fact]
        public void CanGetStringAndRelease()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append(nameof(CanGetStringAndRelease));
            var s = StringBuilderCache.GetStringAndRelease(sb);
            Assert.Equal(nameof(CanGetStringAndRelease), s);            
        }
    }
}
