using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImasaraAlert.Data
{
    interface IAlertData
    {
        string Ng { set; get; }     //00
        string ComId { set; get; }
        string UserId { set; get; }   //ユーザーID
        string Col04 { set; get; } //放送URL 
        string ComName { set; get; }
        string UserName { set; get; } //ユーザー名
        string Group { set; get; }    //06グループ
        string Last_Date { set; get; }
        string Col09 { set; get; } //最近のDateTime
        string Col10 { set; get; } //最近の放送タイトル
        string Col11 { set; get; } //文字色
        string Col12 { set; get; } //背景色
        string Col13 { set; get; } //最近の放送者
        string Col14 { set; get; } //最近の放送番号
        string Col15 { set; get; } //サムネURL
        string Regist_Date { set; get; }
        string Col17 { set; get; } //録画状態
        bool Pop { set; get; }     //*
        bool Ballon { set; get; }  //*
        bool Web { set; get; }     //*
        bool Mail { set; get; }    //*
        bool Sound { set; get; }   //*
        bool Col23 { set; get; }   //席取り
        bool App { set; get; }     //namarokuRecorder *
        bool App_a { set; get; }   //
        bool App_b { set; get; }   //
        bool App_c { set; get; }   //
        bool App_d { set; get; }   //
        string Memo { set; get; }  //
    }
}
