# Directory Replication Tool

A C# console application that periodically synchronizes a **replica directory** with a **source directory**.

The application ensures that the replica has the same directory structure and file contents as the source. Missing files and directories are created, changed files are replaced, and files or directories that exist only in the replica are removed.

## Features

- One-way synchronization from source to replica
- Recursive synchronization of subdirectories
- Automatic creation of missing directories
- Automatic copying of missing files
- SHA-256 file-content verification
- Replacement of files whose contents differ from the source
- Removal of files and directories that no longer exist in the source
- Periodic replication using `PeriodicTimer`
- Console logging
- Persistent logging to a `.txt` file
- Temporary-file copying before replacing the final replica file

## Requirements

- .NET SDK with support for:
  - `PeriodicTimer`
  - `TimeSpan.FromMicroseconds`
- A valid source directory
- A valid replica directory
- An existing `.txt` log file

## Usage

Run the application with exactly five arguments:

```bash
dotnet run -- "<source-directory>" "<replica-directory>" <interval> <unit> "<log-file>"
```

### Arguments

| Position | Argument | Description |
|---|---|---|
| 1 | Source directory | Directory that acts as the original source of data |
| 2 | Replica directory | Directory that will be synchronized with the source |
| 3 | Interval amount | Numeric amount of time between replication runs |
| 4 | Interval unit | Unit used for the replication interval |
| 5 | Log file | Path to an existing `.txt` file used for logging |

## Supported Interval Units

| Unit | Meaning |
|---|---|
| `d` | Days |
| `h` | Hours |
| `m` | Minutes |
| `s` | Seconds |
| `mil` | Milliseconds |
| `mic` | Microseconds |

## Examples

Run replication every 30 seconds:

```bash
dotnet run -- "C:\Data\Source" "D:\Data\Replica" 30 s "C:\Logs\replication.txt"
```

Run replication every 5 minutes:

```bash
dotnet run -- "C:\Data\Source" "D:\Data\Replica" 5 m "C:\Logs\replication.txt"
```

Run replication every 1.5 hours:

```bash
dotnet run -- "C:\Data\Source" "D:\Data\Replica" 1.5 h "C:\Logs\replication.txt"
```

Paths containing spaces should be wrapped in quotation marks:

```bash
dotnet run -- "C:\My Source Folder" "D:\Backup Folder" 10 m "C:\My Logs\replication.txt"
```

## How Replication Works

Each replication run performs the following operations:

1. Files in the current source and replica directories are compared.
2. Files missing from the replica are copied from the source.
3. Files existing in both locations are compared using SHA-256 hashes.
4. If file hashes differ, the replica file is replaced with a new copy of the source file.
5. Files that exist only in the replica are deleted.
6. Missing source directories are created in the replica.
7. Directories that exist only in the replica are deleted.
8. The same process is recursively performed for every source subdirectory.

The first replication run starts immediately when the application is launched. Additional runs are performed according to the specified interval.

## File Copying

Files are copied through a temporary file.

For a file such as:

```text
example.txt
```

the application first creates:

```text
example.txt.tmp
```

inside the replica directory.

After the copy succeeds, the temporary file is moved to the final destination:

```text
example.txt
```

This reduces the chance of leaving a partially copied file at the final replica path if the copy operation fails.

## File Comparison

Files that exist in both the source and replica directories are compared by calculating a SHA-256 hash for each file.

If the hashes are different, the replica file is treated as outdated and is replaced with the source file.

## Logging

The application logs replication activity to:

- the console;
- the configured `.txt` log file.

Example log output:

```text
[INFO] Replication run was started at 23.09.2026 14:00:00.
[INFO] Directory was created at "D:\Data\Replica\Assets".
[INFO] Temporary file was created at "D:\Data\Replica\data.json.tmp" with a content of "C:\Data\Source\data.json".
[INFO] Temporary file "D:\Data\Replica\data.json.tmp" was renamed/moved to "D:\Data\Replica\data.json".
[INFO] Content of files "C:\Data\Source\config.txt" and "D:\Data\Replica\config.txt" mismatch.
[INFO] File was removed at "D:\Data\Replica\obsolete.txt".
[INFO] Replication run was finished at 23.09.2026 14:00:01.
```

Errors related to invalid startup arguments are written to the console.

## Validation

Before replication begins, the application verifies that:

- exactly five command-line arguments were supplied;
- the source directory exists;
- the replica directory exists;
- the log file exists;
- the log file has a `.txt` extension;
- the interval amount can be parsed as a number;
- the specified interval unit is supported.

If validation fails, the application prints an error and terminates.

## Important Warning

> **The replica directory is modified destructively.**

The replica is intended to become an exact copy of the source.

This means that files and directories that exist in the replica but do **not** exist in the source will be permanently deleted.

Do not use an important directory as the replica unless its contents are intended to be controlled entirely by this application.

The synchronization is one-way:

```text
Source -> Replica
```

Changes made manually inside the replica are not copied back to the source and may be overwritten or deleted during the next replication run.

## Project Structure

The main replication process is divided into several responsibilities:

- argument and path validation;
- interval creation;
- recursive directory synchronization;
- file synchronization;
- SHA-256 content comparison;
- file and directory removal;
- logging.

## Build

Build the project with:

```bash
dotnet build
```

## Run

Run the project with:

```bash
dotnet run -- "<source-directory>" "<replica-directory>" <interval> <unit> "<log-file>"
```

## License

No license has been specified for this project.
