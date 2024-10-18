using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;

namespace Test.Common
{
    public sealed class NullTestLoggerEvents : TestLoggerEvents
    {
        private NullTestLoggerEvents()
        {
        }

#pragma warning disable CS0067 // The event 'eventName' is never used
        public override event EventHandler<DiscoveredTestsEventArgs>? DiscoveredTests;

        public override event EventHandler<DiscoveryCompleteEventArgs>? DiscoveryComplete;

        public override event EventHandler<TestRunMessageEventArgs>? DiscoveryMessage;

        public override event EventHandler<DiscoveryStartEventArgs>? DiscoveryStart;

        public override event EventHandler<TestResultEventArgs>? TestResult;

        public override event EventHandler<TestRunCompleteEventArgs>? TestRunComplete;

        public override event EventHandler<TestRunMessageEventArgs>? TestRunMessage;

        public override event EventHandler<TestRunStartEventArgs>? TestRunStart;
#pragma warning restore CS0067

        public static TestLoggerEvents Instance { get; } = new NullTestLoggerEvents();
    }
}
