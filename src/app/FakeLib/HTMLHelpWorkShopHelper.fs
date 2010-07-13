[<AutoOpen>]
module Fake.HTMLHelpWorkShopHelper

open System

/// <summary>Uses the HTML Help Workshop to compile a help project.</summary>
/// <param name="helpCompiler">The filename of the HTML Help WorkShop tool</param>
/// <param name="projectFile">the fileName of the help project</param>
/// <returns>The generated files (fileNames)</returns>
let CompileHTMLHelpProject helpCompiler projectFile =
    traceStartTask "HTMLHelpWorkshop" projectFile
    let fi = new IO.FileInfo(projectFile)
    if not fi.Exists then invalidArg "projectFile" "Projectfile doesn't exist."
    if ExecProcess (fun info ->
        info.FileName <- helpCompiler
        info.Arguments <- fi.FullName |> toParam ) System.TimeSpan.MaxValue <> 1 
    then failwith "Error in HTML Help Workshop"

    let name = fi.Name.Split('.').[0]
    traceEndTask "HTMLHelpWorkshop" projectFile
    [ fi.Directory.FullName @@ sprintf "%s.chm" name
      fi.Directory.FullName @@ sprintf "%s.hh" name]         