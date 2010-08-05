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
          WorkingDir = currentDirectory}

    let CreateGemSpecification setParams =
        let p = 
          setParams {GemDefaults with Version = buildVersion}
            |> fun (p:GemParams) -> 
                if isNullOrEmpty p.RubyForgeProjectName then 
                  {p with RubyForgeProjectName = p.ProjectName}
                else 
                  p

        let sb = new StringBuilder()
        let append text  = sb.AppendLine text |> ignore
        let appends text = Printf.kprintf append text
        let appendIf p text = if isNullOrEmpty p |> not then append text

        append   "Gem::Specification.new do |spec|"
        appends  "  spec.platform    = %s" p.Platform
        appends  "  spec.name        = '%s'" p.ProjectName
        appends  "  spec.version     = '%s'" p.Version
        appendIf p.Summary <| sprintf "  spec.summary     = '%s'" p.Summary
        appendIf p.Description <| sprintf "  spec.description = '%s'" p.Description

        match p.Authors with
        | [] -> ()
        | a::[] -> appends "  spec.authors           = '%s'" a
        | _  -> sprintf "  spec.authors           = %A" p.Authors |> replace ";" "," |> append

        appendIf p.EMail <| sprintf "  spec.email             = '%s'" p.EMail
        appendIf p.Homepage <| sprintf "  spec.homepage          = '%s'" p.Homepage
        appendIf p.RubyForgeProjectName <| sprintf "  spec.rubyforge_project = '%s'" p.RubyForgeProjectName

        append "end"

        sb.ToString()