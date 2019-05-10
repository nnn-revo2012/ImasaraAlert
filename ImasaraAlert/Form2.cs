using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using ImasaraAlert.Prop;

namespace ImasaraAlert
{
    public partial class Form2 : Form
    {

        private static Regex rbRegex = new Regex("^rB_(.+)$", RegexOptions.Compiled);

        private Form1 _form;  //親フォーム
        private Props _props;

        public Form2(Form1 fo)
        {
            InitializeComponent();

            _form = fo;
            _props = new Props();

        }

        //変数→フォーム
        private void SetForm()
        {
            //Popup位置
            foreach (Control co in groupBox4.Controls)
            {
                if (co.GetType().Name == "RadioButton")
                      {
                         if (rbRegex.Replace(co.Name.ToString(), "$1") == _props.PopupPosition.ToString())
                             ((RadioButton)co).Checked = true;
                      }
            }
            textBox1.Text = _props.BrowserPath;
            textBox2.Text = _props.AppA_Path;
            textBox3.Text = _props.AppB_Path;
            textBox4.Text = _props.AppC_Path;
            textBox5.Text = _props.AppD_Path;
            textBox6.Text = _props.Sound_Path;

            checkBox1.Checked = _props.IsDefaultBrowser;
            checkBox2.Checked = _props.IsMinimization;
            checkBox3.Checked = _props.IsLogging;
            checkBox4.Checked = _props.PopupFront;

            return;
        }

        //フォーム→変数
        private void GetForm()
        {
            //Popup位置
            foreach (Control co in groupBox4.Controls)
            {
                if (co.GetType().Name == "RadioButton")
                {
                    if ((bool)((RadioButton)co).Checked)
                        _props.PopupPosition =
                            (PopPos)Enum.Parse(typeof(PopPos), rbRegex.Replace(co.Name.ToString(), "$1"));
                }
            }
            _props.BrowserPath = textBox1.Text;
            _props.AppA_Path = textBox2.Text;
            _props.AppB_Path = textBox3.Text;
            _props.AppC_Path = textBox4.Text;
            _props.AppD_Path = textBox5.Text;
            _props.Sound_Path = textBox6.Text;

            _props.IsDefaultBrowser = checkBox1.Checked;
            _props.IsMinimization = checkBox2.Checked;
            _props.IsLogging = checkBox3.Checked;
            _props.PopupFront = checkBox4.Checked;

            return;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //OKボタンが押されたら設定を保存
            GetForm();
            var result = _props.SaveData(); //設定ファイルに保存
            result = Form1.props.LoadData(); //親フォームの設定データを更新
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //設定値を初期値に戻す
            var _props_save = new Props();
            var result = _props_save.LoadData(); //現在の設定ファイル内容を読み込み
            result = _props.ResetData(); //設定ファイルを初期化
            SetForm();
            result = _props_save.SaveData(); //キャンセルした時用に元の設定をファイルに書き込み
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Webブラウザー指定
        }


        private void Form2_Shown(object sender, EventArgs e)
        {
            //フォーム表示後データー読み込み＆表示
            var result = _props.LoadData();
            SetForm();
        }

        private void SelectFolder()
        {
            try
            {
                using (var folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderBrowserDialog1.Description = "フォルダを指定してください。";
                    folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;
                    if (String.IsNullOrEmpty(textBox1.Text)) //空白の場合はマイドキュメント指定
                    {
                        folderBrowserDialog1.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                    else
                    {
                        folderBrowserDialog1.SelectedPath = textBox1.Text;
                    }
                    folderBrowserDialog1.ShowNewFolderButton = true;

                    //ダイアログを表示する
                    if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
                    {
                        //選択されたフォルダを表示する
                        textBox1.Text = folderBrowserDialog1.SelectedPath;
                    }
                }
            } catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectFile()
        {
            try
            {
                using (var openFileDialog1 = new System.Windows.Forms.OpenFileDialog())
                {
                    //openFileDialog1.FileName = Properties.Settings.Default.DefExecFile;
                    openFileDialog1.InitialDirectory = @"C:\";
                    openFileDialog1.Filter = "実行ファイル(*.exe;*.com)|*.exe;*.com|すべてのファイル(*.*)|*.*";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.Title = "開くファイルを選択してください";
                    //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
                    openFileDialog1.RestoreDirectory = true;

                    if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
                    {
                        //選択されたファイルを表示する
                        textBox3.Text = openFileDialog1.FileName;
                    }
                }
            } catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
