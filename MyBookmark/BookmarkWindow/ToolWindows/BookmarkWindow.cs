using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace BookmarkToolWindow.ToolWindows
{
    [Guid(WindowGuidString)]
    public class BookmarkWindow : ToolWindowPane
    {
        public const string WindowGuidString = "e4e2ba26-a455-4c53-adb3-8225fb696f8b"; // Replace with new GUID in your own code
        public const string Title = "My Bookmark Window";

        // "state" parameter is the object returned from MyPackage.InitializeToolWindowAsync
        public BookmarkWindow(BookmarkWindowState state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;

            Content = new BookmarkToolWindowControl(state);
        }
    }
}
