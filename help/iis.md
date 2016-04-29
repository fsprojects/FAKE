# FAKE.IIS

FAKE.IIS provides extensions around the Microsoft.Web.Administration library to provide clean interfaces to easily configure IIS Sites and Application Pools.

## Installing

FAKE.IIS is a separate package that can be installed from NuGet.

<div class="row">
  <div class="span1"></div>
  <div class="span6">
      <pre>PM> Install-Package FAKE.IIS</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>


## Creating a ApplicationPool and Site

    #r "Fake.IIS.dll"
    #r "Microsoft.Web.Administration.dll"

    open Fake.IISHelper

    let siteName = "fake.site"
    let appPoolName = "fake.appPool"
    let port = ":80:"
    let path =  @"C:\inetpub\wwwroot"

    let siteConfig = SiteConfig(siteName, port, path, appPoolName)
    let appPoolConfig = ApplicationPoolConfig(appPoolName)

    (IIS
      (Site siteConfig)
      (ApplicationPool appPoolConfig)
      (None))

## Deleting an Application Pool and Site

    #r "Fake.IIS.dll"

    open Fake.IISHelper

    let siteName = "fake.site"
    let appPoolName = "fake.appPool"

    deleteSite siteName
    deleteApplicationPool appPoolName
