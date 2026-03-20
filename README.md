# Big File Sorter

Sorts large text files where each line has the format `Number. String`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build

```bash
dotnet build -c Release
```

## Generate a test file

```bash
dotnet run --project src/BigFileSorter.Generator -c Release -- <output-file> <size-in-bytes>
```

Examples:

```bash
dotnet run --project src/BigFileSorter.Generator -c Release -- testdata.txt 1048576
dotnet run --project src/BigFileSorter.Generator -c Release -- testdata.txt 1073741824
dotnet run --project src/BigFileSorter.Generator -c Release -- testdata.txt 107374182400
```

## Sort

```bash
dotnet run --project src/BigFileSorter.Sorter -c Release -- <input-file> <output-file>
```

Example:

```bash
dotnet run --project src/BigFileSorter.Sorter -c Release -- testdata.txt sorted.txt
```

## Run Tests

```bash
dotnet test
```
