using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using Microsoft.Build.Utilities;

namespace BuildTaskVersionControl
{
    /// <summary>
    /// Simple zip task.
    /// </summary>
    public class ZipTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Files to zip. Use metadata 'Path' to overwrite path inside the zip.</summary>
        [Required] public ITaskItem[] Files { get; set; }

        /// <summary>Path and name of the zip file.</summary>
        [Required] public string ZipFileName { get; set; }

        /// <summary>Working directory from which the path inside the zip is determined. If path cannot be reached, then file is put in a dot folder. Ignored if metadata 'Path' is set manually.</summary>
        public string WorkingDirectory { get; set; } = null;

        /// <summary>Suppress all log output.</summary>
        public bool Silent { get; set; } = false;

        /// <summary>
        /// Run task.
        /// </summary>
        public override bool Execute()
        {
            try
            {
                if (string.IsNullOrEmpty(this.WorkingDirectory))
                    this.WorkingDirectory = ".";
                string workingDirectory = new DirectoryInfo(this.WorkingDirectory).FullName.TrimEnd('/', '\\');

                using var zip = ZipFile.Create(this.ZipFileName);
                zip.BeginUpdate();
                //zip.CompressionLevel = CompressionLevel.BestCompression;
                foreach (var file in this.Files)
                {
                    string path = file.ItemSpec;
                    string dirInZip = file.GetMetadata("Path");

                    if (Directory.Exists(path))
                        foreach (var sub in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                            addFile(sub);
                    else if (File.Exists(path))
                        addFile(path);
                    else
                        this.Log.LogError($"error: file not found: {path}");

                    void addFile(string path)
                    {
                        if (dirInZip.Length <= 0)
                        {
                            var fi = new FileInfo(path);
                            string fi_fullname = fi.FullName;
                            if (fi_fullname.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
                                dirInZip = fi_fullname.Substring(workingDirectory.Length).Trim('/', '\\');
                            else
                                dirInZip = Path.Combine(".", fi.Name);
                        }

                        zip.Add(path, dirInZip);
                        LogMsg($"added '{path}' @ '{dirInZip}'", MessageImportance.Low);
                        return;
                    }
                }

                zip.CommitUpdate();
                zip.Close();
                LogMsg($"Saved zip to '{this.ZipFileName}'");

                return true;
            } catch (Exception e)
            {
                this.Log.LogError($"exception: {e.Message}");
                return false;
            }
        }

        private void LogMsg(string msg, MessageImportance importance = MessageImportance.High)
        {
            if (this.Silent)
                return;

            Debug.WriteLine(msg);
            this.Log.LogMessage(importance, msg);
        }
    }
}
