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
    public class VersioningTaskTests
    {
        private List<BuildMessageEventArgs> Messages;
        private List<BuildErrorEventArgs> Errors;
        private Mock<IBuildEngine> BuildEngine;

        [TestInitialize()]
        public void Startup()
        {
            Console.WriteLine("Startup");
            this.Messages = new();
            this.Errors = new();
            this.BuildEngine = new Mock<IBuildEngine>();
            this.BuildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => this.Errors.Add(e));
            this.BuildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback<BuildMessageEventArgs>(this.Messages.Add);
        }

        [TestMethod()]
        public void ExecuteTest()
        {
            Console.WriteLine("ExecuteTest");

            if (!File.Exists("input.txt"))
                File.WriteAllText("input.txt", "[assembly: AssemblyVersion(\"1.2.3\")]\r\n[assembly: AssemblyFileVersion(\"1.2.3\")]");
            if (!File.Exists("output.txt"))
                File.WriteAllText("output.txt", "[assembly: AssemblyVersion(\"1.0.0\")]\r\n[assembly: AssemblyFileVersion(\"1.0.0\")]");

            var item = new Mock<ITaskItem>();
            item.Setup(x => x.GetMetadata("Identity")).Returns($".\\Resources\\complete-prop.setting");
            var vt = new VersioningTask() 
            {
                BuildEngine = this.BuildEngine.Object,
                InputFile = new TaskItem("input.txt"),
                UpdateFiles = [new TaskItem("input.txt"), new TaskItem("output.txt")],
                AutoIncrease = true,
                MaxMatch = 1,
            };
            var success = vt.Execute();
            Console.WriteLine($"Output: {vt.Version} = {vt.Major}.{vt.Minor}.{vt.Build}.{vt.Revision}");

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
