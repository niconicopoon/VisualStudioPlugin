

namespace MyBookmark
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ToolWindowControl.
    /// </summary>
    public partial class ToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowControl"/> class.
        /// </summary>
        public ToolWindowControl()
        {
            this.InitializeComponent();

            // m_BookmarkTreeView
        }

        public void SetBookmarkTreeView()
        {
            System.Collections.Concurrent.ConcurrentDictionary<string, string> heads = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();


            foreach (var bookmarkPrimsIt in MyBookmarkManager.GetInstance().m_FileBookmarkPrims)
            {
                foreach (var bookmarkPrimIt in bookmarkPrimsIt.Value)
                {
                    /*
                    bookmarkPrimIt.Value.line
                    heads.


                    m_BookmarkTreeView.
.
                    bookmarkPrimIt.Value.m_comment = 
                    */
                }
            }
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "MyBookmarkWindow");
        }
    }
}