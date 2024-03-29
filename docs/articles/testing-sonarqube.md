# Analyze your code with SonarQube

[SonarQube](http://sonarqube.org) platform is an open source quality management platform, dedicated to continuously analyzing and 
measuring the technical quality of source code, from project portfolio down to the method level

It can analyze a lot of different programming languages, from PHP, Erlang, CSS to Cobol. C# can be installed
with an additional plugin. This must be done on the SonarQube server. 
To support the analysis process on a build server, an additional command line tool called "MSBuild.SonarQube.Runner.exe"
must be used. The SonarQube module in FAKE provides a function 'SonarQube' to call this tool with the needed parameters.

This function must be called twice, once at the beginning of the compilation process and once after
compilation has finished. The result is then collected and sent to the SonarQube server.


## Minimal working example

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.Testing.SonarQube //"
open Fake.Core
open Fake.Core.TargetOperators
open Fake.Testing

Target.create "BeginSonarQube" (fun _ ->
  SonarQube.start (fun p ->
    {p with
      Key = "MyProject"
      Name = "Main solution"
      Version = "1.0.0" }
    )
  )

Target.create "EndSonarQube" (fun _ ->
  SonarQube.finish None
)

Target.create "Default" DoNothing

"Clean"
  ==> "SetAssemblyInfo"
  ==> "BeginSonarQube"
  ==> "Build" <=> "BuildTests"
  ==> "EndSonarQube"
  ==> "RunTests"
  ==> "Deploy"
  ==> "Default"

Target.runOrDefault "Default"
```

By default, the SonarQube module looks for the MSBuild runner in the `tools/SonarQube` directory. This can be overwritten 
using the ToolsPath property of the parameters.

## Additional options for SonarQube

* You can send additional global settings  to the server with the `/d:` parameter.
In the SonarQubeParams, this is the new field Settings:

```fsharp
SonarQube.start (fun p ->
  {p with
    Key = "MyProject"
    Name = "Main solution"
    Version = "1.0.0" 
    Settings = ["sonar.debug"; "sonar.newversion"] }
  )
```

* Configuration can also be read from a configuration file. This is the `/s:` parameter.
This can be done with the new field Config:

```fsharp
SonarQube.start (fun p ->
  {p with
    Key = "MyProject"
    Name = "Main solution"
    Version = "1.0.0" 
    Config = Some("myconfig.cfg") }
  )
```
