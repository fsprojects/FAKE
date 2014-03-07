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

/// Gets a registy key.
let getRegistryKey baseKey subKey (writePermission : bool) = (getKey baseKey).OpenSubKey(subKey, writePermission)

/// Gets a registy value as string
let getRegistryValue baseKey subKey value = 
    use key = getRegistryKey baseKey subKey false
    (key.GetValue value).ToString()

/// create a registry subKey
let createRegistrySubKey baseKey subKey =
    (getKey baseKey).CreateSubKey subKey |> ignore

/// Sets a registry value
let setRegistryValue<'T> baseKey subKey keyName (value:'T) =
    use key = getRegistryKey baseKey subKey true
    key.SetValue(keyName, value)

let deleteRegistryValue baseKey subKey keyName =
    use key = getRegistryKey baseKey subKey true
    key.DeleteValue keyName