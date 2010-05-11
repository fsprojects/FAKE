[<AutoOpen>]
module Fake.EnvironmentHelper

open System

type EnvironTarget = EnvironmentVariableTarget

/// Retrieves the EnvironmentVariable
let environVar = Environment.GetEnvironmentVariable

/// Retrieves the EnvironmentVariable
let environVars x = 
  [for e in Environment.GetEnvironmentVariables x ->
     let e1 = e :?> Collections.DictionaryEntry
     e1.Key,e1.Value]

/// Returns true if the buildParam is set and otherwise false
let hasBuildParam name = environVar name <> null

/// Returns the value of the buildParam if it is set and otherwise "" 
let getBuildParam name = if hasBuildParam name then environVar name else String.Empty

/// Returns the value of the buildParam if it is set and otherwise the default
let getBuildParamOrDefault name defaultParam = if hasBuildParam name then getBuildParam name else defaultParam

/// The path of Program Files (x86)
let ProgramFilesX86 =
    if 8 = IntPtr.Size || not (isNullOrEmpty(environVar "PROCESSOR_ARCHITEW6432")) then
        environVar "ProgramFiles(x86)"
    else
        environVar "ProgramFiles"