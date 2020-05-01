using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DoxygenComments
{
    class DoxygenCompletionSource : ICompletionSource
    {
        private DoxygenCompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList = new List<Completion>();
        private List<Completion> m_compList_at = new List<Completion>();
        private bool m_isDisposed;

        public DoxygenCompletionSource(DoxygenCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
            ImageSource image = null;

            try
            {
                image = this.m_sourceProvider.GlyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
            }
            catch
            {
            }

            m_compList.Add(new Completion("\\code", "\\code", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\sa", "\\sa", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\see", "\\see", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\include", "\\include", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\li", "\\li", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\param", "\\param", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\tparam", "\\tparam", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\brief", "\\brief", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\throw", "\\throw", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\return", "\\return", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\returns", "\\returns", string.Empty, image, string.Empty));
            m_compList.Add(new Completion("\\relates", "\\relates", string.Empty, image, string.Empty));

            m_compList_at.Add(new Completion("@code", "@code", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@sa", "@sa", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@see", "@see", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@include", "@include", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@li", "@li", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@param", "@param", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@tparam", "@tparam", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@brief", "@brief", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@throw", "@throw", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@return", "@return", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@returns", "@returns", string.Empty, image, string.Empty));
            m_compList_at.Add(new Completion("@relates", "@relates", string.Empty, image, string.Empty));

        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            try
            {
                // Only complete in active sessions
                if (m_isDisposed)
                {
                    return;
                }

                SnapshotPoint? snapshotPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot);
                if (!snapshotPoint.HasValue)
                {
                    return;
                }

                // Only complete in c/c++
                if (m_textBuffer.ContentType.TypeName != DoxygenCompletionHandler.CppTypeName)
                {
                    return;
                }

                // Only complete in comment lines
                string text = snapshotPoint.Value.GetContainingLine().GetText();
                if (!text.TrimStart().StartsWith("* "))
                {
                    return;
                }

                ITrackingSpan trackingSpan = FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session);
                var startText = trackingSpan.GetText(m_textBuffer.CurrentSnapshot);

                bool isAt = startText == "@";

                var newCompletionSet = new CompletionSet(
                    "DoxygenCompletionSet",
                    "DoxygenCompletionSet",
                    trackingSpan,
                    isAt ? m_compList_at : m_compList,
                    Enumerable.Empty<Completion>());
                completionSets.Add(newCompletionSet);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            try
            {
                SnapshotPoint currentPoint = session.TextView.Caret.Position.BufferPosition - 1;
                return currentPoint.Snapshot.CreateTrackingSpan(currentPoint, 1, SpanTrackingMode.EdgeInclusive);
            }
            catch
            {
            }

            return null;
        }
    }
}
