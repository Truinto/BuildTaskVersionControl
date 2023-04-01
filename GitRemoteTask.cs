using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildTaskVersionControl
{
    /// <summary>
    /// Task to read remote repository commit history and download files on change.
    /// </summary>
    public class GitRemoteTask : Microsoft.Build.Utilities.Task
    {
        /// <summary>Url of remote repository. May or may not contain '.git' extension.</summary>
        [Required] public string Url { get; set; }

        /// <summary>Branch or branch path. Will try to find HEAD, if empty string. Examples: "master", "refs/heads/master"</summary>
        public string RepoPath { get; set; } = "refs/heads/master";

        /// <summary>TimeSpan to wait between remote check. Format: [days].[hours]:[minutes]</summary>
        /// <example>"1.12:00" (1 day 12 hours)</example>
        public string Interval { get; set; } = "0.18:00";

        /// <summary>
        /// Collection of files to download or update, whenever the repository's Id changes.<br/>
        /// Set metadata 'Url' to an url to download from.<br/>
        /// The url can include #(Filename)#(Extension) to copy the filename and extension from the file path.<br/>
        /// If a relative name is given, then the url is taken from the repository Url (Github).<br/>
        /// </summary>
        public ITaskItem[] DownloadOnChange { get; set; } = Array.Empty<ITaskItem>(); // "https://github.com/Truinto/DarkCodex/raw/master/CodexLib/CodexLib.dll"; // "CodexLib/CodexLib.dll"

        /// <summary>File last update date is saved to.</summary>
        public ITaskItem CachePath { get; set; } = new TaskItem("obj/remote.cache");

        /// <summary>Whenever to throw an error, if remote connection fails.</summary>
        public bool SurpressErrors { get; set; } = false;

        /// <summary>Whenever to force download, even if repository is unchanged.</summary>
        public bool Force { get; set; } = false;

        /// <summary>Suppress all log output.</summary>
        public bool Silent { get; set; } = false;

        /// <summary>Latest commit id (usually hash).</summary>
        [Output] public string Id { get; set; }

        /// <summary>True if Id has changed.</summary>
        [Output] public bool NeedsUpdate { get; set; }

        /// <summary>
        /// Run task.
        /// </summary>
        public override bool Execute()
        {
            // C:\Program Files\Git\bin\git.exe
            //git ls-remote --heads --quiet https://github.com/Truinto/DarkCodex.git/ master

            try
            {
                // check if files are missing
                this.Force |= this.DownloadOnChange.Any(a => !File.Exists(a.ItemSpec));

                // check if update is necessary
                string[] cache = ReadCache(this.CachePath.ItemSpec);
                if (!this.Force
                    && TimeSpan.TryParse(this.Interval, out TimeSpan interval)
                    && DateTime.TryParse(cache[0], null, DateTimeStyles.RoundtripKind, out DateTime lastTime))
                {
                    var diff = interval - (DateTime.UtcNow - lastTime);
                    if (diff.Ticks > 0)
                    {
                        LogMsg($"Skipped GitRemoteTask. Next update in {diff}");
                        this.Id = cache[1];
                        return true;
                    }
                }

                // get id (which is also the last commit hash)
                ParseId();

                if (string.IsNullOrEmpty(this.Id) && !this.Force)
                {
                    LogMsg("error: could not parse remote repository identifier.");
                    return this.SurpressErrors;
                }

                // check if commit id is different
                if (this.Id != cache[1] || this.Force)
                {
                    this.NeedsUpdate = true;
                    if (!Download())
                    {
                        LogMsg("Not all downloads successful");
                        return this.SurpressErrors;
                    }
                }

                cache[0] = DateTime.UtcNow.ToString("o");
                cache[1] = this.Id;
                new FileInfo(this.CachePath.ItemSpec).Directory.Create();
                File.WriteAllLines(this.CachePath.ItemSpec, cache);

                LogMsg("Remote update done");
                return true;
            }
            catch (Exception e)
            {
                LogMsg($"error: {e.Message}");
                return this.SurpressErrors;
            }
        }

        private string[] ReadCache(string path)
        {
            string[] cache = new string[2] { "", "" };
            if (!File.Exists(path))
                return cache;

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length && i < cache.Length; i++)
                cache[i] = lines[i];
            return cache;
        }

        private void ParseId()
        {
            // get repo path; if empty then copy from HEAD; adds 'refs/heads/' if no slashes found
            string repoPath = this.RepoPath;
            if (repoPath.Length > 0 && !repoPath.Contains("/"))
                repoPath = "refs/heads/" + repoPath;

            // connect with remote repo and enumerate through references (mostly HEAD and tags) until target branch is found
            LogMsg($"Connecting to remote repository '{this.Url}' with branch '{repoPath}'", MessageImportance.Normal);
            /*foreach (var remote in LibGit2Sharp.Repository.ListRemoteReferences(this.Url))
            {
                LogMsg($"{remote.CanonicalName} id={remote.TargetIdentifier}", MessageImportance.Low);
                if (repoPath == "" && remote.CanonicalName == "HEAD")
                    repoPath = remote.TargetIdentifier;
                if (remote.CanonicalName == repoPath)
                {
                    this.Id = remote.TargetIdentifier;
                    break;
                }
            }*/
            RunCommand("git", $"ls-remote --heads --quiet {this.Url} {repoPath}", out string result);

            var match = Regex.Match(result, "[A-Fa-f0-9]{40}");
            this.Id = match.Success ? match.Value : "";
            LogMsg($"Parsed id as '{this.Id}'", MessageImportance.Normal);
        }

        private bool Download()
        {
            // allowed examples
            // https://github.com/Truinto/DarkCodex/raw/master/CodexLib/CodexLib.dll
            // https://github.com/Truinto/DarkCodex/blob/master/CodexLib/CodexLib.dll
            // CodexLib/CodexLib.dll
            // CodexLib/#(Filename)#(Extension)

            var rx_branch = new Regex(@"(?'branch'[^\/\n]*)$");
            var rx_repo = new Regex(@"(?::\/\/)(?'page'.*?)\/(?'owner'.*?)\/(?'repo'.*?)(?:\.git/?)?$");
            var rx_blob = new Regex(@"^(https?:\/\/.+?\/.+?\/.+?\/)(.+?)(\/.+)$");
            var rx_meta = new Regex(@"#\((\w*?)\)", RegexOptions.IgnoreCase);
            //var rx_url = new Regex(@"^(?'prot'https?:\/\/)(?'page'.*?)\/(?'owner'.*?)\/(?'repo'.*?)\/(?'blob'.*?)\/(?'branch'.*?)\/(?'path'.*)$");

            Match match;
            if (!(match = rx_repo.Match(this.Url)).Success)
            {
                LogMsg($"error: unable to parse url '{this.Url}'");
                return false;
            }
            string page = match.Groups["page"].Value;
            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;
            string branch = (match = rx_branch.Match(this.RepoPath)).Success ? match.Groups["branch"].Value : "master";

            LogMsg("Check downloads...", MessageImportance.Normal);
            bool success = true;
            foreach (var item in this.DownloadOnChange)
            {
                string filepath = item.ItemSpec;
                string url = item.GetMetadata("Url");

                if (url.Length < 1)
                {
                    LogMsg($"error: item defines no Url metadata: '{filepath}'");
                    success = false;
                    continue;
                }

                // replace placeholders with metadata, usually #(Filename) and #(Extension)
                LogMsg($"Test for metadata '{url}' name={item.GetMetadata("Filename")} ext={item.GetMetadata("Extension")}", MessageImportance.Normal);
                url = rx_meta.Replace(url, s => item.GetMetadata(s.Groups[1].Value));

                // replace 'blob' with 'raw'
                if ((match = rx_blob.Match(url)).Success)
                {
                    string blob = match.Groups[2].Value;
                    if (blob == "blob")
                        blob = "raw";
                    url = $"{match.Groups[1].Value}{blob}{match.Groups[3].Value}";
                }
                // auto-complete url
                else if (!url.StartsWith("https://"))
                {
                    url = $"https://{page}/{owner}/{repo}/raw/{branch}/{url}";
                }

                var response = DownloadFile(url, filepath);
                if ((int)response < 200 || (int)response > 299)
                {
                    LogMsg($"error: could not download file '{url}' to '{filepath}'");
                    success = false;
                    continue;
                }
            }
            return success;
        }

        private HttpStatusCode DownloadFile(string url, string filePath)
        {
            try
            {
                var uri = new Uri(url);
                if (uri.IsFile)
                    return HttpStatusCode.BadRequest;

                LogMsg($"Downloading file: {uri}", MessageImportance.Normal);
                new FileInfo(filePath).Directory.Create();
                //var client = new WebClient();
                //client.DownloadFile("", "");
                //client.UploadFile("", null, "");
                using var sw = new FileStream(filePath, FileMode.Create);
                using var client2 = new HttpClient();
                using var request = client2.GetAsync(uri);
                var result = request.Result;
                result.Content.CopyToAsync(sw).Wait();
                sw.Close();
                LogMsg($"{(int)result.StatusCode}: {result.ReasonPhrase}", MessageImportance.Low);
                return result.StatusCode;
            }
            catch (Exception e)
            {
                LogMsg($"Exception occured with path='{filePath}' url='{url}' {e.Message}");
                return HttpStatusCode.BadRequest;
            }
        }

        private void LogMsg(string msg, MessageImportance importance = MessageImportance.High)
        {
            if (this.Silent)
                return;

            Debug.WriteLine(msg);
            this.Log.LogMessage(importance, msg);
        }

        /// <summary>
        /// Runs command or program. Blocks execution until finished.
        /// </summary>
        /// <param name="command">Command or program path.</param>
        /// <param name="args">Process arguments to use.</param>
        /// <param name="output">Console text output.</param>
        /// <param name="onData">Data receive action.</param>
        /// <returns>Process exit code</returns>
        public static int RunCommand(string command, string args, out string output, Action<string> onData = null)
        {
            var sb = new StringBuilder();
            if (onData == null)
                onData = s => sb.AppendLine(s);
            else
                onData += s => sb.AppendLine(s);

            using var p = RunCommandAsync(command, args, onData, true);
            p.WaitForExit();

            output = sb.ToString();
            return p.ExitCode;
        }

        /// <summary>
        /// Runs command or program.
        /// </summary>
        /// <param name="command">Command or program path.</param>
        /// <param name="args">Process arguments to use.</param>
        /// <param name="onData">Data receive action. Prints to console, if empty.</param>
        /// <param name="startNow">Whenever to start the process immediately.</param>
        /// <returns>Process thread</returns>
        public static Process RunCommandAsync(string command, string args = "", Action<string> onData = null, bool startNow = true)
        {
            onData ??= Console.WriteLine;

            var p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (sender, args) => onData(args.Data);
            p.ErrorDataReceived += (sender, args) => onData(args.Data);
            if (startNow)
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
            //p.WaitForExit();
            return p;
        }
    }
}
