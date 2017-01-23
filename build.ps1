$ErrorActionPreference  = "Stop"

#MOST COMMON VARIABLES TO CHANGE
$buildFile = "build.fsx"
$toolsFolder = "Tools"
#END  

#UNCOMMON VARIABLES TO CHANGE
$basePath = Split-Path -Parent -Path $PSCommandPath
$fakePath = "FAKE\tools"
$buildFilePath = "$basePath\build.fsx"
$nugetApplicationPath = "$toolsFolder\nuget"
$fakeApplicationPath = "$toolsFolder\$fakePath"
$textColor = "Yellow"
#END

#FUNCTIONS
function ExceptionFormatter([System.Management.Automation.ErrorRecord] $exception, [string] $message)
{
    Write-Host 
    Write-Host  `t $message -ForegroundColor Red
    Write-Host 
    Write-Host  `t $exception.Exception.GetType() -ForegroundColor Magenta
    Write-Host  `t $exception.Exception.Message -ForegroundColor Magenta
    Write-Host 
}

function Write-Caution([string] $message)
{
    Write-Host $message -ForegroundColor Yellow
    Write-Host
}
function Write-Success([string] $message)
{
    Write-Host $message -ForegroundColor Green
    Write-Host    
}
#END

Clear-Host

try
{
    Write-Caution "Attempting to find the build script, $buildFile, at Path:" 

    Write-Success "`t $basePath\$buildFile"

    if (!(Test-Path "$basePath\$buildFile"))
    {
        Write-Host "`t $buildFile NOT FOUND" -ForegroundColor Red
        Write-Host 
        exit
    }
    else
    {
    Write-Success "`t $buildFile FOUND"
    }

}
catch
{
    $message = "Exception caught while finding build script"
    ExceptionFormatter $($error[0]) $message 
    exit
}


try
{
    Write-Caution "Attempting to find Nuget.exe, at Path:" 

    Write-Success "`t $basePath\$nugetApplicationPath"

    if (!(Test-Path "$basePath\$nugetApplicationPath\Nuget.exe"))
    {
        Write-Caution "`t Nuget NOT FOUND"

        Write-Caution "`t Downloading http://nuget.org/nuget.exe" 

        Invoke-WebRequest http://nuget.org/nuget.exe -OutFile "$basePath\$nugetApplicationPath\nuget.exe"

        Write-Success "`t Download a SUCCESS" 
    }
    else
    {
    Write-Success "`t Nuget FOUND" 
    }

}
catch
{
    $message = "Exception caught while installing Nuget"
    ExceptionFormatter $($error[0]) $message 
    exit
}


try
{
    Write-Caution "Attempting to find FAKE, at Path:" 

    Write-Success "`t $basePath\$fakeApplicationPath" 

    if (!(Test-Path $fakeApplicationExecutablePath) )
    {

        Write-Caution "`t FAKE NOT FOUND"

        New-Item -ItemType directory -Path "$basePath\$toolsFolder" -Force

        Write-Success "`t Executing" 

        Write-Success "`t`t $basePath\$nugetApplicationPath\nuget.exe install FAKE -OutputDirectory  $basePath\$toolsFolder -ExcludeVersion" 

        $output = Invoke-Expression -Command "$basePath\$nugetApplicationPath\nuget.exe install FAKE -OutputDirectory  $basePath\$toolsFolder -ExcludeVersion" -ErrorAction Stop
        Write-Host `t`t $output
    }
    else
    {
    Write-Success "`t FAKE FOUND" 
    }

}
catch [System.Management.Automation.RemoteException]
{
    $message = "Please close the any application, processes, folders, etc that point to $basePath or any its children and try running this script again"
    ExceptionFormatter $($error[0]) $message 
    exit
}

catch
{
    $message = "Exception caught while installing FAKE"
    ExceptionFormatter $($error[0]) $message 
    exit
}

# turn ErrorActionPreference back to Continue on so we can all the interesting goodies from FAKE.exe and the build script
$ErrorActionPreference  = "Continue" 

try
{
    Write-Caution "Attempting to FAKE our way to success, with command:"
    Write-Host
    Set-Location $basePath
    Write-Success "$basePath\$fakeApplicationPath\Fake.exe $buildFile"
    Write-Host
    Invoke-Expression -Command "$basePath\$fakeApplicationPath\Fake.exe $buildFile" -ErrorAction Stop
    Write-Host `t $output 

}
catch
{
    $message = "Exceptions caught while FAKEing our way to success: FAILURE"
    ExceptionFormatter $($error[0]) $message 
    exit
}

Read-Host 'Press Enter to continue...' | Out-Null
