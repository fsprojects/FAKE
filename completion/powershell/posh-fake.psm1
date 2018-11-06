using namespace System.Management.Automation
using namespace System.Management.Automation.Language


function New-CommandResult {
    param (
        [Parameter(Mandatory)]
        [string]$CompletionText,
        [string]$ToolTip = $CompletionText,
        [string]$ListItemText = $CompletionText
    )

    New-Object CompletionResult $CompletionText, $ListItemText, Command, $ToolTip
}

function New-TextResult {
    param (
        [Parameter(Mandatory)]
        [string]$CompletionText,
        [string]$ToolTip = $CompletionText,
        [string]$ListItemText = $CompletionText
    )

    New-Object CompletionResult $CompletionText, $ListItemText, Text, $ToolTip
}

function New-ParameterNameResult {
    param (
        [Parameter(Mandatory)]
        [string]$CompletionText,
        [string]$ToolTip = $CompletionText,
        [string]$ListItemText = $CompletionText,
        [string[]]$Aliases = $null
    )
    New-Object CompletionResult $CompletionText, $ListItemText, ParameterName, $ToolTip
    if ($Aliases) {
        $Aliases | ForEach-Object { New-Object CompletionResult $_, $ListItemText, ParameterName, $ToolTip }
    }
}

function Get-Targets {
    Param([string[]]$fakeResult)
    $startIndex = $fakeResult.IndexOf('The following targets are available:')
    $endIndex = $fakeResult.IndexOf('Performance:')
    if ($startIndex -ge 0 -and $endIndex -ge $startIndex) {
        for ($index = $startIndex + 1; $index -lt $endIndex; $index++) {
            $target = $fakeResult[$index].Trim()
            New-TextResult $target
        }
    }
}

function Get-BuildTargets {
    Param($wordToComplete, $commandAst, $cursorPosition)
    $elements = $commandAst.CommandElements
    for ($index = 0; $index -lt $elements.Count; $index++) {
        if ($elements[$index].Value -like '--script' -or $elements[$index].Value -like '-f') {
            $scriptName = $elements[$index + 1].Value
            $extension = [System.IO.Path]::GetExtension($scriptName)
            if ($extension -like '.fsx') {
                return Get-Targets(fake build --script $scriptName --list)
            }
            return @()
        }
    }
    Get-Targets(fake build --list)
}

function Get-RunTargets {
    param($wordToComplete, $commandAst, $cursorPosition)
    $elements = $commandAst.CommandElements
    for ($index = 0; $index -lt $elements.Count; $index++) {
        if ($elements[$index].Value -like 'run') {
            $scriptName = $elements[$index + 1].Value
            $extension = [System.IO.Path]::GetExtension($scriptName)
            if ($extension -like '.fsx') {
                return Get-Targets(fake run $scriptName --list)
            }
        }
    }
    @()
}


$fakeBuild = {
    param($wordToComplete, $prev, $commandAst, $cursorPosition)

    switch ($prev) {
        # file completion
        '--script' {return @()}
        '-f' {return return @()}
        # target completion
        '--target' { return Get-BuildTargets $wordToComplete $commandAst $cursorPosition}
        '-t' { return Get-BuildTargets $wordToComplete $commandAst $cursorPosition}
        'target' { return Get-BuildTargets $wordToComplete $commandAst $cursorPosition}
    }

    @( 
        New-CommandResult 'target' 'Run the given target.' 
        New-ParameterNameResult '--help' 'Show help.' -Aliases '-h'
        New-ParameterNameResult '--debug' 'Debug the script.' -Aliases '-d'
        New-ParameterNameResult '--nocache' 'Disable fake cache for this run.' '-n'
        New-ParameterNameResult '--partial-restore' 'Only restore the required group instead of a full restore.' -Aliases '-p'
        New-ParameterNameResult '--fsiargs' 'Arguments passed to the f# interactive.'
        New-ParameterNameResult '--script' 'The script to execute (defaults to `build.fsx`).' -Aliases '-f'
        New-ParameterNameResult '--target' "Run the given target (ignored if positional argument 'target' is given)." -Aliases '-t'
        New-ParameterNameResult '--list' 'List all available targets.'
        New-ParameterNameResult '--single-target' 'Run only the specified target.' -Aliases '-s'
        New-ParameterNameResult '--parallel' 'Run parallel with the given number of tasks.' -Aliases '-p'
        New-ParameterNameResult '--environment-variable' "Set an environment variable. Use 'key=val'." -Aliases '-e'
    )
}

$fakeRun = {
    param($wordToComplete, $prev, $commandAst, $cursorPosition)

    switch ($prev) {
        # file completion
        'run' {return return @()}
        # target completion
        '--target' { return Get-RunTargets $wordToComplete $commandAst $cursorPosition}
        '-t' { return Get-RunTargets $wordToComplete $commandAst $cursorPosition}
    }
    @( 
        New-CommandResult 'target' 'Run the given target.' 
        New-ParameterNameResult '--help' 'Show help.' -Aliases '-h'
        New-ParameterNameResult '--debug' 'Debug the script.' -Aliases '-d'
        New-ParameterNameResult '--nocache' 'Disable fake cache for this run.' '-n'
        New-ParameterNameResult '--partial-restore' 'Only restore the required group instead of a full restore.' -Aliases '-p'
        New-ParameterNameResult '--fsiargs' 'Arguments passed to the f# interactive.'
        New-ParameterNameResult '--target' "Run the given target (ignored if positional argument 'target' is given)." -Aliases '-t'
        New-ParameterNameResult '--list' 'List all available targets.'
        New-ParameterNameResult '--single-target' 'Run only the specified target.' -Aliases '-s'
        New-ParameterNameResult '--parallel' 'Run parallel with the given number of tasks.' -Aliases '-p'
        New-ParameterNameResult '--environment-variable' "Set an environment variable. Use 'key=val'." -Aliases '-e'
    )
}

$fakeBuildTarget = {
    param($wordToComplete, $prev, $commandAst, $cursorPosition)

    switch ($prev) {
        'target' {return Get-BuildTargets $wordToComplete $commandAst $cursorPosition}
    }
    @()
}

$fakeRunTarget = {
    param($wordToComplete, $prev, $commandAst, $cursorPosition)

    switch ($prev) {
        'target' {return Get-RunTargets $wordToComplete $commandAst $cursorPosition}
    }
    @()
}

$commandCompletions = @{
    fake                = @(
        New-CommandResult 'build' 'build' 
        New-CommandResult 'run' 'run' 
        New-ParameterNameResult '--help' 'Show help.' -Aliases '-h'
        New-ParameterNameResult '--version' 'Show version.'
        New-ParameterNameResult '--verbose' 'Is ignored if -s is used.
        * -v: Log verbose but only for FAKE
        * -vv: Log verbose for Paket as well' -Aliases '-v', '-vv'       
        New-ParameterNameResult '--silent' 'Be silent, use this option if you need to pipe your output into another tool or need some additional processing.' -Aliases '-s'
    )
    'fake_build'        = $fakeBuild
    'fake_run'          = $fakeRun
    'fake_build_target' = $fakeBuildTarget
    'fake_run_target'   = $fakeRunTarget
}


function Get-Completions {
    param (
        [Parameter(Mandatory, Position = 0)]
        [string]$Name,
        [Object[]]$ArgumentList
    )
    function flatten {
        Param($input)
        $input | ForEach-Object {
            if ($_ -is [array]) {
                $_ | flatten
            }
            else {
                $_
            }
        } 
    }
    $commandCompletion = $commandCompletions[$command] 

    if ($commandCompletion -is [scriptblock]) {
        $commandCompletion = Invoke-Command -ScriptBlock $commandCompletion -ArgumentList $ArgumentList
    }

    foreach ($c in $commandCompletion|flatten) {
        if ($c -is [CompletionResult]) {
            $c
        }
    }
}

Register-ArgumentCompleter -Native -CommandName fake -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    $command = 'fake'
    $prev = $null
    for ($index = 1; $index -lt $commandAst.CommandElements.Count; $index++) {
        $ce = $commandAst.CommandElements[$index]
        if ($cursorPosition -lt $ce.Extent.EndColumnNumber) {
            break
        }

        $text = $ce.Extent.Text
        $prev = $text
        $nextCommand = $command + "_$text"
        if (!$text.StartsWith('-') -and $commandCompletions.ContainsKey($nextCommand)) {
            $command = $nextCommand
        } 
    }

    $completions = Get-Completions $command -ArgumentList $wordToComplete, $prev, $commandAst, $cursorPosition
    $completions | Where-Object {($_.CompletionText -Like "$wordToComplete*") } | 
        Sort-Object -Property ListItemText
}

Export-ModuleMember -Function @()
