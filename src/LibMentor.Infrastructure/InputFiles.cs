using LibMentor.Core;
using Microsoft.Extensions.FileSystemGlobbing;

namespace LibMentor.Infrastructure;

internal static class InputFiles
{
    internal static IReadOnlyList<string> Match(string root, IEnumerable<string> patterns, Configuration configuration, bool includeBuildOutput = false)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(patterns);
        matcher.AddExcludePatterns(configuration.Exclude.Concat([
            "**/obj/**", "**/node_modules/**", "**/.git/**", "**/dist/**",
            "**/TestResults/**", "**/.vitepress/**", $"{configuration.Output.Replace('\\', '/')}/**"]));
        if (!includeBuildOutput)
            matcher.AddExclude("**/bin/**");
        var candidates = Enumerate(root, configuration.Output, includeBuildOutput).Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'));
        return matcher.Match(candidates).Files.Select(f => f.Path).Order(StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> Enumerate(string root, string output, bool includeBuildOutput)
    {
        var ignored = new HashSet<string>([".git", "bin", "obj", "node_modules", "dist", "TestResults", ".vitepress"], StringComparer.OrdinalIgnoreCase);
        if (includeBuildOutput)
            ignored.Remove("bin");
        var generated = SafePaths.Resolve(root, output);
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var directory))
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                if ((entry.Attributes & FileAttributes.Directory) == 0)
                    yield return entry.FullName;
                else if (!ignored.Contains(entry.Name) && !entry.FullName.Equals(generated, StringComparison.OrdinalIgnoreCase))
                    pending.Push(entry.FullName);
            }
    }
}
