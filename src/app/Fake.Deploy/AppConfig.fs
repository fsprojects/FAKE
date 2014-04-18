module Fake.AppConfig

open System
open System.Configuration
open System.Collections.Generic

type Authorization = 
    | Off
    | On

let HasKey name =
    ConfigurationManager.AppSettings.AllKeys 
    |> Seq.exists(fun key -> key = name)

let Key<'T>(name : string) = 
    let value = ConfigurationManager.AppSettings.[name]
    Convert.ChangeType(value, typedefof<'T>) :?> 'T

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
