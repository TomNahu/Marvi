using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace Marvi
{
    // A WPF ContextMenu uses a non-activating popup HWND. A separate window
    // gives both the pet and tray entry points a real keyboard input target.
    internal sealed class PetMenu : Window
    {
        private readonly StackPanel rows = new StackPanel();
        public UIElementCollection Items { get { return rows.Children; } }
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr handle,int id,uint modifiers,uint key);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr handle,int id);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle, IntPtr after,
            int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out Bounds bounds);
        [StructLayout(LayoutKind.Sequential)] private struct Bounds { public int Left,Top,Right,Bottom; }

        public static bool KeyAvailable(Key key)
        {
            const int probeId=0x4156;
            if(!RegisterHotKey(IntPtr.Zero,probeId,6,(uint)KeyInterop.VirtualKeyFromKey(key))) return false;
            UnregisterHotKey(IntPtr.Zero,probeId);
            return true;
        }

        public PetMenu()
        {
            Title="Menú de Marvi";
            ShowInTaskbar=false;
            Topmost=true;
            WindowStyle=WindowStyle.None;
            ResizeMode=ResizeMode.NoResize;
            SizeToContent=SizeToContent.WidthAndHeight;
            WindowStartupLocation=WindowStartupLocation.Manual;
            Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source=new Uri("pack://application:,,,/Marvi;component/MenuTheme.xaml")
            });
            var scroll=new ScrollViewer { Content=rows, VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
            var paper=new Border { Child=scroll, Style=(Style)FindResource("Paper") };
            Content=new Border { Child=paper, Style=(Style)FindResource("Frame") };
            FontFamily=new System.Windows.Media.FontFamily("Consolas");
            FontSize=13;
            KeyboardNavigation.SetDirectionalNavigation(rows,KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetTabNavigation(rows,KeyboardNavigationMode.Cycle);
            Deactivated+=(s,e)=>{ if(IsVisible) Hide(); };
            PreviewKeyDown+=(s,e)=>{ if(e.Key==Key.Escape) { e.Handled=true; Hide(); } };
        }

        public void Open()
        {
            var pointer=Forms.Cursor.Position;
            var area=Forms.Screen.FromPoint(pointer).WorkingArea;
            var handle=new WindowInteropHelper(this).EnsureHandle();
            var fromDevice=HwndSource.FromHwnd(handle).CompositionTarget.TransformFromDevice;
            var point=fromDevice.Transform(new Point(pointer.X,pointer.Y));
            MaxHeight=area.Height*fromDevice.M22;
            Left=point.X; Top=point.Y;
            Show();
            UpdateLayout();
            Bounds bounds;
            if(GetWindowRect(handle,out bounds))
            {
                int x=Math.Max(area.Left,Math.Min(pointer.X,area.Right-(bounds.Right-bounds.Left)));
                int y=Math.Max(area.Top,Math.Min(pointer.Y,area.Bottom-(bounds.Bottom-bounds.Top)));
                SetWindowPos(handle,IntPtr.Zero,x,y,0,0,0x0015); // Keep size, z-order and activation.
            }
            SetForegroundWindow(handle);
            Activate();
            foreach(UIElement child in Items)
                if(child is Button && child.IsVisible && child.IsEnabled) { Keyboard.Focus(child); break; }
        }
    }
}
