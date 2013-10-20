namespace Fake

type ReleaseNotes =
    { AssemblyVersion: string
      NugetVersion: string
      Notes: string list }

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module ReleaseNotes =
    val parseReleaseNotes: data:seq<string> -> ReleaseNotes