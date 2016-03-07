/// Contains helper functions and task which allow it to generate a paket.template
/// file for [Paket](http://fsprojects.github.io/Paket/index.html)
module Fake.PaketTemplate

open System
open System.Text

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

/// Contains the different parameters to create a paket.template file
type PaketTemplateParams =
    { /// The file path to the `paket.template` file
      /// if omitted, a `paket.template` file will be created in the current directory
      TemplateFilePath : string option
      /// The type of the template (`File` or `Project`)
      TemplateType : PaketTemplateType
      /// The NuGet Package ID
      /// If omitted, `paket` will use reflection to determine the assembly name.
      Id : string option
      /// The package version.
      /// If omitted, `paket` will use reflection to obtain the value of the `AssemblyInformationalVersionAttribute` or if that is missing the `AssemblyVersionAttribute`.
      Version : string option
      /// The package description
      /// If omitted, `paket` will use reflection to obtain the value of the `AssemblyDescriptionAttribute`.
      Description : string list
      /// The title of the package
      /// If omitted, `paket` will use reflection to obtain the value of the `AssemblyTitleAttribute`.
      Title : string option
      /// a list of authors for the nuget package.
      /// If omitted, `paket`will use reflection to obtain the value of the `AssemblyCompanyAttribute`.
      Authors : string list
      /// A list of package owners
      Owners : string list
      /// the release notes (line by line)
      ReleaseNotes : string list
      /// a short summary (line by line)
      Summary : string list
      /// The package language
      Language : string option
      /// URL to the license of the package
      LicenseUrl : string option
      /// URL to the where the project of the package is hosted
      ProjectUrl : string option
      /// URL to an icon
      IconUrl : string option
      /// the copyright information
      Copyright : string option
      /// a list of tags
      Tags : string list
      /// The included or excluded files (use this if the `TemplateType` is `File`)
      Files : PaketFileInfo list
      /// A list of references
      References : string list
      /// A list of referenced framework assemblies
      /// if omitted all used Framework assemblies will be used by `paket`
      FrameworkAssemblies : string list
      /// A list of dependencies to other packages
      Dependencies : PaketDependency list
      /// A list of excluded dependencies
      ExcludedDependencies : string list
      /// If set to `true` this will tell `nuget`/`paket` to prompt the user for
      /// the acceptance of the provided license
      RequireLicenseAcceptance : bool option
      /// If set to `true` this will tell `nuget`/`paket` that this is a development dependency
      DevelopmentDependency : bool option
      /// With the `IncludePDBs` switch you can tell `paket` to pack pdbs into the package.
      /// this only works for paket.template files of type 'Project'.
      IncludePDBs : bool option}

/// The default parameters for the generation of the `paket.template`
///
/// ## Defaults
///
///   - TemplateFilePath - `None`
///   - TemplateType - `Project`
///   - Id - `None`
///   - Version - `None`
///   - Description - `None`
///   - Title - `None`
///   - Authors - `Empty list`
///   - Owners - `Empty list`
///   - ReleaseNotes - `Empty list`
///   - Summary - `Empty list`
///   - Language - `None`
///   - LicenseUrl - `None`
///   - ProjectUrl - `None`
///   - IconUrl - `None`
///   - Copyright - `None`
///   - Tags - `Empty list`
///   - Files - `Empty list`
///   - References - `Empty list`
///   - FrameworkAssemblies - `Empty list`
///   - Dependencies - `Empty list`
///   - ExcludedDependencies - `Empty list`
///   - RequireLicenseAcceptance - `None`
///   - DevelopmentDependency - `None`
///   - IncludePDBs - `None`
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
    let inline appendWithName name value (sb: StringBuilder) =
        sb.Append(sprintf "%s %s\n" name value)

    let inline appendWithNameIfSome name value sb =
        match value with
        | Some v when (v |> isNullOrWhiteSpace |> not)-> sb |> appendWithName name v
        | _ -> sb

    let inline appendBoolWithNameIfSome name value (sb: StringBuilder) =
        match value with
        | Some v -> sb.Append(sprintf "%s %b\n" name v)
        | _ -> sb

    let inline appendIndented value (sb: StringBuilder) =
        match value with
        | v when (v |> isNullOrWhiteSpace) -> sb
        | _ -> sb.Append(sprintf "    %s\n" value)

    let inline appendListWithName name lines (sb: StringBuilder) =
        match lines with
        | [] -> sb
        | singleLine::[] -> sb |> appendWithName name singleLine
        | _ -> lines
            |> Seq.fold (fun s line -> s |> appendIndented line) (sb.Append(sprintf "%s\n" name))

    let inline appendCommaListWithName name values sb =
        match values with
        | [] -> sb
        | _ -> sb |> appendWithName name (String.Join(",", values))

    let inline renderPaketFileInfo fileInfo =
        match fileInfo with
        | Include (source, target) -> sprintf "%s ==> %s" source target
        | Exclude file -> sprintf "!%s" file

    let inline appendPaketFileInfos (fileInfos : PaketFileInfo seq) (sb : StringBuilder) =
        sb |> appendListWithName "files" (fileInfos |> Seq.map renderPaketFileInfo |> Seq.toList)

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
        | p when (p |> isNullOrWhiteSpace) -> None
        | _ -> Some (sprintf "%s%s" package (renderPaketDependencyVersionInfo versionInfo))

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
        |> appendWithName "type" (match p.TemplateType with
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


/// Creates a paket.template file with the given filename
/// Will fail if the file could not be written
///
/// ## Parameters
///  - `setParams` - Function used to manipulate the default `PaketTemplateParams` value
///
/// ## Sample usage
///
///    Target "Create Paket Template" (fun _ ->
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
let PaketTemplate setParams =
    traceStartTask "PaketTemplate" ""
    let parameters = setParams DefaultPaketTemplateParams
    let filePath = match parameters.TemplateFilePath with
                   | Some v -> v
                   | _ -> "paket.template"

    WriteStringToFile false filePath (Rendering.createLines parameters)
    traceEndTask "PaketTemplate" ""
