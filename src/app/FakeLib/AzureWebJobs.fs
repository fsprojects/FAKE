/// Contains tasks to package and deploy [Azure Web Jobs](http://azure.microsoft.com/en-gb/documentation/articles/web-sites-create-web-jobs/) via the [Kudu](https://github.com/projectkudu/kudu) Zip controller
module Fake.Azure.WebJobs

open Fake
open System.IO
open System
open System.Net
open System.Collections.Generic
open System.Text

/// The running modes of webjobs
[<RequireQualifiedAccess>]
type WebJobType = 
    | Continuous
    | Triggered

type WebClientWithTimeout() =
    inherit WebClient()
    member val Timeout = 600000 with get, set

    override x.GetWebRequest uri =
        let r = base.GetWebRequest(uri)
        r.Timeout <- x.Timeout
        r

/// WebJob type
type WebJob = 
    { 
      /// The name of the web job, this will also be the name out of zip file.
      Name : string
      /// Specifies what type of webjob this is. Note that this also determines it's deployment location on Azure
      JobType : WebJobType
      /// The project to be zipped and deployed as a webjob
      Project : string
      /// The directory path of the webjob to zip
      DirectoryToPackage : string
      /// The package path to once zipped
      PackageLocation: string }

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

let private jobTypePath webJobType = 
    match webJobType with
    | WebJobType.Continuous -> "continuous"
    | WebJobType.Triggered -> "triggered"

let private zipWebJob webSite webJob = 
    let packageFile = fileInfo webJob.PackageLocation
    ensureDirExists packageFile.Directory
    let zipName = webJob.PackageLocation
    let filesToZip = Directory.GetFiles(webJob.DirectoryToPackage, "*.*", SearchOption.AllDirectories)
    tracefn "Zipping %s webjob to %s" webJob.Project webJob.PackageLocation
    CreateZip webJob.DirectoryToPackage zipName "" 0 false filesToZip

/// This task to can be used create a zip for each webjob to deploy to a website
/// The output structure is: `outputpath/{websitename}/webjobs/{continuous/triggered}/{webjobname}.zip`
/// ## Parameters
///
///  - `webSites` - The websites and webjobs to build zips from.
let PackageWebJobs webSites =
    webSites |> List.iter (fun webSite -> webSite.WebJobs |> List.iter (zipWebJob webSite))

let private deployWebJobToWebSite webSite webJob =
    let uploadUri = Uri(webSite.Url, sprintf "api/%swebjobs/%s" (jobTypePath webJob.JobType) webJob.Name)
    let filePath = webJob.PackageLocation
    tracefn "Deploying %s webjob to %O" filePath uploadUri
    use client = new WebClientWithTimeout(Credentials = NetworkCredential(webSite.UserName, webSite.Password))
    
    client.Headers.Add(HttpRequestHeader.ContentType, "application/zip")
    client.Headers.Add("Content-Disposition", sprintf "attachment; filename=%s" (Path.GetFileName webJob.PackageLocation))
    
    let response = client.UploadFile(uploadUri, "PUT", filePath)
    tracefn "Response from webjob upload: %s" (Encoding.ASCII.GetString response)

let private deployWebJobsToWebSite webSite = 
    webSite.WebJobs |> List.iter (deployWebJobToWebSite webSite)

/// This task to can be used deploy a prebuilt webjob zip to a website
/// ## Parameters
///
///  - `webSites` - The websites and webjobs to deploy.
let DeployWebJobs webSites = 
    webSites |> List.iter deployWebJobsToWebSite  