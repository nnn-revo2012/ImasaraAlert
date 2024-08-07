using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace ImasaraAlert.Data
{

    public class User : INotifyPropertyChanged, IAlertData
    {
        private string last_date = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly PropertyChangedEventArgs Last_DatePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Last_Date));
        public string Ng { set; get; }      //00
        public string ComId { set; get; }
        public string UserId { set; get; }   //ユーザーID
        public string Col04 { set; get; } //放送URL 
        public string ComName { set; get; }
        public string UserName { set; get; } //ユーザー名
        public string Group { set; get; }    //06グループ
        public string Last_Date
        {
            get { return this.last_date; }
            set
            {
                if (this.last_date == value) { return; }
                this.last_date = value;
                this.PropertyChanged?.Invoke(this, Last_DatePropertyChangedEventArgs);
            }
        }
        public string Col09 { set; get; } //最近のDateTime
        public string Col10 { set; get; } //最近の放送タイトル
        public string Col11 { set; get; } //文字色
        public string Col12 { set; get; } //背景色
        public string Col13 { set; get; } //最近の放送者
        public string Col14 { set; get; } //最近の放送番号
        public string Col15 { set; get; } //サムネURL
        public string Regist_Date { set; get; }
        public string Col17 { set; get; } //録画状態
        public bool Pop { set; get; }     //*
        public bool Ballon { set; get; }  //*
        public bool Web { set; get; }     //*
        public bool Mail { set; get; }    //*
        public bool Sound { set; get; }   //*
        public bool Col23 { set; get; }   //席取り
        public bool App { set; get; }     //namarokuRecorder *
        public bool App_a { set; get; }   //
        public bool App_b { set; get; }   //
        public bool App_c { set; get; }   //
        public bool App_d { set; get; }   //
        public string Memo { set; get; }  //

        private static Regex RgxUserNo = new Regex("/?([\\d]+)", RegexOptions.Compiled);
        private static Regex RgxLiveID = new Regex("lv([\\d]+)", RegexOptions.Compiled);

        public User()
        { }

        public void Clear()
        {
            this.Ng = ComId = UserId = Col04 = ComName = UserName = "";
            this.Group = this.Last_Date = this.Regist_Date = this.Memo = "";
            this.Col09 = this.Col10 = this.Col13 = this.Col14 = this.Col15 = this.Col17 = "";
            this.Col11 = "windowtext";
            this.Col12 = "LightCyan";
            this.Pop = this.Ballon = this.Web = this.Mail = this.Sound = false;
            this.Col23 = this.App = this.App_a = this.App_b = this.App_c = this.App_d = false;
        }

        //Urlの最後のスラッシュ以降の文字列を取得
        public static string GetUserNo(string url)
        {
            return RgxUserNo.Match(url).Groups[1].Value;
        }

        //放送IDの lv を削除
        public static string GetLiveNumber(string id)
        {
            return RgxLiveID.Match(id).Groups[1].Value;
        }

        //放送IDに lv を追加
        public static string GetLiveID(string id)
        {
            return "lv" + id;
        }

        public static string FindUserNo(string url)
        {
            return RgxUserNo.Match(url).Groups[1].Value;
        }

    }

}