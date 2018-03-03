/// NOTE: Maybe this should be an extra module?
/// Contains basic templating functions. Used in other helpers.
[<RequireQualifiedAccess>]
module Fake.IO.Templates

/// Loads all templates (lazy - line by line!)
let load seq = Seq.map (fun fileName -> fileName, File.read fileName) seq

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
let saveFiles = Seq.iter (fun (fileName, file) -> File.write false fileName (Seq.toList file))

/// Replaces the templates with the given replacements
let replaceInFiles replacements files =
    files
    |> load
    |> replaceKeywords replacements
    |> saveFiles
