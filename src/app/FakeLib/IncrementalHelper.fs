/// Fake isn't really like make even though it pretends to be. The cool thing about make 
/// is that it only builds stuff when it needs to be changed. Fake builds everything every time. You
/// can add conditions on the dependencies but there are no real built in methods for change
/// tracking. 
///
/// The below demonstrates a simple framework for change tracking
/// based on MD5 signitures. A fingerprint database is saved between builds to
/// detect upstream changes and actions will only be triggered if the signiture of
/// any of the upstream dependencies change. 
///
/// Fake's inbuilt dependency trees are used but are handled automatically within 
/// the DSL. For example
///
///        open IncrementHelper
///
///        // Copy aa.txt to a.txt only of aa.txt has changed
///        FileTarget.CopyTo "a.txt" "aa.txt"
///
///        // Build build.out only if a.txt and b.txt change 
///        "build.out" <== [| "a.txt"; "b.txt" |] <| 
///            fun target sources ->
///                let text = 
///                    sources
///                    |> Seq.map File.ReadAllText
///
///                File.WriteAllText(target, System.String.Join("\n", text))
///
///
///
///        // build message.out using a text template only if the template or data changes
///        TemplateTarget.Build "message.out" [| ("@name","brad"); ("@age","24")|] "message.tpl"
///
///        // build the zip file only if any of the input files change
///        ZipTarget.Build id "build.zip" [| "build.out"; "message.out" |]
///
///        // Start a build using the incremental builder
///        FileTarget.Run "build.zip"
///
/// Note: I would class this helper as experimental. It may eat your build environment.
///
/// github:  https://github.com/bradphelan
/// twitter: @bradgonesurfing
module IncrementalHelper

open Fake
open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Runtime.Serialization
open System.Xml

let md5 file =
    let info = file |> fileInfo
    match info with
    | x when not x.Exists -> failwithf "File %s does not exist." x.FullName
    | x when x.Length = 0L -> ""
    | x ->
        use hashAlgorithm = new MD5CryptoServiceProvider()
        File.ReadAllBytes(x.FullName)
        |> hashAlgorithm.ComputeHash
        |> BitConverter.ToString


type IncrementalTarget = 
    val Id:string

    new(id:string, action:unit->unit, condition:unit->bool) = { Id = id} then
        Target id (fun () ->
            if condition() then 
                traceFAKE "Executing target %s" id
                action()
            else 
                traceFAKE "Skipping target %s" id
                )

    new(id:string) = {Id = id} then
        if TargetDict.ContainsKey id |> not  then
            tracefn "YY: Creating target %s" id
            Target id DoNothing

module HashTarget =
            
    let dbFile = "db.fake"
    let dict =
        try
            use stream = new FileStream(dbFile, FileMode.Open)
            use reader = XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas())
            let ser = DataContractSerializer(typeof<Dictionary<string*string,string>>)
            ser.ReadObject(reader, true) :?> Dictionary<string*string,string>
        with _ -> Dictionary<_, _>()

    FinalTarget "SaveHash" ( fun _ ->
        use writer = new FileStream(dbFile, FileMode.Create)
        let ser = new DataContractSerializer(dict.GetType())
        ser.WriteObject(writer, dict)
    )

    ActivateFinalTarget "SaveHash"

    /// <summary>
    /// Runs an action if the hashes change from the previous build. By
    /// convention the head of the hashes sequence is the target and the tail 
    /// contains the sources.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="hashes"></param>
    let create (action:unit->unit) (hashes:(string*(unit->string)) seq) : IncrementalTarget =

        // By convention the head is the target
        let (targetId,_) = Seq.head hashes

        // Ensure that the sources are all valid FAKE targets
        hashes 
        |> Seq.skip 1
        |> Seq.iter ( fun (sourceId,_) -> IncrementalTarget(sourceId) |> ignore)


        // wrap the action so that the hashes are updated on
        // successfull execution of the action
        let action = fun _ -> 
            action()
            hashes
            |> Seq.iter (fun (sourceId, hashF) ->
                dict.[(targetId,sourceId)] <- hashF() )

        // the condition returns true if the hashes have changed
        // since the last time 
        let condition = fun _ ->
            hashes 
            |> Seq.exists (fun (sourceId, hashF) -> 
                match dict.TryGetValue((targetId,sourceId)) with
                | true, sourceHash -> sourceHash <> hashF()
                | _ -> true
            )
                  
        // hook the FAKE target system
        let t = IncrementalTarget(targetId, action, condition)
                
        // hook the FAKE dependency system
        hashes
        |> Seq.iter (fun (sourceId, _) ->
            tracefn "%s ==> %s" sourceId targetId
            if(sourceId<>targetId)then sourceId ==> targetId |> ignore)

        t 

module FileTarget =
    let private fileTargetId name = 
        ( "file://" + (Path.GetFullPath name |> FileSystemHelper.normalizeFileName ))

    let Build  targetFile (sourceFiles:string seq) (action:string->string seq->unit) =

        let action() = 
            action targetFile sourceFiles

        let hashes =
            seq {
                // Target hash
                yield (fileTargetId targetFile, ( fun _ -> 
                    if File.Exists targetFile then 
                        md5 targetFile 
                    else "") )

                // Source hashes
                for f in sourceFiles do
                    yield fileTargetId f, fun _ -> 
                        md5 f
            }

        HashTarget.create action hashes

    let CopyTo targetFile sourceFile =
        Build targetFile [|sourceFile|] (fun _ _ -> FileHelper.CopyFile targetFile sourceFile )

    let Run targetFile =
        RunTargetOrDefault (fileTargetId targetFile)


let (<==) target sources =
    fun action ->
        FileTarget.Build target sources action

module ZipTarget =

    type Config = {
        WorkingDir : string
        Comment : string
        Level : int
        Flatten : bool
    }

    let Build cfgfn targetFile (sourceFiles:string seq) =
        let cfg = cfgfn { 
            WorkingDir = "."
            Comment=""
            Level=4
            Flatten=false
        }
        let action targetFile sources = 
            ZipHelper.CreateZip cfg.WorkingDir  targetFile  cfg.Comment  cfg.Level  cfg.Flatten  sources
        FileTarget.Build targetFile sourceFiles action

module TemplateTarget =

    let Build targetFile replacements templateFile =
        let action _ _ = 
            FileHelper.CopyFile targetFile templateFile
            TemplateHelper.processTemplates replacements [| targetFile |]
        FileTarget.Build targetFile [templateFile] action



