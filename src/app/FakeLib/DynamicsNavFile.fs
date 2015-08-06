/// Provides an abstraction over Dynamics NAV object files.
module Fake.DynamicsNavFile

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Threading

/// A Regex which allows to retrieve the modified flag.
let ModifiedRegex = new Regex(@"\s\s\s\sModified\=Yes;(?:\r\n|\r|\n)", RegexOptions.Compiled)

/// A Regex which allows to retrieve the version list.
let VersionRegex = new Regex(@"\n\s\s\s\sVersion List\=(?<VersionList>[^;\s]*);", RegexOptions.Compiled)

/// A Regex which allows to retrieve modified date.
let DateRegex = new Regex(@"\n\s\s\s\sDate\=(?<Date>[^;]*);", RegexOptions.Compiled)

/// A Regex which allows to retrieve the modified time.
let TimeRegex = new Regex(@"\n\s\s\s\sTime\=(?<Time>[^;]*);", RegexOptions.Compiled)

/// A Regex which allows to parse objects in a Dynamics NAV file.
let ObjectRegex = 
    new Regex(@"OBJECT (?<ObjectType>(Table|Form|Report|Dataport|Codeunit|XMLport|MenuSuite|Page|Query)) (?<ObjectId>(\d+)) (?<ObjectName>([^\r\n]+))", 
              RegexOptions.Compiled)

/// A Regex which allows to find objects in a Dynamics NAV file.
let ObjectSplitRegex = 
    new Regex(@"(?=OBJECT (?:(Table|Form|Report|Dataport|Codeunit|XMLport|MenuSuite|Page|Query)) (?:(\d+)) (?:([^\r\n]+)))", 
              RegexOptions.Compiled)

/// A type definition of a Dynamics NAV object.
type NavObject = 
    { Type : string
      Id : int
      Name : string
      Source : string }

/// A NAV culture-specific date format.
let NavObjectDateFormat = Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("yyyy", "yy")

let replaceDateTimeInStringWithFormat (dateTime : DateTime) (dateFormat : string) text = 
    let t1 = DateRegex.Replace(text, String.Format("\n    Date={0};", dateTime.Date.ToString(dateFormat)))
    TimeRegex.Replace(t1, String.Format("\n    Time={0};", dateTime.ToString("HH:mm:ss")))

/// Replaces the timestamp in a Dynamics NAV object.
let replaceDateTimeInString (dateTime : DateTime) text = 
    replaceDateTimeInStringWithFormat dateTime NavObjectDateFormat text

/// Removes the modified flag from a Dynamics NAV object.
let removeModifiedFlag text = ModifiedRegex.Replace(text, String.Empty)

/// Returns the version tag list from Dynamics NAV object.
let getVersionTagList text = 
    if VersionRegex.IsMatch text then VersionRegex.Match(text).Groups.["VersionList"].Value
    else ""

/// Splits a version tag list from Dynamics NAV object into single tags
let splitVersionTags (tagList : string) = tagList.ToUpper().Split Colon

/// Replaces a version tag in a version tag list from Dynamics NAV object
let replaceInVersionTag (versionTag : string) (newVersion : string) (tagList : string) = 
    let versionTag = versionTag.ToUpper()
    if tagList.ToUpper().Contains versionTag then 
        splitVersionTags tagList
        |> Seq.map (fun tag -> 
               if tag.StartsWith versionTag then versionTag + newVersion
               else tag)
        |> separated (Colon.ToString())
    else tagList + Colon.ToString() + versionTag + newVersion

/// Replaces a version tag list from a complete Dynamics NAV object with a new version tag list
let replaceVersionTagList (text : string) (newTags : string) = 
    VersionRegex.Replace(text, String.Format("\n    Version List={0};", newTags))

/// Replaces a version tag in a Dynamics NAV
let replaceVersionTag versionTag (newVersion : string) sourceCode = 
    let tagList = getVersionTagList sourceCode
    tagList
    |> replaceInVersionTag versionTag (newVersion.Replace(versionTag, ""))
    |> replaceVersionTagList sourceCode

/// Get all missing required tags from a Dynamics NAV version tag list
let getMissingRequiredTags requiredTags versionTags = 
    requiredTags
    |> Seq.map (fun (rTag : string) -> rTag.ToUpper())
    |> Seq.filter (fun rTag -> 
           versionTags
           |> Seq.exists (fun (tag : string) -> tag.StartsWith rTag)
           |> not)

/// Get all invalid tags from a Dynamics NAV version tag list
let getInvalidTags invalidTags versionTags = 
    invalidTags
    |> Seq.map (fun (iTag : string) -> iTag.ToUpper())
    |> Seq.filter (fun iTag -> versionTags |> Seq.exists (fun (tag : string) -> tag.StartsWith iTag))

/// Checks a Dynamics NAV object for missing required and invalid tags and raises this as errors
let checkTagsInObjectString requiredTags acceptPreTagged invalidTags objectString name = 
    try 
        let tagList = getVersionTagList objectString
        let versionTags = tagList.ToUpper().Split Colon
        let isPreTagged = versionTags |> Seq.exists ((=) "PRE")
        let sb = new StringBuilder()
        for tag in getMissingRequiredTags requiredTags versionTags do
            if not (acceptPreTagged && isPreTagged) then 
                sb.AppendFormat("Required VersionTag {0} not found in {1}.", tag, name) |> ignore
        for invalidTag in getInvalidTags invalidTags versionTags do
            sb.AppendFormat("Invalid VersionTag {0} found in {1}.", invalidTag, name) |> ignore
        if sb.Length > 0 then failwith (sb.ToString())
        objectString, tagList
    with ex -> 
        let s = 
            if ex.InnerException = null then ex.Message
            else sprintf "%s\r\n - %s" ex.Message ex.InnerException.Message
        failwithf "Error during VersionTag check in %s.\r\nError: %s" name s

/// Checks a Dynamics NAV file for missing required and invalid tags and raises this as errors
let checkTagsInFile requiredTags acceptPreTagged invalidTags fileName = 
    checkTagsInObjectString requiredTags acceptPreTagged invalidTags (ReadFileAsString fileName) fileName

/// Checks a Dynamics NAV object for missing required and invalid tags and raises this as errors.
/// It also changes the given tag, resets the modified flag and time stamp.
let modifyNavisionFiles requiredTags acceptPreTagged invalidTags versionTag newVersion removeModified newDateTime 
    fileNames = 
    let errors = new System.Collections.Generic.List<string>()
    for fileName in fileNames do
        try 
            let objectString, tagList = checkTagsInFile requiredTags acceptPreTagged invalidTags fileName
            let text = replaceVersionTag versionTag newVersion objectString
            
            let text = 
                if removeModified then removeModifiedFlag text
                else text
            
            let text = 
                if newDateTime <> DateTime.MinValue then replaceDateTimeInString newDateTime text
                else text
            
            ReplaceFile fileName text
        with ex -> 
            errors.Add ex.Message
            if ex.InnerException <> null then errors.Add("   - Inner: " + ex.InnerException.Message)
    if errors.Count <> 0 then 
        errors
        |> separated "\r\n"
        |> failwithf "Error occured during ModifyVersionTags:%s"

/// Checks a Dynamics NAV object for missing required and invalid tags and raises this as errors.
/// It also changes the given tag, resets the modified flag and time stamp.
let setVersionTags requiredTags acceptPreTagged invalidTags versionTag newVersion removeModifiedFlag newDateTime 
    fileNames = 
    trace "Setting VersionTags."
    tracefn "  - Required: %A" requiredTags
    tracefn "  - Invalid:  %A" invalidTags
    if removeModifiedFlag then trace "Removing Modified flag."
    if DateTime.MinValue <> newDateTime then tracefn "Setting DateTime to %A" (newDateTime.ToString())
    modifyNavisionFiles requiredTags acceptPreTagged invalidTags versionTag newVersion removeModifiedFlag newDateTime 
        fileNames

/// Splits an object string into multiple Dynamics NAV objects of type NavObject.
let objectsInObjectString text = 
    ObjectSplitRegex.Split(text)
    |> Seq.filter (fun x -> x.StartsWith("OBJECT "))
    |> Seq.map (fun (objectString : string) -> 
           let m = ObjectRegex.Match(objectString)
           { Type = m.Groups.["ObjectType"].Value
             Id = int m.Groups.["ObjectId"].Value
             Name = m.Groups.["ObjectName"].Value
             Source = objectString })
    |> List.ofSeq

/// Returns a standardized filename based on the given NavObject.
let fileNameFromObject (navObject : NavObject, fileEnding : string) = 
    String.Format("{0}{1}.{2}", navObject.Type.Substring(0, 3).ToUpper(), navObject.Id, fileEnding)

/// Splits the given files into individual object files in the specified destination directory.
let splitNavisionFiles fileNames destDir = 
    trace "Splitting files"
    for fileName in fileNames do
        tracefn " - File: %s" fileName
        let objectString = (ReadFileAsString fileName)
        let objects = objectsInObjectString objectString
        for x in objects do
            tracefn "  - Object: %s %i %s" x.Type x.Id x.Name
            let targetFile = fileNameFromObject (x, "txt")
            tracefn "  - Writing: %s" targetFile
            use outputFile = new StreamWriter(Path.Combine(destDir, targetFile), false)
            outputFile.Write(x.Source)
            outputFile.Close()

/// Gets the version number for the specified version tag in a Dynamics NAV version tag list
let getTagVersionInVersionTagList (versionTag : string) (tagList : string) =
    let versionTag = versionTag.ToUpper()
    if tagList.ToUpper().Contains versionTag then
        let tag = splitVersionTags tagList
                  |> Seq.find (fun x -> x.StartsWith versionTag)
        tag.Replace(versionTag, "")
    else
        ""

/// Gets the version number for the specified version tag in a Dynamics NAV object
let getTagVersionInObject (versionTag : string) sourceCode =
    let tagList = getVersionTagList sourceCode
    getTagVersionInVersionTagList versionTag tagList

/// Gets the highest version number for a specified version tag in a number of Dynamics NAV objects
let getHighestTagVersionInObjects (versionTag : string) sourceCode =
    objectsInObjectString sourceCode
    |> Seq.map (fun objectSourcecode -> getTagVersionInObject versionTag objectSourcecode.Source)
    |> Seq.filter (fun version -> not (String.IsNullOrWhiteSpace(version)))
    |> Seq.max

/// Gets the highest version number for a specified version tag in a number of Dynamics NAV objects in a set of object files
let getHighestTagVersionInFiles (versionTag : string) fileNames =
    fileNames
    |> Seq.map (fun fileName -> File.ReadAllText(fileName))
    |> Seq.map (fun sourceCode -> getTagVersionInObject versionTag sourceCode)
    |> Seq.filter (fun version -> not (String.IsNullOrWhiteSpace(version)))
    |> Seq.max