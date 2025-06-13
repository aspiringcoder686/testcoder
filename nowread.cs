var files = Directory.EnumerateFiles(projectPath, "*.aspx.cs", SearchOption.AllDirectories).ToList();

if (files.Count == 0)
{
    Console.WriteLine("⚠ No .aspx.cs files found in the directory or subdirectories.");
}
else
{
    Console.WriteLine($"Found {files.Count} .aspx.cs files:");
    foreach (var file in files)
    {
        Console.WriteLine($" - {file}");
    }
}
