module Fake.DynamicsNavFile

open System
open System.Text
open System.IO
open System.Text.RegularExpressions

let ModifiedRegex = new Regex(@"\s\s\s\sModified\=Yes;(?:\r\n|\r|\n)", RegexOptions.Compiled)

let VersionRegex = new Regex(@"\n\s\s\s\sVersion List\=(?<VersionList>[^;\s]*);", RegexOptions.Compiled)

let DateRegex = new Regex(@"\r\n\s\s\s\sDate\=(?<Date>[^;]*);", RegexOptions.Compiled)

let TimeRegex = new Regex(@"\r\n\s\s\s\sTime\=(?<Time>[^;]*);", RegexOptions.Compiled)

let replaceDateTimeInString (dateTime:DateTime) text = 
    let t1 = DateRegex.Replace(text, String.Format("\r\n    Date={0};", dateTime.Date.ToString("dd.MM.yy")))
    TimeRegex.Replace(t1, String.Format("\r\n    Time={0};", dateTime.ToString("HH:mm:ss")))

let removeModifiedFlag text = ModifiedRegex.Replace(text, String.Empty)

let findVersionTagListInString text =
    if VersionRegex.IsMatch text then VersionRegex.Match(text).Groups.["VersionList"].Value else ""

let splitVersionTags (tagList:string) = tagList.ToUpper().Split Colon

let replaceInVersionTag (text:string) (versionTag:string) (newVersion:string) =
    let versionTag = versionTag.ToUpper()
    if text.ToUpper().Contains versionTag then
        splitVersionTags text
        |> Seq.map (fun tag ->
              if tag.StartsWith versionTag then
                  versionTag + newVersion
              else
                  tag)
        |> separated (Colon.ToString())
    else
        text + Colon.ToString() + versionTag + newVersion        

let replaceVersionTagList (text:string) (newTags:string) =
    VersionRegex.Replace(text, String.Format("\n    Version List={0};", newTags))

let replaceVersionTag versionTag (newVersion:string) sourceCode =
    let tagList = findVersionTagListInString sourceCode

    replaceInVersionTag tagList versionTag (newVersion.Replace(versionTag,""))
    |> replaceVersionTagList sourceCode

let getMissingRequiredTags requiredTags versionTags =
    requiredTags
        |> Seq.map (fun (rTag:string) -> rTag.ToUpper())
        |> Seq.filter (fun rTag -> versionTags |> Seq.exists (fun (tag:string) -> tag.StartsWith rTag) |> not)

let getInvalidTags invalidTags versionTags =
    invalidTags
        |> Seq.map (fun (iTag:string) -> iTag.ToUpper())
        |> Seq.filter (fun iTag -> versionTags |> Seq.exists (fun (tag:string) -> tag.StartsWith iTag))

let checkTagsInObjectString requiredTags acceptPreTagged invalidTags objectString name =
    try
        let tagList = findVersionTagListInString objectString
        let versionTags = tagList.ToUpper().Split Colon

        let isPreTagged = versionTags |> Seq.exists ((=) "PRE")

        let sb = new StringBuilder()
        for tag in getMissingRequiredTags requiredTags versionTags do
            if not (acceptPreTagged && isPreTagged) then
                sb.AppendFormat("Required VersionTag {0} not found in {1}.", tag, name) |> ignore

        for invalidTag in getInvalidTags invalidTags versionTags do
            sb.AppendFormat("Invalid VersionTag {0} found in {1}.", invalidTag, name) |> ignore

        if sb.Length > 0 then
            failwith (sb.ToString())

        objectString, tagList
    with
    | ex ->
        let s = 
            if ex.InnerException = null then ex.Message else
            sprintf "%s\r\n - %s" ex.Message ex.InnerException.Message

        failwithf "Error during VersionTag check in %s.\r\nError: %s" name s

let checkTagsInFile requiredTags acceptPreTagged invalidTags fileName =
    checkTagsInObjectString requiredTags acceptPreTagged invalidTags (ReadFileAsString fileName) fileName

let modifyNavisionFiles requiredTags acceptPreTagged invalidTags versionTag newVersion removeModified newDateTime fileNames =
    let errors = new System.Collections.Generic.List<string>()
    for fileName in fileNames do   
        try
            let objectString,tagList = checkTagsInFile requiredTags acceptPreTagged invalidTags fileName

            let text =
                replaceVersionTagList
                    objectString
                    (replaceInVersionTag versionTag newVersion tagList)

            let text = if removeModified then removeModifiedFlag text else text
            let text = if newDateTime <> DateTime.MinValue then replaceDateTimeInString newDateTime text else text

            ReplaceFile fileName text
        with
        | ex ->
            errors.Add ex.Message

            if ex.InnerException <> null then
                errors.Add("   - Inner: " + ex.InnerException.Message)
   
    if errors.Count <> 0 then
        errors 
        |> separated "\r\n"
        |> failwithf "Error occured during ModifyVersionTags:%s"

let setVersionTags requiredTags acceptPreTagged invalidTags versionTag newVersion removeModifiedFlag newDateTime fileNames =
    trace "Setting VersionTags."
    tracefn "  - Required: %A" requiredTags
    tracefn "  - Invalid:  %A" invalidTags
    if removeModifiedFlag then
        trace "Removing Modified flag."
    if DateTime.MinValue <> newDateTime then
        tracefn "Setting DateTime to %A" (newDateTime.ToString())
    modifyNavisionFiles requiredTags acceptPreTagged invalidTags versionTag newVersion removeModifiedFlag newDateTime fileNames