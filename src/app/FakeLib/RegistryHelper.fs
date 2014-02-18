[<AutoOpen>]
/// Contains function which allow to retrieve information from the registry.
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
let getRegistryKey baseKey subKey = (getKey baseKey).OpenSubKey subKey

/// Gets a registy value as string
let getRegistryValue baseKey subKey value = 
    use key = getRegistryKey baseKey subKey
    (key.GetValue value).ToString()
