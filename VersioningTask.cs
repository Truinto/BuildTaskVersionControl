using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BuildTaskVersionControl
{
    /// <summary>
    /// Task to read a version string from a file and update it to files.<br/>
    /// Customizeable with regex and build in auto increment.
    /// </summary>
    public class VersioningTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Path to file to extract version string. Use RegexInput to customize logic.</summary>
        [Required] public string InputFile { get; set; }

        /// <summary>Path to files to inject with new version string. Use RegexOutput to customize logic.</summary>
        [Required] public ITaskItem[] UpdateFiles { get; set; }

        /// <summary>If true, will increase version by one before updating files. If you use this, it's recommended to include InputFile in UpdateFiles. Does not work with '*' wildcard.</summary>
        public bool AutoIncrease { get; set; } = false;

        /// <summary>Regex string for the input file. Requires group 'version' for version string.</summary>
        public string RegexInput { get; set; } = @"(?<!Manager)(Version.*?)(?'version'[\d\.\*]{5,})";

        /// <summary>Regex string for output files. Is used in conjunction with RegexReplace.</summary>
        public string RegexOutput { get; set; } = @"(?<!Manager)(Version.*?)(?'version'[\d\.\*]{5,})";

        /// <summary>Replacement string for output files. {version} is replaced by new version.</summary>
        public string RegexReplace { get; set; } = @"${1}{version}";

        /// <summary>Maximum number of lines to match and replace.</summary>
        public int MaxMatch { get; set; } = 0;

        /// <summary>Update write-date when updating output files.</summary>
        public bool TouchFiles { get; set; } = true;

        /// <summary>Extracted version string.</summary>
        [Output] public string Version { get; private set; }

        /// <summary>Extracted major version.</summary>
        [Output] public int Major { get; private set; }

        /// <summary>Extracted minor version</summary>
        [Output] public int Minor { get; private set; }

        /// <summary>Extracted build version</summary>
        [Output] public int Build { get; private set; }

        /// <summary>Extracted revision version</summary>
        [Output] public int Revision { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public override bool Execute()
        {
            try
            {
                string version = null;
                string[] lines;
                var rxIn = new Regex(RegexInput);
                var rxOut = new Regex(RegexOutput);
                var rxNumber = new Regex(@"\d+");

                if (!File.Exists(this.InputFile))
                {
                    LogMsg("error: input file doesn't exist " + this.InputFile);
                    return false;
                }

                lines = File.ReadAllLines(this.InputFile);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = rxIn.Match(lines[i]);
                    if (!match.Success)
                        continue;

                    version = match.Groups["version"].Value;
                    if (version == null && version == "")
                        continue;

                    if (AutoIncrease)
                    {
                        int index = version.LastIndexOf('.') + 1;
                        if (index > 0 && int.TryParse(version.Substring(index), out int rev))
                            version = version.Substring(0, index) + (rev + 1);
                        else
                            LogMsg("error: auto-increase failed");
                    }

                    match = rxNumber.Match(version);
                    if (match.Success)
                    {
                        Major = int.Parse(match.Value);
                        match = match.NextMatch();
                    }
                    if (match.Success)
                    {
                        Minor = int.Parse(match.Value);
                        match = match.NextMatch();
                    }
                    if (match.Success)
                    {
                        Build = int.Parse(match.Value);
                        match = match.NextMatch();
                    }
                    if (match.Success)
                    {
                        Revision = int.Parse(match.Value);
                    }

                    this.Version = version;
                    LogMsg($"Read version as {version}");
                    version = RegexReplace.Replace("{version}", version);
                    break;
                }

                if (version == null || version == "")
                {
                    LogMsg("error: version could not be parsed");
                    return false;
                }

                foreach (var file in this.UpdateFiles)
                {
                    if (!File.Exists(file.ItemSpec))
                    {
                        LogMsg("error: file doesn't exist " + file.ItemSpec);
                        continue;
                    }

                    bool save = false;
                    int counter = 0;
                    lines = File.ReadAllLines(file.ItemSpec);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var match = rxOut.Match(lines[i]);
                        if (!match.Success)
                            continue;

                        var line = rxOut.Replace(lines[i], version);
                        if (line != lines[i])
                        {
                            lines[i] = line;
                            save = true;
                            LogMsg($"Updated entry in '{file.ItemSpec}'", MessageImportance.Normal);
                        }

                        if (++counter >= MaxMatch && MaxMatch > 0)
                            break;
                    }

                    if (save)
                    {
                        var date = File.GetLastWriteTimeUtc(file.ItemSpec);
                        File.WriteAllLines(file.ItemSpec, lines);
                        if (!TouchFiles)
                            File.SetLastWriteTimeUtc(file.ItemSpec, date.AddSeconds(2));
                    }
                }

                LogMsg("Version updated!");

                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.Message);
                return false;
            }
        }

        private void LogMsg(string msg, MessageImportance importance = MessageImportance.High)
        {
#if DEBUG
            Debug.WriteLine(msg);
            //Log.LogMessage(importance, msg);
#else
            Log.LogMessage(importance, msg);
#endif
        }
    }
}