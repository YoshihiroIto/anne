﻿using System;
using System.Windows;
using System.Windows.Media;
using Anne.Foundation;
using ICSharpCode.AvalonEdit.Rendering;

namespace Anne.Features
{
    public static class FileDiffTextEditorHelper
    {
        public class PerLineDrawArgs
        {
            public TextView TextView { get; set; }
            public VisualLine VisualLine { get; set; }
            public DrawingContext DrawingContext { get; set; }
            public Rect Rect { get; set; }
            public FileDiffVm.DiffLine DiffLine { get; set; }
            public int Index { get; set; }
        }

        public static void DrawBackground(
            TextView textView, DrawingContext dc, double x, double width, FileDiffVm.DiffLine[] diffLines,
            bool isLight,
            Action<PerLineDrawArgs> perLineDraw = null)
        {
            if (diffLines == null)
                return;

            foreach (var visualLine in textView.VisualLines)
            {
                var linenum = visualLine.FirstDocumentLine.LineNumber - 1;
                if (linenum >= diffLines.Length)
                    continue;

                var diffLine = diffLines[linenum];
                var brush = default(Brush);

                switch (diffLine.LineType)
                {
                    case FileDiffVm.DiffLine.LineTypes.ChunckTag:
                        brush = isLight
                            ? Constants.LightChunckTagBackground
                            : Constants.ChunckTagBackground;
                        break;

                    case FileDiffVm.DiffLine.LineTypes.Add:
                        brush = isLight
                            ? Constants.LightAddBackground
                            : Constants.AddBackground;
                        break;

                    case FileDiffVm.DiffLine.LineTypes.Delete:
                        brush = isLight
                            ? Constants.LightRemoveBackground
                            : Constants.RemoveBackground;
                        break;
                }

                var perLineDrawArgs = new PerLineDrawArgs
                {
                    TextView = textView,
                    VisualLine = visualLine,
                    DrawingContext = dc,
                    DiffLine = diffLine
                };

                var index = 0;

                foreach (
                    var segRect in BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visualLine, 0, 1000))
                {
                    var rect = new Rect(x, segRect.Top, width, segRect.Height + 1);

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (brush != default(Brush))
                        dc.DrawRectangle(brush, null, rect);

                    if (perLineDraw != null)
                    {
                        perLineDrawArgs.Rect = rect;
                        perLineDrawArgs.Index = index;

                        perLineDraw.Invoke(perLineDrawArgs);
                    }

                    ++index;
                }
            }
        }
    }
}