using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTaskVersionControl;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Moq;
using System.Diagnostics;

namespace BuildTaskVersionControlTests
{
    /// <summary>
    /// Based on https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-test-custom-task?view=vs-2022
    /// </summary>
    [TestClass]
    public class SleepTaskTests
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

        [TestMethod]
        public void ExecuteTest1()
        {
            Console.WriteLine("ExecuteTest");

            var item = new Mock<ITaskItem>();
            item.Setup(x => x.GetMetadata("Identity")).Returns($".\\Resources\\complete-prop.setting");

            var vt = new SleepTask()
            {
                BuildEngine = this.BuildEngine.Object,
                Milliseconds = 1000
            };
            var sw = new Stopwatch();
            sw.Start();
            var success = vt.Execute();
            sw.Stop();
            foreach (var e in this.Messages)
                Console.WriteLine($"{e.Message}");
            this.Messages.Clear();
            Console.WriteLine($"Elapsed time is {sw.ElapsedMilliseconds}ms");

            vt = new SleepTask()
            {
                BuildEngine = this.BuildEngine.Object,
                Milliseconds = 300
            };
            sw.Restart();
            var success2 = vt.Execute();
            sw.Stop();
            foreach (var e in this.Messages)
                Console.WriteLine($"{e.Message}");
            this.Messages.Clear();
            Console.WriteLine($"Elapsed time is {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"Done {success && success2}:{this.Errors.Count}");
            foreach (var e in this.Errors)
                Console.WriteLine($"{e.File}:{e.LineNumber} {e.Message}");

            Assert.IsTrue(success);
            Assert.IsTrue(success2);
            Assert.AreEqual(0, this.Errors.Count);
        }
    }
}
