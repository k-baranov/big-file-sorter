using BigFileSorter.Sorter.Sorting;

if (args.Length < 2)
{
    Console.WriteLine("Usage: BigFileSorter.Sorter <input-file> <output-file>");
    Console.WriteLine("Example: BigFileSorter.Sorter input.txt output.txt");
    return 1;
}

var inputPath = args[0];
var outputPath = args[1];

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: input file not found: {inputPath}");
    return 1;
}

try
{
    var fileSize = new FileInfo(inputPath).Length;
    Console.WriteLine($"Input file: {inputPath} ({fileSize / (1024.0 * 1024 * 1024):F2} GB)");
    Console.WriteLine($"Output file: {outputPath}");

    var sorter = new ExternalSorter(
        inputPath,
        outputPath);

    await sorter.SortAsync();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
