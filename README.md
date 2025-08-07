# BuildTaskVersionControl
[![NuGet version (BuildTaskVersionControl)](https://img.shields.io/nuget/v/BuildTaskVersionControl.svg?style=flat-square)](https://www.nuget.org/packages/BuildTaskVersionControl/) \
MSBuild Task to automate assembly versioning. Extracts a version string from one file and update it in other files.

## Index
  - [VersionTask](#versiontask)
    - [Settings](#settings)
    - [Item Metadata](#item-metadata)
    - [Minimal Example](#minimal-example)
    - [Example](#example)
    - [Tips](#tips)
  - [GitRemoteTask](#gitremotetask)
    - [Settings](#settings-1)
    - [Example](#example-1)
    - [Tips](#tips-1)
  - [ZipTask](#ziptask)
    - [Settings](#settings-2)
    - [Example](#example-2)
  - [SleepTask](#sleeptask)
    - [Settings](#settings-3)
    - [Example](#example-3)

## VersionTask
MSBuild Task to automate assembly versioning. Extracts a version string from one file and update it in other files.

### Settings
* InputFile: Legacy value. Value is added to InputFiles. Default: null
* InputFiles: Path to files to extract version string. Only the greatest value is used. Use RegexInput to customize logic. Default: changelog.md and either Properties\AssemblyInfo.cs or all csproj in ProjectDir
* UpdateFiles: Path to files to inject with new version string. Use RegexOutput to customize logic. Default: null
* MaxMatch: Maximum number of lines to match and replace. Matches at least once. Default: 1
* AutoIncrease: If true, will increase revision by one before updating files. Default: false
* TouchFiles: Update write-date when updating output files. Default: false
* DropRevision: Whenever to drop revision before updating files. (never, always, keep) Default: keep
* Silent: Suppress all log output. Default: false
* RegexInput: Regex string for the input files. Uses capture group 'version' (or the first capture) and 'suffix' (optional). Default: `"(?'version'\d+(?:\.\d+){2,3})(?'suffix'[0-9A-Za-z-]*)"`
* RegexOutput: Regex string for output files. Is used in conjunction with RegexReplace. Default: `"(?'version'\d+(?:\.\d+){2,3})(?'suffix'[0-9A-Za-z-]*)"`
* RegexReplace: Replacement string for output files. `{version}` is replaced by new version and `{suffix}` is replaced by suffix. Default: `"{version}{suffix}"`
* VersionFull: [Output] Extracted version string with suffix.
* Version: [Output] Extracted version string without suffix.
* VersionShort: [Output] Extracted version string without revision.
* Major: [Output] Extracted major version only.
* Minor: [Output] Extracted minor version only.
* Build: [Output] Extracted build version only.
* Revision: [Output] Extracted revision version only.
* Suffix: [Output] Extracted version suffix

### Item Metadata
Files can have metadata defined, which overwrite the global setting.
* Max: Maximum number of matches for this file only.
* Regex: Regex used for this file only.
* Replacement: UpdateFiles only. Replacement string for this file only.
* DropRevision: UpdateFiles only. Whenever to drop revision before updating files. (never, always, keep)
* Touch: UpdateFiles only. Update write-date when updating output files.

### Minimal Example
Set this as the first entry in your csproj file. Add or remove in- and out-files as you like.
```xml
  <PropertyGroup>
    <Version>1.0.0.0</Version>
    <AutoVersion>true</AutoVersion>
  </PropertyGroup>
  <ItemGroup>
    <VersioningTask_In Include="$(MSBuildThisFileFullPath)" />
    <VersioningTask_In Include="changelog.md" />
    <VersioningTask_Out Include="$(MSBuildThisFileFullPath)" />
    <VersioningTask_Out Include="changelog.md" />
  </ItemGroup>
```

### Example
```xml
<Target Name="Versioning" BeforeTargets="BeforeBuild">
  <Message Text="Start version control" Importance="High" />
  <ItemGroup>
    <_In Include="$(MSBuildProjectName).csproj" />
    <_In Include="changelog.md" />
    <_Out Include="$(MSBuildProjectName).csproj" />
    <_Out Include="changelog.md" />
  </ItemGroup>
  <VersioningTask InputFiles="@(_In)" UpdateFiles="@(_Out)" AutoIncrease="true">
    <Output TaskParameter="VersionFull" PropertyName="Version" />
    <Output TaskParameter="Version" ItemName="OutputVersionText" />
  </VersioningTask>
  <Message Text="Finish version control @(OutputVersionText)" Importance="High" />
</Target>
```

### Tips
* Setting the property 'AutoVersion' to true, will run the default target. It will use the items 'VersioningTask_In' and 'VersioningTask_Out' as InputFiles and UpdateFiles respectively.

## GitRemoteTask
Task to read remote repository commit history and download files on change. Requires GIT to be installed!

### Settings
* Url: [Required] Url of remote repository. May or may not contain '.git' extension.
* RepoPath: Branch or branch path. Will try to find HEAD, if empty string. Example: `master` Default: `refs/heads/master`
* Interval: TimeSpan to wait between remote check. Format: `[days].[hours]:[minutes]` Example: `1.12:00` (1 day 12 hours) Default: `0.18:00`
* DownloadOnChange: Collection of files to download or update, whenever the repository's Id changes. Set metadata 'Url' to an url to download from. The url can include #(Filename)#(Extension) to copy the filename and extension from the file path. If a relative name is given, then the url is taken from the repository Url (Github). Default: empty
* CachePath: File last update date is saved to. Default: `obj/remote.cache`
* SurpressErrors: Whenever to throw an error, if remote connection fails. Default: 
* Force: Whenever to force download, even if repository is unchanged. Default: false
* Silent: Suppress all log output. Default: false
* Id: [Output] Latest commit id (usually hash).
* NeedsUpdate: [Output] True if Id has changed.

### Example
```xml
<Target Name="GithubUpdate" BeforeTargets="BeforeBuild;Clean">
  <ItemGroup>
    <_Download Include="Downloads\README.md" Url="#(Filename)#(Extension)" />
  </ItemGroup>
  <GitRemoteTask Url="https://github.com/Truinto/BuildTaskVersionControl.git" RepoPath="master" DownloadOnChange="@(_Download)" />
</Target>
```

### Tips
* Files in the destination will be overwritten. You can also use wildcards (*, ?), if all target files already exist in the destination folder.

## ZipTask
Simple zip task.

### Settings
* ZipFileName: [Required] Path and name of the zip file.
* Files: [Required] Files to zip. Use metadata 'Path' to overwrite path inside the zip.
* WorkingDirectory: Working directory from which the path inside the zip is determined. If path cannot be reached, then file is put in a dot folder. Ignored if metadata 'Path' is set manually. Default: null
* Silent: Suppress all log output. Default: false

### Example
```xml
<Target Name="Zipping" AfterTargets="PostBuildEvent">
  <ItemGroup>
    <_Zip Include="*.dll" />
  </ItemGroup>
  <ZipTask ZipFileName="archive.zip" WorkingDirectory="." Files="@(_Zip)" />
</Target>
```

## SleepTask
Simple sleep task.

### Settings
* Milliseconds: Duration of sleep. Default: 300

### Example
```xml
<Target Name="Waiting" AfterTargets="PostBuildEvent">
  <SleepTask Milliseconds="1000" />
</Target>
```
