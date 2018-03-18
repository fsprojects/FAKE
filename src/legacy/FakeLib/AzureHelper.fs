/// Contains tasks to control the local Azure Emulator
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
module Fake.AzureHelper

open Fake.ProcessHelper
open System

/// A type for the controlling parameter
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
[<CLIMutable>]
type private AzureEmulatorParams = {
    StorageEmulatorToolPath:Lazy<string>
    CSRunToolPath:string
    TimeOut:TimeSpan
    }

/// The default parameters for Azure emulators
let private AzureEmulatorDefaults = {
    StorageEmulatorToolPath =
        lazy
            let path = msSdkBasePath @@ @"\Azure\Storage Emulator\AzureStorageEmulator.exe"
            if fileExists path then path
            else failwith (sprintf "Unable to locate Azure Storage Emulator at %s" path)
    CSRunToolPath = "\"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe\""
    TimeOut = TimeSpan.FromMinutes 5.
    }

let private (|StorageAlreadyStarted|StorageAlreadyStopped|Ok|OtherError|) = function
    | 0 -> Ok
    | -5 -> StorageAlreadyStarted
    | -6 -> StorageAlreadyStopped
    | _ -> OtherError

/// Stops the storage emulator
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
let StopStorageEmulator = (fun _ ->
    match ExecProcess (fun info ->  
        info.FileName <- AzureEmulatorDefaults.StorageEmulatorToolPath.Value
        info.Arguments <- "stop") AzureEmulatorDefaults.TimeOut with
    | Ok | StorageAlreadyStopped -> ()
    | _ -> failwithf "Azure Emulator Failure on stop Storage Emulator"
)

/// Starts the storage emulator
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
let StartStorageEmulator = (fun _ ->
    match ExecProcess (fun info ->  
        info.FileName <- AzureEmulatorDefaults.StorageEmulatorToolPath.Value
        info.Arguments <- "start") AzureEmulatorDefaults.TimeOut with
    | Ok | StorageAlreadyStarted -> ()
    | _ -> failwithf "Azure Emulator Failure on start Storage Emulator"
)

/// Stops the compute emulator
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
let StopComputeEmulator = (fun _ ->
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- AzureEmulatorDefaults.CSRunToolPath
        info.Arguments <- "/devfabric:shutdown") AzureEmulatorDefaults.TimeOut
    then
        failwithf "Azure Emulator Failure on stop Fabric Emulator"
)

/// Starts the compute emulator
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
let StartComputeEmulator = (fun _ ->
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- AzureEmulatorDefaults.CSRunToolPath
        info.Arguments <- "/devfabric:start") AzureEmulatorDefaults.TimeOut
    then
        failwithf "Azure Emulator Failure on start Fabric Emulator"
)

/// Resets the devstore (BLOB, Queues and Tables)
[<System.Obsolete("Please use one of the Fake 5 Fake.Azure.* modules")>]
let ResetDevStorage = (fun _ ->
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- AzureEmulatorDefaults.StorageEmulatorToolPath.Value
        info.Arguments <- "clear all") AzureEmulatorDefaults.TimeOut
    then
        failwithf "Azure Emulator Failure on reset Dev Storage"
)
