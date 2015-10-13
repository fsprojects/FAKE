[<AutoOpen>]
/// Contains functions which allow to read and write information from/to the registry.
module Fake.RegistryHelper

open Microsoft.Win32

/// Registry base keys.
type RegistryBaseKey =
    | HKEYLocalMachine
    | HKEYClassesRoot
    | HKEYUsers
    | HKEYCurrentUser
    | HKEYCurrentConfig
    | HKEYPerformanceData

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

/// Gets a 64-bit registy key
let getRegistryKey64 baseKey subKey (writePermission : bool) =
    (get64BitKey baseKey).OpenSubKey(subKey, writePermission)

/// Gets a registy key and falls back to 32 bit if the 64bit key is not there
let getRegistryKey baseKey subKey (writePermission : bool) =
    let x64BitKey = (getKey baseKey).OpenSubKey(subKey, writePermission)
    if x64BitKey <> null then x64BitKey else
    (get32BitKey baseKey).OpenSubKey(subKey, writePermission)  // fall back to 32 bit

/// Gets a registy value as string
let getRegistryValue baseKey subKey value =
    use key = getRegistryKey baseKey subKey false
    if key = null then
        failwithf "Registry subkey %s could not be found for key %A" subKey baseKey
    let value = key.GetValue value
    if value = null then
        failwithf "Registry value is null for key %s" (key.ToString())
    value.ToString()

/// Gets a registy value as string
let getRegistryValue64 baseKey subKey value =
    use key = getRegistryKey64 baseKey subKey false
    if key = null then
        failwithf "Registry subkey %s could not be found for key %A" subKey baseKey
    let value = key.GetValue value
    if value = null then
        failwithf "Registry value is null for key %s" (key.ToString())
    value.ToString()

/// Create a registry subKey
let createRegistrySubKey baseKey subKey = (getKey baseKey).CreateSubKey subKey |> ignore

/// Sets a registry value
let setRegistryValue<'T> baseKey subKey keyName (value : 'T) =
    use key = getRegistryKey baseKey subKey true
    key.SetValue(keyName, value)

/// Deletes the registry value from its key
let deleteRegistryValue baseKey subKey keyName =
    use key = getRegistryKey baseKey subKey true
    key.DeleteValue keyName

/// Deletes a registry subKey
let deleteRegistrySubKey baseKey subKey keyName =
    use key = getRegistryKey baseKey subKey true
    key.DeleteSubKey keyName

/// Returns all the value names of a registry key
let getRegistryValueNames baseKey subKey =
    use key = getRegistryKey baseKey subKey false
    key.GetValueNames()

/// Returns whether or not a registry value exists for a key
let valueExistsForKey = fun baseKey subKey value ->
    getRegistryValueNames baseKey subKey
    |> Seq.exists (fun v -> v = value)

/// Returns all the subKey names of a registry key
let getRegistrySubKeyNames baseKey subKey =
    use key = getRegistryKey baseKey subKey false
    key.GetSubKeyNames()

