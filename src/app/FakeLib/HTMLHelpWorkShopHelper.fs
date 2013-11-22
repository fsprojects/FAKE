[<AutoOpen>]
/// Contains a task which allows to use [HTML Help Workshop](http://msdn.microsoft.com/en-us/library/windows/desktop/ms670169(v=vs.85).aspx) in order to compile a help project.
module Fake.HTMLHelpWorkShopHelper

open System

/// Uses the HTML Help Workshop to compile a help project and returns the generated file names of the generated files.
/// ## Parameters
///
///  - `helpCompiler` - The filename of the HTML Help WorkShop tool.
///  - `projectFile` - The fileName of the help project.
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