using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SurpMSBuildTasks
{
    /// <summary>
    /// Modify environment variable.
    /// https://social.msdn.microsoft.com/Forums/vstudio/en-US/0fb8d97d-513e-4d37-8528-52973f65a034/how-to-set-envrionment-variables-in-msbuild-file?forum=msbuild
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <PATH>$(PATH);.\bin</PATH>
    ///     </PropertyGroup>
    ///     <Target Name="Default">
    ///         <SetEnvVar Variable="PATH" Value="$(PATH)"/>
    ///     </Target>
    /// 
    ///     <!-- If you put a assembly file with a xml file for processing -->
    ///     <UsingTask TaskName="SetEnvVar" AssemblyFile="SurpMSBuildTasks.dll" />
    /// </Project>
    /// ]]></code>
    /// </example>
    public class SetEnvVar : Task
    {
        [Required]
        public string Variable { get; set; }

        [Required]
        public string Value { get; set; }

        public override bool Execute()
        {
            Environment.SetEnvironmentVariable(Variable, Value);
            return true;
        }
    }
}
