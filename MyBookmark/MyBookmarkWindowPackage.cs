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
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(MyBookmarkWindow))]
    [Guid(MyBookmarkWindowPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]        // これで sln 読み込み時に InitializeAsync が呼ばれる


    /*
    [ProvideOptionPageAttribute(typeof(SccProviderOptions), "Source Control", "Sample Options Page Basic Provider", 106, 107, false)]
    [ProvideToolsOptionsPageVisibility("Source Control", "Sample Options Page Basic Provider", "ADC98052-0000-41D1-A6C3-704E6C1A3DE2")]
    [ProvideToolWindow(typeof(SccProviderToolWindow))]
    [ProvideToolWindowVisibility(typeof(SccProviderToolWindow), "ADC98052-0000-41D1-A6C3-704E6C1A3DE2")]
    [ProvideService(typeof(SccProviderService), ServiceName = "Source Control Sample Basic Provider Service")]
    [@ProvideSourceControlProvider("Managed Source Control Sample Basic Provider", "#100")]
    */

    public sealed class MyBookmarkWindowPackage : AsyncPackage
    {
        public static readonly Guid guidMenuAndCommandsCmdSet = new Guid("C60C4486-09C4-4076-9D8C-7DEB89E42C01");
        public static int cmdidMyCommand = 0x2001;

        /// <summary>
        /// MyBookmarkWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "54e9361a-1a6f-41be-8a6e-9b1581a493b1";

        /// <summary>
        /// Initializes a new instance of the <see cref="MyBookmarkWindowPackage"/> class.
        /// </summary>
        public MyBookmarkWindowPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        // private SccProviderService sccService = null;

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            /* OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID id = new CommandID(GuidsList.guidMenuAndCommandsCmdSet, PkgCmdIDList.cmdidMyCommand);
                // Now create the OleMenuCommand object for this command. The EventHandler object is the
                // function that will be called when the user will select the command.
                OleMenuCommand command = new OleMenuCommand(new EventHandler(MenuCommandCallback), id);
                // Add the command to the command service.
                mcs.AddCommand(command);


                // Now create one object derived from MenuCommand for each command defined in
                // the VSCT file and add it to the command service.

                // For each command we have to define its id that is a unique Guid/integer pair.
                CommandID id = new CommandID(guidMenuAndCommandsCmdSet, cmdidMyCommand);
                // Now create the OleMenuCommand object for this command. The EventHandler object is the
                // function that will be called when the user will select the command.
                OleMenuCommand command = new OleMenuCommand(new EventHandler(MenuCommandCallback), id);
                // Add the command to the command service.
                mcs.AddCommand(command);
            } */

            bool isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
            {
                SolutionEvents.OnAfterBackgroundSolutionLoadComplete += HandleOpenSolution;
            }

            // Listen for subsequent solution events
            
            
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await MyBookmarkWindowCommand.InitializeAsync(this);
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
