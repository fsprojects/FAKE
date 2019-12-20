namespace Fake.Tools

open Fake.Core
open Fake.IO
open System
open System.IO

/// Helpers for running rsync tool
///
/// Under windows you will need to add it yourself to your system or use something like cygwin/babun
///
/// ## Sample
///
/// > let result =
/// >      Rsync.exec
/// >         (Rsync.Options.WithActions
/// >              [
/// >                  Rsync.Compress
/// >                  Rsync.Archive
/// >                  Rsync.Verbose
/// >                  Rsync.NoOption Rsync.Perms
/// >                  Rsync.Delete
/// >                  Rsync.Exclude ".keep"
/// >              ]
/// >          >> Rsync.Options.WithSources
/// >              [ FleetMapping.server </> "build"
/// >                FleetMapping.server </> "package.json"
/// >                FleetMapping.server </> "yarn.lock" ]
/// >          >> Rsync.Options.WithDestination "remote@myserver.com:deploy")
/// >          ""
/// >
/// > if not result.OK then failwithf "Rsync failed with code %i" result.ExitCode
[<RequireQualifiedAccess>]
module Rsync =

    type Action =
        /// Increase verbosity
        | Verbose
        /// Suppress non-error messages
        | Quiet
        /// Suppress daemon-mode MOTD (see manpage caveat)
        | NoMotd
        /// Skip based on checksum, not mod-time & size
        | Checksum
        /// Archive mode; same as -rlptgoD (no -H)
        | Archive
        /// Turn off an implied OPTION (e.g. --no-D)
        /// NoOption Verbose ==> --no-verbose
        | NoOption of Action
        /// Recurse into directories
        | Recursive
        /// Use relative path names
        | Relative
        /// Don't send implied dirs with --relative
        | NoImpliedDirs
        /// Make backups (see --suffix & --backup-dir)
        | Backup
        /// Make backups into hierarchy based in DIR
        | BackupDir of string
        /// Set backup suffix (default ~ w/o --backup-dir)
        | Suffix of string
        /// Skip files that are newer on the receiver
        | Update
        /// Update destination files in-place (SEE MAN PAGE)
        | InPlace
        /// Append data onto shorter files
        | Append
        /// Transfer directories without recursing
        | Dirs
        /// Copy symlinks as symlinks
        | Links
        /// Transform symlink into referent file/dir
        | CopyLinks
        /// Only "unsafe" symlinks are transformed
        | CopyUnsafeLinks
        /// Ignore symlinks that point outside the source tree
        | SafeLinks
        /// Transform symlink to a dir into referent dir
        | CopyDirLinks
        /// Treat symlinked dir on receiver as dir
        | KeepDirLinks
        /// Preserve hard links
        | HardLinks
        /// Preserve permissions
        | Perms
        /// Preserve the file's executability
        | Executability
        /// Affect file and/or directory permissions
        | Chmod of string
        /// Preserve owner (super-user only)
        | Owner
        /// Preserve group
        | Group
        /// Preserve device files (super-user only)
        | Devices
        /// Preserve special files
        | Specials
        /// Same as --devices --specials
        | D
        /// Preserve times
        | Times
        /// Omit directories when preserving times
        | OmitDirTimes
        /// Receiver attempts super-user activities
        | Super
        /// Handle sparse files efficiently
        | Sparse
        /// Show what would have been transferred
        | DryRun
        /// Copy files whole (without rsync algorithm)
        | WholeFile
        /// Don't cross filesystem boundaries
        | OneFileSystem
        /// Force a fixed checksum block-size
        | BlockSize of string
        /// Specify the remote shell to use
        | Rsh of string
        /// Specify the rsync to run on the remote machine
        | RsyncPath of string
        /// Skip creating new files on receiver
        | Existing
        /// Skip updating files that already exist on receiver
        | IgnoreExisting
        /// Sender removes synchronized files (non-dirs)
        | RemoveSourceFiles
        /// An alias for --delete-during
        | Del
        /// Delete extraneous files from destination dirs
        | Delete
        /// Receiver deletes before transfer (default)
        | DeleteBefore
        /// Receiver deletes during transfer, not before
        | DeleteDuring
        /// Receiver deletes after transfer, not before
        | DeleteAfter
        /// Also delete excluded files from destination dirs
        | DeleteExcluded
        /// Delete even if there are I/O errors
        | IgnoreErrors
        /// Force deletion of directories even if not empty
        | Force
        /// Don't delete more than NUM files
        | MaxDelete of int
        /// Don't transfer any file larger than SIZE
        | MaxSize of string
        /// Don't transfer any file smaller than SIZE
        | MinSize of string
        /// Keep partially transferred files
        | Partial
        /// Put a partially transferred file into DIR
        | PartialDir of string
        /// Put all updated files into place at transfer's end
        | DelayUpdates
        /// Prune empty directory chains from the file-list
        | PruneEmptyDirs
        /// Don't map uid/gid values by user/group name
        | NumericIds
        /// Set I/O timeout in seconds
        | Timeout of int
        /// Don't skip files that match in size and mod-time
        | IgnoreTimes
        /// Skip files that match in size
        | SizeOnly
        /// Compare mod-times with reduced accuracy
        | ModifyWindow of string
        /// Create temporary files in directory DIR
        | TempDir of string
        /// Find similar file for basis if no dest file
        | Fuzzy
        /// Also compare destination files relative to DIR
        | CompareDest of string
        /// ... and include copies of unchanged files
        | CopyDest of string
        /// Hardlink to files in DIR when unchanged
        | LinkDest of string
        /// Compress file data during the transfer
        | Compress
        /// Explicitly set compression level
        | CompressLevel of int
        /// Auto-ignore files the same way CVS does
        | CvsExclude
        /// Add a file-filtering RULE
        | Filter of string
        /// Same as --filter='dir-merge /.rsync-filter'
        /// repeated: --filter='- .rsync-filter'
        | F
        /// Exclude files matching PATTERN
        | Exclude of string
        /// Read exclude patterns from FILE
        | ExcludeFrom of string
        /// Don't exclude files matching PATTERN
        | Include of string
        /// Read include patterns from FILE
        | IncludeFrom of string
        /// Read list of source-file names from FILE
        | FilesFrom of string
        /// All *-from/filter files are delimited by 0s
        | From0
        /// Bind address for outgoing socket to daemon
        | Address of string
        /// Specify double-colon alternate port number
        | Port of int
        /// Specify custom TCP options
        | Sockopts of string
        /// Use blocking I/O for the remote shell
        | BlockingIO
        /// Give some file-transfer stats
        | Stats
        /// Leave high-bit chars unescaped in output
        | HeightBitsOutput
        /// Output numbers in a human-readable format
        | HumanReadable
        /// Show progress during transfer
        | Progress
        /// Same as --partial --progress
        | P
        /// Output a change-summary for all updates
        | ItemizeChanges
        /// Output updates using the specified FORMAT
        | OutFormat of string
        /// Log what we're doing to the specified FILE
        | LogFile of string
        /// Log updates using the specified FMT
        | LogFileFormat of string
        /// Read password from FILE
        | PasswordFile of string
        /// List the files instead of copying them
        | ListOnly
        /// Limit I/O bandwidth; KBytes per second
        | Bwlimit of string
        /// Write a batched update to FILE
        | WriteBatch of string
        /// Like --write-batch but w/o updating destination
        | OnlyWriteBatch of string
        /// Read a batched update from FILE
        | ReadBatch of string
        /// Force an older protocol version to be used
        | Protocol of int
        /// Copy extended attributes
        | ExtendedAttributes
        /// Disable fcntl(F_NOCACHE)
        | Cache
        /// Prefer IPv4
        | Ipv4
        /// Prefer IPv6
        | Ipv6
        /// Print version number
        | Version
        /// Show this help (-h works with no other options)
        | Help

    type Options =
        { /// Command working directory
          WorkingDirectory: string
          Actions : Action list
          Sources : string list
          Destination : string }

        static member Create () =
            { WorkingDirectory = Directory.GetCurrentDirectory()
              Actions = []
              Sources = []
              Destination = "" }

        static member WithActions actions options =
            { options with Actions = actions }

        static member WithSources sources options =
            { options with Sources = sources }

        static member WithDestination dest options =
            { options with Destination = dest }

    let rec private actionToString =
        function
        | Verbose -> "--verbose"
        | Quiet -> "--quit"
        | NoMotd -> "--no-motd"
        | Checksum -> "--checksum"
        | Archive -> "--archive"
        | NoOption action ->
            let action = actionToString action
            "--no-" + action.TrimStart([|'-'|])
        | Recursive -> "--recursive"
        | Relative -> "--relative"
        | NoImpliedDirs -> "--no-implied-dirs"
        | Backup -> "--backup"
        | BackupDir dir -> "--backup-dir=" + dir
        | Suffix suffix -> "--suffix=" + suffix
        | Update -> "--update"
        | InPlace -> "--in-place"
        | Append -> "--append"
        | Dirs -> "--dirs"
        | Links -> "--links"
        | CopyLinks -> "--copy-links"
        | CopyUnsafeLinks -> "--copy-unsafe-links"
        | SafeLinks -> "--safe-links"
        | CopyDirLinks -> "--copy-dirlinks"
        | KeepDirLinks -> "--keep-dirlinks"
        | HardLinks -> "--hard-links"
        | Perms -> "--perms"
        | Executability -> "--executability"
        | Chmod chmod -> "--chmod=" + chmod
        | Owner -> "--owner"
        | Group -> "--group"
        | Devices -> "--devices"
        | Specials -> "--specials"
        | D -> "-D"
        | Times -> "--times"
        | OmitDirTimes -> "--omit-dir-times"
        | Super -> "--super"
        | Sparse -> "--sparse"
        | DryRun -> "--dry-run"
        | WholeFile -> "--whole-file"
        | OneFileSystem -> "--one-file-system"
        | BlockSize size -> "--block-size=" + size
        | Rsh command -> "--rsh=" + command
        | RsyncPath program -> "--rsync-path=" + program
        | Existing -> "--existing"
        | IgnoreExisting -> "--ignore-existing"
        | RemoveSourceFiles -> "--remove-source-files"
        | Del -> "--del"
        | Delete -> "--delete"
        | DeleteBefore -> "--delete-before"
        | DeleteDuring -> "--delete-during"
        | DeleteAfter -> "--delete-after"
        | DeleteExcluded -> "--delete-excluded"
        | IgnoreErrors -> "--ignore-errors"
        | Force -> "--force"
        | MaxDelete num -> "--max-delete=" + string num
        | MaxSize size -> "--max-size=" + size
        | MinSize size -> "--min-size=" + size
        | Partial -> "--partial"
        | PartialDir dir -> "--partial-dir=" + dir
        | DelayUpdates -> "--delay-updates"
        | PruneEmptyDirs -> "--prune-empty-dirs"
        | NumericIds -> "--numeric-ids"
        | Timeout time -> "--timeout=" + string time
        | IgnoreTimes -> "--ignore-times"
        | SizeOnly -> "--size-only"
        | ModifyWindow num -> "--modify-window=" + num
        | TempDir dir -> "--temp-dir" + dir
        | Fuzzy -> "--fuzzy"
        | CompareDest dir -> "--compare-dest=" + dir
        | CopyDest dir -> "--copy-dest=" + dir
        | LinkDest dir -> "--link-dest=" + dir
        | Compress -> "--compress"
        | CompressLevel level -> "--compress-level=" + string level
        | CvsExclude -> "--csv-exclude"
        | Filter rule -> "--filter=" + rule
        | F -> "-F"
        | Exclude pattern -> "--exclude=" + pattern
        | ExcludeFrom file -> "--exclude-from=" + file
        | Include pattern -> "--include=" + pattern
        | IncludeFrom file -> "--include-from=" + file
        | FilesFrom file -> "--files-from=" + file
        | From0 -> "--from0"
        | Address address -> "--address=" + address
        | Port port -> "--port=" + string port
        | Sockopts options -> "--sockopts=" + options
        | BlockingIO -> "--blocking-io"
        | Stats -> "--stats"
        | HeightBitsOutput -> "--8-bit-output"
        | HumanReadable -> "--human-readable"
        | Progress -> "--progress"
        | P -> "-P"
        | ItemizeChanges -> "--itemize-changes"
        | OutFormat format -> "--out-format=" + format
        | LogFile file -> "--log-file=" + file
        | LogFileFormat format -> "--log-file-format=" + format
        | PasswordFile file -> "--password-file=" + file
        | ListOnly -> "--list-only"
        | Bwlimit kbps -> "--bwlimit=" + kbps
        | WriteBatch file -> "--write-bratch=" + file
        | OnlyWriteBatch file -> "--only-write-batch=" + file
        | ReadBatch file -> "--read-batch=" + file
        | Protocol num -> "--protocol=" + string num
        | ExtendedAttributes -> "--extended-attributes"
        | Cache -> "--cache"
        | Ipv4 -> "--ipv4"
        | Ipv6 -> "--ipv6"
        | Version -> "--version"
        | Help -> "--help"

    let private buildCommonArgs (param: Options) =
        (param.Actions |> List.map actionToString)
        @ param.Sources
        @ [ param.Destination ]
        |> String.concat " "

    let exec (buildOptions: Options -> Options) args =
        let results = new System.Collections.Generic.List<Fake.Core.ConsoleMessage>()
        let timeout = TimeSpan.MaxValue

        let errorF msg =
            Trace.traceError msg
            results.Add (ConsoleMessage.CreateError msg)

        let messageF msg =
            Trace.trace msg
            results.Add (ConsoleMessage.CreateOut msg)

        let options = buildOptions (Options.Create())
        let commonOptions = buildCommonArgs options
        let cmdArgs = sprintf "%s %s " commonOptions args

        let result =
            let f (info:ProcStartInfo) =
                { info with
                    FileName = "rsync"
                    WorkingDirectory = options.WorkingDirectory
                    Arguments = cmdArgs }

            Process.execRaw f timeout true errorF messageF
        ProcessResult.New result (results |> List.ofSeq)
