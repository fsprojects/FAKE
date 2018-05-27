/// Provides functions and operators to deal with FAKE targets and target dependencies.
module Fake.Core.TargetOperators

open Fake.Core
open System.Collections.Generic

/// Allows to use Tokens instead of strings
let (?) f s = f s

/// Allows to use Tokens instead of strings for TargetNames
let (?<-) f str action = f str action

let (<==) x y = Target.(<==) x y

// Allows to use For? syntax for Dependencies
// I have no idea, remove and wait for people to complain
//let For x y = x <== y

// Converts a dependency into a list
// I have no idea, remove and wait for people to complain
//let Dependency x = [x]

// Appends the dependency to the list of dependencies
// I have no idea, remove and wait for people to complain
//let And x y = y @ [x]


/// Stores which targets are on the same level
let private sameLevels = 
    Target.getVarWithInit "sameLevels" (fun () -> new Dictionary<_,_>(System.StringComparer.OrdinalIgnoreCase))
        

/// Specifies that two targets are on the same level of execution
let internal targetsAreOnSameLevel x y =
    match sameLevels().TryGetValue y with
    | true, _ -> ()
    | _  -> sameLevels().[y] <- x

/// Specifies that two targets have the same dependencies
let rec internal addDependenciesOnSameLevel target dependency =
    match sameLevels().TryGetValue dependency with
    | true, x -> 
        addDependenciesOnSameLevel target x
        Target.Dependencies target [x]
    | _  -> ()

/// Specifies that two targets have the same dependencies
let rec internal addSoftDependenciesOnSameLevel target dependency =
    match sameLevels().TryGetValue dependency with
    | true, x -> 
        addSoftDependenciesOnSameLevel target x
        Target.SoftDependencies target [x]
    | _  -> ()


/// Defines a dependency - y is dependent on x
let (==>) x y =
    addDependenciesOnSameLevel y x 
    Target.Dependencies y [x]
    y


/// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run.
let (?=>) x y = 
   addSoftDependenciesOnSameLevel y x 
   Target.SoftDependencies y [x]
   y

/// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run.
let (<=?) y x = x ?=> y


/// Defines that x and y are not dependent on each other but y is dependent on all dependencies of x.
let (<=>) x y =   
    let target_x = Target.get x
    Target.Dependencies y target_x.Dependencies
    targetsAreOnSameLevel x y
    y

/// Defines a conditional dependency - y is dependent on x if the condition is true
let (=?>) x (y,condition) = if condition then x ==> y else x
