
using System.Runtime.InteropServices;

namespace BuildScripts;

public sealed class BuildPremake
{
    public void Run(BuildContext context, string name, string workingDirectory, string solutionFile)
    {
        if (context.Environment.Platform.Family == PlatformFamily.Windows)
        {
            // Cross-compile both architectures on the same x64 runner
            BuildForArch(context, name, workingDirectory, solutionFile, "x64");
            BuildForArch(context, name, workingDirectory, solutionFile, "arm64");
        }
        else
        {
            // Linux/macOS build for the host architecture only
            var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            BuildForArch(context, name, workingDirectory, solutionFile, arch);
        }
    }

    private void BuildForArch(BuildContext context, string name, string workingDirectory, string solutionFile, string arch)
    {
        int exit;
        exit = context.StartProcess("premake5", new ProcessSettings { WorkingDirectory = workingDirectory, Arguments = "clean" });
        if (exit != 0)
            throw new Exception($"{name} Premake clean failed! {exit}");

        string? premakeArguments;
        switch (context.Environment.Platform.Family)
        {
            case PlatformFamily.Windows:
                premakeArguments = $"--arch={arch} --verbose vs2022";
                break;
            case PlatformFamily.Linux or PlatformFamily.OSX:
                premakeArguments = $"--arch={arch} gmake2";
                break;
            default:
                throw new NotSupportedException($"Platform {context.Environment.Platform.Family} is not supported for building the {name}.");
        }

        exit = context.StartProcess("premake5", new ProcessSettings { WorkingDirectory = workingDirectory, Arguments = premakeArguments });
        if (exit != 0)
            throw new Exception($"{name} Premake generation failed! {exit}");

        if (context.Environment.Platform.Family == PlatformFamily.Windows)
        {
            var msbuildPlatform = arch == "arm64" ? "ARM64" : "x64";
            exit = context.StartProcess("msbuild", new ProcessSettings { WorkingDirectory = workingDirectory, Arguments = $"{solutionFile} /p:Configuration=Release /p:Platform={msbuildPlatform}" });
            if (exit != 0)
                throw new Exception($"{name} build failed with msbuild! {exit}");
        }
        else
        {
            exit = context.StartProcess("make", new ProcessSettings { WorkingDirectory = workingDirectory, Arguments = "config=release" });
            if (exit != 0)
                throw new Exception($"{name} build failed with make! {exit}");
        }
    }
}
