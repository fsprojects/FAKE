namespace Fake.Azure

/// <summary>
/// Contains tasks to stage and deploy Azure website and webjobs using source code deployment with Kudu Sync.
/// </summary>
[<RequireQualifiedAccess>]
module Kudu =

    open System
    open System.IO
    open System.Net.Http
    open Fake.Core
    open Fake.IO

    /// Location where staged outputs should go before synced up to the site.
    let deploymentTemp =
        Environment.environVarOrDefault "DEPLOYMENT_TEMP" (Path.GetTempPath() + "kudutemp")

    /// Location where synced outputs should be deployed to.
    let deploymentTarget =
        Environment.environVarOrDefault "DEPLOYMENT_TARGET" (Path.GetTempPath() + "kudutarget")

    /// Used by KuduSync for tracking and diffing deployments.
    let nextManifestPath =
        Environment.environVarOrDefault "NEXT_MANIFEST_PATH" String.Empty

    /// Used by KuduSync for tracking and diffing deployments.
    let previousManifestPath =
        Environment.environVarOrDefault "PREVIOUS_MANIFEST_PATH" String.Empty

    /// The path to the KuduSync application.
    let kuduPath =
        Environment.environVarOrNone "GO_WEB_CONFIG_TEMPLATE"
        |> function
            | Some goWebConfigTemplate -> Path.GetDirectoryName goWebConfigTemplate
            | None -> "."
        |> DirectoryInfo.ofPath

    /// The different types of web jobs.
    type WebJobType =
        | Scheduled
        | Continuous

    // Some initial cleanup / prep
    do
        Directory.ensure deploymentTemp |> ignore
        Directory.ensure deploymentTarget |> ignore
        Shell.cleanDir deploymentTemp

    /// Kudu ZipDeploy parameters
    type ZipDeployParams =
        {
            /// The url of the website, usually in the format of https://<yourwebsite>.scm.azurewebsites.net
            Url: Uri
            /// The WebDeploy or Git username, usually the $username from the site's publish profile
            UserName: string
            /// The WebDeploy or Git Password
            Password: string
            /// The path to the zip archive to upload
            PackageLocation: string
        }

    /// <summary>
    /// Stages a folder and all subdirectories into the temp deployment area, ready for deployment into the website.
    /// </summary>
    ///
    /// <param name="source">The source folder to copy.</param>
    /// <param name="shouldInclude">A predicate which includes files from the folder. If the entire directory should be copied, this predicate should always return true.</param>
    let stageFolder source shouldInclude =
        Shell.copyRecursive source deploymentTemp true
        |> Seq.filter (not << shouldInclude)
        |> Seq.iter File.Delete

    /// <summary>
    /// Gets the path for deploying a web job to.
    /// </summary>
    ///
    /// <param name="webJobType">The web job type. Of type <c>WebJobType</c></param>
    /// <param name="webJobName">The name of the web job</param>
    let getWebJobPath webJobType webJobName =
        let webJobType =
            match webJobType with
            | Scheduled -> "triggered"
            | Continuous -> "continuous"

        sprintf @"%s\app_data\jobs\%s\%s\" deploymentTemp webJobType webJobName

    /// <summary>
    /// Stages a set of files into a WebJob folder in the temp deployment area, ready for deployment into the website as a webjob.
    /// </summary>
    ///
    /// <param name="webJobType">The web job type. Of type <c>WebJobType</c></param>
    /// <param name="webJobName">The name of the web job</param>
    /// <param name="files">Files to deploy</param>
    let stageWebJob webJobType webJobName files =
        let webJobPath = getWebJobPath webJobType webJobName
        Directory.ensure webJobPath |> ignore
        files |> Shell.copyFiles webJobPath

    /// Synchronises all staged files from the temporary deployment to the actual deployment, removing
    /// any obsolete files, updating changed files and adding new files.
    let kuduSync () =
        let args =
            Args.toWindowsCommandLine
                [ "-v"
                  "50"
                  "-f"
                  deploymentTemp
                  "-t"
                  deploymentTarget
                  "-n"
                  nextManifestPath
                  "-p"
                  previousManifestPath
                  "-i"
                  ".git;.hg;.deployment;deploy.cmd" ]

        let results = System.Collections.Generic.List<ConsoleMessage>()

        let errorF msg =
            Trace.traceError msg
            results.Add(ConsoleMessage.CreateError msg)

        let messageF msg =
            Trace.trace msg
            results.Add(ConsoleMessage.CreateOut msg)

        let processResult =
            CreateProcess.fromRawCommandLine (Path.Combine(kuduPath.FullName, "kudusync.cmd")) args
            |> CreateProcess.withTimeout (TimeSpan.FromMinutes 5.)
            |> CreateProcess.redirectOutput
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> Proc.run

        results |> Seq.iter (fun cm -> printfn "%O: %s" cm.Timestamp cm.Message)

        if processResult.ExitCode <> 0 then
            failwith "Error occurred during Kudu Sync deployment."

    /// <summary>
    /// Synchronizes contents of the zip package with the target web app using Kudu ZipDeploy.
    /// </summary>
    ///
    /// <param name="zipDeployParams">The parameters for zip deploy command</param>
    let zipDeploy (zipDeployParams: ZipDeployParams) =
        let authToken =
            Convert.ToBase64String(
                Text.Encoding.ASCII.GetBytes(zipDeployParams.UserName + ":" + zipDeployParams.Password)
            )

        let statusCode =
            use client = new HttpClient(Timeout = TimeSpan.FromMilliseconds 300000.)
            client.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Basic", authToken)

            use fileStream = new FileStream(zipDeployParams.PackageLocation, FileMode.Open)
            use content = new StreamContent(fileStream)
            content.Headers.ContentType <- Headers.MediaTypeHeaderValue("multipart/form-data")

            let response =
                client
                    .PostAsync(zipDeployParams.Url.AbsoluteUri + "api/zipdeploy", content)
                    .Result

            response.StatusCode

        if statusCode = Net.HttpStatusCode.OK then
            Trace.tracefn "Deployed %s" zipDeployParams.Url.AbsoluteUri
        else
            failwithf "Failed to deploy package with status code %A" statusCode
