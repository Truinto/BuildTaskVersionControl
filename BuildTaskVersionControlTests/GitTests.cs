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
        private List<BuildMessageEventArgs> _Messages = null!;
        private List<BuildErrorEventArgs> _Errors = null!;
        private Mock<IBuildEngine> _Build_Engine = null!;

        [TestInitialize]
        public void Startup()
        {
            Console.WriteLine("Startup");
            this._Messages = new();
            this._Errors = new();
            this._Build_Engine = new Mock<IBuildEngine>();
            this._Build_Engine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => this._Errors.Add(e));
            this._Build_Engine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback<BuildMessageEventArgs>(this._Messages.Add);
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
                BuildEngine = this._Build_Engine.Object,
                Url = "https://github.com/Truinto/BuildTaskVersionControl.git/",
                Interval = "0.00:00",
                DownloadOnChange = [item1, item2],
                Force = true
            };
            var success = vt.Execute();
            Console.WriteLine($"Done {vt.NeedsUpdate}");

            foreach (var e in this._Messages)
                Console.WriteLine($"{e.Message}");
            Console.WriteLine($"Done {success}:{this._Errors.Count} NeedsUpdate={vt.NeedsUpdate}");
            foreach (var e in this._Errors)
                Console.WriteLine($"{e.File}:{e.LineNumber} {e.Message}");

            Assert.IsTrue(success);
            Assert.AreEqual(0, this._Errors.Count);
        }
    }
}
