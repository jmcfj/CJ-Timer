using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// A per-pixel alpha surface preserves fractional edge coverage against any desktop.
sealed class AlphaArtwork : Form {
    readonly Form host; readonly Image source;string currentValue="30:00";
    public AlphaArtwork(Form owner,Image image){host=owner;source=image;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){Point pt=e.Location;if(pt.Y>Height*0.9){ReleaseCapture();SendMessage(host.Handle,0xA1,(IntPtr)17,IntPtr.Zero);return;}foreach(Control control in host.Controls)if(control.Bounds.Contains(pt)){var check=control as CheckBox;if(check!=null)check.Checked=!check.Checked;else{var button=control as Button;if(button!=null)button.PerformClick();}if(!IsDisposed)RefreshSurface(currentValue);return;}ReleaseCapture();SendMessage(host.Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};}
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80000|0x80;return p;}}
    public Bitmap RenderBitmap(string value){
        int w=host.ClientSize.Width,h=Math.Max(1,(int)(280*host.ClientSize.Height/280f));
        using(var large=new Bitmap(w*2,h*2,PixelFormat.Format32bppPArgb)){
            using(var g=Graphics.FromImage(large)){
                g.SmoothingMode=SmoothingMode.HighQuality;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                float sx=w*2/320f,sy=host.ClientSize.Height*2/280f;
                g.DrawImage(source,new RectangleF(20*sx,0,280*sx,280*sy));
                g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                using(var f=new Font("Segoe UI",32*Math.Min(sx,sy),FontStyle.Bold,GraphicsUnit.Pixel))
                using(var b=new SolidBrush(Color.FromArgb(74,42,40)))
                using(var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(value,f,b,new RectangleF(55*sx,176*sy,210*sx,40*sy),sf);
            }
            using(var g=Graphics.FromImage(large)){g.ScaleTransform(2,2);foreach(Control control in host.Controls){using(var b=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(b,new Rectangle(Point.Empty,control.Size));g.DrawImage(b,control.Bounds);}}}
            var result=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(large,new Rectangle(0,0,w,h),0,0,large.Width,large.Height,GraphicsUnit.Pixel);}return result;
        }
    }
    public void RefreshSurface(string value){currentValue=value;
        if(IsDisposed||!host.Visible)return;
        using(var bitmap=RenderBitmap(value)){
            IntPtr screen=GetDC(IntPtr.Zero),dc=IntPtr.Zero,hbitmap=IntPtr.Zero,old=IntPtr.Zero;
            try{dc=CreateCompatibleDC(screen);hbitmap=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(dc,hbitmap);
                var pos=new POINT(host.Left,host.Top);var origin=new POINT(0,0);var size=new SIZE(bitmap.Width,bitmap.Height);var blend=new BLEND{Operation=0,Flags=0,Alpha=255,Format=1};
                if(!UpdateLayeredWindow(Handle,screen,ref pos,ref size,dc,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }finally{if(old!=IntPtr.Zero)SelectObject(dc,old);if(hbitmap!=IntPtr.Zero)DeleteObject(hbitmap);if(dc!=IntPtr.Zero)DeleteDC(dc);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
        }
    }
    [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;public POINT(int x,int y){X=x;Y=y;}}
    [StructLayout(LayoutKind.Sequential)]struct SIZE{public int W,H;public SIZE(int w,int h){W=w;H=h;}}
    [StructLayout(LayoutKind.Sequential,Pack=1)]struct BLEND{public byte Operation,Flags,Alpha,Format;}
    [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr h,IntPtr d,ref POINT p,ref SIZE s,IntPtr src,ref POINT o,uint key,ref BLEND blend,uint flags);
    [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr h,IntPtr dc);
    [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr h);
    [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr h);
    [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr h);
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
}

