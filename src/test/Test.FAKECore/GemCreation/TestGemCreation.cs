using System;
using System.Collections.Generic;
using Fake;
using Microsoft.FSharp.Core;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.GemCreation
{
    [TestFixture]
    public class TestGemCreation
    {
        private static readonly GemParams DefaultParam = GemHelper.GemDefaults;

        [Test]
        public void CanCreateGemSpecifactionWithDependencies()
        {
            var dependencies =
                new List<Tuple<string, FSharpOption<string>>>
                    {
                        Tuple.Create("naturalspec", FSharpOption<string>.None),
                        Tuple.Create("nunit", FSharpOption<string>.Some(">= 1.4.5.6"))
                    };

            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "1.2.3.4",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              dependencies.ToFSharpList(),
                              @"C:\test\");

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.add_dependency('naturalspec')\r\n" +
                    "  spec.add_dependency('nunit', '>= 1.4.5.6')\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersion()
        {
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "1.2.3.4",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndDescription()
        {
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "1.2.3.4",
                              DefaultParam.Summary,
                              "My description",
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.description       = 'My description'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndDifferentRubyForgeProjectName()
        {
            var param =
                new GemParams("naturalspec",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              "naturalspec2",
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'naturalspec'\r\n" +
                    "  spec.version           = '4.3.2.1'\r\n" +
                    "  spec.rubyforge_project = 'naturalspec2'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndEMail()
        {
            var param =
                new GemParams("naturalspec",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              "test@test.com",
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'naturalspec'\r\n" +
                    "  spec.version           = '4.3.2.1'\r\n" +
                    "  spec.email             = 'test@test.com'\r\n" +
                    "  spec.rubyforge_project = 'naturalspec'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndHomepage()
        {
            var param =
                new GemParams("naturalspec",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              "http://github.com/forki/fake",
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'naturalspec'\r\n" +
                    "  spec.version           = '4.3.2.1'\r\n" +
                    "  spec.homepage          = 'http://github.com/forki/fake'\r\n" +
                    "  spec.rubyforge_project = 'naturalspec'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndMultipleAuthors()
        {
            var authors = new List<string> {"Steffen Forkmann", "Max Mustermann"};
            var param =
                new GemParams("naturalspec",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              authors.ToFSharpList(),
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'naturalspec'\r\n" +
                    "  spec.version           = '4.3.2.1'\r\n" +
                    "  spec.authors           = [\"Steffen Forkmann\", \"Max Mustermann\"]\r\n" +
                    "  spec.rubyforge_project = 'naturalspec'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndSingleAuthor()
        {
            var authors = new List<string> {"Steffen Forkmann"};
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              authors.ToFSharpList(),
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '4.3.2.1'\r\n" +
                    "  spec.authors           = 'Steffen Forkmann'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndSomeFiles()
        {
            var files =
                new List<string>
                    {
                        @".\lib\test.text",
                        @".\docs\index.htm"
                    };
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "1.2.3.4",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              files.ToFSharpList(),
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.files             = [\"./lib/test.text\", \"./docs/index.htm\"]\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }


        [Test]
        public void CanCreateGemSpecifactionWithVersionAndSummary()
        {
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "1.2.3.4",
                              "My summary",
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
                              DefaultParam.Dependencies,
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.summary           = 'My summary'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanReplaceWorkingDir()
        {
            var files =
                new List<string>
                    {
                        @"C:\test\lib\test.text",
                        @"C:\test\docs\index.htm"
                    };
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "1.2.3.4",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              DefaultParam.Authors,
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              files.ToFSharpList(),
                              DefaultParam.Dependencies,
                              @"C:\test\");

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.files             = [\"./lib/test.text\", \"./docs/index.htm\"]\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }
    }
}