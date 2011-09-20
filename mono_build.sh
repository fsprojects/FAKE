#!/bin/bash

platform='x86'
monopath='unknown'
fscorepath='unknown'
buildpath='build/'
srcfakepath='src/app/FAKE/'
srcfakelibpath='src/app/FakeLib/'
unamestr=`uname`

if [ "$unamestr" == "Darwin" ]; then
	monopath='/Library/Frameworks/Mono.framework/libraries/mono/4.0/'
	fscorepath='/Library/Frameworks/Mono.framework/libraries/mono/4.0/'
elif [ "unamestr" == "Linux" ]; then
	monopath='/usr/lib/mono/4.0/'
	fscorepath='/usr/local/lib/mono/4.0/'
fi

`fsc -o:$buildpath'FakeLib.dll' --debug:pdbonly --noframework --optimize+ --define:BIGINTEGER --platform:$platform -r:$fscorepath'FSharp.Core.dll' -r:'lib/ICSharpCode.SharpZipLib.dll' -r:$monopath'mscorlib.dll' -r:$monopath'System.dll' -r:$monopath'System.Configuration.dll' -r:$monopath'System.Core.dll' -r:$monopath'System.Web.dll' -r:$monopath'System.Xml.dll' -r:$monopath'System.Xml.Linq.dll' --target:library --warn:4 --warnaserror:76 --vserrors --LCID:1033 --utf8output --fullpaths --flaterrors $srcfakelibpath'AssemblyInfo.fs' $srcfakelibpath'FSharpHelpers/SeqExtensions.fs' $srcfakelibpath'FSharpHelpers/OptionExtensions.fs' $srcfakelibpath'FSharpHelpers/AsyncHelper.fs' $srcfakelibpath'RegistryHelper.fs' $srcfakelibpath'FileSystemHelper.fs' $srcfakelibpath'StringHelper.fs' $srcfakelibpath'TemplateHelper.fs' $srcfakelibpath'EnvironmentHelper.fs' $srcfakelibpath'TimeoutHelper.fs' $srcfakelibpath'CacheHelper.fs' $srcfakelibpath'XMLHelper.fs' $srcfakelibpath'REST.fs' $srcfakelibpath'BuildServerHelper.fs' $srcfakelibpath'TraceListener.fs' $srcfakelibpath'TeamCityHelper.fs' $srcfakelibpath'TeamCityRESTHelper.fs' $srcfakelibpath'TraceHelper.fs' $srcfakelibpath'AssemblyInfoHelper.fs' $srcfakelibpath'ProcessHelper.fs' $srcfakelibpath'NCoverHelper.fs' $srcfakelibpath'NCoverHelper.fs' $srcfakelibpath'NUnitHelper.fs' $srcfakelibpath'XUnitHelper.fs' $srcfakelibpath'MSpecHelper.fs' $srcfakelibpath'FileSet.fs' $srcfakelibpath'MSBuildHelper.fs' $srcfakelibpath'ZipHelper.fs' $srcfakelibpath'FileHelper.fs' $srcfakelibpath'FileUtils.fs' $srcfakelibpath'DocuHelper.fs' $srcfakelibpath'ILMergeHelper.fs' $srcfakelibpath'WiXHelper.fs' $srcfakelibpath'NuGetHelper.fs' $srcfakelibpath'VSSHelper.fs' $srcfakelibpath'SCPHelper.fs' $srcfakelibpath'XCopyHelper.fs' $srcfakelibpath'MSBuild/SpecsRemovement.fs' $srcfakelibpath'Git/CommandHelper.fs' $srcfakelibpath'Git/Sha1.fs' $srcfakelibpath'Git/Repository.fs' $srcfakelibpath'Git/Submodule.fs' $srcfakelibpath'Git/Branches.fs' $srcfakelibpath'Git/Reset.fs' $srcfakelibpath'Git/Merge.fs' $srcfakelibpath'Git/Stash.fs' $srcfakelibpath'Git/SanityChecks.fs' $srcfakelibpath'Git/Information.fs' $srcfakelibpath'Git/FileStatus.fs' $srcfakelibpath'Git/Rebase.fs' $srcfakelibpath'Git/CommitMessage.fs' $srcfakelibpath'Git/Staging.fs' $srcfakelibpath'FSIHelper.fs' $srcfakelibpath'MessageHelper.fs' $srcfakelibpath'HTMLHelpWorkShopHelper.fs' $srcfakelibpath'TargetHelper.fs' $srcfakelibpath'AdditionalSyntax.fs' > /dev/null`

`fsc -o:$buildpath'FAKE.exe' --debug:pdbonly --noframework --optimize+ --define:BIGINTEGER --platform:$platform -r:$fscorepath'FSharp.Core.dll' -r:$monopath'mscorlib.dll' -r:$monopath'System.dll' -r:$monopath'System.Core.dll' -r:$buildpath'FakeLib.dll' --target:exe --warn:4 --warnaserror:76 --vserrors --LCID:1033 --utf8output --fullpaths --flaterrors $srcfakepath'AssemblyInfo.fs' $srcfakepath'CommandlineParams.fs' $srcfakepath'Program.fs' > /dev/null`
