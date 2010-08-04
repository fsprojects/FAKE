namespace Fake

type GemParams =
    { ProjectName: string;
      ToolPath: string;
      Platform: string;
      Version: string;
      Summary: string;
      Description: string;
      WorkingDir: string;}

[<AutoOpen>]
module GemHelper =
    open System
    open System.Text
   
    /// Gem default params
    let GemDefaults =   
        { ProjectName = String.Empty;
          ToolPath = "gem.bat";
          Platform = "Gem::Platform::RUBY";
          Version = String.Empty;
          Summary = String.Empty;
          Description = String.Empty;
          WorkingDir = currentDirectory}

    let CreateGemSpecification setParams =
        let p = setParams {GemDefaults with Version = buildVersion}

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
        append "end"

        sb.ToString()