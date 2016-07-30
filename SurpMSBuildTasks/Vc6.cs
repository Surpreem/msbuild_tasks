using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace SurpMSBuildTasks
{
    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Build</i> (<b>Required: </b> Projects <b>Optional: </b>MsDevPath, Target, StopOnError)</para>
    /// <para/>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <ItemGroup>
    ///         <!-- This uses $(Platform) and $(Configuration) for all projects in the .dsp file -->
    ///         <ProjectsToBuild Include="C:\MyVC6Project.dsp"/>
    ///         <!-- Uses supplied platform and configuration for all projects in the .dsp file -->
    ///         <ProjectsToBuild Include="C:\MyVC6Project2.dsp">
    ///             <Platform>Win32</Platform>
    ///             <Configuration>Debug</Configuration>
    ///         </ProjectsToBuild>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Build a collection of VC6 projects -->
    ///         <Vc6 Projects="@(ProjectsToBuild)"/>
    ///     </Target>
    ///     <Target Name="Clean">
    ///         <!-- Build a collection of VC6 projects -->
    ///         <Vc6 Projects="@(ProjectsToBuild)" Target="CLEAN"/>
    ///     </Target>
    /// 
    ///     <!-- If you put a assembly file with a xml file for building -->
    ///     <UsingTask TaskName="Vc6" AssemblyFile="SurpMSBuildTasks.dll" />
    /// </Project>
    /// ]]></code>
    /// </example>
    public class Vc6 : Task
    {
        /// <summary>
        /// Sets the MSDev fully qualified path.
        /// Default is Environment::GetEnvironmentVariable('MSDevDir')
        /// \Bin\msdev.exe
        /// </summary>
        public string MsDevPath { get; set; }

        /// <summary>
        /// Sets the build action. Default is Build.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Sets the build timeout. Default is 5min.
        /// </summary>
        public int Timeout { get; set; } = 5;

        /// <summary>
        /// Set to true to stop processing when a project in the projects
        /// collection fails to compile. Default is false.
        /// </summary>
        public bool StopOnError { get; set; } = false;

        /// <summary>
        /// Sets the .dsp/.dsw projects to build.
        /// </summary>
        [Required]
        public ITaskItem[] Projects { get; set; }


        public override bool Execute()
        {
            string msDev = GenerateFullPathToTool();
            if (File.Exists(msDev))
            {
                return !Projects.Any(
                        project => !Build(msDev, project) && StopOnError);
            }
            Log.LogError("Could not find MSDEV.exe at {0}. Use MsDevPath to specify it.", msDev);
            return false;
        }

        private bool Build(string msDev, ITaskItem project)
        {
            // http://stackoverflow.com/a/7608823
            // http://stackoverflow.com/a/10086961
            // http://stackoverflow.com/a/22956924
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = msDev;
                    process.StartInfo.Arguments = GenerateArguments(project);
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    StringBuilder outputMessage = new StringBuilder();
                    StringBuilder errorMessage = new StringBuilder();
                    var timeout = Timeout * MinToMillisec;

                    try
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (null == e.Data)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                outputMessage.AppendLine(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (null == e.Data)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                outputMessage.AppendLine(e.Data);
                            }
                        };

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        if (process.WaitForExit(timeout))
                        {
                            // Process completed
                            if (outputMessage.Length > 0)
                            {
                                Log.LogMessage(outputMessage.ToString());
                            }
                            if (errorMessage.Length > 0)
                            {
                                Log.LogError(errorMessage.ToString());
                            }

                            return 0 == process.ExitCode;
                        }
                        else
                        {
                            // Timed out
                            Log.LogError("Timed out!");
                            return false;
                        }
                    }
                    finally
                    {
                        outputWaitHandle.WaitOne(timeout);
                        errorWaitHandle.WaitOne(timeout);
                    }
                }
            }
        }

        /// <summary>
        /// Get the fully qualified path to msdev.exe, if the path isn't
        /// specified explicitly.
        /// </summary>
        /// <returns>
        /// The fully qualified path to msdev.exe, or just "msdev.exe"
        /// if it isn't found.
        /// </returns>
        private string GenerateFullPathToTool()
        {
            if (string.IsNullOrEmpty(MsDevPath))
            {
                string toolPath =
                    Environment.GetEnvironmentVariable("MSDevDir");
                if (string.IsNullOrEmpty(toolPath))
                {
                    return MsDevName;
                }
                else
                {
                    string pathBin = Path.Combine(toolPath, "Bin");
                    return Path.Combine(pathBin, MsDevName);
                }
            }
            else
            {
                return MsDevPath;
            }
        }

        /// <summary>
        /// Generate the command line arguments to msdev.exe.
        /// Ex. "C:\myproject.dsp /MAKE 'myproject - WIN32 Release' /CLEAN"
        /// </summary>
        private string GenerateArguments(ITaskItem project)
        {
            string platform = project.GetMetadata(PlatformMetaName);
            if (string.IsNullOrEmpty(platform))
            {
                platform = Platform;
            }

            string configuration = project.GetMetadata(ConfigurationMetaName);
            if (string.IsNullOrEmpty(configuration))
            {
                configuration = Configuration;
            }

            StringBuilder arguments = new StringBuilder();
            arguments.AppendFormat(
                    "\"{0}\" /MAKE \"{1} - {2} {3}\"",
                    project.ItemSpec,
                    Path.GetFileNameWithoutExtension(project.ItemSpec),
                    platform,
                    configuration);
            if (!string.IsNullOrEmpty(Target))
            {
                arguments.AppendFormat(" /{0}", Target);
            }

            return arguments.ToString();
        }


        private const string MsDevName = "msdev.exe";
        private const string Platform = "Win32";
        private const string Configuration = "Debug";
        private const string PlatformMetaName = "Platform";
        private const string ConfigurationMetaName = "Configuration";
        private const int MinToMillisec = 60 * 1000;
    }
}
