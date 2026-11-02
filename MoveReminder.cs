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
    string reminderTitle="该起来活动啦！", reminderBody="不要久坐不动，站起来走走、伸展一下。\n下一段专注，从活动身体开始。";
    readonly string contentPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MoveReminder","content.txt");
    readonly string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MoveReminder","interval.txt");
    [STAThread] static void Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        using(var controller=new MoveReminder()){
            if(Array.IndexOf(args,"--check")>=0){
                controller.popup.Show();Application.DoEvents();
                if(controller.popup.TimerValue=="00:00")throw new Exception("Startup countdown missing");
                using(var image=new Bitmap(320,450)){controller.popup.BackColor=Color.White;controller.popup.DrawToBitmap(image,new Rectangle(0,0,320,450));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-preview.png"));}
                controller.BecomeDue();controller.ToggleExercise();if(!controller.exercise)throw new Exception("Pause failed");controller.ToggleExercise();if(controller.exercise||controller.popup.IsDisposed)throw new Exception("Restart failed");
                controller.ExitApp();if(controller.timer.Enabled||!controller.popup.IsDisposed)throw new Exception("Exit failed");return;
            }
            try{Application.Run(controller.popup);}finally{controller.timer.Stop();controller.timer.Dispose();}
        }
    }
    public MoveReminder(){
        Text="动一下 · MOVE"; ClientSize=new Size(480,650); MinimumSize=MaximumSize=Size; MaximizeBox=false; BackColor=Color.White; ForeColor=Ink; Font=new Font("Microsoft YaHei UI",11,FontStyle.Regular); StartPosition=FormStartPosition.CenterScreen; AutoScaleMode=AutoScaleMode.Dpi;
        try {int n;if(int.TryParse(File.ReadAllText(settings),out n)&&n>=1&&n<=240)interval=n;}catch{}
        try {var lines=File.ReadAllLines(contentPath);if(lines.Length>=2){reminderTitle=lines[0];reminderBody=string.Join(Environment.NewLine,lines,1,lines.Length-1);}}catch{}
        timer.Interval=500;timer.Tick+=(s,e)=>{if(!exercise&&!due&&DateTime.UtcNow>=deadline)BecomeDue();UpdateUI();};Restart();timer.Start();
        FormClosing+=(s,e)=>{timer.Stop();if(popup!=null&&!popup.IsDisposed)popup.Close();tray.Visible=false;tray.Dispose();};
    }
    Label LabelAt(string text,int x,int y,int w,int h,int size,bool bold){var l=new Label{Text=text,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),TextAlign=ContentAlignment.MiddleLeft};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
    internal static void Style(Button b){b.BackColor=Blue;b.ForeColor=Color.White;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.Font=new Font("Microsoft YaHei UI",12,FontStyle.Bold);}
    void ExitApp(){timer.Stop();if(popup!=null&&!popup.IsDisposed)popup.Close();tray.Dispose();}
    void Restart(){exercise=false;due=false;deadline=DateTime.UtcNow.AddMinutes(interval);
        if(popup==null)popup=new Reminder(ToggleExercise,reminderTitle,reminderBody,ExitApp,EditContent);
        popup.RefreshState(false);UpdateUI();}
    void ToggleExercise(){if(exercise){Restart();return;}exercise=true;due=false;popup.RefreshState(true);UpdateUI();}
    void BecomeDue(){if(exercise)return;due=true;popup.RefreshState(false);popup.TopMost=true;UpdateUI();}
    void UpdateUI(){var remaining=Math.Max(0,(int)Math.Ceiling((deadline-DateTime.UtcNow).TotalSeconds));
        popup.UpdateTimer(exercise?"休息中":string.Format("{0:00}:{1:00}",remaining/60,remaining%60),due,exercise);}
    void EditContent(){
        using(var dialog=new Form{Text="提醒设置",ClientSize=new Size(430,410),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font,AutoScaleMode=AutoScaleMode.Dpi}){
            var intervalLabel=new Label{Text="提醒间隔（分钟）",Left=20,Top=18,Width=180};
            var intervalBox=new NumericUpDown{Minimum=1,Maximum=240,Value=interval,Left=230,Top=18,Width=180};
            var titleLabel=new Label{Text="提醒标题（最多 16 字）",Left=20,Top=78,Width=390};
            var titleBox=new TextBox{Text=reminderTitle,MaxLength=16,Left=20,Top=108,Width=390};
            var bodyLabel=new Label{Text="提醒正文（最多 120 字）",Left=20,Top=152,Width=390};
            var bodyBox=new TextBox{Text=reminderBody,MaxLength=120,Multiline=true,ScrollBars=ScrollBars.Vertical,Left=20,Top=182,Width=390,Height=132};
            var save=new Button{Text="保存并重新计时",Left=210,Top=338,Width=180,Height=44};Style(save);
            save.Click+=(sender,args)=>{if(string.IsNullOrWhiteSpace(titleBox.Text)||string.IsNullOrWhiteSpace(bodyBox.Text)){MessageBox.Show(dialog,"标题和正文都不能为空。","请填写提醒内容");return;}try{Directory.CreateDirectory(Path.GetDirectoryName(contentPath));File.WriteAllText(contentPath,titleBox.Text.Trim()+Environment.NewLine+bodyBox.Text.Trim());File.WriteAllText(settings,intervalBox.Value.ToString());}catch(Exception ex){MessageBox.Show(dialog,"保存失败："+ex.Message);return;}interval=(int)intervalBox.Value;reminderTitle=titleBox.Text.Trim();reminderBody=bodyBox.Text.Trim();Restart();if(popup!=null&&!popup.IsDisposed)popup.SetContent(reminderTitle,reminderBody);dialog.Close();};
            dialog.Controls.AddRange(new Control[]{intervalLabel,intervalBox,titleLabel,titleBox,bodyLabel,bodyBox,save});dialog.ShowDialog(popup);
        }
    }
    class Reminder:Form{
        readonly Timer pulse=new Timer();readonly Button button=new Button();readonly Label heading=new Label();
        readonly TextBox message=new TextBox{Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.Vertical,BackColor=Color.FromArgb(255,249,236)};
        readonly Image artwork;bool exercising=false,bright=false;string customTitle,customBody;public string TimerValue="30:00";bool isDue=false;
        public Reminder(Action toggle,string title,string body,Action exit,Action settingsAction){
            customTitle=title;customBody=body;Text="动一下 · 西瓜提醒";ClientSize=new Size(320,450);
            FormBorderStyle=FormBorderStyle.None;TopMost=true;ShowInTaskbar=true;DoubleBuffered=true;
            BackColor=Color.FromArgb(255,0,255);TransparencyKey=BackColor;Font=new Font("Microsoft YaHei UI",10);AutoScaleMode=AutoScaleMode.None;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("melon.png"))using(var source=Image.FromStream(stream))artwork=new Bitmap(source);
            heading.SetBounds(29,322,262,28);heading.Font=new Font(Font.FontFamily,12,FontStyle.Bold);heading.TextAlign=ContentAlignment.MiddleCenter;heading.BackColor=Color.FromArgb(255,249,236);heading.ForeColor=Ink;Controls.Add(heading);
            message.SetBounds(29,353,262,34);message.ForeColor=Ink;Controls.Add(message);
            button.SetBounds(29,395,262,38);Style(button);button.BackColor=Color.FromArgb(48,123,76);button.Font=new Font(Font.FontFamily,10,FontStyle.Bold);button.Click+=(s,e)=>toggle();Controls.Add(button);
            var pin=new CheckBox{Text="固定",Checked=true,Appearance=Appearance.Button,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(255,249,236)};pin.FlatStyle=FlatStyle.Flat;pin.FlatAppearance.BorderSize=0;pin.SetBounds(24,12,64,28);pin.CheckedChanged+=(s,e)=>TopMost=pin.Checked;Controls.Add(pin);
            var close=new Button{Text="×",BackColor=Color.FromArgb(255,249,236),FlatStyle=FlatStyle.Flat};close.FlatAppearance.BorderSize=0;close.SetBounds(264,12,32,28);close.AccessibleName="关闭并退出提醒器";close.Click+=(s,e)=>exit();Controls.Add(close);
            var settingsButton=new Button{Text="设置",FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(255,249,236)};settingsButton.FlatAppearance.BorderSize=0;settingsButton.SetBounds(190,12,64,28);settingsButton.Click+=(s,e)=>settingsAction();Controls.Add(settingsButton);
            Shown+=(s,e)=>Position();pulse.Interval=850;pulse.Tick+=(s,e)=>{bright=!bright;Invalidate();};FormClosed+=(s,e)=>{pulse.Dispose();artwork.Dispose();};RefreshState(false);
            MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}};
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
        void Position(){var area=Screen.FromPoint(Cursor.Position).WorkingArea;Location=new Point(area.Right-Width-12,area.Bottom-Height-12);}
        public void UpdateTimer(string value,bool due,bool active){TimerValue=value;isDue=due;heading.Text=active?"享受活动的时间":due?customTitle:"距离下次活动";if(due&&!active)pulse.Start();else pulse.Stop();Invalidate();}
        public void SetContent(string title,string body){customTitle=title;customBody=body;RefreshState(exercising);}
        public void RefreshState(bool active){exercising=active;heading.Text=active?"享受活动的时间":customTitle;message.Text=active?"提醒已暂停，运动结束后再开始。":customBody;message.Select(0,0);button.Text=active?"运动结束，重新计时":"暂停提醒，去运动";if(active)pulse.Stop();else pulse.Start();Invalidate();}
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            using(var toolbar=new SolidBrush(Color.FromArgb(255,249,236)))g.FillRoundedRectangle(toolbar,new Rectangle(17,4,286,40),12);g.DrawImage(artwork,new Rectangle(20,44,280,280));
            using(var brush=new SolidBrush(Color.FromArgb(255,249,236)))g.FillRoundedRectangle(brush,new Rectangle(17,313,286,127),18);
            using(var pen=new Pen(isDue&&!exercising&&bright?Color.FromArgb(88,170,101):Color.FromArgb(221,230,202),3))g.DrawRoundedRectangle(pen,new Rectangle(17,313,286,127),18);
            using(var font=new Font("Segoe UI",27,FontStyle.Bold))TextRenderer.DrawText(g,TimerValue,font,new Rectangle(55,222,210,50),Color.FromArgb(74,42,40),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
        }
    }
}
static class RoundedDrawing{
    static GraphicsPath Path(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    public static void FillRoundedRectangle(this Graphics g,Brush b,Rectangle r,int radius){using(var p=Path(r,radius))g.FillPath(b,p);}
    public static void DrawRoundedRectangle(this Graphics g,Pen b,Rectangle r,int radius){using(var p=Path(r,radius))g.DrawPath(b,p);}
}




