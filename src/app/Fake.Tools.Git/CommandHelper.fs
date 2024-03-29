namespace Fake.Tools.Git

open System
open System.IO
open Fake.Core
open Fake.Core.String.Operators
open Fake.IO
open Fake.IO.FileSystemOperators

/// <summary>
/// Contains helpers which allow to interact with [git](http://git-scm.com/) via the command line.
/// </summary>
[<RequireQualifiedAccess>]
module CommandHelper =

    /// <summary>
    /// Specifies a global timeout for <c>git.exe</c> - default is <ins>no timeout</ins>
    /// </summary>
    let mutable gitTimeOut = TimeSpan.MaxValue

    let private GitPath =
        [ Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles
          </> "Git"
          </> "cmd"
          Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles
          </> "Git"
          </> "bin"
          Environment.GetFolderPath Environment.SpecialFolder.ProgramFilesX86
          </> "Git"
          </> "cmd"
          Environment.GetFolderPath Environment.SpecialFolder.ProgramFilesX86
          </> "Git"
          </> "bin" ]

    /// Tries to locate the <c>git.exe</c> via the environment variable "GIT".
    let gitPath =
        if Environment.isUnix then
            "git"
        else
            let ev = Environment.environVar "GIT"

            if not (String.isNullOrEmpty ev) then
                ev
            else
                ProcessUtils.findPath GitPath "git.exe"

    let inline private setInfo gitPath repositoryDir command (info: ProcStartInfo) =
        { info with
            FileName = gitPath
            WorkingDirectory = repositoryDir
            Arguments = command }

    /// <summary>
    /// Runs <c>git.exe</c> with the given command in the given repository directory.
    /// </summary>
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let runGitCommand repositoryDir command =
        let messages = System.Collections.Generic.List<string>()
        let errors = System.Collections.Generic.List<string>()

        let errorF msg = errors.Add msg
        let messageF msg = messages.Add msg

        let processResult =
            CreateProcess.fromRawCommandLine gitPath command
            |> CreateProcess.withTimeout gitTimeOut
            |> CreateProcess.redirectOutput
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> CreateProcess.withWorkingDirectory repositoryDir
            |> Proc.run

        processResult.ExitCode = 0, messages |> List.ofSeq, String.toLines (errors |> List.ofSeq)

    /// <summary>
    /// Runs <c>git.exe</c> with the given formatted command
    /// </summary>
    ///
    /// <param name="fmt">The formatted GIT command string to execute</param>
    let runGitCommandf fmt = Printf.ksprintf runGitCommand fmt

    /// <summary>
    /// Runs <c>git.exe</c> with the given command in the given repository directory and return results
    /// </summary>
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let getGitResult repositoryDir command =
        let _, msg, _ = runGitCommand repositoryDir command
        msg

    /// <summary>
    /// Fires the given git command in the given repository directory and returns immediately.
    /// </summary>
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let fireAndForgetGitCommand repositoryDir command =
        CreateProcess.fromRawCommandLine gitPath command
        |> CreateProcess.withWorkingDirectory repositoryDir
        |> Proc.startRawSync
        |> ignore


    /// <summary>
    /// Runs the given git command, waits for its completion and returns whether it succeeded.
    /// </summary>
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let directRunGitCommand repositoryDir command =
        let processResult =
            CreateProcess.fromRawCommandLine gitPath command
            |> CreateProcess.withWorkingDirectory repositoryDir
            |> Proc.run

        processResult.ExitCode = 0

    /// <summary>
    /// Runs the given git command, waits for its completion and fails when it didn't succeeded.
    /// </summary>
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let directRunGitCommandAndFail repositoryDir command =
        directRunGitCommand repositoryDir command
        |> fun ok ->
            if not ok then
                failwith "Command failed."

    /// Runs the given git command, waits for its completion.
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let gitCommand repositoryDir command =
        let ok, msg, error = runGitCommand repositoryDir command

        if not ok then
            failwith error
        else
            msg |> Seq.iter (Trace.logfn "%s")

    /// Runs git.exe with the given formatted command
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let gitCommandf repositoryDir fmt =
        Printf.ksprintf (gitCommand repositoryDir) fmt

    /// Runs the given git command, waits for its completion.
    /// This version doesn't throw an exception if an error occurs. It just traces the error.
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let showGitCommand repositoryDir command =
        let _, msg, errors = runGitCommand repositoryDir command
        msg |> Seq.iter (Trace.logfn "%s")

        if errors <> "" then
            Trace.traceError <| sprintf "Errors: %s" errors

    /// Runs the git command and returns the first line of the result.
    ///
    /// <param name="repositoryDir">The repository directory to execute command in</param>
    /// <param name="command">The GIT command to execute</param>
    let runSimpleGitCommand repositoryDir command =
        try
            let _, msg, errors = runGitCommand repositoryDir command

            let errorText = String.toLines msg + Environment.NewLine + errors

            if errorText.Contains "fatal: " then
                failwith errorText

            if msg.Length = 0 then
                ""
            else
                msg |> Seq.iter (Trace.logfn "%s")
                msg[0]
        with exn ->
            failwithf "Could not run \"git %s\".\r\nError: %s" command exn.Message

    /// <summary>
    /// Fixes the given path by escaping backslashes
    /// </summary>
    /// [omit]
    let fixPath (path: string) =
        let path = path.Trim()

        if "\\\\" <* path then
            path.Trim()
        else
            path.Replace('\\', '/').Trim()

    /// <summary>
    /// Searches for a .git directory in the specified directory or any parent directory.
    /// </summary>
    /// <exception href="System.InvalidOperationException">Thrown when no .git directory is found.</exception>
    let findGitDir repositoryDir =
        let rec findGitDir (dirInfo: DirectoryInfo) =
            let gitDir =
                dirInfo.FullName + Path.directorySeparator + ".git" |> DirectoryInfo.ofPath

            if gitDir.Exists then
                gitDir
            elif isNull dirInfo.Parent then
                invalidOp
                    "Not a git repository: no .git directory found in the specified directory or any parent directory."
            else
                findGitDir dirInfo.Parent

        if String.isNullOrEmpty repositoryDir then
            "."
        else
            repositoryDir
        |> DirectoryInfo.ofPath
        |> findGitDir
