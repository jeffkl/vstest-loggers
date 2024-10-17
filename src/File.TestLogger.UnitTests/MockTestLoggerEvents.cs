using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Threading.Tasks;

namespace File.TestLogger.UnitTests
{
    internal sealed class MockTestLoggerEvents : TestLoggerEvents
    {
        private readonly DateTimeOffset _stableEndTime;
        private readonly DateTimeOffset _stableStartTime = DateTimeOffset.Now;

        public MockTestLoggerEvents()
        {
            _stableEndTime = _stableStartTime + TimeSpan.FromMilliseconds(10);
        }

        public override event EventHandler<DiscoveredTestsEventArgs>? DiscoveredTests;

        public override event EventHandler<DiscoveryCompleteEventArgs>? DiscoveryComplete;

        public override event EventHandler<TestRunMessageEventArgs>? DiscoveryMessage;

        public override event EventHandler<DiscoveryStartEventArgs>? DiscoveryStart;

        public override event EventHandler<TestResultEventArgs>? TestResult;

        public override event EventHandler<TestRunCompleteEventArgs>? TestRunComplete;

        public override event EventHandler<TestRunMessageEventArgs>? TestRunMessage;

        public override event EventHandler<TestRunStartEventArgs>? TestRunStart;

        public void LogTestOutcome(int count, TestOutcome testOutcome, string testNamePrefix = "Test.", string source = "UnitTests.dll")
        {
            for (int i = 0; i < count; i++)
            {
                OnTestResult(
                   new TestResultEventArgs(
                       new TestResult(new TestCase($"{testNamePrefix}{i}", new Uri("executor://test"), source))
                       {
                           Outcome = testOutcome,
                           StartTime = _stableStartTime,
                           EndTime = _stableEndTime,
                           Duration = TimeSpan.FromMilliseconds(10),
                       }));
            }
        }

        public void LogTestOutcomeFailed(int count, string testNamePrefix = "FailedTest.", string source = "UnitTests.dll")
        {
            LogTestOutcome(count, TestOutcome.Failed, testNamePrefix, source);
        }

        public void LogTestOutcomePassed(int count, string testNamePrefix = "PassedTest.", string source = "UnitTests.dll")
        {
            LogTestOutcome(count, TestOutcome.Passed, testNamePrefix, source);
        }

        public void LogTestOutcomeSkipped(int count, string testNamePrefix = "SkippedTest.", string source = "UnitTests.dll")
        {
            LogTestOutcome(count, TestOutcome.Failed, testNamePrefix, source);
        }

        public void OnDiscoveredTestsEventArgs(DiscoveredTestsEventArgs e) => DiscoveredTests?.Invoke(this, e);

        public void OnDiscoveryComplete(DiscoveryCompleteEventArgs e) => DiscoveryComplete?.Invoke(this, e);

        public void OnDiscoveryMessage(string message, TestMessageLevel testMessageLevel = TestMessageLevel.Informational) => OnDiscoveryMessage(new TestRunMessageEventArgs(testMessageLevel, message));

        public void OnDiscoveryMessage(TestRunMessageEventArgs e) => DiscoveryMessage?.Invoke(this, e);

        public void OnDiscoveryStart(DiscoveryStartEventArgs e) => DiscoveryStart?.Invoke(this, e);

        public void OnTestResult(TestResultEventArgs e) => TestResult?.Invoke(this, e);

        public void OnTestRunComplete(TestRunCompleteEventArgs e) => TestRunComplete?.Invoke(this, e);

        public void OnTestRunMessage(TestRunMessageEventArgs e) => TestRunMessage?.Invoke(this, e);

        public void OnTestRunMessage(string message, TestMessageLevel testMessageLevel = TestMessageLevel.Informational) => OnTestRunMessage(new TestRunMessageEventArgs(testMessageLevel, message));

        public void OnTestRunStart(TestRunStartEventArgs e) => TestRunStart?.Invoke(this, e);
    }
}
