using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTaskVersionControl;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Moq;

namespace BuildTaskVersionControlTests
{
    /// <summary>
    /// Based on https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-test-custom-task?view=vs-2022
    /// </summary>
    [TestClass]
    public class ZipTaskTests
    {
        private List<BuildMessageEventArgs> Messages = null!;
        private List<BuildErrorEventArgs> Errors = null!;
        private Mock<IBuildEngine> BuildEngine = null!;

        [TestInitialize]
        public void Startup()
        {
            Console.WriteLine("Startup");
            this.Messages = new();
            this.Errors = new();
            this.BuildEngine = new Mock<IBuildEngine>();
            this.BuildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(this.Errors.Add);
            this.BuildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback<BuildMessageEventArgs>(this.Messages.Add);
        }

        private TaskItem Ensure(string path)
        {
            var info = new FileInfo(path);
            info.Directory?.Create();
            if (info.Name.Length > 0 && !File.Exists(path))
                File.WriteAllText(path, path);
            return new TaskItem(path);
        }

        [TestMethod]
        public void ExecuteTest1()
        {
            Console.WriteLine("ExecuteTest");

            Ensure(@"sub2\input2.txt");

            var vt = new ZipTask()
            {
                BuildEngine = this.BuildEngine.Object,
                ZipFileName = "zip.zip",
                WorkingDirectory = null,
                Files = [
                    Ensure(@"input0.txt"),
                    Ensure(@"sub1\input1.txt"),
                    Ensure(@"sub2\"),
                    Ensure(@"..\@Testb\input0b.txt"),
                ],
            };
            var success = vt.Execute();
            foreach (var e in this.Messages)
                Console.WriteLine($"{e.Message}");
            Console.WriteLine($"Done {success}:{this.Errors.Count}");
            foreach (var e in this.Errors)
                Console.WriteLine($"{e.File}:{e.LineNumber} {e.Message}");

            Assert.IsTrue(success);
            Assert.AreEqual(0, this.Errors.Count);
        }

        [TestMethod]
        public void ExecuteTest2()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Console.WriteLine("Test skipped, because not on Windows");
                return;
            }

            Console.WriteLine("ExecuteTest");

            Ensure(@"C:\Temp\folder\sub2\input2.txt");

            var vt = new ZipTask()
            {
                BuildEngine = this.BuildEngine.Object,
                ZipFileName = @"C:\Temp\zip.zip",
                WorkingDirectory = @"C:\Temp",
                Files = [
                    Ensure(@"C:\Temp\folder\input0.txt"),
                    Ensure(@"C:\Temp\folder\sub1\input1.txt"),
                    Ensure(@"C:\Temp\folder\sub2\"),
                ],
            };
            var success = vt.Execute();

            foreach (var e in this.Messages)
                Console.WriteLine($"{e.Message}");
            Console.WriteLine($"Done {success}:{this.Errors.Count}");
            foreach (var e in this.Errors)
                Console.WriteLine($"{e.File}:{e.LineNumber} {e.Message}");

            Assert.IsTrue(success);
            Assert.AreEqual(0, this.Errors.Count);
        }
    }
}
