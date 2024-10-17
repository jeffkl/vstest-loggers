using System;
using System.Threading;

namespace Test.Common
{
    public sealed class MockTimeProvider : TimeProvider
    {
        public static readonly DateTimeOffset DefaultUtcNow = new DateTimeOffset(2024, 10, 17, 15, 13, 34, 841, TimeSpan.FromHours(10));

        private readonly DateTimeOffset _utcNow;

        public MockTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public static MockTimeProvider Default { get; } = new MockTimeProvider(DefaultUtcNow);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => throw new NotImplementedException();

        public override long GetTimestamp() => throw new NotImplementedException();

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
