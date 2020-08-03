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
                    else if (num == 2) //エラー
                    {
                        MessageBox.Show(s + "\r\n",
                            "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (num == 3) //注意
                    {
                        MessageBox.Show(s + "\r\n",
                            "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (props.IsLogging && LogFile != null)
                        System.IO.File.AppendAllText(LogFile, System.DateTime.Now.ToString("HH:mm:ss ") + s + "\r\n");
                }
            }));
        }

        private void ReadAllData()
        {
            try
            {
                //データーをファイルから読み込み
                lists_c = new SortableBindingList<Comm>(ReadCommData<Comm>(dbfilecomm));
                lists_u = new SortableBindingList<User>(ReadCommData<User>(dbfileuser));

                //BindingList<T>型データをDataGridViewに格納
                dataGridView2.DataSource = lists_c;
                dataGridView3.DataSource = lists_u;

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
            AddLog("データー読み込み開始", 1);
            //タイマーセット
            _readTimer = new Timer();
            _readTimer.Tick += new EventHandler(rssTimer_Tick);
            _readTimer.Interval = 1000;
            _readTimer_dt = DateTime.Now;
            _readTimer.Enabled = true;
            Debug.WriteLine("_readTimer Start");
        }

        private async void rssTimer_Tick(object sender, EventArgs e)
        {
            _readTimer.Enabled = false;
            var now = DateTime.Now;
            if (now >= _readTimer_dt)
            {
                _readTimer_dt = now.AddSeconds(60);
                Debug.WriteLine("Read NextDate: " + _readTimer_dt.ToString());
                await ReadAlert(now);
            }
            _readTimer.Enabled = true;
        }

        private async Task ReadAlert(DateTime now)
        {
            try
            {
                for (var i = 0; i < Props.Cates.Count(); i++)
                {
                    Debug.WriteLine("Cate: " + Props.Cates[i]);
                    Debug.WriteLine("LastTime: " + now.AddMinutes(-5).ToString());
                    //var lgsi = await _nLiveNet.ReadRssAsync(Props.NicoRssUrl, Props.Cates[i], now);
                    var lgsi = await _nLiveNet.ReadCateApiAsync(Props.NicoCateApi, Props.Cates[i], now);
                    Debug.WriteLine("lgsi: " + lgsi.Count().ToString());
                    foreach (var gsi in lgsi)
                    {
                        DispStreamInfo(gsi);
                        var f_idx = lists_c.ToList().FindIndex(x => x.ComId == gsi.Community_Id);
                        if (f_idx > -1 && (Comm.GetLiveID(lists_c[f_idx].Col14) != gsi.LiveId))
                        {
                            this.Invoke(new Action(async () => await work2(gsi, f_idx)));
                        }
                        //f_idx = lists_u.ToList().FindIndex(x => x.Id == gsi.Provider_Id);
                        //if (f_idx > -1) work2(gsi, f_idx);
                        //f_idx = lists_l.ToList().FindIndex(x => x.Id == gsi.Live_Id);
                        //if (f_idx > -1) work2(gsi, f_idx);
                        //DEBUG
                        //Debug.WriteLine(gsi.Community_Thumbnail);
                        //gsi.Col02 = await _nLiveNet.CreateImageAsync(gsi.Community_Thumbnail);
                        //this.Invoke(new Action(() => lists_si.Add(gsi)));
                        //await Task.Delay(1000);
                        //DEBUG
                    }
                    await Task.Delay(500);
                }
            }
            catch (OperationCanceledException Ex)
            {
                Debug.WriteLine("CANCELED");
                return;
            }
            catch (Exception Ex)
            {
                AddLog("RSS読み込みエラー\r\n" + Ex.Message, 2);
                return;
            }
        }

        private void CancelAlert()
        {
            try
            {
            }
            catch (Exception Ex)
            {
                DebugWrite.Writeln(nameof(CancelAlert), Ex);
            }
        }

        private void EndAlert()
        {
            try
            {
                _readTimer?.Dispose();
                _readTimer = null;
            }
            catch (Exception Ex)
            {
                DebugWrite.Writeln(nameof(EndAlert), Ex);
            }
        }


        private async Task work2(GetStreamInfo gsi, int f_idx)
        {
            try
            {
                //var gsi2 = await _nLiveNet.GetStreamInfo2Async(gsi.LiveId, gsi.Provider_Id);
                lists_c[f_idx].Last_Date = gsi.Col12.ToString("yyyy/MM/dd HH:mm:ss");
                lists_c[f_idx].Col14 = Comm.GetLiveNumber(gsi.LiveId);
                gsi.Col02 = await _nLiveNet.CreateImageAsync(gsi.Community_Thumbnail);
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
                DebugWrite.Writeln(nameof(GetStreamInfo), Ex);
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
                DebugWrite.Writeln(nameof(SoundProc), Ex);
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
                DebugWrite.Writeln(nameof(StopSound), Ex);
            }
        }

        private void DispStreamInfo(GetStreamInfo gsi)
        {
            var ttt = string.Format(DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss]") + "  放送ID：{0}  コミュニティID：{1}  ユーザーID：{2}",
                gsi.LiveId, gsi.Community_Id, gsi.Provider_Id);
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
                DebugWrite.Writeln(nameof(ReadCommData), Ex);
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
                DebugWrite.Writeln(nameof(SaveCommData), Ex);
            }

            return result;
        }



        //favaritecom.ini / favariteuser.iniを変換する
        private List<T> ConvertData<T>(string r_file) where T : IAlertData, new()
        {
            var enc = new System.Text.UTF8Encoding(false);
            var lists = new List<T>();

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
                        var data = new T();
                        data.Ng = line;
                        data.ComId = sr.ReadLine();
                        data.UserId = sr.ReadLine();
                        data.Col04 = sr.ReadLine();
                        data.ComName = sr.ReadLine(); //5
                        data.UserName = sr.ReadLine();
                        data.Group = sr.ReadLine();
                        data.Last_Date = sr.ReadLine();
                        data.Col09 = sr.ReadLine(); //9
                        data.Col10 = sr.ReadLine(); //10
                        data.Col11 = sr.ReadLine();
                        data.Col12 = sr.ReadLine();
                        data.Col13 = sr.ReadLine();
                        data.Col14 = sr.ReadLine();
                        data.Col15 = sr.ReadLine();
                        data.Regist_Date = sr.ReadLine(); //16
                        data.Col17 = sr.ReadLine();
                        data.Pop = sr.ReadLine().Equals("true");
                        data.Ballon = sr.ReadLine().Equals("true"); //バルーン
                        data.Web = sr.ReadLine().Equals("true");
                        data.Mail = sr.ReadLine().Equals("true"); //メール
                        data.Sound = sr.ReadLine().Equals("true"); //22
                        data.Col23 = sr.ReadLine().Equals("true"); //23
                        data.App = sr.ReadLine().Equals("true");
                        data.App_a = sr.ReadLine().Equals("true");
                        data.App_b = sr.ReadLine().Equals("true");
                        data.App_c = sr.ReadLine().Equals("true");
                        data.App_d = sr.ReadLine().Equals("true");
                        data.Memo = sr.ReadLine(); //メモ
                        lists.Add(data);
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
            comm.Regist_Date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            comm.Pop = true;
            comm.App_a = true;
            lists_c.Add(comm);

            comm = new Comm();
            comm.ComId = "co3313757";
            comm.ComName = "七原くんは死にました。";
            comm.Regist_Date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            comm.Pop = true;
            comm.Sound = true;
            lists_c.Add(comm);

        }

    }
}
