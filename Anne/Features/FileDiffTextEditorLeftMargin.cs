﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Anne.Foundation;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace Anne.Features
{
    public class FileDiffTextEditorLeftMargin : AbstractMargin
    {
        private readonly FileDiffTextEditor _editor;
        private FileDiffVm.DiffLine[] DiffLines => ((FileDiffVm) _editor?.DataContext)?.DiffLines;

        private const double IndexWidth = 40;
        private const double LineTypeWidth = 16;

        private const double MarginWidth = IndexWidth + IndexWidth + LineTypeWidth;
        private const double OldIndexOffset = 0;
        private const double NewIndexOffset = IndexWidth;
        private const double LineTypeIndexOffset = IndexWidth + IndexWidth;

        public FileDiffTextEditorLeftMargin(FileDiffTextEditor editor)
        {
            Debug.Assert(editor != null);

            _editor = editor;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(MarginWidth, 0);
        }

        protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= VisualLinesChanged;
                oldTextView.ScrollOffsetChanged -= ScrollOffsetChanged;
            }

            base.OnTextViewChanged(oldTextView, newTextView);

            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += VisualLinesChanged;
                newTextView.ScrollOffsetChanged += ScrollOffsetChanged;
            }
        }

        private void VisualLinesChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private void ScrollOffsetChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            FileDiffTextEditorHelper.DrawBackground(TextView, dc, MarginWidth, DiffLines, DrawForeground);
        }

        private static void DrawForeground(TextView textView, DrawingContext dc, Rect rect,
            FileDiffVm.DiffLine diffLine, int index)
        {
            dc.DrawLine(
                Constants.FramePen,
                new Point(OldIndexOffset, rect.Top),
                new Point(OldIndexOffset, rect.Bottom));

            dc.DrawLine(
                Constants.FramePen,
                new Point(NewIndexOffset, rect.Top),
                new Point(NewIndexOffset, rect.Bottom));

            dc.DrawLine(
                Constants.FramePen,
                new Point(LineTypeIndexOffset, rect.Top),
                new Point(LineTypeIndexOffset, rect.Bottom));

            if (index == 0)
            {
                DrawIndex(dc, rect, diffLine);
                DrawFileTypeMark(dc, rect, diffLine);
            }
        }

        private static void DrawIndex(DrawingContext dc, Rect rect, FileDiffVm.DiffLine diffLine)
        {
            switch(diffLine.LineType)
            {
                case FileDiffVm.DiffLine.LineTypes.ChunckTag:
                    DrawIndexText(dc, rect, "･･･", OldIndexOffset, TextAlignment.Center);
                    DrawIndexText(dc, rect, "･･･", NewIndexOffset, TextAlignment.Center);
                    break;

                case FileDiffVm.DiffLine.LineTypes.Normal:
                    DrawIndexText(dc, rect, diffLine.OldIndex, OldIndexOffset, TextAlignment.Right);
                    DrawIndexText(dc, rect, diffLine.NewIndex, NewIndexOffset, TextAlignment.Right);
                    break;

                case FileDiffVm.DiffLine.LineTypes.Add:
                    DrawIndexText(dc, rect, diffLine.NewIndex, NewIndexOffset, TextAlignment.Right);
                    break;

                case FileDiffVm.DiffLine.LineTypes.Delete:
                    DrawIndexText(dc, rect, diffLine.OldIndex, OldIndexOffset, TextAlignment.Right);
                    break;
            }
        }

        private static void DrawFileTypeMark(DrawingContext dc, Rect rect, FileDiffVm.DiffLine diffLine)
        {
            string mark;
            Brush brush;
            if (diffLine.LineType == FileDiffVm.DiffLine.LineTypes.Add)
            {
                mark = "+";
                brush = Brushes.Green;
            }
            else if (diffLine.LineType == FileDiffVm.DiffLine.LineTypes.Delete)
            {
                mark = "-";
                brush = Brushes.Red;
            }
            else
                return;

            dc.DrawText(
                new FormattedText(
                    mark,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    14,
                    brush),
                new Point(rect.Left + LineTypeIndexOffset + 4, rect.Top));
        }

        private static void DrawIndexText(DrawingContext dc, Rect rect, string text, double x, TextAlignment align)
        {
            var ft =
                new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas"),
                    14,
                    Brushes.DimGray);

            double offset;
            {
                if (align == TextAlignment.Center)
                    offset = (IndexWidth - ft.Width) * 0.5;

                else if (align == TextAlignment.Right)
                    offset = (IndexWidth - ft.Width) - 4;

                else
                    throw new NotImplementedException();
            }

            dc.DrawText(ft, new Point(rect.Left + x + offset, rect.Top));
        }

        private static void DrawIndexText(DrawingContext dc, Rect rect, int index, double x, TextAlignment align)
        {
            DrawIndexText(dc, rect, index.ToString(), x, align);
        }
    }
}