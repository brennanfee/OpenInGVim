using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace OpenInGVim
{
    internal sealed class OpenVimCommand
    {
        private readonly Package _package;
        private readonly Options _options;

        private OpenVimCommand(Package package, Options options)
        {
            _package = package;
            _options = options;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));

            if (commandService == null) return;

            var menuCommandId = new CommandID(PackageGuids.guidOpenInVimCmdSet, PackageIds.OpenInVim);
            var menuItem = new MenuCommand(OpenFolderInVim, menuCommandId);
            commandService.AddCommand(menuItem);

            var ctxMenuCommandId = new CommandID(PackageGuids.guidCtxMenuOpenInVimCmdSet, PackageIds.ContextMenuOpenInVim);
            var ctxMenuCommand = new MenuCommand(OpenFileInVim, ctxMenuCommandId);
            commandService.AddCommand(ctxMenuCommand);
        }

        public static OpenVimCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, Options options)
        {
            Instance = new OpenVimCommand(package, options);
        }

        private void OpenFileInVim(object sender, EventArgs e)
        {
            try
            {
                var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
                var activeDocument = dte.ActiveDocument;
                var path = Path.Combine(activeDocument.Path, activeDocument.FullName);

                if (!string.IsNullOrEmpty(path))
                {
                    OpenVim(path, true);
                }
                else
                {
                    MessageBox.Show("Couldn't resolve the file");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OpenFolderInVim(object sender, EventArgs e)
        {
            try
            {
                var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
                var path = ProjectHelpers.GetSelectedPath(dte, _options.OpenSolutionProjectAsRegularFile);

                if (!string.IsNullOrEmpty(path))
                {
                    OpenVim(path);
                }
                else
                {
                    MessageBox.Show("Couldn't resolve the folder");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void OpenVim(string path, bool contextMenuOptionClicked = false)
        {
            EnsurePathExist();

            bool isDirectory = !contextMenuOptionClicked && Directory.Exists(path);
            var cwd = File.Exists(path) ? Path.GetDirectoryName(path) : path;

            var start = new System.Diagnostics.ProcessStartInfo()
            {
                WorkingDirectory = cwd ?? "",
                FileName = $"\"{_options.PathToExe}\"",
                Arguments = isDirectory ? "." : $"\"{path}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };

            using (System.Diagnostics.Process.Start(start))
            {
                string evt = isDirectory ? "directory" : "file";
            }
        }

        private void EnsurePathExist()
        {
            if (File.Exists(_options.PathToExe))
                return;

            var box = MessageBox.Show("I can't find Vim (gvim.exe). Would you like to help me find it?", Vsix.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (box == DialogResult.No)
                return;

            var dialog = new OpenFileDialog
            {
                DefaultExt = ".exe",
                FileName = "gvim.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                CheckFileExists = true
            };

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                _options.PathToExe = dialog.FileName;
                _options.SaveSettingsToStorage();
            }
        }
    }
}
