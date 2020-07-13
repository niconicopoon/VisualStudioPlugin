namespace MyBookmark
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("7a4ff12d-c8e1-4617-aa99-f897be4df08a")]
    public class MyBookmarkWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MyBookmarkWindow"/> class.
        /// </summary>
        public MyBookmarkWindow() : base(null)
        {
            this.Caption = "MyBookmarkWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new MyBookmarkWindowControl();
        }
    }
}
