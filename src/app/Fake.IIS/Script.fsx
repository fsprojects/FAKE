#r @"..\..\..\packages\Microsoft.Web.Administration.7.0.0.0\lib\net20\Microsoft.Web.Administration.dll"
#r @"bin\Debug\FakeLib.dll"

#load "IISHelper.fs"
open Fake.IISHelper

(IIS
  (Site "Fake.Deploy.Web" "http" ":8081:" @"D:\PublishedSites\Fake.Web.Deploy")
  (ApplicationPool "Fake.Deploy.Pool")
  None)
