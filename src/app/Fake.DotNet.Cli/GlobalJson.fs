module internal GlobalJson

open System.IO
open System.Text.Json

/// <summary>
/// Tries to get the DotNet SDK from the global.json, starts searching in the given directory.
/// Returns None if global.json is not found
/// </summary>
///
/// <param name="startDir">The directory to start search from</param>
let internal tryGetSDKVersionFromGlobalJsonDir startDir : string option =
    let globalJsonPaths rootDir =
        let rec loop (dir: DirectoryInfo) =
            seq {
                match dir.GetFiles "global.json" with
                | [| json |] -> yield json
                | _ -> ()

                if not (isNull dir.Parent) then
                    yield! loop dir.Parent
            }

        loop (DirectoryInfo rootDir)

    match Seq.tryHead (globalJsonPaths startDir) with
    | None -> None
    | Some globalJson ->
        try
            let content = File.ReadAllText globalJson.FullName

            let json =
                JsonDocument.Parse(content, JsonDocumentOptions(CommentHandling = JsonCommentHandling.Skip))

            let sdk = json.RootElement.GetProperty("sdk")

            match sdk.TryGetProperty("version") with
            | false, _ -> None
            | true, version -> Some(version.GetString())
        with exn ->
            failwithf "Could not parse `sdk.version` from global.json at '%s': %s" globalJson.FullName exn.Message



/// <summary>
/// Gets the DotNet SDK from the global.json, starts searching in the given directory.
/// </summary>
let internal getSDKVersionFromGlobalJsonDir startDir : string =
    tryGetSDKVersionFromGlobalJsonDir startDir
    |> function
        | Some version -> version
        | None -> failwithf "global.json not found"

/// <summary>
/// Tries the DotNet SDK from the global.json. This file can exist in the working
/// directory or any of the parent directories Returns None if global.json is not found
/// </summary>
let tryGetSDKVersionFromGlobalJson () : string option = tryGetSDKVersionFromGlobalJsonDir "."

/// <summary>
/// Gets the DotNet SDK from the global.json. This file can exist in the working
/// directory or any of the parent directories
/// </summary>
let getSDKVersionFromGlobalJson () : string = getSDKVersionFromGlobalJsonDir "."
