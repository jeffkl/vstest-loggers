# VS Test File Logger
This is a logger for the Visual Studio Test Platform that writes to a file.

## Usage

```xml
<PackageReference Include="VSTest.FileLogger" Version="1.0.0" />
```

This will place the `File.TestLogger.dll` in your test project's output directory.

To enable the logger, you must do so with either command-line arguments or a RunSettings file

### Command-line Arguments

```
dotnet test --logger:File
```

```
vstest.console.exe MyUnitTests.dll --logger:File
```

### RunSettings
You can also enable the logger to your [RunSettings](https://learn.microsoft.com/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file) file:
```xml
<RunSettings>
  <RunConfiguration>
  </RunConfiguration>
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName="File" enabled="True">
        <Configuration>
          <Verbosity>Detailed</Verbosity>
        </Configuration>
      </Logger>
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```

## Logger Parameters
The File logger supports the following parameters:

| Name | Description | Default |
| --- | --- | --- |
| `Verbosity` | The verbosity level of the output.  Valid values are `Quiet`, `Minimal`, `Normal`, or `Detailed`. | `Normal` |
| `LogFileName` | The name of the log file to write to.  If the value is a relative path, it will be combined with the test run directory. | `%TestRunDirectory%\vstest.console.%UserName%_%MachineName%_%Now%.log` |
| `Append` | If `true`, the logger will append to the log file if it exists, otherwise the the log file will be overwritten. | `false` |
