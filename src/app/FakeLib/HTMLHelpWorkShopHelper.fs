[<AutoOpen>]
module Fake.HTMLHelpWorkShopHelper

open System
open System.IO

/// Uses the HTML Help Workshop to compile a help project
///   param helpCompiler: The filename of the HTML Help WorkShop tool
///   param projectFile: the fileName of the help project 
///   returns: The generated files (fileNames)
let CompileHTMLHelpProject helpCompiler projectFile =
  traceStartTask "HTMLHelpWorkshop" projectFile
  let fi = new FileInfo(projectFile)
  if not fi.Exists then invalidArg "projectFile" "Projectfile doesn't exist."
  if ExecProcess (fun info ->
    info.FileName <- helpCompiler
    info.Arguments <- fi.FullName |> toParam ) <> 1 
  then failwith "Error in HTML Help Workshop"

  let name = fi.Name.Split('.').[0]
  traceEndTask "HTMLHelpWorkshop" projectFile
  [ sprintf "%s\\%s.chm" fi.Directory.FullName name;
    sprintf "%s\\%s.hh" fi.Directory.FullName name]         