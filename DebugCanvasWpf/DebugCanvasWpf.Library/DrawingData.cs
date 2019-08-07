using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace DebugCanvasWpf.Library
{
    public class DrawingData
    {
        public bool IsEnabled { get; set; }

        public DrawingData() => IsEnabled = true;

        public DrawingData Clone()
        {
            return new DrawingData()
            {
                _screenWorldBounds = _screenWorldBounds,
                IsEnabled = IsEnabled,
                DrawBox_Calls = DrawBox_Calls.ToList(),
                DrawLine_Calls = DrawLine_Calls.ToList(),
                DrawText_Calls = DrawText_Calls.ToList(),
                DrawX_Calls = DrawX_Calls.ToList(),
                Log_Calls = Log_Calls.ToList(),
            };
        }

        public void ClearDrawings()
        {
            DrawLine_Calls.Clear();
            DrawBox_Calls.Clear();
            DrawX_Calls.Clear();
            DrawText_Calls.Clear();
        }

        public void ClearLog() => Log_Calls.Clear();

        private RectangleF _screenWorldBounds = new RectangleF(0, 0, 80, 45);
        public RectangleF ScreenWorldBounds
        {
            get => _screenWorldBounds;
            set
            {
                if (value.Height >= 0)
                {
                    _screenWorldBounds = value;
                }
                else
                {
                    _screenWorldBounds = new RectangleF(value.X, value.Y + value.Height, value.Width, -value.Height);
                }
            }
        }

        public void ZoomToContents()
        {
            var allPoints =
                DrawBox_Calls.Select(x => x.Center - x.Size)
                .Concat(DrawBox_Calls.Select(x => x.Center + x.Size))
                .Concat(DrawLine_Calls.Select(x => x.A))
                .Concat(DrawLine_Calls.Select(x => x.B))
                .Concat(DrawX_Calls.Select(x => x.Center - x.Size))
                .Concat(DrawX_Calls.Select(x => x.Center + x.Size))
                .ToList();

            var allX = allPoints.Select(x => x.X).ToList();
            var allY = allPoints.Select(x => x.Y).ToList();
            var min = new Vector2(allX.Min(), allY.Min());
            var max = new Vector2(allX.Max(), allY.Max());

            ScreenWorldBounds = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
        }

        public struct DrawLineInfo
        {
            public Vector2 A { get; set; }
            public Vector2 B { get; set; }
            public Color Color { get; set; }
        }

        public List<DrawLineInfo> DrawLine_Calls = new List<DrawLineInfo>();

        public void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            if (!IsEnabled) { return; }
            DrawLine_Calls.Add(new DrawLineInfo() { A = a, B = b, Color = color });
        }

        public struct DrawXInfo
        {
            public Vector2 Center { get; set; }
            public Color Color { get; set; }
            public Vector2 Size { get; set; }
        }

        public List<DrawXInfo> DrawX_Calls = new List<DrawXInfo>();

        public void DrawX(Vector2 center, Color color, float size = 1)
        {
            if (!IsEnabled) { return; }
            DrawX_Calls.Add(new DrawXInfo() { Center = center, Color = color, Size = new Vector2(size, size) });
        }

        public void DrawX(Vector2 center, Color color, Vector2 size)
        {
            if (!IsEnabled) { return; }
            DrawX_Calls.Add(new DrawXInfo() { Center = center, Color = color, Size = size });
        }

        public struct DrawBoxInfo
        {
            public Vector2 Center { get; set; }
            public Color Color { get; set; }
            public Vector2 Size { get; set; }
            public bool ShouldFill { get; set; }
        }

        public List<DrawBoxInfo> DrawBox_Calls = new List<DrawBoxInfo>();

        public void DrawBox(Vector2 center, Color color, float size = 1, bool shouldFill = true)
        {
            if (!IsEnabled) { return; }
            DrawBox_Calls.Add(new DrawBoxInfo() { Center = center, Color = color, Size = new Vector2(size, size), ShouldFill = shouldFill });
        }

        public void DrawBox(Vector2 center, Color color, Vector2 size, bool shouldFill = true)
        {
            if (!IsEnabled) { return; }
            DrawBox_Calls.Add(new DrawBoxInfo() { Center = center, Color = color, Size = size, ShouldFill = shouldFill });
        }


        public struct TextInfo
        {
            public string Text { get; set; }
            public Vector2 Center { get; set; }
            public Color Color { get; set; }
            public Color? Shadow { get; set; }
            public Vector2 Size { get; set; }
            public float FontHeight { get; set; }

            public Vector2 TopLeft => Center + new Vector2(-Size.X * 0.5f, Size.Y * 0.5f);
        }

        public List<TextInfo> DrawText_Calls = new List<TextInfo>();
        public void DrawText(string text, Vector2 center, Color color, Vector2 size, float fontHeight, Color? shadow = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            DrawText_Calls.Add(new TextInfo()
            {
                Text = text,
                Center = center,
                Color = color,
                Shadow = shadow,
                Size = size,
                FontHeight = fontHeight,
            });
        }

        public struct LogInfo
        {
            public DateTime Time { get; set; }
            public string Message { get; set; }
            public object[] Data { get; set; }
        }

        public List<LogInfo> Log_Calls = new List<LogInfo>();
        public virtual void Log(string message, params object[] data)
        {
            if (!IsEnabled) { return; }
            Log_Calls.Add(new LogInfo() { Time = DateTime.Now, Message = message, Data = data });
        }

    }
}
