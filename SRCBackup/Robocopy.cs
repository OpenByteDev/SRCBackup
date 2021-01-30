using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace SRCBackup {
    public static class Robocopy {
        public class Command {
            public string SourceFolder;
            public string DestinationFolder;
            public string BackupFolderName;

            public IList<string> Options = new List<string>() { "FFT", "R:3", "W:5", "Z", "XJ", "EFSRAW", "S" };
            public string OptionString => Options.Select(e => (e.FirstOrDefault() != '/' ? "/" : "") + e).Aggregate("", (a, b) => a + " " + b);
            public string Arguments => $"\"{SourceFolder}\" \"{DestinationFolder}\\{BackupFolderName}\" {OptionString}";

            public Command(string sourceFolder, string destinationFolder, string backupFolderName = "Get-Date -Format \"yyyy-MM-dd-HHmmss\"", bool WrapBackupFolderName = true) {
                SourceFolder = sourceFolder;
                DestinationFolder = destinationFolder;
                BackupFolderName = WrapBackupFolderName ? "$(" + backupFolderName + ")" : backupFolderName;
            }

            public override string ToString() => "robocopy " + Arguments;
        }

        public static Process Run(Command command) {
            Process process = new Process {
                StartInfo = new ProcessStartInfo {
                    WindowStyle = ProcessWindowStyle.Normal,
                    FileName = "powershell.exe",
                    Arguments = "-Command " + command
                }
            };
            process.Start();

            return process;
        }
    }
}
