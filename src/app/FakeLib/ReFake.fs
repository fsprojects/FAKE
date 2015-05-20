module Fake.ReFake

/// Types of ReTargets. They can be 'virtual', 'file', or 'reference'.
type ReTargetType = Vx | Fx | Rx
/// Type that represents an action that's carried out to build a target.
/// It takes the name of the target, which may be a file name; a list of
/// the target's dependencies, which may be empty; and returns the exit
/// status code of the action.
type BuildAction = string -> ReTarget list -> int
/// Type that represents a ReFake target.
and ReTarget =
  { /// The target type
    ReTargetType: ReTargetType
    /// The name of the target. If target type is virtual, this is just
    /// descriptive. Otherwise it's the name of the output file that
    /// will be built.
    Name: string
    /// Other targets that this target depends on. The entire
    /// dependency tree for a target is captured within the target
    /// itself. If the list is empty, the target doesn't have any
    /// dependencies and is at the lowest level in the tree.
    Dependencies: ReTarget list
    /// The build action that will be carried out for the target to be
    /// built.
    Action: BuildAction }

let private targetInfoFunc typ name deps action =
  { ReTargetType = typ
    Name = name
    Dependencies = deps
    Action = action }

/// An empty build action that always succeeds.
let nop _ _ = 0

/// A reference file with no dependencies and no build action. This
/// will usually be a pre-built library that your project depends on.
///
/// ## Parameters
///
/// - `name`: the name of the reference DLL.
///
/// ## Returns
///
/// The DLL file wrapped up as a reference 'target'.
let rx (name: string): ReTarget = targetInfoFunc Rx name [] nop

/// A helper function to create a 'reference' target, one that will be
/// passed in as a reference to an F# compile.
///
/// ## Parameters
///
/// - `name`: the name of the reference target. This will usually become
///   the DLL file name.
/// - `deps`: a list of targets that _this_ target will depend on.
/// - `action`: a function to run to build this target.
///
/// ## Returns
///
/// The reference target.
let rx'
  (name: string)
  (deps: ReTarget list)
  (action: BuildAction): ReTarget =
  targetInfoFunc Rx name deps action

/// A helper function to create a 'virtual' target. A virtual target
/// has the property of always requiring its dependencies to be built.
///
/// ## Parameters
///
/// - `name`: the name of the virtual target. This is usually just for
///   descriptive purposes.
/// - `deps`: a list of targets that _this_ target will depend on.
/// - `action`: a function to run to build this target.
///
/// ## Returns
///
/// The virtual target.
let vx
  (name: string)
  (deps: ReTarget list)
  (action: BuildAction): ReTarget =
  targetInfoFunc Vx name deps action

/// A source file with no dependencies and no build action. Lowest
/// level of dependency that other targets can depend on.
///
/// ## Parameters
///
/// - `name`: the name of the reference target. This is the name of the
///   source file.
///
/// ## Returns
///
/// The source file wrapped up as a file 'target'.
let fx (name: string): ReTarget = targetInfoFunc Fx name [] nop

/// A file target with dependencies and a build action, like an
/// executable output file. A slightly more inconvenient name because
/// this function will probably be used less often than `fx`.
///
/// ## Parameters
///
/// - `name`: the name of the file target. This will usually become the
///   name of the output file.
/// - `deps`: a list of targets that _this_ target will depend on.
/// - `action`: a function to run to build this target.
///
/// ## Returns
///
/// The file target.
let fx'
  (name: string)
  (deps: ReTarget list)
  (action: BuildAction): ReTarget =
  targetInfoFunc Fx name deps action

// Returns a list of the names of the targets.
let private targetsOfType typ =
  List.filter (fun dep -> dep.ReTargetType = typ)
    >> List.map (fun dep -> dep.Name)

/// Returns a list of names of all virtual targets in `targets`.
let vxs (targets: ReTarget list): string list = targetsOfType Vx targets
/// Returns a list of names of all file targets in `targets`.
let fxs (targets: ReTarget list): string list = targetsOfType Fx targets
/// Returns a list of names of all reference targets in `targets`.
let rxs (targets: ReTarget list): string list = targetsOfType Rx targets

/// Do an incremental build.
///
/// ## Parameters
///
/// - `target`: the target to incrementally build.
///
/// ## Returns
///
/// The exit status code of the compile run.
let rec reRun (target: ReTarget): int =
  let name = target.Name
  let deps = target.Dependencies
  let action = target.Action

  let reRunDepsAndBuild () =
    for dep in deps do reRun dep |> ignore
    trace ("ReFake: Building " + name)
    let res = action name deps
    // Exit as soon as possible if anything is wrong.
    if res <> 0 then exit res
    res

  // This does a depth-first traversal of the target's dependency
  // tree, building anything that doesn't exist or was modified.
  match target.ReTargetType with
  | Vx -> reRunDepsAndBuild ()
  | Fx | Rx ->
    // If there are no 'virtual' dependencies, we have a chance to skip
    // this build step.
    if deps |> vxs |> List.isEmpty
      then
        let targetInfo = System.IO.FileInfo(name)

        if not targetInfo.Exists
          then reRunDepsAndBuild ()
          else
            // LaterDeps is a list of all the dependencies which either
            // don't exist or were modified after the target file.
            let laterDeps =
              deps |> List.filter (fun dep ->
                let depInfo = System.IO.FileInfo(dep.Name)
                (not depInfo.Exists)
                  || (depInfo.LastWriteTime > targetInfo.LastWriteTime))

            // No dependencies modified after the target file.
            if laterDeps.IsEmpty
              then 0 // No build necessary.
              // Rebuild everything that's necessary.
              else reRunDepsAndBuild ()
      // If there are 'virtual' dependencies we need to build all of them.
      else reRunDepsAndBuild ()

/// Same as reRun, but immediately exit with the status code returned by
/// the compile process.
let reRunAndExit (target: ReTarget): 'T = reRun target |> exit
