[<AutoOpen>]
/// Contains a task which can be used to run [FxCop](http://msdn.microsoft.com/en-us/library/bb429476(v=vs.80).aspx) on .NET assemblies. There is also a [tutorial](../fxcop.html) for this task available.
module Fake.FxCopHelper

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open Microsoft.Win32

/// The FxCop error reporting level
type FxCopErrorLevel = 
    | Warning = 5
    | CriticalWarning = 4
    | Error = 3
    | CriticalError = 2
    | ToolError = 1
    | DontFailBuild = 0

/// Parameter type for the FxCop tool
type FxCopParams = 
    { ApplyOutXsl : bool
      DirectOutputToConsole : bool
      DependencyDirectories : string seq
      ImportFiles : string seq
      RuleLibraries : string seq
      Rules : string seq
      CustomRuleset : string
      IgnoreGeneratedCode : bool
      ConsoleXslFileName : string
      ReportFileName : string
      OutputXslFileName : string
      PlatformDirectory : string
      ProjectFile : string
      IncludeSummaryReport : bool
      UseGACSwitch : bool
      TypeList : string seq
      SaveResultsInProjectFile : bool
      WorkingDir : string
      Verbose : bool
      FailOnError : FxCopErrorLevel
      TimeOut : TimeSpan
      ToolPath : string
      ForceOutput : bool
      CustomDictionary : string }

/// This checks the result file with some XML queries for errors
/// [omit]
let checkForErrors resultFile = 
    // original version found at http://blogs.conchango.com/johnrayner/archive/2006/10/05/Getting-FxCop-to-break-the-build.aspx
    let FxCopCriticalWarnings = 0
    
    let getErrorValue s = 
        let found, value = 
            XMLRead_Int false resultFile String.Empty String.Empty (sprintf "string(count(//Issue[@Level='%s']))" s)
        value
    getErrorValue "CriticalError", getErrorValue "Error", getErrorValue "CriticalWarning", getErrorValue "Warning"

/// FxCop Default parameters
let FxCopDefaults = 
    { ApplyOutXsl = false
      DirectOutputToConsole = true
      DependencyDirectories = Seq.empty
      ImportFiles = Seq.empty
      RuleLibraries = Seq.empty
      Rules = Seq.empty
      CustomRuleset = String.Empty
      IgnoreGeneratedCode = false
      ConsoleXslFileName = String.Empty
      ReportFileName = currentDirectory @@ "FXCopResults.html"
      OutputXslFileName = String.Empty
      PlatformDirectory = String.Empty
      ProjectFile = String.Empty
      IncludeSummaryReport = true
      TypeList = Seq.empty
      UseGACSwitch = false
      SaveResultsInProjectFile = false
      WorkingDir = currentDirectory
      Verbose = true
      FailOnError = FxCopErrorLevel.DontFailBuild
      TimeOut = TimeSpan.FromMinutes 5.
      ToolPath = ProgramFilesX86 @@ @"Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\FxCopCmd.exe"
      ForceOutput = false
      CustomDictionary = String.Empty }

/// Run FxCop on a group of assemblies.
let FxCop setParams (assemblies : string seq) = 
    let param = setParams FxCopDefaults
    traceStartTask "FxCop" ""
    let param = 
        if param.ApplyOutXsl && param.OutputXslFileName = String.Empty then 
            { param with OutputXslFileName = param.ToolPath @@ "Xml" @@ "FxCopReport.xsl" }
        else param
    
    let commandLineCommands = 
        let args = ref (new StringBuilder())
        
        let append predicate (s : string) = 
            if predicate then args := (!args).Append(s)
        
        let appendFormat (format : string) (value : string) = 
            if value <> String.Empty then args := (!args).AppendFormat(format, value)
        
        let appendItems format items = items |> Seq.iter (appendFormat format)
        append param.ApplyOutXsl "/aXsl "
        append param.DirectOutputToConsole "/c "
        append param.ForceOutput "/fo "
        appendFormat "/cXsl:\"{0}\" " param.ConsoleXslFileName
        appendItems "/d:\"{0}\" " param.DependencyDirectories
        appendItems "/f:\"{0}\" " assemblies
        appendItems "/i:\"{0}\" " param.ImportFiles
        appendFormat "/o:\"{0}\" " param.ReportFileName
        appendFormat "/oXsl:\"{0}\" " param.OutputXslFileName
        appendFormat "/plat:\"{0}\" " param.PlatformDirectory
        appendFormat "/p:\"{0}\" " param.ProjectFile
        appendFormat "/ruleset:=\"{0}\" " param.CustomRuleset
        for item in param.RuleLibraries do
            appendFormat "/r:\"{0}\" " (param.ToolPath @@ "Rules" @@ item)
        appendItems "/rid:{0} " param.Rules
        append param.IgnoreGeneratedCode "/ignoregeneratedcode "
        append param.IncludeSummaryReport "/s "
        appendFormat "/t:{0} " (separated "," param.TypeList)
        append param.SaveResultsInProjectFile "/u "
        append param.Verbose "/v "
        append param.UseGACSwitch "/gac "
        appendFormat "/dic:\"{0}\" " param.CustomDictionary
        (!args).ToString()
    
    tracefn "FxCop command\n%s %s" param.ToolPath commandLineCommands
    let ok = 
        0 = ExecProcess (fun info -> 
                info.FileName <- param.ToolPath
                if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
                info.Arguments <- commandLineCommands) param.TimeOut
    if param.ReportFileName <> String.Empty then sendTeamCityFXCopImport param.ReportFileName
    // test if FxCop test failed
    if not ok && (param.FailOnError >= FxCopErrorLevel.ToolError) then failwith "FxCop test failed."
    if param.FailOnError <> FxCopErrorLevel.DontFailBuild && param.ReportFileName <> String.Empty then 
        let criticalErrors, errors, criticalWarnings, warnings = checkForErrors param.ReportFileName
        if criticalErrors <> 0 && param.FailOnError >= FxCopErrorLevel.CriticalError then 
            failwithf "FxCop found %d critical errors." criticalErrors
        if errors <> 0 && param.FailOnError >= FxCopErrorLevel.Error then failwithf "FxCop found %d errors." errors
        if criticalWarnings <> 0 && param.FailOnError >= FxCopErrorLevel.CriticalWarning then 
            failwithf "FxCop found %d critical warnings." criticalWarnings
        if warnings <> 0 && param.FailOnError >= FxCopErrorLevel.Warning then 
            failwithf "FxCop found %d warnings." warnings
    traceEndTask "FxCop" ""
