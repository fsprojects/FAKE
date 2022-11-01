namespace Fake.IO

// NOTE: Maybe this should be an extra module?
/// <summary>
/// Contains basic templating functions. Used in other helpers.
/// </summary>
[<RequireQualifiedAccess>]
module Templates =

    /// <summary>
    /// Loads all templates (lazy - line by line!)
    /// </summary>
    ///
    /// <param name="seq">The files to load</param>
    let load seq =
        Seq.map (fun fileName -> fileName, File.read fileName) seq

    /// <summary>
    /// Replaces a bunch of the keywords in all files (lazy - line by line!)
    /// </summary>
    ///
    /// <param name="replacements">The replacement map</param>
    let replaceKeywords replacements =
        Seq.map (fun (fileName, file) ->
            fileName,
            file
            |> Seq.map (fun (line: string) ->
                let mutable sb = System.Text.StringBuilder(line)

                for k: string, r: string in replacements do
                    sb <- sb.Replace(k, r)

                sb.ToString()))

    /// <summary>
    /// Saves all files (lazy - file by file!)
    /// </summary>
    ///
    /// <param name="seq">The sequence of file names and lines to save</param>
    let saveFiles =
        Seq.iter (fun (fileName, file) -> File.write false fileName (Seq.toList file))

    /// <summary>
    /// Replaces the templates with the given replacements
    /// </summary>
    ///
    /// <param name="replacements">The replacement map</param>
    /// <param name="files">The files to replace text in</param>
    let replaceInFiles replacements files =
        files |> load |> replaceKeywords replacements |> saveFiles
