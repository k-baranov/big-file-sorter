using BigFileSorter.Generator;

if (args.Length < 2)
{
    Console.WriteLine("Usage: BigFileSorter.Generator <output-file> <size-in-bytes>");
    Console.WriteLine("Example: BigFileSorter.Generator output.txt 107374182400");
    Console.WriteLine("  Sizes: 1MB=1048576, 1GB=1073741824, 100GB=107374182400");
    return 1;
}

var outputPath = args[0];

if (!long.TryParse(args[1], out var targetSize) || targetSize <= 0)
{
    Console.Error.WriteLine("Error: size-in-bytes must be a positive integer.");
    return 1;
}

try
{
    var generator = new FileGenerator(outputPath, targetSize);
    generator.Generate();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
