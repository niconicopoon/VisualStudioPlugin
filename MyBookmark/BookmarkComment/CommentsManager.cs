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
using System.Windows.Data;
using Markdig;          // PM> Install-Package Markdig -Version 0.17.1
using System.Windows.Markup;
using System.Linq;
using HtmlToXaml;
using System.Windows.Threading;

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

        public string m_FileName;

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
        public IWpfTextView GetView() { return m_view; }

        public ConcurrentDictionary<int, CommentImage> Images { get; set; }
        public ConcurrentDictionary<int, CommentRichTextBox> RichTextBoxs { get; set; }     // #eiichi

        private IAdornmentLayer m_layer;
        private IWpfTextView m_view;
        private Util m_Util;
        private ITextDocumentFactoryService m_textDocumentFactory;
        private string m_contentTypeName;
        private bool m_initialised1 = false;
        private bool m_initialised2 = false;
        private List<ITagSpan<ErrorTag>> m_errorTags;

        private List<string> m_processingUris = new List<string>();
        private ConcurrentDictionary<WebClient, ImageParameters> m_toaddImages = new ConcurrentDictionary<WebClient, ImageParameters>();
        private ConcurrentDictionary<int, ITextViewLine> m_editedLines = new ConcurrentDictionary<int, ITextViewLine>();

        private System.Timers.Timer m_timer = new System.Timers.Timer(200);

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
            m_textDocumentFactory = textDocumentFactory;
            m_view = view;
            m_layer = view.GetAdornmentLayer("CommentImageAdornmentLayer");

            // #hang_no 2
            m_view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);
            m_FileName = document.FilePath;

            Images = new ConcurrentDictionary<int, CommentImage>();                 // #Image Images = new ConcurrentDictionary
            RichTextBoxs = new ConcurrentDictionary<int, CommentRichTextBox>();     // #eiichi

            // MyBookmarkManager.SetView(this, serviceProvider);

            m_view.LayoutChanged += OnLayoutChanged;
            m_view.GotAggregateFocus += delegate { MyBookmarkManager.SetView(this, serviceProvider); };
            m_view.Closed += delegate { MyBookmarkManager.CloseView(this); };

            m_contentTypeName = view.TextBuffer.ContentType.TypeName;
            m_view.TextBuffer.ContentTypeChanged += OnContentTypeChanged;
            // m_view.TextBuffer.Delete

            m_errorTags = new List<ITagSpan<ErrorTag>>();
            m_Util = new Util(m_view, serviceProvider);

            m_timer.Elapsed += _timer_Elapsed;
        }

        public void SetLineHeight(CommentRichTextBox rtb)
        {
            rtb.Width = 1024;
            // Paragraph p = rtb.Document.Blocks.FirstBlock as Paragraph;
            foreach(var p in rtb.Document.Blocks)
            {
                p.LineHeight = 10;
                p.Margin = new Thickness(0);
            }
        }

    /* public static double GetBlockHeight(Block block)
    {
        Rect rectangleInFirstBlockLine = block.ElementStart.GetCharacterRect(LogicalDirection.Forward);
        Rect rectangleInLastBlockLine = block.ElementEnd.GetCharacterRect(LogicalDirection.Forward);

        double blockHeight = rectangleInLastBlockLine.Top - rectangleInFirstBlockLine.Top;

        return blockHeight;
    } */

    private double GethDocumentHeight(Viewbox viewbox, CommentRichTextBox RichTextBox)
        {
            viewbox.Child = RichTextBox;
            viewbox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            viewbox.Arrange(new Rect(viewbox.DesiredSize));
            var size = new Size() { Height = viewbox.ActualHeight, Width = viewbox.ActualWidth };
            return size.Height;
        }

        public void SetBookmark(BookmarkPrims bookmarkPrims)
        {
            var pipeline = new Markdig.MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            Viewbox viewbox = new Viewbox();

            foreach (var it in bookmarkPrims)
            {
                int lineNo = it.Key - 1;

                CommentRichTextBox TextBox = null;
                if (!RichTextBoxs.ContainsKey(lineNo))
                {
                    TextBox = new CommentRichTextBox(it.Value);
                    RichTextBoxs.TryAdd(lineNo, TextBox);
                }
                else
                if (RichTextBoxs[lineNo].m_comment != it.Value.m_comment)
                {
                    TextBox = RichTextBoxs[lineNo];
                }
                if (TextBox != null)
                {
                    var html = Markdig.Markdown.ToHtml(it.Value.m_comment, pipeline);
                    var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(html, true);
                    CommentRichTextBox rtb = new CommentRichTextBox();
                    rtb.Document = XamlReader.Parse(xaml) as FlowDocument;
                    SetLineHeight(rtb);
                    double Height = GethDocumentHeight(viewbox, rtb);

                    TextBox.Document = XamlReader.Parse(xaml) as FlowDocument;
                    TextBox.Document.Background = Brushes.LightGray;
                    SetLineHeight(TextBox);

                    TextBox.Height = Height;
                    TextBox.Visibility = Visibility.Visible;
                }

                RequestRedrawLine(lineNo);
            }
        }

        public void DelBookmark(int lineNo)
        {
            lineNo -= 1;
            if (RichTextBoxs.ContainsKey(lineNo))
            {
                CommentRichTextBox TextBox = null;
                RichTextBoxs.TryRemove(lineNo, out TextBox);
                RequestRedrawLine(lineNo);
            }
        }

        private static void HyperlinksSubscriptions(FlowDocument flowDocument)
        {
            if (flowDocument == null) return;
            GetVisualChildren(flowDocument).OfType<Hyperlink>().ToList()
                     .ForEach(i => i.RequestNavigate += HyperlinkNavigate);
        }
        private static IEnumerable<DependencyObject> GetVisualChildren(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var descendants in GetVisualChildren(child)) yield return descendants;
            }
        }
        private static void HyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            m_contentTypeName = e.AfterContentType.TypeName;
        }

        /* private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        } */

        private void RequestRedrawView()
        {
            TagsChanged?.Invoke(
               this,
               new SnapshotSpanEventArgs(
                  new SnapshotSpan(
                     m_view.TextSnapshot,
                     new Span(0, m_view.TextSnapshot.Length))));
        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)          // #eiichi OnLayoutChanged
        {
            // #hang_no 3
            try
            {
                if (!Enabled)
                    return;

                m_errorTags.Clear();
                RequestRedrawView();

                foreach (ITextViewLine line in e.NewOrReformattedLines)
                {
                    int lineNumber = line.Snapshot.GetLineFromPosition(line.Start.Position).LineNumber;
                    m_editedLines[lineNumber] = line;            // #Image _editedLines[lineNumber] = line
                }

                ResetTimer();

                // Sometimes, on loading a file in an editor view, the line transform gets triggered before the image adornments
                // have been added, so the lines don't resize to the image height. So here's a workaround:
                // Changing the zoom level triggers the required update.
                // Need to do it twice - once to trigger the event, and again to change it back to the user's expected level.
                if (!m_initialised1)
                {
                    m_view.ZoomLevel++;
                    m_initialised1 = true;
                }
                if (!m_initialised2)
                {
                    m_view.ZoomLevel--;
                    m_initialised2 = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Notify(ex, true);
            }
        }

        private void RequestRedrawLine(int lineNo)
        {
            try
            {
                IWpfTextViewLineCollection textViewLines = this.m_view.TextViewLines;
                ITextViewLine line = textViewLines[lineNo];
                m_view.DisplayTextLineContainingBufferPosition(line.Start, line.Top, ViewRelativePosition.Top);
            }
            catch (Exception ex)
            {
                ExceptionHandler.Notify(ex, true);
            }
        }


        private void ResetTimer()
        {
            m_timer.Stop();
            m_timer.Start();
        }

        private string GetDocumentFilePath()
        {
            string filepath = null;
            ITextDocument textDocument;
            if (m_textDocumentFactory != null &&
            m_textDocumentFactory.TryGetTextDocument(m_view.TextBuffer, out textDocument))
            {
                filepath = textDocument.FilePath;
            }
            return filepath;
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_timer.Stop();

            // #hang_no 2
            Application.Current.Dispatcher.Invoke(() =>
            {
                string filepath = GetDocumentFilePath();

                foreach (var kvp in m_editedLines)
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

                m_editedLines.Clear();
            });
        }

        private void CreateVisuals(ITextViewLine line, int lineNumber, string filepath)     // #eiichi CreateVisuals
        {
            // #hang_no 4
            try
            {
                // #eiichi start
               if (RichTextBoxs.ContainsKey(lineNumber))
                {   // BookMarkがある
                    var start = line.Extent.Start.Position + 0;
                    int len = line.Extent.Length - 1;
                    if (len <= 0) len = 1; ;
                    var end = line.Start + len;
                    var span = new SnapshotSpan(m_view.TextSnapshot, Span.FromBounds(start, end));

                    CommentRichTextBox TextBox = RichTextBoxs[lineNumber];
                    AddComment(TextBox, line, lineNumber, span);
                }
                else
                {
                    var lineText = line.Extent.GetText();
                    var lines = lineText.Split(
                       new string[] { Environment.NewLine },
                       StringSplitOptions.RemoveEmptyEntries);
                    // multiline mean a block of code is collapsed
                    // do not display pics from the collapsed text
                    if (lines.Length > 1)
                        return;
                    var matchIndex = CommentImageParser.Match(m_contentTypeName, lineText, out string matchedText);
                    if (matchIndex >= 0)
                    {
                        // Get coordinates of text
                        var start = line.Extent.Start.Position + matchIndex;
                        var end = line.Start + (line.Extent.Length - 1);
                        var span = new SnapshotSpan(m_view.TextSnapshot, Span.FromBounds(start, end));

                        CommentImageParser.TryParse(
                           matchedText,
                           out string imageUrl, out double scale, out Exception xmlParseException);

                        if (xmlParseException != null)
                        {
                            CommentImage commentImage;
                            if (Images.TryRemove(lineNumber, out commentImage))
                            {
                                m_layer.RemoveAdornment(commentImage);
                                commentImage.Dispose();
                            }

                            m_errorTags.Add(
                               new TagSpan<ErrorTag>(
                                  span,
                                  new ErrorTag("XML parse error", GetErrorMessage(xmlParseException))));

                            return;
                        }

                        var reload = false;
                        CommentImage image = Images.AddOrUpdate(lineNumber, ln =>
                        {
                            reload = true;
                            return new CommentImage(m_Util);
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
                            if (m_processingUris.Contains(imageUrl)) return;

                            if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                if (ImageCache.Instance.TryGetValue(imageUrl, out string localPath))
                                {
                                    imageUrl = localPath;
                                }
                                else
                                {
                                    m_processingUris.Add(imageUrl);
                                    var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(imageUrl));
                                    WebClient client = new WebClient();
                                    client.DownloadDataCompleted += Client_DownloadDataCompleted;

                                    m_toaddImages.TryAdd(
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

                if (m_toaddImages.TryGetValue(client, out ImageParameters item))
                {
                    byte[] data = e.Result;
                    File.WriteAllBytes(item.LocalPath, data);
                    ImageCache.Instance.Add(item.Uri, item.LocalPath);
                    m_processingUris.Remove(item.Uri);

                    ProcessImage(item.Image,
                       item.LocalPath,
                       item.Uri,
                       item.Line,
                       item.LineNumber,
                       item.Span,
                       item.Scale,
                       item.Filepath);

                    m_toaddImages.TryRemove(client, out var value);
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

                    m_errorTags.Add(
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
                geometry = m_view.TextViewLines.GetMarkerGeometry(span);
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
                m_layer.RemoveAdornment(element);
                m_layer.AddAdornment(
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
            m_view.LayoutChanged -= OnLayoutChanged;
            m_view.TextBuffer.ContentTypeChanged -= OnContentTypeChanged;
        }

        #region ITagger<ErrorTag> Members

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return m_errorTags;
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
                    m_timer.Dispose();
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
