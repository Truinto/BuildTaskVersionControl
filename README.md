# BuildTaskVersionControl
MSBuild Task to automate assembly versioning. Extracts a version string from one file and update it in other files.

Settings
-----------
* InputFile: [Required] Path to file to extract version string. Use RegexInput to customize logic.
* UpdateFiles: [Required] Path to files to inject with new version string. Use RegexOutput to customize logic.
* AutoIncrease: If true, will increase version by one before updating files. If you use this, it's recommended to include InputFile in UpdateFiles. Does not work with '*' wildcard.
* RegexInput: Regex string for the input file. Requires group 'version' for version string. Default: `"(?<!Manager)(Version.*?)(?'version'[\d\.\*]{5,})"`
* RegexOutput: Regex string for output files. Is used in conjunction with RegexReplace. Default: `"(?<!Manager)(Version.*?)(?'version'[\d\.\*]{5,})"`
* RegexReplace: Replacement string for output files. `{version}` is replaced by new version. Default: `"${1}{version}"`
* MaxMatch: Maximum number of lines to match and replace. 0 or less will replace all.

Example
-----------
```xml
  <Target Name="Versioning" BeforeTargets="Build">
    <Message Text="Start version control" Importance="High" />
    <ItemGroup>
      <a1 Include="input.txt" />
      <a2 Include="output1.txt" />
      <a3 Include="output2.txt" />
    </ItemGroup>
    <VersioningTask InputFile="input.txt" UpdateFiles="@(a1);@(a2);@(a3)" AutoIncrease="true" />
  </Target>
```
