using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace SurpMSBuildTasks
{
    /// <summary>
    /// Transforms the input Items parameter by evaluating the
    /// regular expression in their Find metadata and
    /// replacing with their ReplaceWith metadata.Optional, the
    /// options for the regular expression evaluation can be specified.
    /// http://blogs.clariusconsulting.net/kzu/how-to-perform-regular-expression-based-replacements-on-files-with-msbuild/
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <ItemGroup>
    ///         <RegexItems Include="$(BuildRoot)Src\GlobalAssemblyInfo.cs">
    ///             <Find>AssemblyFileVersion\(".*?"\)</Find>
    ///             <ReplaceWith>AssemblyFileVersion("$(FileVersion)")</ReplaceWith>
    ///             <Options>Multiline | IgnorePatternWhitespace</Options>
    ///         </RegexItems>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <RegexTransform Items="@(RegexItems)"/>
    ///     </Target>
    /// 
    ///     <!-- If you put a assembly file with a xml file for processing -->
    ///     <UsingTask TaskName="RegexTransform" AssemblyFile="SurpMSBuildTasks.dll" />
    /// </Project>
    /// ]]></code>
    /// </example>
    public class RegexTransform : Task
    {
        [Required]
        public ITaskItem[] Items { get; set; }


        public override bool Execute()
        {
            foreach (var item in Items)
            {
                string fileName = item.ItemSpec;
                if (!File.Exists(fileName))
                {
                    Log.LogError("Could not find file: {0}", fileName);
                    return false;
                }

                string find = item.GetMetadata(FindMetaName);
                string replaceWith = item.GetMetadata(ReplaceWithMetaName);
                string optionsValue = item.GetMetadata(OptionsMetaName) ?? "";

                var options =
                    string.IsNullOrEmpty(optionsValue) ?
                    RegexOptions.None :
                    (RegexOptions)Enum.Parse(
                        typeof(RegexOptions), optionsValue.Replace('|', ','));

                string content = File.ReadAllText(fileName);
                File.WriteAllText(
                    fileName,
                    Regex.Replace(
                        content,
                        find,
                        replaceWith,
                        options));
            }
            return true;
        }


        private const string FindMetaName = "Find";
        private const string ReplaceWithMetaName = "ReplaceWith";
        private const string OptionsMetaName = "Options";
    }
}
