[<AutoOpen>]
module EnvironmentHelper

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