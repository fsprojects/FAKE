SET TOOL_PATH=(ToolPath)

IF NOT EXIST "%TOOL_PATH%\fake.exe" (
  dotnet tool install fake-cli --tool-path ./%TOOL_PATH% --version (version)
)

"%TOOL_PATH%/fake.exe" %*