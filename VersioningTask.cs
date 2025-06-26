using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace BuildTaskVersionControl
{
    /// <summary>
    /// Task to read a version string from a file and update it to files.<br/>
    /// Customizeable with regex and has build-in auto increment.
    /// </summary>
    public class VersioningTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Legacy value. Value is added to InputFiles.</summary>
        public ITaskItem? InputFile { get; set; } = null;

        /// <summary>Path to files to extract version string. Only the greatest value is used. Use RegexInput to customize logic.</summary>
        public ITaskItem[] InputFiles { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>Path to files to inject with new version string. Use RegexOutput to customize logic.</summary>
        public ITaskItem[] UpdateFiles { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>Maximum number of lines to match and replace. Matches at least once.</summary>
        public int MaxMatch { get; set; } = 1;

        /// <summary>If true, will increase revision by one before updating files.</summary>
        public bool AutoIncrease { get; set; } = false;

        /// <summary>Update write-date when updating output files.</summary>
        public bool TouchFiles { get; set; } = false;

        /// <summary>Whenever to drop revision before updating files. (never, always, keep)</summary>
        public string DropRevision { get; set; } = "keep";

        /// <summary>Suppress all log output.</summary>
        public bool Silent { get; set; } = false;

        /// <summary>Regex string for the input files. Uses capture group 'version' (or the first capture) and 'suffix' (optional).</summary>
        public string RegexInput { get; set; } = @"(?'version'\d+(?:\.\d+){2,3})(?'suffix'[0-9A-Za-z-]*)";

        /// <summary>Regex string for output files. Is used in conjunction with RegexReplace.</summary>
        public string RegexOutput { get; set; } = @"(?'version'\d+(?:\.\d+){2,3})(?'suffix'[0-9A-Za-z-]*)";

        /// <summary>Replacement string for output files. {version} is replaced by new version and {suffix} is replaced by suffix.</summary>
        public string RegexReplace { get; set; } = @"{version}{suffix}";

        /// <summary>Extracted version string with suffix.</summary>
        [Output] public string? VersionFull { get; private set; }

        /// <summary>Extracted version string without suffix.</summary>
        [Output] public string? Version { get; private set; }

        /// <summary>Extracted version string without revision.</summary>
        [Output] public string? VersionShort { get; private set; }

        /// <summary>Extracted major version.</summary>
        [Output] public int Major { get; private set; }

        /// <summary>Extracted minor version.</summary>
        [Output] public int Minor { get; private set; }

        /// <summary>Extracted build version.</summary>
        [Output] public int Build { get; private set; }

        /// <summary>Extracted revision version.</summary>
        [Output] public int Revision { get; private set; }

        /// <summary>Extracted version suffix.</summary>
        [Output] public string? Suffix { get; private set; }

        /// <summary></summary>
        public enum Operation
        {
            /// <summary></summary>
            Default = 0,
            /// <summary></summary>
            Keep = 0,
            /// <summary></summary>
            Never,
            /// <summary></summary>
            Always,
        }

        private List<ITaskItem> inputs = null!;
        private List<ITaskItem> outputs = null!;
        private Regex rxIn = null!;
        private Regex rxOut = null!;
        private Version version = null!;
        private string suffix = null!;

        /// <summary>
        /// Run task.
        /// </summary>
        public override bool Execute()
        {
            try
            {
                LogMsg($"Start VersioningTask auto={this.AutoIncrease} max={this.MaxMatch} touch={this.TouchFiles}", MessageImportance.Normal);

                inputs = new(this.InputFiles);
                outputs = new(this.UpdateFiles);
                rxIn = new Regex(this.RegexInput, RegexOptions.CultureInvariant);
                rxOut = new Regex(this.RegexOutput, RegexOptions.CultureInvariant);
                version = new();
                suffix = "";

                // try autofill, if no input given
                Autofill();

                // search for greatest input version
                ParseInput();

                if (version == new Version())
                {
                    LogMsg("error: no input version");
                    return false;
                }

                // set output properties; increment revision by 1 if AutoIncrease
                SetOutValues();

                // write to output files
                DoUpdate();

                LogMsg("Version updated!");

                return true;
            }
            catch (Exception e)
            {
                this.Log.LogError(e.Message);
                return false;
            }
        }

        private void Autofill()
        {
            if (this.InputFile != null)
                inputs.Insert(0, this.InputFile);
            else if (inputs.Count <= 0)
            {
                LogMsg($"No input files, searching...", MessageImportance.Low);
                TaskItem item;
                if (File.Exists("changelog.md"))
                {
                    item = new TaskItem("changelog.md");
                    inputs.Add(item);
                    LogMsg($" added changelog.md", MessageImportance.Low);
                }

                if (File.Exists(Path.Combine("Properties", "AssemblyInfo.cs")))
                {
                    item = new TaskItem(Path.Combine("Properties", "AssemblyInfo.cs"));
                    item.SetMetadata("Max", "2");
                    inputs.Add(item);
                    if (this.AutoIncrease)
                        outputs.Add(item);
                    LogMsg($" added assemblyinfo.cs {this.AutoIncrease}", MessageImportance.Low);
                }
                else if (File.Exists(this.BuildEngine?.ProjectFileOfTaskNode) && this.BuildEngine!.ProjectFileOfTaskNode.EndsWith(".csproj"))
                {
                    item = new TaskItem(this.BuildEngine.ProjectFileOfTaskNode);
                    inputs.Add(item);
                    if (this.AutoIncrease)
                        outputs.Add(item);
                    LogMsg($" added {this.BuildEngine.ProjectFileOfTaskNode} {this.AutoIncrease}", MessageImportance.Low);
                }
                else
                {
                    foreach (string path in Directory.EnumerateFiles(".", "*.csproj"))
                    {
                        item = new TaskItem(path);
                        inputs.Add(item);
                        if (this.AutoIncrease)
                            outputs.Add(item);
                        LogMsg($" added {path} {this.AutoIncrease}", MessageImportance.Low);
                    }
                }
            }
        }

        private void ParseInput()
        {
            Capture c;

            foreach (var item in inputs)
            {
                string path = item.ItemSpec;
                if (!File.Exists(path))
                {
                    LogMsg("error: input file doesn't exist " + path);
                    continue;
                }

                // meta override max matches
                if (int.TryParse(item.GetMetadata("MaxMatch"), out int max))
                { }
                else if (int.TryParse(item.GetMetadata("Max"), out max))
                { }
                else
                    max = this.MaxMatch;

                // meta override regex
                string text = item.GetMetadata("Regex");
                Regex rx = text.Length > 0 ? new Regex(text) : rxIn;

                foreach (string line in File.ReadLines(path))
                {
                    var match = rx.Match(line);
                    if (!match.Success)
                        continue;

                    // read either "version" or the first capture
                    if ((c = match.Groups["version"]).Length <= 0)
                        if ((c = match.Groups[1]).Length <= 0)
                            continue;

                    LogMsg($"Parsed entry '{match.Value}' in '{path}'", MessageImportance.Normal);

                    var version2 = new Version(c.Value);
                    if (version2 > version)
                    {
                        version = version2;
                        if ((c = match.Groups["suffix"]).Length > 0)
                            suffix = c.Value;
                    }

                    if (--max <= 0)
                        break;
                }
            }
        }

        private void SetOutValues()
        {
            int revision = this.AutoIncrease ? version.Revision + 1 : Math.Max(0, version.Revision);
            this.VersionShort = $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
            this.Version = $"{this.VersionShort}.{revision}";
            this.Suffix = suffix;
            this.VersionFull = $"{this.Version}{this.Suffix}";
            LogMsg($"Read version as {this.VersionFull}");

            this.Major = version.Major;
            this.Minor = version.Minor;
            this.Build = version.Build;
            this.Revision = version.Revision;

            this.RegexReplace = this.RegexReplace.Replace("{suffix}", this.Suffix);
        }

        private void DoUpdate()
        {
            Capture c;

            foreach (var item in outputs)
            {
                string path = item.ItemSpec;
                if (!File.Exists(path))
                {
                    LogMsg("error: file doesn't exist " + path);
                    continue;
                }

                // meta override max matches
                if (int.TryParse(item.GetMetadata("MaxMatch"), out int max))
                { }
                else if (int.TryParse(item.GetMetadata("Max"), out max))
                { }
                else
                    max = this.MaxMatch;

                // meta override regex
                string text = item.GetMetadata("Regex");
                Regex rx = text.Length > 0 ? new Regex(text) : rxOut;

                // meta override DropRevision
                text = item.GetMetadata("DropRevision");
                Enum.TryParse<Operation>(text.Length > 0 ? text : this.DropRevision, true, out var op);

                // meta override TouchFiles
                text = item.GetMetadata("Touch");
                bool touch = text.Length > 0 ? text.Equals("true", StringComparison.OrdinalIgnoreCase) : this.TouchFiles;

                // meta override replacement
                string replacementOrg = item.GetMetadata("Replacement");
                replacementOrg = replacementOrg.Length > 0 ? replacementOrg.Replace("{suffix}", this.Suffix) : this.RegexReplace;

                // if operation is definitive, replace string now, otherwise do it for each line
                string replacement = "";
                if (op == Operation.Always)
                    replacement = replacementOrg.Replace("{version}", this.VersionShort);
                else if (op == Operation.Never)
                    replacement = replacementOrg.Replace("{version}", this.Version);

                bool save = false;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = rx.Match(lines[i]);
                    if (!match.Success)
                        continue;

                    // if operation is dependent on the match, check now
                    if (op == Operation.Keep)
                    {
                        if ((c = match.Groups["version"]).Length <= 0)
                            c = match.Groups[1];
                        int j = c.Value.Count(f => f == '.');
                        if (j > 0 && j < 3)
                            replacement = replacementOrg.Replace("{version}", this.VersionShort);
                        else
                            replacement = replacementOrg.Replace("{version}", this.Version);
                    }

                    var line = rx.Replace(lines[i], replacement);
                    if (line != lines[i])
                    {
                        lines[i] = line;
                        save = true;
                        LogMsg($"Updated entry in '{path}'", MessageImportance.Normal);
                    }

                    if (--max <= 0)
                        break;
                }

                if (save)
                {
                    var date = File.GetLastWriteTimeUtc(path);
                    File.WriteAllLines(path, lines);
                    if (!touch)
                        File.SetLastWriteTimeUtc(path, date.AddSeconds(1));
                }
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
