namespace LibMentor.Infrastructure;

internal static class SafePaths
{
    internal static void ValidateRelative(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains(':') ||
            path.Split('/', '\\').Any(p => p is ".." or ".git") || path.Contains('\0'))
            throw new InvalidDataException($"Expected a contained relative path: {path}");
    }

    internal static string Resolve(string root, string path)
    {
        ValidateRelative(path);
        var absoluteRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var result = Path.GetFullPath(Path.Combine(absoluteRoot, path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!result.StartsWith(absoluteRoot, comparison))
            throw new InvalidDataException($"Path escapes the repository: {path}");
        for (var current = result; current.Length >= absoluteRoot.Length; current = Path.GetDirectoryName(current)!)
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Symbolic links are not supported as inputs or outputs: {path}");
        return result;
    }
}
