function Add-SolutionFolder {
    param(
       [string]$Name
    )
    $solution2 = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
    $solution2.AddSolutionFolder($Name)
}

function Get-SolutionFolder {
    param (
        [string]$Name
    )
    $solution2.Projects | ?{ $_.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder -and $_.Name -eq $Name }
}

# Adding a solution folder and a file
$sf = Add-SolutionFolder folder

# Pass the absolute path to the file
$sf.ProjectItems.AddFromFileCopy("c:\Foo.proj")

# Removing
#$sf = Get-SolutionFolder folder
#$dte.Solution.Remove($sf)