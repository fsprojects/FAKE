[<AutoOpen>]
module Fake.AdditionalSyntax

open System.Collections.Generic

/// Allows to use Tokens instead of strings
let (?) f s = f s

/// Allows to use Tokens instead of strings for TargetNames
let (?<-) f str action = f str action

/// Allows to use For? syntax for Dependencies
let For x y = x <== y

/// Converts a dependency into a list
let Dependency x = [x]

/// Appends the dependency to the list of dependencies
let And x y = y @ [x]

/// Runs a Target and its dependencies
let Run = run

/// Runs the target given by the build script parameter or the given default target
let RunParameterTargetOrDefault parameterName defaultTarget = getBuildParamOrDefault parameterName defaultTarget |> Run

/// Stores which targets are on the same level
let private sameLevels = new Dictionary<_,_>()

let targetsAreOnSameLevel x y =
    match sameLevels.TryGetValue y with
    | true, z -> failwithf "Target %s is already on same level with %s" x z
    | _  -> sameLevels.[y] <- x

let rec addDependenciesOnSameLevel target dependency =
    match sameLevels.TryGetValue dependency with
    | true, x -> 
        addDependenciesOnSameLevel target x
        Dependencies target [x]
    | _  -> ()

/// Defines a dependency - y is dependent on x
let inline (==>) x y =
    addDependenciesOnSameLevel y x 
    Dependencies y [x]

    y

/// Defines that x and y are not dependent on each other but y is dependent on all dependencies of x.
let inline (<=>) x y =   
    let target_x = getTarget x
    Dependencies y target_x.Dependencies
    targetsAreOnSameLevel x y
    y

/// Defines a conditional dependency - y is dependent on x if the condition is true
let inline (=?>) x (y,condition) = if condition then x ==> y else x