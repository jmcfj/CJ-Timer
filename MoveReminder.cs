using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

class MoveReminder : Form {
    static readonly Color Blue=Color.FromArgb(18,99,237), Ink=Color.FromArgb(20,41,72);
    readonly Timer timer=new Timer(); readonly NumericUpDown minutes=new NumericUpDown();
    readonly Label status=new Label(); readonly Button action=new Button(); readonly Ring ring=new Ring();
    readonly NotifyIcon tray=new NotifyIcon(); Reminder popup; DateTime deadline; bool exercise=false,due=false,quitting=false; int interval=30;
    string reminderTitle="该起来活动啦！", reminderBody="不要久坐不动，站起来走走、伸展一下。\n下一段专注，从活动身体开始。";
    readonly string contentPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MoveReminder","content.txt");
    readonly string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MoveReminder","interval.txt");
    [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(Array.IndexOf(args,"--check")>=0){using(var f=new MoveReminder()){f.CreateControl();using(var image=new Bitmap(244,244)){f.ring.Value="28:50";f.ring.DrawToBitmap(image,new Rectangle(0,0,244,244));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"font-check.png"));}f.BecomeDue();if(f.popup==null)throw new Exception("Missing reminder");f.popup.BackColor=Color.White;using(var preview=new Bitmap(320,398)){f.popup.DrawToBitmap(preview,new Rectangle(0,0,320,398));preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"melon-preview.png"));}f.popup.SetContent("自定义测试","起身走动一下");f.ToggleExercise();if(!f.exercise||f.due)throw new Exception("Pause failed");f.ToggleExercise();if(f.exercise||f.due||f.popup!=null)throw new Exception("Restart failed");f.tray.Dispose();f.timer.Dispose();}return;}Application.Run(new MoveReminder());}
    public MoveReminder(){
        Text="动一下 · MOVE"; ClientSize=new Size(480,650); MinimumSize=MaximumSize=Size; MaximizeBox=false; BackColor=Color.White; ForeColor=Ink; Font=new Font("Microsoft YaHei UI",11,FontStyle.Regular); StartPosition=FormStartPosition.CenterScreen; AutoScaleMode=AutoScaleMode.Dpi;
        try {int n;if(int.TryParse(File.ReadAllText(settings),out n)&&n>=1&&n<=240)interval=n;}catch{}
        try {var lines=File.ReadAllLines(contentPath);if(lines.Length>=2){reminderTitle=lines[0];reminderBody=string.Join(Environment.NewLine,lines,1,lines.Length-1);}}catch{}
        var title=LabelAt("动一下 · MOVE",26,22,420,35,20,true); title.ForeColor=Blue;
        LabelAt("专注，也别忘了动一下。",26,70,430,38,19,true);
        status.SetBounds(26,116,430,32);Controls.Add(status);
        ring.SetBounds(118,170,244,244);Controls.Add(ring);
        action.SetBounds(26,447,428,58); Style(action);action.Click+=(s,e)=>ToggleExercise();Controls.Add(action);
        LabelAt("提醒间隔",26,533,100,34,11,false);minutes.SetBounds(130,533,72,34);minutes.Minimum=1;minutes.Maximum=240;minutes.Value=interval;Controls.Add(minutes);LabelAt("分钟",210,533,50,34,11,false);
        var apply=new Button{Text="应用并重新计时"};apply.SetBounds(270,530,184,40);apply.Click+=(s,e)=>{interval=(int)minutes.Value;try{Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllText(settings,interval.ToString());}catch{}Restart();};Controls.Add(apply);
        var preview=new Button{Text="预览到点提醒"};preview.SetBounds(26,580,180,36);preview.Click+=(s,e)=>BecomeDue();Controls.Add(preview);
        var customize=new Button{Text="自定义提醒内容"};customize.SetBounds(218,580,236,36);customize.Click+=(s,e)=>EditContent();Controls.Add(customize);
        LabelAt("关闭窗口后仍在托盘运行",26,619,428,26,10,false);
        var menu=new ContextMenuStrip();menu.Items.Add("打开主界面",null,(s,e)=>ShowMain());menu.Items.Add("退出提醒器",null,(s,e)=>{quitting=true;Close();});tray.Icon=SystemIcons.Information;tray.Text="动一下 · MOVE";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=(s,e)=>ShowMain();
        MinimumSize=Size.Empty;MaximumSize=Size.Empty;Scale(new SizeF(0.86f,0.86f));MinimumSize=MaximumSize=Size;
        timer.Interval=500;timer.Tick+=(s,e)=>{if(!exercise&&!due&&DateTime.UtcNow>=deadline)BecomeDue();UpdateUI();};Restart();timer.Start();
        FormClosing+=(s,e)=>{if(!quitting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}else{timer.Stop();if(popup!=null)popup.Close();tray.Dispose();}};
    }
    Label LabelAt(string text,int x,int y,int w,int h,int size,bool bold){var l=new Label{Text=text,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),TextAlign=ContentAlignment.MiddleLeft};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
    internal static void Style(Button b){b.BackColor=Blue;b.ForeColor=Color.White;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.Font=new Font("Microsoft YaHei UI",12,FontStyle.Bold);}
    void ShowMain(){Show();WindowState=FormWindowState.Normal;Activate();}
    void Restart(){exercise=false;due=false;deadline=DateTime.UtcNow.AddMinutes(interval);if(popup!=null){popup.Close();popup=null;}UpdateUI();}
    void ToggleExercise(){if(exercise){Restart();return;}exercise=true;due=false;UpdateUI();if(popup!=null)popup.RefreshState(true);}
    void BecomeDue(){if(exercise)return;due=true;if(popup==null||popup.IsDisposed){popup=new Reminder(ToggleExercise,reminderTitle,reminderBody);popup.Show();}else popup.Show();popup.RefreshState(false);UpdateUI();}
    void UpdateUI(){var remaining=Math.Max(0,(int)Math.Ceiling((deadline-DateTime.UtcNow).TotalSeconds));ring.Caption=exercise?"保持活力":"距离下次活动";ring.Value=exercise?"运动中":due?"00:00":string.Format("{0:00}:{1:00}",remaining/60,remaining%60);ring.Invalidate();status.Text=exercise?"提醒已暂停 · 等待运动结束":due?"时间到 · 请起来活动":"正在计时 · 到点提醒你起身活动";action.Text=exercise?"运动结束，重新计时":"暂停提醒，去运动";}
    void EditContent(){
        using(var dialog=new Form{Text="自定义提醒内容",ClientSize=new Size(430,340),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font,AutoScaleMode=AutoScaleMode.Dpi}){
            var titleLabel=new Label{Text="提醒标题（最多 16 字）",Left=20,Top=18,Width=390};
            var titleBox=new TextBox{Text=reminderTitle,MaxLength=16,Left=20,Top=48,Width=390};
            var bodyLabel=new Label{Text="提醒正文（最多 120 字）",Left=20,Top=92,Width=390};
            var bodyBox=new TextBox{Text=reminderBody,MaxLength=120,Multiline=true,ScrollBars=ScrollBars.Vertical,Left=20,Top=122,Width=390,Height=132};
            var save=new Button{Text="保存内容",Left=230,Top=278,Width=180,Height=44};Style(save);
            save.Click+=(sender,args)=>{if(string.IsNullOrWhiteSpace(titleBox.Text)||string.IsNullOrWhiteSpace(bodyBox.Text)){MessageBox.Show(dialog,"标题和正文都不能为空。","请填写提醒内容");return;}try{Directory.CreateDirectory(Path.GetDirectoryName(contentPath));File.WriteAllText(contentPath,titleBox.Text.Trim()+Environment.NewLine+bodyBox.Text.Trim());}catch(Exception ex){MessageBox.Show(dialog,"保存失败："+ex.Message);return;}reminderTitle=titleBox.Text.Trim();reminderBody=bodyBox.Text.Trim();if(popup!=null&&!popup.IsDisposed)popup.SetContent(reminderTitle,reminderBody);dialog.Close();};
            dialog.Controls.AddRange(new Control[]{titleLabel,titleBox,bodyLabel,bodyBox,save});dialog.ShowDialog(this);
        }
    }
    class Ring:Control{
        public string Caption="",Value="";
        public Ring(){DoubleBuffered=true;BackColor=Color.White;}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);if(Width<1||Height<1)return;
            // Render grayscale antialiasing at 3x the actual device resolution,
            // then downsample once. No ClearType colour fringes or transformed glyph hinting.
            using(var bitmap=new Bitmap(Width*3,Height*3)){
                using(var g=Graphics.FromImage(bitmap)){
                    g.Clear(Color.White);g.SmoothingMode=SmoothingMode.HighQuality;
                    g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                    g.ScaleTransform(Width*3/244f,Height*3/244f);
                    using(var pen=new Pen(Color.FromArgb(232,240,255),10))g.DrawEllipse(pen,8,8,228,228);
                    using(var pen=new Pen(Blue,10))g.DrawArc(pen,8,8,228,228,-130,290);
                    Draw(g,Caption,14,60,FontStyle.Regular,false);
                    Draw(g,Value,31,100,FontStyle.Bold,Value.IndexOf(':')>=0);
                    Draw(g,"起来走走，状态更好",12,165,FontStyle.Regular,false);
                }
                e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
                e.Graphics.DrawImage(bitmap,new Rectangle(0,0,Width,Height),0,0,bitmap.Width,bitmap.Height,GraphicsUnit.Pixel);
            }
        }
        void Draw(Graphics g,string text,float size,int y,FontStyle style,bool numeric){
            using(var font=new Font(numeric?"Segoe UI":"Microsoft YaHei UI",size*96f/72f,style,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(Ink))
            using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
                g.DrawString(text,font,brush,new RectangleF(12,y,220,45),format);
        }
    }
    class Reminder:Form{
        readonly Timer pulse=new Timer();readonly Button button=new Button();readonly Label heading=new Label();
        readonly TextBox message=new TextBox{Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.Vertical,BackColor=Color.FromArgb(255,249,236)};
        readonly Image artwork;bool exercising=false,bright=false;string customTitle,customBody;
        public Reminder(Action toggle,string title,string body){
            customTitle=title;customBody=body;Text="动一下 · 西瓜提醒";ClientSize=new Size(320,398);
            FormBorderStyle=FormBorderStyle.None;TopMost=true;ShowInTaskbar=false;DoubleBuffered=true;
            BackColor=Color.FromArgb(255,0,255);TransparencyKey=BackColor;Font=new Font("Microsoft YaHei UI",10);AutoScaleMode=AutoScaleMode.None;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("melon.png"))using(var source=Image.FromStream(stream))artwork=new Bitmap(source);
            heading.SetBounds(29,278,262,28);heading.Font=new Font(Font.FontFamily,12,FontStyle.Bold);heading.TextAlign=ContentAlignment.MiddleCenter;heading.BackColor=Color.FromArgb(255,249,236);heading.ForeColor=Ink;Controls.Add(heading);
            message.SetBounds(29,309,262,34);message.ForeColor=Ink;Controls.Add(message);
            button.SetBounds(29,351,262,38);Style(button);button.BackColor=Color.FromArgb(48,123,76);button.Font=new Font(Font.FontFamily,10,FontStyle.Bold);button.Click+=(s,e)=>toggle();Controls.Add(button);
            Shown+=(s,e)=>Position();pulse.Interval=850;pulse.Tick+=(s,e)=>{bright=!bright;Invalidate();};FormClosed+=(s,e)=>{pulse.Dispose();artwork.Dispose();};RefreshState(false);
            MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
        void Position(){var area=Screen.FromPoint(Cursor.Position).WorkingArea;Location=new Point(area.Right-Width-12,area.Bottom-Height-12);}
        public void SetContent(string title,string body){customTitle=title;customBody=body;RefreshState(exercising);}
        public void RefreshState(bool active){exercising=active;heading.Text=active?"享受活动的时间":customTitle;message.Text=active?"提醒已暂停，运动结束后再开始。":customBody;message.Select(0,0);button.Text=active?"运动结束，重新计时":"暂停提醒，去运动";if(active)pulse.Stop();else pulse.Start();Invalidate();}
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.DrawImage(artwork,new Rectangle(0,0,320,280));
            using(var brush=new SolidBrush(Color.FromArgb(255,249,236)))g.FillRoundedRectangle(brush,new Rectangle(17,269,286,127),18);
            using(var pen=new Pen(!exercising&&bright?Color.FromArgb(88,170,101):Color.FromArgb(221,230,202),3))g.DrawRoundedRectangle(pen,new Rectangle(17,269,286,127),18);
            using(var font=new Font("Segoe UI",27,FontStyle.Bold))TextRenderer.DrawText(g,exercising?"休息中":"00:00",font,new Rectangle(55,178,210,50),Color.FromArgb(74,42,40),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        }
    }
}
static class RoundedDrawing{
    static GraphicsPath Path(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    public static void FillRoundedRectangle(this Graphics g,Brush b,Rectangle r,int radius){using(var p=Path(r,radius))g.FillPath(b,p);}
    public static void DrawRoundedRectangle(this Graphics g,Pen b,Rectangle r,int radius){using(var p=Path(r,radius))g.DrawPath(b,p);}
}


