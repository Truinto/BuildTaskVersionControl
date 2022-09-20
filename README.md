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
* Version: [Output] Extracted version string.
* Major: [Output] Extracted major version.
* Minor: [Output] Extracted minor version.
* Build: [Output] Extracted build version.
* Revision: [Output] Extracted revision version.

Example
-----------
```xml
  <Target Name="Versioning" BeforeTargets="PreBuildEvent">
    <Message Text="Start version control" Importance="High" />
    <ItemGroup>
      <a1 Include="output1.txt" />
      <a2 Include="output2.txt" />
    </ItemGroup>
    <VersioningTask InputFile="input.txt" UpdateFiles="@(a1);@(a2)">
      <Output TaskParameter="Version" ItemName="OutputVersionControl" />
    </VersioningTask>
    <Message Text="Finish version control @(OutputVersionControl)" Importance="High" />
  </Target>
```

Tips
-----------
With MSBuild 17 use ExcludeAssets="runtime" so the dll doesn't copy to output.
```xml
	<ItemGroup>
	  <PackageReference Include="BuildTaskVersionControl" Version="1.0.3" ExcludeAssets="runtime" />
	</ItemGroup>
```
\
When you want to auto-increase version while using MSBuild 17, disable GenerateAssemblyInfo and use AutoIncrease=true and TouchFiles=false.
```xml
	<PropertyGroup>
		<GenerateAssemblyInfo>false</GenerateAssemblyInfo>
	</PropertyGroup>
	<Target Name="Versioning" BeforeTargets="PreBuildEvent">
		<ItemGroup>
			<a1 Include="Properties\AssemblyInfo.cs" />
			<a2 Include="output1.txt" />
			<a3 Include="output2.txt" />
		</ItemGroup>
		<VersioningTask InputFile="@(a1)" UpdateFiles="@(a1);@(a2);@(a3)" AutoIncrease="true" TouchFiles="false">
			<Output TaskParameter="Version" ItemName="OutputVersionControl" />
		</VersioningTask>
		<Message Text="Finish version control @(OutputVersionControl)" Importance="High" />
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