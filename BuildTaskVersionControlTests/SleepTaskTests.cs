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
            this._Build_Engine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(this._Errors.Add);
            this._Build_Engine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback<BuildMessageEventArgs>(this._Messages.Add);
        }

        [TestMethod]
        public void ExecuteTest1()
        {
            Console.WriteLine("ExecuteTest");

            var item = new Mock<ITaskItem>();
            item.Setup(x => x.GetMetadata("Identity")).Returns($".\\Resources\\complete-prop.setting");

            var vt = new SleepTask()
            {
                BuildEngine = this._Build_Engine.Object,
                Milliseconds = 1000
            };
            var sw = new Stopwatch();
            sw.Start();
            var success = vt.Execute();
            sw.Stop();
            foreach (var e in this._Messages)
                Console.WriteLine($"{e.Message}");
            this._Messages.Clear();
            Console.WriteLine($"Elapsed time is {sw.ElapsedMilliseconds}ms");

            vt = new SleepTask()
            {
                BuildEngine = this._Build_Engine.Object,
                Milliseconds = 300
            };
            sw.Restart();
            var success2 = vt.Execute();
            sw.Stop();
            foreach (var e in this._Messages)
                Console.WriteLine($"{e.Message}");
            this._Messages.Clear();
            Console.WriteLine($"Elapsed time is {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"Done {success && success2}:{this._Errors.Count}");
            foreach (var e in this._Errors)
                Console.WriteLine($"{e.File}:{e.LineNumber} {e.Message}");

            Assert.IsTrue(success);
            Assert.IsTrue(success2);
            Assert.AreEqual(0, this._Errors.Count);
        }
    }
}
