using System;
using Fake;
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
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '1.2.3.4'\r\n" +
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
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '1.2.3.4'\r\n" +
                    "  spec.summary     = 'My summary'\r\n" +
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
                              defaultParam.WorkingDir);

            GemHelper.CreateGemSpecification(func.Convert())
                .ShouldEqual(
                    "Gem::Specification.new do |spec|\r\n" +
                    "  spec.platform    = Gem::Platform::RUBY\r\n" +
                    "  spec.name        = 'fake'\r\n" +
                    "  spec.version     = '1.2.3.4'\r\n" +
                    "  spec.description = 'My description'\r\n" +
                    "end\r\n");
        }
    }
}