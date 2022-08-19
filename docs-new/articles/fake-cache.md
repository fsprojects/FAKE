# Caching of build scripts

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help!</p>
</div>

Starting with version `4.0.0` of FAKE, the first time a script is run the
compiled assembly that is generated is saved into the hidden `.fake` directory. This
allows FAKE to start in milliseconds instead of seconds. Your script files are
cached with a crc32 key generated from the first scripts contents, and then each
`#load`ed scripts contents. This prevents you from having to manually clear the
cache whenever you are working on your script or pulling in changes from a
remote repository. If for some reason you would like to disable saving the
compiled assembly to disk, you can call FAKE with the `--nocache` argument,
which stops FAKE from dumping the compiled assembly to disk. Do note that the
assembly is still being compiled by FSI, all you are disabling is the saving
to disk.

You should add the `.fake` folder to your `.gitignore` file.
