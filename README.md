# BuildTaskVersionControl
MSBuild Task to automate assembly versioning. Extracts a version string from one file and update it in other files.

Settings
-----------
* InputFile: [Required] Path to file to extract version string. Use RegexInput to customize logic.
* UpdateFiles: [Required] Path to files to inject with new version string. Use RegexOutput to customize logic.
* AutoIncrease: If true, will increase version by one before updating files. If you use this, it's recommended to include InputFile in UpdateFiles. Does not work with '*' wildcard. Default: false
* RegexInput: Regex string for the input file. Requires group 'version' for version string. Default: `"(?<!Manager)(Version.*?)(?'version'[\d\.\*]{5,})"`
* RegexOutput: Regex string for output files. Is used in conjunction with RegexReplace. Default: `"(?<!Manager)(Version.*?)(?'version'[\d\.\*]{5,})"`
* RegexReplace: Replacement string for output files. `{version}` is replaced by new version. Default: `"${1}{version}"`
* TouchFiles: Update write-date when updating output files. Default: true
* MaxMatch: Maximum number of lines to match and replace. 0 or less will replace all. Default: 0
* Version: [Output] Extracted full version string.
* Major: [Output] Extracted major version only.
* Minor: [Output] Extracted minor version only.
* Build: [Output] Extracted build version only.
* Revision: [Output] Extracted revision version only.

Example
-----------
```xml
<Target Name="Versioning" BeforeTargets="PreBuildEvent">
  <Message Text="Start version control" Importance="High" />
  <ItemGroup>
    <VersioningOutput Include="output1.txt" />
    <VersioningOutput Include="output2.txt" />
  </ItemGroup>
  <VersioningTask InputFile="input.txt" UpdateFiles="@(VersioningOutput)">
    <Output TaskParameter="Version" ItemName="OutputVersionText" />
  </VersioningTask>
  <Message Text="Finish version control @(OutputVersionText)" Importance="High" />
</Target>
```

Tips
-----------
When you want to auto-increase version while using MSBuild 17, disable GenerateAssemblyInfo and use AutoIncrease=true and TouchFiles=false.
```xml
<PropertyGroup>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
</PropertyGroup>
<Target Name="Versioning" BeforeTargets="PreBuildEvent">
  <ItemGroup>
    <VersioningInput Include="Properties\AssemblyInfo.cs" />
    <VersioningOutput Include="Properties\AssemblyInfo.cs" />
    <VersioningOutput Include="output1.txt" />
    <VersioningOutput Include="output2.txt" />
  </ItemGroup>
  <VersioningTask InputFile="@(VersioningInput)" UpdateFiles="@(VersioningOutput)" AutoIncrease="true" TouchFiles="false">
    <Output TaskParameter="Version" ItemName="OutputVersionText" />
  </VersioningTask>
  <Message Text="Finish version control @(OutputVersionText)" Importance="High" />
</Target>

<!--
Content of 'Properties\AssemblyInfo.cs'

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly: AssemblyVersion("1.0.18")]
[assembly: AssemblyFileVersion("1.0.18")]
-->
```