using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Interop = System.Windows.Interop;

namespace ODR
{
    public static class WpfExtensions
    {
        public static IWin32Window GetIWin32Window(this Visual visual)
        {
            var source = PresentationSource.FromVisual(visual) as Interop.HwndSource;
            IWin32Window win = new OldWindow(source.Handle);
            return win;
        }

        private class OldWindow : IWin32Window
        {
            private readonly IntPtr _handle;
            public OldWindow(IntPtr handle)
            {
                _handle = handle;
            }

            IntPtr IWin32Window.Handle
            {
                get { return _handle; }
            }
        }
    }
}
