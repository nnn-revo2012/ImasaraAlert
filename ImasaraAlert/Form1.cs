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
        private SortableBindingList<User> lists_u = new SortableBindingList<User>();
        //private SortableBindingList<Prog> lists_p = new SortableBindingList<Prog>();

        private NicoLiveNet _nLiveNet = null;         //WebClient
        private SoundPlayer _player = null;

        private System.Windows.Forms.Timer _readTimer = null;
        private DateTime _readTimer_dt = DateTime.MinValue;

        private readonly object lockObject = new object();  //情報表示用
        //private readonly object lockObject2 = new object(); //実行ファイルのログ用
        private string dbfilecomm;
        private string dbfileuser;

        private string LogFile;
        private string LogFile2;


        public Form1()
        {
            InitializeComponent();
            this.Text = Ver.GetFullVersion();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                //設定データーの読み込み
                props = new Props();
                props.LoadData();
                if (!Directory.Exists(Props.GetSettingDirectory()))
                    props.SaveData();

                Popup.InitPopi();

                //最小化して起動する
                if (props.IsMinimization)
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                }

                LogFile = Props.GetLogfile(Props.GetSettingDirectory(), "imasaraalert");
                dbfilecomm = Path.Combine(Props.GetApplicationDirectory(), Props.CommDb);
                dbfileuser = Path.Combine(Props.GetApplicationDirectory(), Props.UserDb);

                //データー読み込み
                ReadAllData();

                _nLiveNet = new NicoLiveNet();
                //アラートに接続
                StartAlert();
            }
            catch (Exception Ex)
            {
                AddLog("アラート接続エラー\r\n" + Ex.Message, 2);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                EndAlert();

                //MessageBox.Show("データーを保存します。");
                if (lists_c.Count > 0)
                    SaveCommData(dbfilecomm, (IList<Comm>)lists_c);
                if (lists_u.Count > 0)
                    SaveCommData(dbfileuser, (IList<User>)lists_u);

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
            comm.Clear();
            comm.ComId = ttt;
            comm.Regist_Date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
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
                var ttt = (string)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value;
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

        private void この行を削除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value;
                var ttt2 = lists_si.FirstOrDefault(x => x.LiveId == ttt);
                lists_si.Remove(ttt2);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void 最近行われた放送のURLを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[13].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetLiveUrl(Comm.GetLiveID(ttt));
                    OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    //Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void ユーザーURLを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[2].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetUserUrl(ttt);
                    OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    //Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void 最近行われた放送のURLをコピーToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[13].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetLiveUrl(Comm.GetLiveID(ttt));
                    //OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

        }

        private void ユーザーURLをコピーToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var ttt = (string)dataGridView2.Rows[dataGridView2.CurrentCell.RowIndex].Cells[2].Value;
                if (!string.IsNullOrEmpty(ttt))
                {
                    ttt = Props.GetUserUrl(ttt);
                    //OpenProcess.OpenWeb(ttt, props.BrowserPath, props.IsDefaultBrowser);
                    Clipboard.SetText(ttt);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        private void コミュ一覧favoritecominiを読み込むToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog1 = new System.Windows.Forms.OpenFileDialog())
                {
                    openFileDialog1.FileName = Props.FavoriteCom;
                    openFileDialog1.InitialDirectory = @"C:\";
                    openFileDialog1.Filter = "favoritecom.ini|favoritecom.ini|すべてのファイル(*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "namarokuの " + Props.FavoriteCom + " を選択してください";
                    //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
                    openFileDialog1.RestoreDirectory = true;

                    if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                    {
                        lists_c = new SortableBindingList<Comm>(ConvertData<Comm>(openFileDialog1.FileName));
                        dataGridView2.DataSource = lists_c;
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void ユーザー一覧favoriteuseriniを読み込むToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog1 = new System.Windows.Forms.OpenFileDialog())
                {
                    openFileDialog1.FileName = Props.FavoriteUser;
                    openFileDialog1.InitialDirectory = @"C:\";
                    openFileDialog1.Filter = "favoriteuser.ini|favoriteuser.ini|すべてのファイル(*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "namarokuの " + Props.FavoriteUser + " を選択してください";
                    //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
                    openFileDialog1.RestoreDirectory = true;

                    if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                    {
                        lists_u = new SortableBindingList<User>(ConvertData<User>(openFileDialog1.FileName));
                        dataGridView3.DataSource = lists_u;
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (lists_c.Count > 0)
                SaveCommData(dbfilecomm, (IList<Comm>)lists_c);
            if (lists_u.Count > 0)
                SaveCommData(dbfileuser, (IList<User>)lists_u);
            MessageBox.Show("データーを保存しました。");

        }
    }

}

