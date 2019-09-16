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

    public class Comm : INotifyPropertyChanged
    {
        private DateTime last_date = DateTime.MinValue; 

        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly PropertyChangedEventArgs Last_DatePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Last_Date));
        public string Ng { set; get; }      //00
        public string ComId { set; get; }
        public string UserId { set; get; }   //ユーザーID
        public string Col04 { set; get; } //放送URL 
        public string ComName { set; get; }
        public string UserName { set; get; } //ユーザー名
        public string Group { set; get; }    //06グループ
        public DateTime Last_Date
        {
            get { return this.last_date; }
            set
            {
                if (this.last_date == value) { return; }
                this.last_date = value;
                this.PropertyChanged?.Invoke(this, Last_DatePropertyChangedEventArgs);
            }

        }
        public DateTime Col09 { set; get; } //最近のDateTime
        public string Col10 { set; get; } //最近の放送タイトル
        public string Col11 { set; get; } //文字色
        public string Col12 { set; get; } //背景色
        public string Col13 { set; get; } //最近の放送者
        public string Col14 { set; get; } //最近の放送URL
        public string Col15 { set; get; } //サムネURL
        public DateTime Regist_Date { set; get; }
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

        private static Regex RgxChNo = new Regex("/?((co|ch)[\\d]+)", RegexOptions.Compiled);

        public Comm()
        {}

        //Urlの最後のスラッシュ以降の文字列を取得
        public static string GetChNo(string url)
        {
            return RgxChNo.Match(url).Groups[1].Value;
        }

        public static string FindChNo(string url)
        {
            return RgxChNo.Match(url).Groups[1].Value;
        }

    }

}
