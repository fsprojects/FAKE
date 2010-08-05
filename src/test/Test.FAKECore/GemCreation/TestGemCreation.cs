using Fake;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.GemCreation
{
    [TestFixture]
    public class TestGemCreation
    {
        private static readonly GemParams DefaultParam = GemHelper.GemDefaults;

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
            var param =
                new GemParams("naturalspec",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              new FSharpList<string>("Steffen Forkmann",
                                                     new FSharpList<string>("Max Mustermann",
                                                                            FSharpList<string>.Empty)),
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
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
            var param =
                new GemParams("fake",
                              DefaultParam.ToolPath,
                              DefaultParam.Platform,
                              "4.3.2.1",
                              DefaultParam.Summary,
                              DefaultParam.Description,
                              new FSharpList<string>("Steffen Forkmann", FSharpList<string>.Empty),
                              DefaultParam.EMail,
                              DefaultParam.Homepage,
                              DefaultParam.RubyForgeProjectName,
                              DefaultParam.Files,
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
                              new FSharpList<string>("lib/test.text",
                                                     new FSharpList<string>("docs/index.htm",
                                                                            FSharpList<string>.Empty)),
                              DefaultParam.WorkingDir);

            GemHelper.CreateGemSpecificationAsString(param)
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform          = Gem::Platform::RUBY\r\n" +
                    "  spec.name              = 'fake'\r\n" +
                    "  spec.version           = '1.2.3.4'\r\n" +
                    "  spec.files             = [\"lib/test.text\", \"docs/index.htm\"]\r\n" +
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
    }
}