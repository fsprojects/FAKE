/// Contains tasks to stage and deploy Azure website and webjobs using source code deployment with Kudu Sync.
module Fake.Azure.Kudu

open Fake
open System
open System.IO

/// Location where staged outputs should go before before synced up to the site.
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

/// Stages a set of files into the temp deployment area, ready from deployment into the website.
let stageWebsite files = files |> FileHelper.CopyFiles deploymentTemp

/// Stages a webjob into the temp deployment area, ready for deployment into the website as a webjob.
let stageWebJob webJobType webjobName files =
    let webJobType = match webJobType with Scheduled -> "scheduled" | Continuous -> "continous"
    let webjobPath = sprintf @"%s\app_data\jobs\%s\%s\" deploymentTemp webJobType webjobName
    CreateDir webjobPath
    files |> FileHelper.CopyFiles webjobPath

/// Synchronises all stages files from the temporary deployment to the actual deployment, removing
/// any obsolete files, updating changed files and adding new files.
let kuduSync() =
    let succeeded, output =
        ProcessHelper.ExecProcessRedirected(fun psi ->
            psi.FileName <- combinePaths kuduPath "kudusync.cmd"
            psi.Arguments <- sprintf """-v 50 -f "%s" -t "%s" -n "%s" -p "%s" -i ".git;.hg;.deployment;deploy.cmd""" deploymentTemp deploymentTarget nextManifestPath previousManifestPath)
            (TimeSpan.FromMinutes 5.)
    output |> Seq.iter (fun cm -> printfn "%O: %s" cm.Timestamp cm.Message)
    if not succeeded then failwith "Error occurred during Kudu Sync deployment."