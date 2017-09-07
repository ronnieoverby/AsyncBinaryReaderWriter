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
            const string s1 = nameof(CanGetStringAndRelease);

            var sb = StringBuilderCache.Acquire();
            sb.Append(s1);
            var s2 = StringBuilderCache.GetStringAndRelease(sb);
            Assert.Equal(s1, s2);            
        }
    }
}
