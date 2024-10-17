using System;
using Xunit.Abstractions;

namespace Test.Common
{
    public abstract class TestBase : IDisposable
    {
        public TestBase(ITestOutputHelper testOutput)
        {
            TestOutputHelper = testOutput;
        }

        public ITestOutputHelper TestOutputHelper { get; }

        public virtual void Dispose()
        {
        }
    }
}
