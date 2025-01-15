using System;

namespace BuildTaskVersionControl
{
    /// <summary>
    /// Simple sleep task.
    /// </summary>
    public class SleepTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Wait time in milliseconds</summary>
        public int Milliseconds { get; set; } = 300;

        /// <summary>
        /// Run task.
        /// </summary>
        public override bool Execute()
        {
            this.Log.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, $"Waiting for {Milliseconds}ms");
            Thread.Sleep(Milliseconds);
            return true;
        }
    }
}
