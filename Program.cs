using System.Diagnostics;
using static win32.FindFiles;

// Code is stolen from fastest example here https://stackoverflow.com/questions/26321366/fastest-way-to-get-directory-data-in-net
namespace win32;

public static class Program
{
    private static void Main(string[] args)
    {
        // a program that uses pinvoke to enumerate all files recursively in a given directory.
        Console.WriteLine("Please enter a directory to enumerate:");
        var dir = Console.ReadLine();
        new List<WIN32_FIND_DATAW>();
        new List<WIN32_FIND_DATAW>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        FindNextFilePInvokeRecursive(dir, out var files, out var directories);
        foreach (var file in files)
            if (string.IsNullOrWhiteSpace(file.cFileName))
                if (file.FullPath.Length > 260)
                    Console.WriteLine(file.FullPath);
        stopwatch.Stop();
        Console.WriteLine("Time taken: {0}", stopwatch.Elapsed);
        Console.WriteLine("Files: {0}", files.Count);
        Console.WriteLine("Directories: {0}", directories.Count);
        GC.Collect();
    }
}