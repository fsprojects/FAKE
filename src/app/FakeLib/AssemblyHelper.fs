[<AutoOpen>]
module Fake.AssemblyHelper

open System.Reflection

/// Gets file assembly version in form of major.minor.build.revision.
/// ## Parameters
///  - `assemblyFile` - The assembly file path.
let GetAssemblyVersion (assemblyFile: string) = 
    AssemblyName.GetAssemblyName(assemblyFile).Version.ToString()

