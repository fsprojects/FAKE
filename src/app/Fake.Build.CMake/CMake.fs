namespace Fake.Build

open System
open System.IO
open Fake.Core
open Fake.IO.FileSystemOperators

/// <namespacedoc>
/// <summary>
/// Build namespace contains tasks to interact with other Build Systems, like CMake
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains tasks which allow to use CMake to build CMakeLists files.
/// </summary>
[<RequireQualifiedAccess>]
module CMake =

    /// The possible variable value types for CMake variables.
    type CMakeValue =
        | CMakeBoolean of bool
        | CMakeString of string
        | CMakeFilePath of string
        | CMakeDirPath of string

    /// A CMake variable.
    type CMakeVariable =
        {
            /// The name of the variable.
            /// It cannot contains spaces and special characters.
            Name: string
            /// The value of the variable.
            /// Will be automatically converted to the CMake format when required.
            Value: CMakeValue
        }

    /// The CMakeGenerate parameter type.
    type CMakeGenerateParams =
        {
            /// The location of the CMake executable. Automatically found if null or empty.
            ToolPath: string
            /// The source directory which should include a `CMakeLists.txt` file.
            SourceDirectory: string
            /// The binary build directory where CMake will generate the files.
            BinaryDirectory: string
            /// An optional toolchain file to load.
            /// Equivalent to the `-D CMAKE_TOOLCHAIN_FILE:FILEPATH="<toolchain-file>"` CMake option.
            Toolchain: string
            /// The native build system generator to use for writing the files.
            /// See `cmake --help` for a list of the available entries.
            /// *To avoid unpredictable generator usage, it is recommended to define it.*
            /// Equivalent to the `-G <generator-name>` option.
            Generator: string
            /// An optional toolset (!= toolchain) to use.
            /// Equivalent to the `-T <toolset-name>` option.
            /// Not supported by every generator.
            Toolset: string
            /// An optional CMake platform.
            /// Equivalent to the `-A <platform-name>` option.
            /// Not supported by every generator.
            Platform: string
            /// A list of the optional CMake cache files to load.
            /// Equivalent to the `-C <initial-cache>` options.
            Caches: string list
            /// The directory where CMake will install the generated files.
            /// Equivalent to the `-D CMAKE_INSTALL_PREFIX:DIRPATH="<install-directory>"` CMake option.
            InstallDirectory: string
            /// A list of every variable to pass as a CMake argument.
            /// Equivalent to the `-D <var>:<type>=<value>` options.
            Variables: CMakeVariable list
            /// Remove matching entries from CMake cache.
            /// Equivalent to the `-U <globbing_expr>` options.
            CacheEntriesToRemove: string list
            /// The CMake execution timeout.
            Timeout: TimeSpan
            /// A character string containing additional arguments to give to CMake.
            AdditionalArgs: string
        }

    /// The CMakeBuild parameter type.
    type CMakeBuildParams =
        {
            /// The location of the CMake executable. Automatically found if null or empty.
            ToolPath: string
            /// The binary build directory where CMake will generate the files.
            BinaryDirectory: string
            /// The CMake target to build instead of the default one.
            /// Equivalent to the `--target <target>` option.
            Target: string
            /// The build configuration to use (e.g. `Release`).
            /// Equivalent to the `--config <cfg>` option.
            /// Not supported by every generator.
            Config: string
            /// The CMake execution timeout.
            Timeout: TimeSpan
            /// A character string containing additional arguments to give to CMake.
            AdditionalArgs: string
        }

    let private currentDirectory = Directory.GetCurrentDirectory()

    /// The default option set given to CMakeGenerate.
    let CMakeGenerateDefaults =
        { ToolPath = ""
          SourceDirectory = currentDirectory
          BinaryDirectory = currentDirectory @@ "build"
          Toolchain = ""
          Generator = ""
          Toolset = ""
          Platform = ""
          Caches = []
          InstallDirectory = currentDirectory @@ "install"
          Variables = []
          CacheEntriesToRemove = []
          Timeout = TimeSpan.MaxValue
          AdditionalArgs = "" }

    /// The default option set given to CMakeBuild.
    let CMakeBuildDefaults =
        { ToolPath = ""
          BinaryDirectory = currentDirectory @@ "build"
          Target = ""
          Config = ""
          Timeout = TimeSpan.MaxValue
          AdditionalArgs = "" }

    /// <summary>
    /// Tries to find the specified CMake executable:
    /// <list type="number">
    /// <item>
    /// Locally in <c>./&lt;tools|packages&lt;cmake.portable&gt;|&lt;cmake&gt;/bin</c>
    /// </item>
    /// <item>
    /// In the <c>PATH</c> environment variable.
    /// </item>
    /// <item>
    /// In the <c>&lt;ProgramFilesx86>\CMake\bin</c> directory.
    /// </item>
    /// </list>
    /// </summary>
    ///
    /// <param name="exeName">The name of the CMake executable (e.g. `cmake`, `ctest`, etc.) to find.
    ///    The `.exe` suffix will be automatically appended on Windows.</param>
    let FindExe exeName =
        let fullName = exeName + if Environment.isUnix then "" else ".exe"

        [ Seq.singleton (currentDirectory @@ "tools" @@ "cmake.portable" @@ "tools" @@ "bin")
          Seq.singleton (currentDirectory @@ "packages" @@ "cmake.portable" @@ "tools" @@ "bin")
          Seq.singleton (currentDirectory @@ "tools" @@ "cmake" @@ "tools" @@ "bin")
          Seq.singleton (currentDirectory @@ "packages" @@ "cmake" @@ "tools" @@ "bin")
          Environment.pathDirectories ]
        |> (fun list ->
            if Environment.isUnix then
                list
            else
                List.append list [ Seq.singleton (Environment.ProgramFilesX86 @@ "CMake" @@ "bin") ])
        |> Seq.concat
        |> Seq.map (fun directory -> directory @@ fullName)
        |> Seq.tryFind File.Exists

    /// <summary>
    /// Converts a file path to a valid CMake format.
    /// </summary>
    ///
    /// <param name="path">The path to reformat.</param>
    let private FormatCMakePath (path: string) = path.Replace("\\", "/")

    /// <summary>
    /// Invokes the CMake executable with the specified arguments.
    /// </summary>
    ///
    /// <param name="toolPath">The location of the executable. Automatically found if null or empty.</param>
    /// <param name="binaryDir">The location of the binary directory.</param>
    /// <param name="args">The arguments given to the executable.</param>
    /// <param name="timeout">The CMake execution timeout</param>
    let private CallCMake toolPath binaryDir args timeout =
        // CMake expects an existing binary directory.
        // Not defaulted because it would prevent building multiple CMake projects in the same FAKE script.
        if String.IsNullOrEmpty binaryDir then
            failwith "The CMake binary directory is not set."
        // Try to find the CMake executable if not specified by the user.
        let cmakeExe =
            if String.isNotNullOrEmpty toolPath then
                toolPath
            else
                let found = FindExe "cmake"

                if found <> None then
                    found.Value
                else
                    failwith "Cannot find the CMake executable."
        // CMake expects the binary directory to be passed as an argument.
        let arguments =
            if (String.IsNullOrEmpty args) then
                "\"" + binaryDir + "\""
            else
                args

        let fullCommand = cmakeExe + " " + arguments
        use __ = Trace.traceTask "CMake" fullCommand

        let result =
            CreateProcess.fromRawCommandLine cmakeExe arguments
            |> CreateProcess.withWorkingDirectory binaryDir
            |> CreateProcess.withTimeout (timeout)
            |> Proc.run

        if result.ExitCode <> 0 then
            failwithf $"CMake failed with exit code %i{result.ExitCode}."

    let internal getGenerateArguments parameters =
        // CMake expects an existing source directory.
        // Not defaulted because it would prevent building multiple CMake projects in the same FAKE script.
        if String.IsNullOrEmpty parameters.SourceDirectory then
            failwith "The CMake source directory is not set."

        let argsIfNotEmpty format values =
            List.filter String.isNotNullOrEmpty values |> List.map (sprintf format)

        let generator = argsIfNotEmpty "-G \"%s\"" [ parameters.Generator ]

        let toolchain =
            argsIfNotEmpty "-D CMAKE_TOOLCHAIN_FILE:FILEPATH=\"%s\"" [ FormatCMakePath parameters.Toolchain ]

        let toolset = argsIfNotEmpty "-T \"%s\"" [ parameters.Toolset ]
        let platform = argsIfNotEmpty "-A \"%s\"" [ parameters.Platform ]

        let caches =
            parameters.Caches |> List.map FormatCMakePath |> argsIfNotEmpty "-C \"%s\""

        let installDir =
            argsIfNotEmpty "-D CMAKE_INSTALL_PREFIX:PATH=\"%s\"" [ FormatCMakePath parameters.InstallDirectory ]

        let variables =
            parameters.Variables
            |> List.map (fun option ->
                "-D "
                + option.Name
                + match option.Value with
                  | CMakeBoolean(value) -> ":BOOL=" + if value then "ON" else "OFF"
                  | CMakeString(value) -> ":STRING=\"" + value + "\""
                  | CMakeDirPath(value) -> FormatCMakePath value |> sprintf ":PATH=\"%s\""
                  | CMakeFilePath(value) -> FormatCMakePath value |> sprintf ":FILEPATH=\"%s\"")

        let cacheEntriesToRemove =
            argsIfNotEmpty "-U \"%s\"" parameters.CacheEntriesToRemove

        let args =
            [ generator
              toolchain
              toolset
              platform
              caches
              installDir
              variables
              cacheEntriesToRemove
              [ parameters.AdditionalArgs; "\"" + parameters.SourceDirectory + "\"" ] ]
            |> List.concat
            |> String.concat " "

        args

    /// <summary>
    /// Calls <c>cmake</c> to generate a project.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default CMake parameters. See <c>CMakeGenerateParams</c>.</param>
    let Generate setParams =
        let parameters = setParams CMakeGenerateDefaults
        let args = getGenerateArguments parameters
        CallCMake parameters.ToolPath parameters.BinaryDirectory args parameters.Timeout

    /// <summary>
    /// Calls <c>cmake --build</c> to build a project.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default CMake parameters. See <c>CMakeBuildParams</c>.</param>
    let Build setParams =
        let parameters = setParams CMakeBuildDefaults

        let targetArgs =
            if String.IsNullOrEmpty parameters.Target then
                ""
            else
                " --target \"" + parameters.Target + "\""

        let configArgs =
            if String.IsNullOrEmpty parameters.Config then
                ""
            else
                " --config \"" + parameters.Config + "\""

        let args =
            "--build \""
            + parameters.BinaryDirectory
            + "\""
            + targetArgs
            + configArgs
            + parameters.AdditionalArgs

        CallCMake parameters.ToolPath parameters.BinaryDirectory args parameters.Timeout
