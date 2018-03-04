module Fake.AppConfig

open System
open System.IO
open System.Configuration

type Authorization = 
    | Off
    | On

let mutable appSettings = ConfigurationManager.AppSettings

let HasKey name = 
    ConfigurationManager.AppSettings.AllKeys
    |> Seq.exists (fun key -> key = name)

let Key<'T>(name : string) = 
    let value = appSettings.[name]
    Convert.ChangeType(value, typedefof<'T>) :?> 'T

let WorkDirectory = 
    match (HasKey "WorkDirectory") with
    | false -> "."
    | true -> Key<string> "WorkDirectory"
    |> Path.GetFullPath

let LogDirectory = 
    match (HasKey "LogDirectory") with
    | false -> WorkDirectory
    | true -> 
        let dir = Key<string> "LogDirectory"
        if dir.StartsWith("~") then dir.Replace("~", WorkDirectory)
        else dir
        |> Path.GetFullPath

let AuthorizedKeysFile = Key<string> "AuthorizedKeysFile"

let Authorization =
    let keyName = "Authorization"
    match (HasKey keyName) with
    | false -> Off
    | true -> 
        match (Key<string> keyName).ToLower() with
        | "on" -> On
        | "off" -> Off
        | x -> failwith (sprintf "'%s' is not a valid value for Authorization" x)

let ServerName = Key<string> "ServerName"

let Port = Key<string> "Port"