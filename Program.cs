using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

class Program
{
    private const string ERROR_MASSAGE_DIRECTORY_NOT_EXIST = "Sorry, the path to the {0} directory you provided is invalid. Check it and restart the application.";
    private const string SOURCE_FOLDER_TEXT = "source";
    private const string REPLICA_FOLDER_TEXT = "replica";

    private static string? sourceDirectory;
    private static string? replicaDirectory;

    static void Main(string[] args)
    {
        if (!CheckDirectoryPathValidity(args[0], args[1]))
            return;
        
        sourceDirectory = args[0];
        replicaDirectory = args[1];

        List<string> sourceFiles = (List<string>) Directory.EnumerateFiles(sourceDirectory).OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
        List<string> replicaFiles = (List<string>) Directory.EnumerateFiles(replicaDirectory).OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

        for(var i = 0; i < sourceFiles.Count; i++)
        {
            //
        }
        
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
}