# FAKE.IIS

FAKE.IIS provides extensions around the Microsoft.Web.Administration library to provide clean interfaces to easily configure IIS Sites and Application Pools.

## Creating a ApplicationPool and Site
    #r "Fake.IIS.dll"
    #load "IISHelper.fs"

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

    #load "IISHelper.fs"

    open Fake.IISHelper

    let siteName = "fake.site"
    let appPoolName = "fake.appPool"

    deleteSite siteName
    deleteApplicationPool appPool
