using System;
using System.IO;
using Fake;
using NUnit.Framework;
using Test.FAKECore.FileHandling;
using Test.Git;

namespace Test.FAKECore
{
    [TestFixture]
    public class TestRelativePaths : BaseTest
    {
        [Test]
        public void CanGetRelativePathOfCurrentDirectory()
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory);

            StringHelper.toRelativePath(di.FullName).
                ShouldEqual(".");
        }

        [Test]
        public void CanGetRelativePathOfFileSubDirectory()
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory + @"\Test1\Test2\text.txt");

            StringHelper.toRelativePath(di.FullName).
                ShouldEqual(@".\Test1\Test2\text.txt");
        }

        [Test]
        public void CanGetRelativePathOfParentDirectory()
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory).Parent;

            StringHelper.toRelativePath(di.FullName).
                ShouldEqual(@"..");
        }

        [Test]
        public void CanGetRelativePathOfParentOfParentDirectory()
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent;

            StringHelper.toRelativePath(di.FullName).
                ShouldEqual(@"..\..");
        }

        [Test]
        public void CanGetRelativePathOfSubDirectory()
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory + @"\Test1\Test2");

            StringHelper.toRelativePath(di.FullName).
                ShouldEqual(@".\Test1\Test2");
        }


        [Test]
        public void CanGetRelativePathOfTestDirectory()
        {
            var di = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent;

            StringHelper.toRelativePath(di.FullName + "\\Test1").
                ShouldEqual(@"..\..\Test1");
        }
    }
}