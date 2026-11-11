using System.Text.Json;
using LibMentor.Application;
using LibMentor.Core;
using LibMentor.Infrastructure;

return await Command.RunAsync(args);

internal static class Command
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"])
        {
            Console.WriteLine("LibMentor\n\nlibmentor init [--config path]\nlibmentor generate [--config path] [--check] [--baseline knowledge.json]\nlibmentor validate [--config path] [--baseline knowledge.json]\n\nExit codes: 0 success, 1 validation/build failure, 2 usage/configuration error, 3 generated output drift.");
            return 0;
        }
        if (args is ["--version"])
        {
            Console.WriteLine(typeof(Command).Assembly.GetName().Version?.ToString(3));
            return 0;
        }
        try
        {
            var command = args[0];
            if (command is not ("init" or "generate" or "validate"))
                throw new ArgumentException($"Unknown command: {command}");
            var configPath = "libmentor.json";
            string? baselinePath = null;
            var check = false;
            string? revision = null;
            string? libraryVersion = null;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 1; i < args.Length; i++)
            {
                if (!seen.Add(args[i]))
                    throw new ArgumentException($"Duplicate option: {args[i]}");
                switch (args[i])
                {
                    case "--config" when i + 1 < args.Length: configPath = args[++i]; break;
                    case "--baseline" when i + 1 < args.Length && command != "init": baselinePath = args[++i]; break;
                    case "--check" when command == "generate": check = true; break;
                    case "--revision" when i + 1 < args.Length && command != "init": revision = args[++i]; break;
                    case "--library-version" when i + 1 < args.Length && command != "init": libraryVersion = args[++i]; break;
                    default: throw new ArgumentException($"Invalid or incomplete option: {args[i]}");
                }
            }
            configPath = Path.GetFullPath(configPath);
            if (command == "init")
            {
                var configuration = new Configuration
                {
                    Name = "MyLibrary",
                    Description = "Describe your library.",
                    Version = "0.1.0",
                    Repository = "https://github.com/owner/repository"
                };
                using var stream = new FileStream(configPath, FileMode.CreateNew, FileAccess.Write);
                await JsonSerializer.SerializeAsync(stream, configuration, ConfigurationFile.Json);
                Console.WriteLine($"Created {configPath}");
                return 0;
            }
            var config = ConfigurationFile.Load(configPath);
            var root = Path.GetDirectoryName(configPath)!;
            config = config with { Revision = revision ?? config.Revision, Version = libraryVersion ?? config.Version };
            ConfigurationFile.Validate(config, root);
            var baseline = baselinePath is null ? null : JsonSerializer.Deserialize<Knowledge>(await File.ReadAllTextAsync(baselinePath), ConfigurationFile.Json)
                ?? throw new InvalidDataException("Baseline is empty.");
            var build = new BuildDocumentation(new RepositoryExtractor(), new KnowledgeValidator(), [new PortableExporter()]);
            var result = await build.ExecuteAsync(config, root, baseline);
            if (command == "generate")
            {
                if (check)
                {
                    var differences = OutputDirectory.Differences(root, config, result.Files);
                    if (differences.Count > 0)
                    {
                        Console.Error.WriteLine($"Generated output differs:\n{string.Join('\n', differences)}");
                        return 3;
                    }
                }
                else OutputDirectory.Write(root, config, result.Files);
            }
            Console.WriteLine($"Validated {result.Knowledge.Name} {result.Knowledge.Version}: {result.Knowledge.Documents.Count} documents, {result.Knowledge.Api.Count} APIs, {result.Files.Count} output files.");
            return 0;
        }
        catch (ValidationException exception) { Console.Error.WriteLine(exception.Message); return 1; }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or JsonException or IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine(exception.Message); return 2; }
    }
}
