namespace Fake.Azure

/// <summary>
/// Contains tasks to package and deploy <seealso href="http://azure.microsoft.com/en-gb/documentation/articles/web-sites-create-web-jobs/">Azure Web Jobs</seealso>
/// via the <seealso href="https://github.com/projectkudu/kudu">Kudu</seealso> Zip controller
/// </summary>
[<RequireQualifiedAccess>]
module WebJobs =

    open System
    open System.IO
    open System.Net.Http
    open Fake.IO
    open Fake.Core

    /// The running modes of webjobs
    [<RequireQualifiedAccess>]
    type WebJobType =
        | Continuous
        | Triggered

    /// WebJob type
    type WebJob =
        {
          /// The name of the web job, this will also be the name out of zip file.
          Name: string
          /// Specifies what type of webjob this is. Note that this also determines it's deployment location on Azure
          JobType: WebJobType
          /// The project to be zipped and deployed as a webjob
          Project: string
          /// The directory path of the webjob to zip
          DirectoryToPackage: string
          /// The package path to once zipped
          PackageLocation: string }

    /// The website that webjobs are deployed to
    type WebSite =
        {
          /// The url of the website, usually in the format of https://<yourwebsite>.scm.azurewebsites.net
          Url: Uri
          /// The FTP username, usually the $username from the site's publish profile
          UserName: string
          /// The FTP Password
          Password: string
          /// The webjobs to deploy to this web site
          WebJobs: WebJob list }

    let private jobTypePath webJobType =
        match webJobType with
        | WebJobType.Continuous -> "continuous"
        | WebJobType.Triggered -> "triggered"

    let private zipWebJob webSite webJob =
        let packageFile = FileInfo.ofPath webJob.PackageLocation
        DirectoryInfo.ensure packageFile.Directory
        let zipName = webJob.PackageLocation

        let filesToZip =
            Directory.GetFiles(webJob.DirectoryToPackage, "*.*", SearchOption.AllDirectories)

        Trace.tracefn "Zipping %s webjob to %s" webJob.Project webJob.PackageLocation
        Zip.createZip webJob.DirectoryToPackage zipName "" 0 false filesToZip

    /// <summary>
    /// This task to can be used create a zip for each webjob to deploy to a website
    /// The output structure is: `outputpath/{websitename}/webjobs/{continuous/triggered}/{webjobname}.zip`
    /// </summary>
    /// 
    /// <param name="webSites">The websites and webjobs to build zips from.</param>
    let packageWebJobs webSites =
        webSites
        |> List.iter (fun webSite -> webSite.WebJobs |> List.iter (zipWebJob webSite))

    let private deployWebJobToWebSite webSite webJob =
        let uploadUri =
            Uri(
                webSite.Url,
                sprintf "api/zip/site/wwwroot/App_Data/jobs/%s/%s" (jobTypePath webJob.JobType) webJob.Name
            )

        let filePath = webJob.PackageLocation
        Trace.tracefn "Deploying %s webjob to %O" filePath uploadUri
        use client = new HttpClient(Timeout = TimeSpan.FromMilliseconds 600000.)

        let authToken =
            Convert.ToBase64String(Text.Encoding.ASCII.GetBytes(webSite.UserName + ":" + webSite.Password))

        client.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Basic", authToken)

        use fileStream = new FileStream(filePath, FileMode.Open)
        use content = new StreamContent(fileStream)
        content.Headers.ContentType <- Headers.MediaTypeHeaderValue("application/zip")

        let response = client.PutAsync(uploadUri, content).Result
        let result = response.Content.ReadAsStringAsync().Result
        Trace.tracefn "Response from webjob upload: %s" result

    let private deployWebJobsToWebSite webSite =
        webSite.WebJobs
        |> List.iter (deployWebJobToWebSite webSite)

    /// <summary>
    /// This task to can be used deploy a prebuilt webjob zip to a website
    /// </summary>
    ///  
    /// <param name="webSites">The websites and webjobs to deploy.</param>
    let deployWebJobs webSites =
        webSites |> List.iter deployWebJobsToWebSite
