namespace Fake

type GemDependency = string * (string option)
type GemParams =
    { ProjectName: string;
      ToolPath: string;
      Platform: string;
      Version: string;
      Summary: string;
      Description: string;
      Authors: string list;
      EMail: string;
      Homepage: string;
      RubyForgeProjectName: string;
      Files: string list;
      Dependencies: GemDependency list;
      WorkingDir: string;}

[<AutoOpen>]
module GemHelper =
    open System
    open System.Text
   
    /// Gem default params
    let GemDefaults =   
        { ProjectName = ""
          ToolPath = @"c:\Ruby\bin\gem.bat" // FullName
          Platform = "Gem::Platform::RUBY"
          Version = "0.0.0.0"
          Summary = ""
          Description = ""
          Authors = []
          EMail = ""
          Homepage = ""
          RubyForgeProjectName = ""
          Files = []
          Dependencies = []
          WorkingDir = @".\gems" }

    let CreateGemSpecificationAsString gemParams =        
        let sb = new StringBuilder()
        let append text  = sb.AppendLine text |> ignore
        let appends text = Printf.kprintf append text
        let appendIf p text = if isNullOrEmpty p |> not then append text

        append   "Gem::Specification.new do |spec|"
        appends  "  spec.platform          = %s" gemParams.Platform
        appends  "  spec.name              = '%s'" gemParams.ProjectName
        appends  "  spec.version           = '%s'" gemParams.Version
        appendIf gemParams.Summary <| sprintf "  spec.summary           = '%s'" gemParams.Summary
        appendIf gemParams.Description <| sprintf "  spec.description       = '%s'" gemParams.Description

        match gemParams.Authors with
        | [] -> ()
        | a::[] -> appends "  spec.authors           = '%s'" a
        | _  ->           
            gemParams.Authors
              |> Seq.map quote
              |> separated ", "
              |> sprintf "  spec.authors           = [%s]"
              |> append
        
        let encapsulate =  
            let replaceFolder dir = replace dir (if dir.EndsWith directorySeparator then "." + directorySeparator else ".") 
            replaceFolder gemParams.WorkingDir
              >> replaceFolder (gemParams.WorkingDir |> FullName)
              >> replace "\\" "/" 
              >> quote

        match gemParams.Files with
        | [] -> ()
        | a::[] -> appends "  spec.files             = '%s'" (encapsulate a)
        | _  -> 
            gemParams.Files 
              |> Seq.map encapsulate 
              |> separated ", "
              |> sprintf "  spec.files             = [%s]" 
              |> append

        
        gemParams.Dependencies 
          |> Seq.iter (fun (gem,version) ->
                match version with
                | None   -> sprintf "  spec.add_dependency('%s')" gem
                | Some v -> sprintf "  spec.add_dependency('%s', '%s')" gem v
                |> append)

        appendIf gemParams.EMail <| sprintf "  spec.email             = '%s'" gemParams.EMail
        appendIf gemParams.Homepage <| sprintf "  spec.homepage          = '%s'" gemParams.Homepage
        appendIf gemParams.RubyForgeProjectName <| sprintf "  spec.rubyforge_project = '%s'" gemParams.RubyForgeProjectName

        append "end"

        sb.ToString()

    let getGemSpecFileName (gemParams:GemParams) = 
        if isNullOrEmpty gemParams.ProjectName then
            failwith "You have to specify a project name for your GemSpec."

        gemParams.WorkingDir @@ (gemParams.ProjectName + ".gemspec")
          |> FullName

    let getGemName (gemParams:GemParams) = 
        if isNullOrEmpty gemParams.ProjectName then
            failwith "You have to specify a project name for your Gem."

        if isNullOrEmpty gemParams.Version then
            failwith "You have to specify a version for your Gem."

        gemParams.ProjectName + "-" + gemParams.Version

    let getGemFileName (gemParams:GemParams) = gemParams.WorkingDir @@ (getGemName gemParams + ".gem") |> FullName

    let CreateGemSpecification setParams =
        let p = setParams (if isLocalBuild then GemDefaults else {GemDefaults with Version = buildVersion})
                
        CreateGemSpecificationAsString p
          |> WriteStringToFile false (getGemSpecFileName p)
        p
    
    let private RunGem args gemParams =
        tracefn "%s %s" gemParams.ToolPath args
        let result = 
            ExecProcess (fun info ->
                info.FileName <- gemParams.ToolPath
                info.WorkingDirectory <- gemParams.WorkingDir |> FullName
                info.Arguments <- args) System.TimeSpan.MaxValue
               
        if result <> 0 then failwithf "Error while running gem %s" args
        gemParams

    let BuildGem (gemParams:GemParams) = 
        if gemParams.Files = [] then
            failwith "You have to specify target files for your Gem."

        let args = sprintf "build \"%s\"" (getGemSpecFileName gemParams)
        RunGem args gemParams
    
    let InstallGem gemParams = 
        let args = sprintf "install \"%s\"" (getGemFileName gemParams)
        RunGem args gemParams

    let UninstallGem (gemParams:GemParams) = 
        if isNullOrEmpty gemParams.ProjectName then
            failwith "You have to specify a project name for your Gem."

        let args =
            if isNullOrEmpty gemParams.Version then
                sprintf "uninstall %s -a" gemParams.ProjectName
            else
                sprintf "uninstall %s -v %s" gemParams.ProjectName gemParams.Version

        RunGem args gemParams

    let PushGem gemParams = 
        let args = sprintf "push \"%s\"" (getGemFileName gemParams)
        RunGem args gemParams |> ignore

