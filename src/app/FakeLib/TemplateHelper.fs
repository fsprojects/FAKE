[<AutoOpen>]
/// Contains basic templating functions. Used in other helpers.
module Fake.TemplateHelper

/// Loads all templates (lazy - line by line!)    
let loadTemplates seq = Seq.map (fun fileName -> fileName, ReadFile fileName) seq

/// Replaces a bunch of the keywords in all files (lazy - line by line!)
let replaceKeywords replacements = 
    Seq.map (fun (fileName, file) -> 
        fileName, 
        file |> Seq.map (fun (line : string) -> 
                    let mutable sb = new System.Text.StringBuilder(line)
                    for (k : string, r : string) in replacements do
                        sb <- sb.Replace(k, r)
                    sb.ToString()))

/// Saves all files (lazy - file by file!)
let saveFiles = Seq.iter (fun (fileName, file) -> WriteFile fileName (Seq.toList file))

/// Replaces the templates with the given replacements
let processTemplates replacements files = 
    files
    |> loadTemplates
    |> replaceKeywords replacements
    |> saveFiles
