# Keeping track of changed files

The module TimeStamps allows for tracking changed files by checking their last modification time or, in addition, their MD5 signature (these will not be checked whenever file times are different, in order to speed-up computations).

## Tracking files changes

Initialize the module by calling initTS or initTSwithName and then use the updateTimestamps or updateMD5 to

	// include Fake libs
	#r "FakeLib.dll"

	// helper function for processes
	let startProcess fileName args =
		let result =
			ExecProcess (fun info ->
				info.FileName <- fileName
				info.WorkingDirectory <- FullName "."
				info.Arguments <- args) (TimeSpan.FromSeconds(120.0))
		if result <> 0 then
			failwithf "Process '%s' failed with exit code '%d'" fileName result

	// a generic task generator based on file extensions
	let task_gen proc dir_from dir_to ext_from ext_to x =
		let files_out = Seq.toList (Include(dir_from </> ("*." + ext_from)))
		if not(List.forall (fun value -> value) (TimeSignatures.updateTimestamps files_out)) then
			files_out 
			|> Seq.iter (fun file_in ->
								let file_name = filename file_in
								let file_in = ext_from </> file_name
								let file_out = dir_to </> (changeExt ext_to file_name)
								traceFAKE "%s will be processed" file_out
								startProcess proc (sprintf " %s %s" file_in file_out))

	let task_zip x =
		let files = Seq.toList(Include("*.log"))
		let uptodate = TimeSignatures.updateTimestamps files
		let files = List.filter (fun (file, uptodate_file) -> uptodate_file) (List.zip files uptodate)
		List.iter (fun (file, _) -> ZipFile file (changeExt "zip" file)) files


	let xml = TimeSignatures.initTS

	Description "Zip changed log files"
	TargetTemplate task_zip "task_zip" xml

	RunTargetOrDefault "task_zip"

To see the module in action try to change the date of a file or its content.