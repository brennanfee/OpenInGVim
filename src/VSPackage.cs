using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace OpenInGVim
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Options), "VsVim", Vsix.Name, 101, 102, true, new string[0], ProvidesLocalizedCategoryName = false)]
    [Guid(PackageGuids.guidPackageString)]
    public sealed class VSPackage : AsyncPackage
    {
        protected override Task InitializeAsync(System.Threading.CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            var options = (Options)GetDialogPage(typeof(Options));
            Logger.Initialize(ServiceProvider.GlobalProvider, Vsix.Name);
            OpenVimCommand.Initialize(this, options);

            return Task.FromResult<object>(null);
        }
    }
}
