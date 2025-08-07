using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BuildTaskVersionControlTests
{
    /// <summary>
    /// see https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-test-custom-task?view=vs-2022
    /// </summary>
    [TestClass]
    public class IntegrationTests
    {
        public const string MSBUILD = "C:/Program Files/dotnet/dotnet.exe";
        public const string MSBUILDARGS = "build --nologo -p:SolutionDir=../";
        public const string WORKINGDIR = "../../../../DummyProject/";
        public Process BuildProcess = null!;

        [TestInitialize()]
        public void Startup()
        {
            BuildProcess = new Process();
            BuildProcess.StartInfo = new()
            {
                FileName = MSBUILD,
                Arguments = MSBUILDARGS,
                CreateNoWindow = true,
                WorkingDirectory = WORKINGDIR,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            BuildProcess.OutputDataReceived += (sender, args) => { Console.WriteLine(args?.Data); };
            BuildProcess.ErrorDataReceived += (sender, args) => { Console.WriteLine(args?.Data); };
        }

        [TestCleanup()]
        public void Cleanup()
        {
            BuildProcess.Close();
        }

        [TestMethod]
        public void ExecuteTest()
        {
            Console.WriteLine("Start process");
            BuildProcess.Start();
            BuildProcess.BeginOutputReadLine();
            BuildProcess.BeginErrorReadLine();
            BuildProcess.WaitForExit();
            Console.WriteLine("Finished process");

            Assert.AreEqual(0, BuildProcess.ExitCode);
        }
    }
}
