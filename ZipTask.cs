using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;
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
                string workingDirectory = new DirectoryInfo(this.WorkingDirectory).FullName.TrimEnd(Path.DirectorySeparatorChar);

                using var zip = new ZipFile();
                zip.CompressionLevel = CompressionLevel.BestCompression;
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
                        LogMsg($"error: file not found: {path}");

                    void addFile(string path)
                    {
                        if (dirInZip.Length > 0)
                        {
                            zip.UpdateFile(path, dirInZip);
                            return;
                        }

                        string fdir = new FileInfo(path).DirectoryName;
                        if (fdir.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))                        
                            zip.UpdateFile(path, fdir.Substring(workingDirectory.Length).Trim(Path.DirectorySeparatorChar));                        
                        else
                            zip.UpdateFile(path, ".");
                    }
                }

                zip.Save(this.ZipFileName);
                LogMsg($"Saved zip to '{this.ZipFileName}'");

                return true;
            }
            catch (Exception e)
            {
                LogMsg($"error: {e.Message}");
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
