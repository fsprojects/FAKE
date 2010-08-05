namespace Fake

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
      WorkingDir: string;}

[<AutoOpen>]
module GemHelper =
    open System
    open System.Text
   
    /// Gem default params
    let GemDefaults =   
        { ProjectName = ""
          ToolPath = @"c:\Ruby191\bin\gem.bat" // FullName
          Platform = "Gem::Platform::RUBY"
          Version = ""
          Summary = ""
          Description = ""
          Authors = []
          EMail = ""
          Homepage = ""
          RubyForgeProjectName = ""
          Files = []
          WorkingDir = @".\gems" }

    let CreateGemSpecificationAsString gemParams =        
        let rubyForgeName = if isNullOrEmpty gemParams.RubyForgeProjectName then gemParams.ProjectName else gemParams.RubyForgeProjectName

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
        | _  -> sprintf "  spec.authors           = %A" gemParams.Authors |> replace ";" "," |> append
        
        match gemParams.Files with
        | [] -> ()
        | a::[] -> appends "  spec.files             = '%s'" a
        | _  -> sprintf "  spec.files             = %A" gemParams.Files |> replace ";" "," |> append

        appendIf gemParams.EMail <| sprintf "  spec.email             = '%s'" gemParams.EMail
        appendIf gemParams.Homepage <| sprintf "  spec.homepage          = '%s'" gemParams.Homepage
        appends "  spec.rubyforge_project = '%s'" rubyForgeName

        append "end"

        sb.ToString()

    let getGemSpecFileName (gemParams:GemParams) = 
        if isNullOrEmpty gemParams.ProjectName then
            failwith "You have to specify a project name for your GemSpec."

        gemParams.WorkingDir @@ (gemParams.ProjectName + ".gemspec")

    let getGemFileName (gemParams:GemParams) = 
        if isNullOrEmpty gemParams.ProjectName then
            failwith "You have to specify a project name for your Gem."

        if isNullOrEmpty gemParams.Version then
            failwith "You have to specify a version for your Gem."

        gemParams.WorkingDir @@ (gemParams.ProjectName + "-" + gemParams.Version + ".gem")

    let CreateGemSpecification setParams =
        let p = setParams {GemDefaults with Version = buildVersion}
                
        CreateGemSpecificationAsString p
          |> WriteStringToFile false (getGemSpecFileName p)
        p
       
    let private RunGem command onCreatedGem gemParams =
        let fileName = if onCreatedGem then getGemFileName gemParams else getGemSpecFileName gemParams
        let fi = fileInfo fileName
        let args = sprintf "%s \"%s\"" command (FullName fileName)
        tracefn "%s %s" gemParams.ToolPath args
        let result = 
            ExecProcess (fun info ->
                info.FileName <- gemParams.ToolPath
                info.WorkingDirectory <- gemParams.WorkingDir |> FullName
                info.Arguments <- args) System.TimeSpan.MaxValue
               
        if result <> 0 then failwithf "Error while running gem %s for %s" command fileName
        gemParams

    let BuildGem gemParams = RunGem "build" false gemParams
    
    let InstallGem gemParams = RunGem "install" true gemParams

    let PushGem gemParams = RunGem "push" true gemParams |> ignore

