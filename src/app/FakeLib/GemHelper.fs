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
      WorkingDir: string;}

[<AutoOpen>]
module GemHelper =
    open System
    open System.Text
   
    /// Gem default params
    let GemDefaults =   
        { ProjectName = ""
          ToolPath = "gem.bat"
          Platform = "Gem::Platform::RUBY"
          Version = ""
          Summary = ""
          Description = ""
          Authors = []
          EMail = ""
          Homepage = ""
          RubyForgeProjectName = ""
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

        appendIf gemParams.EMail <| sprintf "  spec.email             = '%s'" gemParams.EMail
        appendIf gemParams.Homepage <| sprintf "  spec.homepage          = '%s'" gemParams.Homepage
        appends "  spec.rubyforge_project = '%s'" rubyForgeName

        append "end"

        sb.ToString()

    let CreateGemSpecification setParams =
        let p = setParams {GemDefaults with Version = buildVersion}
        if isNullOrEmpty p.ProjectName then
            failwith "You have to specify a project name for your GemSpec"

        CreateGemSpecificationAsString p
          |> WriteStringToFile false (p.WorkingDir @@ (p.ProjectName + ".gemspec"))