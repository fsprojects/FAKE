/// Contains tasks to package and deploy [Azure Web Jobs](http://azure.microsoft.com/en-gb/documentation/articles/web-sites-create-web-jobs/) via the [Kudu](https://github.com/projectkudu/kudu) Zip controller
module Fake.AzureWebJobs

open System.IO
open System

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

/// This task to can be used create a zip for each webjob to deploy to a website
/// The output structure is: `outputpath/{websitename}/webjobs/{continuous/triggered}/{webjobname}.zip`
/// ## Parameters
///
///  - `setParams` - Function used to overwrite webjobs outputpath.
///  - `websites` - The websites and webjobs to build zips from.
let BuildZip setParams websites =
    let parameters = setParams WebJobDefaultParams 
    let zipWebJob siteOutputPath webJob = 
        let releaseDirectory = directoryInfo ("src/" + webJob.Project + "/bin/Release")
        let zipDirectory = directoryInfo siteOutputPath
        ensureDirExists zipDirectory
        let zipName = Path.Combine(zipDirectory.FullName, webJob.Name + ".zip")
        let fileToZip = releaseDirectory.GetFiles() |> Array.map (fun f -> f.FullName)
        CreateZip releaseDirectory.FullName zipName "" 0 false fileToZip
    websites |> List.iter (fun website -> 
                    let siteOutputPath = parameters.OutputPath + "/" + website.Url.Host
                    website.WebJobs |> List.iter (zipWebJob siteOutputPath))