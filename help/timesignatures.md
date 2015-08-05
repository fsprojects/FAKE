# Keeping track of changed files

The module TimeStamps allows for tracking changed files by checking their last modification time or, in addition, their MD5 signature (MD5 will be computed only if file times are different, for efficiency).

## How to use

Initialize the module by calling initTS or initTSwithName and then use the updateTimestamps or updateMD5 to obtain a list of changed filenames. As an example, the following script will create a zipped archive of the new/changed FSX scripts in its folder.

	// include Fake libs
	#r "FakeLib.dll"
	open Fake
	open Fake.FileHelper
	open System
	open System.IO
	open TimeSignatures

	// helper function for executing processes
	let startProcess fileName args =
		let result =
			ExecProcess (fun info ->
				info.FileName <- fileName
				info.WorkingDirectory <- FullName "."
				info.Arguments <- args) (TimeSpan.FromSeconds(120.0))
		if result <> 0 then
			failwithf "Process '%s' failed with exit code '%d'" fileName result

	// a generic task generator based on file extensions that keeps track of the changed files
	let task_gen proc dir_from dir_to ext_from ext_to x =
	    // collect the desired files according to their file extension
		let files_in = dir_from </> ("*." + ext_from) |> Include |> Seq.toList
		let files_out = files_in |> List.map (fun filename -> changeExt ext_to filename)
		// get the status of the input files
		let uptodate_in = updateMD5 files_in
		traceFAKE "Changed input files: %A" uptodate_in
		// check if there are missing ZIP output files that need to be (re-)created
		let uptodate_out = files_out |> List.filter (fun filename -> not(File.Exists(filename)))
		traceFAKE "Changed output files: %A" uptodate_out
		let uptodate = Set.union (Set.ofList uptodate_in) (Set.ofList uptodate_out)
		traceFAKE "Files needing update: %A" uptodate
		Set.iter (fun file -> ZipFile (changeExt "zip" file) file) uptodate

	let task_zip x =
		task_gen (fun from to_ -> ZipFile to_ from) "." "." "fsx" "zip" x

	let xml = initTS

	Description "Zip changed fsx files"
	TargetTemplate task_zip "task_zip" xml

	Run "task_zip"


To see the module in action try to change the date of a file or its contents.