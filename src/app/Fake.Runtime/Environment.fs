/// This module contains functions which allow to read and write environment variables and build parameters
module Fake.Runtime.Environment
//type Environment = System.Environment

open System
open System.IO

/// Gets the FAKE version no.
let fakeVersion = AssemblyVersionInformation.AssemblyInformationalVersion

/// Gets the FAKE Version string
let fakeVersionStr = sprintf "FAKE 5 - F# Make (%s)" fakeVersion

/// Retrieves the environment variable with the given name
let environVar name = System.Environment.GetEnvironmentVariable name

/// Retrieves all environment variables from the given target

let environVars () = 
    let vars = System.Environment.GetEnvironmentVariables ()
    [ for e in vars -> 
          let e1 = e :?> Collections.DictionaryEntry
          e1.Key, e1.Value ]

/// Sets the environment variable with the given name
let setEnvironVar name value = System.Environment.SetEnvironmentVariable(name, value)

/// Retrieves the environment variable with the given name or returns the default if no value was set
let environVarOrDefault name defaultValue = 
    let var = environVar name
    if String.IsNullOrEmpty var then defaultValue
    else var

/// Retrieves the environment variable with the given name or fails if not found
let environVarOrFail name = 
    let var = environVar name
    if String.IsNullOrEmpty var then failwith <| sprintf "Environment variable '%s' not found" name
    else var

/// Retrieves the environment variable with the given name or returns the default bool if no value was set
let getEnvironmentVarAsBoolOrDefault varName defaultValue =
    try  
        (environVar varName).ToUpper() = "TRUE" 
    with
    | _ ->  defaultValue

/// Retrieves the environment variable with the given name or returns the false if no value was set
let getEnvironmentVarAsBool varName = getEnvironmentVarAsBoolOrDefault varName false

/// Retrieves the environment variable or None
let environVarOrNone name = 
    let var = environVar name
    if String.IsNullOrEmpty var then None
    else Some var

/// Returns if the build parameter with the given name was set
let inline hasEnvironVar name = environVar name |> isNull |> not

open System.Reflection

let inline internal getAssemblyFromType (t:System.Type) =
#if NETSTANDARD1_6
    t.GetTypeInfo().Assembly
#else
    t.Assembly
#endif

let inline internal fsCoreAssembly () = getAssemblyFromType typeof<Microsoft.FSharp.Core.AutoOpenAttribute>
let inline internal fakeContextAssembly () = getAssemblyFromType typeof<Fake.Core.Context.FakeExecutionContext>
