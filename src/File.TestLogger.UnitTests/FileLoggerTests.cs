using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace File.TestLogger.UnitTests
{
    public class FileLoggerTests : TestBase
    {
        public FileLoggerTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory]
        [InlineData(Verbosity.Quiet)]
        [InlineData(Verbosity.Minimal)]
        [InlineData(Verbosity.Normal)]
        [InlineData(Verbosity.Detailed)]
        public void CapturesDiscoveryMessages(Verbosity verbosity)
        {
            MockEnvironmentProvider mockEnvironmentProvider = new MockEnvironmentProvider
            {
            };

            MockTestLoggerEvents testLoggerEvents = new MockTestLoggerEvents();

            StringTextWriter textWriter = new();

            FileLogger logger = new FileLogger(mockEnvironmentProvider, textWriter);

            logger.Initialize(testLoggerEvents, new Dictionary<string, string?>
            {
                [ParameterNames.TestRunDirectory] = "C:\\temp",
                [ParameterNames.Verbosity] = verbosity.ToString()
            });

            testLoggerEvents.OnDiscoveryMessage("Informational message", TestMessageLevel.Informational);
            testLoggerEvents.OnDiscoveryMessage("Error message", TestMessageLevel.Error);
            testLoggerEvents.OnDiscoveryMessage("Warning message", TestMessageLevel.Warning);

            TestRunStatistics testRunStatistics = new(
                executedTests: 15,
                new Dictionary<TestOutcome, long>
                {
                    [TestOutcome.Passed] = 10,
                    [TestOutcome.Failed] = 3,
                    [TestOutcome.Skipped] = 2,
                });

            testLoggerEvents.LogTestOutcomePassed(5, source: "One.UnitTests.dll");
            testLoggerEvents.LogTestOutcomePassed(5, source: "Two.UnitTests.dll");
            testLoggerEvents.LogTestOutcomeFailed(3, source: "One.UnitTests.dll");
            testLoggerEvents.LogTestOutcomeSkipped(2, source: "One.UnitTests.dll");

            testLoggerEvents.OnTestRunComplete(
                new TestRunCompleteEventArgs(
                    testRunStatistics,
                    isCanceled: false,
                    isAborted: false,
                    error: null,
                    attachmentSets: null,
                    elapsedTime: TimeSpan.FromSeconds(3)));

            string output = textWriter;

            switch (verbosity)
            {
                case Verbosity.Quiet:
                    output.ShouldBe(
@"Error message
Failed!  - Failed:     5, Passed:     5, Skipped:     0, Total:    10, Duration: 10 ms - One.UnitTests.dll
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 10 ms - Two.UnitTests.dll
"
                            , StringCompareShould.IgnoreLineEndings);
                    break;

                case Verbosity.Minimal:
                    output.ShouldBe(
@"Error message
Warning message
  Failed FailedTest.0 [10 ms]
  Failed FailedTest.1 [10 ms]
  Failed FailedTest.2 [10 ms]
  Failed SkippedTest.0 [10 ms]
  Failed SkippedTest.1 [10 ms]
Failed!  - Failed:     5, Passed:     5, Skipped:     0, Total:    10, Duration: 10 ms - One.UnitTests.dll
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 10 ms - Two.UnitTests.dll
",
                        StringCompareShould.IgnoreLineEndings);
                    break;

                case Verbosity.Normal:
                    output.ShouldBe(
@"Informational message
Error message
Warning message
  Passed PassedTest.0 [10 ms]
  Passed PassedTest.1 [10 ms]
  Passed PassedTest.2 [10 ms]
  Passed PassedTest.3 [10 ms]
  Passed PassedTest.4 [10 ms]
  Passed PassedTest.0 [10 ms]
  Passed PassedTest.1 [10 ms]
  Passed PassedTest.2 [10 ms]
  Passed PassedTest.3 [10 ms]
  Passed PassedTest.4 [10 ms]
  Failed FailedTest.0 [10 ms]
  Failed FailedTest.1 [10 ms]
  Failed FailedTest.2 [10 ms]
  Failed SkippedTest.0 [10 ms]
  Failed SkippedTest.1 [10 ms]
Test run failed.

Total tests: 15
     Passed: 10
     Failed: 3
    Skipped: 2
 Total time: 3.00 Seconds
",
                            StringCompareShould.IgnoreLineEndings);
                    break;

                case Verbosity.Detailed:
                    output.ShouldBe(
@"Informational message
Error message
Warning message
  Passed PassedTest.0 [10 ms]
  Passed PassedTest.1 [10 ms]
  Passed PassedTest.2 [10 ms]
  Passed PassedTest.3 [10 ms]
  Passed PassedTest.4 [10 ms]
  Passed PassedTest.0 [10 ms]
  Passed PassedTest.1 [10 ms]
  Passed PassedTest.2 [10 ms]
  Passed PassedTest.3 [10 ms]
  Passed PassedTest.4 [10 ms]
  Failed FailedTest.0 [10 ms]
  Failed FailedTest.1 [10 ms]
  Failed FailedTest.2 [10 ms]
  Failed SkippedTest.0 [10 ms]
  Failed SkippedTest.1 [10 ms]
Test run failed.

Total tests: 15
     Passed: 10
     Failed: 3
    Skipped: 2
 Total time: 3.00 Seconds
"
                            , StringCompareShould.IgnoreLineEndings);
                    break;

                default:
                    throw new ArgumentException($"Invalid verbosity value '{verbosity}'", nameof(verbosity));
            }
        }

        [Fact]
        public void Test1()
        {
            MockEnvironmentProvider mockEnvironmentProvider = new MockEnvironmentProvider
            {
            };

            MockTestLoggerEvents testLoggerEvents = new MockTestLoggerEvents();

            //TextWriter textWriter = new StringTextWriter();

            TextWriter textWriter = new TestOutputHelperTextWriter(TestOutputHelper);

            FileLogger logger = new FileLogger(mockEnvironmentProvider, textWriter);

            logger.Initialize(testLoggerEvents, new Dictionary<string, string?>
            {
                ["TestRunDirectory"] = "C:\\temp"
            });

            testLoggerEvents.OnTestRunMessage("Hello World");

            testLoggerEvents.OnTestRunComplete(new TestRunCompleteEventArgs(new TestRunStatistics(), isCanceled: false, isAborted: false, error: null, attachmentSets: null, elapsedTime: TimeSpan.FromSeconds(3)));

            //string? result = textWriter.ToString();
        }
    }
}
