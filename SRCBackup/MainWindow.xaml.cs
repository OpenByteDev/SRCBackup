using SRCBackup.Properties;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using static SRCBackup.Robocopy;
using System.Text;
using Trigger = Microsoft.Win32.TaskScheduler.Trigger;

namespace SRCBackup {

    public partial class MainWindow : Window {

        private static readonly IDictionary<string, Trigger> TaskTriggers = new Dictionary<string, Trigger>() {
            { "Daily", new DailyTrigger() },
            { "Weekly", new WeeklyTrigger() },
            { "Monthly", new MonthlyTrigger() }
        };

        string TaskSourceFolder => Settings.Default.SourceFolder;
        string TaskDestinationFolder => Settings.Default.DestinationFolder;
        private Trigger ProtoTaskTrigger => TaskTriggers.GetOrDefault(Settings.Default.Trigger, TaskTriggers.Values.FirstOrDefault);
        private Trigger TaskTrigger {
            get {
                var trigger = ProtoTaskTrigger;
                var now = DateTime.Now;
                var taskTime = TaskTime;
                var taskDateTime = new DateTime(now.Year, now.Month, now.Day, TaskTime.Hours, TaskTime.Minutes, TaskTime.Seconds, TaskTime.Milliseconds, now.Kind);
                trigger.StartBoundary = taskDateTime;
                return trigger;
            }
        }
        TimeSpan TaskTime => Settings.Default.Time.TimeOfDay;
        bool TaskActive => Settings.Default.Active;

        public MainWindow() {
            InitializeComponent();

            ContentRendered += (sender, args) => {
                source.Text = Settings.Default.SourceFolder;
                source.ValueChanged += (s, a) => {
                    Settings.Default.SourceFolder = a.NewValue;
                    Settings.Default.Save();
                    SetActive(false);
                };
                destination.Text = Settings.Default.DestinationFolder;
                destination.ValueChanged += (s, a) => {
                    Settings.Default.DestinationFolder = a.NewValue;
                    Settings.Default.Save();
                    SetActive(false);
                };
                
                combo.ItemsSource = TaskTriggers.Keys;
                combo.SelectedItem = Settings.Default.Trigger;
                combo.SelectionChanged += (s, a) => {
                    Settings.Default.Trigger = (string)combo.SelectedValue;
                    Settings.Default.Save();
                    SetActive(false);
                };

                time.Value = Settings.Default.Time;
                time.ValueChanged += (s, a) => {
                    Settings.Default.Time = time.Value ?? DateTime.Now;
                    Settings.Default.Save();
                    SetActive(false);
                };

                active.Content = ActivateText(Settings.Default.Active);
                active.Click += (s, a) => ToggleActive();

                backup.Click += (s, a) => {
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WindowStyle = ProcessWindowStyle.Normal;
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = "-EncodedCommand \"" + ScriptFromSettings() + "\"";
                    process.StartInfo = startInfo;
                    process.Start();
                };
            };
        }
        private void ToggleActive() {
            SetActive(!Settings.Default.Active);
        }
        private void SetActive(bool state = true) {
            if (Settings.Default.Active == state)
                return;
            Settings.Default.Active = state;
            Settings.Default.Save();
            active.Content = ActivateText(Settings.Default.Active);
            SetScheduledBackupTaskActive(Settings.Default.Active);
        }

        private Command CommandFromSettings() {
            return new Command(Settings.Default.SourceFolder, Settings.Default.DestinationFolder);
        }
        private string ScriptFromSettings() {
            var command = CommandFromSettings();
            var script = @"
$file = """ + command.DestinationFolder + @"\last_access_date.txt"";
$currdate = Get-Date -Format ""yyyyMMdd"";
$maxlad_flag = $true;

try {
    $lbd = Get-Content -Path $file -ErrorAction Stop;
} catch { $maxlad_flag = $false; }

try {
    $currdate | Set-Content -Path $file -ErrorAction Stop;
} catch { }
" + command.ToString() + @"(&{ If($maxlad_flag) { ""/MAXAGE:$lbd""} Else { """" } })";
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            //return Regex.Replace(script, @"\t|\n|\r", " ").Replace("\"", "\\\"");
        }

        private string ActivateText(bool active) {
            return active ? "Deactivate" : "Activate";
        }


        private static string ScheduledBackupTaskName = @"BackupTask";

        private Task SetScheduledBackupTaskActive(bool active) {
            using (TaskService ts = new TaskService())
                return SetScheduledBackupTaskActive(active, ts);
        }
        private Task SetScheduledBackupTaskActive(bool active, TaskService ts) {
            if (IsScheduledBackupTaskActive(ts))
                RemoveScheduledBackupTask(ts);
            return active ? AddScheduledBackupTask(ts) : null;
        }
        private void RemoveScheduledBackupTask() {
            using (TaskService ts = new TaskService())
                RemoveScheduledBackupTask(ts);
        }
        private void RemoveScheduledBackupTask(TaskService ts) {
            ts.RootFolder.DeleteTask(ScheduledBackupTaskName, false);
        }

        private bool IsScheduledBackupTaskActive() {
            using (TaskService ts = new TaskService())
                return IsScheduledBackupTaskActive(ts);
        }
        private bool IsScheduledBackupTaskActive(TaskService ts) {
            return GetScheduledBackupTask(ts) != null;
        }

        private Task GetScheduledBackupTask() {
            using (TaskService ts = new TaskService())
                return GetScheduledBackupTask(ts);
        }
        private Task GetScheduledBackupTask(TaskService ts) {
            return ts.FindTask(ScheduledBackupTaskName);
        }

        private Task AddScheduledBackupTask() {
            using (TaskService ts = new TaskService())
                return AddScheduledBackupTask(ts);
        }
        private Task AddScheduledBackupTask(TaskService ts) {
            TaskDefinition td = ts.NewTask();
            td.RegistrationInfo.Author = "Martin Braunsperger";
            td.RegistrationInfo.Description = "Backup Task";
            td.RegistrationInfo.Date = DateTime.Now;
            td.Settings.AllowDemandStart = true;
            td.Settings.AllowHardTerminate = true;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.DisallowStartOnRemoteAppSession = true;
            td.Settings.Enabled = TaskActive;
            td.Settings.ExecutionTimeLimit = TimeSpan.FromHours(1);
            td.Settings.Hidden = true;
            td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
            td.Settings.RestartCount = 5;
            td.Settings.RestartInterval = TimeSpan.FromHours(1);
            td.Settings.RunOnlyIfIdle = false;
            td.Settings.RunOnlyIfNetworkAvailable = true;
            td.Settings.StartWhenAvailable = true;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Triggers.Add(TaskTrigger);
            td.Actions.Add(new ExecAction("powershell.exe", "-NonInteractive -NoLogo -WindowStyle hidden -EncodedCommand \"" + ScriptFromSettings() + "\""));
            return ts.RootFolder.RegisterTaskDefinition(ScheduledBackupTaskName, td);
        }
    }
}
