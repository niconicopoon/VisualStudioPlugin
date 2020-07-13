using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace MyBookmark
{
    public class CommentRichTextBox : RichTextBox, IDisposable
    {
        private VariableExpander _variableExpander;


        public CommentRichTextBox(VariableExpander variableExpander) : base()
        {
            _variableExpander = variableExpander ?? throw new ArgumentNullException("variableExpander");
        }
        public void Dispose()
        {
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) => Dispatcher.Invoke(() =>
        {
        });
    }
}
