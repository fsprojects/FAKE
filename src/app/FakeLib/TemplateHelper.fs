[<AutoOpen>]
module Fake.TemplateHelper

/// Loads all templates (lazy - line by line!)    
let loadTemplates seq =
  seq |> Seq.map (fun fileName -> fileName,ReadFile fileName)

/// replaces a bunch of the keywords in all files (lazy - line by line!)
let replaceKeywords m seq =
  seq |> Seq.map (fun (fileName,file) -> 
     fileName, 
       file 
         |> Seq.map (fun (line:string) -> 
              m |> Seq.fold (fun (acc:string) (k:string,r:string) -> acc.Replace(k,r)) line))
  
/// saves all files (lazy - file by file!)
let saveFiles seq =
  seq |> Seq.iter (fun (fileName,file) -> WriteFile fileName (file |> Seq.toList))
  

/// Replaces the templates with the given replacements
let processTemplates replacements files =
  files
    |> loadTemplates
    |> replaceKeywords replacements
    |> saveFiles