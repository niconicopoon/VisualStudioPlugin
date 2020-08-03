namespace MyBookmark
{
    using System;
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
        /// 

        static ToolWindowControl m_Instance;

        static public TreeView GetBookmarkTreeView()
        {
            return m_Instance.m_BookmarkTreeView;
        }

        static public ToolWindowControl GetInstance()
        {
            return m_Instance;
        }

        public ToolWindowControl()
        {
            m_Instance = this;
            this.InitializeComponent();
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

        private void BookmarkTreeViewSelectionChanged(object sender, RoutedPropertyChangedEventArgs<Object> e)
        {
            // MessageBox.Show(((TreeViewItem)e.NewValue).Header.ToString());
            MyBookmarkManager.Jump(((TreeViewItem)e.NewValue).Header.ToString());
        }
    }
}