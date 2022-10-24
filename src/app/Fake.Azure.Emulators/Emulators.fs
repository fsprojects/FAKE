namespace Fake.Azure

/// <summary>
/// Contains tasks to control the local Azure Emulator
/// </summary>
[<RequireQualifiedAccess>]
module Emulators =

    open System
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

    /// A type for the controlling parameter
    type private AzureEmulatorParams =
        { StorageEmulatorToolPath: Lazy<string>
          CSRunToolPath: string
          TimeOut: TimeSpan }

    /// Base path for getting tools from Microsoft SDKs
    let msSdkBasePath = Environment.ProgramFilesX86 @@ "Microsoft SDKs"

    /// The default parameters for Azure emulators
    let private AzureEmulatorDefaults =
        { StorageEmulatorToolPath =
            lazy
                let path = msSdkBasePath @@ @"\Azure\Storage Emulator\AzureStorageEmulator.exe"

                if File.exists path then
                    path
                else
                    failwith (sprintf "Unable to locate Azure Storage Emulator at %s" path)
          CSRunToolPath = "\"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe\""
          TimeOut = TimeSpan.FromMinutes 5. }

    let private (|StorageAlreadyStarted|StorageAlreadyStopped|Ok|OtherError|) =
        function
        | 0 -> Ok
        | -5 -> StorageAlreadyStarted
        | -6 -> StorageAlreadyStopped
        | _ -> OtherError

    /// Stops the storage emulator
    let stopStorageEmulator =
        (fun _ ->
            let processResult =
                CreateProcess.fromRawCommandLine AzureEmulatorDefaults.StorageEmulatorToolPath.Value "stop"
                |> CreateProcess.withTimeout AzureEmulatorDefaults.TimeOut
                |> Proc.run

            match processResult.ExitCode with
            | Ok
            | StorageAlreadyStopped -> ()
            | _ -> failwithf "Azure Emulator Failure on stop Storage Emulator")

    /// Starts the storage emulator
    let startStorageEmulator =
        (fun _ ->
            let processResult =
                CreateProcess.fromRawCommandLine AzureEmulatorDefaults.StorageEmulatorToolPath.Value "start"
                |> CreateProcess.withTimeout AzureEmulatorDefaults.TimeOut
                |> Proc.run

            match processResult.ExitCode with
            | Ok
            | StorageAlreadyStarted -> ()
            | _ -> failwithf "Azure Emulator Failure on start Storage Emulator")

    /// Stops the compute emulator
    let stopComputeEmulator =
        (fun _ ->
            let processResult =
                CreateProcess.fromRawCommandLine AzureEmulatorDefaults.CSRunToolPath "/devfabric:shutdown"
                |> CreateProcess.withTimeout AzureEmulatorDefaults.TimeOut
                |> Proc.run

            match processResult.ExitCode with
            | Ok
            | _ -> failwithf "Azure Emulator Failure on stop Fabric Emulator")

    /// Starts the compute emulator
    let startComputeEmulator =
        (fun _ ->
            let processResult =
                CreateProcess.fromRawCommandLine AzureEmulatorDefaults.CSRunToolPath "/devfabric:start"
                |> CreateProcess.withTimeout AzureEmulatorDefaults.TimeOut
                |> Proc.run

            match processResult.ExitCode with
            | Ok
            | _ -> failwithf "Azure Emulator Failure on start Fabric Emulator")

    /// Resets the devstore (BLOB, Queues and Tables)
    let resetDevStorage =
        (fun _ ->
            let processResult =
                CreateProcess.fromRawCommandLine AzureEmulatorDefaults.StorageEmulatorToolPath.Value "clear all"
                |> CreateProcess.withTimeout AzureEmulatorDefaults.TimeOut
                |> Proc.run

            match processResult.ExitCode with
            | Ok
            | _ -> failwithf "Azure Emulator Failure on reset Dev Storage")
