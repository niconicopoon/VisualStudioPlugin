using System;
using System.IO;
// using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace MyBookmark
{
    public class CommentRichTextBox : RichTextBox, IDisposable
    {
        private BookmarkPrim m_bookmarkPrim;


        public CommentRichTextBox(BookmarkPrim bookmarkPrim) : base()
        {
            this.AcceptsReturn = true;
            this.AcceptsTab = true;

            m_bookmarkPrim = bookmarkPrim;
            // Document.Blocks.FirstBlock.LineHeight = 1;
            AppendText(bookmarkPrim.m_comment);
        }
        public void Dispose()
        {
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            TextRange textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
            m_bookmarkPrim.m_comment = textRange.Text;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) => Dispatcher.Invoke(() =>
        {
        });
    }
}
