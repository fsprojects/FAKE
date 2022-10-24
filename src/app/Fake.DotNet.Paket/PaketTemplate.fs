/// <summary>
/// Contains helper functions and task which allow it to generate a paket.template
/// file for <a href="http://fsprojects.github.io/Paket/index.html">Paket</a>
/// </summary>
module Fake.DotNet.PaketTemplate

open System
open System.Text
open Fake.Core
open Fake.IO

type PaketTemplateType =
    | File
    | Project

type PaketFileInfo =
    /// Include a file and store it into a targed
    | Include of string * string
    /// Explicitely exclude a file
    | Exclude of string

type PaketDependencyVersion =
    /// A specific version string
    | Version of string
    /// Use the currently loaded version as dependency
    | CURRENTVERSION
    /// Use the currently locked version as dependency
    | LOCKEDVERSION

type PaketDependencyVersionInfo =
    /// For example ~> 2.0
    | GreaterOrEqualSafe of PaketDependencyVersion

    /// For example >= 2.0
    | GreaterOrEqual of PaketDependencyVersion

    /// no explicit version
    | AnyVersion

type PaketDependency = string * PaketDependencyVersionInfo

/// <summary>
/// Contains the different parameters to create a paket.template file
/// </summary>
type PaketTemplateParams =
    {
        /// The file path to the <c>paket.template</c> file
        /// if omitted, a <c>paket.template</c> file will be created in the current directory
        TemplateFilePath: string option
        /// The type of the template (`File` or `Project`)
        TemplateType: PaketTemplateType
        /// The NuGet Package ID
        /// If omitted, `paket` will use reflection to determine the assembly name.
        Id: string option
        /// The package version.
        /// If omitted, `paket` will use reflection to obtain the value of the
        /// <c>AssemblyInformationalVersionAttribute</c> or if that is missing the `AssemblyVersionAttribute`.
        Version: string option
        /// The package description
        /// If omitted, `paket` will use reflection to obtain the value of the <c>AssemblyDescriptionAttribute</c>.
        Description: string list
        /// The title of the package
        /// If omitted, `paket` will use reflection to obtain the value of the <c>AssemblyTitleAttribute</c>.
        Title: string option
        /// a list of authors for the nuget package.
        /// If omitted, `paket`will use reflection to obtain the value of the <c>AssemblyCompanyAttribute</c>.
        Authors: string list
        /// A list of package owners
        Owners: string list
        /// the release notes (line by line)
        ReleaseNotes: string list
        /// a short summary (line by line)
        Summary: string list
        /// The package language
        Language: string option
        /// URL to the license of the package
        LicenseUrl: string option
        /// URL to the where the project of the package is hosted
        ProjectUrl: string option
        /// URL to an icon
        IconUrl: string option
        /// the copyright information
        Copyright: string option
        /// a list of tags
        Tags: string list
        /// The included or excluded files (use this if the <c>TemplateType</c> is <c>File</c>)
        Files: PaketFileInfo list
        /// A list of references
        References: string list
        /// A list of referenced framework assemblies
        /// if omitted all used Framework assemblies will be used by `paket`
        FrameworkAssemblies: string list
        /// A list of dependencies to other packages
        Dependencies: PaketDependency list
        /// A list of excluded dependencies
        ExcludedDependencies: string list
        /// If set to <c>true</c> this will tell <c>nuget</c>/<c>paket</c> to prompt the user for
        /// the acceptance of the provided license
        RequireLicenseAcceptance: bool option
        /// If set to <c>true</c> this will tell <c>nuget</c>/<c>paket</c> that this is a development dependency
        DevelopmentDependency: bool option
        /// With the <c>IncludePDBs</c> switch you can tell `paket` to pack pdbs into the package.
        /// this only works for paket.template files of type 'Project'.
        IncludePDBs: bool option
    }

/// <summary>
/// The default parameters for the generation of the <c>paket.template</c>
/// </summary>
///
/// <list type="number">
/// <item>
/// <c>TemplateFilePath</c> - <c>None</c>
/// </item>
/// <item>
/// <c>TemplateType</c> - <c>Project</c>
/// </item>
/// <item>
/// <c>Id</c> - <c>None</c>
/// </item>
/// <item>
/// <c>Version</c> - <c>None</c>
/// </item>
/// <item>
/// <c>Description</c> - <c>None</c>
/// </item>
/// <item>
/// <c>Title</c> - <c>None</c>
/// </item>
/// <item>
/// <c>Authors</c> - <c>>Empty list</c>
/// </item>
/// <item>
/// <c>Owners</c> - <c>>Empty list</c>
/// </item>
/// <item>
/// <c>ReleaseNotes</c> - <c>>Empty list</c>
/// </item>
/// <item>
/// <c>Summary</c> - <c>>Empty list</c>
/// </item>
/// <item>
/// <c>Language</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>LicenseUrl</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>ProjectUrl</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>IconUrl</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>Copyright</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>Tags</c> - <c>Empty list</c>
/// </item>
/// <item>
/// <c>Files</c> - <c>Empty list</c>
/// </item>
/// <item>
/// <c>References</c> - <c>Empty list</c>
/// </item>
/// <item>
/// <c>FrameworkAssemblies</c> - <c>Empty list</c>
/// </item>
/// <item>
/// <c>Dependencies</c> - <c>Empty list</c>
/// </item>
/// <item>
/// <c>ExcludedDependencies</c> - <c>Empty list</c>
/// </item>
/// <item>
/// <c>RequireLicenseAcceptance</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>DevelopmentDependency</c> - <c>>None</c>
/// </item>
/// <item>
/// <c>IncludePDBs</c> - <c>>None</c>
/// </item>
/// </list>
let DefaultPaketTemplateParams =
    { TemplateFilePath = None
      TemplateType = Project
      Id = None
      Version = None
      Description = []
      Title = None
      Authors = []
      Owners = []
      ReleaseNotes = []
      Summary = []
      Language = None
      LicenseUrl = None
      ProjectUrl = None
      IconUrl = None
      Copyright = None
      Tags = []
      Files = []
      References = []
      FrameworkAssemblies = []
      Dependencies = []
      ExcludedDependencies = []
      RequireLicenseAcceptance = None
      DevelopmentDependency = None
      IncludePDBs = None }

module internal Rendering =
    let inline appendWithName name value (sb: StringBuilder) = sb.Append(sprintf "%s %s\n" name value)

    let inline appendWithNameIfSome name value sb =
        match value with
        | Some v when (v |> String.isNullOrWhiteSpace |> not) -> sb |> appendWithName name v
        | _ -> sb

    let inline appendBoolWithNameIfSome name value (sb: StringBuilder) =
        match value with
        | Some v -> sb.Append(sprintf "%s %b\n" name v)
        | _ -> sb

    let inline appendIndented value (sb: StringBuilder) =
        match value with
        | v when (v |> String.isNullOrWhiteSpace) -> sb
        | _ -> sb.Append(sprintf "    %s\n" value)

    let inline appendListWithName name lines (sb: StringBuilder) =
        match lines with
        | [] -> sb
        | singleLine :: [] -> sb |> appendWithName name singleLine
        | _ ->
            lines
            |> Seq.fold (fun s line -> s |> appendIndented line) (sb.Append(sprintf "%s\n" name))

    let inline appendCommaListWithName name values sb =
        match values with
        | [] -> sb
        | _ -> sb |> appendWithName name (String.Join(",", values))

    let inline renderPaketFileInfo fileInfo =
        match fileInfo with
        | Include (source, target) -> sprintf "%s ==> %s" source target
        | Exclude file -> sprintf "!%s" file

    let inline appendPaketFileInfos (fileInfos: PaketFileInfo seq) (sb: StringBuilder) =
        sb
        |> appendListWithName "files" (fileInfos |> Seq.map renderPaketFileInfo |> Seq.toList)

    let inline renderPaketDependencyVersion version =
        match version with
        | Version x -> x
        | CURRENTVERSION -> "CURRENTVERSION"
        | LOCKEDVERSION -> "LOCKEDVERSION"

    let inline renderPaketDependencyVersionInfo versionInfo =
        match versionInfo with
        | AnyVersion -> ""
        | GreaterOrEqual version -> sprintf " >= %s" (renderPaketDependencyVersion version)
        | GreaterOrEqualSafe version -> sprintf " ~> %s" (renderPaketDependencyVersion version)

    let inline renderPaketDependency (package, versionInfo) =
        match package with
        | p when (p |> String.isNullOrWhiteSpace) -> None
        | _ -> Some(sprintf "%s%s" package (renderPaketDependencyVersionInfo versionInfo))

    let inline appendDependencies dependencies sb =
        let dependencyStrings =
            dependencies
            |> Seq.map renderPaketDependency
            |> Seq.filter Option.isSome
            |> Seq.map Option.get
            |> Seq.toList

        sb |> appendListWithName "dependencies" dependencyStrings

    let inline createLines p =
        let sb = StringBuilder()

        sb
        |> appendWithName
            "type"
            (match p.TemplateType with
             | File -> "file"
             | Project -> "project")
        |> appendWithNameIfSome "id" p.Id
        |> appendWithNameIfSome "version" p.Version
        |> appendWithNameIfSome "title" p.Title
        |> appendListWithName "summary" p.Summary
        |> appendListWithName "description" p.Description
        |> appendWithNameIfSome "copyright" p.Copyright
        |> appendCommaListWithName "authors" p.Authors
        |> appendCommaListWithName "owners" p.Owners
        |> appendCommaListWithName "tags" p.Tags
        |> appendWithNameIfSome "language" p.Language
        |> appendWithNameIfSome "projectUrl" p.ProjectUrl
        |> appendWithNameIfSome "iconUrl" p.IconUrl
        |> appendWithNameIfSome "licenseUrl" p.LicenseUrl
        |> appendListWithName "releaseNotes" p.ReleaseNotes
        |> appendBoolWithNameIfSome "requireLicenseAcceptance" p.RequireLicenseAcceptance
        |> appendBoolWithNameIfSome "developmentDependency" p.DevelopmentDependency
        |> appendBoolWithNameIfSome "include-pdbs" p.IncludePDBs
        |> appendPaketFileInfos p.Files
        |> appendListWithName "references" p.References
        |> appendListWithName "frameworkAssemblies" p.FrameworkAssemblies
        |> appendDependencies p.Dependencies
        |> appendListWithName "excludeddependencies" p.ExcludedDependencies
        |> string


/// <summary>
/// Creates a paket.template file with the given filename
/// Will fail if the file could not be written
/// </summary>
///
/// <param name="setParams">Function used to manipulate the default <c>PaketTemplateParams</c> value</param>
///
/// <example>
/// <code lang="fsharp">
/// Target "Create Paket Template" (fun _ ->
///       PaketTemplate (fun p ->
///            { p with
///                TemplateFilePath = Some "./deploytemp/paket.template"
///                Id = Some "My.New.Package"
///                Version = Some "0.0.1-alpha"
///                Authors = ["Me"; "You"; "Someone Other"]
///                Files = [ Include ("./**/*.dll", "/lib/dlls")
///                          Exclude "./Foo/bar.dll"
///                          Include ("./*.jpg", "/images") ]
///                Dependencies = [ "Paket1.FOO", AnyVersion
///                                 "Paket2.BAR", GreaterOrEqual CURRENTVERSION
///                                 "Paket3.BAZ", GreaterOrEqualSafe LOCKEDVERSION
///                                 "Paket4.BOO", GreaterOrEqual (Version "1.2.3") ]
///            }
///        )
///    )
/// </code>
/// </example>
let create setParams =
    use __ = Trace.traceTask "PaketTemplate" ""
    let parameters = setParams DefaultPaketTemplateParams

    let filePath =
        match parameters.TemplateFilePath with
        | Some v -> v
        | _ -> "paket.template"

    File.writeString false filePath (Rendering.createLines parameters)
    __.MarkSuccess()
