using System.Windows;
using System.Windows.Controls;

namespace BookmarkToolWindow.ToolWindows
{
    public partial class BookmarkToolWindowControl : UserControl
    {
        private BookmarkWindowState _state;

        public BookmarkToolWindowControl(BookmarkWindowState state)
        {
            _state = state;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string version = _state.DTE.FullName;

            MessageBox.Show($"Visual Studio is located here: '{version}'");
        }
    }
}
