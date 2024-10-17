using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace File.TestLogger.UnitTests
{
    internal class TestOutputHelperTextWriter : TextWriter
    {
        private readonly ITestOutputHelper _testOutputHelper;

        private StringBuilder _stringBuilder = new StringBuilder(capacity: 1024);

        public TestOutputHelperTextWriter(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public override Encoding Encoding { get; } = Encoding.Default;

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                return;
            }

            for (int i = 0; i < count && index + i < buffer.Length; i++)
            {
                _stringBuilder.Append(buffer[index + i]);
            }
        }

        public override void Write(char value)
        {
            _stringBuilder.Append(value);
        }

        public override void Write(string? value)
        {
            _stringBuilder.Append(value);
        }

        public override void WriteLine()
        {
            if (_stringBuilder.Length <= 0)
            {
                _testOutputHelper.WriteLine(string.Empty);
                return;
            }

            _testOutputHelper.WriteLine(_stringBuilder.ToString());
            _stringBuilder.Clear();
        }

        public override void WriteLine(string? value)
        {
            if (value != null)
            {
                _testOutputHelper.WriteLine(value);
            }
        }
    }
}
