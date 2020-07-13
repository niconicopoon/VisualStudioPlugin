using System.Windows;
using System.Windows.Controls;

namespace MyBookmarkWindow.ToolWindows
{
    public partial class MyBookmarkToolWindowControl : UserControl
    {
        private MyBookmarkToolWindowState _state;

        public MyBookmarkToolWindowControl(MyBookmarkToolWindowState state)
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
