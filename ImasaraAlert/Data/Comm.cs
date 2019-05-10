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

    public class Comm : INotifyPropertyChanged, IAlertData
    {
        public Comm()
        { }
        private DateTime last_date = DateTime.MinValue; 

        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly PropertyChangedEventArgs Last_DatePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Last_Date));

        public string Ng { set; get; }
        public string Id { set; get; }
        public string Name { set; get; }
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
        public DateTime Regist_Date { set; get; }
        public bool Pop { set; get; }
        public bool Web { set; get; }
        public bool Sound { set; get; }
        public bool App_a { set; get; }
        public bool App_b { set; get; }
        public bool App_c { set; get; }
        public bool App_d { set; get; }
        public string Memo { set; get; }

        private static Regex RgxChNo = new Regex("/?((co|ch)[\\d]+)", RegexOptions.Compiled);

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
