namespace Fake.IO

open System.Collections.Generic

/// <summary>
/// The glob pattern type
/// </summary>
type IGlobbingPattern =
    inherit IEnumerable<string>
    abstract BaseDirectory: string
    abstract Includes: string list
    abstract Excludes: string list

namespace Fake.IO.Globbing

open Fake.IO
open System.Collections.Generic

/// <summary>
/// The lazy glob pattern type
/// </summary>
type LazyGlobbingPattern =
    { BaseDirectory: string
      Includes: string list
      Excludes: string list }

    interface IGlobbingPattern with
        member this.BaseDirectory = this.BaseDirectory
        member this.Includes = this.Includes
        member this.Excludes = this.Excludes

    interface IEnumerable<string> with

        member this.GetEnumerator() =
            let hashSet = HashSet()

            let excludes =
                seq {
                    for pattern in this.Excludes do
                        yield! Glob.search this.BaseDirectory pattern
                }
                |> Set.ofSeq

            let files =
                seq {
                    for pattern in this.Includes do
                        yield! Glob.search this.BaseDirectory pattern
                }
                |> Seq.filter (fun x -> not (Set.contains x excludes))
                |> Seq.filter (fun x -> hashSet.Add x)

            files.GetEnumerator()

        member this.GetEnumerator() =
            (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

/// <summary>
/// Holds globbing patterns for backward compatability, see <c>GlobbingPatternExtensions</c>
/// </summary>
type ResolvedGlobbingPattern =
    { BaseDirectory: string
      Includes: string list
      Excludes: string list
      Results: string list }

    interface IGlobbingPattern with
        member this.BaseDirectory = this.BaseDirectory
        member this.Includes = this.Includes
        member this.Excludes = this.Excludes

    interface IEnumerable<string> with
        member this.GetEnumerator() =
            (this.Results :> IEnumerable<string>).GetEnumerator()

        member this.GetEnumerator() =
            (this :> IEnumerable<string>).GetEnumerator() :> System.Collections.IEnumerator

namespace Fake.IO

open System.IO
open Fake.IO.Globbing

/// <summary>
/// Contains extensions for glob pattern module.
/// </summary>
[<AutoOpen>] // A bit of a hack but we need those extensions for backwards compat.
module GlobbingPatternExtensions =
    type IGlobbingPattern with

        member internal this.Pattern =
            match this with
            | :? LazyGlobbingPattern as l -> l
            | _ ->
                { BaseDirectory = this.BaseDirectory
                  Includes = this.Includes
                  Excludes = this.Excludes }

        member this.Resolve() =
            match this with
            | :? ResolvedGlobbingPattern as res -> res :> IGlobbingPattern
            | _ ->
                let list = this |> Seq.toList

                { BaseDirectory = this.BaseDirectory
                  Includes = this.Includes
                  Excludes = this.Excludes
                  Results = list }
                :> IGlobbingPattern

        /// Adds the given pattern to the file includes
        member this.And pattern =
            { this.Pattern with Includes = this.Includes @ [ pattern ] } :> IGlobbingPattern

        /// Ignores files with the given pattern
        member this.ButNot pattern =
            { this.Pattern with Excludes = pattern :: this.Excludes } :> IGlobbingPattern

        /// Sets a directory as BaseDirectory.
        member this.SetBaseDirectory(dir: string) =
            { this.Pattern with
                BaseDirectory = dir.TrimEnd(Path.DirectorySeparatorChar) }
            :> IGlobbingPattern

        /// Checks if a particular file is matched
        member this.IsMatch(path: string) =
            let fullDir (pattern: string) =
                if Path.IsPathRooted(pattern) then
                    pattern
                else
                    Path.Combine(this.BaseDirectory, pattern)

            let fullPath = Path.GetFullPath path

            let included =
                this.Includes
                |> Seq.exists (fun fileInclude -> Glob.isMatch (fullDir fileInclude) fullPath)

            let excluded =
                this.Excludes
                |> Seq.exists (fun fileExclude -> Glob.isMatch (fullDir fileExclude) fullPath)

            included && not excluded


/// <summary>
/// Contains tasks to interact with file system using glob patterns
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GlobbingPattern =
    let private defaultBaseDir = Path.GetFullPath "."

    /// Include files
    let create x =
        { BaseDirectory = defaultBaseDir
          Includes = [ x ]
          Excludes = [] }
        :> IGlobbingPattern

    /// Start an empty globbing pattern from the specified directory
    let createFrom (dir: string) =
        { BaseDirectory = dir; Includes = []; Excludes = [] } :> IGlobbingPattern

    /// Sets a directory as baseDirectory for fileIncludes.
    let setBaseDir (dir: string) (fileIncludes: IGlobbingPattern) = fileIncludes.SetBaseDirectory dir

    /// Get base include directories. Used to get a smaller set of directories from a globbing pattern.
    let getBaseDirectoryIncludes (fileIncludes: IGlobbingPattern) =
        let directoryIncludes =
            fileIncludes.Includes
            |> Seq.map (fun file -> Glob.getRoot fileIncludes.BaseDirectory file)

        // remove subdirectories
        directoryIncludes
        |> Seq.filter (fun d ->
            directoryIncludes
            |> Seq.exists (fun p -> d.StartsWith(p + string Path.DirectorySeparatorChar) && p <> d)
            |> not)
        |> Seq.toList

namespace Fake.IO.Globbing

open Fake.IO

/// <summary>
/// Contains operators to find and process files.
/// This module is part of the <c>Fake.IO.FileSystem</c> package
/// </summary>
///
/// <example>
/// Simple glob using as list
/// <code lang="fsharp">
/// #r "paket: nuget Fake.IO.FileSystem //"
///     open Fake.IO.Globbing.Operators
///     let csProjectFiles = !! "src/*.csproj"
///
///     for projectFile in csProjectFiles do
///         printf "F# ProjectFile: %s" projectFile
/// </code>
/// </example>
///
/// <example>
/// Combine globs
/// <code lang="fsharp">
/// #r "paket: nuget Fake.IO.FileSystem //"
///     open Fake.IO.Globbing.Operators
///     let projectFiles =
///         !! "src/*/*.*proj"
///         ++ "src/*/*.target"
///         -- "src/*/*.vbproj"
///
///     for projectFile in projectFiles do
///         printf "ProjectFile: %s" projectFile
/// </code>
/// </example>
///
/// <example>
/// Forward globs to tasks
/// <code lang="fsharp">
/// #r "paket:
///     nuget Fake.Core.Target
///     nuget Fake.IO.FileSystem //"
///     open Fake.Core
///     open Fake.IO
///     open Fake.IO.Globbing.Operators
///     Target.create "Clean" (fun _ ->
///        !! "src/*/*/obj/**/*.nuspec"
///        |> File.deleteAll
///     )
/// </code>
/// </example>
module Operators =
    /// <summary>
    /// Add Include operator
    /// </summary>
    ///
    /// <param name="x">The pattern to include</param>
    ///  - `x` - The pattern to include
    let inline (++) (x: IGlobbingPattern) pattern = x.And pattern

    /// <summary>
    /// Exclude operator
    /// </summary>
    ///
    /// <param name="x">The pattern to include</param>
    let inline (--) (x: IGlobbingPattern) pattern = x.ButNot pattern

    /// <summary>
    /// Includes a single pattern and scans the files - <c>!! x = AllFilesMatching x</c>
    /// </summary>
    ///
    /// <param name="x">The pattern to create globbing from</param>
    let inline (!!) x = GlobbingPattern.create x
