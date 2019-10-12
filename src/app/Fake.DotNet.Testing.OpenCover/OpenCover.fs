/// Contains a task which can be used to run [OpenCover](https://github.com/sawilde/opencover) on .NET assemblies.
module Fake.DotNet.Testing.OpenCover

    open System
    open System.IO
    open System.Text
    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

    type RegisterType = 
        | Manual
        | Register
        | RegisterUser
        | Path32
        | Path64

    type HideSkippedType =
        | All
        | File
        | Filter
        | Attribute
        | MissingPdb
 
    type ReturnTargetCodeType = No | Yes | Offset of int

    /// OpenCover parameters, for more details see: https://github.com/OpenCover/opencover/wiki/Usage#console-application-usage.
    type OpenCoverParams = 
        { /// (Required) Path to the OpenCover console application
          ExePath : string
          /// (Required) Path to the NUnit/XUnit console runner
          TestRunnerExePath : string
          /// The location and name of the output xml file. 
          /// If no value is supplied then the current directory 
          /// will be used and the output filename will be results.xml.
          Output : string
          /// Use this to register and de-register the code coverage profiler.
          Register : RegisterType
          /// A list of filters to apply to selectively include or exclude assemblies and classes from coverage results.
          Filter : string
          /// The timeout for the OpenCover process.
          TimeOut : TimeSpan
          /// The directory where the OpenCover process will be started.
          WorkingDir : string 
          /// This option is used to merge the coverage results for an assembly regardless of where it was loaded 
          /// assuming the assembly has the same file-hash in each location. 
          MergeByHash : bool
          /// Exclude a class or method by filter(s) that match attributes that have been applied. An * can be used as a wildcard.
          ExcludeByAttribute: string list
          /// Exclude a class (or methods) by filter(s) that match the filenames. An * can be used as a wildcard.
          ExcludeByFile : string list
          /// Assemblies being loaded from these locations will be ignored.
          ExcludeDirs : string list
          /// Remove information from output file that relates to classes/modules that have been skipped (filtered) due to the use of the parameters ExcludeBy*, Filter or where the PDB is missing.
          HideSkipped : HideSkippedType list
          /// Allow to merge the results with an existing file (specified by Output parameter). So the coverage from the output file will be loaded first (if exists).
          MergeOutput : bool
          /// Return the target process return code instead of the OpenCover console return code. Use the offset to return the OpenCover console at a value outside the range returned by the target process.
          ReturnTargetCode : ReturnTargetCodeType
          /// Alternative locations to look for PDBs.
          SearchDirs : string list
          /// Neither track nor record auto-implemented properties.
          /// That is, skip getters and setters like these: public bool Service { get; set; }
          SkipAutoProps : bool
          /// This options is used to add additional optional arguments, could be somthing like "-returntargetcode "
          OptionalArguments : string }

    /// OpenCover default parameters
    let OpenCoverDefaults = 
        { ExePath = if Environment.isMono then String.Empty else Environment.environVar "LOCALAPPDATA" @@ "Apps" @@ "OpenCover" @@ "OpenCover.Console.exe"
          TestRunnerExePath = if Environment.isMono then String.Empty else Environment.ProgramFiles @@ "NUnit" @@ "bin" @@ "nunit-console.exe"
          Output = String.Empty
          Register = Manual
          Filter = String.Empty
          TimeOut = TimeSpan.FromMinutes 5.
          WorkingDir = Directory.GetCurrentDirectory()
          MergeByHash = false
          ExcludeByAttribute = []
          ExcludeByFile = []
          ExcludeDirs = []
          HideSkipped = []
          MergeOutput = false
          ReturnTargetCode = No
          SearchDirs = []
          SkipAutoProps = false
          OptionalArguments = String.Empty }

    /// Builds the command line arguments from the given parameter record
    /// [omit]
    let private buildOpenCoverArgs param targetArgs = 
            let quote arg = sprintf "\"%s\"" arg
            let printParam paramName = sprintf "-%s" paramName
            let printParamWithValue paramName paramValue = sprintf "-%s:%s" paramName paramValue
            let mergeListAsValues paramList valueModification = paramList |> List.fold (fun acc x -> acc + (match acc with ""-> "" | _ -> ";") + valueModification x) ""
            let printParamListAsValuesWithModification paramName paramList valueModification = printParamWithValue paramName (mergeListAsValues paramList valueModification)
            let printParamListAsValuesWithQuote paramName paramList = printParamWithValue paramName (quote (mergeListAsValues paramList (fun v -> v)))
            let printParamListAsValues paramName paramList = printParamListAsValuesWithModification paramName paramList (fun v -> string v)

            new StringBuilder()
            |> StringBuilder.appendWithoutQuotes (printParamWithValue "target" (quote (param.TestRunnerExePath |> Path.getFullName)))
            |> StringBuilder.appendWithoutQuotes (printParamWithValue "targetargs" (quote targetArgs))
            |> StringBuilder.appendIfTrueWithoutQuotes (String.isNotNullOrEmpty param.Output) (printParamWithValue "output" (quote param.Output))
            |> StringBuilder.appendWithoutQuotes
                    (match param.Register with
                    | Manual -> String.Empty
                    | Register -> printParam "register"
                    | RegisterUser -> printParamWithValue "register" "user"
                    | Path32 -> printParamWithValue "register" "Path32"
                    | Path64 -> printParamWithValue "register" "Path64")
            |> StringBuilder.appendIfTrueWithoutQuotes (String.isNotNullOrEmpty param.Filter) (printParamWithValue "filter" (quote param.Filter))
            |> StringBuilder.appendIfTrueWithoutQuotes param.MergeByHash (printParam "mergebyhash")
            |> StringBuilder.appendIfTrueWithoutQuotes (not param.ExcludeByAttribute.IsEmpty) (printParamListAsValuesWithQuote "excludebyattribute" param.ExcludeByAttribute)
            |> StringBuilder.appendIfTrueWithoutQuotes (not param.ExcludeByFile.IsEmpty) (printParamListAsValuesWithQuote "excludebyfile" param.ExcludeByFile)
            |> StringBuilder.appendIfTrueWithoutQuotes (not param.ExcludeDirs.IsEmpty) (printParamListAsValuesWithQuote "excludedirs" param.ExcludeDirs)
            |> StringBuilder.appendIfTrueWithoutQuotes (not param.HideSkipped.IsEmpty) (printParamListAsValues "hideskipped" param.HideSkipped)
            |> StringBuilder.appendIfTrueWithoutQuotes param.MergeOutput (printParam "mergeoutput")
            |> StringBuilder.appendWithoutQuotes
                    (match param.ReturnTargetCode with
                    | No ->  String.Empty
                    | Yes -> printParam "returntargetcode"
                    | Offset o -> printParamWithValue "returntargetcode" (string o))
            |> StringBuilder.appendIfTrueWithoutQuotes (not param.SearchDirs.IsEmpty) (printParamListAsValuesWithQuote "searchdirs" param.SearchDirs)
            |> StringBuilder.appendIfTrueWithoutQuotes param.SkipAutoProps (printParam "skipautoprops")
            |> StringBuilder.appendIfTrueWithoutQuotes (String.isNotNullOrEmpty param.OptionalArguments) param.OptionalArguments
            |> StringBuilder.toText

    /// Runs OpenCover on a group of assemblies.
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the default OpenCover parameters.
    ///  - `targetArgs` - Test runner arguments.
    ///
    /// ## Sample
    ///
    ///      OpenCover.Run (fun p -> { p with TestRunnerExePath = "./Tools/NUnit/nunit-console.exe" }) 
    ///         "project-file.nunit /config:Release /noshadow /xml:artifacts/nunit.xml /framework:net-4.0"
    let run setParams targetArgs =
        use __ = Trace.traceTask "OpenCover" "Gathering coverage statistics"
        let param = setParams OpenCoverDefaults
    
        let processArgs = buildOpenCoverArgs param targetArgs
        Trace.tracefn "OpenCover command\n%s %s" param.ExePath processArgs
        let ok = 
            Process.execSimple ((fun info ->
            { info with
                FileName = param.ExePath
                WorkingDirectory =
                    if param.WorkingDir <> String.Empty then param.WorkingDir else info.WorkingDirectory
                Arguments = processArgs }) >> Process.withFramework) param.TimeOut
        if ok <> 0 then failwithf "OpenCover reported errors."
        __.MarkSuccess()

    /// Show version OpenCover
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the default OpenCover parameters.
    ///
    /// ## Sample
    ///
    ///      OpenCover.Version None
    ///      OpenCover.Version (fun p -> { p with TestRunnerExePath = "./Tools/NUnit/nunit-console.exe" })
    let getVersion setParams =
        use __ = Trace.traceTask "OpenCover" "Version"
        let param = match setParams with
                    | Some setParams -> setParams OpenCoverDefaults
                    | None -> OpenCoverDefaults

        Process.execSimple ((fun info ->
        { info with
            FileName = param.ExePath
            Arguments = "-version" }) >> Process.withFramework) param.TimeOut |> ignore
        __.MarkSuccess()
