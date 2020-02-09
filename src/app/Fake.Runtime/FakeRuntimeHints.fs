module Fake.Runtime.FakeRuntimeHints

open System
open System.IO
open Fake.Runtime
open Fake.Runtime.Runners
open Fake.Runtime.Trace
open Paket
open System
open Fake.Core

type Hint =
 { Important : bool
   Text : string }

let paketVersion =
    let paketCoreVersion = typeof<Paket.DependenciesFile>.Assembly.GetName().Version
    let semVerPaketCore = SemVer.Parse (paketCoreVersion.ToString())
    semVerPaketCore.Normalize()

let retrieveHints (prepareInfo:FakeRuntime.PrepareInfo) (context:FakeContext) (runResult:Runners.RunResult) (cache:ResultCoreCacheInfo) =
    let config = context.Config
    // https://github.com/fsharp/FAKE/issues/2001
    let fsCoreDll =
        config.CompileOptions.FsiOptions.References
        |> Seq.tryFind (fun r -> r.ToLower().EndsWith("fsharp.core.dll")) 
    let fsCoreHint =
        fsCoreDll
        |> Option.bind (fun fsCoreDll -> FakeRuntime.filterValidAssembly VerboseLevel.Silent ("", false, FileInfo fsCoreDll))
        |> Option.bind (fun assInfo ->
            let refVersion = Version assInfo.Info.Version
            let currentVersion = Environment.fsCoreAssembly().GetName().Version
            if refVersion > currentVersion then
                Some { Important = true
                       Text = sprintf "Paket resolved a FSharp.Core with version '%O', but fake runs with a version of '%O'. This is not supported.\nPlease either lock the version via 'nuget FSharp.Core <nuget-version>' or upgrade fake.\nRead https://github.com/fsharp/FAKE/issues/2001 for details." refVersion currentVersion }
            else None)

    let paketVersionHint =
        // from paket bootstrapper code https://github.com/fsprojects/Paket/blob/master/src/Paket.Bootstrapper/PaketDependencies.cs
        let regex = lazy new System.Text.RegularExpressions.Regex("^\\s*version\\s+(?<args>.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase ||| System.Text.RegularExpressions.RegexOptions.Compiled)
        let getBootstrapperArgs (paketDeps:string) =
            use reader = new StreamReader(paketDeps)
            let lines = seq {
                let mutable line = reader.ReadLine()
                while not (isNull line) do
                    yield line
                    line <- reader.ReadLine() }
            lines
            |> Seq.map (fun line -> regex.Value.Match(line))
            |> Seq.tryFind (fun regMatch -> regMatch.Success)
            |> Option.map (fun regMatch -> regMatch.Groups.["args"].Value.Trim())
        let findVersionFromBootstrapperArgs argLine =
            let args = CmdLineParsing.windowsCommandLineToArgv argLine
            let argPrefixesToSkip =
                [ "--help"; "--prefer-nuget"; "--force-nuget"; "--nuget-source="; "--self"; "-s"; "-v"; "-f"; "--max-file-age"; "--run" ]

            let firstOk =
                args
                |> Seq.filter (fun arg ->
                    argPrefixesToSkip
                    |> Seq.exists (fun prefix -> arg.StartsWith prefix)
                    |> not)
                |> Seq.tryHead
            firstOk
        match prepareInfo.DependencyType with
        | FakeRuntime.PaketInline -> None
        | FakeRuntime.PaketDependenciesRef ->
            let paketCoreVersionString = paketVersion
            match prepareInfo._Section with
            | FakeHeader.PaketDependencies(_, d, _, _) ->
                match getBootstrapperArgs d.DependenciesFile with
                | None ->
                    { Important = false
                      Text =
                        sprintf "Could not find a version in your paket.dependencies file, consider adding 'version %s' at the top of your dependencies file (%s).\nRead https://github.com/fsharp/FAKE/issues/2193 for details."
                            paketCoreVersionString d.DependenciesFile }
                    |> Some
                | Some argLine ->
                    match findVersionFromBootstrapperArgs argLine with
                    | None ->
                        { Important = false
                          Text =
                            sprintf "Could not find a version in your paket.dependencies file bootstrapper arguments, consider locking the version to '%s' in your dependencies file (%s).\nRead https://github.com/fsharp/FAKE/issues/2193 for details."
                                paketCoreVersionString d.DependenciesFile }
                        |> Some
                    | Some v ->
                        if System.String.Equals(v, "prerelease", System.StringComparison.OrdinalIgnoreCase) then
                            { Important = false
                              Text =
                                sprintf "Detected 'prerelease' in your paket.dependencies file bootstrapper arguments, consider locking the version to '%s' in your dependencies file (%s).\nRead https://github.com/fsharp/FAKE/issues/2193 for details."
                                    paketCoreVersionString d.DependenciesFile }
                            |> Some
                        else
                            let s1 = SemVer.Parse v
                            let s2 = SemVer.Parse paketCoreVersionString
                            if s1 <> s2 then
                                { Important = false
                                  Text =
                                    sprintf "Detected paket version '%s' in your paket.dependencies file bootstrapper arguments, consider locking the version to '%O' in your dependencies file (%s).\nRead https://github.com/fsharp/FAKE/issues/2193 for details."
                                        (s1.Normalize()) paketCoreVersionString d.DependenciesFile }
                                |> Some
                            else None

        | FakeRuntime.DefaultDependencies -> None

    let versionUpgradeHint =
        match DateTime.TryParseExact
                (AssemblyVersionInformation.AssemblyMetadata_BuildDate,
                 "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                 System.Globalization.DateTimeStyles.AssumeUniversal) with
        // around 6 months old.              
        | true, dt when DateTime.UtcNow - dt > TimeSpan.FromDays(30. * 6.) ->
            let atLeast12 = DateTime.UtcNow - dt > TimeSpan.FromDays(30. * 12.)
            { Important = atLeast12
              Text = 
                sprintf "The fake-runner has not been updated for at least %d months. Please consider upgrading to get latest bugfixes, improved suggestions and F# features."
                  (if atLeast12 then 12 else 6) }
            |> Some
        | _ -> None                              

    let globalHints =
        [ match fsCoreHint with
          | Some hint -> yield hint
          | None -> ()
          match paketVersionHint with
          | Some hint -> yield hint
          | None -> ()
          match versionUpgradeHint with
          | Some hint -> yield hint
          | None -> ()
        ]

    let rec findException f (err:exn) =
        match err with
        | _ when f err -> true
        | :? AggregateException as agg ->
            agg.InnerExceptions
            |> Seq.exists (findException f)
        | _ when not (isNull err.InnerException) ->
            findException f err.InnerException
        | _ -> false


    match runResult with
    | Runners.RunResult.SuccessRun _ -> globalHints
    | Runners.RunResult.CompilationError err ->
      [
        // Add some hints about the error, for example
        // detect https://github.com/fsharp/FAKE/issues/1783
        let containsNotDefined = err.Errors |> Seq.exists (fun er -> er.ErrorNumber = 39)
        let containsNotSupportOperator = err.Errors |> Seq.exists (fun er -> er.ErrorNumber = 43)
        if containsNotDefined then
          yield { Important = false; Text = sprintf "If you have updated your dependencies you might need to run 'paket install' or delete '%s.lock' for fake to pick them up." config.ScriptFilePath }
          yield { Important = false; Text = "If this doesn't help but you are sure this should work try to clean your nuget cache and delete the .fake directory. If this helps please report this problem to Paket." }
        if containsNotSupportOperator then
          yield { Important = false; Text = "Operators now need to be opened manually, try to add 'open Fake.IO.FileSystemOperators' and 'open Fake.IO.Globbing.Operators' to your script to import the most common operators" }
        yield! globalHints
      ]
    | Runners.RunResult.RuntimeError err ->
      [
        if findException (fun e -> e :? MissingMethodException) err then
          yield { Important = false; Text = "The given error might indicate a problem with the fake cache. Backup the '.fake' directory, delete it and try again. If it works or you need help consider reporting a new issue." }
        if not config.VerboseLevel.PrintVerbose && Environment.GetEnvironmentVariable "FAKE_DETAILED_ERRORS" <> "true" then
          yield { Important = false; Text = "To further diagnose the problem you can run fake in verbose mode `fake -v run ...` or set the 'FAKE_DETAILED_ERRORS' environment variable to 'true'" }
        
        yield! globalHints
      ]
