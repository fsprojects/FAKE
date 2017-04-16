$fake_exe = $env:FAKE
$fake_dir = $env:FAKE_DIR
if ([string]::IsNullOrWhiteSpace($fake_exe)) {
    $fake_dir = "..\..\..\build"
    $fake_exe = "$fake_dir\fake.exe"
}

# when this is set, powershell throws an exception when any process writes to STDERR
$ErrorActionPreference = "Stop"
# this issue only appears if the FAKE output is redirected to a file
&"$fake_exe" "fake-powershell-redirected.fsx" 2>&1 > out-run-fake-powershell-redirected.log
