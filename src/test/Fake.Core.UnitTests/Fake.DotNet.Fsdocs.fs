module Fake.DotNet.FsdocsTests

open Fake.DotNet
open Expecto

[<Tests>]
let tests =
    testList
        "Fake.DotNet.Fsdocs.Tests"
        [ testCase "It append input parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Input = Some("docs-new") }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --input docs-new' command with input parameter"
              |> Expect.equal cmd "--input docs-new"

          testCase "It append projects parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with
                      Projects =
                          Some(
                              seq {
                                  "src/app/Fake.Api.GitHub/Fake.Api.GitHub.fsproj"
                                  "src/app/Fake.DotNet.Fsdocs/Fake.DotNet.Fsdocs.fsproj"
                              }
                          ) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --projects src/app/Fake.Api.GitHub/Fake.Api.GitHub.fsproj,src/app/Fake.DotNet.Fsdocs/Fake.DotNet.Fsdocs.fsproj' command with projects parameter"
              |> Expect.equal
                  cmd
                  "--projects src/app/Fake.Api.GitHub/Fake.Api.GitHub.fsproj src/app/Fake.DotNet.Fsdocs/Fake.DotNet.Fsdocs.fsproj"

          testCase "It append output parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Output = Some("site") }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --output site' command with output parameter"
              |> Expect.equal cmd "--output site"

          testCase "It append noapidocs parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with NoApiDocs = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --noapidocs' command with noapidocs parameter"
              |> Expect.equal cmd "--noapidocs"

          testCase "It append eval parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Eval = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --eval' command with eval parameter"
              |> Expect.equal cmd "--eval"

          testCase "It append saveimages parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with SaveImages = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --saveimages' command with saveimages parameter"
              |> Expect.equal cmd "--saveimages"

          testCase "It append linenumbers parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with LineNumbers = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --linenumbers' command with linenumbers parameter"
              |> Expect.equal cmd "--linenumbers"

          testCase "It append ignoreprojects parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with IgnoreProjects = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --ignoreprojects' command with ignoreprojects parameter"
              |> Expect.equal cmd "--ignoreprojects"

          testCase "It append qualify parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Qualify = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --qualify' command with qualify parameter"
              |> Expect.equal cmd "--qualify"

          testCase "It append parameters parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with
                      Parameters =
                          Some(
                              seq {
                                  ("root", "http://127.0.0.1:5500/")
                                  ("fsdocs-logo-src", "http://127.0.0.1:5500/img/logo.svg")
                              }
                          ) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --parameters root http://127.0.0.1:5500/ fsdocs-logo-src http://127.0.0.1:5500/img/logo.svg' command with parameters parameter"
              |> Expect.equal
                  cmd
                  "--parameters root http://127.0.0.1:5500/ fsdocs-logo-src http://127.0.0.1:5500/img/logo.svg"

          testCase "It append nonpublic parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with NoPublic = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --nonpublic' command with nonpublic parameter"
              |> Expect.equal cmd "--nonpublic"

          testCase "It append nodefaultcontent parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with NoDefaultContent = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --nodefaultcontent' command with nodefaultcontent parameter"
              |> Expect.equal cmd "--nodefaultcontent"

          testCase "It append clean parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Clean = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --clean' command with clean parameter"
              |> Expect.equal cmd "--clean"

          testCase "It append version parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Version = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --version' command with version parameter"
              |> Expect.equal cmd "--version"

          testCase "It append properties parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Properties = Some("Configuration=Release") }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --properties Configuration=Release' command with properties parameter"
              |> Expect.equal cmd "--properties Configuration=Release"

          testCase "It append fscoptions parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with FscOptions = Some("-r:MyAssembly.dll") }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --fscoptions Configuration=Release' command with fscoptions parameter"
              |> Expect.equal cmd "--fscoptions -r:MyAssembly.dll"

          testCase "It append strict parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with Strict = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --strict' command with strict parameter"
              |> Expect.equal cmd "--strict"

          testCase "It append sourcefolder parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with SourceFolder = Some("src/app") }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --sourcefolder src/app' command with sourcefolder parameter"
              |> Expect.equal cmd "--sourcefolder src/app"

          testCase "It append sourcerepo parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with SourceRepository = Some("FAKE") }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --sourcerepo FAKE' command with sourcerepo parameter"
              |> Expect.equal cmd "--sourcerepo FAKE"

          testCase "It append mdcomments parameter when its value is overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with MdComments = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --mdcomments' command with mdcomments parameter"
              |> Expect.equal cmd "--mdcomments"

          testCase "It append multiple parameters to build command when values are overriden"
          <| fun _ ->
              let buildParams: Fsdocs.BuildCommandParams =
                  { Fsdocs.BuildCommandParams.Default with
                      Clean = Some(true)
                      NoDefaultContent = Some(true)
                      Parameters =
                          Some(
                              seq {
                                  ("root", "http://127.0.0.1:5500/")
                                  ("fsdocs-logo-src", "http://127.0.0.1:5500/img/logo.svg")
                              }
                          )
                      SaveImages = Some(true)
                      Strict = Some(true) }

              let cmd = Fsdocs.buildBuildCommandParams buildParams

              "expected to call 'build --saveimages --parameters root http://127.0.0.1:5500/ fsdocs-logo-src http://127.0.0.1:5500/img/logo.svg --nodefaultcontent --clean --strict' command with multiple parameters"
              |> Expect.equal
                  cmd
                  "--saveimages --parameters root http://127.0.0.1:5500/ fsdocs-logo-src http://127.0.0.1:5500/img/logo.svg --nodefaultcontent --clean --strict"

          testCase "It calls watch command without any parameters when using default parameters"
          <| fun _ ->
              let cmd = Fsdocs.buildWatchCommandParams Fsdocs.WatchCommandParams.Default

              "expected to call 'watch' command without any parameters" |> Expect.equal cmd ""

          testCase "It append noserver parameter when its value is overriden"
          <| fun _ ->
              let cmd =
                  Fsdocs.buildWatchCommandParams { Fsdocs.WatchCommandParams.Default with NoServer = Some(true) }

              "expected to call 'watch --noserver' command with noserver parameter"
              |> Expect.equal cmd "--noserver"

          testCase "It append nolaunch parameter when its value is overriden"
          <| fun _ ->
              let cmd =
                  Fsdocs.buildWatchCommandParams { Fsdocs.WatchCommandParams.Default with NoLaunch = Some(true) }

              "expected to call 'watch --nolaunch' command with nolaunch parameter"
              |> Expect.equal cmd "--nolaunch"

          testCase "It append open parameter when its value is overriden"
          <| fun _ ->
              let cmd =
                  Fsdocs.buildWatchCommandParams
                      { Fsdocs.WatchCommandParams.Default with Open = Some("http://localhost:/apidocs/index.html") }

              "expected to call 'watch --open http://localhost:/apidocs/index.html' command with open parameter"
              |> Expect.equal cmd "--open http://localhost:/apidocs/index.html"

          testCase "It append port parameter when its value is overriden"
          <| fun _ ->
              let cmd =
                  Fsdocs.buildWatchCommandParams { Fsdocs.WatchCommandParams.Default with Port = Some(3007) }

              "expected to call 'watch --port 3007' command with port parameter"
              |> Expect.equal cmd "--port 3007"

          testCase "It append multiple parameters to watch command when values are overriden"
          <| fun _ ->
              let watchParams: Fsdocs.WatchCommandParams =
                  { Fsdocs.WatchCommandParams.Default with
                      Port = Some(3007)
                      Open = Some("http://localhost:/apidocs/index.html") }

              let cmd = Fsdocs.buildWatchCommandParams watchParams

              "expected to call 'watch --open http://localhost:/apidocs/index.html --port 3007' command with multiple parameters"
              |> Expect.equal cmd "--open http://localhost:/apidocs/index.html --port 3007" ]
