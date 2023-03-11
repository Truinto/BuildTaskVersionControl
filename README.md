# BuildTaskVersionControl
MSBuild Task to automate assembly versioning. Extracts a version string from one file and update it in other files.

Settings
-----------
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

Item Metadata
-----------
Files can have metadata defined, which overwrite the global setting.
* Max: Maximum number of matches for this file only.
* Regex: Regex used for this file only.
* Replacement: UpdateFiles only. Replacement string for this file only.
* DropRevision: UpdateFiles only. Whenever to drop revision before updating files. (never, always, keep)
* Touch: UpdateFiles only. Update write-date when updating output files.

Minimal Example
-----------
Set this as the first entry in your csproj file. Add or remove in- and out-files as you like.
```xml
  <PropertyGroup>
    <Version>1.0.0.0</Version>
    <AutoVersion>true</AutoVersion>
  </PropertyGroup>
  <ItemGroup>
    <BuildTaskVersionControl_In Include="$(MSBuildProjectName).csproj" />
    <BuildTaskVersionControl_In Include="changelog.md" />
    <BuildTaskVersionControl_Out Include="$(MSBuildProjectName).csproj" />
    <BuildTaskVersionControl_Out Include="changelog.md" />
  </ItemGroup>
```

Example
-----------
```xml
<Target Name="Versioning" BeforeTargets="PreBuildEvent">
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

Tips
-----------
* Setting the property 'AutoVersion' to true, will run the default target. It will use the items 'BuildTaskVersionControl_In' and 'BuildTaskVersionControl_Out' as InputFiles and UpdateFiles respectively.
* Consider using AssemblyInfo.cs to set your project's version. [Link](https://learn.microsoft.com/en-US/troubleshoot/developer/visualstudio/general/assembly-version-assembly-file-version)

