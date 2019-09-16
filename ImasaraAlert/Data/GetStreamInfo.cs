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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ImasaraAlert.Prop;
using ImasaraAlert.Data;

namespace ImasaraAlert.Data
{

    public class GetStreamInfo
    {
        public string Status;
        public string Error;

        private string provider_type;
        private string community_only;

        //view1 comm view2 user view3 program view4 live
        public string Col01 { set; get; } //新
        public string Community_Thumbnail { set; get; }
        public string Title { set; get; }
        public string Provider_Name { set; get; }
        public string Community_Title { set; get; }
        public string Description { set; get; }
        public string LiveId { set; get; }
        public string Col08 { set; get; } //*放送URL
        public string Community_Id { set; get; }
        public string Col10 { set; get; } //*コミュURL
        public DateTime Start_Time { set; get; }
        public string Col12 { set; get; } //*pubDate
        public string Col13 { set; get; } //*コメント数
        public string Col14 { set; get; } //*来場者数
        public string Col15 { set; get; } //カテゴリー
        public string Col16 { set; get; } //顔
        public string Col17 { set; get; } //凸
        public string Col18 { set; get; } //クルーズ
        public string Community_Only
        {
            set { community_only = value; }
            get { return (community_only == "1") ? "限定" : ""; }
        }
        public string Provider_Type
        {
            set { provider_type = value; }
            get { return Props.GetProviderType(provider_type); }
        }
        public string Col21 { set; get; } //グループ
        public string Col22 { set; get; } //*予備)
        public string Col23 { set; get; } //*予備
        public string Col24 { set; get; } //お気に入り
        public string Col25 { set; get; } //メモ
        public string Ng { set; get; } //*Ng
        public string Provider_Id { set; get; } //ユーザーID *追加

        private static Regex RgxChNo = new Regex("/([^/]+)$", RegexOptions.Compiled);

        public GetStreamInfo()
        {
            this.Status = null;
            this.Error = null;
        }

        //Urlの最後のスラッシュ以降の文字列を取得
        public static string GetChNo(string url)
        {
            return RgxChNo.Match(url).Groups[1].Value;
        }

    }

}

