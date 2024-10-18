using System;
using System.Collections.Generic;

namespace File.TestLogger.UnitTests
{
    public sealed class NullEnvironmentProvider : IEnvironmentProvider
    {
        private NullEnvironmentProvider()
        {
        }

        public static IEnvironmentProvider Instance { get; } = new NullEnvironmentProvider();
        public string CommandLine => throw new NotImplementedException();

        public string CurrentDirectory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int CurrentManagedThreadId => throw new NotImplementedException();

        public int ExitCode => throw new NotImplementedException();

        public bool HasShutdownStarted => throw new NotImplementedException();

        public bool Is64BitOperatingSystem => throw new NotImplementedException();

        public bool Is64BitProcess => throw new NotImplementedException();

        public string MachineName => throw new NotImplementedException();

        public string NewLine => throw new NotImplementedException();

        public OperatingSystem OSVersion => throw new NotImplementedException();

        public int ProcessId => throw new NotImplementedException();

        public int ProcessorCount => throw new NotImplementedException();

        public string? ProcessPath => throw new NotImplementedException();
        public string StackTrace => throw new NotImplementedException();

        public string SystemDirectory => throw new NotImplementedException();

        public int SystemPageSize => throw new NotImplementedException();

        public int TickCount => throw new NotImplementedException();

        public long TickCount64 => throw new NotImplementedException();

        public string UserDomainName => throw new NotImplementedException();

        public bool UserInteractive => throw new NotImplementedException();

        public string UserName => throw new NotImplementedException();

        public Version Version => throw new NotImplementedException();

        public long WorkingSet => throw new NotImplementedException();

        public void Exit(int exitCode) => throw new NotImplementedException();

        public string? ExpandEnvironmentVariables(string name) => throw new NotImplementedException();

        public void FailFast(string? message) => throw new NotImplementedException();

        public void FailFast(string? message, Exception? exception) => throw new NotImplementedException();

        public string[] GetCommandLineArgs() => throw new NotImplementedException();

        public string? GetEnvironmentVariable(string name, EnvironmentVariableTarget target) => throw new NotImplementedException();

        public string? GetEnvironmentVariable(string name) => throw new NotImplementedException();

        public IReadOnlyDictionary<string, string> GetEnvironmentVariables() => throw new NotImplementedException();

        public IReadOnlyDictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target) => throw new NotImplementedException();

        public string GetFolderPath(Environment.SpecialFolder folder) => throw new NotImplementedException();

        public string GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption option) => throw new NotImplementedException();

        public string[] GetLogicalDrives() => throw new NotImplementedException();

        public void SetEnvironmentVariable(string name, string? value) => throw new NotImplementedException();

        public void SetEnvironmentVariable(string name, string? value, EnvironmentVariableTarget target) => throw new NotImplementedException();
    }
}
