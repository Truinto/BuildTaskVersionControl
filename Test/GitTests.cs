using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTaskVersionControl;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Moq;
using System.Reflection;

namespace BuildTaskVersionControl.Tests
{
    /// <summary>
    /// Based on https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-test-custom-task?view=vs-2022
    /// </summary>
    [TestClass()]
    public class GitTests
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

        [TestMethod()]
        public void ExecuteTest()
        {
            Console.WriteLine("ExecuteTest");

            var item1 = new TaskItem("Downloads/README.md");
            item1.SetMetadata("Url", "https://github.com/Truinto/BuildTaskVersionControl/blob/master/README.md");
            var item2 = new TaskItem("Downloads/BuildTaskVersionControlTests.csproj");
            item2.SetMetadata("Url", "Test/#(Filename)#(Extension)");

            var vt = new GitRemoteTask()
            {
                BuildEngine = this.BuildEngine.Object,
                Url = "https://github.com/Truinto/BuildTaskVersionControl.git/",
                Interval = "0.00:00",
                DownloadOnChange = new ITaskItem[] { item1, item2 },
                Force = true
            };
            var success = vt.Execute();
            Console.WriteLine($"Done {vt.NeedsUpdate}");

            Assert.IsTrue(success);
            Assert.AreEqual(this.Errors.Count, 0);
        }
    }
}