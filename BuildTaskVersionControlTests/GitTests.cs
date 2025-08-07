using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTaskVersionControl;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Moq;
using System.Reflection;

namespace BuildTaskVersionControlTests
{
    /// <summary>
    /// Based on https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-test-custom-task?view=vs-2022
    /// </summary>
    [TestClass]
    public class GitTests
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
            this.BuildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => this.Errors.Add(e));
            this.BuildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback<BuildMessageEventArgs>(this.Messages.Add);
        }

        [TestMethod]
        public void ExecuteTest()
        {
            Console.WriteLine("ExecuteTest");

            var item1 = new TaskItem("Downloads/README.md");
            item1.SetMetadata("Url", "https://github.com/Truinto/BuildTaskVersionControl/blob/master/README.md");
            var item2 = new TaskItem("Downloads/BuildTaskVersionControlTests.csproj");
            item2.SetMetadata("Url", "BuildTaskVersionControlTests/#(Filename)#(Extension)");

            var vt = new GitRemoteTask()
            {
                BuildEngine = this.BuildEngine.Object,
                Url = "https://github.com/Truinto/BuildTaskVersionControl.git/",
                Interval = "0.00:00",
                DownloadOnChange = [item1, item2],
                Force = true
            };
            var success = vt.Execute();
            Console.WriteLine($"Done {vt.NeedsUpdate}");

            foreach (var e in this.Messages)
                Console.WriteLine($"{e.Message}");
            Console.WriteLine($"Done {success}:{this.Errors.Count} NeedsUpdate={vt.NeedsUpdate}");
            foreach (var e in this.Errors)
                Console.WriteLine($"{e.File}:{e.LineNumber} {e.Message}");

            Assert.IsTrue(success);
            Assert.AreEqual(0, this.Errors.Count);
        }
    }
}
