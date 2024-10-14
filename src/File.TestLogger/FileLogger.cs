using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private ActionBlock<EventArgs> _events;

        public FileLogger()
        {
            _events = new ActionBlock<EventArgs>(
                ProcessEvent,
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1,
                });
        }

        public bool Append { get; set; }

        public TextWriter? ConsoleError { get; set; }
        public TextWriter? ConsoleOut { get; set; }
        public TextWriter? FileWriter { get; set; }
        public FileInfo? LogFile { get; set; }
        public Verbosity VerbosityLevel { get; set; } = Verbosity.Normal;

        public void Initialize(TestLoggerEvents events, Dictionary<string, string?> parameters)
        {
            Initialize(events, parameters, fileWriter: null, consoleOut: null, consoleError: null);
        }

        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            Initialize(
                events,
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [ParameterNames.TestRunDirectory] = testRunDirectory
                });
        }

        internal void Initialize(TestLoggerEvents events, Dictionary<string, string?> parameters, TextWriter? fileWriter, TextWriter? consoleOut, TextWriter? consoleError)
        {
            if (parameters.TryGetValue(ParameterNames.Debug, out string? debugString) && bool.TryParse(debugString, out bool debug) && debug)
            {
                System.Diagnostics.Debugger.Launch();
            }

            SetProperties(parameters);

            FileWriter = fileWriter ?? new StreamWriter(new FileStream(LogFile!.FullName, Append ? FileMode.Append : FileMode.Create));
            ConsoleOut = consoleOut ?? Console.Out;
            ConsoleError = consoleError ?? Console.Error;

            events.DiscoveryMessage += (_, args) => _events.Post(args);
            events.TestResult += (_, args) => _events.Post(args);
            events.TestRunComplete += (_, args) => Shutdown(args);
            events.TestRunMessage += (_, args) => _events.Post(args);
        }

        internal void SetProperties(Dictionary<string, string?> parameters)
        {
            if (parameters.TryGetValue(ParameterNames.Append, out string? appendString) && bool.TryParse(appendString, out bool append))
            {
                Append = append;
            }

            if (parameters.TryGetValue(ParameterNames.Verbosity, out string? verbosityString) && Enum.TryParse(verbosityString, ignoreCase: true, out Verbosity verbosity))
            {
                VerbosityLevel = verbosity;
            }

            if (parameters.TryGetValue(ParameterNames.Path, out string? path) && !string.IsNullOrWhiteSpace(path))
            {
                LogFile = new FileInfo(Path.GetFullPath(path));
            }
            else
            {
                if (!parameters.TryGetValue(ParameterNames.TestRunDirectory, out string? testRunDirectory) || string.IsNullOrWhiteSpace(testRunDirectory))
                {
                    testRunDirectory = Environment.CurrentDirectory;
                }

                LogFile = new FileInfo(Path.Combine(testRunDirectory, "TestLog.txt"));
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
            }
        }

        private void ProcessTestResultEvent(TestResultEventArgs args)
        {
            if (FileWriter == null)
            {
                return;
            }

            switch (args.Result.Outcome)
            {
                case TestOutcome.Failed:
                    if (VerbosityLevel >= Verbosity.Minimal)
                    {
                        ProcessTestResult(args.Result, "Failed");
                    }
                    break;

                case TestOutcome.Passed:
                    if (VerbosityLevel >= Verbosity.Normal)
                    {
                        ProcessTestResult(args.Result, "Passed");
                    }
                    break;

                case TestOutcome.Skipped:
                    if (VerbosityLevel >= Verbosity.Minimal)
                    {
                        ProcessTestResult(args.Result, "Skipped");
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

                WriteTestDuration(testResult);

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
                    FileWriter.WriteLine("Error Message:");

                    FileWriter.Write(TestResultPrefix);
                    FileWriter.Write(TestMessageFormattingPrefix);
                    FileWriter.WriteLine(testResult.ErrorMessage);
                }

                if (!string.IsNullOrWhiteSpace(testResult.ErrorStackTrace))
                {
                    FileWriter.Write(TestResultPrefix);
                    FileWriter.WriteLine("Stack Trace:");

                    FileWriter.Write(TestResultPrefix);
                    FileWriter.WriteLine(testResult.ErrorStackTrace);
                }

                WriteTestMessages(testResult.Messages, TestResultMessage.StandardOutCategory, "Standard Output Messages:");
                WriteTestMessages(testResult.Messages, TestResultMessage.StandardErrorCategory, "Standard Error Messages:");
                WriteTestMessages(testResult.Messages, TestResultMessage.DebugTraceCategory, "Debug Traces Messages:");
                WriteTestMessages(testResult.Messages, TestResultMessage.AdditionalInfoCategory, "Additional Information Messages:");

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
                }
                else if (args.IsAborted)
                {
                    if (args.Error == null)
                    {
                        FileWriter.WriteLine("Test run aborted.");
                    }
                    else
                    {
                        FileWriter.WriteLine("Test run aborted with error:");
                        FileWriter.WriteLine(args.Error.ToString());
                    }
                }
                else
                {
                    bool failed = args.TestRunStatistics?.Stats?.TryGetValue(TestOutcome.Failed, out long failCount) == true && failCount > 0;

                    if (failed)
                    {
                        FileWriter.WriteLine("Test run failed.");
                    }
                    else
                    {
                        FileWriter.WriteLine("Test run successful.");
                    }
                }

                if (VerbosityLevel <= Verbosity.Minimal)
                {
                    return;
                }

                FileWriter.WriteLine();

                if (args.TestRunStatistics?.ExecutedTests != null)
                {
                    FileWriter.Write("Total tests: ");
                    FileWriter.WriteLine(args.TestRunStatistics.ExecutedTests);
                }

                if (args.TestRunStatistics?.Stats != null)
                {
                    WriteTestOutcomeStatistics(args.TestRunStatistics.Stats, TestOutcome.Passed, "     Passed");
                    WriteTestOutcomeStatistics(args.TestRunStatistics.Stats, TestOutcome.Failed, "     Failed");
                    WriteTestOutcomeStatistics(args.TestRunStatistics.Stats, TestOutcome.Skipped, "    Skipped");
                }

                FileWriter.Write(" Total time: ");
                WriteDuration(args.ElapsedTimeInRunningTests);
                FileWriter.WriteLine();
            }
            finally
            {
                FileWriter?.Dispose();
            }

            ConsoleOut?.WriteLine("Log file: {0}", LogFile?.FullName);
        }

        private void WriteDuration(TimeSpan timeSpan)
        {
            if (FileWriter == null)
            {
                return;
            }

            if (timeSpan.TotalDays >= 1)
            {
                FileWriter.Write($"{timeSpan.TotalDays:N2} Days");
            }
            else if (timeSpan.TotalHours >= 1)
            {
                FileWriter.Write($"{timeSpan.TotalHours:N2} Hours");
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                FileWriter.Write($"{timeSpan.TotalMinutes:N2} Minutes");
            }
            else
            {
                FileWriter.Write($"{timeSpan.TotalSeconds:N2} Seconds");
            }
        }

        private void WriteTestDuration(TestResult testResult)
        {
            if (FileWriter == null || (testResult.Outcome != TestOutcome.Passed && testResult.Outcome != TestOutcome.Failed) || testResult.Duration == default)
            {
                return;
            }

            FileWriter.Write(" [");

            if (testResult.Duration.Hours > 0)
            {
                FileWriter.Write(testResult.Duration.Hours);
                FileWriter.Write(" h");
            }

            if (testResult.Duration.Minutes > 0)
            {
                FileWriter.Write(testResult.Duration.Minutes);
                FileWriter.Write(" m");
            }

            if (testResult.Duration.Hours == 0)
            {
                if (testResult.Duration.Seconds > 0)
                {
                    FileWriter.Write(testResult.Duration.Seconds);
                    FileWriter.Write(" s");
                }

                if (testResult.Duration.Milliseconds > 0 && testResult.Duration.Minutes == 0 && testResult.Duration.Seconds == 0)
                {
                    FileWriter.Write(testResult.Duration.Milliseconds);
                    FileWriter.Write(" ms");
                }
            }

            FileWriter.Write("]");
        }

        private void WriteTestOutcomeStatistics(IDictionary<TestOutcome, long> stats, TestOutcome key, string label)
        {
            if (FileWriter == null)
            {
                return;
            }

            if (stats.TryGetValue(key, out long number) && number > 0)
            {
                FileWriter.Write(label);
                FileWriter.Write(": ");
                FileWriter.WriteLine(number);
            }
        }
    }
}
