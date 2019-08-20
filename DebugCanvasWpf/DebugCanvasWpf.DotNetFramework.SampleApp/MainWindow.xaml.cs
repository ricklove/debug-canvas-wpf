using System;
using System.Numerics;
using System.Windows;
using System.Windows.Threading;

namespace DebugCanvasWpf.DotNetFramework.SampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            var dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromMilliseconds(10);
            dt.Tick += Dt_Tick;
            dt.Start();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            var d = compDebugCanvas.DrawingData;
            d.DrawX(new Vector2(d.DrawX_Calls.Count % 100, (int)(d.DrawX_Calls.Count / 100)), System.Drawing.Color.Blue, new Vector2(1, 1));
            compDebugCanvas.Render();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var d = compDebugCanvas.DrawingData;
            d.DrawBox(new Vector2(10, 10), System.Drawing.Color.Red, new Vector2(100, 50), false);
            d.DrawEllipse(new Vector2(10, 10), System.Drawing.Color.Cyan, new Vector2(200, 100), false);
            d.DrawLine(new Vector2(10, 10), new Vector2(1000, 1000), System.Drawing.Color.Blue);
            d.DrawText("Hello World!", new Vector2(20, 20), System.Drawing.Color.Yellow, new Vector2(200, 20), 16, shadow: System.Drawing.Color.Blue);

            compDebugCanvas.Render();
        }
    }
}
