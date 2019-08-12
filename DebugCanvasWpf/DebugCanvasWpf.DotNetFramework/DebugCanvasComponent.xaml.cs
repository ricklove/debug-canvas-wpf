using DebugCanvasWpf.Library;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DebugCanvasWpf.DotNetFramework
{
    public partial class DebugCanvasComponent : UserControl
    {
        public DrawingData DrawingData { get; private set; }

        public class ClickEventArgs
        {
            public Vector2 WorldPosition { get; set; }
        }
        public event EventHandler<ClickEventArgs> Click;

        private readonly GdiRenderer _renderer;
        private DateTime _timeDown;
        private Vector2 _oldMouseWorldPosition;

        private System.Threading.Timer _renderDebounceTimer;

        public DebugCanvasComponent()
        {
            InitializeComponent();

            DrawingData = new DrawingData();
            _renderer = new GdiRenderer(s =>
            {
                imgMain.Source = s;
            });
        }

        public void Clear()
        {
            DrawingData.ClearDrawings();
            DrawingData.ClearLog();
            Render();
        }

        public void Render()
        {
            if (_renderDebounceTimer == null)
            {
                _renderDebounceTimer = new System.Threading.Timer((_) =>
                {
                    if (_renderer.IsBusy)
                    {
                        Render_Queue();
                        return;
                    }

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        Render_Inner();
                    }), null);
                });
            }

            if (_renderer.IsBusy)
            {
                Render_Queue();
                return;
            }

            Render_Inner();
        }

        private void Render_Queue() => _renderDebounceTimer.Change(50, System.Threading.Timeout.Infinite);

        private void Render_Inner() => _renderer.UpdateImageAsync(viewMain.ActualWidth, viewMain.ActualHeight, DrawingData);

        private void imgMain_SizeChanged(object sender, SizeChangedEventArgs e) => Render();

        public event EventHandler WorldBoundsChanged;
        public System.Drawing.RectangleF WorldBounds
        {
            get
            {
                return DrawingData.ScreenWorldBounds;
            }
            set
            {
                DrawingData.ScreenWorldBounds = value;
                WorldBoundsChanged?.Invoke(this, new EventArgs());
            }
        }

        private void imgMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _timeDown = DateTime.Now;
            _oldMouseWorldPosition = GetMouseWorldPosition(sender, e);
        }

        private void imgMain_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DateTime.Now > _timeDown + TimeSpan.FromMilliseconds(250)) { return; }

            var m = _oldMouseWorldPosition = GetMouseWorldPosition(sender, e);

            // TODO: Create Click Item Event
            //var hitBounds = DrawingData.DrawBox_Calls.Where(x=>x.
            Click?.Invoke(this, new ClickEventArgs() { WorldPosition = new Vector2(m.X, m.Y) });


            //var hitTables = _document.TableEntries.Where(x => x.Bounds.Contains(m.X, m.Y)).ToList();
            //var tablesCsv = _tableCsvs.Where(x => hitTables.Contains(x.Table)).ToList();

            //if (!tablesCsv.Any()) { return; }

            //lstTables.ItemsSource = tablesCsv;
            //lstTables.SelectedIndex = 0;

            //if (e.ChangedButton == MouseButton.Right)
            //{
            //    Clipboard.SetText(tablesCsv[0].Csv);
            //    popCopied.IsOpen = true;
            //    popCopied.StaysOpen = false;
            //}
        }

        private void imgMain_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed
                && e.MiddleButton != MouseButtonState.Pressed
                ) { return; }

            var m = GetMouseWorldPosition(sender, e);
            var mDelta = m - _oldMouseWorldPosition;

            var w = WorldBounds;
            w = w.Offset(-mDelta);
            WorldBounds = w;
            Render();

            _oldMouseWorldPosition = GetMouseWorldPosition(sender, e);
        }

        private void imgMain_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var d = e.Delta;

            var m = GetMouseWorldPosition(sender, e);
            var w = WorldBounds;
            w = w.Offset(-m);

            if (d > 0) { w = w.Scale(1 / 1.1f); e.Handled = true; }
            else { w = w.Scale(1.1f); e.Handled = true; }

            w = w.Offset(m);


            if (w.Height < 0 || w.Width < 0)
            {
                var breakdance = true;
            }


            WorldBounds = w;
            Render();
        }

        private Vector2 GetMouseWorldPosition(object sender, MouseEventArgs e)
        {
            var mouseWorldPos = _renderer.GetImagePointToWorldPosition(e.GetPosition(sender as IInputElement));
            var m = new Vector2(mouseWorldPos.X, mouseWorldPos.Y);
            return m;
        }
    }

    internal static class Rect_Ext
    {
        public static System.Drawing.RectangleF Offset(this System.Drawing.RectangleF rect, Vector2 offset) { rect.Offset(offset.X, offset.Y); return rect; }
        public static System.Drawing.RectangleF Scale(this System.Drawing.RectangleF rect, float scale)
        {
            var size = new Vector2(rect.Width * scale, rect.Height * scale);
            rect.Inflate(size.X - rect.Width, size.Y - rect.Height);
            return rect;
        }
    }
}
