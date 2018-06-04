# Convert Gherkin to HTML with Pickles

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="apidocs/v4/fake-pickleshelper.html">here</a></p>
</div>

[Pickles] is a Living Documentation generator: it takes your Specification (written in Gherkin, with Markdown descriptions) and turns them into an always up-to-date documentation of the current state of your software - in a variety of formats.

[API-Reference](apidocs/v5/fake-tools-pickles.html)

## Minimal working example

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.Tools.Pickles
//"

open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing
open Fake.Tools
open System.IO

let currentDirectory = Directory.GetCurrentDirectory()

Target.create "BuildDoc" (fun _ ->
    Pickles.convert (fun p ->
        { p with FeatureDirectory = currentDirectory </> "Specs"
                 OutputDirectory = currentDirectory </> "SpecDocs"
                 OutputFileFormat = Pickles.DocumentationFormat.DHTML })
)

Target.runOrDefault "BuildDoc"
```

[Pickles]: http://www.picklesdoc.com/
