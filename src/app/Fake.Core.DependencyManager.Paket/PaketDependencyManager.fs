namespace Fake.Core.DependencyManager.Paket

/// <namespacedoc>
/// <summary>
/// DependencyManager.Paket namespace contains tasks to interact with paket dependency manager
/// </summary>
/// </namespacedoc>
module Internals = 
    open System

    /// <summary>
    /// A marker attribute to tell FCS that this assembly contains a Dependency Manager, or
    /// that a class with the attribute is a <c>DependencyManager</c>
    /// </summary>
    [<AttributeUsage(AttributeTargets.Assembly ||| AttributeTargets.Class , AllowMultiple = false)>]
    type DependencyManagerAttribute() =
        inherit Attribute()

    [<assembly: DependencyManagerAttribute()>]
    do ()

    /// <summary>
    /// returned structure from the ResolveDependencies method call.
    /// </summary>
    type ResolveDependenciesResult (success: bool, stdOut: string array, stdError: string array, resolutions: string seq, sourceFiles: string seq, roots: string seq) =

        /// Succeeded?
        member _.Success = success

        /// The resolution output log
        member _.StdOut = stdOut

        /// The resolution error log (* process stderror *)
        member _.StdError = stdError

        /// The resolution paths (will be treated as #r options)
        member _.Resolutions = resolutions

        /// The source code file paths (will be treated as #load options)
        member _.SourceFiles = sourceFiles

        /// The roots to package directories (will be treated like #I options)
        member _.Roots = roots

    type ScriptExtension = string
    type HashRLines = string seq
    type TFM = string

    [<DependencyManager>]
    // the type _must_ take an optional output directory
    type PaketDependencyManager(outputDir: string option) =

        /// Name of the dependency manager
        member val Name = "Dummy Paket Dependency Manager" with get

        /// Key that identifies the types of dependencies that this DependencyManager operates on
        member val Key = "paket" with get

        /// Resolve the dependencies, for the given set of arguments, go find the .dll references, scripts and additional include values.
        member _.ResolveDependencies(scriptExt: ScriptExtension, includeLines: HashRLines, tfm: TFM): obj = 
            // generally, here we'd parse the includeLines to determine what to do,
            // package those results into a `ResolveDependenciesResult`,
            // and return it boxed as obj.
            // but here we will return a dummy
            ResolveDependenciesResult(true, [|"Skipped processing of paket references"|], [||], Seq.empty, Seq.empty, Seq.empty) :> _


