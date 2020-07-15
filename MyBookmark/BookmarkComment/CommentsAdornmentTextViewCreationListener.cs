using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;

namespace MyBookmark
{
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypes.Cpp),
     ContentType(ContentTypes.CSharp),
     ContentType(ContentTypes.VisualBasic),
     ContentType(ContentTypes.FSharp),
     ContentType(ContentTypes.JavaScript),
     ContentType(ContentTypes.TypeScript),
     ContentType(ContentTypes.Python),
     ContentType(ContentTypes.Java)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CommentsAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169

        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered
        /// after the selection layer in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("CommentImageAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        private AdornmentLayerDefinition editorAdornmentLayer;

#pragma warning restore 649, 169

        [Import]
        public ITextDocumentFactoryService TextDocumentFactory { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        [Import(typeof(IVsEditorAdaptersFactoryService))]
        internal IVsEditorAdaptersFactoryService editorFactory = null;

        #region IWpfTextViewCreationListener

        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// Instantiates a CommentsAdornment manager when the textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)                    // TEXT view を作成
        {
            AddCommandFilter(textView, new KeyBindingCommandFilter(textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentsAdornment(textView, TextDocumentFactory, ServiceProvider));
        }

        void AddCommandFilter(IWpfTextView textView, KeyBindingCommandFilter commandFilter)
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
        }
        #endregion
    }
}
