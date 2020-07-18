using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Windows;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Windows.Documents;
using Span = Microsoft.VisualStudio.Text.Span;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;

namespace MyBookmark
{
    public class CommentImageTest : Image, IDisposable
    {
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// CommentsAdornment places red boxes behind all the "a"s in the editor window
    /// </summary>
    internal sealed class CommentsManager : ITagger<ErrorTag>, IDisposable
    {
        // [Import(typeof(IVsEditorAdaptersFactoryService))]
        // internal IVsEditorAdaptersFactoryService editorFactory = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentsAdornment"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for</param>
        static CommentsManager()
        {
            Enabled = true;
        }

        public static void ToggleEnabled()
        {
            Enabled = !Enabled;
            string message = "Memeful comments " + (Enabled ? "enabled" : "disabled") + ". Scroll editor window(s) to update.";
            UIMessage.Show(message);
        }

        public static bool Enabled { get; set; }
        public IWpfTextView GetView() { return _view; }

        public ConcurrentDictionary<int, CommentImage> Images { get; set; }
        public ConcurrentDictionary<int, CommentRichTextBox> RichTextBoxs { get; set; }     // #eiichi

        private IAdornmentLayer _layer;
        private IWpfTextView _view;
        private VariableExpander _variableExpander;
        private ITextDocumentFactoryService _textDocumentFactory;
        private string _contentTypeName;
        private bool _initialised1 = false;
        private bool _initialised2 = false;
        private List<ITagSpan<ErrorTag>> _errorTags;

        private List<string> _processingUris = new List<string>();
        private ConcurrentDictionary<WebClient, ImageParameters> _toaddImages = new ConcurrentDictionary<WebClient, ImageParameters>();
        private ConcurrentDictionary<int, ITextViewLine> _editedLines = new ConcurrentDictionary<int, ITextViewLine>();

        private System.Timers.Timer _timer = new System.Timers.Timer(200);

        private class ImageParameters
        {
            public string Uri { get; set; }
            public string LocalPath { get; set; }
            public CommentImage Image { get; set; }
            public ITextViewLine Line { get; set; }
            public int LineNumber { get; set; }
            public SnapshotSpan Span { get; set; }
            public double Scale { get; set; }
            public string Filepath { get; set; }
        }

        /* void AddCommandFilter(IWpfTextView textView, KeyBindingCommandFilter commandFilter)
        {
            if (commandFilter.m_added == false)
            {
                //get the view adapter from the editor factory
                IOleCommandTarget next;
                IVsTextView view = editorFactory.GetViewAdapter(textView);

                int hr = view.AddCommandFilter(commandFilter, out next);

                if (hr == VSConstants.S_OK)
                {
                    commandFilter.m_added = true;
                    //you'll need the next target for Exec and QueryStatus
                    if (next != null)
                        commandFilter.m_nextTarget = next;
                }
            }
        } */

        public CommentsManager(IWpfTextView view, ITextDocumentFactoryService textDocumentFactory, SVsServiceProvider serviceProvider)
        {
            // AddCommandFilter(view, new KeyBindingCommandFilter(view));            // #eiichi

            // #hang_no 1 コメントアウトしたらハングしなかった
            _textDocumentFactory = textDocumentFactory;
            _view = view;
            _layer = view.GetAdornmentLayer("CommentImageAdornmentLayer");

            // #hang_no 2
            MyBookmarkManager.SetView(this, serviceProvider);            // #eiichi

            Images = new ConcurrentDictionary<int, CommentImage>();                 // #Image Images = new ConcurrentDictionary
            RichTextBoxs = new ConcurrentDictionary<int, CommentRichTextBox>();     // #eiichi

            _view.LayoutChanged += OnLayoutChanged;

            _contentTypeName = view.TextBuffer.ContentType.TypeName;
            _view.TextBuffer.ContentTypeChanged += OnContentTypeChanged;

            _errorTags = new List<ITagSpan<ErrorTag>>();
            _variableExpander = new VariableExpander(_view, serviceProvider);

            _timer.Elapsed += _timer_Elapsed;
        }

        public void SetBookmark(BookmarkPrims bookmarkPrims)
        {
            RichTextBoxs.Clear();

            System.Windows.Forms.RichTextBox rtb = new System.Windows.Forms.RichTextBox();
            rtb.AppendText(@"AAAAA\r\nBBBBB\r\nCCCCC");
            int fonth =     rtb.GetPositionFromCharIndex(rtb.GetFirstCharIndexFromLine(1)).Y -
                            rtb.GetPositionFromCharIndex(rtb.GetFirstCharIndexFromLine(0)).Y;

            foreach (var it in bookmarkPrims)
            {
                CommentRichTextBox TextBox = new CommentRichTextBox(it.Value);
                TextBox.Width = 1024;
                TextBox.Height = fonth * Util.getLineNum(it.Value.m_comment);
                RichTextBoxs.TryAdd(it.Key, TextBox);
            }
        }

        private void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            _contentTypeName = e.AfterContentType.TypeName;
        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)          // #eiichi OnLayoutChanged
        {
            // #hang_no 3
            try
            {
                if (!Enabled)
                    return;

                _errorTags.Clear();
                TagsChanged?.Invoke(
                   this,
                   new SnapshotSpanEventArgs(
                      new SnapshotSpan(
                         _view.TextSnapshot,
                         new Span(0, _view.TextSnapshot.Length))));

                foreach (ITextViewLine line in e.NewOrReformattedLines)
                {
                    int lineNumber = line.Snapshot.GetLineFromPosition(line.Start.Position).LineNumber;

                    _editedLines[lineNumber] = line;            // #Image _editedLines[lineNumber] = line
                }

                ResetTimer();

                // Sometimes, on loading a file in an editor view, the line transform gets triggered before the image adornments
                // have been added, so the lines don't resize to the image height. So here's a workaround:
                // Changing the zoom level triggers the required update.
                // Need to do it twice - once to trigger the event, and again to change it back to the user's expected level.
                if (!_initialised1)
                {
                    _view.ZoomLevel++;
                    _initialised1 = true;
                }
                if (!_initialised2)
                {
                    _view.ZoomLevel--;
                    _initialised2 = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Notify(ex, true);
            }
        }

        private void ResetTimer()
        {
            _timer.Stop();
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();

            // #hang_no 2
            Application.Current.Dispatcher.Invoke(() =>
            {
                string filepath = null;
                ITextDocument textDocument;
                if (_textDocumentFactory != null &&
                _textDocumentFactory.TryGetTextDocument(_view.TextBuffer, out textDocument))
                {
                    filepath = textDocument.FilePath;
                }

                foreach (var kvp in _editedLines)
                {
                    try
                    {
                        CreateVisuals(kvp.Value, kvp.Key, filepath);
                    }
                    catch (InvalidOperationException ex)
                    {
                        ExceptionHandler.Notify(ex, true);
                    }
                }

                _editedLines.Clear();
            });
        }

        private void CreateVisuals(ITextViewLine line, int lineNumber, string filepath)     // #eiichi CreateVisuals
        {
            // #hang_no 4
            try
            {
                // #eiichi start
                if(RichTextBoxs.ContainsKey(lineNumber))
                {   // BookMarkがある
                    var start = line.Extent.Start.Position + 0;
                    var end = line.Start + (line.Extent.Length - 1);
                    var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(start, end));

                    CommentRichTextBox TextBox = RichTextBoxs[lineNumber];
                    // TextBox.Document.Blocks.FirstBlock.LineHeight = 1;
                    // TextBox.AppendText("test test test\nabc abc acb");
                    AddComment(TextBox, line, lineNumber, span);
                }
                // #eiichi end

                var lineText = line.Extent.GetText();
                var lines = lineText.Split(
                   new string[] { Environment.NewLine },
                   StringSplitOptions.RemoveEmptyEntries);
                // multiline mean a block of code is collapsed
                // do not display pics from the collapsed text
                if (lines.Length > 1)
                    return;
                var matchIndex = CommentImageParser.Match(_contentTypeName, lineText, out string matchedText);
                if (matchIndex >= 0)
                {
                    // Get coordinates of text
                    var start = line.Extent.Start.Position + matchIndex;
                    var end = line.Start + (line.Extent.Length - 1);
                    var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(start, end));

                    CommentImageParser.TryParse(
                       matchedText,
                       out string imageUrl, out double scale, out Exception xmlParseException);

                    if (xmlParseException != null)
                    {
                        CommentImage commentImage;
                        if (Images.TryRemove(lineNumber, out commentImage))
                        {
                            _layer.RemoveAdornment(commentImage);
                            commentImage.Dispose();
                        }

                        _errorTags.Add(
                           new TagSpan<ErrorTag>(
                              span,
                              new ErrorTag("XML parse error", GetErrorMessage(xmlParseException))));

                        return;
                    }

                    var reload = false;
                    CommentImage image = Images.AddOrUpdate(lineNumber, ln =>
                    {
                        reload = true;
                        return new CommentImage(_variableExpander);
                    }, (ln, img) =>
                    {
                        if (img.OriginalUrl == imageUrl && img.Scale != scale)
                        {
                            // URL same but scale changed
                            img.Scale = scale;
                            reload = true;
                        }
                        else if (img.OriginalUrl != imageUrl)
                        {
                            // URL different, must load from new source
                            reload = true;
                        }
                        return img;
                    });

                    var originalUrl = imageUrl;
                    if (reload)
                    {
                        if (_processingUris.Contains(imageUrl)) return;

                        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ImageCache.Instance.TryGetValue(imageUrl, out string localPath))
                            {
                                imageUrl = localPath;
                            }
                            else
                            {
                                _processingUris.Add(imageUrl);
                                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(imageUrl));
                                WebClient client = new WebClient();
                                client.DownloadDataCompleted += Client_DownloadDataCompleted;

                                _toaddImages.TryAdd(
                                   client,
                                   new ImageParameters()
                                   {
                                       Uri = imageUrl,
                                       LocalPath = tempPath,
                                       Image = image,
                                       Line = line,
                                       LineNumber = lineNumber,
                                       Span = span,
                                       Scale = scale,
                                       Filepath = filepath
                                   });

                                client.DownloadDataAsync(new Uri(imageUrl));

                                return;
                            }
                        }
                    }

                    if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImageCache.Instance.TryGetValue(imageUrl, out string localPath))
                        {
                            imageUrl = localPath;
                        }
                    }
                    ProcessImage(image, imageUrl, originalUrl, line, lineNumber, span, scale, filepath);        // #Image ProcessImage
                }
                else
                {
                    Images.TryRemove(lineNumber, out var commentImage);
                    if (commentImage != null)           // #hang_this これ入れないとハングする
                    {
                        commentImage.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Notify(ex, true);
            }
        }

        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            try
            {
                var client = sender as WebClient;

                client.DownloadDataCompleted -= Client_DownloadDataCompleted;

                if (_toaddImages.TryGetValue(client, out ImageParameters item))
                {
                    byte[] data = e.Result;
                    File.WriteAllBytes(item.LocalPath, data);
                    ImageCache.Instance.Add(item.Uri, item.LocalPath);
                    _processingUris.Remove(item.Uri);

                    ProcessImage(item.Image,
                       item.LocalPath,
                       item.Uri,
                       item.Line,
                       item.LineNumber,
                       item.Span,
                       item.Scale,
                       item.Filepath);

                    _toaddImages.TryRemove(client, out var value);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Notify(ex, true);
            }
        }

        private void ProcessImage(CommentImage image, string imageUrl, string originalUrl, ITextViewLine line, int lineNumber, SnapshotSpan span, double scale, string filepath)
        {
            try
            {
                var result = image.TrySet(
                   imageUrl,
                   originalUrl,
                   scale,
                   filepath,
                   out Exception imageLoadingException);

                // Position image and add as adornment
                if (imageLoadingException == null)
                {
                    AddComment(image, line, lineNumber, span);
                }
                else
                {
                    Images.TryRemove(lineNumber, out var commentImage);
                    commentImage.Dispose();

                    _errorTags.Add(
                       new TagSpan<ErrorTag>(
                          span,
                          new ErrorTag("Trouble loading image", GetErrorMessage(imageLoadingException))));
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Notify(ex, true);
            }
        }

        private void AddComment(UIElement element, ITextViewLine line, int lineNumber, SnapshotSpan span)
        {
            Geometry geometry = null;
            try
            {
                geometry = _view.TextViewLines.GetMarkerGeometry(span);
            }
            catch { }

            if (geometry == null)
            {
                // Exceptional case when image dimensions are massive (e.g. specifying very large scale factor)
                throw new InvalidOperationException("Couldn't get source code line geometry. Is the loaded image massive?");
            }

            try
            {
                 Canvas.SetLeft(element, geometry.Bounds.Left);
                // Canvas.SetRight(element, geometry.Bounds.Right);
                Canvas.SetTop(element, line.TextBottom);
                // Canvas.SetBottom(element, line.TextBottom + geometry.Bounds.Bottom);
            }
            catch { }

            // Add element to the editor view
            try
            {
                _layer.RemoveAdornment(element);
                _layer.AddAdornment(
                                   AdornmentPositioningBehavior.TextRelative,
                                   line.Extent,
                                   null,
                                   element,
                                   null);
            }
            catch (Exception ex)
            {
                // No expected exceptions, so tell user something is wrong.
                ExceptionHandler.Notify(ex, true);
            }
        }

        private static string GetErrorMessage(Exception exception)
        {
            Trace.WriteLine("Problem parsing comment text or loading image...\n" + exception);

            string message;
            if (exception is XmlException)
                message = "Problem with comment format: " + exception.Message;
            else if (exception is NotSupportedException)
                message = exception.Message + "\nThis problem could be caused by a corrupt, invalid or unsupported image file.";
            else
                message = exception.Message;
            return message;
        }

        private void UnsubscribeFromViewerEvents()
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.TextBuffer.ContentTypeChanged -= OnContentTypeChanged;
        }

        #region ITagger<ErrorTag> Members

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _errorTags;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    UnsubscribeFromViewerEvents();
                    _timer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CommentsAdornment() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        #endregion
    }
}
