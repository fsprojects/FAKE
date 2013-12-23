module AzureHelper

open Fake.ProcessHelper
open System

type AzureEmulatorParams = {
    ToolPath:string
    StartStorage:string
    StopStorage:string
    StartFabric:string
    StopFabric:string
    RemoveAll:string
    TimeOut:TimeSpan
    }

let AzureEmulatorDefaults = {
    ToolPath = "\"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe\""
    StartStorage = "/devstore:start"
    StopStorage = "/devstore:shutdown"
    StartFabric = "/devfabric:start"
    StopFabric = "/devfabric:shutdown"
    RemoveAll = "/removeAll"
    TimeOut = TimeSpan.FromMinutes 5.
    }

let StopEmulator = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.ToolPath
        info.Arguments <- emulatorParameter.StopStorage) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure"
)

let StartEmulator = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.ToolPath
        info.Arguments <- emulatorParameter.StartStorage) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure"
)

