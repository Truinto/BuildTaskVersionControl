using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BuildTaskVersionControl
{
    public class VersioningTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Path to file to extract version string. Use RegexInput to customize logic.</summary>
        [Required] public string InputFile { get; set; }

        /// <summary>Path to files to inject with new version string. Use RegexOutput to customize logic.</summary>
        [Required] public ITaskItem[] UpdateFiles { get; set; }

        /// <summary>If true, will increase version by one before updating files. If you use this, it's recommended to include InputFile in UpdateFiles. Does not work with '*' wildcard.</summary>
        public bool AutoIncrease { get; set; } = false;

        /// <summary>Regex string for the input file. Requires group 'version' for version string.</summary>
        public string RegexInput { get; set; } = @"(?<!Manager)(Version.*)(?'version'[\d\.\*]{5,})";

        /// <summary>Regex string for output files. Is used in conjunction with RegexReplace.</summary>
        public string RegexOutput { get; set; } = @"(?<!Manager)(Version.*)(?'version'[\d\.\*]{5,})";

        /// <summary>Replacement string for output files. {version} is replaced by new version.</summary>
        public string RegexReplace { get; set; } = @"${1}{version}";

        /// <summary>Maximum number of lines to match and replace.</summary>
        public int MaxMatch { get; set; } = 0;

        public override bool Execute()
        {
            try
            {
                string version = null;
                string[] lines;
                var rxIn = new Regex(RegexInput);
                var rxOut = new Regex(RegexOutput);
                var rxMinor = new Regex(@"\d+");

                lines = File.ReadAllLines(this.InputFile);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = rxIn.Match(lines[i]);
                    if (!match.Success)
                        continue;

                    version = match.Groups["version"].Value;

                    if (AutoIncrease)
                    {
                        int index = version.LastIndexOf('.') + 1;
                        if (index > 0 && int.TryParse(version.Substring(index), out int rev))
                            version = version.Substring(0, index) + (rev + 1);
                        else
                            LogMsg("error: auto-increase failed");
                    }

                    LogMsg($"Read version as {version}");
                    version = RegexReplace.Replace("{version}", version);
                    break;
                }

                if (version == null || version == "")
                {
                    LogMsg("error: version could not be parsed");
                    return false;
                }

                foreach (var file in UpdateFiles)
                {
                    bool save = false;
                    int counter = 0;
                    lines = File.ReadAllLines(file.ItemSpec);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var match = rxOut.Match(lines[i]);
                        if (!match.Success)
                            continue;

                        var line = rxIn.Replace(lines[i], version);
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
                        File.WriteAllLines(file.ItemSpec, lines);
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
#else
            Log.LogMessage(importance, msg);
#endif
        }
    }
}