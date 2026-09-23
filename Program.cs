using System.Security.Cryptography;
using System.Text;

class Program
{
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

    private static StringBuilder logBuffer = new StringBuilder();

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

    private static bool CheckProperAmountOfArguments(string[] args)
    {
        if(args.Count() != PROPER_AMOUNT_OF_ARGS)
        {
            Log(ERROR_WRONG_AMOUNT_OF_ARGUMENTS, LogPrefix.ERROR, false);
            return false;
        }

        return true;
    }

    private static bool CheckDirectoryPathValidity(string sourceDirectory, string repliceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            Log(string.Format(ERROR_MASSAGE_DIRECTORY_NOT_EXIST, SOURCE_FOLDER_TEXT), LogPrefix.ERROR, false);
            return false;
        }
        else if (!Directory.Exists(repliceDirectory))
        {
            Log(string.Format(ERROR_MASSAGE_DIRECTORY_NOT_EXIST, REPLICA_FOLDER_TEXT), LogPrefix.ERROR, false);
            return false;
        }
        return true;
    }

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

    private static bool CheckForProperUnitFormat(string unit)
    {
        if(!ACCEPTABLE_UNITS.Contains(unit))
        {
            Log(ERROR_MASSAGE_WRONG_UNIT, LogPrefix.ERROR, false);
            return false;
        }

        return true;
    }

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

    private static void RunReplication(string srcDir, string rplDir)
    {
        FileVerification(srcDir, rplDir);
        foreach(var dirName in DirectoryVerification(srcDir, rplDir))
        {
            RunReplication($"{srcDir}\\{dirName}", $"{rplDir}\\{dirName}");
        }
    }

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

    private static void RemoveFiles(string dirPath, IEnumerable<string> fileNames)
    {
        foreach(var fileName in fileNames)
        {
            var fileRemovePath = $"{dirPath}\\{fileName}";
            File.Delete(fileRemovePath);
            Log(string.Format(INFO_FILE_REMOVED, fileRemovePath), LogPrefix.INFO);
        }
    }

    private static void RemoveDirectories(string dirPath, IEnumerable<string> dirNames)
    {
        foreach(var dirName in dirNames)
        {
            var removeDirPath = $"{dirPath}\\{dirName}";
            Directory.Delete(removeDirPath, true);
            Log(string.Format(INFO_DIRECTORY_REMOVED, removeDirPath), LogPrefix.INFO);
        }
    }

    private static byte[] GetFileHash(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var sha = SHA256.Create();

        return sha.ComputeHash(fileStream);
    }

    private static void Log(string content, LogPrefix logPrefix, bool logToFile = true)
    {
        var info = $"{LogPrefixToString(logPrefix)} {content}";
        if (logToFile) logBuffer.Append($"{info}\n");
        Console.WriteLine(info);
    }

    private static string LogPrefixToString(LogPrefix logPrefix)
    {
        return logPrefix switch
    {
        LogPrefix.INFO => LOG_PREFIX_INFO,
        LogPrefix.ERROR => LOG_PREFIX_ERROR,
        _ => throw new NotImplementedException()
    };
    }

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