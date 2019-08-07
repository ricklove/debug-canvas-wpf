using DebugCanvasWpf.Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Media.Imaging;
using Pen = System.Drawing.Pen;

namespace DebugCanvasWpf.DotNetFramework
{
    public class GdiRenderer : GdiRendererBase<DrawingData>
    {
        public GdiRenderer(Action<BitmapSource> setSource)
            : base(setSource, x => x)
        {
        }

        private DrawingData _data = new DrawingData();

        public override void RenderData(DrawingData data)
        {
            _data = data;

            try
            {
                RenderData_Inner();
            }
            catch (Exception ex)
            {
#pragma warning disable IDE0059 // Value assigned to symbol is never used
                var breakdance = ex;
#pragma warning restore IDE0059 // Value assigned to symbol is never used
            }
        }

        private void RenderData_Inner()
        {
            DrawingData data = _data;

            // Draw Origin
            if (true)
            {
                DrawLine(GetPen(Color.White, 1), GetPoint(new Vector2(-100, 0)), GetPoint(new Vector2(100, 0)));
                DrawLine(GetPen(Color.White, 1), GetPoint(new Vector2(0, -100)), GetPoint(new Vector2(0, 100)));
            }

            foreach (var call in data.DrawBox_Calls.ToList())
            {
                if (!IsInBounds(call.Center, call.Size)) { continue; }
                var tl = GetPoint(call.Center + new Vector2(-call.Size.X * 0.5f, -call.Size.Y * 0.5f));
                var br = GetPoint(call.Center + new Vector2(call.Size.X * 0.5f, call.Size.Y * 0.5f));
                var pad = 0.5f;
                var x = Math.Min(tl.X, br.X) - pad;
                var y = Math.Min(tl.Y, br.Y) - pad;
                var w = Math.Abs(br.X - tl.X) + pad * 2;
                var h = Math.Abs(br.Y - tl.Y) + pad * 2;

                if (call.ShouldFill)
                {
                    FillRectangle(GetBrush(call.Color), new RectangleF(x, y, w, h));
                }
                else
                {
                    DrawRectangle(GetPen(call.Color, 1), new RectangleF(x, y, w, h));
                }

                // DrawRectangle(GetPen(Color.black, 1), new Rectangle(x, y, w, h));
                //DrawLine(GetPen((call.Color), 1), GetPoint(call.Center + new Vector2(-call.Size * 0.5f, -call.Size * 0.5f)), GetPoint(call.Center + new Vector2(call.Size * 0.5f, -call.Size * 0.5f)));
                //DrawLine(GetPen((call.Color), 1), GetPoint(call.Center + new Vector2(call.Size * 0.5f, -call.Size * 0.5f)), GetPoint(call.Center + new Vector2(call.Size * 0.5f, call.Size * 0.5f)));
                //DrawLine(GetPen((call.Color), 1), GetPoint(call.Center + new Vector2(call.Size * 0.5f, call.Size * 0.5f)), GetPoint(call.Center + new Vector2(-call.Size * 0.5f, call.Size * 0.5f)));
                //DrawLine(GetPen((call.Color), 1), GetPoint(call.Center + new Vector2(-call.Size * 0.5f, call.Size * 0.5f)), GetPoint(call.Center + new Vector2(-call.Size * 0.5f, -call.Size * 0.5f)));
            }

            foreach (var call in data.DrawX_Calls.ToList())
            {
                if (!IsInBounds(call.Center, call.Size)) { continue; }
                DrawLine(GetPen((call.Color), 1), GetPoint(call.Center - new Vector2(call.Size.X * 0.5f, call.Size.Y * 0.5f)), GetPoint(call.Center + new Vector2(call.Size.X * 0.5f, call.Size.Y * 0.5f)));
                DrawLine(GetPen((call.Color), 1), GetPoint(call.Center - new Vector2(-call.Size.X * 0.5f, call.Size.Y * 0.5f)), GetPoint(call.Center + new Vector2(-call.Size.X * 0.5f, call.Size.Y * 0.5f)));
            }

            foreach (var call in data.DrawLine_Calls.ToList())
            {
                var center = (call.A * 0.5f) + (call.B * 0.5f);
                var size = new Vector2(Math.Abs((call.A - call.B).X), Math.Abs((call.A - call.B).Y));

                if (!IsInBounds(center, size)) { continue; }
                DrawLine(GetPen((call.Color), 1), GetPoint(call.A), GetPoint(call.B));
            }

            foreach (var call in data.DrawText_Calls.ToList())
            {
                if (!IsInBounds(call.Center, call.Size)) { continue; }

                var height = GetSize(new Vector2(0, call.FontHeight)).Height;

                //height *= 0.8f;

                var topLeft = GetPoint(call.TopLeft);
                var size = new SizeF(GetSize(call.Size));

                if (height <= 0 || size.Height <= 0 || size.Width <= 0)
                {
                    continue;
                }

                if (call.Shadow != null)
                {
                    DrawText(call.Text, GetFont(height), GetBrush(call.Shadow.Value), new PointF(topLeft.X + 1, topLeft.Y + 1), size);
                }
                DrawText(call.Text, GetFont(height), GetBrush(call.Color), topLeft, size);
            }
        }

        private SizeF GetSize(Vector2 vector2)
        {
            var origin = GetPoint(new Vector2(0, 0));
            var size = GetPoint(vector2);
            return new SizeF(Math.Abs(origin.X - size.X), Math.Abs(origin.Y - size.Y));
        }

        private void DrawText(string text, Font font, Brush brush, PointF topLeft, SizeF size)
        {
            var rect = new RectangleF(topLeft, size);
            GdiGraphics.DrawString(text, font, brush, rect);
        }

        private void FillRectangle(System.Drawing.Brush brush, System.Drawing.RectangleF rect) => GdiGraphics.FillRectangle(brush, rect);

        private void DrawRectangle(System.Drawing.Pen pen, System.Drawing.RectangleF rect) => GdiGraphics.DrawRectangle(pen, System.Drawing.Rectangle.Round(rect));

        private void DrawLine(Pen pen, System.Drawing.Point a, System.Drawing.Point b) => GdiGraphics.DrawLine(pen, a, b);

        private bool IsInBounds(Vector2 center, Vector2 size)
        {
            var a = GetPoint(center - size * 0.5f);
            var b = GetPoint(center + size * 0.5f);

            var minX = Math.Min(a.X, b.X);
            var minY = Math.Min(a.Y, b.Y);
            var maxX = Math.Max(a.X, b.X);
            var maxY = Math.Max(a.Y, b.Y);

            var isOverlapX = maxX > 0 && minX < ImageWidth;
            var isOverlapY = maxY > 0 && minY < ImageHeight;

            return isOverlapX && isOverlapY;
        }

        private Point GetPoint(Vector2 position)
        {
            var scaleX = ImageWidth / _data.ScreenWorldBounds.Width;
            var scaleY = ImageHeight / _data.ScreenWorldBounds.Height;
            var scale = Math.Min(scaleX, scaleY);
            var offsetX = _data.ScreenWorldBounds.X;
            var offsetY = _data.ScreenWorldBounds.Y;

            return new System.Drawing.Point(
                (int)((position.X - offsetX) * scale),
                (int)((position.Y - offsetY) * -scale + ImageHeight));
        }

        public Vector2 GetImagePointToWorldPosition(System.Windows.Point point)
        {
            var scaleX = ImageWidth / _data.ScreenWorldBounds.Width;
            var scaleY = ImageHeight / _data.ScreenWorldBounds.Height;
            var scale = Math.Min(scaleX, scaleY);
            var offsetX = _data.ScreenWorldBounds.X;
            var offsetY = _data.ScreenWorldBounds.Y;

            return new Vector2(
                ((float)point.X / scale) + offsetX,
                ((float)(point.Y - ImageHeight) / -scale) + offsetY);
        }

        private readonly Dictionary<double, Font> fonts = new Dictionary<double, Font>();
        private Font GetFont(double fontSize)
        {
            if (!fonts.ContainsKey(fontSize))
            {
                fonts.Add(fontSize, new Font("Simplex", (float)fontSize));
            }
            return fonts[fontSize];
        }

        private readonly Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();
        private System.Drawing.Brush GetBrush(Color color)
        {
            if (!brushes.ContainsKey(color))
            {
                brushes.Add(color, new SolidBrush(color));

                // Pre-Multiplied Alpha
                // brushes.Add(color, new SolidBrush(System.Drawing.Color.FromArgb((byte)(color.a * 255), (byte)(color.r * color.a * 255), (byte)(color.g * color.a * 255), (byte)(color.b * color.a * 255))));
            }
            return brushes[color];
        }

        private readonly Dictionary<Color, System.Drawing.Pen> pens = new Dictionary<Color, System.Drawing.Pen>();
        private Pen GetPen(Color color, float thickness)
        {
            if (!pens.ContainsKey(color))
            {
                var pen = new Pen(color, thickness);

                // Pre-Multiplied Alpha
                // var pen = new Pen(System.Drawing.Color.FromArgb((byte)(color.a * 255), (byte)(color.r * color.a * 255), (byte)(color.g * color.a * 255), (byte)(color.b * color.a * 255)), thickness);
                pens.Add(color, pen);
            }
            return pens[color];
        }


    }

}