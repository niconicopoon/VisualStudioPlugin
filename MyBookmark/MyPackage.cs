using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace MyBookmark
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ToolWindow))]
    [Guid(MyPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]        // これで sln 読み込み時に InitializeAsync が呼ばれる


    [ProvideToolsOptionsPageVisibility("Source Control", "Sample Options Page Basic Provider", "ADC98052-0000-41D1-A6C3-704E6C1A3DE2")]
    [ProvideToolWindow(typeof(ToolWindow))]
    [ProvideToolWindowVisibility(typeof(ToolWindow), "ADC98052-0000-41D1-A6C3-704E6C1A3DE2")]
    [ProvideAutoLoad("ADC98052-0000-41D1-A6C3-704E6C1A3DE2")]

    // [ProvideOptionPageAttribute(typeof(BookmarkOptions), "Source Control", "Sample Options Page Basic Provider", 106, 107, false)]
    // [ProvideService(typeof(BookmarkService), ServiceName = "Source Control Sample Basic Provider Service")]
    // [@ProvideSourceControlProvider("Managed Source Control Sample Basic Provider", "#100")]

    public sealed class MyPackage : AsyncPackage
    {
        public static readonly Guid guidMenuAndCommandsCmdSet = new Guid("C60C4486-09C4-4076-9D8C-7DEB89E42C01");
        public const int cmdidMyCommand = 0x2001;

        public const int CommandIdIcmdBookmarkAddCommand = 0x101;
        public const int CommandIdIcmdBookmarkDelCommand = 0x102;

        /// <summary>
        /// MyPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "54e9361a-1a6f-41be-8a6e-9b1581a493b1";

        /// <summary>
        /// Initializes a new instance of the <see cref="MyPackage"/> class.
        /// </summary>
        public MyPackage()
        {
        }

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID cmd = null;
                MenuCommand menuCmd = null;

                cmd = new CommandID(GuidList.guidBookmarkCmdSet, CommandIdIcmdBookmarkAddCommand);
                menuCmd = new MenuCommand(new EventHandler(OnBookmarkAddCommand), cmd);
                mcs.AddCommand(menuCmd);

                cmd = new CommandID(GuidList.guidBookmarkCmdSet, CommandIdIcmdBookmarkDelCommand);
                menuCmd = new MenuCommand(new EventHandler(OnBookmarkDelCommand), cmd);
                mcs.AddCommand(menuCmd);
            }

            bool isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
            {
                SolutionEvents.OnAfterBackgroundSolutionLoadComplete += HandleOpenSolution;
            }

            // Listen for subsequent solution events
            
            await ToolWindowCommand.InitializeAsync(this);
        }

        private void OnBookmarkAddCommand(object sender, EventArgs e)
        {
            MyBookmarkManager.GetInstance().AddEditBookmark();

            // Toggle the checked state of this command
            /* MenuCommand thisCommand = sender as MenuCommand;
            if (thisCommand != null)
            {
                thisCommand.Checked = !thisCommand.Checked;
            } */
        }

        private void OnBookmarkDelCommand(object sender, EventArgs e)
        {
            MyBookmarkManager.GetInstance().DelBookmark();

            // Toggle the checked state of this command
            /* MenuCommand thisCommand = sender as MenuCommand;
            if (thisCommand != null)
            {
                thisCommand.Checked = !thisCommand.Checked;
            } */
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        private void HandleOpenSolution(object sender = null, EventArgs e = null)
        {
            // Handle the open solution and try to do as much work
            // on a background thread as possible
        }



        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.MenuCommands.MenuCommandsPackage.OutputCommandString(System.String)")]
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            // OutputCommandString("Sample Command Callback.");
        }
        #endregion
    }
}
