using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DebugCanvasWpf.DotNetFramework
{
    // Based on http://www.newyyz.com/blog/2012/02/14/fast-line-rendering-in-wpf/
    public abstract class GdiRendererBase<T>
    {
        private readonly BackgroundWorker RenderWorker;
        private bool IsBitmapInitialized { get; set; }

        protected Graphics GdiGraphics;
        private System.Drawing.Bitmap GdiBitmap;
        private InteropBitmap InteropBitmap;

        private const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        private const uint PAGE_READWRITE = 0x04;
        private readonly PixelFormat PixelFormat;

        private readonly System.Drawing.Imaging.PixelFormat GdiPixelFormat;
        private readonly int BytesPerPixel;

        protected int ImageWidth { get; private set; } = 1;
        protected int ImageHeight { get; private set; } = 1;

        private readonly Action<BitmapSource> _setSource;
        private readonly Func<T, T> _cloneData;

        public GdiRendererBase(Action<BitmapSource> setSource, Func<T, T> cloneData)
        {
            _setSource = setSource;
            _cloneData = cloneData;

            //GdiPixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
            //PixelFormat = PixelFormats.Pbgra32;
            GdiPixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            PixelFormat = PixelFormats.Bgra32;

            BytesPerPixel = PixelFormat.BitsPerPixel / 8;

            RenderWorker = new BackgroundWorker();
            RenderWorker.DoWork += new DoWorkEventHandler(RenderWorker_DoWork);
            RenderWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RenderWorker_RunWorkerCompleted);
            RenderWorker.WorkerSupportsCancellation = true;
        }

        private bool _isBusy;
        public bool IsBusy => RenderWorker.IsBusy || _isBusy;

        private bool IsBitmapInvalid = true;

        protected DateTime RenderStarted = DateTime.Now;

        public void UpdateImageAsync(double actualWidth, double actualHeight, T data)
        {
            if (IsBusy)
            {
                if (DateTime.UtcNow < RenderStarted + TimeSpan.FromMilliseconds(50))
                {
                    // Debug.WriteLine("RenderWorker busy.");
                    return;
                }

                RenderWorker.CancelAsync();
                return;
            }

            ImageWidth = Convert.ToInt32(actualWidth);
            ImageHeight = Convert.ToInt32(actualHeight);

            RenderStarted = DateTime.UtcNow;
            _isBusy = true;
            RenderWorker.RunWorkerAsync(_cloneData(data));
        }

        private void RenderWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _isBusy = false;
                return;
            }

            try
            {
                var bmpsrc = (e.Result as BitmapSource);

                if (bmpsrc != null && bmpsrc.CheckAccess())
                {
                    _setSource(bmpsrc);
                }
                else
                {
                    Debug.WriteLine("No access to TheImage");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occured getting image from drawing thread: {ex}");
            }
            finally
            {
                _isBusy = false;
            }

            //Debug.WriteLine("Render finished in " + (DateTime.UtcNow - RenderStarted).TotalMilliseconds + "ms");
        }

        private void RenderWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var renderDataList = (T)e.Argument;

                if (renderDataList == null)
                {
                    return;
                }

                Initialize();

                RenderData(renderDataList);

                InteropBitmap.Invalidate();
                InteropBitmap.Freeze();

                //var debug_bytes = Debug_BitmapSourceToArray(InteropBitmap);
                //var debug_values = debug_bytes.OrderByDescending(x => x).GroupBy(x => x).ToList();

                e.Result = InteropBitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception occurred in RenderWorker_DoWork: " + ex.Message);
            }
        }

        //// DEBUG
        //private byte[] Debug_BitmapSourceToArray(BitmapSource bitmapSource)
        //{
        //    // Stride = (width) x (bytes per pixel)
        //    var stride = bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
        //    var pixels = new byte[bitmapSource.PixelHeight * stride];

        //    bitmapSource.CopyPixels(pixels, stride, 0);

        //    return pixels;
        //}

        public abstract void RenderData(T renderDataList);


        public void Clear()
        {
            //The next draw will release the image bitmap from memory.
            _setSource(null);
        }

        protected IntPtr MapFileHandle = (IntPtr)(-1);
        protected IntPtr MapViewPtr = (IntPtr)(-1);

        protected void Initialize()
        {
            if (IsBitmapInitialized || IsBitmapInvalid)
            {
                Deallocate();
            }

            // this additional copy may not be necessary, but i want to keep using a local variable, and i don't want to access this.ActualWidth from this thread.
            var actualWidth = ImageWidth;
            var actualHeight = ImageHeight;

            // but at least 1 pixel to avoid problems.
            actualHeight = Math.Max(1, actualHeight);
            actualWidth = Math.Max(1, actualWidth);

            var byteCount = (uint)(actualWidth * actualHeight * BytesPerPixel);

            // Make memory map
            MapFileHandle = NativeMethods.CreateFileMapping(new IntPtr(-1), IntPtr.Zero, PAGE_READWRITE, 0, byteCount, null);
            MapViewPtr = NativeMethods.MapViewOfFile(MapFileHandle, FILE_MAP_ALL_ACCESS, 0, 0, byteCount);

            //Create the InteropBitmap  
            InteropBitmap = Imaging.CreateBitmapSourceFromMemorySection(MapFileHandle,
                                                                        actualWidth,
                                                                        actualHeight,
                                                                        PixelFormat,
                                                                        actualWidth * PixelFormat.BitsPerPixel / 8,
                                                                        0)
                                                                        as InteropBitmap;
            GdiGraphics = GetGdiGraphics(MapViewPtr);

            IsBitmapInitialized = true;
            IsBitmapInvalid = false;
        }

        private void Deallocate()
        {
            if (GdiGraphics != null)
            {
                GdiGraphics.Dispose();
            }
            try
            {
                if (MapViewPtr != (IntPtr)(-1))
                {
                    NativeMethods.UnmapViewOfFile(MapViewPtr);
                }
                if (MapFileHandle != (IntPtr)(-1))
                {
                    NativeMethods.CloseHandle(MapFileHandle);
                }
            }
            catch (Exception ex)
            {
                // most likely error is because we freed twice, so don't worry about it. 
                // if its in use, let them worry about it.
#if DEBUG
                MessageBox.Show("Error occurred freeing bitmap in GraphCanvas." + ex.Message);
#endif
            }
            finally
            {
                MapViewPtr = (IntPtr)(-1);
                MapFileHandle = (IntPtr)(-1);
                IsBitmapInitialized = false;
            }
        }

        private Graphics GetGdiGraphics(IntPtr mapPointer)
        {
            var actualWidth = Convert.ToInt32(ImageWidth);
            var actualHeight = Convert.ToInt32(ImageHeight);
            actualHeight = Math.Max(1, actualHeight);
            actualWidth = Math.Max(1, actualWidth);

            //create the GDI Bitmap 
            GdiBitmap = new System.Drawing.Bitmap(actualWidth,
                                                  actualHeight,
                                                  actualWidth * BytesPerPixel,
                                                  GdiPixelFormat,
                                                  mapPointer);
            // Get GDI Graphics 
            var gdiGraphics = System.Drawing.Graphics.FromImage(GdiBitmap);
            // Not Allowed with Font
            // https://stackoverflow.com/questions/1232165/system-drawing-graphics-drawstring-parameter-is-not-valid-exception
            // gdiGraphics.CompositingMode = CompositingMode.SourceCopy;
            gdiGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            gdiGraphics.SmoothingMode = SmoothingMode.HighSpeed;

            return gdiGraphics;
        }
    }

    internal class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateFileMapping(IntPtr hFile,
        IntPtr lpFileMappingAttributes,
        uint flProtect,
        uint dwMaximumSizeHigh,
        uint dwMaximumSizeLow,
        string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject,
        uint dwDesiredAccess,
        uint dwFileOffsetHigh,
        uint dwFileOffsetLow,
        uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int CloseHandle(IntPtr hObject);

        [DllImport("gdi32")]
        internal static extern int DeleteObject(IntPtr o);
    }
}