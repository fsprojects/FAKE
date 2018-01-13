/// Contains tasks to stage and deploy Azure website and webjobs using source code deployment with Kudu Sync.
module Fake.Azure.Kudu

open Fake
open System
open System.IO

/// Location where staged outputs should go before synced up to the site.
let deploymentTemp = getBuildParamOrDefault "DEPLOYMENT_TEMP" (Path.GetTempPath() + "kudutemp")
/// Location where synced outputs should be deployed to.
let deploymentTarget = getBuildParamOrDefault "DEPLOYMENT_TARGET" (Path.GetTempPath() + "kudutarget")
/// Used by KuduSync for tracking and diffing deployments.
let nextManifestPath = getBuildParam "NEXT_MANIFEST_PATH"
/// Used by KuduSync for tracking and diffing deployments.
let previousManifestPath = getBuildParam "PREVIOUS_MANIFEST_PATH"
/// The path to the KuduSync application.
let kuduPath = (getBuildParamOrDefault "GO_WEB_CONFIG_TEMPLATE" ".") |> directory

/// The different types of web jobs.
type WebJobType = Scheduled | Continuous

// Some initial cleanup / prep
do
    CreateDir deploymentTemp
    CreateDir deploymentTarget
    CleanDir deploymentTemp

/// <summary>
/// Stages a folder and all subdirectories into the temp deployment area, ready for deployment into the website.
/// </summary>
/// <param name="source">The source folder to copy.</param>
/// <param name="shouldInclude">A predicate which includes files from the folder. If the entire directory should be copied, this predicate should always return true.</param>
let stageFolder source shouldInclude =
    FileHelper.CopyRecursive source deploymentTemp true
    |> Seq.filter (not << shouldInclude)
    |> Seq.iter File.Delete

/// Gets the path for deploying a web job to.
let getWebJobPath webJobType webJobName =
    let webJobType = match webJobType with Scheduled -> "triggered" | Continuous -> "continuous" 
    sprintf @"%s\app_data\jobs\%s\%s\" deploymentTemp webJobType webJobName

/// Stages a set of files into a WebJob folder in the temp deployment area, ready for deployment into the website as a webjob.
let stageWebJob webJobType webJobName files =
    let webJobPath = getWebJobPath webJobType webJobName
    CreateDir webJobPath
    files |> FileHelper.CopyFiles webJobPath

/// Synchronises all staged files from the temporary deployment to the actual deployment, removing
/// any obsolete files, updating changed files and adding new files.
let kuduSync() =
    let succeeded, output =
        ProcessHelper.ExecProcessRedirected(fun psi ->
            psi.FileName <- combinePaths kuduPath "kudusync.cmd"
            psi.Arguments <- sprintf """-v 50 -f "%s" -t "%s" -n "%s" -p "%s" -i ".git;.hg;.deployment;deploy.cmd""" deploymentTemp deploymentTarget nextManifestPath previousManifestPath)
            (TimeSpan.FromMinutes 5.)
    output |> Seq.iter (fun cm -> printfn "%O: %s" cm.Timestamp cm.Message)
    if not succeeded then failwith "Error occurred during Kudu Sync deployment."

/// Kudu ZipDeploy parameters
type ZipDeployParams =
  { /// The url of the website, usually in the format of https://<yourwebsite>.scm.azurewebsites.net
    Url : Uri
    /// The WebDeploy or Git username, usually the $username from the site's publish profile
    UserName : string
    /// The WebDeploy or Git Password
    Password : string
    /// The path to the zip archive to upload
    PackageLocation: string }

/// Synchronizes contents of the zip package with the target web app using Kudu ZipDeploy.
/// See https://blogs.msdn.microsoft.com/appserviceteam/2017/10/16/zip-push-deployment-for-web-apps-functions-and-webjobs/
let zipDeploy { Url = uri; UserName = username; Password = password; PackageLocation = zipFile } =
    // Create the web request.
    let request =
        Net.HttpWebRequest.Create(uri.AbsoluteUri + "api/zipdeploy",
                                  Method = "POST",
                                  ContentType = "multipart/form-data",
                                  Timeout = 300000) :?> Net.HttpWebRequest

    // Set the authorization header.
    let authToken =
        Convert.ToBase64String(Text.Encoding.ASCII.GetBytes(sprintf "%s:%s" username password))
    request.Headers.Add("Authorization", sprintf "Basic %s" authToken)

    // Write the zip file to the request stream, then flush and close it to send.
    do  use fileStream = new FileStream(zipFile, FileMode.Open)
        use inFile = request.GetRequestStream()
        fileStream.CopyTo(inFile)
        inFile.Flush()
        inFile.Close()

    // Get the response. If 200 OK, then the deploy succeeded. Otherwise, the deploy failed.
    use response = request.GetResponse() :?> Net.HttpWebResponse
    if response.StatusCode = Net.HttpStatusCode.OK then
        logfn "Deployed %s" uri.AbsoluteUri
    else
        failwithf "Failed to deploy package with status code %A" response.StatusCode
