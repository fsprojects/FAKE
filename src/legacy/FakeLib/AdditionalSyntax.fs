[<AutoOpen>]
/// Provides functions and operators to deal with FAKE targets and target dependencies.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target")>]
module Fake.AdditionalSyntax

open System.Collections.Generic

/// Allows to use Tokens instead of strings
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(?)")>]
let (?) f s = f s

/// Allows to use Tokens instead of strings for TargetNames
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(?<-)")>]
let (?<-) f str action = f str action

/// Allows to use For? syntax for Dependencies
[<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
let For x y = x <== y

/// Converts a dependency into a list
[<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
let Dependency x = [x]

/// Appends the dependency to the list of dependencies
[<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
let And x y = y @ [x]

/// Runs a Target and its dependencies
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.run)")>]
let Run targetName = run targetName

/// Runs the target given by the build script parameter or the given default target
[<System.Obsolete("Please open an issue and tell us why you need it! (FAKE0002 - no longer supported)")>]
let RunParameterTargetOrDefault parameterName defaultTarget = getBuildParamOrDefault parameterName defaultTarget |> Run

/// Runs the target given by the target parameter or the given default target
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.runOrDefault)")>]
let RunTargetOrDefault defaultTarget = getBuildParamOrDefault "target" defaultTarget |> Run

/// Runs the target given by the target parameter or lists the available targets
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.runOrList)")>]
let RunTargetOrListTargets() =
    if hasBuildParam "target" then getBuildParam "target" |> Run
    else listTargets()

/// Runs the target given by the target parameter
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.Target.runOrDefault with \"\" default target)")>]
let RunTarget() = getBuildParam "target" |> Run

/// Stores which targets are on the same level
let private sameLevels = new Dictionary<_,_>()

/// Specifies that two targets are on the same level of execution
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let targetsAreOnSameLevel x y =
    match sameLevels.TryGetValue y with
    | true, z -> ()
    | _  -> sameLevels.[y] <- x

/// Specifies that two targets have the same dependencies
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let rec addDependenciesOnSameLevel target dependency =
    match sameLevels.TryGetValue dependency with
    | true, x -> 
        addDependenciesOnSameLevel target x
        Dependencies target [x]
    | _  -> ()

/// Specifies that two targets have the same dependencies
[<System.Obsolete("Internal state is no more accessible now (FAKE0003 - package: Fake.Core.Target). If you consider, it is still useful, please open an issue and explain your use case")>]
let rec addSoftDependenciesOnSameLevel target dependency =
    match sameLevels.TryGetValue dependency with
    | true, x -> 
        addSoftDependenciesOnSameLevel target x
        SoftDependencies target [x]
    | _  -> ()


/// Defines a dependency - y is dependent on x
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(==>)")>]
let inline (==>) x y =
    addDependenciesOnSameLevel y x 
    Dependencies y [x]
    y


/// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(?=>)")>]
let inline (?=>) x y = 
   addSoftDependenciesOnSameLevel y x 
   SoftDependencies y [x]
   y

/// Defines a soft dependency. x must run before y, if it is present, but y does not require x to be run.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(<=?)")>]
let inline (<=?) y x = x ?=> y


/// Defines that x and y are not dependent on each other but y is dependent on all dependencies of x.
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(<=>)")>]
let inline (<=>) x y =   
    let target_x = getTarget x
    Dependencies y target_x.Dependencies
    targetsAreOnSameLevel x y
    y

/// Defines a conditional dependency - y is dependent on x if the condition is true
[<System.Obsolete("Use Fake.Core.Target instead (FAKE0001 - package: Fake.Core.Target - member: Fake.Core.TargetOperators.(=?>)")>]
let inline (=?>) x (y,condition) = if condition then x ==> y else x
