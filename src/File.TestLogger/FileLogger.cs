using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace File.TestLogger
{
    [FriendlyName("File")]
    [ExtensionUri("logger://Microsoft/TestPlatform/FileLogger/v1")]
    public class FileLogger : ITestLoggerWithParameters
    {
        private const string TestMessageFormattingPrefix = " ";
        private const string TestResultPrefix = "  ";
        private const string TestResultSuffix = " ";

        private readonly IEnvironmentProvider _environmentProvider;

        private readonly ActionBlock<EventArgs> _events;
        private readonly IFileSystem _fileSystem;
        private Dictionary<string, TestSourceResult>? _resultsByTestSource;

        public FileLogger()
            : this(SystemEnvironmentProvider.Instance, new FileSystem())
        {
        }

        internal FileLogger(IEnvironmentProvider environmentProvider, IFileSystem fileSystem, TextWriter? fileWriter = null, TextWriter? consoleOut = null, TextWriter? consoleError = null)
        {
            _environmentProvider = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            FileWriter = fileWriter;
            ConsoleOut = consoleOut ?? Console.Out;
            ConsoleError = consoleError ?? Console.Error;

            _events = new ActionBlock<EventArgs>(
                ProcessEvent,
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1,
                });
        }

        internal bool Append { get; private set; }

        internal TextWriter ConsoleError { get; private set; }

        internal TextWriter ConsoleOut { get; private set; }

        internal TextWriter? FileWriter { get; private set; }

        internal IFileInfo? LogFile { get; private set; }

        internal string? TargetFramework { get; private set; }

        internal string? TestRunDirectory { get; private set; }

        internal Verbosity VerbosityLevel { get; private set; } = Verbosity.Normal;

        public void Initialize(TestLoggerEvents events, Dictionary<string, string?> parameters)
        {
            if (parameters.TryGetValue(ParameterNames.Debug, out string? debugString) && bool.TryParse(debugString, out bool debug) && debug)
            {
                System.Diagnostics.Debugger.Launch();
            }

            SetProperties(parameters);
            
            FileWriter ??= new StreamWriter(_fileSystem.FileStream.New(LogFile!.FullName, Append ? FileMode.Append : FileMode.Create));

            events.DiscoveryMessage += (_, args) => _events.Post(args);
            events.TestResult += (_, args) => _events.Post(args);
            events.TestRunComplete += (_, args) => Shutdown(args);
            events.TestRunMessage += (_, args) => _events.Post(args);
            events.TestRunStart += (_, args) => _events.Post(args);
        }

        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            Initialize(
                events,
                new Dictionary<string, string?>(capacity: 1, StringComparer.OrdinalIgnoreCase)
                {
                    [ParameterNames.TestRunDirectory] = testRunDirectory
                });
        }

        internal void SetProperties(Dictionary<string, string?> parameters)
        {
            if (!parameters.TryGetValue(ParameterNames.TestRunDirectory, out string? testRunDirectory) || string.IsNullOrWhiteSpace(testRunDirectory))
            {
                testRunDirectory = _environmentProvider.CurrentDirectory;
            }

            TestRunDirectory = testRunDirectory;

            if (parameters.TryGetValue(ParameterNames.Append, out string? appendString) && bool.TryParse(appendString, out bool append))
            {
                Append = append;
            }

            if (parameters.TryGetValue(ParameterNames.Verbosity, out string? verbosityString) && Enum.TryParse(verbosityString, ignoreCase: true, out Verbosity verbosity))
            {
                VerbosityLevel = verbosity;
            }

            if (!parameters.TryGetValue(ParameterNames.LogFileName, out string? logFileName) || string.IsNullOrWhiteSpace(logFileName))
            {
                logFileName = string.Format(Strings.DefaultLogFileName, _environmentProvider.UserName, _environmentProvider.MachineName, DateTime.Now);
            }

            LogFile = _fileSystem.FileInfo.New(Path.Combine(TestRunDirectory, logFileName));

            if (parameters.TryGetValue(ParameterNames.TargetFramework, out string? targetFrameworkString))
            {
                TargetFramework = Framework.FromString(targetFrameworkString)?.ShortName;
            }

            LogFile.Directory?.Create();
        }

        private void ProcessEvent(EventArgs e)
        {
            switch (e)
            {
                case TestRunMessageEventArgs args:
                    ProcessTestRunMessageEvent(args);
                    break;

                case TestResultEventArgs args:
                    ProcessTestResultEvent(args);
                    break;

                case TestRunStartEventArgs args:
                    ProcessTestRunStartEvent(args);
                    break;
            }
        }

        private void ProcessTestResultEvent(TestResultEventArgs args)
        {
            if (FileWriter == null)
            {
                return;
            }

            TestSourceResult? testSourceResult = null;

            if (VerbosityLevel <= Verbosity.Minimal)
            {
                _resultsByTestSource ??= new(capacity: 4096);

                if (!_resultsByTestSource.TryGetValue(args.Result.TestCase.Source, out testSourceResult))
                {
                    testSourceResult = new TestSourceResult();

                    _resultsByTestSource.Add(args.Result.TestCase.Source, testSourceResult);
                }

                testSourceResult.Results[3]++;

                if (args.Result.StartTime < testSourceResult.StartTime)
                {
                    testSourceResult.StartTime = args.Result.StartTime;
                }

                if (args.Result.EndTime > testSourceResult.EndTime)
                {
                    testSourceResult.EndTime = args.Result.EndTime;
                }
            }

            switch (args.Result.Outcome)
            {
                case TestOutcome.Passed:
                    if (testSourceResult != null)
                    {
                        testSourceResult.Results[0]++;
                    }

                    if (VerbosityLevel >= Verbosity.Normal)
                    {
                        ProcessTestResult(args.Result, Strings.Message_TestOutcomePassedLabel);
                    }
                    break;

                case TestOutcome.Failed:
                    if (testSourceResult != null)
                    {
                        testSourceResult.Results[1]++;
                    }

                    if (VerbosityLevel >= Verbosity.Minimal)
                    {
                        ProcessTestResult(args.Result, Strings.Message_TestOutcomeFailedLabel);
                    }
                    break;

                case TestOutcome.Skipped:
                    if (testSourceResult != null)
                    {
                        testSourceResult.Results[2]++;
                    }
                    if (VerbosityLevel >= Verbosity.Minimal)
                    {
                        ProcessTestResult(args.Result, Strings.Message_TestOutcomeSkippedLabel);
                    }
                    break;

                case TestOutcome.None:
                case TestOutcome.NotFound:
                    break;
            }

            void ProcessTestResult(TestResult testResult, string outcome)
            {
                if (FileWriter == null)
                {
                    return;
                }

                FileWriter.Write(TestResultPrefix);
                FileWriter.Write(outcome);
                FileWriter.Write(TestResultSuffix);

                if (!string.IsNullOrWhiteSpace(testResult.DisplayName))
                {
                    FileWriter.Write(testResult.DisplayName);
                }
                else
                {
                    FileWriter.Write(testResult.TestCase.DisplayName);
                }
                if ((testResult.Outcome == TestOutcome.Passed || testResult.Outcome == TestOutcome.Failed) && testResult.Duration != default)
                {
                    FileWriter.Write(" [");
                    WriteDuration(testResult.Duration);
                    FileWriter.Write("]");
                }

                if (VerbosityLevel >= Verbosity.Detailed)
                {
                    WriteDetailedTestResultInformation(testResult);
                }

                FileWriter.WriteLine();
            }

            void WriteDetailedTestResultInformation(TestResult testResult)
            {
                if (!string.IsNullOrWhiteSpace(testResult.ErrorMessage))
                {
                    FileWriter.Write(TestResultPrefix);
                    FileWriter.WriteLine(Strings.Message_ErrorMesageHeader);

                    FileWriter.Write(TestResultPrefix);
                    FileWriter.Write(TestMessageFormattingPrefix);
                    FileWriter.WriteLine(testResult.ErrorMessage);
                }

                if (!string.IsNullOrWhiteSpace(testResult.ErrorStackTrace))
                {
                    FileWriter.Write(TestResultPrefix);
                    FileWriter.WriteLine(Strings.Message_StackTraceHeader);

                    FileWriter.Write(TestResultPrefix);
                    FileWriter.WriteLine(testResult.ErrorStackTrace);
                }

                WriteTestMessages(testResult.Messages, TestResultMessage.StandardOutCategory, Strings.Message_StandardOutputMessagesHeader);
                WriteTestMessages(testResult.Messages, TestResultMessage.StandardErrorCategory, Strings.Message_StandardErrorMessagesHeader);
                WriteTestMessages(testResult.Messages, TestResultMessage.DebugTraceCategory, Strings.Message_DebugTracesMessagesHeader);
                WriteTestMessages(testResult.Messages, TestResultMessage.AdditionalInfoCategory, Strings.Message_AdditionalInformationMessagesHeader);

                void WriteTestMessages(Collection<TestResultMessage> messages, string category, string banner)
                {
                    bool wroteBanner = false;

                    for (int i = 0; i < messages.Count; i++)
                    {
                        TestResultMessage message = messages[i];

                        if (!string.Equals(message.Category, category, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!wroteBanner)
                        {
                            FileWriter.Write(TestResultPrefix);
                            FileWriter.WriteLine(banner);
                            wroteBanner = true;
                        }

                        FileWriter.Write(TestResultPrefix);
                        FileWriter.Write(TestMessageFormattingPrefix);
                        FileWriter.WriteLine(message.Text);
                    }
                }
            }
        }

        private void ProcessTestRunMessageEvent(TestRunMessageEventArgs args)
        {
            if (FileWriter == null)
            {
                return;
            }

            switch (args.Level)
            {
                case TestMessageLevel.Informational:
                    if (VerbosityLevel >= Verbosity.Normal)
                    {
                        FileWriter.WriteLine(args.Message);
                    }
                    break;

                case TestMessageLevel.Warning:
                    if (VerbosityLevel >= Verbosity.Minimal)
                    {
                        FileWriter.WriteLine(args.Message);
                    }
                    break;

                case TestMessageLevel.Error:
                    FileWriter.WriteLine(args.Message);
                    break;
            }
        }

        private void ProcessTestRunStartEvent(TestRunStartEventArgs args)
        {
            if (FileWriter == null)
            {
                return;
            }

            FileWriter.WriteLine(Strings.Message_StartingTestExecution);

            List<string>? sources = args.TestRunCriteria.Sources?.ToList();

            if (sources?.Count > 0)
            {
                FileWriter.WriteLine(Strings.Message_TestSourcesToRun, sources.Count);

                if (VerbosityLevel >= Verbosity.Detailed)
                {
                    foreach (string source in sources)
                    {
                        FileWriter.WriteLine(source);
                    }
                }
            }
        }

        private void Shutdown(TestRunCompleteEventArgs args)
        {
            _events.Complete();
            _events.Completion.Wait();

            if (FileWriter == null)
            {
                return;
            }

            try
            {
                if (args.IsCanceled)
                {
                    // TODO: Does this happen?
                }
                else if (args.IsAborted)
                {
                    if (args.Error == null)
                    {
                        FileWriter.WriteLine(Strings.Message_TestRunAborted);
                    }
                    else
                    {
                        FileWriter.WriteLine(Strings.Message_TestRunAbortedWithError);
                        FileWriter.WriteLine(args.Error.ToString());
                    }
                }

                if (VerbosityLevel <= Verbosity.Minimal && _resultsByTestSource != null)
                {
                    foreach (KeyValuePair<string, TestSourceResult> item in _resultsByTestSource)
                    {
                        TestSourceResult testSourceResult = item.Value;

                        int passCount = testSourceResult.Results[0];
                        int failedCount = testSourceResult.Results[1];
                        int skippedCount = testSourceResult.Results[2];
                        int totalCount = testSourceResult.Results[3];

                        if (failedCount > 0)
                        {
                            FileWriter.Write(Strings.Message_TestSourceSummaryFailedLabel);
                        }
                        else if (passCount > 0)
                        {
                            FileWriter.Write(Strings.Message_TestSourceSummaryPassedLabel);
                        }
                        else
                        {
                            FileWriter.Write(Strings.Message_TestSourceSummarySkippedLabel);
                        }

                        TimeSpan duration = testSourceResult.StartTime == DateTimeOffset.MaxValue ? TimeSpan.Zero : testSourceResult.EndTime - testSourceResult.StartTime;

                        FileWriter.Write(" - ");
                        FileWriter.Write(Strings.Message_TestSourceSummaryFailedCountLabel, failedCount.ToString().PadLeft(5));
                        FileWriter.Write(Strings.Message_TestSourceSummaryPassedCountLabel, passCount.ToString().PadLeft(5));
                        FileWriter.Write(Strings.Message_TestSourceSummarySkippedCountLabel, skippedCount.ToString().PadLeft(5));
                        FileWriter.Write(Strings.Message_TestSourceSummaryTotalCountLabel, totalCount.ToString().PadLeft(5));
                        FileWriter.Write(Strings.Message_TestSourceSummaryDurationLabel);
                        WriteDuration(duration);
                        FileWriter.Write(" - ");
                        FileWriter.Write(Path.GetFileName(item.Key));

                        if (TargetFramework != null)
                        {
                            FileWriter.Write(" (");
                            FileWriter.Write(TargetFramework);
                            FileWriter.Write(")");
                        }

                        FileWriter.WriteLine();
                    }
                    return;
                }

                bool failed = args.TestRunStatistics?.Stats?.TryGetValue(TestOutcome.Failed, out long totalFailedCount) == true && totalFailedCount > 0;

                if (failed)
                {
                    FileWriter.WriteLine(Strings.Message_TestRunFailed);
                }
                else
                {
                    FileWriter.WriteLine(Strings.Message_TestRunSuccessful);
                }

                FileWriter.WriteLine();

                if (args.TestRunStatistics?.ExecutedTests != null)
                {
                    FileWriter.Write(Strings.Message_TotalTestsHeader);
                    FileWriter.WriteLine(args.TestRunStatistics.ExecutedTests);
                }

                if (args.TestRunStatistics?.Stats != null)
                {
                    WriteTestOutcomeStatistics(args.TestRunStatistics.Stats, TestOutcome.Passed, Strings.Message_TestRunStatisticsPassed);
                    WriteTestOutcomeStatistics(args.TestRunStatistics.Stats, TestOutcome.Failed, Strings.Message_TestRunStasticsFailed);
                    WriteTestOutcomeStatistics(args.TestRunStatistics.Stats, TestOutcome.Skipped, Strings.Message_TestRunStatisticsSkipped);
                }

                FileWriter.Write(Strings.Message_TotalTimeHeader);
                WriteTestRunDuration(args.ElapsedTimeInRunningTests);
                FileWriter.WriteLine();
            }
            finally
            {
                FileWriter?.Dispose();
            }

            ConsoleOut?.WriteLine(Strings.Message_LogFilePath, LogFile?.FullName);
        }

        private void WriteDuration(TimeSpan duration)
        {
            if (FileWriter == null)
            {
                return;
            }

            if (duration.Hours > 0)
            {
                FileWriter.Write(Strings.Message_TestDurationLabelHours, duration.Hours);
            }

            if (duration.Minutes > 0)
            {
                FileWriter.Write(Strings.Message_TestDurationLabelMinutes, duration.Minutes);
            }

            if (duration.Hours == 0)
            {
                if (duration.Seconds > 0)
                {
                    FileWriter.Write(Strings.Message_TestDurationLabelSeconds, duration.Seconds);
                }

                if (duration.Milliseconds > 0 && duration.Minutes == 0 && duration.Seconds == 0)
                {
                    FileWriter.Write(Strings.Message_TestDurationLabelMilliseconds, duration.Milliseconds);
                }
            }
        }

        private void WriteTestOutcomeStatistics(IDictionary<TestOutcome, long> stats, TestOutcome key, string label)
        {
            if (FileWriter == null)
            {
                return;
            }

            if (stats.TryGetValue(key, out long number) && number > 0)
            {
                FileWriter.WriteLine(Strings.Message_TestOutcome, label, number);
            }
        }

        private void WriteTestRunDuration(TimeSpan timeSpan)
        {
            if (FileWriter == null)
            {
                return;
            }

            if (timeSpan.TotalDays >= 1)
            {
                FileWriter.Write(Strings.Message_DurationLabelDays, timeSpan.TotalDays);
            }
            else if (timeSpan.TotalHours >= 1)
            {
                FileWriter.Write(Strings.Message_DurationLabelHours, timeSpan.TotalHours);
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                FileWriter.Write(Strings.Message_DurationLabelMinutes, timeSpan.TotalMinutes);
            }
            else
            {
                FileWriter.Write(Strings.Message_DurationLabelSeconds, timeSpan.TotalSeconds);
            }
        }

        private class TestSourceResult
        {
            public DateTimeOffset EndTime { get; set; } = DateTimeOffset.MinValue;
            public int[] Results { get; } = new int[4];

            public DateTimeOffset StartTime { get; set; } = DateTimeOffset.MaxValue;
        }
    }
}
