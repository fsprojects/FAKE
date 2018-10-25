[<RequireQualifiedAccess>]
module Fake.DotNet.FxCop

open System
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

/// The FxCop error reporting level
type FxCopErrorLevel =
    | Warning = 5
    | CriticalWarning = 4
    | Error = 3
    | CriticalError = 2
    | ToolError = 1
    | DontFailBuild = 0

/// Parameter type for the FxCop tool
[<NoComparison>]
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
      ToolPath : string
      ForceOutput : bool
      CustomDictionary : string }

    static member private vsInstallPath() =
        if Environment.isWindows then
            use hklmKey =
                Microsoft.Win32.RegistryKey.OpenBaseKey
                    (Microsoft.Win32.RegistryHive.LocalMachine,
                     Microsoft.Win32.RegistryView.Registry32)
            use key = hklmKey.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\SxS\VS7")
            key.GetValue("15.0") :?> string
        else String.Empty

    /// FxCop Default parameters
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
          TypeList = Seq.empty
          UseGACSwitch = false
          SaveResultsInProjectFile = false
          WorkingDir = Shell.pwd()
          Verbose = true
          FailOnError = FxCopErrorLevel.DontFailBuild
          ToolPath =
              FxCopParams.vsInstallPath()
              @@ "Team Tools/Static Analysis Tools/FxCop/FxCopCmd.exe"
          ForceOutput = false
          CustomDictionary = String.Empty }

/// This checks the result file with some XML queries for errors
/// [omit]
let checkForErrors resultFile =
    // original version found at http://blogs.conchango.com/johnrayner/archive/2006/10/05/Getting-FxCop-to-break-the-build.aspx
    let getErrorValue s =
        let _, value =
            Xml.read_Int false resultFile String.Empty String.Empty
                (sprintf "string(count(//Issue[@Level='%s']))" s)
        value
    getErrorValue "CriticalError", getErrorValue "Error", getErrorValue "CriticalWarning",
    getErrorValue "Warning"

/// Run FxCop on a group of assemblies.
let FxCop fxparams (assemblies : string seq) =
    use __ = Trace.traceTask "FxCop" ""

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

    let args =
        [ Flag param.ApplyOutXsl "/aXsl"
          Flag param.DirectOutputToConsole "/c"
          Flag param.ForceOutput "/fo"
          Item "/cXsl:\"%s\"" param.ConsoleXslFileName
          ItemList "/d:\"%s\"" param.DependencyDirectories
          ItemList "/f:\"%s\"" assemblies
          ItemList "/i:\"%s\"" param.ImportFiles
          Item "/o:\"%s\"" param.ReportFileName
          Item "/oXsl:\"%s\"" param.OutputXslFileName
          Item "/plat:\"%s\"" param.PlatformDirectory
          Item "/p:\"%s\"" param.ProjectFile
          Item "/ruleset:=\"%s\"" param.CustomRuleset
          ItemList "/r:\"%s\"" rules
          ItemList "/rid:%s" param.Rules
          Flag param.IgnoreGeneratedCode "/ignoregeneratedcode"
          Flag param.IncludeSummaryReport "/s"
          Item "/t:%s" (String.separated "," param.TypeList)
          Flag param.SaveResultsInProjectFile "/u"
          Flag param.Verbose "/v"
          Flag param.UseGACSwitch "/gac"
          Item "/dic:\"%s\"" param.CustomDictionary ]
        |> List.concat

    let run =
        CreateProcess.fromRawCommand param.ToolPath args
        |> if String.IsNullOrWhiteSpace param.WorkingDir then id
           else CreateProcess.withWorkingDirectory param.WorkingDir
        |> Proc.run

    let ok = 0 = run.ExitCode
    if not ok && (param.FailOnError >= FxCopErrorLevel.ToolError) then
        failwith "FxCop test failed."
    if param.FailOnError <> FxCopErrorLevel.DontFailBuild
       && param.ReportFileName <> String.Empty then
        let criticalErrors, errors, criticalWarnings, warnings =
            checkForErrors param.ReportFileName
        if criticalErrors <> 0 && param.FailOnError >= FxCopErrorLevel.CriticalError then
            failwithf "FxCop found %d critical errors." criticalErrors
        if errors <> 0 && param.FailOnError >= FxCopErrorLevel.Error then
            failwithf "FxCop found %d errors." errors
        if criticalWarnings <> 0 && param.FailOnError >= FxCopErrorLevel.CriticalWarning then
            failwithf "FxCop found %d critical warnings." criticalWarnings
        if warnings <> 0 && param.FailOnError >= FxCopErrorLevel.Warning then
            failwithf "FxCop found %d warnings." warnings
    __.MarkSuccess()
