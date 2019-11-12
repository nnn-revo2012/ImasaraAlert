using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using ImasaraAlert.Net;
using ImasaraAlert.Prop;
using ImasaraAlert.Proc;
using ImasaraAlert.Data;

namespace ImasaraAlert
{
    public partial class Popup : Form
    {

        private const int POPI_NUM = 50;
        private static int[] popi;
        private GetStreamInfo _gsi = null;
        private int Popi_Num = -1;
        private string _hoso_url = null;

        private Timer _timer = null;
        private int _timer_count = 0;
        private int _pop_time;

        private Form1 _form;  //親フォーム

        public Popup(Form1 fo, GetStreamInfo gsi)
        {
            InitializeComponent();

            _form = fo;
            _gsi = gsi;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this._timer = new Timer();
            this._timer.Tick += new EventHandler(timer_Tick);
            _pop_time = Form1.props.PopupTime * 10;
            _timer_count = 0;
            _timer.Interval = 100;
            this._timer.Start();

            var title = _gsi.Title + " - " + _gsi.Col12.ToString("yyyy/MM/dd(ddd) HH:mm") + "開始";
            this.linkLabel1.Text = title;
            this.label1.Text = _gsi.Provider_Name;
            this.label2.Text = _gsi.Community_Title;
            this.label3.Text = _gsi.Description;
            pictureBox1.ErrorImage = Image.FromFile(Props.GetDefaultThumbnail("comm"));
            pictureBox1.Image = _gsi.Col02;
            _hoso_url = Props.GetLiveUrl(_gsi.LiveId);
        }

        public static void InitPopi()
        {
            popi = new int[POPI_NUM];
            for (int i = 0; i < POPI_NUM; i++)
            {
                popi[i] = 0;
            }
        }

        public int GetPopiNum()
        {
            int num = 0;
            for (int i = 0; i < POPI_NUM; i++)
            {
                if (popi[i] != 1)
                {
                    num = i;
                    break;
                }
            }
            popi[num] = 1;
            Popi_Num = num;

            return num;
        }

        public int ResetPopiNum()
        {
            int num = Popi_Num;
            if (popi[num] == 1)
            {
                popi[num] = 0;
            }
            Popi_Num = -1;

            return num;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                _timer_count++;
                if (_timer_count > _pop_time)
                {
                    base.Opacity -= 0.05;
                    if (base.Opacity < 0.1)
                    {
                        this.Close();
                    }
                }
            }
            catch (Exception Ex)
            {
            }
        }

        public void SetPosition(string xy)
        {
            int width7 = Screen.PrimaryScreen.WorkingArea.Width;
            int height7 = Screen.PrimaryScreen.WorkingArea.Height;
            int width8 = this.Width;
            int height8 = this.Height;
            int num8 = height7 - height8;
            int num9 = width7 - width8;
            this.Top = num8;
            this.Left = num9;

        }

        private void linkLabel1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button != MouseButtons.Right && !string.IsNullOrEmpty(_hoso_url))
                {
                    OpenProcess.OpenWeb(_hoso_url, Form1.props.BrowserPath, Form1.props.IsDefaultBrowser);
                }
                this.Close();
            }
            catch (Exception Ex)
            {
            }
        }

        private void Popup_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._timer.Stop();
            Task.Delay(100).Wait();
            this._timer.Dispose();
            ResetPopiNum();
        }
    }
}
