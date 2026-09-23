using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Periodically synchronizes a replica directory with a source directory.
/// Files and directories missing from the replica are created, obsolete ones
/// are removed, and files with mismatching content are replaced.
/// All replication operations are logged to the console and to a log file.
/// </summary>
/// <remarks>
/// The application expects exactly five command-line arguments:
/// <list type="number">
/// <item><description>Source directory path.</description></item>
/// <item><description>Replica directory path.</description></item>
/// <item><description>Replication interval amount.</description></item>
/// <item><description>
/// Replication interval unit: d, h, m, s, mil, or mic.
/// </description></item>
/// <item><description>Path to an existing .txt log file.</description></item>
/// </list>
/// </remarks>
class Program
{
    /// <summary>
    /// Defines the available prefixes used for log messages.
    /// </summary>
    private enum LogPrefix
    {
        ERROR, INFO
    }

    private const string ERROR_MASSAGE_DIRECTORY_NOT_EXIST = "Sorry, the path to the {0} directory you provided is invalid. Check it and restart the application.";
    private const string ERROR_MASSAGE_WRONG_NUMBER_FORMAT = "Sorry, the amount of time you provided is invalid. Use \"##.##\" format.";
    private const string ERROR_MASSAGE_WRONG_UNIT = "Sorry, the unit you provided is invalid. Use only \"d\" for days, \"h\" for hours, \"m\" for minutes, \"s\" for seconds, \"mil\" for miliseconds, \"mic\" for microseconds.";
    private const string ERROR_LOG_NOT_EXIST = "Sorry, the path to the log file is invalid. Check it and restart the application.";
    private const string ERROR_WRONG_LOG_EXTENSION = "Sorry, the log file has wrong extension. The extension should be \".txt\". Check it and restart the application.";
    private const string ERROR_WRONG_AMOUNT_OF_ARGUMENTS = "Sorry, there should be strictly 5 arguments. 1 - source folder path, 2 - replica folder path, 3 - amount of time of periodical check, 4 - unit of time of periodical check, 5 - log file path";
    private const string INFO_REPLICATION_STARTS = "Replication run was started at ";
    private const string INFO_REPLICATION_FINISHED = "Replication run was finished at {0}.\n\n";
    private const string INFO_DIRECTORY_CREATED = "Directory was created at \"{0}\".";
    private const string INFO_TMP_FILE_CREATED = "Temporary file was created at \"{0}\" with a content of \"{1}\".";
    private const string INFO_TMP_FILE_MOVED = "Temporary file \"{0}\" was renamed/moved to \"{1}\".";
    private const string INFO_CONTENT_MISMATCH = "Content of files \"{0}\" and \"{1}\" mismatch.";
    private const string INFO_FILE_REMOVED = "File was removed at \"{0}\".";
    private const string INFO_DIRECTORY_REMOVED = "Directory was removed at \"{0}\".";
    private const string SOURCE_FOLDER_TEXT = "source";
    private const string REPLICA_FOLDER_TEXT = "replica";
    private const string LOG_FILE_EXTENSION = ".txt";
    private const string LOG_PREFIX_ERROR = "[ERROR]";
    private const string LOG_PREFIX_INFO = "[INFO]";
    private const int PROPER_AMOUNT_OF_ARGS = 5;
    private static readonly string[] ACCEPTABLE_UNITS = ["d", "h", "m", "s", "mil", "mic"];

    private static string? sourceDirectory;
    private static string? replicaDirectory;
    private static string logFile = "";

    /// <summary>
    /// Contains log messages generated during the current replication run.
    /// The buffer is written to the log file after the replication finishes.
    /// </summary>
    private static StringBuilder logBuffer = new StringBuilder();

    /// <summary>
    /// Application entry point.
    /// Validates command-line arguments and periodically runs directory replication.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments containing the source directory, replica directory,
    /// interval amount, interval unit, and log file path.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous execution of the application.
    /// </returns>
    static async Task Main(string[] args)
    {
        if(!CheckProperAmountOfArguments(args))
            return;

        var srcDir = args[0];
        var rplDir = args[1];

        if (!CheckDirectoryPathValidity(srcDir, rplDir))
            return;
        
        sourceDirectory = srcDir;
        replicaDirectory = rplDir;

        var logTmpVar = args[4];
        if (!CheckLogPathValidity(logTmpVar))
            return;
        logFile = logTmpVar;

        var formatCheck = CheckForProperNumberFormat(args[2]);
        if(formatCheck == null)
            return;
        var amountOfTime = (double) formatCheck;

        var unit = args[3];
        if(!CheckForProperUnitFormat(unit))
            return;
        var unitOfMesurment = unit;

        var interval = GetInterval(unitOfMesurment, amountOfTime);

        using PeriodicTimer timer = new PeriodicTimer(interval);

        do
        {
            Log($"{INFO_REPLICATION_STARTS}{DateTime.Now}.", LogPrefix.INFO);
            RunReplication(sourceDirectory, replicaDirectory);
            Log(string.Format(INFO_REPLICATION_FINISHED, DateTime.Now), LogPrefix.INFO);
            WriteLogsToFile();
        }
        while(await timer.WaitForNextTickAsync());
    }

    /// <summary>
    /// Checks whether the application received the required number of command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>
    /// <see langword="true"/> if the number of arguments is correct;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool CheckProperAmountOfArguments(string[] args)
    {
        if(args.Count() != PROPER_AMOUNT_OF_ARGS)
        {
            Log(ERROR_WRONG_AMOUNT_OF_ARGUMENTS, LogPrefix.ERROR, false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether both the source and replica directories exist.
    /// </summary>
    /// <param name="sourceDirectory">Path to the source directory.</param>
    /// <param name="replicaDirectory">Path to the replica directory.</param>
    /// <returns>
    /// <see langword="true"/> if both directories exist;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool CheckDirectoryPathValidity(string sourceDirectory, string replicaDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            Log(string.Format(ERROR_MASSAGE_DIRECTORY_NOT_EXIST, SOURCE_FOLDER_TEXT), LogPrefix.ERROR, false);
            return false;
        }
        else if (!Directory.Exists(replicaDirectory))
        {
            Log(string.Format(ERROR_MASSAGE_DIRECTORY_NOT_EXIST, REPLICA_FOLDER_TEXT), LogPrefix.ERROR, false);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Validates the path of the log file.
    /// </summary>
    /// <param name="logPath">Path to the log file.</param>
    /// <returns>
    /// <see langword="true"/> if the file exists and has a .txt extension;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool CheckLogPathValidity(string logPath)
    {
        if (!File.Exists(logPath))
        {
            Log(string.Format(ERROR_LOG_NOT_EXIST, SOURCE_FOLDER_TEXT), LogPrefix.ERROR, false);
            return false;
        }
        else if (!logPath.EndsWith(".txt"))
        {
            Log(string.Format(ERROR_WRONG_LOG_EXTENSION, REPLICA_FOLDER_TEXT), LogPrefix.ERROR,false);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Attempts to parse a string into a floating-point number representing
    /// the replication interval amount.
    /// </summary>
    /// <param name="numberToParse">String containing the number to parse.</param>
    /// <returns>
    /// The parsed number if parsing succeeds; otherwise, <see langword="null"/>.
    /// </returns>
    private static double? CheckForProperNumberFormat(string numberToParse)
    {
        double amountOfTime;
        try
        {
            amountOfTime = double.Parse(numberToParse);
        }
        catch (Exception)
        {
            Log(ERROR_MASSAGE_WRONG_NUMBER_FORMAT, LogPrefix.ERROR, false);
            return null;
        }

        return amountOfTime;
    }

    /// <summary>
    /// Checks whether the supplied time unit is supported by the application.
    /// </summary>
    /// <param name="unit">
    /// Time unit to validate. Supported values are:
    /// d, h, m, s, mil, and mic.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the unit is supported;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool CheckForProperUnitFormat(string unit)
    {
        if(!ACCEPTABLE_UNITS.Contains(unit))
        {
            Log(ERROR_MASSAGE_WRONG_UNIT, LogPrefix.ERROR, false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Converts an interval amount and unit into a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="unit">
    /// Unit of time: d, h, m, s, mil, or mic.
    /// </param>
    /// <param name="amount">Amount of time expressed in the specified unit.</param>
    /// <returns>
    /// A <see cref="TimeSpan"/> representing the requested interval.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when an unsupported time unit is supplied.
    /// </exception>
    private static TimeSpan GetInterval(string unit, double amount)
    {
        return unit switch
        {
            "d" => TimeSpan.FromDays(amount),
            "h" => TimeSpan.FromHours(amount),
            "m" => TimeSpan.FromMinutes(amount),
            "s" => TimeSpan.FromSeconds(amount),
            "mil" => TimeSpan.FromMilliseconds(amount),
            "mic" => TimeSpan.FromMicroseconds(amount),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// Recursively synchronizes a replica directory with a source directory.
    /// </summary>
    /// <param name="srcDir">Current source directory.</param>
    /// <param name="rplDir">Corresponding replica directory.</param>
    /// <remarks>
    /// The method first synchronizes files in the current directory,
    /// then verifies subdirectories and recursively processes each source subdirectory.
    /// </remarks>
    private static void RunReplication(string srcDir, string rplDir)
    {
        FileVerification(srcDir, rplDir);
        foreach(var dirName in DirectoryVerification(srcDir, rplDir))
        {
            RunReplication($"{srcDir}\\{dirName}", $"{rplDir}\\{dirName}");
        }
    }

    /// <summary>
    /// Synchronizes the directory structure between a source and replica directory.
    /// </summary>
    /// <param name="srcDir">Source directory to inspect.</param>
    /// <param name="rplDir">Replica directory to synchronize.</param>
    /// <returns>
    /// The names of all source subdirectories that should be recursively processed.
    /// </returns>
    /// <remarks>
    /// Missing directories are created in the replica.
    /// Directories that exist only in the replica are removed.
    /// </remarks>
    private static IEnumerable<string> DirectoryVerification(string srcDir, string rplDir)
    {
        var sourceDirs = Directory.EnumerateDirectories(srcDir).Select(dir => Path.GetFileName(dir)).ToHashSet();
        var replicaDirs = Directory.EnumerateDirectories(rplDir).Select(dir => Path.GetFileName(dir)).ToHashSet();

        foreach(var sourceDir in sourceDirs)
        {
            if (!replicaDirs.Contains(sourceDir)){
                var newDir = $"{rplDir}\\{sourceDir}";
                Directory.CreateDirectory(newDir);
                Log(string.Format(INFO_DIRECTORY_CREATED, newDir), LogPrefix.INFO);
            }
            else
                replicaDirs.Remove(sourceDir);
        }

        RemoveDirectories(rplDir, replicaDirs);

        return sourceDirs;
    }

    /// <summary>
    /// Synchronizes files in a source directory with the corresponding replica directory.
    /// </summary>
    /// <param name="srcDir">Source directory containing the original files.</param>
    /// <param name="rplDir">Replica directory containing replicated files.</param>
    /// <remarks>
    /// Missing files are copied to the replica.
    /// Existing files are compared using SHA-256 hashes.
    /// Files existing only in the replica are deleted.
    /// </remarks>
    private static void FileVerification(string srcDir, string rplDir)
    {
        var sourceFiles = Directory.EnumerateFiles(srcDir).Select(file => Path.GetFileName(file)).ToHashSet();
        var replicaFiles = Directory.EnumerateFiles(rplDir).Select(file => Path.GetFileName(file)).ToHashSet();

        foreach(var sourceFile in sourceFiles)
        {
            if(!replicaFiles.Contains(sourceFile))
                CopyFile(srcDir, rplDir, sourceFile);
            else{
                ValidateFileContent(srcDir, rplDir, sourceFile);
                replicaFiles.Remove(sourceFile);
            }
        }

        RemoveFiles(rplDir, replicaFiles);
    }

    /// <summary>
    /// Copies a file from the source directory to the replica directory.
    /// </summary>
    /// <param name="srcDir">Directory containing the original file.</param>
    /// <param name="rplDir">Destination replica directory.</param>
    /// <param name="fileName">Name of the file to copy.</param>
    /// <remarks>
    /// The file is first copied to a temporary file and is then moved to
    /// the final replica path. This reduces the chance of leaving a partially
    /// copied final file if copying fails.
    /// </remarks>
    private static void CopyFile(string srcDir, string rplDir, string fileName)
    {
        var tmpFilePath = $"{rplDir}\\{fileName}.tmp";
        var origninalFilePath = $"{srcDir}\\{fileName}";
        var replicaFilePath = $"{rplDir}\\{fileName}";

        File.Copy(origninalFilePath, tmpFilePath, true);
        Log(string.Format(INFO_TMP_FILE_CREATED, tmpFilePath, origninalFilePath), LogPrefix.INFO);
        File.Move(tmpFilePath, replicaFilePath, true);
        Log(string.Format(INFO_TMP_FILE_MOVED, tmpFilePath, replicaFilePath), LogPrefix.INFO);
    }

    /// <summary>
    /// Compares the contents of a source file and its replica using SHA-256 hashes.
    /// </summary>
    /// <param name="srcDir">Directory containing the original file.</param>
    /// <param name="rplDir">Directory containing the replica file.</param>
    /// <param name="fileName">Name of the file to compare.</param>
    /// <remarks>
    /// If the hashes differ, the replica file is replaced with a new copy
    /// of the source file.
    /// </remarks>
    private static void ValidateFileContent(string srcDir, string rplDir, string fileName)
    {
        var origninalFilePath = $"{srcDir}\\{fileName}";
        var replicaFilePath = $"{rplDir}\\{fileName}";

        var originalFileHash = GetFileHash(origninalFilePath);
        var replicaFileHash = GetFileHash(replicaFilePath);
        
        if (!originalFileHash.SequenceEqual(replicaFileHash))
        {
            Log(string.Format(INFO_CONTENT_MISMATCH, origninalFilePath, replicaFilePath), LogPrefix.INFO);
            CopyFile(srcDir, rplDir, fileName);
        }
    }

    /// <summary>
    /// Removes the specified files from a directory.
    /// </summary>
    /// <param name="dirPath">Directory containing the files.</param>
    /// <param name="fileNames">Names of the files to remove.</param>
    private static void RemoveFiles(string dirPath, IEnumerable<string> fileNames)
    {
        foreach(var fileName in fileNames)
        {
            var fileRemovePath = $"{dirPath}\\{fileName}";
            File.Delete(fileRemovePath);
            Log(string.Format(INFO_FILE_REMOVED, fileRemovePath), LogPrefix.INFO);
        }
    }

    /// <summary>
    /// Recursively removes the specified directories.
    /// </summary>
    /// <param name="dirPath">Parent directory containing the directories.</param>
    /// <param name="dirNames">Names of directories to remove.</param>
    private static void RemoveDirectories(string dirPath, IEnumerable<string> dirNames)
    {
        foreach(var dirName in dirNames)
        {
            var removeDirPath = $"{dirPath}\\{dirName}";
            Directory.Delete(removeDirPath, true);
            Log(string.Format(INFO_DIRECTORY_REMOVED, removeDirPath), LogPrefix.INFO);
        }
    }

    /// <summary>
    /// Calculates the SHA-256 hash of a file.
    /// </summary>
    /// <param name="filePath">Path to the file whose hash should be calculated.</param>
    /// <returns>
    /// A byte array containing the SHA-256 hash of the file.
    /// </returns>
    private static byte[] GetFileHash(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var sha = SHA256.Create();

        return sha.ComputeHash(fileStream);
    }

    /// <summary>
    /// Writes a message to the console and optionally adds it to the log buffer.
    /// </summary>
    /// <param name="content">Message to log.</param>
    /// <param name="logPrefix">Type of log message.</param>
    /// <param name="logToFile">
    /// If <see langword="true"/>, adds the message to the log buffer.
    /// If <see langword="false"/>, writes it only to the console.
    /// </param>
    private static void Log(string content, LogPrefix logPrefix, bool logToFile = true)
    {
        var info = $"{LogPrefixToString(logPrefix)} {content}";
        if (logToFile) logBuffer.Append($"{info}\n");
        Console.WriteLine(info);
    }

    /// <summary>
    /// Converts a <see cref="LogPrefix"/> value into the corresponding
    /// textual log prefix.
    /// </summary>
    /// <param name="logPrefix">Log prefix type.</param>
    /// <returns>
    /// A string such as "[INFO]" or "[ERROR]".
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when an unsupported log prefix is supplied.
    /// </exception>
    private static string LogPrefixToString(LogPrefix logPrefix)
    {
        return logPrefix switch
    {
        LogPrefix.INFO => LOG_PREFIX_INFO,
        LogPrefix.ERROR => LOG_PREFIX_ERROR,
        _ => throw new NotImplementedException()
    };
    }

    /// <summary>
    /// Appends all buffered log messages to the configured log file
    /// and clears the buffer.
    /// </summary>
    private static void WriteLogsToFile()
    {
        using (var streamWriter = new StreamWriter(logFile, append: true))
        {
            var log = logBuffer.ToString();
            streamWriter.WriteLine(log);
            logBuffer.Clear();
        }
    }
}