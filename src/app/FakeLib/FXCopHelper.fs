[<AutoOpen>]
module Fake.FxCopHelper

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open Microsoft.Win32

type FxCopyErrorLevel =
| Warning = 5
| CriticalWarning = 4
| Error = 3
| CriticalError = 2
| ToolError = 1
| DontFailBuild = 0
  
type FxCopParams =
 { ApplyOutXsl:bool;
   DirectOutputToConsole: bool;
   DependencyDirectories: string seq;
   ImportFiles: string seq;
   RuleLibraries: string seq;
   Rules: string seq;
   ConsoleXslFileName: string;
   ReportFileName: string;
   OutputXslFileName: string;
   PlatformDirectory: string;
   ProjectFile: string;
   IncludeSummaryReport: bool;
   TypeList: string seq;
   SaveResultsInProjectFile: bool;
   WorkingDir: string;
   Verbose: bool;
   FailOnError: FxCopyErrorLevel;
   ToolPath:string}
 
let checkForErrors resultFile =
  // This version checks the result file with some Xml queries see
  // http://blogs.conchango.com/johnrayner/archive/2006/10/05/Getting-FxCop-to-break-the-build.aspx
  let FxCopCriticalWarnings = 0
  let getErrorValue s =
    let found,value = XMLRead_Int false resultFile String.Empty String.Empty (sprintf "string(count(//Issue[@Level='%s']))" s)
    value
    
  getErrorValue "CriticalError",
  getErrorValue "Error",
  getErrorValue "CriticalWarning",
  getErrorValue "Warning"

/// FxCop Default params  
let FxCopDefaults = 
  { ApplyOutXsl = false;
    DirectOutputToConsole = true;
    DependencyDirectories = Seq.empty;
    ImportFiles  = Seq.empty;
    RuleLibraries = Seq.empty;
    Rules = Seq.empty;
    ConsoleXslFileName = String.Empty;
    ReportFileName = Path.Combine(currentDirectory,"FXCopResults.html");
    OutputXslFileName = String.Empty;
    PlatformDirectory = String.Empty;
    ProjectFile = String.Empty;
    IncludeSummaryReport = true;
    TypeList = Seq.empty;
    SaveResultsInProjectFile = false;
    WorkingDir = currentDirectory;
    Verbose = true;
    FailOnError = FxCopyErrorLevel.DontFailBuild;
    ToolPath = Path.Combine(ProgramFilesX86,"Microsoft FxCop 1.36\"") }
        
/// Run FxCop on a group of assemblies.
let FxCop setParams (assemblies: string seq) =
  let param = FxCopDefaults |> setParams
  traceStartTask "FxCop" "" 
      
  let param = 
    if param.ApplyOutXsl && param.OutputXslFileName = String.Empty then
      {param with
        OutputXslFileName = Path.Combine(Path.Combine(param.ToolPath, "Xml"),"FxCopReport.xsl") }
    else param
      
  let commandLineCommands =
    let args = ref (new StringBuilder())
    let append predicate (s:string) = 
      if predicate then args := (!args).Append(s)
    let appendFormat (format:string) (value:string) = 
      if value <> String.Empty then
        args := (!args).AppendFormat(format, value)
        
    let appendItems format items =
      items |> Seq.iter (appendFormat format)
    
    append param.ApplyOutXsl "/aXsl "
    append param.DirectOutputToConsole "/c "
      
    appendFormat "/cXsl:\"{0}\" " param.ConsoleXslFileName      
    appendItems "/d:\"{0}\" " param.DependencyDirectories      
    appendItems "/f:\"{0}\" " assemblies      
    appendItems "/i:\"{0}\" " param.ImportFiles       
    appendFormat "/o:\"{0}\" " param.ReportFileName  
    appendFormat "/oXsl:\"{0}\" " param.OutputXslFileName  
    appendFormat "/plat:\"{0}\" " param.PlatformDirectory  
    appendFormat "/p:\"{0}\" " param.ProjectFile
 
    for item in param.RuleLibraries do      
      appendFormat "/r:\"{0}\" " (Path.Combine(Path.Combine(param.ToolPath, "Rules"), item))

    appendItems "/rid:{0} " param.Rules
    
    append param.IncludeSummaryReport "/s "
    appendFormat "/t:{0} " (separated "," param.TypeList)
    append param.SaveResultsInProjectFile "/u "  
    append param.Verbose "/v "
    
    (!args).ToString()
  
  tracefn "FxCop command\n%s %s" param.ToolPath commandLineCommands
  let ok = 
    execProcess3 (fun info ->  
      info.FileName <- param.ToolPath
      if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
      info.Arguments <- commandLineCommands)
  if param.ReportFileName <> String.Empty then 
    sendTeamCityFXCopImport param.ReportFileName

  // test if FxCop test failed
  if not ok && (param.FailOnError >= FxCopyErrorLevel.ToolError) then
    failwith "FxCop test failed."
  if param.FailOnError <> FxCopyErrorLevel.DontFailBuild && param.ReportFileName <> String.Empty then 
    let criticalErrors,errors,criticalWarnings,warnings = checkForErrors param.ReportFileName
    if criticalErrors <> 0 && param.FailOnError >= FxCopyErrorLevel.CriticalError then
      failwithf "FxCop found %d critical errors." criticalErrors
    if criticalErrors <> 0 && param.FailOnError >= FxCopyErrorLevel.Error then
      failwithf "FxCop found %d errors." errors      
    if criticalWarnings <> 0 && param.FailOnError >= FxCopyErrorLevel.CriticalWarning then
      failwithf "FxCop found %d critical warnings." criticalWarnings
    if warnings <> 0 && param.FailOnError >= FxCopyErrorLevel.Warning then
      failwithf "FxCop found %d warnings." warnings       
  
  traceEndTask "FxCop" ""