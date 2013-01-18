#r @"..\..\..\packages\Microsoft.Web.Administration.7.0.0.0\lib\net20\Microsoft.Web.Administration.dll"
#r @"bin\Debug\FakeLib.dll"

#load "IISHelper.fs"
open Fake.IISHelper

let siteName = "fake.site"
let appPool = "fake.appPool"
let port = ":8081:"
let vdir = "/fakevdir"
let appDir = @"C:\work\subversion\Sunshine\trunk\SunshineGUI\src\Sunshine.Web"

deleteSite siteName
deleteApplicationPool appPool

(IIS
  (Site siteName "http" port @"C:\inetpub\wwwroot" appPool)
  (ApplicationPool appPool)
  (Some(Application vdir appDir)))