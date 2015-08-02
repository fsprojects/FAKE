module FsCheck.Fake.TestNuget

open System
open global.Xunit
open Fake

// taken from http://www.nuget.org/api/v2/Packages()?$filter=(Id%20eq%20%27FAKE%27)%20and%20IsLatestVersion
let xmlForFirstEntryFromNugetOrgPackageList = """<?xml version="1.0" encoding="utf-8"?><feed xml:base="http://www.nuget.org/api/v2/" xmlns="http://www.w3.org/2005/Atom" xmlns:d="http://schemas.microsoft.com/ado/2007/08/dataservices" xmlns:m="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"><id>http://www.nuget.org/api/v2/Packages</id><title type="text">Packages</title><updated>2015-04-29T16:34:33Z</updated><link rel="self" title="Packages" href="Packages" /><entry><id>http://www.nuget.org/api/v2/Packages(Id='FAKE',Version='3.30.0')</id><category term="NuGetGallery.V2FeedPackage" scheme="http://schemas.microsoft.com/ado/2007/08/dataservices/scheme" /><link rel="edit" title="V2FeedPackage" href="Packages(Id='FAKE',Version='3.30.0')" /><title type="text">FAKE</title><summary type="text">FAKE - F# Make - Get rid of the noise in your build scripts.</summary><updated>2015-04-29T16:29:58Z</updated><author><name>Steffen Forkmann,  Mauricio Scheffer,  Colin Bull</name></author><link rel="edit-media" title="V2FeedPackage" href="Packages(Id='FAKE',Version='3.30.0')/$value" /><content type="application/zip" src="http://www.nuget.org/api/v2/package/FAKE/3.30.0" /><m:properties><d:Version>3.30.0</d:Version><d:NormalizedVersion>3.30.0</d:NormalizedVersion><d:Copyright m:null="true" /><d:Created m:type="Edm.DateTime">2015-04-29T07:55:25.17</d:Created><d:Dependencies></d:Dependencies><d:Description>FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#. This package bundles all extensions.</d:Description><d:DownloadCount m:type="Edm.Int32">162456</d:DownloadCount><d:GalleryDetailsUrl>http://www.nuget.org/packages/FAKE/3.30.0</d:GalleryDetailsUrl><d:IconUrl>https://raw.githubusercontent.com/fsharp/FAKE/master/help/pics/logo.png</d:IconUrl><d:IsLatestVersion m:type="Edm.Boolean">true</d:IsLatestVersion><d:IsAbsoluteLatestVersion m:type="Edm.Boolean">true</d:IsAbsoluteLatestVersion><d:IsPrerelease m:type="Edm.Boolean">false</d:IsPrerelease><d:Language>en-US</d:Language><d:Published m:type="Edm.DateTime">2015-04-29T07:55:25.17</d:Published><d:PackageHash>tVt0qOybftvi01x3envu9tPO9wErXVHv+zKMay+7h2RRJHIAkA/GgP3XzWlf1tgUQEnd0F/pW+izcSaiOQkG8Q==</d:PackageHash><d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm><d:PackageSize m:type="Edm.Int64">13414832</d:PackageSize><d:ProjectUrl>http://www.github.com/fsharp/Fake</d:ProjectUrl><d:ReportAbuseUrl>http://www.nuget.org/package/ReportAbuse/FAKE/3.30.0</d:ReportAbuseUrl><d:ReleaseNotes>FCS simplification - https://github.com/fsharp/FAKE/pull/773
Paket push task runs in parallel - https://github.com/fsharp/FAKE/pull/768</d:ReleaseNotes><d:RequireLicenseAcceptance m:type="Edm.Boolean">false</d:RequireLicenseAcceptance><d:Tags>build fake f#</d:Tags><d:Title m:null="true" /><d:VersionDownloadCount m:type="Edm.Int32">136</d:VersionDownloadCount><d:MinClientVersion m:null="true" /><d:LastEdited m:type="Edm.DateTime" m:null="true" /><d:LicenseUrl>http://www.github.com/fsharp/Fake/blob/master/License.txt</d:LicenseUrl><d:LicenseNames m:null="true" /><d:LicenseReportUrl m:null="true" /></m:properties></entry></feed>"""

[<Fact>]
let ``Can parse package information from nuget.org stream`` () = 
    let xml = XMLDoc xmlForFirstEntryFromNugetOrgPackageList
    let firstEntry = xml.DocumentElement.GetElementsByTagName("entry").Item(0)
    let result = extractFeedPackageFromXml firstEntry
    let expected = { 
        Id = "FAKE"
        Version = "3.30.0"
        Authors = "Steffen Forkmann,  Mauricio Scheffer,  Colin Bull"
        Owners = "Steffen Forkmann,  Mauricio Scheffer,  Colin Bull"
        Url = "http://www.nuget.org/api/v2/package/FAKE/3.30.0"
        IsLatestVersion = true
        Created = DateTime.Parse("2015-04-29T07:55:25.17")
        Published = DateTime.Parse("2015-04-29T07:55:25.17")
        PackageHash = "tVt0qOybftvi01x3envu9tPO9wErXVHv+zKMay+7h2RRJHIAkA/GgP3XzWlf1tgUQEnd0F/pW+izcSaiOQkG8Q=="
        PackageHashAlgorithm = "SHA512"
        LicenseUrl = "http://www.github.com/fsharp/Fake/blob/master/License.txt"
        ProjectUrl = "http://www.github.com/fsharp/Fake"
        RequireLicenseAcceptance = false
        Description = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#. This package bundles all extensions."
        Language = "en-US"
        ReleaseNotes = "FCS simplification - https://github.com/fsharp/FAKE/pull/773
Paket push task runs in parallel - https://github.com/fsharp/FAKE/pull/768" 
        Tags = "build fake f#" }

    Assert.Equal<string>(expected.Id, result.Id)
    Assert.Equal<string>(expected.Version, result.Version)
    Assert.Equal<string>(expected.Authors, result.Authors)
    Assert.Equal<string>(expected.Owners, result.Owners)
    Assert.Equal<string>(expected.Url, result.Url)
    Assert.Equal<bool>(expected.IsLatestVersion, result.IsLatestVersion)
    Assert.Equal<DateTime>(expected.Created, result.Created)
    Assert.Equal<DateTime>(expected.Published, result.Published)
    Assert.Equal<string>(expected.PackageHash, result.PackageHash)
    Assert.Equal<string>(expected.PackageHashAlgorithm, result.PackageHashAlgorithm)
    Assert.Equal<string>(expected.LicenseUrl, result.LicenseUrl)
    Assert.Equal<string>(expected.ProjectUrl, result.ProjectUrl)
    Assert.Equal<bool>(expected.RequireLicenseAcceptance, result.RequireLicenseAcceptance)
    Assert.Equal<string>(expected.Description, result.Description)
    Assert.Equal<string>(expected.Language, result.Language)
    Assert.Equal<string>(expected.Tags, result.Tags)
    Assert.Equal<string>(expected.ReleaseNotes, result.ReleaseNotes)
    