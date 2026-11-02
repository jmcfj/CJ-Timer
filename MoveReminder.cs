using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO;

class MoveReminder : Form {
    static readonly Color Blue=Color.FromArgb(18,99,237), Ink=Color.FromArgb(20,41,72);
    readonly Timer timer=new Timer(); readonly NumericUpDown minutes=new NumericUpDown();
    readonly Label status=new Label(); readonly Button action=new Button(); readonly Ring ring=new Ring();
    readonly NotifyIcon tray=new NotifyIcon(); Reminder popup; DateTime deadline; bool exercise=false,due=false,quitting=false; int interval=30;
    readonly string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MoveReminder","interval.txt");
    [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(Array.IndexOf(args,"--check")>=0){using(var f=new MoveReminder()){f.CreateControl();}return;}Application.Run(new MoveReminder());}
    public MoveReminder(){
        Text="动一下 · MOVE"; ClientSize=new Size(480,650); MinimumSize=MaximumSize=Size; MaximizeBox=false; BackColor=Color.White; ForeColor=Ink; Font=new Font("Microsoft YaHei UI",11,FontStyle.Regular); StartPosition=FormStartPosition.CenterScreen; AutoScaleMode=AutoScaleMode.Dpi;
        try {int n;if(int.TryParse(File.ReadAllText(settings),out n)&&n>=1&&n<=240)interval=n;}catch{}
        var title=LabelAt("动一下 · MOVE",26,22,420,35,20,true); title.ForeColor=Blue;
        LabelAt("专注，也别忘了动一下。",26,70,430,38,19,true);
        status.SetBounds(26,116,430,32);Controls.Add(status);
        ring.SetBounds(118,170,244,244);Controls.Add(ring);
        action.SetBounds(26,447,428,58); Style(action);action.Click+=(s,e)=>ToggleExercise();Controls.Add(action);
        LabelAt("提醒间隔",26,533,100,34,11,false);minutes.SetBounds(130,533,72,34);minutes.Minimum=1;minutes.Maximum=240;minutes.Value=interval;Controls.Add(minutes);LabelAt("分钟",210,533,50,34,11,false);
        var apply=new Button{Text="应用并重新计时"};apply.SetBounds(270,530,184,40);apply.Click+=(s,e)=>{interval=(int)minutes.Value;try{Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllText(settings,interval.ToString());}catch{}Restart();};Controls.Add(apply);
        var preview=new Button{Text="预览到点提醒"};preview.SetBounds(26,590,180,36);preview.Click+=(s,e)=>BecomeDue();Controls.Add(preview);
        LabelAt("关闭窗口后仍在托盘运行",218,592,240,30,10,false);
        var menu=new ContextMenuStrip();menu.Items.Add("打开主界面",null,(s,e)=>ShowMain());menu.Items.Add("退出提醒器",null,(s,e)=>{quitting=true;Close();});tray.Icon=SystemIcons.Information;tray.Text="动一下 · MOVE";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=(s,e)=>ShowMain();
        timer.Interval=500;timer.Tick+=(s,e)=>{if(!exercise&&!due&&DateTime.UtcNow>=deadline)BecomeDue();UpdateUI();};Restart();timer.Start();
        FormClosing+=(s,e)=>{if(!quitting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}else{timer.Stop();if(popup!=null)popup.Close();tray.Dispose();}};
    }
    Label LabelAt(string text,int x,int y,int w,int h,int size,bool bold){var l=new Label{Text=text,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),TextAlign=ContentAlignment.MiddleLeft};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
    internal static void Style(Button b){b.BackColor=Blue;b.ForeColor=Color.White;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.Font=new Font("Microsoft YaHei UI",12,FontStyle.Bold);}
    void ShowMain(){Show();WindowState=FormWindowState.Normal;Activate();}
    void Restart(){exercise=false;due=false;deadline=DateTime.UtcNow.AddMinutes(interval);if(popup!=null){popup.Close();popup=null;}UpdateUI();}
    void ToggleExercise(){if(exercise){Restart();return;}exercise=true;due=false;UpdateUI();if(popup!=null)popup.RefreshState(true);}
    void BecomeDue(){if(exercise)return;due=true;if(popup==null||popup.IsDisposed){popup=new Reminder(ToggleExercise);popup.Show();}else popup.Show();popup.RefreshState(false);UpdateUI();}
    void UpdateUI(){var remaining=Math.Max(0,(int)Math.Ceiling((deadline-DateTime.UtcNow).TotalSeconds));ring.Caption=exercise?"保持活力":"距离下次活动";ring.Value=exercise?"运动中":due?"00:00":string.Format("{0:00}:{1:00}",remaining/60,remaining%60);ring.Invalidate();status.Text=exercise?"提醒已暂停 · 等待运动结束":due?"时间到 · 请起来活动":"正在计时 · 到点提醒你起身活动";action.Text=exercise?"运动结束，重新计时":"暂停提醒，去运动";}
    class Ring:Control{public string Caption="",Value="";public Ring(){DoubleBuffered=true;}protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;e.Graphics.ScaleTransform(Width/244f,Height/244f);using(var p=new Pen(Color.FromArgb(232,240,255),10))e.Graphics.DrawEllipse(p,8,8,228,228);using(var p=new Pen(Blue,10))e.Graphics.DrawArc(p,8,8,228,228,-130,290);Draw(e,Caption,14,60,FontStyle.Regular);Draw(e,Value,31,100,FontStyle.Bold);Draw(e,"起来走走，状态更好",12,165,FontStyle.Regular);}void Draw(PaintEventArgs e,string t,float size,int y,FontStyle style){using(var f=new Font("Microsoft YaHei UI",size,style))using(var brush=new SolidBrush(Ink))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})e.Graphics.DrawString(t,f,brush,new RectangleF(12,y,220,45),format);}}
    class Reminder:Form{
        readonly Timer pulse=new Timer(); readonly Button button=new Button(); readonly Label heading=new Label(),message=new Label();bool exercising=false,bright=false;
        public Reminder(Action toggle){Text="动一下 · 久坐提醒";ClientSize=new Size(390,285);FormBorderStyle=FormBorderStyle.FixedToolWindow;MaximizeBox=false;MinimizeBox=false;ControlBox=false;TopMost=true;ShowInTaskbar=false;BackColor=Color.White;Font=new Font("Microsoft YaHei UI",11);AutoScaleMode=AutoScaleMode.Dpi;
            heading.SetBounds(25,28,340,45);heading.Font=new Font(Font.FontFamily,22,FontStyle.Bold);heading.ForeColor=Ink;Controls.Add(heading);message.SetBounds(25,95,340,75);message.ForeColor=Ink;Controls.Add(message);button.SetBounds(25,190,340,62);Style(button);button.Click+=(s,e)=>toggle();Controls.Add(button);
            Shown+=(s,e)=>Position();SystemEventsHook();pulse.Interval=850;pulse.Tick+=(s,e)=>{bright=!bright;Invalidate();};pulse.Start();FormClosed+=(s,e)=>pulse.Dispose();RefreshState(false);
        }
        void SystemEventsHook(){LocationChanged+=(s,e)=>{};}
        void Position(){var area=Screen.FromPoint(Cursor.Position).WorkingArea;Location=new Point(area.Right-Width-16,area.Bottom-Height-16);}
        public void RefreshState(bool active){exercising=active;heading.Text=active?"享受活动的时间":"该起来活动啦！";message.Text=active?"计时和闪动已暂停。\n运动结束后，点击下方按钮开启新一轮。":"不要久坐不动，站起来走走、伸展一下。\n下一段专注，从活动身体开始。";button.Text=active?"运动结束，重新计时":"暂停提醒，去运动";if(active)pulse.Stop();else pulse.Start();Invalidate();}
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var pen=new Pen(!exercising&&bright?Blue:Color.FromArgb(202,222,255),5))e.Graphics.DrawRectangle(pen,2,2,ClientSize.Width-5,ClientSize.Height-5);}
    }
}


