using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

class Program
{
    private const string ERROR_MASSAGE_DIRECTORY_NOT_EXIST = "Sorry, the path to the {0} directory you provided is invalid. Check it and restart the application.";
    private const string SOURCE_FOLDER_TEXT = "source";
    private const string REPLICA_FOLDER_TEXT = "replica";

    private static string? sourceDirectory;
    private static string? replicaDirectory;

    static void Main(string[] args)
    {
        var srcDir = args[0];
        var rplDir = args[1];

        if (!CheckDirectoryPathValidity(srcDir, rplDir))
            return;
        
        sourceDirectory = srcDir;
        replicaDirectory = rplDir;

        RunReplication(sourceDirectory, replicaDirectory);
    }

    private static bool CheckDirectoryPathValidity(string sourceDirectory, string repliceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            Console.WriteLine(string.Format(ERROR_MASSAGE_DIRECTORY_NOT_EXIST, SOURCE_FOLDER_TEXT));
            return false;
        }
        else if (!Directory.Exists(repliceDirectory))
        {
            Console.WriteLine(string.Format(ERROR_MASSAGE_DIRECTORY_NOT_EXIST, REPLICA_FOLDER_TEXT));
            return false;
        }
        return true;
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
            if (!replicaDirs.Contains(sourceDir))
                Directory.CreateDirectory($"{rplDir}\\{sourceDir}");
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
        File.Move(tmpFilePath, replicaFilePath, true);
    }

    private static void ValidateFileContent(string srcDir, string rplDir, string fileName)
    {
        var origninalFilePath = $"{srcDir}\\{fileName}";
        var replicaFilePath = $"{rplDir}\\{fileName}";

        var originalFileHash = GetFileHash(origninalFilePath);
        var replicaFileHash = GetFileHash(replicaFilePath);

        if (!originalFileHash.SequenceEqual(replicaFileHash))
        {
            CopyFile(srcDir, rplDir, fileName);
        }
    }

    private static void RemoveFiles(string dirPath, IEnumerable<string> fileNames)
    {
        foreach(var fileName in fileNames)
        {
            File.Delete($"{dirPath}\\{fileName}");
        }
    }

    private static void RemoveDirectories(string dirPath, IEnumerable<string> dirNames)
    {
        foreach(var dirName in dirNames)
        {
            Directory.Delete($"{dirPath}\\{dirName}");
        }
    }

    private static byte[] GetFileHash(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var sha = SHA256.Create();

        return sha.ComputeHash(fileStream);
    }
}