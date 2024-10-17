using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace File.TestLogger.UnitTests
{
    public class FileLoggerTests : TestBase
    {
        public const string DefaultCurrentDirectory = @"C:\src\ProjectA";
        public const string DefaultTestRunDirectory = @"C:\temp\TestResults";

        public readonly MockEnvironmentProvider DefaultEnvironmentProvider = new(addExistingEnvironmentVariables: false)
        {
            CurrentDirectory = DefaultCurrentDirectory,
            MachineName = "MachineA",
            UserName = "UserA",
        };

        public readonly IFileSystem DefaultFileSystem = new MockFileSystem();

        public readonly Dictionary<string, string?> DefaultTestRunParameters = new(StringComparer.OrdinalIgnoreCase)
        {
            [ParameterNames.TestRunDirectory] = DefaultTestRunDirectory,
        };

        public readonly TimeProvider DefaultTimeProvider = TimeProvider.System;

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
            const string logFilePath = @"C:\file.log";

            IFileSystem fileSystem = new MockFileSystem();

            MockTestLoggerEvents testLoggerEvents = new();

            DefaultTestRunParameters[ParameterNames.LogFileName] = logFilePath;
            DefaultTestRunParameters[ParameterNames.Verbosity] = verbosity.ToString();

            FileLogger logger = new(DefaultEnvironmentProvider, fileSystem, DefaultTimeProvider);

            logger.Initialize(testLoggerEvents, DefaultTestRunParameters);

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

            string output = fileSystem.File.ReadAllText(logger.LogFile!.FullName);

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

        [Theory]
        [InlineData("True", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("FalSe", false)]
        [InlineData("Invalid", null)]
        public void InitializeSetsAppend(string value, bool? expected)
        {
            DefaultTestRunParameters[ParameterNames.Append] = value;

            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, DefaultTimeProvider);

            logger.Initialize(NullTestLoggerEvents.Instance, DefaultTestRunParameters);

            if (expected is null)
            {
                logger.Append.ShouldBeNull();
            }
            else if (expected is true)
            {
                logger.Append?.ShouldBeTrue();
            }
            else if (expected is false)
            {
                logger.Append?.ShouldBeFalse();
            }
        }

        [Fact]
        public void InitializeSetsLogFileNameDefault()
        {
            MockTimeProvider timeProvider = MockTimeProvider.Default;

            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, timeProvider);

            logger.Initialize(NullTestLoggerEvents.Instance, DefaultTestRunParameters);

            logger.LogFile.ShouldNotBeNull();

            logger.LogFile.FullName.ShouldBe(DefaultFileSystem.Path.Combine(DefaultTestRunDirectory, "vstest.console.UserA_MachineA_20241017_081334841.log"));

            logger.LogFile.Directory.ShouldNotBeNull();

            logger.LogFile.Directory.Exists.ShouldBeTrue();
        }

        [Theory]
        [InlineData(DefaultTestRunDirectory, "SubDirectory", "MyLogFile.log")]
        [InlineData("AnotherDirectory", "OtherLogFile.log")]
        public void InitializeSetsLogFileNameRelativeOrAbsolute(params string[] paths)
        {
            MockTimeProvider timeProvider = MockTimeProvider.Default;

            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, timeProvider);

            DefaultTestRunParameters[ParameterNames.LogFileName] = DefaultFileSystem.Path.Combine(paths);

            logger.Initialize(NullTestLoggerEvents.Instance, DefaultTestRunParameters);

            logger.LogFile.ShouldNotBeNull();

            logger.LogFile.FullName.ShouldBe(DefaultFileSystem.Path.Combine(DefaultTestRunDirectory, DefaultTestRunParameters[ParameterNames.LogFileName]!));

            logger.LogFile.Directory.ShouldNotBeNull();

            logger.LogFile.Directory.Exists.ShouldBeTrue();
        }

        [Theory]
        [InlineData(".NETFramework,Version=v4.7.2", "net472")]
        [InlineData(".NETCoreApp,Version=v8.0", "net8.0")]
        [InlineData("net472", "net472")]
        [InlineData("invalid", null)]
        public void InitializeSetsTargetFramework(string value, string? expected)
        {
            DefaultTestRunParameters[ParameterNames.TargetFramework] = value;

            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, DefaultTimeProvider);

            logger.Initialize(NullTestLoggerEvents.Instance, DefaultTestRunParameters);

            logger.TargetFramework.ShouldBe(expected);
        }

        [Fact]
        public void InitializeSetsTestRunDirectory()
        {
            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, DefaultTimeProvider);

            logger.Initialize(NullTestLoggerEvents.Instance, DefaultTestRunParameters);

            logger.TestRunDirectory.ShouldBe(DefaultTestRunDirectory);
        }

        [Fact]
        public void InitializeSetsTestRunDirectoryToCurrentDirectory()
        {
            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, DefaultTimeProvider);

            logger.Initialize(NullTestLoggerEvents.Instance, new Dictionary<string, string?>());

            logger.TestRunDirectory.ShouldBe(DefaultEnvironmentProvider.CurrentDirectory);
        }

        [Theory]
        [InlineData(nameof(Verbosity.Quiet), Verbosity.Quiet)]
        [InlineData(nameof(Verbosity.Minimal), Verbosity.Minimal)]
        [InlineData(nameof(Verbosity.Normal), Verbosity.Normal)]
        [InlineData(nameof(Verbosity.Detailed), Verbosity.Detailed)]
        [InlineData("", Verbosity.Normal)]
        [InlineData("Invalid", Verbosity.Normal)]
        public void InitializeSetsVerbosity(string verbosity, Verbosity expected)
        {
            DefaultTestRunParameters[ParameterNames.Verbosity] = verbosity;

            FileLogger logger = new(DefaultEnvironmentProvider, DefaultFileSystem, DefaultTimeProvider);

            logger.Initialize(NullTestLoggerEvents.Instance, DefaultTestRunParameters);

            logger.VerbosityLevel.ShouldBe(expected);
        }
    }
}
