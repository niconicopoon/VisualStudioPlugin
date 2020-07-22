using System;
using System.IO;
// using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace MyBookmark
{
    public class CommentRichTextBox : RichTextBox, IDisposable  // TextBox, IDisposable
    {
        public string m_comment;
        private BookmarkPrim m_bookmarkPrim;

        public CommentRichTextBox() : base()
        {
            m_comment = "a";
            AppendText("a");
        }

        public CommentRichTextBox(BookmarkPrim bookmarkPrim) : base()
        {
            // this.AcceptsReturn = true;
            // this.AcceptsTab = true;
            // Document.Blocks.FirstBlock.LineHeight = 1;

            m_bookmarkPrim = bookmarkPrim;
            m_comment = bookmarkPrim.m_comment;
            AppendText("a");
        }
        public void Dispose()
        {
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {

        }

        /* うまく入力できないから、別のエディターを用意する
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            // TextRange textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
            // m_bookmarkPrim.m_comment = textRange.Text;
            // m_bookmarkPrim.m_comment = this.Text;
        }
        */

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) => Dispatcher.Invoke(() =>
        {
        });
    }
}
