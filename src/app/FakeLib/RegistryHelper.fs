[<AutoOpen>]
module RegistryHelper

open Microsoft.Win32

type RegistryBaseKey =
| HKEYLocalMachine
| HKEYClassesRoot
| HKEYUsers
| HKEYCurrentUser
| HKEYCurrentConfig
| HKEYDyncData
| HKEYPerformanceData

/// Maps the RegistryBaseKey to a RegistryKey
let getKey = function
| HKEYLocalMachine -> Registry.LocalMachine
| HKEYClassesRoot -> Registry.ClassesRoot
| HKEYUsers -> Registry.Users
| HKEYCurrentUser -> Registry.CurrentUser
| HKEYCurrentConfig -> Registry.CurrentConfig
| HKEYDyncData -> Registry.DynData
| HKEYPerformanceData -> Registry.PerformanceData

/// gets a registy key
let getRegistryKey baseKey subKey =
  (getKey baseKey).OpenSubKey subKey

/// gets a registy value as string
let getRegistryValue baseKey subKey value =
  use key = getRegistryKey baseKey subKey
  (key.GetValue value).ToString()
    