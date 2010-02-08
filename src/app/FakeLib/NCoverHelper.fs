[<AutoOpen>]
module Fake.NCoverHelper

open System 
open System.IO
open System.Text  

type NCoverParams =
 { ProjectName: string;
   ToolPath: string;
   TestRunnerExe: string;
   WorkingDir: string;}
   
/// NCover default params
let NCoverDefaults =   
  { ProjectName = String.Empty;
    ToolPath = "C:\Programs\NCover\ncover.console.exe";
    TestRunnerExe = @"C:\Programs\Nunit\bin\nunit-console.exe";
    WorkingDir = "."}
    
/// Run NCover on a group of assemblies.
/// params:
///   params - NCover params
///   assemblies - the test assemblies, which should be inspected#
///   excludeAssemblies - these assemblies are excluded 
let NCover setParams (assemblies: string seq) (excludeAssemblies: string seq) =
  let param = NCoverDefaults |> setParams
      
  let commandLineCommands =
    let args = ref (new StringBuilder())
    
    let append (s:string) = args := (!args).Append(s + " ")      
    let appendQuoted (s:string) = args := (!args).Append("\"" + s + "\" ")
    
    let fi = new FileInfo(param.TestRunnerExe)
    appendQuoted fi.FullName
    assemblies |> Seq.iter(appendQuoted)
    
    if excludeAssemblies |> Seq.isEmpty |> not then
      append "//eas"
      excludeAssemblies |> Seq.iter(appendQuoted)
    
    append "//p"
    appendQuoted param.ProjectName
    
    append "//ssp \"Registry, SymbolServer, BuildPath, ExecutingDir\""
    
    let fi = new FileInfo(param.WorkingDir)
    append "//w"

    fi.FullName.TrimEnd([| '\\' |]) |> appendQuoted 

    (!args).ToString()
 
  trace <| sprintf "NCover command\n%s %s" param.ToolPath commandLineCommands
  
  let ok =
    execProcess (fun info ->  
      info.FileName <- param.ToolPath
      if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
      info.Arguments <- commandLineCommands)
  if not ok then
    failwith <| sprintf "NCover reported errors."