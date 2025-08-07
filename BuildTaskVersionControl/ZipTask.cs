using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using Microsoft.Build.Utilities;
using System.Runtime.InteropServices;
using SymbolicLinkSupport;

namespace BuildTaskVersionControl
{
    /// <summary>
    /// Simple zip task.
    /// </summary>
    public class ZipTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Files to zip. Use metadata 'Path' to overwrite path inside the zip.</summary>
        [Required] public ITaskItem[]? Files { get; set; }

        /// <summary>Path and name of the zip file.</summary>
        [Required] public string? ZipFileName { get; set; }

        /// <summary>Working directory from which the path inside the zip is determined. If path cannot be reached, then file is put in a dot folder. Ignored if metadata 'Path' is set manually.</summary>
        public string? WorkingDirectory { get; set; } = null;

        /// <summary>Suppress all log output.</summary>
        public bool Silent { get; set; } = false;

        /// <summary>
        /// Run task.
        /// </summary>
        public override bool Execute()
        {
            try
            {
                string workingDirectory = Path.GetFullPath(this.WorkingDirectory is null or "" ? "." : this.WorkingDirectory).Replace('\\', '/');
                if (workingDirectory.Length == 0)
                    throw new Exception($"Unable to resolve WorkingDirectory");
                if (workingDirectory[workingDirectory.Length - 1] != '/')
                    workingDirectory += '/';
                LogMsg($"WorkingDirectory is '{workingDirectory}'", MessageImportance.Low);

                using var zip = ZipFile.Create(this.ZipFileName);
                zip.BeginUpdate();
                //zip.CompressionLevel = CompressionLevel.BestCompression;
                foreach (var file in this.Files ?? Enumerable.Empty<ITaskItem>())
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
                        path = path.Replace('\\', '/');
                        bool isLink = false;
                        string fi_fullname = "";
                        var fi = new FileInfo(path);
                        if (dirInZip.Length <= 0)
                        {
                            fi_fullname = fi.FullName.Replace('\\', '/');
                            if (fi_fullname.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
                                dirInZip = fi_fullname.Substring(workingDirectory.Length);
                            else
                                dirInZip = fi.Name;
                        }

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            if (fi.IsSymbolicLink())
                            {
                                isLink = true;
                                path = fi.GetSymbolicLinkTarget().Replace('\\', '/');
                            }
                        }

                        zip.Add(path, dirInZip);
                        LogMsg($"added {(isLink ? "(is link) " : "")}'{path}' or '{fi_fullname}' @ '{dirInZip}'", MessageImportance.Low);
                        return;
                    }
                }

                zip.CommitUpdate();
                zip.Close();
                LogMsg($"Saved zip to '{this.ZipFileName}'");

                return true;
            } catch (Exception e)
            {
                this.Log.LogError($"zip failed with exception: {e}");
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
