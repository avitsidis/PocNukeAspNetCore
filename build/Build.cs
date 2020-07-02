using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Publish);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Runtime = "win-x64";

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetRuntime(Runtime)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });
    Target Pack => _ => _
    .Produces(OutputDirectory / "*.nupkg")
    .Executes(() =>
    {
        DotNetPack(s => s
            .SetProject(Solution.GetProject("Demo.Core"))
            .SetOutputDirectory(OutputDirectory));
    });

    Target Test => _ => _
    .DependsOn(Compile)
    .Executes(() =>
    {
        var testProjects = Solution.GetProjects("*.Test");
        foreach (var testProject in testProjects)
        {
            DotNetTest(s => s.SetProjectFile(testProject));
        }
    });

    Target Publish => _ => _
    .DependsOn(Clean)
    .DependsOn(Restore)
    .DependsOn(Compile)
    .Executes(() => {
        DotNetPublish(s => s
        .SetProject(Solution.GetProject("Demo.Hosting"))
        .SetRuntime(Runtime)
        .SetNoBuild(true)
        .SetNoRestore(true)
        .SetProperty("PublishSingleFile", true)
        .SetProperty("PublishTrimmed", true)
        .SetProperty("PublishSingleFile", true)
        .SetConfiguration(Configuration)
        .SetOutput(OutputDirectory / "app")
        ) ;
        DotNetPack(s => s
        .SetProject(Solution.GetProject("Demo.Core"))
        .SetOutputDirectory(OutputDirectory / "packages")
        );
        DotNetPublish(s => s
        .SetProject(Solution.GetProject("Demo.Core.Test"))
        .SetRuntime("win7-x64")
        .SetOutput(OutputDirectory / "test")
        );
    });

}
