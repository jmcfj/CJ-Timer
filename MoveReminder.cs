using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

class MoveReminder : Form {
    static readonly Color Blue=Color.FromArgb(18,99,237), Ink=Color.FromArgb(20,41,72);
    readonly Timer timer=new Timer();
    
    readonly NotifyIcon tray=new NotifyIcon(); Reminder popup; DateTime deadline; bool exercise=false,due=false; int interval=30;
    readonly string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MoveReminder","interval.txt");
    [STAThread] static void Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        using(var controller=new MoveReminder()){
            if(Array.IndexOf(args,"--check")>=0){
                controller.popup.Show();Application.DoEvents();
                if(controller.popup.ShowInTaskbar||controller.popup.alphaArtwork.ShowInTaskbar)throw new Exception("Taskbar visibility regression");if(controller.popup.TimerValue=="00:00")throw new Exception("Startup countdown missing");
                using(var image=new Bitmap(320,370)){controller.popup.BackColor=Color.White;controller.popup.DrawToBitmap(image,new Rectangle(0,0,320,370));using(var g=Graphics.FromImage(image))using(var art=controller.popup.alphaArtwork.RenderBitmap(controller.popup.TimerValue))g.DrawImageUnscaled(art,0,0);image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-preview.png"));}
                controller.popup.ClientSize=new Size(500,500);if(Math.Abs(controller.popup.Height-controller.popup.Width*370.0/320.0)>1)throw new Exception("Aspect ratio failed");
                controller.popup.HideToTray();if(controller.popup.Visible||controller.popup.alphaArtwork.Visible||!controller.tray.Visible||!controller.timer.Enabled)throw new Exception("Tray minimize failed");controller.BecomeDue();if(!controller.popup.Visible||!controller.popup.alphaArtwork.Visible)throw new Exception("Due restore failed");controller.Restart();
                controller.popup.ClientSize=new Size(480,675);if(controller.popup.Region==null)throw new Exception("Resize shape missing");controller.popup.ClientSize=new Size(320,370);controller.BecomeDue();controller.ToggleExercise();if(!controller.exercise)throw new Exception("Pause failed");controller.ToggleExercise();if(controller.exercise||controller.popup.IsDisposed)throw new Exception("Restart failed");
                controller.ExitApp();if(controller.timer.Enabled||!controller.popup.IsDisposed)throw new Exception("Exit failed");return;
            }
            try{Application.Run(controller.popup);}finally{controller.timer.Stop();controller.timer.Dispose();}
        }
    }
    public MoveReminder(){
        Text="动一下 · MOVE"; ClientSize=new Size(480,650); MinimumSize=MaximumSize=Size; MaximizeBox=false; BackColor=Color.White; ForeColor=Ink; Font=new Font("Microsoft YaHei UI",11,FontStyle.Regular); StartPosition=FormStartPosition.CenterScreen; AutoScaleMode=AutoScaleMode.Dpi;
        try {int n;if(int.TryParse(File.ReadAllText(settings),out n)&&n>=1&&n<=240)interval=n;}catch{}
        timer.Interval=500;timer.Tick+=(s,e)=>{if(!exercise&&!due&&DateTime.UtcNow>=deadline)BecomeDue();UpdateUI();};Restart();timer.Start();
        var trayMenu=new ContextMenuStrip();trayMenu.Items.Add("显示西瓜计时器",null,(s,e)=>popup.RestoreFromTray());trayMenu.Items.Add("最小化到托盘",null,(s,e)=>popup.HideToTray());trayMenu.Items.Add("退出",null,(s,e)=>ExitApp());
        tray.Icon=SystemIcons.Information;tray.Text="动一下 · 西瓜提醒";tray.ContextMenuStrip=trayMenu;tray.DoubleClick+=(s,e)=>popup.RestoreFromTray();tray.Visible=true;
        popup.FormClosed+=(s,e)=>{timer.Stop();tray.Visible=false;tray.Dispose();};
        FormClosing+=(s,e)=>{timer.Stop();if(popup!=null&&!popup.IsDisposed)popup.Close();tray.Visible=false;tray.Dispose();};
    }
    Label LabelAt(string text,int x,int y,int w,int h,int size,bool bold){var l=new Label{Text=text,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),TextAlign=ContentAlignment.MiddleLeft};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
    internal static void Style(Button b){b.BackColor=Blue;b.ForeColor=Color.White;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.Font=new Font("Microsoft YaHei UI",12,FontStyle.Bold);}
    void ExitApp(){timer.Stop();if(popup!=null&&!popup.IsDisposed)popup.Close();tray.Dispose();}
    void Restart(){exercise=false;due=false;deadline=DateTime.UtcNow.AddMinutes(interval);
        if(popup==null)popup=new Reminder(ToggleExercise,ExitApp,EditSettings);
        popup.RefreshState(false);UpdateUI();}
    void ToggleExercise(){if(exercise){Restart();return;}exercise=true;due=false;popup.RefreshState(true);UpdateUI();}
    void BecomeDue(){if(exercise)return;due=true;popup.RestoreFromTray();popup.RefreshState(false);popup.TopMost=true;UpdateUI();}
    void UpdateUI(){var remaining=Math.Max(0,(int)Math.Ceiling((deadline-DateTime.UtcNow).TotalSeconds));
        popup.UpdateTimer(exercise?"休息中":string.Format("{0:00}:{1:00}",remaining/60,remaining%60),due,exercise);}
    void EditSettings(){
        using(var dialog=new Form{Text="提醒设置",ShowInTaskbar=false,ClientSize=new Size(350,150),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font,AutoScaleMode=AutoScaleMode.Dpi}){
            var label=new Label{Text="提醒间隔（分钟）",Left=20,Top=25,Width=190};
            var input=new NumericUpDown{Minimum=1,Maximum=240,Value=interval,Left=220,Top=25,Width=100};
            var save=new Button{Text="保存并重新计时",Left=120,Top=86,Width=200,Height=42};Style(save);
            save.Click+=(sender,args)=>{try{Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllText(settings,input.Value.ToString());}catch(Exception ex){MessageBox.Show(dialog,"保存失败："+ex.Message);return;}interval=(int)input.Value;Restart();dialog.Close();};
            dialog.Controls.AddRange(new Control[]{label,input,save});dialog.ShowDialog(popup);
        }
    }
    class Reminder:Form{
        readonly Timer pulse=new Timer();readonly Button button=new Button();        readonly Image artwork;public AlphaArtwork alphaArtwork;bool exercising=false,bright=false;public string TimerValue="30:00";bool isDue=false;bool layoutReady=false;readonly System.Collections.Generic.Dictionary<Control,Font> scaledFonts=new System.Collections.Generic.Dictionary<Control,Font>();
        public Reminder(Action toggle,Action exit,Action settingsAction){
            Text="动一下 · 西瓜提醒";ClientSize=new Size(320,370);MinimumSize=new Size(280,324);MaximumSize=new Size(640,740);
            FormBorderStyle=FormBorderStyle.None;TopMost=true;ShowInTaskbar=false;DoubleBuffered=true;
            BackColor=Color.FromArgb(75,42,32);Font=new Font("Microsoft YaHei UI",10);AutoScaleMode=AutoScaleMode.None;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("melon.png"))using(var source=Image.FromStream(stream))artwork=new Bitmap(source);
            button.SetBounds(29,320,262,38);Style(button);button.BackColor=Color.FromArgb(48,123,76);button.Font=new Font(Font.FontFamily,10,FontStyle.Bold);button.Click+=(s,e)=>toggle();Controls.Add(button);
            var pin=new CheckBox{Text="固定",Checked=true,Appearance=Appearance.Button,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(255,249,236)};pin.FlatStyle=FlatStyle.Flat;pin.FlatAppearance.BorderSize=0;pin.SetBounds(29,283,64,28);pin.CheckedChanged+=(s,e)=>TopMost=pin.Checked;Controls.Add(pin);
            var close=new Button{Text="×",BackColor=Color.FromArgb(255,249,236),FlatStyle=FlatStyle.Flat};close.FlatAppearance.BorderSize=0;close.SetBounds(259,283,32,28);close.AccessibleName="关闭并退出提醒器";close.Click+=(s,e)=>exit();Controls.Add(close);
            var settingsButton=new Button{Text="设置",FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(255,249,236)};settingsButton.FlatAppearance.BorderSize=0;settingsButton.SetBounds(185,283,64,28);settingsButton.Click+=(s,e)=>settingsAction();Controls.Add(settingsButton);
            var minimize=new Button{Text="收起",FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(255,249,236)};minimize.FlatAppearance.BorderSize=0;minimize.SetBounds(105,283,64,28);minimize.AccessibleName="最小化到系统托盘";minimize.Click+=(s,e)=>HideToTray();Controls.Add(minimize);
            foreach(Control control in Controls)control.Tag=control.Bounds;
            layoutReady=true;Resize+=(s,e)=>Relayout();Relayout();
            Shown+=(s,e)=>{Position();alphaArtwork=new AlphaArtwork(this,artwork);alphaArtwork.Show(this);alphaArtwork.RefreshSurface(TimerValue);};LocationChanged+=(s,e)=>{if(alphaArtwork!=null)alphaArtwork.RefreshSurface(TimerValue);};pulse.Interval=850;pulse.Tick+=(s,e)=>{bright=!bright;Invalidate();};FormClosed+=(s,e)=>{pulse.Dispose();if(alphaArtwork!=null)alphaArtwork.Close();artwork.Dispose();foreach(var font in scaledFonts.Values)font.Dispose();};RefreshState(false);
            MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};
        }
        public void HideToTray(){if(alphaArtwork!=null)alphaArtwork.Hide();Hide();}
        public void RestoreFromTray(){Show();WindowState=FormWindowState.Normal;if(alphaArtwork!=null){if(!alphaArtwork.Visible)alphaArtwork.Show();alphaArtwork.RefreshSurface(TimerValue);}Activate();}
        protected override void SetBoundsCore(int x,int y,int width,int height,BoundsSpecified specified){
            if(layoutReady&&(specified&BoundsSpecified.Size)!=0){
                int w=width!=Width?width:(int)Math.Round(height*320.0/370.0);
                width=Math.Max(280,Math.Min(640,w));height=(int)Math.Round(width*370.0/320.0);
            }
            base.SetBoundsCore(x,y,width,height,specified);
        }
        void Relayout(){
            if(!layoutReady||ClientSize.Width<1||ClientSize.Height<1)return;
            float sx=ClientSize.Width/320f,sy=ClientSize.Height/370f;
            foreach(Control control in Controls){Rectangle b=(Rectangle)control.Tag;control.SetBounds((int)(b.X*sx),(int)(b.Y*sy),(int)(b.Width*sx),(int)(b.Height*sy));
                Font oldFont;scaledFonts.TryGetValue(control,out oldFont);control.Font=new Font("Microsoft YaHei UI",(10)*Math.Min(sx,sy),control==button?FontStyle.Bold:FontStyle.Regular);scaledFonts[control]=control.Font;if(oldFont!=null)oldFont.Dispose();}
            using(var path=RoundedDrawing.Path(new Rectangle((int)(17*sx),(int)(273*sy),(int)(286*sx),(int)(95*sy)),(int)(18*Math.Min(sx,sy)))){
                var previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();
            }
            if(alphaArtwork!=null)alphaArtwork.RefreshSurface(TimerValue);Invalidate();
        }
        protected override void WndProc(ref Message m){
            if(m.Msg==0x214&&layoutReady){
                var r=(NativeRect)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam,typeof(NativeRect));
                int edge=m.WParam.ToInt32();int width=r.Right-r.Left,height=r.Bottom-r.Top;
                if(edge==3||edge==6){width=Math.Max(280,Math.Min(640,(int)Math.Round(height*320.0/370.0)));height=(int)Math.Round(width*370.0/320.0);r.Right=r.Left+width;if(edge==3)r.Top=r.Bottom-height;else r.Bottom=r.Top+height;}
                else{width=Math.Max(280,Math.Min(640,width));height=(int)Math.Round(width*370.0/320.0);if(edge==1||edge==4||edge==7)r.Left=r.Right-width;else r.Right=r.Left+width;if(edge==4||edge==5)r.Top=r.Bottom-height;else r.Bottom=r.Top+height;}
                System.Runtime.InteropServices.Marshal.StructureToPtr(r,m.LParam,false);m.Result=(IntPtr)1;return;
            }
            base.WndProc(ref m);
            if(m.Msg==0x84&&layoutReady){long packed=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(packed&65535),(short)((packed>>16)&65535)));float sx=ClientSize.Width/320f,sy=ClientSize.Height/370f;
                bool left=p.X<25*sx,right=p.X>295*sx,bottom=p.Y>358*sy,top=p.Y<12*sy;
                if(p.Y>280*sy||top)m.Result=(IntPtr)(bottom?(left?16:right?17:15):top?(left?13:right?14:12):left?10:right?11:1);
            }
        }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
        [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
        void Position(){var area=Screen.FromPoint(Cursor.Position).WorkingArea;Location=new Point(area.Right-Width-12,area.Bottom-Height-12);}
        public void UpdateTimer(string value,bool due,bool active){bool changed=TimerValue!=value;TimerValue=value;if(changed&&alphaArtwork!=null)alphaArtwork.RefreshSurface(value);isDue=due;if(due&&!active)pulse.Start();else pulse.Stop();Invalidate();}
        public void RefreshState(bool active){exercising=active;button.Text=active?"运动结束，重新计时":"暂停提醒，去运动";if(active)pulse.Stop();else pulse.Start();Invalidate();}
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.ScaleTransform(ClientSize.Width/320f,ClientSize.Height/370f);

            using(var brush=new SolidBrush(Color.FromArgb(255,249,236)))g.FillRoundedRectangle(brush,new Rectangle(17,273,286,95),18);
            using(var pen=new Pen(isDue&&!exercising&&bright?Color.FromArgb(88,170,101):Color.FromArgb(221,230,202),3))g.DrawRoundedRectangle(pen,new Rectangle(17,273,286,95),18);

        }
    }
}
static class RoundedDrawing{
    public static GraphicsPath Path(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    public static void FillRoundedRectangle(this Graphics g,Brush b,Rectangle r,int radius){using(var p=Path(r,radius))g.FillPath(b,p);}
    public static void DrawRoundedRectangle(this Graphics g,Pen b,Rectangle r,int radius){using(var p=Path(r,radius))g.DrawPath(b,p);}
}









