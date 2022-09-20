namespace Fake.JavaScript

open Fake.Core
open Fake.IO.FileSystemOperators
open System
open System.IO
open System.Text

/// <summary>
/// Helpers to run the typeScript compiler.
/// </summary>
/// 
/// <example>
/// <code lang="fsharp">
/// !! "src/**/*.ts"
///         |> TypeScript.compile (fun p -> { p with TimeOut = TimeSpan.MaxValue })
/// </code>
/// </example>   
[<RequireQualifiedAccess>]
module TypeScript =
    /// Generated ECMAScript version
    type ECMAScript =
        | ES3
        | ES5
        | ES6
        | ES7
        | ES2017
        | ES2018
        | ES2019
        | ES2020
        | ESNext
    
    /// Generated JavaScript module type
    type ModuleGeneration = 
        | CommonJs
        | ES6
        | ES2020
        | None
        | UMD
        | AMD
        | System
        | ESNext

    /// TypeScript task parameter type
    type TypeScriptParams =
        { 
          /// Specifies which ECMAScript version the TypeScript compiler should generate. Default is ES3.
          ECMAScript : ECMAScript
          /// Specifies if the TypeScript compiler should generate a single output file and its filename.
          OutputSingleFile : string option
          /// Specifies if the TypeScript compiler should generate declaration. Default is false.
          EmitDeclaration : bool
          /// Specifies which JavaScript module type the TypeScript compiler should generate. Default is CommonJs.
          ModuleGeneration : ModuleGeneration
          /// Specifies if the TypeScript compiler should emit source maps. Default is false.
          EmitSourceMaps : bool
          /// Specifies if the TypeScript compiler should not use libs. Default is false.
          NoLib : bool      
          /// Specifies if the TypeScript compiler should remove comments. Default is false.
          RemoveComments : bool
          /// Specifies the TypeScript compiler path.
          ToolPath : string
          /// Specifies the TypeScript compiler output path.
          OutputPath : string
          /// Specifies the timeout for the TypeScript compiler.
          TimeOut : TimeSpan }

    /// [omit]
    let extractVersionNumber (di : DirectoryInfo) =
        match Double.TryParse di.Name with
        | true, d -> d
        | false, _ -> 0.0

    /// We will resolve TypeScript compiler installation in the following order for a Windows installation
    ///  (Please see TypeScript installation page: https://www.typescriptlang.org/download)
    /// 2. We will try to look for global installation of TypeScript as a global NPM or Yarn tool
    /// 3. Then, we will try to resolve it from Microsoft Visual Studio installation.
    /// 4. Finally, we will default to "tsc.exe"
    let internal resolveTypeScriptCompilerInstallation () =
        if Environment.isUnix then "tsc"
        else
            let globalNodePackageInstallationPaths =
                [ Environment.GetFolderPath Environment.SpecialFolder.ApplicationData </> "Local" </> "Yarn" </> "bin"
                  Environment.GetFolderPath Environment.SpecialFolder.ApplicationData </> "Roaming" </> "npm" ]

            let visualStudioInstallationPaths =
                [ Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles
                  Environment.GetFolderPath Environment.SpecialFolder.ProgramFilesX86 ]
                |> List.map (fun p -> p </> "Microsoft SDKs" </> "TypeScript")
                |> List.collect (fun p -> try DirectoryInfo(p).GetDirectories() |> List.ofArray with | _ -> [])
                |> List.sortByDescending extractVersionNumber
                |> List.map (fun di -> di.FullName)

            ProcessUtils.tryFindPath globalNodePackageInstallationPaths "tsc.cmd"
            |> Option.orElseWith(fun _ -> ProcessUtils.tryFindPath visualStudioInstallationPaths "tsc.exe")
            |> Option.defaultWith (fun _ -> "tsc.exe")

    /// Default parameters for the TypeScript task
    let TypeScriptDefaultParams = 
        { ECMAScript = ECMAScript.ESNext
          OutputSingleFile = Option.None
          EmitDeclaration = false
          ModuleGeneration = ModuleGeneration.ESNext
          EmitSourceMaps = false
          NoLib = false
          RemoveComments = false
          OutputPath = null
          ToolPath = resolveTypeScriptCompilerInstallation()
          TimeOut = TimeSpan.FromMinutes 5. }

    let internal buildArguments parameters file = 
        let version = 
            match parameters.ECMAScript with
            | ECMAScript.ES3 -> "ES3"
            | ECMAScript.ES5 -> "ES5"
            | ECMAScript.ES6 -> "ES6"
            | ECMAScript.ES7 -> "ES7"
            | ECMAScript.ES2017 -> "ES2017"
            | ECMAScript.ES2018 -> "ES2018"
            | ECMAScript.ES2019 -> "ES2019"
            | ECMAScript.ES2020 -> "ES2020"
            | ECMAScript.ESNext -> "ESNext"
        
        let moduleGeneration = 
            match parameters.ModuleGeneration with
            | ModuleGeneration.CommonJs -> "CommonJS"
            | ModuleGeneration.ES6 -> "ES6"
            | ModuleGeneration.ES2020 -> "ES2020"
            | ModuleGeneration.None -> "None"
            | ModuleGeneration.UMD -> "UMD"
            | ModuleGeneration.AMD -> "AMD"
            | ModuleGeneration.System -> "System"
            | ModuleGeneration.ESNext -> "ESNext"
        
        let args = 
            StringBuilder()
            |> StringBuilder.appendWithoutQuotes (" --target " + version)
            |> StringBuilder.appendIfSome parameters.OutputSingleFile (fun s -> sprintf " --outFile %s" s)
            |> StringBuilder.appendQuotedIfNotNull parameters.OutputPath " --outDir "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.EmitDeclaration " --declaration"
            |> StringBuilder.appendWithoutQuotes (" --module " + moduleGeneration)
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.EmitSourceMaps " --sourceMap"
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.NoLib " --noLib"
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.RemoveComments " --removeComments"
            |> StringBuilder.appendWithoutQuotes " "
            |> StringBuilder.append file
        
        args.ToString()

    /// <summary>
    /// Run <c>tsc --declaration src/app/index.ts</c>
    /// </summary>
    /// 
    /// <param name="setParams">Function used to overwrite the TypeScript compiler flags.</param>
    /// <param name="files">The type script files to compile.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// !! "src/**/*.ts"
    ///             |> TypeScript.compile (fun p -> { p with TimeOut = TimeSpan.MaxValue }) 
    /// </code>
    /// </example>
    let compile setParams files = 
        use __ = Trace.traceTask "TypeScript" ""
        let parameters = setParams TypeScriptDefaultParams

        let callResults = 
            files
            |> Seq.map (buildArguments parameters)
            |> Seq.map (fun arguments ->
                Diagnostics.ProcessStartInfo(FileName = parameters.ToolPath, Arguments = arguments)
                |> CreateProcess.ofStartInfo
                |> CreateProcess.withTimeout parameters.TimeOut
                |> Proc.run)

        let hasErrors =
            callResults
            |> Seq.exists (fun result -> result.ExitCode <> 0)

        if hasErrors then
            failwith "TypeScript compiler encountered errors!"
