# Fake Template

The Fake template bootstraps FAKE and sets up a basic build-script.

## Installation

Run
```shell
dotnet new -i "fake-template::*"
```
to install or update the template.

## Usage
After you have installed the template you can setup FAKE by running:
```shell
dotnet new fake
```
This will create a default `build.fsx` file and two shell scripts `fake.sh` and `fake.cmd`. The shell scripts are used to bootstrap 
and run FAKE. All of the arguments are passed directly to FAKE so you can run:
```shell
.\fake.cmd build
```
to run your build. Have a look at the [*fake commandline*](/guide/commandline.html) for the available command-line options. 
For additional information on how to use a build script, checkout the 
[*getting started*](/guide/fake-gettingstarted.html#Example-Compiling-and-building-your-NET-application) page.

## Options

### --script-name
Specifies the name of the generated build-script. Defaults to `build.fsx`.

### --bootstrap
Specifies your preferred way to bootstrap FAKE.

- `local` (default) - Installs the FAKE dotnet sdk local tool into the `dotnet-tools.json` manifest. This requires dotnet SDK version 3 or higher.
- `tool` - Installs the FAKE dotnet sdk global tool into the `--tool-path` folder
- `none` - Does not bootstrap FAKE. Use this if you want to use a global installation of FAKE

### --dependencies
Specifies your preferred way to define the nuget packages used in your build:

- `inline` (default) - Defines build dependencies inside the build script
- `file` - Creates a `paket.dependencies` file to define build dependencies
- `none` - Use this if you already have a `paket.dependencies` in your folder

### --dsl
Specifies your preferred way to define build tasks inside your build script:

- `fake` (default) - Uses the default FAKE domain specific language
- `buildtask` - Uses a string free domain specific language, called [*BuildTask*](https://github.com/vbfox/FoxSharp/blob/master/src/BlackFox.Fake.BuildTask/Readme.md)

### --tool-path
Specifies the folder for the fake-cli tool. This parameter is only applicable when `tool` option is used for bootstrapping with `--bootstrap`. Defaults to `.fake`.

### --version
Specifies the version of FAKE to install. Defaults to `5.*`. This parameter is only applicable when either `local` or `tool` is used for bootstrapping.
