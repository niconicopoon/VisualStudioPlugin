// 1
// 2
// 3
// 4
// 5
// 6
// 7
// 8
// 9

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
		
		public CommentRichTextBox(BookmarkPrim bookmarkPrim) : base()
		{
			// this.AcceptsReturn = true;
			// this.AcceptsTab = true;
			Document.Blocks.FirstBlock.LineHeight = 1;

			m_bookmarkPrim = bookmarkPrim;

			m_comment = bookmarkPrim.m_comment;
			AppendText(m_comment);
		}


		
		public void Dispose()
		{
		}

		private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) = > Dispatcher.Invoke(() = >
		{
		});
	}
}
