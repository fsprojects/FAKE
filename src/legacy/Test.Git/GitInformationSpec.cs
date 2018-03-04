using System;
using Machine.Specifications;
using Fake.Git;

namespace Test.Git
{
	public class when_getting_git_information
	{
		It should_be_able_to_get_the_git_version = 
			() => Information
				.extractGitVersion("git version 2.4.9")
				.ShouldEqual("2.4.9");

		It should_be_able_to_get_the_git_version_apple = 
			() => Information
				.extractGitVersion("git version 2.4.9 (Apple Git-60)")
				.ShouldEqual("2.4.9");

		It should_be_able_to_get_the_git_silly = 
			() => Information
				.extractGitVersion("git version 400.44312.9 (Apple Git-60)")
				.ShouldEqual("400.44312.9");
	}
}

