using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
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
        public FileLoggerTests(ITestOutputHelper testOutput)
            : base(testOutput)
        {
        }

        [Fact]
        public void Test1()
        {
            MockEnvironmentProvider mockEnvironmentProvider = new MockEnvironmentProvider
            {
            };

            MockTestLoggerEvents testLoggerEvents = new MockTestLoggerEvents();

            TextWriter textWriter = new StringWriter();

            FileLogger logger = new FileLogger(mockEnvironmentProvider, textWriter);

            logger.Initialize(testLoggerEvents, new Dictionary<string, string?>
            {
                ["TestRunDirectory"] = "C:\\temp"
            });

            testLoggerEvents.OnTestRunMessage("Hello World");

            testLoggerEvents.OnTestRunComplete(new TestRunCompleteEventArgs(new TestRunStatistics(), isCanceled: false, isAborted: false, error: null, attachmentSets: null, elapsedTime: TimeSpan.FromSeconds(3)));

            string? result = textWriter.ToString();
        }
    }

    internal class StringWriter : TextWriter
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly System.IO.StringWriter _stringWriter;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stringWriter.Dispose();
            }
        }

        public StringWriter()
        {
            _stringWriter = new System.IO.StringWriter(_stringBuilder);
        }

        public override Encoding Encoding { get; } = Encoding.Default;

        public override void Flush()
        {
            _stringWriter.Flush();
        }

        public override string ToString()
        {
            return _stringBuilder.ToString();
        }

        public override void Write(bool value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(char value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(char[]? buffer)
        {
            if (buffer != null)
            {
                _stringWriter.Write(buffer);
            }
        }

        public override void Write(char[]? buffer, int index, int count)
        {
            if (buffer != null)
            {
                _stringWriter.Write(buffer, index, count);
            }
        }

        public override void Write(decimal value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(double value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(int value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(long value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(object? value)
        {
            _stringWriter.Write(value);
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                _stringWriter.Write(value);
            }
        }

        public override void Write(string? format, object? arg0)
        {
            if (format != null)
            {
                _stringWriter.Write(format, arg0);
            }
        }

        public override void Write(string? format, object? arg0, object? arg1)
        {
            if (format != null)
            {
                _stringWriter.Write(format, arg0, arg1);
            }
        }

        public override void WriteLine()
        {
            _stringWriter.WriteLine();
        }

        public override void WriteLine(bool value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(char value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(char[]? buffer)
        {
            if (buffer != null)
            {
                _stringWriter.WriteLine(buffer);
            }
        }

        public override void WriteLine(char[]? buffer, int index, int count)
        {
            if (buffer != null)
            {
                _stringWriter.WriteLine(buffer, index, count);
            }
        }

        public override void WriteLine(decimal value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(object? value)
        {
            _stringWriter.WriteLine(value);
        }

        public override void WriteLine(string? value)
        {
            if (value != null)
            {
                _stringWriter.WriteLine(value);
            }
        }
    }
}
