/// Contains tasks to control the local Azure Emulator
module Fake.Azure.Emulators

open System
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

/// A type for the controlling parameter
[<CLIMutable>]
type private AzureEmulatorParams = {
    StorageEmulatorToolPath:Lazy<string>
    CSRunToolPath:string
    TimeOut:TimeSpan
    }

/// Base path for getting tools from Microsoft SDKs
let msSdkBasePath = Environment.ProgramFilesX86 @@ "Microsoft SDKs"

/// The default parameters for Azure emulators
let private AzureEmulatorDefaults = {
    StorageEmulatorToolPath =
        lazy
            let path = msSdkBasePath @@ @"\Azure\Storage Emulator\AzureStorageEmulator.exe"
            if File.exists path then path
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
let StopStorageEmulator = (fun _ ->
    match Process.Exec (fun info ->
        { info with
            FileName = AzureEmulatorDefaults.StorageEmulatorToolPath.Value
            Arguments = "stop" }) AzureEmulatorDefaults.TimeOut with
    | Ok | StorageAlreadyStopped -> ()
    | _ -> failwithf "Azure Emulator Failure on stop Storage Emulator"
)

/// Starts the storage emulator
let StartStorageEmulator = (fun _ ->
    match Process.Exec (fun info ->
        { info with
            FileName = AzureEmulatorDefaults.StorageEmulatorToolPath.Value
            Arguments = "start" }) AzureEmulatorDefaults.TimeOut with
    | Ok | StorageAlreadyStarted -> ()
    | _ -> failwithf "Azure Emulator Failure on start Storage Emulator"
)

/// Stops the compute emulator
let StopComputeEmulator = (fun _ ->
    if 0 <> Process.Exec (fun info ->
        { info with
            FileName = AzureEmulatorDefaults.CSRunToolPath
            Arguments = "/devfabric:shutdown" }) AzureEmulatorDefaults.TimeOut
    then
        failwithf "Azure Emulator Failure on stop Fabric Emulator"
)

/// Starts the compute emulator
let StartComputeEmulator = (fun _ ->
    if 0 <> Process.Exec (fun info ->
        { info with
            FileName = AzureEmulatorDefaults.CSRunToolPath
            Arguments = "/devfabric:start" }) AzureEmulatorDefaults.TimeOut
    then
        failwithf "Azure Emulator Failure on start Fabric Emulator"
)

/// Resets the devstore (BLOB, Queues and Tables)
let ResetDevStorage = (fun _ ->
    if 0 <> Process.Exec (fun info ->
        { info with
            FileName = AzureEmulatorDefaults.StorageEmulatorToolPath.Value
            Arguments = "clear all" }) AzureEmulatorDefaults.TimeOut
    then
        failwithf "Azure Emulator Failure on reset Dev Storage"
)
