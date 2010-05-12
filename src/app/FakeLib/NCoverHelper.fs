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
    ToolPath = ProgramFiles @@ "NCover" @@ "ncover.console.exe";
    TestRunnerExe = ProgramFiles @@ "NUnit" @@ "bin" @@ "nunit-console.exe";
    WorkingDir = currentDirectory}
    
/// Run NCover on a group of assemblies.
/// params:
///   params - NCover params
///   assemblies - the test assemblies, which should be inspected#
///   excludeAssemblies - these assemblies are excluded 
let NCover setParams (assemblies: string seq) (excludeAssemblies: string seq) =
  let param = setParams NCoverDefaults
      
  let commandLineCommands =
    let args = ref (new StringBuilder())
    
    let append (s:string) = args := (!args).Append(s).Append(" ")      
    let appendQuoted (s:string) = args := (!args).Append("\"").Append(s).Append("\" ")
    
    param.TestRunnerExe
      |> FullName    
      |> appendQuoted 

    Seq.iter appendQuoted assemblies
    
    if excludeAssemblies |> Seq.isEmpty |> not then
        append "//eas"
        Seq.iter appendQuoted excludeAssemblies
    
    append "//p"
    appendQuoted param.ProjectName
    
    append "//ssp \"Registry, SymbolServer, BuildPath, ExecutingDir\""    
    append "//w"

    param.WorkingDir
      |> FullName
      |> trimSeparator
      |> appendQuoted 

    (!args).ToString()
 
  tracefn "NCover command\n%s %s" param.ToolPath commandLineCommands
  
  let ok =
    execProcess (fun info ->  
      info.FileName <- param.ToolPath
      if param.WorkingDir <> String.Empty then 
          info.WorkingDirectory <- param.WorkingDir
      info.Arguments <- commandLineCommands)
  if not ok then failwithf "NCover reported errors."