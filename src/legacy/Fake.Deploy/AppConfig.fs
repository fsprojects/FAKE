[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.AppConfig

open System
open System.IO
open System.Configuration

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Authorization = 
    | Off
    | On

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let mutable appSettings = ConfigurationManager.AppSettings

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let HasKey name = 
    ConfigurationManager.AppSettings.AllKeys
    |> Seq.exists (fun key -> key = name)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Key<'T>(name : string) = 
    let value = appSettings.[name]
    Convert.ChangeType(value, typedefof<'T>) :?> 'T

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let WorkDirectory = 
    match (HasKey "WorkDirectory") with
    | false -> "."
    | true -> Key<string> "WorkDirectory"
    |> Path.GetFullPath

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let LogDirectory = 
    match (HasKey "LogDirectory") with
    | false -> WorkDirectory
    | true -> 
        let dir = Key<string> "LogDirectory"
        if dir.StartsWith("~") then dir.Replace("~", WorkDirectory)
        else dir
        |> Path.GetFullPath

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let AuthorizedKeysFile = Key<string> "AuthorizedKeysFile"

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Authorization =
    let keyName = "Authorization"
    match (HasKey keyName) with
    | false -> Off
    | true -> 
        match (Key<string> keyName).ToLower() with
        | "on" -> On
        | "off" -> Off
        | x -> failwith (sprintf "'%s' is not a valid value for Authorization" x)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let ServerName = Key<string> "ServerName"

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Port = Key<string> "Port"
