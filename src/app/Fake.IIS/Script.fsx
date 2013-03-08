#r @"..\..\..\packages\Microsoft.Web.Administration.7.0.0.0\lib\net20\Microsoft.Web.Administration.dll"
#r @"..\..\..\build\FakeLib.dll"

#load "IISHelper.fs"
open Fake.IISHelper

let siteName = "fake.site"
let appPool = "fake.appPool"
let port = ":8081:"
let vdir = "/fakevdir"
let appDir = @"C:\Users"

UnlockSection "system.webServer/security/authentication/anonymousauthentication"

(IIS
  (Site siteName "http" port @"C:\inetpub\wwwroot" appPool)
  (ApplicationPool appPool true "v4.0")
  (Some(Application vdir appDir)))

(IIS
  (Site siteName "http" port @"C:\inetpub\wwwroot" appPool)
  (ApplicationPool appPool true "v2.0")
  (Some(Application "/vdir2" @"C:\temp")))

deleteSite siteName
deleteApplicationPool appPool