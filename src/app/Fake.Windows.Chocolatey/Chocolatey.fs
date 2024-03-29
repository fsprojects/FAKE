namespace Fake.Windows

/// <namespacedoc>
/// <summary>
/// Windows namespace contains tasks to interact with Windows specific tools, like Chocolatey and Registery
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains tasks which allow to call <a href="https://chocolatey.org">Chocolatey</a>
/// </summary>
[<RequireQualifiedAccess>]
module Choco =

    open System
    open System.Text
    open System.IO
    open System.Xml.Linq
    open Fake.DotNet.NuGet.NuGet
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

    /// The choco installer type
    type ChocolateyInstallerType =
        | Zip
        | Exe
        | Msi
        | SelfContained

    /// The choco checksum type
    type ChocolateyChecksumType =
        | Md5
        | Sha1
        | Sha256
        | Sha512

    /// <summary>
    /// The choco install parameter type.
    /// </summary>
    type ChocoInstallParams =
        {
            /// Version of the package
            /// Equivalent to the <c>--version <version></c> option.
            Version: string
            /// Include prerelease. Default <c>false</c>.
            /// Equivalent to the <c>--pre</c> option.
            Prerelease: bool
            /// Parameters to pass to the package.
            /// Equivalent to the <c>--params <params></c> option.
            PackageParameters: string
            /// The source to find the package(s) to install.
            /// Special sources  include: ruby, webpi, cygwin, windowsfeatures, and python.
            /// Equivalent to the <c>--source <source></c> option.
            Source: string
            /// Force x86 (32bit) installation on 64 bit systems. Default <c>false</c>.
            /// Equivalent to the <c>--forcex86</c> option.
            ForceX86: bool
            /// Install Arguments to pass to the native installer in the package.
            /// Equivalent to the <c>--installargs <args></c> option.
            InstallArgs: string
            /// Should install arguments be used exclusively without appending to current package passed arguments?
            /// Default <c>false</c>.
            /// Equivalent to the <c>--overrideargs</c> option.
            OverrideArgs: bool
            /// Skip Powershell - Do not run chocolateyInstall.ps1. Default <c>false</c>.
            /// Equivalent to the <c>--skippowershell</c> option.
            SkipPowershell: bool
            /// User - used with authenticated feeds.
            /// Equivalent to the <c>--user <user></c> option.
            User: string
            /// Password - the user's password to the source.
            /// Equivalent to the <c>--password <password></c> option.
            Password: string
            /// The choco execution timeout.
            Timeout: TimeSpan
            /// The location of the choco executable. Automatically found if null or empty.
            ToolPath: string
            /// A character string containing additional arguments to give to choco.
            AdditionalArgs: string
            /// Do not prompt for user input or confirmations. Default <c>true</c>.
            /// Equivalent to the <c>-y</c> option.
            NonInteractive: bool
        }

    /// <summary>
    /// The choco pack parameter type.
    /// </summary>
    type ChocoPackParams =
        {
            /// The version you would like to insert into the package.
            /// Equivalent to the <c>--version <version></c> option.
            Version: string
            /// The choco execution timeout.
            Timeout: TimeSpan
            /// The location of the choco executable. Automatically found if null or empty.
            ToolPath: string
            /// A character string containing additional arguments to give to choco.
            AdditionalArgs: string
            /// Do not prompt for user input or confirmations. Default <c>true</c>.
            /// Equivalent to the <c>-y</c> option.
            NonInteractive: bool
            /// Authors of the package.
            /// Used for the nuspec creation.
            Authors: string list
            /// Owners of the package.
            /// Used for the nuspec creation.
            Owners: string list
            /// Id of the package. Should be lowercase, not contains weird chars and use dash (-) instead of spaces.
            /// Used for the nuspec creation.
            PackageId: string
            /// Title of the package.
            /// Used for the nuspec creation.
            Title: string
            /// Summary of the package.
            /// Used for the nuspec, <c>chocolateyInstall.ps1</c> and <c>chocolateyUninstall.ps1</c> creation.
            Summary: string
            /// Description of the package.
            /// Used for the nuspec creation.
            Description: string
            /// Tags of the package.
            /// Used for the nuspec creation.
            Tags: string list
            /// Release notes of the package.
            /// Used for the nuspec creation.
            ReleaseNotes: string
            /// Copyright of the package.
            /// Used for the nuspec creation.
            Copyright: string
            /// Output directory for the files (nuspec, <c>chocolateyInstall.ps1</c> and <c>chocolateyUninstall.ps1</c>)
            /// creation.
            OutputDir: string
            /// Dependencies of the package.
            /// Used for the nuspec creation.
            Dependencies: NugetDependencies
            /// Dependencies by framework of the package.
            /// Used for the nuspec creation.
            DependenciesByFramework: NugetFrameworkDependencies list
            /// References of the package.
            /// Used for the nuspec creation.
            References: NugetReferences
            /// References by framework of the package.
            /// Used for the nuspec creation.
            ReferencesByFramework: NugetFrameworkReferences list
            /// Framework assemblies of the package.
            /// Used for the nuspec creation.
            FrameworkAssemblies: NugetFrameworkAssemblyReferences list
            /// Files of the package.
            /// Used for the nuspec creation.
            Files: list<string * string option * string option>
            /// Url to the software.
            /// Used for the nuspec creation.
            ProjectUrl: string
            /// Url to the icon of the package.
            /// Used for the nuspec creation.
            IconUrl: string
            /// Url to the license of the software.
            /// Used for the nuspec creation.
            LicenseUrl: string
            /// True if the software needs license acceptance. Default: false.
            /// Used for the nuspec creation.
            RequireLicenseAcceptance: bool
            /// Url to the chocolatey package repository, not the software (unless they are the same).
            /// Used for the nuspec creation.
            PackageSourceUrl: string
            /// Url pointing to the location of the wiki or docs of the software.
            /// Used for the nuspec creation.
            DocsUrl: string
            /// Url pointing to the forum or email list group for the software.
            /// Used for the nuspec creation.
            MailingListUrl: string
            /// Url pointing to the location where issues and tickets can be accessed.
            /// Used for the nuspec creation.
            BugTrackerUrl: string
            /// Url pointing to the location of the underlying software source.
            /// Used for the nuspec creation.
            ProjectSourceUrl: string
            /// Boolean specifying whether the package will be marked as a
            /// <a href="https://docs.nuget.org/Release-Notes/NuGet-2.7#development-only-dependencies">
            /// development-only dependency</a>. Default: false.
            /// Used for the nuspec creation.
            DevelopmentDependency: bool
            /// Url pointing to the installer (exe, msi, zip) of the package.
            /// Used to create chocolateyInstall.ps1 if it doesn't exists.
            PackageDownloadUrl: string
            /// Url pointing to the installer (exe, msi, zip) of the 64 bits version of the package.
            /// Used to create chocolateyInstall.ps1 if it doesn't exists.
            PackageDownload64Url: string
            /// Silent args for the installer.
            /// Used to create <c>chocolateyInstall.ps1</c> and/or <c>chocolateyUninstall.ps1</c> if it doesn't exists.
            SilentArgs: string
            /// Unzip location for zip package. Default: Chocolatey install folder.
            /// Used to create <c>chocolateyInstall.ps1</c> if it doesn't exists.
            UnzipLocation: string
            /// Installer type. Default: Zip.
            /// Used to create <c>chocolateyInstall.ps1</c> and/or <c>chocolateyUninstall.ps1</c> if it doesn't exists.
            InstallerType: ChocolateyInstallerType
            /// Either:
            /// - For zip: the zip filename originally installed
            /// - For exe or msi: the full path to the native uninstaller to run
            UninstallPath: string
            /// The checksum hash value of the PackageDownloadUrl resource
            /// This allows a checksum to be validated for files that are not local. The checksum type is
            /// covered by ChecksumType. Equivalent to the <c>--checksum <string></c> option of
            /// <c>Install-Chocolatey[Zip]Package</c> functions. Used to create <c>chocolateyInstall.ps1</c>
            /// if it doesn't exists.
            Checksum: string
            /// The checksum hash value of the PackageDownload64Url resource
            /// This allows a checksum to be validated for files that are not local. The checksum type is covered by
            /// ChecksumType64. Equivalent to the <c>--checksum <string></c> option of
            /// <c>Install-Chocolatey[Zip]Package</c> functions. Used to create <c>chocolateyInstall.ps1</c> if
            /// it doesn't exists.
            Checksum64: string
            /// The type of checksum that the file is validated with. Default: Sha256
            /// Used to create <c>chocolateyInstall.ps1</c> if it doesn't exists.
            ChecksumType: ChocolateyChecksumType
            /// The type of checksum that the file is validated with. Default: Sha256
            /// Used to create <c>chocolateyInstall.ps1</c> if it doesn't exists.
            Checksum64Type: ChocolateyChecksumType
        }

    /// <summary>
    /// The choco push parameter type.
    /// </summary>
    type ChocoPushParams =
        {
            /// The source we are pushing the package to. Default: "https://chocolatey.org/"
            /// Equivalent to the <c>--source <source></c> option.
            Source: string
            /// The api key for the source. If not specified (and not local file source), does a lookup.
            /// If not specified and one is not found for an https source, push will fail.
            /// Equivalent to the <c>--apikey <apikey></c> option.
            ApiKey: string
            /// The choco execution timeout.
            Timeout: TimeSpan
            /// The location of the choco executable. Automatically found if null or empty.
            ToolPath: string
            /// A character string containing additional arguments to give to choco.
            AdditionalArgs: string
            /// Do not prompt for user input or confirmations. Default <c>true</c>.
            /// Equivalent to the <c>-y</c> option.
            NonInteractive: bool
            /// Force - force the behavior. Do not use force during normal operation -
            /// it subverts some of the smart behavior for commands. Maybe used for pushing
            /// packages ot insecure private feeds. Default <c>false</c>.
            /// Equivalent to the <c>--force</c> option.
            Force: bool
        }

    type private NuspecData =
        { Version: string
          Title: string
          Authors: string
          Owners: string
          PackageId: string
          Summary: string
          Description: string
          Tags: string
          ReleaseNotes: string
          Copyright: string
          ProjectUrl: string
          IconUrl: string
          LicenseUrl: string
          RequireLicenseAcceptance: string
          PackageSourceUrl: string
          DocsUrl: string
          MailingListUrl: string
          BugTrackerUrl: string
          ProjectSourceUrl: string
          DevelopmentDependency: string
          DependenciesXml: string
          ReferencesXml: string
          FrameworkAssembliesXml: string
          FilesXml: string }

    /// The default option set given to choco install.
    let ChocoInstallDefaults =
        { Timeout = TimeSpan.FromMinutes 5.
          NonInteractive = true
          Prerelease = false
          ForceX86 = false
          OverrideArgs = false
          SkipPowershell = false
          Version = null
          PackageParameters = null
          Source = null
          InstallArgs = null
          User = null
          Password = null
          ToolPath = null
          AdditionalArgs = null }

    /// The default option set given to choco pack.
    let ChocoPackDefaults: ChocoPackParams =
        { Timeout = TimeSpan.FromMinutes 5.
          NonInteractive = true
          Version = null
          ToolPath = null
          AdditionalArgs = null
          Authors = []
          Owners = []
          PackageId = ""
          Title = ""
          Summary = null
          Description = null
          Tags = []
          ReleaseNotes = null
          Copyright = null
          OutputDir = "./Chocolatey"
          Dependencies = []
          DependenciesByFramework = []
          References = []
          ReferencesByFramework = []
          FrameworkAssemblies = []
          Files = []
          ProjectUrl = null
          IconUrl = null
          LicenseUrl = null
          RequireLicenseAcceptance = false
          PackageSourceUrl = null
          DocsUrl = null
          MailingListUrl = null
          BugTrackerUrl = null
          ProjectSourceUrl = null
          PackageDownloadUrl = null
          PackageDownload64Url = null
          SilentArgs = ""
          UnzipLocation = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
          InstallerType = ChocolateyInstallerType.Zip
          UninstallPath = null
          DevelopmentDependency = false
          Checksum = null
          Checksum64 = null
          ChecksumType = ChocolateyChecksumType.Sha256
          Checksum64Type = ChocolateyChecksumType.Sha256 }

    /// The default option set given to choco push.
    let ChocoPushDefaults =
        { Timeout = TimeSpan.FromMinutes 5.
          NonInteractive = true
          // See https://github.com/chocolatey/chocolatey.org/issues/499
          Source = "https://push.chocolatey.org/"
          ApiKey = null
          ToolPath = null
          AdditionalArgs = null
          Force = false }

    let private getPaths =
        let programDataPath = Environment.environVar "ProgramData"

        if programDataPath |> String.isNotNullOrEmpty then
            [ Seq.singleton (programDataPath @@ "chocolatey" @@ "bin")
              Environment.pathDirectories ]
        else
            [ Environment.pathDirectories ]

    /// <summary>
    /// Tries to find the specified choco executable:
    /// 1. In the <c>&lt;ProgramData&gt;\chocolatey\bin</c> directory
    /// 2. In the <c>PATH</c> environment variable.
    /// </summary>
    /// [omit]
    let findExe =
        getPaths
        |> Seq.concat
        |> Seq.map (fun directory -> directory @@ "choco.exe")
        |> Seq.tryFind File.Exists

    /// <summary>
    /// Invokes chocolatey with the specified arguments
    /// </summary>
    ///
    /// <param name="exePath">The location of choco executable. Automatically found if null or empty.</param>
    /// <param name="args">The arguments given to the executable.</param>
    /// <param name="timeout">The choco execution timeout</param>
    let private callChoco exePath args timeout =
        // Try to find the choco executable if not specified by the user.
        let chocoExe =
            if not <| String.isNullOrEmpty exePath then
                exePath
            else
                let found = findExe

                if found <> None then
                    found.Value
                else
                    failwith "Cannot find the choco executable."

        use __ = Trace.traceTask "choco" args

        let processResult =
            CreateProcess.fromRawCommandLine chocoExe args
            |> CreateProcess.withTimeout timeout
            |> Proc.run

        if processResult.ExitCode <> 0 then
            failwithf "choco failed with exit code %i." processResult.ExitCode

        __.MarkSuccess()

    let private getTempFolder =
        // temp folder in current working directory has the advantage of being compatible
        // with chocolatey on docker on mono...
        let tempFolder = DirectoryInfo.ofPath (".fake" @@ "temp" @@ "FakeChocolateyPack")

        if tempFolder.Exists then
            tempFolder.Delete(true)

        tempFolder.Create()

        Directory.create (tempFolder.FullName @@ "tools")

        tempFolder.FullName

    let private appendLine line builder =
        Printf.bprintf builder "%s%s" line Environment.NewLine
        builder

    let private appendFormattedLine format value builder =
        appendLine (sprintf format value) builder

    let private appendFormattedLineIfNotNull format value builder =
        if isNull value then
            builder
        else
            appendFormattedLine format value builder

    let private getNuspecData parameters =
        let getFrameworkGroup (frameworkTags: (string * string) seq) =
            frameworkTags
            |> Seq.map (fun (frameworkVersion, tags) ->
                if String.isNullOrEmpty frameworkVersion then
                    sprintf "<group>%s</group>" tags
                else
                    sprintf "<group targetFramework=\"%s\">%s</group>" frameworkVersion tags)
            |> String.toLines

        let getGroup items toTags =
            if List.isEmpty items then
                null
            else
                sprintf "<group>%s</group>" (items |> toTags)

        let getReferencesTags references =
            references
            |> Seq.map (fun assembly -> sprintf "<reference file=\"%s\" />" assembly)
            |> String.toLines

        let references = getGroup parameters.References getReferencesTags

        let referencesByFramework =
            parameters.ReferencesByFramework
            |> Seq.map (fun x -> (x.FrameworkVersion, getReferencesTags x.References))
            |> getFrameworkGroup

        let referencesXml =
            match (references + referencesByFramework) with
            | null -> null
            | "" -> null
            | r -> sprintf "<references>%s</references>" r

        let getFrameworkAssemblyTags references =
            references
            |> Seq.map (fun x ->
                if List.isEmpty x.FrameworkVersions then
                    sprintf "<frameworkAssembly assemblyName=\"%s\" />" x.AssemblyName
                else
                    sprintf
                        "<frameworkAssembly assemblyName=\"%s\" targetFramework=\"%s\" />"
                        x.AssemblyName
                        (x.FrameworkVersions |> String.separated ", "))
            |> String.toLines

        let frameworkAssembliesXml =
            if List.isEmpty parameters.FrameworkAssemblies then
                null
            else
                sprintf
                    "<frameworkAssemblies>%s</frameworkAssemblies>"
                    (parameters.FrameworkAssemblies |> getFrameworkAssemblyTags)

        let getDependenciesTags dependencies =
            dependencies
            |> Seq.map (fun (package, version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
            |> String.toLines

        let dependencies = getGroup parameters.Dependencies getDependenciesTags

        let dependenciesByFramework =
            parameters.DependenciesByFramework
            |> Seq.map (fun x -> (x.FrameworkVersion, getDependenciesTags x.Dependencies))
            |> getFrameworkGroup

        let dependenciesXml =
            match (dependencies + dependenciesByFramework) with
            | null -> null
            | "" -> null
            | d -> sprintf "<dependencies>%s</dependencies>" d

        let filesTags =
            parameters.Files
            |> Seq.map (fun (source, target, exclude) ->
                let excludeStr =
                    if exclude.IsSome then
                        sprintf " exclude=\"%s\"" exclude.Value
                    else
                        String.Empty

                let targetStr =
                    if target.IsSome then
                        sprintf " target=\"%s\"" target.Value
                    else
                        String.Empty

                sprintf "<file src=\"%s\"%s%s />" source targetStr excludeStr)
            |> String.toLines

        let filesXml =
            match filesTags.Length with
            | 0 -> null
            | _ -> sprintf "<files>%s</files>" filesTags

        let xmlEncode (notEncodedText: string) =
            if String.IsNullOrWhiteSpace notEncodedText then
                null
            else
                XText(notEncodedText).ToString().Replace("�", "&szlig;")

        let toSingleLine (text: string) =
            match text with
            | null -> null
            | _ -> text.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

        { Version = parameters.Version |> xmlEncode
          Title = parameters.Title |> xmlEncode
          Authors = parameters.Authors |> String.separated ", " |> xmlEncode
          Owners = parameters.Owners |> String.separated ", " |> xmlEncode
          PackageId = parameters.PackageId |> xmlEncode
          Summary = parameters.Summary |> toSingleLine |> xmlEncode
          Description = parameters.Description |> toSingleLine |> xmlEncode
          Tags = parameters.Tags |> String.separated " " |> xmlEncode
          ReleaseNotes = parameters.ReleaseNotes |> xmlEncode
          Copyright = parameters.Copyright |> xmlEncode
          ProjectUrl = parameters.ProjectUrl |> xmlEncode
          IconUrl = parameters.IconUrl |> xmlEncode
          LicenseUrl = parameters.LicenseUrl |> xmlEncode
          RequireLicenseAcceptance = parameters.RequireLicenseAcceptance.ToString().ToLower()
          PackageSourceUrl = parameters.PackageSourceUrl |> xmlEncode
          DocsUrl = parameters.DocsUrl |> xmlEncode
          MailingListUrl = parameters.MailingListUrl |> xmlEncode
          BugTrackerUrl = parameters.BugTrackerUrl |> xmlEncode
          ProjectSourceUrl = parameters.ProjectSourceUrl |> xmlEncode
          DevelopmentDependency =
            match parameters.DevelopmentDependency with
            | false -> null
            | true -> "true"
          DependenciesXml = dependenciesXml
          ReferencesXml = referencesXml
          FrameworkAssembliesXml = frameworkAssembliesXml
          FilesXml = filesXml }

    /// <summary>
    /// Create nuspec from template
    /// </summary>
    let private createNuSpecFromTemplate (parameters: ChocoPackParams) (templateNuSpec: FileInfo) outputDir =
        let specFile =
            outputDir
            @@ (templateNuSpec.Name.Replace("nuspec", "") + parameters.Version + ".nuspec")
            |> Path.getFullName

        Trace.tracefn "Creating .nuspec file at %s" specFile

        templateNuSpec.CopyTo(specFile, true) |> ignore

        let nuspecData = getNuspecData parameters

        let replacements =
            [ "@build.number@", nuspecData.Version
              "@title@", nuspecData.Title
              "@authors@", nuspecData.Authors
              "@owners@", nuspecData.Owners
              "@project@", nuspecData.PackageId
              "@summary@", nuspecData.Summary
              "@description@", nuspecData.Description
              "@tags@", nuspecData.Tags
              "@releaseNotes@", nuspecData.ReleaseNotes
              "@copyright@", nuspecData.Copyright
              "@projectUrl@", nuspecData.ProjectUrl
              "@iconUrl@", nuspecData.IconUrl
              "@licenseUrl@", nuspecData.LicenseUrl
              "@requireLicenseAcceptance@", nuspecData.RequireLicenseAcceptance
              "@packageSourceUrl@", parameters.PackageSourceUrl
              "@docsUrl@", nuspecData.DocsUrl
              "@mailingListUrl@", nuspecData.MailingListUrl
              "@bugTrackerUrl@", nuspecData.BugTrackerUrl
              "@projectSourceUrl@", nuspecData.ProjectSourceUrl
              "@developmentDependency@", nuspecData.DevelopmentDependency
              "@dependencies@", nuspecData.DependenciesXml
              "@references@", nuspecData.ReferencesXml
              "@frameworkAssemblies@", nuspecData.FrameworkAssembliesXml
              "@files@", nuspecData.FilesXml ]

        Templates.replaceInFiles replacements [ specFile ]
        Trace.tracefn "Created nuspec file %s" specFile
        specFile

    /// <summary>
    /// Create nuspec from data
    /// </summary>
    let private createNuSpec (parameters: ChocoPackParams) outputDir =
        let specFile =
            outputDir @@ parameters.PackageId + "." + parameters.Version + ".nuspec"
            |> Path.getFullName

        Trace.tracefn "Creating .nuspec file at %s" specFile

        let nuspecData = getNuspecData parameters

        let nuspecContent =
            StringBuilder()
            |> appendLine "<?xml version=\"1.0\"?>"
            |> appendLine "<package xmlns=\"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd\">"
            |> appendLine "  <metadata>"
            |> appendFormattedLine "    <id>%s</id>" nuspecData.PackageId
            |> appendFormattedLine "    <version>%s</version>" nuspecData.Version
            |> appendFormattedLineIfNotNull "    <title>%s</title>" nuspecData.Title
            |> appendFormattedLine "    <authors>%s</authors>" nuspecData.Authors
            |> appendFormattedLineIfNotNull "    <owners>%s</owners>" nuspecData.Owners
            |> appendFormattedLine "    <description>%s</description>" nuspecData.Description
            |> appendFormattedLineIfNotNull "    <releaseNotes>%s</releaseNotes>" nuspecData.ReleaseNotes
            |> appendFormattedLineIfNotNull "    <summary>%s</summary>" nuspecData.Summary
            |> appendFormattedLineIfNotNull "    <projectUrl>%s</projectUrl>" nuspecData.ProjectUrl
            |> appendFormattedLineIfNotNull "    <iconUrl>%s</iconUrl>" nuspecData.IconUrl
            |> appendFormattedLineIfNotNull "    <licenseUrl>%s</licenseUrl>" nuspecData.LicenseUrl
            |> appendFormattedLineIfNotNull "    <copyright>%s</copyright>" nuspecData.Copyright
            |> appendFormattedLineIfNotNull
                "    <requireLicenseAcceptance>%s</requireLicenseAcceptance>"
                nuspecData.RequireLicenseAcceptance
            |> appendFormattedLineIfNotNull "    <tags>%s</tags>" nuspecData.Tags
            |> appendFormattedLineIfNotNull "    <packageSourceUrl>%s</packageSourceUrl>" nuspecData.PackageSourceUrl
            |> appendFormattedLineIfNotNull "    <docsUrl>%s</docsUrl>" nuspecData.DocsUrl
            |> appendFormattedLineIfNotNull "    <mailingListUrl>%s</mailingListUrl>" nuspecData.MailingListUrl
            |> appendFormattedLineIfNotNull "    <bugTrackerUrl>%s</bugTrackerUrl>" nuspecData.BugTrackerUrl
            |> appendFormattedLineIfNotNull "    <projectSourceUrl>%s</projectSourceUrl>" nuspecData.ProjectSourceUrl
            |> appendFormattedLineIfNotNull
                "    <developmentDependency>%s</developmentDependency>"
                nuspecData.DevelopmentDependency
            |> appendFormattedLineIfNotNull "    %s" nuspecData.DependenciesXml
            |> appendFormattedLineIfNotNull "    %s" nuspecData.ReferencesXml
            |> appendFormattedLineIfNotNull "    %s" nuspecData.FrameworkAssembliesXml
            |> appendLine "  </metadata>"
            |> appendFormattedLineIfNotNull "  %s" nuspecData.FilesXml
            |> StringBuilder.appendWithoutQuotes "</package>"
            |> StringBuilder.toText

        File.writeString false specFile nuspecContent
        Trace.tracefn "Created nuspec file %s" specFile
        specFile

    let private installerTypeToString x =
        match x with
        | Zip -> "zip"
        | Exe -> "exe"
        | Msi -> "msi"
        | SelfContained ->
            failwithf
                "this should never be used (this type is from us -> to embedd everything) and not known by the choco helpers."

    let private checksumTypeToString x =
        match x with
        | Md5 -> "md5"
        | Sha1 -> "sha1"
        | Sha256 -> "sha256"
        | Sha512 -> "sha512"

    let private createChocolateyInstallPs1 (parameters: ChocoPackParams) outputDir =
        let outputPath = outputDir @@ "tools" @@ "chocolateyInstall.ps1" |> Path.getFullName
        Trace.tracefn "Create chocolateyInstall.ps1 at %s" outputPath

        if
            String.isNullOrWhiteSpace parameters.Title
            || String.isNullOrWhiteSpace parameters.PackageDownloadUrl
        then
            failwith "chocolateyInstall.ps1 need at least Title and PackageDownloadUrl to be created."

        let installContent =
            StringBuilder()
            |> appendFormattedLine "$packageName = '%s'" parameters.Title
            |> match parameters.InstallerType with
               | ChocolateyInstallerType.Zip -> id
               | _ -> appendFormattedLine "$installerType = '%s'" (installerTypeToString parameters.InstallerType)
            |> appendFormattedLine "$url = '%s'" parameters.PackageDownloadUrl
            |> appendFormattedLineIfNotNull "$url64 = '%s'" parameters.PackageDownload64Url
            |> match parameters.InstallerType with
               | ChocolateyInstallerType.Zip -> id
               | _ -> appendFormattedLine "$silentArgs = '%s'" parameters.SilentArgs
            |> match parameters.InstallerType with
               | ChocolateyInstallerType.Zip -> appendFormattedLine "$unzipLocation = \"%s\"" parameters.UnzipLocation
               | _ -> id
            |> appendLine String.Empty
            |> match parameters.InstallerType with
               | ChocolateyInstallerType.Zip ->
                   StringBuilder.appendWithoutQuotes "Install-ChocolateyZipPackage $packageName $url $unzipLocation"
               | _ ->
                   StringBuilder.appendWithoutQuotes
                       "Install-ChocolateyPackage $packageName $installerType $silentArgs $url"
            |> StringBuilder.appendIfTrue (String.isNotNullOrEmpty parameters.PackageDownload64Url) " $url64"
            |> StringBuilder.appendIfTrueWithoutQuotes
                (String.isNotNullOrEmpty parameters.Checksum)
                ("-Checksum " + parameters.Checksum)
            |> StringBuilder.appendIfTrueWithoutQuotes
                (String.isNotNullOrEmpty parameters.Checksum)
                ("-ChecksumType " + checksumTypeToString parameters.ChecksumType)
            |> StringBuilder.appendIfTrueWithoutQuotes
                (String.isNotNullOrEmpty parameters.Checksum64)
                ("-Checksum64 " + parameters.Checksum64)
            |> StringBuilder.appendIfTrueWithoutQuotes
                (String.isNotNullOrEmpty parameters.Checksum64)
                ("-Checksum64Type " + checksumTypeToString parameters.Checksum64Type)
            |> StringBuilder.toText

        File.writeString false outputPath installContent

        Trace.tracefn "Created chocolateyInstall.ps1 at %s" outputPath

    let private createChocolateyInstallPs1FromTemplate (parameters: ChocoPackParams) templatePath outputDir =
        let outputPath = outputDir @@ "tools" @@ "chocolateyInstall.ps1" |> Path.getFullName
        Trace.tracefn "Create chocolateyInstall.ps1 at %s from template %s" outputPath templatePath

        templatePath |> Shell.copyFile outputPath

        let replacements =
            [ "@packageName@", parameters.Title
              "@url@", parameters.PackageDownloadUrl
              "@url64@", parameters.PackageDownload64Url
              "@silentArgs@", parameters.SilentArgs
              "@unzipLocation@", parameters.UnzipLocation
              "@installerType@", (installerTypeToString parameters.InstallerType)
              "@checksum@", parameters.Checksum
              "@checksumType@", (checksumTypeToString parameters.ChecksumType)
              "@checksum64@", parameters.Checksum64
              "@checksum64Type@", (checksumTypeToString parameters.Checksum64Type) ]

        Templates.replaceInFiles replacements [ outputPath ]

        Trace.tracefn "Created chocolateyInstall.ps1 at %sfrom template %s" outputPath templatePath

    let private createChocolateyUninstallPs1 (parameters: ChocoPackParams) outputDir =
        if
            not (String.isNullOrWhiteSpace parameters.Title)
            && not (String.isNullOrWhiteSpace parameters.UninstallPath)
        then
            let outputPath =
                outputDir @@ "tools" @@ "chocolateyUninstall.ps1" |> Path.getFullName

            Trace.tracefn "Create chocolateyUninstall.ps1 at %s" outputPath

            let uninstallContent =
                StringBuilder()
                |> appendFormattedLine "$packageName = '%s'" parameters.Title
                |> match parameters.InstallerType with
                   | ChocolateyInstallerType.Zip -> id
                   | _ -> appendFormattedLine "$installerType = '%s'" (installerTypeToString parameters.InstallerType)
                |> appendFormattedLine "$file = \"%s\"" parameters.UninstallPath
                |> match parameters.InstallerType with
                   | ChocolateyInstallerType.Zip -> id
                   | _ -> appendFormattedLine "$silentArgs = '%s'" parameters.SilentArgs
                |> appendLine String.Empty
                |> match parameters.InstallerType with
                   | ChocolateyInstallerType.Zip ->
                       StringBuilder.appendWithoutQuotes "Uninstall-ChocolateyZipPackage $packageName $file"
                   | _ ->
                       StringBuilder.appendWithoutQuotes
                           "Uninstall-ChocolateyPackage $packageName $installerType $silentArgs $file"
                |> StringBuilder.toText

            File.writeString false outputPath uninstallContent

            Trace.tracefn "Created chocolateyUninstall.ps1 at %s" outputPath

    let private createChocolateyUninstallPs1FromTemplate (parameters: ChocoPackParams) templatePath outputDir =
        let outputPath =
            outputDir @@ "tools" @@ "chocolateyUninstall.ps1" |> Path.getFullName

        Trace.tracefn "Create chocolateyUninstall.ps1 at %s from template %s" outputPath templatePath

        templatePath |> Shell.copyFile outputPath

        let replacements =
            [ "@packageName@", parameters.Title
              "@silentArgs@", parameters.SilentArgs
              "@installerType@", (installerTypeToString parameters.InstallerType)
              "@uninstallPath@", parameters.UninstallPath ]

        Templates.replaceInFiles replacements [ outputPath ]

        Trace.tracefn "Created chocolateyUninstall.ps1 at %sfrom template %s" outputPath templatePath

    let private callChocoPack nuspecFile (parameters: ChocoPackParams) =
        let args =
            StringBuilder()
            |> StringBuilder.appendWithoutQuotes "pack"
            |> StringBuilder.append nuspecFile
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Version "--version "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.AdditionalArgs parameters.AdditionalArgs
            |> StringBuilder.toText

        callChoco parameters.ToolPath args parameters.Timeout

    /// <summary>
    /// True if choco is available (only on windows)
    /// </summary>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// "Build" =?> ("ChocoInstall", Choco.IsAvailable)
    /// </code>
    /// </example>
    let IsAvailable = not Environment.isUnix && findExe <> None

    /// <summary>
    /// Call choco to <a href="https://docs.chocolatey.org/en-us/choco/commands/install">install</a> a package
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default choco parameters.
    /// See <c>ChocoInstallParams</c></param>
    /// <param name="packages">Names of packages, path to packages.config, .nuspec or .nupkg to install</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "ChocoInstall" (fun _ ->
    ///         "pretzel" |> Choco.Install (fun p -> { p with Version = "0.4.0" })
    ///     )
    /// </code>
    /// </example>
    let install (setParams: ChocoInstallParams -> ChocoInstallParams) (packages: string) =
        if packages |> String.isNullOrEmpty then
            failwith "'packages' must not be empty."

        let parameters = setParams ChocoInstallDefaults

        let args =
            StringBuilder()
            |> StringBuilder.appendWithoutQuotes "install"
            |> StringBuilder.append packages
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Version "--version "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.Prerelease "--pre"
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.PackageParameters "--params "
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Source "--source "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.ForceX86 "--forcex86"
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.InstallArgs "--installargs "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.OverrideArgs "--overrideargs"
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.SkipPowershell "--skippowershell"
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.User "--user "
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Password "--password "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
            |> StringBuilder.appendIfNotNullOrEmpty parameters.AdditionalArgs ""
            |> StringBuilder.toText

        callChoco parameters.ToolPath args parameters.Timeout

    /// <summary>
    /// Call choco to <a href="https://docs.chocolatey.org/en-us/create/commands/pack">pack</a> a package and
    /// create <c>.nuspec</c>, <c>chocolateyInstall.ps1</c> and <c>chocolateyUninstall.ps1</c> if information are specified
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default choco parameters.
    /// See <c>ChocoPackParams</c></param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "ChocoPack" (fun _ ->
    ///         Choco.Pack (fun p -> { p with Version = "0.5.0"; ... })
    ///     )
    /// </code>
    /// </example>
    let pack setParams =

        let parameters = setParams ChocoPackDefaults

        if Directory.Exists parameters.OutputDir |> not then
            failwithf "OutputDir %s does not exist." parameters.OutputDir

        let tempFolder = getTempFolder

        let nuspecFile = createNuSpec parameters tempFolder

        if parameters.InstallerType <> ChocolateyInstallerType.SelfContained then
            createChocolateyInstallPs1 parameters tempFolder
            createChocolateyUninstallPs1 parameters tempFolder

        callChocoPack nuspecFile parameters

        parameters.PackageId + "." + parameters.Version + ".nupkg"
        |> Shell.moveFile parameters.OutputDir

    /// <summary>
    /// Call choco to <a href="https://docs.chocolatey.org/en-us/create/commands/pack">pack</a> a package
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default choco parameters.
    /// See <c>ChocoPackParams</c></param>
    /// <param name="nuspecPath">path to the .nuspec to pack</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "ChocoPack" (fun _ ->
    ///         "pretzel.nuspec" |> Choco.Pack (fun p -> { p with Version = "0.5.0" })
    ///     )
    /// </code>
    /// </example>
    let packFromTemplate setParams nuspecPath =

        if nuspecPath |> String.isNullOrEmpty then
            failwith "'nuspecPath' must not be empty."

        let parameters = setParams ChocoPackDefaults

        if Directory.Exists parameters.OutputDir |> not then
            failwithf "OutputDir %s does not exist." parameters.OutputDir

        let tempFolder = getTempFolder

        let nuspecFile =
            createNuSpecFromTemplate parameters (FileInfo.ofPath nuspecPath) tempFolder

        let rootFolder = (Directory.GetParent nuspecPath).FullName

        let chocoInstallPath = rootFolder @@ "tools" @@ "chocolateyInstall.ps1"

        if File.Exists chocoInstallPath then
            createChocolateyInstallPs1FromTemplate parameters chocoInstallPath tempFolder
        elif parameters.InstallerType <> ChocolateyInstallerType.SelfContained then
            createChocolateyInstallPs1 parameters tempFolder

        let chocoUninstallPath = rootFolder @@ "tools" @@ "chocolateyUninstall.ps1"

        if File.Exists chocoUninstallPath then
            createChocolateyUninstallPs1FromTemplate parameters chocoUninstallPath tempFolder
        elif parameters.InstallerType <> ChocolateyInstallerType.SelfContained then
            createChocolateyUninstallPs1 parameters tempFolder

        callChocoPack nuspecFile parameters

        parameters.PackageId + "." + parameters.Version + ".nupkg"
        |> Shell.moveFile parameters.OutputDir

    /// <summary>
    /// Call choco to <a href="https://docs.chocolatey.org/en-us/create/commands/push">push</a> a package
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default choco parameters.
    /// See <c>ChocoPushParams</c></param>
    /// <param name="nupkgPath">path to the <c>.nupkg</c> to push</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "ChocoPush" (fun _ ->
    ///         "pretzel.0.5.0.nupkg" |> Choco.Push (fun p -> { p with ApiKey = "123-123123-123" })
    ///     )
    /// </code>
    /// </example>
    let push setParams nupkgPath =
        if nupkgPath |> String.isNullOrEmpty then
            failwith "'nupkgPath' must not be empty."

        let parameters = setParams ChocoPushDefaults

        let args =
            StringBuilder()
            |> StringBuilder.appendWithoutQuotes "push"
            |> StringBuilder.append nupkgPath
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.Source "--source "
            |> StringBuilder.appendWithoutQuotesIfNotNull parameters.ApiKey "--apikey "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.Force "--force"
            |> StringBuilder.appendIfTrueWithoutQuotes
                (parameters.AdditionalArgs |> String.isNotNullOrEmpty)
                parameters.AdditionalArgs
            |> StringBuilder.toText

        let rec tries n =
            try
                callChoco parameters.ToolPath args parameters.Timeout
            with e when n > 1 ->
                eprintf "pushing to chocolatey server failed, trying again: %O" e
                tries (n - 1)

        tries 3

    /// <summary>
    /// Call custom choco command
    /// </summary>
    ///
    /// <param name="args">string that will be appended to choco.exe call</param>
    /// <param name="timeout">parent process maximum completion time</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target "ChocoPush" (fun _ ->
    ///
    ///          let newSpecFile = ...
    ///          let args =
    ///                 new StringBuilder()
    ///                 |> append "pack"
    ///                 |> append newSpecFile
    ///                 |> append "-y"
    ///                 |> toText
    ///
    ///         args |> Choco.CallChoco TimeSpan.FromMinutes 1.
    ///     )
    /// </code>
    /// </example>
    let exec args timeout =
        if args |> String.isNullOrEmpty then
            failwith "'args' must not be empty."

        callChoco null args timeout
