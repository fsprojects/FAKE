[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.AppConfig

open System
open System.IO
open System.Configuration

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type Authorization = 
    | Off
    | On

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let mutable appSettings = ConfigurationManager.AppSettings

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let HasKey name = 
    ConfigurationManager.AppSettings.AllKeys
    |> Seq.exists (fun key -> key = name)

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Key<'T>(name : string) = 
    let value = appSettings.[name]
    Convert.ChangeType(value, typedefof<'T>) :?> 'T

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let WorkDirectory = 
    match (HasKey "WorkDirectory") with
    | false -> "."
    | true -> Key<string> "WorkDirectory"
    |> Path.GetFullPath

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let LogDirectory = 
    match (HasKey "LogDirectory") with
    | false -> WorkDirectory
    | true -> 
        let dir = Key<string> "LogDirectory"
        if dir.StartsWith("~") then dir.Replace("~", WorkDirectory)
        else dir
        |> Path.GetFullPath

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let AuthorizedKeysFile = Key<string> "AuthorizedKeysFile"

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Authorization =
    let keyName = "Authorization"
    match (HasKey keyName) with
    | false -> Off
    | true -> 
        match (Key<string> keyName).ToLower() with
        | "on" -> On
        | "off" -> Off
        | x -> failwith (sprintf "'%s' is not a valid value for Authorization" x)

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let ServerName = Key<string> "ServerName"

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Port = Key<string> "Port"
