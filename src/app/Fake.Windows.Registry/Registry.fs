/// Contains functions which allow to read and write information from/to the registry.
///
/// ## Sample
/// 
/// #### Create a subkey
///
///     let subkey = "Company/MyApp"
///     Registry.createRegistrySubKey Registry.HKEYCurrentUser subkey
///
/// #### Write a key-value pair to a subkey
///
///     Registry.setRegistryValue Registry.HKEYCurrentUser subkey "AppType" "Premium"
///     Registry.setRegistryValue Registry.HKEYCurrentUser subkey "Version" "1.0.4"
///
/// #### Get a list of key-value names in a subkey
///
///     let values = Registry.getRegistryValueNames Registry.HKEYCurrentUser subkey
///     values |> Array.iter (Trace.trace << (sprintf "Found value name: %s!"))
///
/// #### Read the value of a key-value pair
///
///     let AppType = Registry.getRegistryValue Registry.HKEYCurrentUser subkey values.[0]
///     Trace.trace (sprintf "You are running the %s version" AppType)
///
/// #### Check if a value exists within a subkey
///
///     let exists b = if b then Trace.trace "It exists!" else Trace.trace "It doesn't exist!"
///     exists <| Registry.valueExistsForKey Registry.HKEYCurrentUser subkey "DateCreated"
///     exists <| Registry.valueExistsForKey Registry.HKEYCurrentUser subkey "Version"
///
/// #### Delete a key-value pair from a subkey
///
///     Registry.deleteRegistryValue Registry.HKEYCurrentUser subkey "AppType"
///
/// #### Delete a subkey
///
///     Registry.deleteRegistrySubKey Registry.HKEYCurrentUser subkey

[<RequireQualifiedAccess>]
module Fake.Windows.Registry

open Microsoft.Win32

/// Registry base keys.
type RegistryBaseKey =
    | HKEYLocalMachine
    | HKEYClassesRoot
    | HKEYUsers
    | HKEYCurrentUser
    | HKEYCurrentConfig
    | HKEYPerformanceData

(*
Lower level Registry Queries
(Should these be private?)
*)
/// Maps the RegistryBaseKey to a RegistryKey
/// [omit]
let getKey name =
    match name with
    | HKEYLocalMachine -> Registry.LocalMachine
    | HKEYClassesRoot -> Registry.ClassesRoot
    | HKEYUsers -> Registry.Users
    | HKEYCurrentUser -> Registry.CurrentUser
    | HKEYCurrentConfig -> Registry.CurrentConfig
    | HKEYPerformanceData -> Registry.PerformanceData

/// Maps the RegistryBaseKey to a RegistryKey for a 64bit System
/// [omit]
let get64BitKey name =
    match name with
    | HKEYLocalMachine -> RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
    | HKEYClassesRoot -> RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64)
    | HKEYUsers -> RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64)
    | HKEYCurrentUser -> RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
    | HKEYCurrentConfig -> RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry64)
    | HKEYPerformanceData -> RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Registry64)

/// Maps the RegistryBaseKey to a RegistryKey for a 32bit System
/// [omit]
let get32BitKey name =
    match name with
    | HKEYLocalMachine -> RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
    | HKEYClassesRoot -> RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32)
    | HKEYUsers -> RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry32)
    | HKEYCurrentUser -> RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
    | HKEYCurrentConfig -> RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry32)
    | HKEYPerformanceData -> RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Registry32)

/// Gets a 64-bit registry key
let getRegistryKey64 baseKey subKey (writePermission : bool) =
    (get64BitKey baseKey).OpenSubKey(subKey, writePermission)

/// Gets a registry key and falls back to 32 bit if the 64bit key is not there
let getRegistryKey baseKey subKey (writePermission : bool) =
    let x64BitKey = (getKey baseKey).OpenSubKey(subKey, writePermission)
    if (isNull >> not) x64BitKey then x64BitKey else
    (get32BitKey baseKey).OpenSubKey(subKey, writePermission)  // fall back to 32 bit

(*
Registry Value Commands
*)
/// Gets a registry value as string
let getRegistryValue baseKey subKey name =
    use key = getRegistryKey baseKey subKey false
    if isNull key then
        failwithf "Registry subkey %s could not be found for key %A" subKey baseKey
    let value = key.GetValue name
    if isNull value then
        failwithf "Registry value is null for key %s" (key.ToString())
    value.ToString()

/// Gets a registry value as string
let getRegistryValue64 baseKey subKey name =
    use key = getRegistryKey64 baseKey subKey false
    if isNull key then
        failwithf "Registry subkey %s could not be found for key %A" subKey baseKey
    let value = key.GetValue name
    if isNull value then
        failwithf "Registry value is null for key %s" (key.ToString())
    value.ToString()

/// Sets a registry value
let setRegistryValue<'T> baseKey subKey name (value : 'T) =
    use key = getRegistryKey baseKey subKey true
    key.SetValue(name, value)

/// Deletes the registry value from its key
let deleteRegistryValue baseKey subKey name =
    use key = getRegistryKey baseKey subKey true
    key.DeleteValue name

/// Returns all the value names of a registry key
let getRegistryValueNames baseKey subKey =
    use key = getRegistryKey baseKey subKey false
    key.GetValueNames()

/// Returns whether or not a registry value name exists for a key
let valueExistsForKey = fun baseKey subKey name ->
    getRegistryValueNames baseKey subKey
    |> Seq.exists (fun n -> n = name)

(*
Subkey Commands
*)
/// Create a registry subKey
let createRegistrySubKey baseKey subKey =
    use key = getKey baseKey
    key.CreateSubKey subKey |> ignore

/// Deletes a registry subKey
let deleteRegistrySubKey baseKey subKey =
    use key = getKey baseKey
    key.DeleteSubKey subKey

/// Returns all the subKey names of a registry key
let getRegistrySubKeyNames baseKey subKey =
    use key = getRegistryKey baseKey subKey false
    key.GetSubKeyNames()

