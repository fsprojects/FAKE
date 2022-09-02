namespace Fake.Core

/// <summary>
/// Provides functions and operators to deal with FAKE targets and target dependencies.
/// </summary>
module TargetOperators =

    open Fake.Core
    open System.Collections.Generic

    /// <summary>
    /// Allows to use Tokens instead of strings
    /// </summary>
    let (?) f s = f s

    /// <summary>
    /// Allows to use Tokens instead of strings for TargetNames
    /// </summary>
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


    /// <summary>
    /// Stores which targets are on the same level
    /// </summary>
    let private sameLevels =
        Target.getVarWithInit "sameLevels" (fun () -> Dictionary<_, _>(System.StringComparer.OrdinalIgnoreCase))


    /// <summary>
    /// Specifies that two targets are on the same level of execution
    /// </summary>
    let internal targetsAreOnSameLevel x y =
        match sameLevels().TryGetValue y with
        | true, _ -> ()
        | _ -> sameLevels().[y] <- x

    /// <summary>
    /// Specifies that two targets have the same dependencies
    /// </summary>
    let rec internal addDependenciesOnSameLevel target dependency =
        match sameLevels().TryGetValue dependency with
        | true, x ->
            addDependenciesOnSameLevel target x
            Target.Dependencies target [ x ]
        | _ -> ()

    /// <summary>
    /// Specifies that two targets have the same dependencies
    /// </summary>
    let rec internal addSoftDependenciesOnSameLevel target dependency =
        match sameLevels().TryGetValue dependency with
        | true, x ->
            addSoftDependenciesOnSameLevel target x
            Target.SoftDependencies target [ x ]
        | _ -> ()


    /// <summary>
    /// Defines a dependency - y is dependent on x
    /// </summary>
    let (==>) x y =
        addDependenciesOnSameLevel y x
        Target.Dependencies y [ x ]
        y


    /// <summary>
    /// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run.
    /// </summary>
    let (?=>) x y =
        addSoftDependenciesOnSameLevel y x
        Target.SoftDependencies y [ x ]
        y

    /// <summary>
    /// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run.
    /// </summary>
    let (<=?) y x = x ?=> y


    /// <summary>
    /// Defines that x and y are not dependent on each other but y is dependent on all dependencies of x.
    /// </summary>
    let (<=>) x y =
        let target_x = Target.get x
        Target.Dependencies y target_x.Dependencies
        targetsAreOnSameLevel x y
        y

    /// <summary>
    /// Defines a conditional dependency - y is dependent on x if the condition is true
    /// </summary>
    let (=?>) x (y, condition) = if condition then x ==> y else x
