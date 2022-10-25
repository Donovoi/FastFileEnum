using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace win32;

public static class FindFiles
{
    private static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    public static DateTime LastWriteTime { get; set; }

    public static string FullPath { get; set; }

    public static bool FindNextFilePInvokeRecursive(string path, out List<WIN32_FIND_DATAW> files,
        out List<WIN32_FIND_DATAW> directories)
    {
        var fileList = new List<WIN32_FIND_DATAW>();
        var directoryList = new List<WIN32_FIND_DATAW>();
        WIN32_FIND_DATAW findData;
        var findHandle = INVALID_HANDLE_VALUE;
        var info = new List<Tuple<string, DateTime>>();
        try
        {
            findHandle = FindFirstFileW(path + @"\*", out findData);
            if (findHandle != INVALID_HANDLE_VALUE)
                do
                {
                    // Skip current directory and parent directory symbols that are returned.
                    if (findData.cFileName != "." && findData.cFileName != "..")
                    {
                        var fullPath = path + @"\" + findData.cFileName;
                        // Check if this is a directory and not a symbolic link since symbolic links could lead to repeated files and folders as well as infinite loops.
                        if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory) &&
                            !findData.dwFileAttributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            directoryList.Add(new WIN32_FIND_DATAW());
                            var subDirectoryFileList = new List<WIN32_FIND_DATAW>();
                            var subDirectoryDirectoryList =
                                new List<WIN32_FIND_DATAW>();
                            if (FindNextFilePInvokeRecursive(fullPath, out subDirectoryFileList,
                                    out subDirectoryDirectoryList))
                            {
                                fileList.AddRange(subDirectoryFileList);
                                directoryList.AddRange(subDirectoryDirectoryList);
                            }
                        }
                        else if (!findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
                        {
                            fileList.Add(new WIN32_FIND_DATAW(fullPath));
                            {
                                FullPath = fullPath;
                                LastWriteTime = FILETIMEExtensions.ToDateTime(findData.ftLastWriteTime);
                            }
                        }
                    }
                } while (FindNextFile(findHandle, out findData));
        }
        catch (Exception exception)
        {
            Console.WriteLine("Caught exception while trying to enumerate a directory. {0}", exception);
            if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
            files = null;
            directories = null;
            return false;
        }

        if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
        files = fileList;
        directories = directoryList;
        return true;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr FindFirstFileW(string lpFileName, out WIN32_FIND_DATAW lpFindFileData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATAW lpFindFileData);

    [DllImport("kernel32.dll")]
    public static extern bool FindClose(IntPtr hFindFile);

    private static bool FindNextFilePInvokeRecursiveParalleled(string path, out List<WIN32_FIND_DATAW> files,
        out List<WIN32_FIND_DATAW> directories)
    {
        var fileList = new List<WIN32_FIND_DATAW>();
        var fileListLock = new object();
        var directoryList = new List<WIN32_FIND_DATAW>();
        var directoryListLock = new object();
        WIN32_FIND_DATAW findData;
        var findHandle = INVALID_HANDLE_VALUE;
        var info = new List<Tuple<string, DateTime>>();
        try
        {
            path = path.EndsWith(@"\", StringComparison.Ordinal) ? path : path + @"\";
            findHandle = FindFirstFileW(path + @"*", out findData);
            if (findHandle != INVALID_HANDLE_VALUE)
            {
                do
                {
                    // Skip current directory and parent directory symbols that are returned.
                    if (findData.cFileName != "." && findData.cFileName != "..")
                    {
                        var fullPath = path + findData.cFileName;
                        // Check if this is a directory and not a symbolic link since symbolic links could lead to repeated files and folders as well as infinite loops.
                        if (findData.dwFileAttributes.HasFlag(FileAttributes.Directory) &&
                            !findData.dwFileAttributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            directoryList.Add(new WIN32_FIND_DATAW());
                            {
                                FullPath = fullPath;
                                LastWriteTime = FILETIMEExtensions.ToDateTime(findData.ftLastWriteTime);
                            }
                            ;
                        }
                        else if (!findData.dwFileAttributes.HasFlag(FileAttributes.Directory))
                        {
                            fileList.Add(new WIN32_FIND_DATAW(fullPath));
                            {
                                FullPath = fullPath;
                                LastWriteTime = FILETIMEExtensions.ToDateTime(findData.ftLastWriteTime);
                            }
                        }
                    }
                } while (FindNextFile(findHandle, out findData));

                directoryList.AsParallel().ForAll(x =>
                {
                    var subDirectoryFileList = new List<WIN32_FIND_DATAW>();
                    var subDirectoryDirectoryList = new List<WIN32_FIND_DATAW>();
                    if (FindNextFilePInvokeRecursive(x.FullPath, out subDirectoryFileList,
                            out subDirectoryDirectoryList))
                    {
                        lock (fileListLock)
                        {
                            fileList.AddRange(subDirectoryFileList);
                        }

                        lock (directoryListLock)
                        {
                            directoryList.AddRange(subDirectoryDirectoryList);
                        }
                    }
                });
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("Caught exception while trying to enumerate a directory. {0}", exception);
            if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
            files = null;
            directories = null;
            return false;
        }

        if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
        files = fileList;
        directories = directoryList;
        return true;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WIN32_FIND_DATAW
    {
        public FileAttributes dwFileAttributes;
        internal FILETIME ftCreationTime;
        internal FILETIME ftLastAccessTime;
        internal FILETIME ftLastWriteTime;
        public int nFileSizeHigh;
        public int nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;

        public WIN32_FIND_DATAW(string fullPath)
        {
            FullPath = fullPath;
        }

        public string FullPath { get; }
    }

    public static class FILETIMEExtensions
    {
        public static DateTime ToDateTime(FILETIME time)
        {
            var high = (ulong)time.dwHighDateTime;
            var low = (ulong)time.dwLowDateTime;
            var fileTime = (long)((high << 32) + low);
            return DateTime.FromFileTimeUtc(fileTime);
        }
    }
}