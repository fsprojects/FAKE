/// Contains tasks to package and deploy [Azure Web Jobs](http://azure.microsoft.com/en-gb/documentation/articles/web-sites-create-web-jobs/) via the [Kudu](https://github.com/projectkudu/kudu) Zip controller
module Fake.AzureWebJobs

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
      Url : string
      /// The FTP username, usually the $username from the site's publish profile
      UserName : string
      /// The FTP Password
      Password : string
      /// The webjobs to deploy to this web site
      WebJobs : WebJob list }
