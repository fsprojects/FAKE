/// Contains a task to invoke the FxCop tool
///
/// ### Sample
///
///        Target.create "FxCop" (fun _ ->
///            Directory.ensure "./_Reports"
///            let rules = [ "-Microsoft.Design#CA1011"   // maybe sometimes
///                          "-Microsoft.Design#CA1062" ] // null checks,  In F#!
///            !! ("**/bin/Debug/*.dll")
///            |> FxCop.run { FxCop.Params.Create() with WorkingDirectory = "."
///                                                      UseGAC = true
///                                                      Verbose = false
///                                                      ReportFileName = "_Reports/FxCopReport.xml"
///                                                      Rules = rules
///                                                      FailOnError = FxCop.ErrorLevel.Warning
///                                                      IgnoreGeneratedCode = true})
///
[<RequireQualifiedAccess>]
module Fake.DotNet.FxCop

open System
open Microsoft.Win32
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

/// The FxCop error reporting level : warning, critical warning, error, critical error, tool error, don't fail build (Default: DontFailBuild)
type ErrorLevel =
    | Warning = 5
    | CriticalWarning = 4
    | Error = 3
    | CriticalError = 2
    | ToolError = 1
    | DontFailBuild = 0

/// Parameter type for the FxCop tool
[<NoComparison>]
type Params =
    { /// Apply the XSL style sheet to the output.  Default false.
      ApplyOutXsl : bool
      /// Output messages to console, including file and line number information.  Default true.
      DirectOutputToConsole : bool
      /// Locations to search for assembly dependencies.  Default empty.
      DependencyDirectories : string seq
      /// Import XML report(s) or FxCop project file(s).  Default empty.
      ImportFiles : string seq
      /// Directory containing rule assemblies or path to rule assembly. Enables all rules.  Default empty.
      RuleLibraries : string seq
      /// Namespace and CheckId strings that identify a Rule. '+' enables the rule; '-' disables the rule.  Default empty.
      Rules : string seq
      /// Rule set to be used for the analysis. It can be a file path to the rule set
      /// file or the file name of a built-in rule set. '+' enables all rules in the
      /// rule set; '-' disables all rules in the rule set; '=' sets rules to match the
      /// rule set and disables all rules that are not enabled in the rule set.
      /// Default empty.
      CustomRuleset : string
      /// Suppress analysis results against generated code.  Default false.
      IgnoreGeneratedCode : bool
      /// Apply specified XSL to console output.  Default empty.
      ConsoleXslFileName : string
      /// FxCop project or XML report output file.  Default "FXCopResults.html" in the current working directory
      ReportFileName : string
      /// Reference the specified XSL in the XML report file or "none" to generate an XML report with no XSL style sheet.
      /// Default empty.
      OutputXslFileName : string
      /// Location of platform assemblies.  Default empty.
      PlatformDirectory : string
      /// Project file to load.  Default empty.
      ProjectFile : string
      /// Display summary after analysis.  Default true.
      IncludeSummaryReport : bool
      /// Search Global Assembly Cache for missing references.  Default false.
      UseGAC : bool
      /// Analyze only these types and members.  Default empty
      Types : string seq
      /// Update the project file if there are any changes.  Default false.
      SaveResultsInProjectFile : bool
      /// Working directory for relative file paths.  Default is the current working directory
      WorkingDirectory : string
      /// Give verbose output during analysis.  Default true.
      Verbose : bool
      /// The error level that will cause a build failure.  Default ontFailBuild.
      FailOnError : ErrorLevel
      /// Path to the FxCop executable.  Default = %VSINSTALLDIR%/Team Tools/Static Analysis Tools/FxCop/FxCopCmd.exe
      /// where %VSINSTALLDIR% is a Visual Stdio 2017 installation location derived from the registry
      ToolPath : string
      /// Write output XML and project files even in the case where no violations
      /// occurred.  Default false.
      ForceOutput : bool
      /// Custom dictionary used by spelling rules.  Default empty.
      CustomDictionary : string }

    static member private vsInstallPath() =
        if Environment.isWindows then
            let instance =
                BlackFox.VsWhere.VsInstances.getWithPackage
                    "Microsoft.VisualStudio.Component.Static.Analysis.Tools" false
                |> List.tryHead
            match instance with
            | Some vs -> vs.InstallationPath
            | None -> String.Empty
        else String.Empty

    /// FxCop Default parameters, values as above
    static member Create() =
        { ApplyOutXsl = false
          DirectOutputToConsole = true
          DependencyDirectories = Seq.empty
          ImportFiles = Seq.empty
          RuleLibraries = Seq.empty
          Rules = Seq.empty
          CustomRuleset = String.Empty
          IgnoreGeneratedCode = false
          ConsoleXslFileName = String.Empty
          ReportFileName = Shell.pwd() @@ "FXCopResults.html"
          OutputXslFileName = String.Empty
          PlatformDirectory = String.Empty
          ProjectFile = String.Empty
          IncludeSummaryReport = true
          Types = Seq.empty
          UseGAC = false
          SaveResultsInProjectFile = false
          WorkingDirectory = Shell.pwd()
          Verbose = true
          FailOnError = ErrorLevel.DontFailBuild
          ToolPath =
              Params.vsInstallPath()
              @@ "Team Tools/Static Analysis Tools/FxCop/FxCopCmd.exe"
          ForceOutput = false
          CustomDictionary = String.Empty }

// default Xml reader
let internal XmlReadIntBase failOnError xmlFileName nameSpace prefix xPath =
  (Xml.read_Int failOnError xmlFileName nameSpace prefix xPath) |> snd

// Unit test mocking point
let mutable internal XmlReadInt = XmlReadIntBase

/// This checks the result file with some XML queries for errors
/// [omit]
let internal checkForErrors resultFile =
    // original version found at http://blogs.conchango.com/johnrayner/archive/2006/10/05/Getting-FxCop-to-break-the-build.aspx
    let getErrorValue s =
        XmlReadInt false resultFile String.Empty String.Empty
            (sprintf "string(count(//Issue[@Level='%s']))" s)
    getErrorValue "CriticalError", getErrorValue "Error", getErrorValue "CriticalWarning",
    getErrorValue "Warning"

let internal createProcess param args =
    CreateProcess.fromRawCommand param.ToolPath args
    |> if String.IsNullOrWhiteSpace param.WorkingDirectory then id
       else CreateProcess.withWorkingDirectory param.WorkingDirectory

let internal createArgs fxparams assemblies =
    let param =
        if fxparams.ApplyOutXsl && (String.IsNullOrWhiteSpace fxparams.OutputXslFileName) then
            { fxparams with OutputXslFileName =
                                fxparams.ToolPath @@ "Xml" @@ "FxCopReport.xsl" }
        else fxparams

    let Item a x =
        if x |> String.IsNullOrWhiteSpace then []
        else [ sprintf a x ]

    let ItemList a x =
        if x |> isNull then []
        else
            x
            |> Seq.collect (fun i -> [ sprintf a i ])
            |> Seq.toList

    let Flag predicate a =
        if predicate then [ a ]
        else []

    let rules =
        param.RuleLibraries |> Seq.map (fun item -> param.ToolPath @@ "Rules" @@ item)
    [ Flag param.ApplyOutXsl "/aXsl"
      Flag param.DirectOutputToConsole "/c"
      Flag param.ForceOutput "/fo"
      Item "/cXsl:%s" param.ConsoleXslFileName
      ItemList "/d:%s" param.DependencyDirectories
      ItemList "/f:%s" assemblies
      ItemList "/i:%s" param.ImportFiles
      Item "/o:%s" param.ReportFileName
      Item "/oXsl:%s" param.OutputXslFileName
      Item "/plat:%s" param.PlatformDirectory
      Item "/p:%s" param.ProjectFile
      Item "/ruleset:=%s" param.CustomRuleset
      ItemList "/r:%s" rules
      ItemList "/rid:%s" param.Rules
      Flag param.IgnoreGeneratedCode "/ignoregeneratedcode"
      Flag param.IncludeSummaryReport "/s"
      Item "/t:%s" (String.separated "," param.Types)
      Flag param.SaveResultsInProjectFile "/u"
      Flag param.Verbose "/v"
      Flag param.UseGAC "/gac"
      Item "/dic:%s" param.CustomDictionary ]
    |> List.concat

let internal failAsrequired param result =
    let ok = 0 = result.ExitCode
    if not ok && (param.FailOnError >= ErrorLevel.ToolError) then
        failwith "FxCop test failed."
    if param.FailOnError > ErrorLevel.ToolError
       && param.ReportFileName <> String.Empty then
        let criticalErrors, errors, criticalWarnings, warnings =
            checkForErrors param.ReportFileName
        if criticalErrors <> 0 && param.FailOnError >= ErrorLevel.CriticalError then
            failwithf "FxCop found %d critical errors." criticalErrors
        if errors <> 0 && param.FailOnError >= ErrorLevel.Error then
            failwithf "FxCop found %d errors." errors
        if criticalWarnings <> 0 && param.FailOnError >= ErrorLevel.CriticalWarning then
            failwithf "FxCop found %d critical warnings." criticalWarnings
        if warnings <> 0 && param.FailOnError >= ErrorLevel.Warning then
            failwithf "FxCop found %d warnings." warnings

let internal composeCommandLine param assemblies =
    let args = createArgs param assemblies
    createProcess param args

/// Run FxCop on a group of assemblies.
let run param (assemblies : string seq) =
    if Environment.isWindows then
        use __ = Trace.traceTask "FxCop" ""
        composeCommandLine param assemblies
        |> Proc.run
        |> failAsrequired param
        __.MarkSuccess()
    else
        raise <| NotSupportedException("FxCop is currently not supported on mono")