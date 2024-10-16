using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;

namespace File.TestLogger.UnitTests
{
    internal sealed class MockTestLoggerEvents : TestLoggerEvents
    {
        public override event EventHandler<DiscoveredTestsEventArgs>? DiscoveredTests;

        public override event EventHandler<DiscoveryCompleteEventArgs>? DiscoveryComplete;

        public override event EventHandler<TestRunMessageEventArgs>? DiscoveryMessage;

        public override event EventHandler<DiscoveryStartEventArgs>? DiscoveryStart;

        public override event EventHandler<TestResultEventArgs>? TestResult;

        public override event EventHandler<TestRunCompleteEventArgs>? TestRunComplete;

        public override event EventHandler<TestRunMessageEventArgs>? TestRunMessage;

        public override event EventHandler<TestRunStartEventArgs>? TestRunStart;

        public void OnDiscoveredTestsEventArgs(DiscoveredTestsEventArgs e) => DiscoveredTests?.Invoke(this, e);

        public void OnDiscoveryComplete(DiscoveryCompleteEventArgs e) => DiscoveryComplete?.Invoke(this, e);

        public void OnDiscoveryMessage(TestRunMessageEventArgs e) => DiscoveryMessage?.Invoke(this, e);

        public void OnDiscoveryStart(DiscoveryStartEventArgs e) => DiscoveryStart?.Invoke(this, e);

        public void OnTestResult(TestResultEventArgs e) => TestResult?.Invoke(this, e);

        public void OnTestRunComplete(TestRunCompleteEventArgs e) => TestRunComplete?.Invoke(this, e);

        public void OnTestRunMessage(TestRunMessageEventArgs e) => TestRunMessage?.Invoke(this, e);

        public void OnTestRunMessage(string message, TestMessageLevel testMessageLevel = TestMessageLevel.Informational) => OnTestRunMessage(new TestRunMessageEventArgs(testMessageLevel, message));

        public void OnTestRunStart(TestRunStartEventArgs e) => TestRunStart?.Invoke(this, e);
    }
}
