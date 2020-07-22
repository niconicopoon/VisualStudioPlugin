using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace MyBookmark
{
    internal class CommentLineTransformSource : ILineTransformSource
    {
        private CommentsManager _adornment;

        public CommentLineTransformSource(CommentsManager adornment)
        {
            _adornment = adornment;
        }

        LineTransform ILineTransformSource.GetLineTransform(      // #Image GetLineTransform
           ITextViewLine line,
           double yPosition,
           ViewRelativePosition placement)
        {
            // #hang_no 5
            var lineNumber = line.Snapshot.GetLineFromPosition(line.Start.Position).LineNumber;

            // Look up Image for current line and increase line height as necessary
            if (CommentsManager.Enabled)
            {
                var defaultHeight = line.DefaultLineTransform.BottomSpace;
                if ( _adornment.Images.ContainsKey(lineNumber) &&
                    _adornment.Images[lineNumber].Source != null)           // #Image LineTransform
                {
                    return new LineTransform(0, _adornment.Images[lineNumber].Height + defaultHeight, 1.0);
                }
                if (_adornment.RichTextBoxs.ContainsKey(lineNumber))        // #eiichi LineTransform
                {
                   return new LineTransform(0, _adornment.RichTextBoxs[lineNumber].Height + defaultHeight, 1.0);
                }
            }
            return new LineTransform(0, 0, 1.0);
        }
    }
}
