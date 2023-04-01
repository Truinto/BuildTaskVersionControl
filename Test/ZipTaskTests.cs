using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTaskVersionControl;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Moq;

namespace BuildTaskVersionControl.Tests
{
    /// <summary>
    /// Based on https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-test-custom-task?view=vs-2022
    /// </summary>
    [TestClass()]
    public class ZipTaskTests
    {
        private List<BuildErrorEventArgs> Errors;
        private Mock<IBuildEngine> BuildEngine;

        [TestInitialize()]
        public void Startup()
        {
            Console.WriteLine("Startup");
            this.Errors = new List<BuildErrorEventArgs>();
            this.BuildEngine = new Mock<IBuildEngine>();
            this.BuildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => this.Errors.Add(e));
        }

        private TaskItem Ensure(string path)
        {
            var info = new FileInfo(path);
            info.Directory.Create();
            if (info.Name.Length > 0 && !File.Exists(path))
                File.WriteAllText(path, path);
            return new TaskItem(path);
        }

        [TestMethod()]
        public void ExecuteTest1()
        {
            Console.WriteLine("ExecuteTest");

            Ensure(@"sub2\input2.txt");

            var vt = new ZipTask()
            {
                BuildEngine = this.BuildEngine.Object,
                ZipFileName = "zip.zip",
                Files = new ITaskItem[] {
                    Ensure(@"input0.txt"),
                    Ensure(@"sub1\input1.txt"),
                    Ensure(@"sub2\"),
                },
            };
            var success = vt.Execute();
            Console.WriteLine($"Done");

            Assert.IsTrue(success);
            Assert.AreEqual(this.Errors.Count, 0);
        }

        [TestMethod()]
        public void ExecuteTest2()
        {
            Console.WriteLine("ExecuteTest");

            Ensure(@"C:\Temp\folder\sub2\intput2.txt");

            var vt = new ZipTask()
            {
                BuildEngine = this.BuildEngine.Object,
                ZipFileName = @"C:\Temp\zip.zip",
                WorkingDirectory = @"C:\Temp",
                Files = new ITaskItem[] {
                    Ensure(@"C:\Temp\folder\input0.txt"),
                    Ensure(@"C:\Temp\folder\sub1\input1.txt"),
                    Ensure(@"C:\Temp\folder\sub2\"),
                },
            };
            var success = vt.Execute();
            Console.WriteLine($"Done");

            Assert.IsTrue(success);
            Assert.AreEqual(this.Errors.Count, 0);
        }
    }
}