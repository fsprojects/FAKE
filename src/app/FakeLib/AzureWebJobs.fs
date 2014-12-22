/// Contains tasks to package and deploy [Azure Web Jobs](http://azure.microsoft.com/en-gb/documentation/articles/web-sites-create-web-jobs/) via the [Kudu](https://github.com/projectkudu/kudu) Zip controller
module Fake.Azure.WebJobs

open Fake
open System.IO
open System
open System.Net

type Uri with
    member this.SubDomain = this.Host.Split([|'.'|],2).[0]

/// The running modes of webjobs
[<RequireQualifiedAccess>]
type WebJobType = 
    | Continuous
    | Triggered

/// WebJob type
type WebJob = 
    { 
      /// The name of the web job, this will also be the name out of zip file.
      Name : string
      /// Specifies what type of webjob this is. Note that this also determines it's deployment location on Azure
      JobType : WebJobType
      /// The project to be zipped and deployed as a webjob
      Project : string }

/// The website that webjobs are deployed to
type WebSite = 
    { 
      /// The url of the website, usually in the format of https://<yourwebsite>.scm.azurewebsites.net
      Url : Uri
      /// The FTP username, usually the $username from the site's publish profile
      UserName : string
      /// The FTP Password
      Password : string
      /// The webjobs to deploy to this web site
      WebJobs : WebJob list }

/// TypeScript task parameter type
type WebJobParams =
    { 
      /// Specifies the zip output path.
      OutputPath : string }

/// Default parameters for the WebJobs task
let WebJobDefaultParams = 
    { OutputPath = null}

let private jobTypePath webJob = 
    match webJob.JobType with
    | WebJobType.Continuous -> "continuous"
    | WebJobType.Triggered -> "triggered"

let private webJobPath outputPath webSite webJob = 
    sprintf "%s/%s/webjobs/%s/%s.zip" outputPath webSite.Url.SubDomain (jobTypePath webJob) webJob.Name

let private zipWebJob outputPath webSite webJob = 
    let releaseDirectory = directoryInfo ("src/" + webJob.Project + "/bin/Release")
    let zipDirectory = directoryInfo (webJobPath outputPath webSite webJob)
    ensureDirExists zipDirectory
    let zipName = Path.Combine(zipDirectory.FullName, webJob.Name + ".zip")
    let fileToZip = releaseDirectory.GetFiles() |> Array.map (fun f -> f.FullName)
    tracefn "Zipping %s webjob to %O" webJob.Project zipDirectory
    CreateZip releaseDirectory.FullName zipName "" 0 false fileToZip

/// This task to can be used create a zip for each webjob to deploy to a website
/// The output structure is: `outputpath/{websitename}/webjobs/{continuous/triggered}/{webjobname}.zip`
/// ## Parameters
///
///  - `setParams` - Function used to overwrite webjobs outputpath.
///  - `webSites` - The websites and webjobs to build zips from.
let BuildZip setParams webSites =
    let parameters = setParams WebJobDefaultParams 
    webSites |> List.iter (fun webSite -> webSite.WebJobs |> List.iter (zipWebJob parameters.OutputPath webSite))

let private deployWebJobToWebSite outputPath webSite webJob =
    let uploadApi = Uri(webSite.Url, sprintf"api/zip/site/wwwroot/App_Data/jobs/%s/%s" (jobTypePath webJob) webJob.Name)
    let filePath = (webJobPath outputPath webSite webJob)
    tracefn "Deploying %s webjob to %O" filePath uploadApi
    use client = new WebClient()

    client.Credentials <-NetworkCredential(webSite.UserName, webSite.Password)
    client.UploadData(uploadApi,"PUT",File.ReadAllBytes(filePath)) |> ignore

let private deployWebJobsToWebSite outputPath webSite = 
    webSite.WebJobs |> List.iter (deployWebJobToWebSite outputPath webSite)

/// This task to can be used deploy a prebuilt webjob zip to a website
/// ## Parameters
///
///  - `setParams` - Function used to overwrite webjobs outputpath.
///  - `webSites` - The websites and webjobs to deploy.
let DeployWebJobs setParams webSites = 
    let parameters = setParams WebJobDefaultParams 
    webSites |> List.iter(deployWebJobsToWebSite parameters.OutputPath)