using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// A per-pixel alpha surface preserves fractional edge coverage against any desktop.
sealed class AlphaArtwork : Form {
    readonly Form host; readonly Image source;string currentValue="30:00";readonly ToolTip hints=new ToolTip{InitialDelay=300,ReshowDelay=100};string hoverText="";
    public AlphaArtwork(Form owner,Image image){host=owner;source=image;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){Point pt=e.Location;if(pt.Y>Height*0.9){ReleaseCapture();SendMessage(host.Handle,0xA1,(IntPtr)17,IntPtr.Zero);return;}foreach(Control control in host.Controls)if(control.Bounds.Contains(pt)){var check=control as CheckBox;if(check!=null)check.Checked=!check.Checked;else{var button=control as Button;if(button!=null)button.PerformClick();}if(!IsDisposed)RefreshSurface(currentValue);return;}ReleaseCapture();SendMessage(host.Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);string text="";foreach(Control c in host.Controls)if(c.Bounds.Contains(e.Location)){text=c.Name=="pin"?(((CheckBox)c).Checked?"取消固定在最前":"固定在最前"):c.Name=="minimize"?"收起到系统托盘":c.Name=="settings"?"设置提醒间隔":c.Name=="close"?"关闭并退出":c.AccessibleName;break;}if(text!=hoverText){hoverText=text;hints.SetToolTip(this,text);}}
    protected override void Dispose(bool disposing){if(disposing)hints.Dispose();base.Dispose(disposing);}
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
            using(var g=Graphics.FromImage(large)){g.ScaleTransform(2,2);g.SmoothingMode=SmoothingMode.AntiAlias;foreach(Control control in host.Controls)DrawIcon(g,control);}
            var result=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(large,new Rectangle(0,0,w,h),0,0,large.Width,large.Height,GraphicsUnit.Pixel);}return result;
        }
    }
    void DrawIcon(Graphics g,Control control){
        var state=g.Save();Rectangle r=control.Bounds;g.TranslateTransform(r.X+r.Width/2f,r.Y+r.Height/2f);float scale=host.ClientSize.Width/320f;g.ScaleTransform(scale,scale);
        Color ink=Color.FromArgb(93,41,35);
        using(var pen=new Pen(ink,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
            switch(control.Name){
                case "pin":
                    if(((CheckBox)control).Checked)using(var b=new SolidBrush(Color.FromArgb(255,204,169)))g.FillEllipse(b,-14,-13,28,26);
                    g.DrawLine(pen,-5,-7,5,-7);g.DrawLine(pen,-3,-7,-3,0);g.DrawLine(pen,3,-7,3,0);g.DrawLines(pen,new PointF[]{new PointF(-3,0),new PointF(-6,4),new PointF(6,4),new PointF(3,0)});g.DrawLine(pen,0,4,0,10);break;
                case "minimize":g.DrawLines(pen,new PointF[]{new PointF(-5,-3),new PointF(0,2),new PointF(5,-3)});g.DrawLine(pen,-7,8,7,8);break;
                case "settings":
                    g.DrawEllipse(pen,-5,-5,10,10);g.DrawEllipse(pen,-1.5f,-1.5f,3,3);
                    for(int i=0;i<8;i++){double angle=i*Math.PI/4;g.DrawLine(pen,(float)Math.Cos(angle)*6,(float)Math.Sin(angle)*6,(float)Math.Cos(angle)*9,(float)Math.Sin(angle)*9);}break;
                case "close":g.DrawLine(pen,-5,-5,5,5);g.DrawLine(pen,-5,5,5,-5);break;
                case "action":
                    using(var shadow=new SolidBrush(Color.FromArgb(65,75,55,32)))g.FillEllipse(shadow,-15,-12,30,30);
                    using(var gradient=new LinearGradientBrush(new Rectangle(-16,-16,32,32),Color.FromArgb(91,167,101),Color.FromArgb(34,108,64),90f))g.FillEllipse(gradient,-15,-15,30,30);
                    using(var white=new Pen(Color.White,2)){white.StartCap=white.EndCap=LineCap.Round;
                        if(control.Text.StartsWith("运动结束")){g.DrawArc(white,-7,-7,14,14,25,285);g.DrawLines(white,new PointF[]{new PointF(3,-8),new PointF(7,-6),new PointF(7,-11)});}
                        else{g.DrawLine(white,-4,-6,-4,6);g.DrawLine(white,4,-6,4,6);}
                    }break;
            }
        }g.Restore(state);
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

