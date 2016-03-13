namespace Fake


/// Contains tasks which allow to call [Chocolatey](https://chocolatey.org)
module Choco =
    open System
    open System.Diagnostics
    open System.Text;
    open System.IO
    open System.Xml.Linq

    type ChocolateyInstallerType = 
    | Zip
    | Exe
    | Msi

    /// The choco install parameter type.
    type ChocoInstallParams = {
        /// Version of the package
        /// Equivalent to the `--version <version>` option.
        Version: string
        /// Include prerelease. Default `false`.
        /// Equivalent to the `--pre` option.
        Prerelease: bool
        /// Parameters to pass to the package.
        /// Equivalent to the `--params <params>` option.
        PackageParameters: string
        /// The source to find the package(s) to install.
        // Special sources  include: ruby, webpi, cygwin, windowsfeatures, and python.
        /// Equivalent to the `--source <source>` option.
        Source: string
        /// Force x86 (32bit) installation on 64 bit systems. Default `false`.
        /// Equivalent to the `--forcex86` option.
        ForceX86: bool
        /// Install Arguments to pass to the native installer in the package.
        /// Equivalent to the `--installargs <args>` option.
        InstallArgs: string
        /// Should install arguments be used exclusively without appending to current package passed arguments? Default `false`.
        /// Equivalent to the `--overrideargs` option.
        OverrideArgs: bool
        /// Skip Powershell - Do not run chocolateyInstall.ps1. Default `false`.
        /// Equivalent to the `--skippowershell` option.
        SkipPowershell: bool
        /// User - used with authenticated feeds.
        /// Equivalent to the `--user <user>` option.
        User: string
        /// Password - the user's password to the source.
        /// Equivalent to the `--password <password>` option.
        Password: string
        /// The choco execution timeout.
        Timeout:TimeSpan
        /// The location of the choco executable. Automatically found if null or empty.
        ToolPath: string
        /// A character string containing additional arguments to give to choco.
        AdditionalArgs: string
        /// Do not prompt for user input or confirmations. Default `true`.
        /// Equivalent to the `-y` option.
        NonInteractive: bool
    }

    /// The choco pack parameter type.
    type ChocoPackParams = {
        /// The version you would like to insert into the package.
        /// Equivalent to the `--version <version>` option.
        Version: string
        /// The choco execution timeout.
        Timeout:TimeSpan
        /// The location of the choco executable. Automatically found if null or empty.
        ToolPath: string
        /// A character string containing additional arguments to give to choco.
        AdditionalArgs: string
        /// Do not prompt for user input or confirmations. Default `true`.
        /// Equivalent to the `-y` option.
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
        /// Used for the nuspec, chocolateyInstall.ps1 and chocolateyUninstall.ps1 creation.
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
        /// Output directory for the files (nuspec, chocolateyInstall.ps1 and chocolateyUninstall.ps1) creation.
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
        Files: list<string*string option*string option>
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
        /// Boolean specifying whether the package will be marked as a [development-only dependency](https://docs.nuget.org/Release-Notes/NuGet-2.7#development-only-dependencies). Default: false.
        /// Used for the nuspec creation.
        DevelopmentDependency: bool
        /// Url pointing to the installer (exe, msi, zip) of the package.
        /// Used to create chocolateyInstall.ps1 if it doesn't exists.
        PackageDownloadUrl: string
        /// Url pointing to the installer (exe, msi, zip) of the 64 bits version of the package.
        /// Used to create chocolateyInstall.ps1 if it doesn't exists.
        PackageDownload64Url: string
        /// Silent args for the installer.
        /// Used to create chocolateyInstall.ps1 and/or chocolateyUninstall.ps1 if it doesn't exists.
        SilentArgs: string
        /// Unzip location for zip package. Default: Chocolatey install folder.
        /// Used to create chocolateyInstall.ps1 if it doesn't exists.
        UnzipLocation: string
        /// Installer type. Default: Zip.
        /// Used to create chocolateyInstall.ps1 and/or chocolateyUninstall.ps1 if it doesn't exists.
        InstallerType: ChocolateyInstallerType
        /// Either:
        ///
        /// - For zip: the zip filename originally installed
        /// - For exe or msi: the full path to the native uninstaller to run
        UninstallPath: string
    }

    /// The choco push parameter type.
    type ChocoPushParams = {
        /// The source we are pushing the package to. Default: "https://chocolatey.org/"
        /// Equivalent to the `--source <source>` option.
        Source: string
        /// The api key for the source. If not specified (and not local file source), does a lookup. 
        /// If not specified and one is not found for an https source, push will fail.
        /// Equivalent to the `--apikey <apikey>` option.
        ApiKey: string
        /// The choco execution timeout.
        Timeout:TimeSpan
        /// The location of the choco executable. Automatically found if null or empty.
        ToolPath: string
        /// A character string containing additional arguments to give to choco.
        AdditionalArgs: string
        /// Do not prompt for user input or confirmations. Default `true`.
        /// Equivalent to the `-y` option.
        NonInteractive: bool
    }

     type private NuspecData = {
         Version: string
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
         FilesXml: string
    }

    /// The default option set given to choco install.
    let ChocoInstallDefaults = {
        Timeout = TimeSpan.FromMinutes 5.
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
        AdditionalArgs = null
    }

    /// The default option set given to choco pack.
    let ChocoPackDefaults: ChocoPackParams = {
        Timeout = TimeSpan.FromMinutes 5.
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
    }
    
    /// The default option set given to choco pack.
    let ChocoPushDefaults = {
        Timeout = TimeSpan.FromMinutes 5.
        NonInteractive = true
        Source = "https://chocolatey.org/"
        ApiKey = null
        ToolPath = null
        AdditionalArgs = null
    }
    
    let private getPaths =
        let programDataPath = environVar "ProgramData"
        if programDataPath |> isNotNullOrEmpty
        then 
            [
                Seq.singleton (programDataPath @@ "chocolatey" @@ "bin")
                pathDirectories
            ]
        else
            [
                pathDirectories
            ]

    /// [omit]
    /// Tries to find the specified choco executable:
    ///
    /// 1. In the `<ProgramData>\chocolatey\bin` directory
    /// 2. In the `PATH` environment variable.
    let FindExe =
        getPaths |> Seq.concat
            |> Seq.map (fun directory -> directory @@ "choco.exe")
            |> Seq.tryFind fileExists

    /// [omit]
    /// Invokes chocolatey with the specified arguments
    /// ## Parameters
    ///  - `exePath` - The location of choco executable. Automatically found if null or empty.
    ///  - `args` - The arguments given to the executable.
    ///  - `timeout` - The choco execution timeout
    let private callChoco exePath args timeout =
        // Try to find the choco executable if not specified by the user.
        let chocoExe =
            if not <| isNullOrEmpty exePath then exePath else
            let found = FindExe
            if found <> None then found.Value else failwith "Cannot find the choco executable."

        traceStartTask "choco" args
        let setInfo (info:ProcessStartInfo) =
            info.FileName <- chocoExe
            info.Arguments <- args
        let result = ExecProcess (setInfo) timeout
        if result <> 0 then failwithf "choco failed with exit code %i." result
        traceEndTask "choco" args
        
    let private getTempFolder =
        let tempFolder = directoryInfo (Path.GetTempPath() @@ "FakeChocolateyPack")
        
        if tempFolder.Exists
        then tempFolder.Delete(true)

        tempFolder.Create()
        
        CreateDir (tempFolder.FullName @@ "tools")
            
        tempFolder.FullName
            

    let private appendLine line builder =
        Printf.bprintf builder "%s%s" line Environment.NewLine
        builder

    let private appendFormattedLine format value builder =
        appendLine (sprintf format value) builder

    let private appendFormattedLineIfNotNull format value builder =
        if isNull value then builder
        else appendFormattedLine format value builder
        
    let private getNuspecData parameters =
        
        let getFrameworkGroup (frameworkTags : (string * string) seq) =
            frameworkTags
            |> Seq.map (fun (frameworkVersion, tags) ->
                        if isNullOrEmpty frameworkVersion then sprintf "<group>%s</group>" tags
                        else sprintf "<group targetFramework=\"%s\">%s</group>" frameworkVersion tags)
            |> toLines

        let getGroup items toTags =
            if List.isEmpty items then null
            else sprintf "<group>%s</group>" (items |> toTags)

        let getReferencesTags references = 
            references
            |> Seq.map (fun assembly -> sprintf "<reference file=\"%s\" />" assembly)
            |> toLines
    
        let references = getGroup parameters.References getReferencesTags
    
        let referencesByFramework = 
            parameters.ReferencesByFramework
            |> Seq.map (fun x -> (x.FrameworkVersion, getReferencesTags x.References))
            |> getFrameworkGroup

        let referencesXml = match (references + referencesByFramework) with | null -> null | "" -> null | r -> sprintf "<references>%s</references>" r
    
        let getFrameworkAssemblyTags references =
            references
            |> Seq.map (fun x ->
                        if List.isEmpty x.FrameworkVersions then sprintf "<frameworkAssembly assemblyName=\"%s\" />" x.AssemblyName
                        else sprintf "<frameworkAssembly assemblyName=\"%s\" targetFramework=\"%s\" />" x.AssemblyName (x.FrameworkVersions |> separated ", "))
            |> toLines

        let frameworkAssembliesXml =
            if List.isEmpty parameters.FrameworkAssemblies then null
            else sprintf "<frameworkAssemblies>%s</frameworkAssemblies>" (parameters.FrameworkAssemblies |> getFrameworkAssemblyTags)

        let getDependenciesTags dependencies = 
            dependencies
            |> Seq.map (fun (package, version) -> sprintf "<dependency id=\"%s\" version=\"%s\" />" package version)
            |> toLines
    
        let dependencies = getGroup parameters.Dependencies getDependenciesTags
    
        let dependenciesByFramework = 
            parameters.DependenciesByFramework
            |> Seq.map (fun x -> (x.FrameworkVersion, getDependenciesTags x.Dependencies))
            |> getFrameworkGroup
    
        let dependenciesXml = match (dependencies + dependenciesByFramework) with | null -> null | "" -> null | d -> sprintf "<dependencies>%s</dependencies>" d
    
        let filesTags =
            parameters.Files
            |> Seq.map (fun (source, target, exclude) -> 
                let excludeStr = 
                    if exclude.IsSome then sprintf " exclude=\"%s\"" exclude.Value
                    else String.Empty
                let targetStr = 
                    if target.IsSome then sprintf " target=\"%s\"" target.Value
                    else String.Empty
                sprintf "<file src=\"%s\"%s%s />" source targetStr excludeStr)
            |> toLines

        let filesXml = match filesTags.Length with | 0 -> null | _ -> sprintf "<files>%s</files>" filesTags
    
        let xmlEncode (notEncodedText : string) = 
            if String.IsNullOrWhiteSpace notEncodedText then null
            else XText(notEncodedText).ToString().Replace("ß","&szlig;")

        let toSingleLine (text:string) =
            match text with
            | null -> null 
            | _ -> text.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
        
        { Version = parameters.Version |> xmlEncode
          Title = parameters.Title |> xmlEncode
          Authors = parameters.Authors |> separated ", " |> xmlEncode
          Owners = parameters.Owners |> separated ", " |> xmlEncode
          PackageId = parameters.PackageId |> xmlEncode
          Summary = parameters.Summary |> toSingleLine |> xmlEncode
          Description = parameters.Description |> toSingleLine |> xmlEncode
          Tags = parameters.Tags |> separated " " |> xmlEncode
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
          DevelopmentDependency = match parameters.DevelopmentDependency with | false -> null | true -> "true"
          DependenciesXml = dependenciesXml
          ReferencesXml = referencesXml
          FrameworkAssembliesXml = frameworkAssembliesXml
          FilesXml = filesXml }

    /// [omit]
    /// Create nuspec from template
    let private createNuSpecFromTemplate (parameters:ChocoPackParams) (templateNuSpec:FileInfo) outputDir =
        let specFile = outputDir @@ (templateNuSpec.Name.Replace("nuspec", "") + parameters.Version + ".nuspec")
                        |> FullName
        tracefn "Creating .nuspec file at %s" specFile

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
    
        processTemplates replacements [ specFile ]
        tracefn "Created nuspec file %s" specFile
        specFile
        
    /// [omit]
    /// Create nuspec from data
    let private createNuSpec (parameters:ChocoPackParams) outputDir =
        let specFile = outputDir @@ parameters.PackageId + "." + parameters.Version + ".nuspec"
                        |> FullName
        tracefn "Creating .nuspec file at %s" specFile

        let nuspecData = getNuspecData parameters
        
        let nuspecContent = new StringBuilder()
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
                            |> appendFormattedLineIfNotNull "    <requireLicenseAcceptance>%s</requireLicenseAcceptance>" nuspecData.RequireLicenseAcceptance
                            |> appendFormattedLineIfNotNull "    <tags>%s</tags>" nuspecData.Tags
                            |> appendFormattedLineIfNotNull "    <packageSourceUrl>%s</packageSourceUrl>" nuspecData.PackageSourceUrl
                            |> appendFormattedLineIfNotNull "    <docsUrl>%s</docsUrl>" nuspecData.DocsUrl
                            |> appendFormattedLineIfNotNull "    <mailingListUrl>%s</mailingListUrl>" nuspecData.MailingListUrl
                            |> appendFormattedLineIfNotNull "    <bugTrackerUrl>%s</bugTrackerUrl>" nuspecData.BugTrackerUrl
                            |> appendFormattedLineIfNotNull "    <projectSourceUrl>%s</projectSourceUrl>" nuspecData.ProjectSourceUrl
                            |> appendFormattedLineIfNotNull "    <developmentDependency>%s</developmentDependency>" nuspecData.DevelopmentDependency
                            |> appendFormattedLineIfNotNull "    %s" nuspecData.DependenciesXml
                            |> appendFormattedLineIfNotNull "    %s" nuspecData.ReferencesXml
                            |> appendFormattedLineIfNotNull "    %s" nuspecData.FrameworkAssembliesXml
                            |> appendLine "  </metadata>"
                            |> appendFormattedLineIfNotNull "  %s" nuspecData.FilesXml
                            |> appendWithoutQuotes "</package>"
                            |> toText
        
        WriteStringToFile false specFile nuspecContent 
        tracefn "Created nuspec file %s" specFile
        specFile

    let private installerTypeToString x = 
        match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<ChocolateyInstallerType>) with
        | case, _ -> case.Name.ToLower()

    let private createChocolateyInstallPs1 (parameters:ChocoPackParams) outputDir =
        let outputPath = outputDir @@ "tools" @@ "chocolateyInstall.ps1" |> FullName
        tracefn "Create chocolateyInstall.ps1 at %s" outputPath

        if isNullOrWhiteSpace parameters.Title || isNullOrWhiteSpace parameters.PackageDownloadUrl
        then failwith "chocolateyInstall.ps1 need at least Title and PackageDownloadUrl to be created."
        
        let installContent = new StringBuilder()
                            |> appendFormattedLine "$packageName = '%s'" parameters.Title
                            |> match parameters.InstallerType with ChocolateyInstallerType.Zip -> id | _ -> appendFormattedLine "$installerType = '%s'" (installerTypeToString parameters.InstallerType)
                            |> appendFormattedLine "$url = '%s'" parameters.PackageDownloadUrl
                            |> appendFormattedLineIfNotNull "$url64 = '%s'" parameters.PackageDownload64Url 
                            |> match parameters.InstallerType with ChocolateyInstallerType.Zip -> id | _ -> appendFormattedLine "$silentArgs = '%s'" parameters.SilentArgs
                            |> match parameters.InstallerType with ChocolateyInstallerType.Zip -> appendFormattedLine "$unzipLocation = \"%s\"" parameters.UnzipLocation | _ -> id
                            |> appendLine String.Empty
                            |> match parameters.InstallerType with
                                | ChocolateyInstallerType.Zip -> appendWithoutQuotes "Install-ChocolateyZipPackage $packageName $url $unzipLocation"
                                | _ -> appendWithoutQuotes "Install-ChocolateyPackage $packageName $installerType $silentArgs $url"
                            |> appendIfTrue (isNotNullOrEmpty parameters.PackageDownload64Url) " $url64"
                            |> toText

        WriteStringToFile false outputPath installContent 
    
        tracefn "Created chocolateyInstall.ps1 at %s" outputPath

        
    let private createChocolateyInstallPs1FromTemplate (parameters:ChocoPackParams) templatePath outputDir =
        let outputPath = outputDir @@ "tools" @@ "chocolateyInstall.ps1" |> FullName
        tracefn "Create chocolateyInstall.ps1 at %s from template %s" outputPath templatePath

        templatePath |> CopyFile outputPath

        let replacements = 
            [ "@packageName@", parameters.Title
              "@url@", parameters.PackageDownloadUrl
              "@url64@", parameters.PackageDownload64Url
              "@silentArgs@", parameters.SilentArgs
              "@unzipLocation@", parameters.UnzipLocation
              "@installerType@", (installerTypeToString parameters.InstallerType)
            ]
    
        processTemplates replacements [ outputPath ]
    
        tracefn "Created chocolateyInstall.ps1 at %sfrom template %s" outputPath templatePath
        
    let private createChocolateyUninstallPs1 (parameters: ChocoPackParams) outputDir =
        
        if not (isNullOrWhiteSpace parameters.Title) && not (isNullOrWhiteSpace parameters.UninstallPath)
        then 
            let outputPath = outputDir @@ "tools" @@ "chocolateyUninstall.ps1" |> FullName
            tracefn "Create chocolateyUninstall.ps1 at %s" outputPath

            let uninstallContent = new StringBuilder()
                                |> appendFormattedLine "$packageName = '%s'" parameters.Title
                                |> match parameters.InstallerType with ChocolateyInstallerType.Zip -> id | _ -> appendFormattedLine "$installerType = '%s'" (installerTypeToString parameters.InstallerType)
                                |> appendFormattedLine "$file = \"%s\"" parameters.UninstallPath
                                |> match parameters.InstallerType with ChocolateyInstallerType.Zip -> id | _ -> appendFormattedLine "$silentArgs = '%s'" parameters.SilentArgs
                                |> appendLine String.Empty
                                |> match parameters.InstallerType with
                                    | ChocolateyInstallerType.Zip -> appendWithoutQuotes "Uninstall-ChocolateyZipPackage $packageName $file"
                                    | _ -> appendWithoutQuotes "Uninstall-ChocolateyPackage $packageName $installerType $silentArgs $file"
                                |> toText

            WriteStringToFile false outputPath uninstallContent 
    
            tracefn "Created chocolateyUninstall.ps1 at %s" outputPath

    let private createChocolateyUninstallPs1FromTemplate (parameters:ChocoPackParams) templatePath outputDir =
        let outputPath = outputDir @@ "tools" @@ "chocolateyUninstall.ps1" |> FullName
        tracefn "Create chocolateyUninstall.ps1 at %s from template %s" outputPath templatePath

        templatePath |> CopyFile outputPath

        let replacements = 
            [ "@packageName@", parameters.Title
              "@silentArgs@", parameters.SilentArgs
              "@installerType@", (installerTypeToString parameters.InstallerType)
              "@uninstallPath@", parameters.UninstallPath
            ]
    
        processTemplates replacements [ outputPath ]
    
        tracefn "Created chocolateyUninstall.ps1 at %sfrom template %s" outputPath templatePath
    
    let private callChocoPack nuspecFile (parameters: ChocoPackParams) =
        let args = new StringBuilder()
                |> appendWithoutQuotes "pack"
                |> append nuspecFile
                |> appendWithoutQuotesIfNotNull parameters.Version "--version "
                |> appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
                |> appendWithoutQuotesIfNotNull parameters.AdditionalArgs parameters.AdditionalArgs
                |> toText

        callChoco parameters.ToolPath args parameters.Timeout

    /// True if choco is available (only on windows)
    /// ## Sample usage
    ///     "Build" =?> ("ChocoInstall", Choco.IsAvailable)
    let IsAvailable = not isUnix && FindExe <> None

    /// Call choco to [install](https://github.com/chocolatey/choco/wiki/CommandsInstall) a package
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoInstallParams`
    ///  - `packages` - Names of packages, path to packages.config, .nuspec or .nupkg to install
    /// ## Sample usage
    ///     
    ///     Target "ChocoInstall" (fun _ ->
    ///         "pretzel" |> Choco.Install (fun p -> { p with Version = "0.4.0" })
    ///     )
    let Install (setParams: (ChocoInstallParams -> ChocoInstallParams)) (packages: string) =
        if packages |> isNullOrEmpty then failwith "'packages' must not be empty."

        let parameters = setParams ChocoInstallDefaults

        let args = new StringBuilder()
                |> appendWithoutQuotes "install"
                |> append packages
                |> appendWithoutQuotesIfNotNull parameters.Version "--version "
                |> appendIfTrueWithoutQuotes parameters.Prerelease "--pre"
                |> appendWithoutQuotesIfNotNull parameters.PackageParameters "--params "
                |> appendWithoutQuotesIfNotNull parameters.Source "--source "
                |> appendIfTrueWithoutQuotes parameters.ForceX86 "--forcex86"
                |> appendWithoutQuotesIfNotNull parameters.InstallArgs "--installargs "
                |> appendIfTrueWithoutQuotes parameters.OverrideArgs "--overrideargs"
                |> appendIfTrueWithoutQuotes parameters.SkipPowershell "--skippowershell"
                |> appendWithoutQuotesIfNotNull parameters.User "--user "
                |> appendWithoutQuotesIfNotNull parameters.Password "--password "
                |> appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
                |> appendIfNotNullOrEmpty parameters.AdditionalArgs ""
                |> toText

        callChoco parameters.ToolPath args parameters.Timeout
        
    /// Call choco to [pack](https://github.com/chocolatey/choco/wiki/CommandsPack) a package and create .nuspec, chocolateyInstall.ps1 and chocolateyUninstall.ps1 if informations are specified
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoPackParams`
    /// ## Sample usage
    ///     
    ///     Target "ChocoPack" (fun _ ->
    ///         Choco.Pack (fun p -> { p with Version = "0.5.0"; ... })
    ///     )
    let Pack setParams =
        
        let parameters = setParams ChocoPackDefaults
        
        if Directory.Exists parameters.OutputDir |> not then 
            failwithf "OutputDir %s does not exist." parameters.OutputDir

        let tempFolder =  getTempFolder

        let nuspecFile = createNuSpec parameters tempFolder

        createChocolateyInstallPs1 parameters tempFolder
        
        createChocolateyUninstallPs1 parameters tempFolder

        callChocoPack nuspecFile parameters

        parameters.PackageId + "." + parameters.Version + ".nupkg" |> FileHelper.MoveFile parameters.OutputDir
        
    /// Call choco to [pack](https://github.com/chocolatey/choco/wiki/CommandsPack) a package
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoPackParams`
    ///  - `nuspecPath` - path to the .nuspec to pack
    /// ## Sample usage
    ///     
    ///     Target "ChocoPack" (fun _ ->
    ///         "pretzel.nuspec" |> Choco.Pack (fun p -> { p with Version = "0.5.0" })
    ///     )
    let PackFromTemplate setParams nuspecPath =
        
        if nuspecPath |> isNullOrEmpty then failwith "'nuspecPath' must not be empty."
        
        let parameters = setParams ChocoPackDefaults
        
        if Directory.Exists parameters.OutputDir |> not then 
            failwithf "OutputDir %s does not exist." parameters.OutputDir

        let tempFolder =  getTempFolder

        let nuspecFile = createNuSpecFromTemplate parameters (fileInfo nuspecPath) tempFolder

        let rootFolder = (Directory.GetParent nuspecPath).FullName
        
        let chocoInstallPath = rootFolder @@ "tools" @@ "chocolateyInstall.ps1"
        if fileExists chocoInstallPath
        then createChocolateyInstallPs1FromTemplate parameters chocoInstallPath tempFolder
        else createChocolateyInstallPs1 parameters tempFolder
        
        let chocoUninstallPath = rootFolder @@ "tools" @@ "chocolateyUninstall.ps1"
        if fileExists chocoUninstallPath
        then createChocolateyUninstallPs1FromTemplate parameters chocoUninstallPath tempFolder
        else createChocolateyUninstallPs1 parameters tempFolder

        callChocoPack nuspecFile parameters
        
        parameters.PackageId + "." + parameters.Version + ".nupkg" |> FileHelper.MoveFile parameters.OutputDir
        
    /// Call choco to [push](https://github.com/chocolatey/choco/wiki/CommandsPush) a package
    /// ## Parameters
    ///  - `setParams` - Function used to manipulate the default choco parameters. See `ChocoPushParams`
    ///  - `nupkgPath` - path to the .nupkg to push
    /// ## Sample usage
    ///     
    ///     Target "ChocoPush" (fun _ ->
    ///         "pretzel.0.5.0.nupkg" |> Choco.Push (fun p -> { p with ApiKey = "123-123123-123" })
    ///     )
    let Push setParams nupkgPath =
        if nupkgPath |> isNullOrEmpty then failwith "'nupkgPath' must not be empty."

        let parameters = setParams ChocoPushDefaults

        let args = new StringBuilder()
                |> appendWithoutQuotes "push"
                |> append nupkgPath
                |> appendWithoutQuotesIfNotNull parameters.Source "--source "
                |> appendWithoutQuotesIfNotNull parameters.ApiKey "--apikey "
                |> appendIfTrueWithoutQuotes parameters.NonInteractive "-y"
                |> appendWithoutQuotesIfNotNull parameters.AdditionalArgs parameters.AdditionalArgs
                |> toText

        callChoco parameters.ToolPath args parameters.Timeout
