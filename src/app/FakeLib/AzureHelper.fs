/// Contains tasks to control the local Azure Emulator
module Fake.AzureHelper

open Fake.ProcessHelper
open System

/// A type for the controlling parameter
type AzureEmulatorParams = {
    CSRunToolPath:string
    DSInitToolPath:string
    StartStorage:string
    StopStorage:string
    StartFabric:string
    StopFabric:string
    ClearFabric:string
    ForceCreate:string
    TimeOut:TimeSpan
    }

/// The default parameter of emulator
let AzureEmulatorDefaults = {
    CSRunToolPath = "\"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe\""
    DSInitToolPath = "\"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\devstore\dsinit.exe\""
    StartStorage = "/devstore:start"
    StopStorage = "/devstore:shutdown"
    StartFabric = "/devfabric:start"
    StopFabric = "/devfabric:shutdown"
    ClearFabric = "/devfabric:clear"
    ForceCreate = "/forceCreate"
    TimeOut = TimeSpan.FromMinutes 5.
    }

/// Stops the storage emulator
let StopStorageEmulator = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.CSRunToolPath
        info.Arguments <- emulatorParameter.StopStorage) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure on stop Storage Emulator"
)

/// Starts the storage emulator
let StartStorageEmulator = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.CSRunToolPath
        info.Arguments <- emulatorParameter.StartStorage) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure on start Storage Emulator"
)

/// Stops the compute emulator
let StopComputeEmulator = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.CSRunToolPath
        info.Arguments <- emulatorParameter.StopFabric) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure on stop Fabric Emulator"
)

/// Starts the compute emulator
let StartComputeEmulator = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.CSRunToolPath
        info.Arguments <- emulatorParameter.StartFabric) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure on start Fabric Emulator"
)

/// Resets the devstore (BLOB, Queues and Tables)
let ResetDevStorage = (fun _ ->
    let emulatorParameter = AzureEmulatorDefaults

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- emulatorParameter.DSInitToolPath
        info.Arguments <- emulatorParameter.ForceCreate) emulatorParameter.TimeOut
    then
        failwithf "Azure Emulator Failure on reset Dev Storage"
)
