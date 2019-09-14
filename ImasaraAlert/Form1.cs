using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Media;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ImasaraAlert.Net;
using ImasaraAlert.Prop;
using ImasaraAlert.List;
using ImasaraAlert.Data;
using ImasaraAlert.Proc;

namespace ImasaraAlert
{

    public partial class Form1 : Form
    {

        public static Props props; //設定

        private SortableBindingList<GetStreamInfo> lists_si = new SortableBindingList<GetStreamInfo>();
        private SortableBindingList<Comm> lists_c = new SortableBindingList<Comm>();
        //private SortableBindingList<User> lists_u = new SortableBindingList<User>();
        //private SortableBindingList<Prog> lists_u = new SortableBindingList<Prog>();

        private NicoLiveNet _nLiveNet = null;         //WebClient
        private NicoRss _nRss = null;                 //RSS
        private volatile int _rss_status = 0;
        private SoundPlayer _player = null;

        private readonly object lockObject = new object();  //情報表示用
        //private readonly object lockObject2 = new object(); //実行ファイルのログ用
        private string LogFile;
        private string LogFile2;

        private string dbfile = "comm.json";
        private string convfile = @"D:\home\bin\namaroku\favoritecom.ini";

        public Form1()
        {

            try
            {
                InitializeComponent();

                //設定データーの読み込み
                props = new Props();
                props.LoadData();
                if (!Directory.Exists(Props.GetSettingDirectory()))
                    props.SaveData();

                Popup.InitPopi();

                //最小化して起動する
                if (props.IsMinimization)
                {
                    //this.WindowState = FormWindowState.Minimized;
                    //this.ShowInTaskbar = false;
                }

                LogFile = Props.GetLogfile(Props.GetSettingDirectory(), "imasaraalert");
                //dbfile = Path.Combine(Props.GetSettingDirectory(), dbfile);
                //DEBUG
                dbfile = Path.Combine(Props.GetApplicationDirectory(), dbfile);

                //データー読み込み
                ReadAllData();

                _nLiveNet = new NicoLiveNet(null);

                //アラートに接続
                Task.Run(() => StartAlert());

                Debug.WriteLine("Form1 END");

            }
            catch (Exception Ex)
            {
                AddLog("Form1: \r\n"+Ex.Message, 2);
            }

        }

        public void AddLog(string s, int num)
        {
            this.Invoke(new Action(() =>
            {
                lock (lockObject)
                {
                    if (num == 1)
                    {
                        toolStripStatusLabel1.Text = s;
                    }
                    else if (num == 2)
                    {
                        MessageBox.Show(s + "\r\n");
                    }
                    else if (num == 3)
                    {
                        MessageBox.Show(s + "\r\n");
                    }
                    if (props.IsLogging)
                        System.IO.File.AppendAllText(LogFile, System.DateTime.Now.ToString("HH:mm:ss ") + s + "\r\n");
                }
            }));
        }

        private void ReadAllData()
        {
            try
            {
                //データーをファイルから読み込み
                //DebugDataRead();
                lists_c = new SortableBindingList<Comm>(ReadCommData<Comm>(dbfile));
                //ists_u = new SortableBindingList<Comm>(ReadCommData<User>(dbfile));

                //BindingList<T>型データをDataGridViewに格納
                dataGridView2.DataSource = lists_c;

                //BindingList<T>型データをDataGridViewに格納
                dataGridView1.DataSource = lists_si;

            }
            catch (Exception Ex)
            {
                AddLog("データー読み込みエラー\r\n" + Ex.Message, 2);
            }

        }

        private void StartAlert()
        {
            _nRss = new NicoRss(this, _nLiveNet);
            //タイマーセット
            //タイマー開始
            //タイマー中に時間になれば ReadAlert() を呼ぶ
            ReadAlert();
        }

        private async void ReadAlert()
        {
            try
            {
                AddLog("RSS読み込み開始", 1);

                _rss_status = 2;
                while (_rss_status == 2)
                {
                    var lgsi = await _nLiveNet.ReadRssAsync(Props.NicoRssUrl);
                    if (_rss_status != 2) break;

                    foreach (var gsi in lgsi)
                    {
                        DispStreamInfo(gsi);
                        var f_idx = lists_c.ToList().FindIndex(x => x.ComId == gsi.Community_Id);
                        if (f_idx > -1)
                        {
                            this.Invoke(new Action(() => work2(gsi, f_idx)));
                        }
                        //f_idx = lists_u.ToList().FindIndex(x => x.Id == gsi.Provider_Id);
                        //if (f_idx > -1) work2(gsi, f_idx);
                        //f_idx = lists_l.ToList().FindIndex(x => x.Id == gsi.Live_Id);
                        //if (f_idx > -1) work2(gsi, f_idx);
                        //DEBUG
                        //this.Invoke(new Action(() => lists_si.Add(gsi)));
                        //DEBUG
                    }
                    _rss_status = 4;
                }
                _rss_status = 4;
                _nRss?.Dispose();
                Debug.WriteLine("StartAlert END");
            }
            catch (OperationCanceledException Ex)
            {
                Debug.WriteLine("CANCELED");
            }
            catch (Exception Ex)
            {
                AddLog("RSS読み込みエラー\r\n" + Ex.Message, 2);
                _rss_status = 4;
                _nRss?.Dispose();
                _nRss = null;
            }
        }

        private void CancelAlert()
        {
            try
            {
                if (_rss_status == 2)
                {
                    _rss_status = 4;
                    _nRss?.Dispose();
                    _nRss = null;
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void EndAlert()
        {
            try
            {
                _nRss?.Dispose();
                _nRss = null;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }


        private async void work2(GetStreamInfo gsi, int f_idx)
        {
            try
            {
                //var gsi2 = await _nLiveNet.GetStreamInfo2Async(gsi.LiveId, gsi.Provider_Id);
                lists_c[f_idx].Last_Date = gsi.Start_Time;
                lists_si.Add(gsi);
                var liveid = Props.GetLiveUrl(gsi.LiveId);
                if (lists_c[f_idx].Pop) PopupProc(gsi);
                if (lists_c[f_idx].Web) OpenProcess.OpenWeb(liveid, props.BrowserPath, props.IsDefaultBrowser);
                if (lists_c[f_idx].Sound) SoundProc();
                if (lists_c[f_idx].App_a) OpenProcess.OpenProgram(liveid, props.AppA_Path);
                if (lists_c[f_idx].App_b) OpenProcess.OpenProgram(liveid, props.AppB_Path);
                if (lists_c[f_idx].App_c) OpenProcess.OpenProgram(liveid, props.AppC_Path);
                if (lists_c[f_idx].App_d) OpenProcess.OpenProgram(liveid, props.AppD_Path);
            }
            catch (Exception Ex)
            {
                AddLog("work2: " + Ex.Message, 2);
            }
        }

        private void PopupProc(GetStreamInfo gsi)
        {
            var fpop = new Popup(this, gsi);
            try
            {
                int i = fpop.GetPopiNum();
                fpop.SetPosition("RD");     //表示する座標を決定
                fpop.Show();
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void SoundProc()
        {
            try
            {
                if (_player != null) StopSound();

                _player = new SoundPlayer(props.GetSoundFile());
                _player.Play();
            }
            catch (Exception Ex)
            {
            }
        }

        private void StopSound()
        {
            try
            {
                if (_player != null)
                {
                    _player.Stop();
                    _player.Dispose();
                    _player = null;
                }

            }
            catch (Exception Ex)
            {
            }
        }

        private void DispStreamInfo(GetStreamInfo gsi)
        {
            var ttt = string.Format(DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss]") + "  放送ID：{0}  コミュニティID：{1}  ユーザー：{2}",
                gsi.LiveId, gsi.Community_Id, gsi.Provider_Name);
            AddLog(ttt, 1);
        }

        private GetStreamInfo GetStreamData(string data)
        {
            var gsi = new GetStreamInfo();
            gsi.LiveId = string.Empty;
            gsi.Community_Id = string.Empty;
            gsi.Provider_Id = string.Empty;

            if (!string.IsNullOrEmpty(data))
            {
                if (data.IndexOf(',') > 0)
                {
                    var ttt = data.Split(',');
                    gsi.LiveId = "lv" + ttt[0];
                    gsi.Community_Id = ttt[1];
                    gsi.Provider_Id = ttt[2];
                }
                else
                {
                    gsi.LiveId = data;
                }
            }

            return gsi;
        }

        //データー読込
        private List<T> ReadCommData<T>(string r_file)
        {
            var enc = new System.Text.UTF8Encoding(false);
            var lists = new List<T>();

            try
            {
                using (var sr = new StreamReader(r_file, enc))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var ttt = JsonConvert.DeserializeObject<T>(line);
                        lists.Add(ttt);
                    }
                }
            }
            catch (Exception Ex)
            {
            }

            return lists;
        }

        //データー出力
        private bool SaveCommData<T>(string w_file, IList<T> lists)
        {
            var enc = new System.Text.UTF8Encoding(false);
            var result = false;

            try
            {
                using (var sw = new StreamWriter(w_file, false, enc))
                {

                    foreach (var li in lists)
                    {
                        string ttt = JsonConvert.SerializeObject(li);
                        sw.WriteLine(ttt);
                    }
                }
                result = true;
            }
            catch (Exception Ex)
            {
            }

            return result;
        }

        //favaritecomm.iniを変換する
        private List<Comm> ConvertCommData(string r_file)
        {
            var enc = new System.Text.UTF8Encoding(false);
            var lists = new List<Comm>();

            try
            {
                using (var sr = new StreamReader(r_file, enc))
                {
                    string line;
                    line = sr.ReadLine(); //
                    while (true) // 1行ずつ読み出し
                    {
                        line = sr.ReadLine();
                        if (line == "namarokuEndLine") break;
                        var comm = new Comm();
                        comm.Ng = line;
                        comm.ComId = sr.ReadLine();
                        comm.UserId = sr.ReadLine();
                        comm.Col04 = sr.ReadLine();
                        comm.ComName = sr.ReadLine(); //5
                        comm.UserName = sr.ReadLine();
                        comm.Group = sr.ReadLine();
                        line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                            comm.Last_Date = DateTime.Parse(line);
                        line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                            comm.Col09 = DateTime.Parse(line);
                        comm.Col10 = sr.ReadLine(); //10
                        comm.Col11 = sr.ReadLine();
                        comm.Col12 = sr.ReadLine();
                        comm.Col13 = sr.ReadLine();
                        comm.Col14 = sr.ReadLine();
                        comm.Col15 = sr.ReadLine();
                        line = sr.ReadLine(); //16
                        if (!string.IsNullOrEmpty(line))
                            comm.Regist_Date = DateTime.Parse(line);
                        comm.Col17 = sr.ReadLine();
                        comm.Pop = sr.ReadLine().Equals("true");
                        comm.Ballon = sr.ReadLine().Equals("true"); //バルーン
                        comm.Web = sr.ReadLine().Equals("true");
                        comm.Mail = sr.ReadLine().Equals("true"); //メール
                        comm.Sound = sr.ReadLine().Equals("true"); //22
                        comm.Col23 = sr.ReadLine().Equals("true"); //23
                        comm.App = sr.ReadLine().Equals("true");
                        comm.App_a = sr.ReadLine().Equals("true");
                        comm.App_b = sr.ReadLine().Equals("true");
                        comm.App_c = sr.ReadLine().Equals("true");
                        comm.App_d = sr.ReadLine().Equals("true");
                        comm.Memo = sr.ReadLine(); //メモ
                        lists.Add(comm);
                    }
                }
            }
            catch (Exception Ex)
            {
                AddLog("データー変換エラー\r\n" + Ex.Message, 2);
            }

            return lists;
        }

        //テストデーター作成
        private void DebugDataRead()
        {

            var comm = new Comm();
            comm.ComId = "co10000";
            comm.ComName = "ちくわちゃん";
            comm.Regist_Date = DateTime.Now;
            comm.Pop = true;
            comm.App_a = true;
            lists_c.Add(comm);

            comm = new Comm();
            comm.ComId = "co3313757";
            comm.ComName = "七原くんは死にました。";
            comm.Regist_Date = DateTime.Now;
            comm.Pop = true;
            comm.Sound = true;
            lists_c.Add(comm);

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_rss_status == 2)
                {
                    CancelAlert();
                }
                EndAlert();

                //MessageBox.Show("データーを保存します。");
                if (lists_c.Count > 0)
                    SaveCommData(dbfile, (IList<Comm>)lists_c);
                //if (lists_u.Count > 0)
                //    SaveCommData(dbfile, (IList<User>)lists_u);

                _nLiveNet?.Dispose();
            }
            catch (Exception Ex)
            {
                AddLog("終了時エラー\r\n" + Ex.Message, 2);
            }
        }

        //コミュ登録
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text)) return;

            var ttt = Comm.GetChNo(textBox1.Text);
            if (string.IsNullOrEmpty(ttt))
            {
                AddLog("正しいコミュを登録してください。", 3);
                return; ;
            }
            if (lists_c.Count(x => x.ComId == ttt) > 0)
            {
                AddLog("そのコミュは登録済です。", 3);
                return; ;
            }
            //コミュの存在チェック、コミュ名をゲット

            //コミュを登録
            var comm = new Comm();
            comm.ComId = ttt;
            comm.Regist_Date = DateTime.Now;
            lists_c.Add(comm);

        }

        private void 終了XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 設定フォルダーを開くToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var setfolder = Props.GetSettingDirectory();
            if (Directory.Exists(setfolder))
            {
                Process.Start(setfolder);
            }
        }

        private void 設定SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fo2 = new Form2(this))
            {
                fo2.ShowDialog();

            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(lists_c[0].Pop.ToString());

        }

        private void dataGridView1_RowContextMenuStripNeeded(object sender, DataGridViewRowContextMenuStripNeededEventArgs e)
        {
            try
            {
                DataGridView dgv = (DataGridView)sender;

                dgv.ClearSelection();
                dgv.Rows[e.RowIndex].Selected = true;
                e.ContextMenuStrip = this.contextMenuStrip1;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void dataGridView2_RowContextMenuStripNeeded(object sender, DataGridViewRowContextMenuStripNeededEventArgs e)
        {
            try
            {
                DataGridView dgv = (DataGridView)sender;

                dgv.ClearSelection();
                dgv.Rows[e.RowIndex].Selected = true;
                e.ContextMenuStrip = this.contextMenuStrip2;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void コミュURLを開くToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[1].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetCommUrl(ttt);
                    OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    //Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void コミュURLをコピーToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[1].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetCommUrl(ttt);
                    //OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void この行を削除ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[1].Value;
                var ttt2 = lists_c.FirstOrDefault(x => x.ComId == ttt);
                lists_c.Remove(ttt2);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void 放送URLを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetLiveUrl(ttt);
                    OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    //Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void コミュURLを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[8].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetCommUrl(ttt);
                    OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    //Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }


        }

        private void 放送URLをコピーToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void コミュURLをコピーToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void この行を削除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value;
                var ttt2 = lists_si.FirstOrDefault(x => x.LiveId == ttt);
                lists_si.Remove(ttt2);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void namarokuのファイルを読み込むToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lists_c = new SortableBindingList<Comm>(ConvertCommData(convfile));
            dataGridView2.DataSource = lists_c;
        }
    }

}

