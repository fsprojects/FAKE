using System;
using Fake;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using Test.Git;

namespace Test.FAKECore.GemCreation
{
    [TestFixture]
    public class TestGemCreation
    {
        [Test]
        public void CanCreateGemSpecifactionWithVersion()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("fake",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "1.2.3.4",
                              defaultParam.Summary,
                              defaultParam.Description,
                              defaultParam.Authors,
                              defaultParam.EMail,
                              defaultParam.Homepage,
                              defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '1.2.3.4'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndSummary()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("fake",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "1.2.3.4",
                              "My summary",
                              defaultParam.Description,
                              defaultParam.Authors,
                              defaultParam.EMail,
                              defaultParam.Homepage,
                              defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '1.2.3.4'\r\n" +
                    "  spec.summary     = 'My summary'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndDescription()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("fake",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "1.2.3.4",
                              defaultParam.Summary,
                              "My description",
                              defaultParam.Authors,
                              defaultParam.EMail,
                              defaultParam.Homepage,
                              defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '1.2.3.4'\r\n" +
                    "  spec.description = 'My description'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndSingleAuthor()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("fake",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "4.3.2.1",
                              defaultParam.Summary,
                              defaultParam.Description,
                              new FSharpList<string>("Steffen Forkmann", FSharpList<string>.Empty),
                              defaultParam.EMail,
                              defaultParam.Homepage,
                              defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '4.3.2.1'\r\n" +
                    "  spec.authors           = 'Steffen Forkmann'\r\n" +
                    "  spec.rubyforge_project = 'fake'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndMultipleAuthors()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("naturalspec",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "4.3.2.1",
                              defaultParam.Summary,
                              defaultParam.Description,
                              new FSharpList<string>("Steffen Forkmann",
                              new FSharpList<string>("Max Mustermann",
                                  FSharpList<string>.Empty)),
                                  defaultParam.EMail,
                                  defaultParam.Homepage,
                                  defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'naturalspec'\r\n" +
                    "  spec.version     = '4.3.2.1'\r\n" +
                    "  spec.authors           = [\"Steffen Forkmann\", \"Max Mustermann\"]\r\n" +
                    "  spec.rubyforge_project = 'naturalspec'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndEMail()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("naturalspec",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "4.3.2.1",
                              defaultParam.Summary,
                              defaultParam.Description,
                              defaultParam.Authors,
                              "test@test.com",
                              defaultParam.Homepage,
                              defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'naturalspec'\r\n" +
                    "  spec.version     = '4.3.2.1'\r\n" +
                    "  spec.email             = 'test@test.com'\r\n" +
                    "  spec.rubyforge_project = 'naturalspec'\r\n" +
                    "end\r\n");
        }      
        
        [Test]
        public void CanCreateGemSpecifactionWithVersionAndHomepage()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("naturalspec",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "4.3.2.1",
                              defaultParam.Summary,
                              defaultParam.Description,
                              defaultParam.Authors,
                              defaultParam.EMail,
                              "http://github.com/forki/fake",
                              defaultParam.RubyForgeProjectName,
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'naturalspec'\r\n" +
                    "  spec.version     = '4.3.2.1'\r\n" +
                    "  spec.homepage          = 'http://github.com/forki/fake'\r\n" +
                    "  spec.rubyforge_project = 'naturalspec'\r\n" +
                    "end\r\n");
        }

        [Test]
        public void CanCreateGemSpecifactionWithVersionAndDifferentRubyForgeProjectName()
        {
            Func<GemParams, GemParams> func =
                defaultParam =>
                new GemParams("naturalspec",
                              defaultParam.ToolPath,
                              defaultParam.Platform,
                              "4.3.2.1",
                              defaultParam.Summary,
                              defaultParam.Description,
                              defaultParam.Authors,
                              defaultParam.EMail,
                              defaultParam.Homepage,
                              "naturalspec2",
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'naturalspec'\r\n" +
                    "  spec.version     = '4.3.2.1'\r\n" +
                    "  spec.rubyforge_project = 'naturalspec2'\r\n" +
                    "end\r\n");
        }
    }
}