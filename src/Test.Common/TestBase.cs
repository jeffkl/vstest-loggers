using System;
using Xunit.Abstractions;

namespace Test.Common
{
    public abstract class TestBase : IDisposable
    {
        public TestBase(ITestOutputHelper testOutput)
        {
            TestOutput = testOutput;
        }

        public ITestOutputHelper TestOutput { get; }

        public virtual void Dispose()
        {
        }
    }
}
