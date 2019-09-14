using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ImasaraAlert.Prop
{

    public enum PopPos { RD, RU, LD, LU, };

    public class Props
    {
        //定数設定
        public static readonly string Version = "0.1.0.0";
        public static readonly string UserAgent = "Mozilla/5.0 (ImasaraAlert; " + Props.Version + ")";
        //public static readonly string UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
        public static readonly string NicoDomain = "https://nicovideo.jp/";

        //public static string NicoGetStreamInfo = "http://live.nicovideo.jp/api/getstreaminfo/";
        //public static string NicoGetAlertInfo = "http://live.nicovideo.jp/api/getalertinfo";
        public static readonly string NicoRssUrl = "http://live.nicovideo.jp/recent/rss?tab={0}&p={1}";
        public static readonly string NicoUserInfo = "https://seiga.nicovideo.jp/api/user/info?id=";
        public static readonly string NicoCasApi = "https://api.cas.nicovideo.jp/v1/services/live/programs/";

        public static readonly string NicoLiveUrl = "https://live.nicovideo.jp/watch/";
        public static readonly string NicoCommUrl = "https://com.nicovideo.jp/community/";
        public static readonly string NicoChannelUrl = "https://ch.nicovideo.jp/";
        public static readonly string NicoUserUrl = "https://www.nicovideo.jp/user/";


        public bool IsDebug { get; set; }

        public bool IsDefaultBrowser { get; set; }
        public string BrowserPath { get; set; }
        public bool IsMinimization { get; set; }
        public bool IsLogging { get; set; }
        public PopPos PopupPosition { get; set; }
        public int PopupTime { get; set; }
        public bool PopupFront { get; set; }
        public string AppA_Path { get; set; }
        public string AppB_Path { get; set; }
        public string AppC_Path { get; set; }
        public string AppD_Path { get; set; }
        public string Sound_Path { get; set; }
        public bool IsDefaultSound { get; set; }
        public string Sound_File { get; set; }

        public bool LoadData()
        {
            try
            {
                this.IsDefaultBrowser = Properties.Settings.Default.IsDefaultBrowser;
                this.BrowserPath = Properties.Settings.Default.BrowserPath;
                this.IsMinimization = Properties.Settings.Default.IsMinimization;
                this.IsLogging = Properties.Settings.Default.IsLogging;
                //this.PopupPosition = Properties.Settings.Default.PopupPosition;
                this.PopupPosition = (PopPos)Properties.Settings.Default.PopupPosition;
                this.PopupTime = Properties.Settings.Default.PopupTime;
                this.PopupFront = Properties.Settings.Default.PopupFront;
                this.AppA_Path = Properties.Settings.Default.AppA_Path;
                this.AppB_Path = Properties.Settings.Default.AppB_Path;
                this.AppC_Path = Properties.Settings.Default.AppC_Path;
                this.AppD_Path = Properties.Settings.Default.AppD_Path;
                this.Sound_Path = Properties.Settings.Default.Sound_Path;
                this.IsDefaultSound = Properties.Settings.Default.IsDefaultSound;
                this.Sound_File = Properties.Settings.Default.Sound_File;
            }
            catch (Exception Ex)
            {
                MessageBox.Show("LoadData Error: " + Ex.Message);
                return false;
            }
            return true;
        }

        public bool SaveData()
        {
            try
            {
                Properties.Settings.Default.IsDefaultBrowser = this.IsDefaultBrowser;
                Properties.Settings.Default.BrowserPath = this.BrowserPath;
                Properties.Settings.Default.IsMinimization = this.IsMinimization;
                Properties.Settings.Default.IsLogging = this.IsLogging;
                //Properties.Settings.Default.PopupPosition = this.PopupPosition;
                Properties.Settings.Default.PopupPosition = (int)this.PopupPosition;
                Properties.Settings.Default.PopupTime = this.PopupTime;
                Properties.Settings.Default.PopupFront = this.PopupFront;
                Properties.Settings.Default.AppA_Path = this.AppA_Path;
                Properties.Settings.Default.AppB_Path = this.AppB_Path;
                Properties.Settings.Default.AppC_Path = this.AppC_Path;
                Properties.Settings.Default.AppD_Path = this.AppD_Path;
                Properties.Settings.Default.Sound_Path = this.Sound_Path;
                Properties.Settings.Default.IsDefaultSound = this.IsDefaultSound;
                Properties.Settings.Default.Sound_File = this.Sound_File;
                //Properties.Settings.Default. = this.;
                Properties.Settings.Default.Save();

            } catch (Exception Ex)
            {
                MessageBox.Show("SaveData Error: " + Ex.Message);
                return false;
            }
            return true;
        }

        public bool ReloadData()
        {
            Properties.Settings.Default.Reload();
            return this.LoadData();
        }

        public bool ResetData()
        {
            Properties.Settings.Default.Reset();
            return this.LoadData();
        }

        public string GetSoundFile()
        {
            var player_file = string.Empty;

            if (string.IsNullOrEmpty(Sound_Path))
                player_file = Path.Combine(GetApplicationDirectory(), Sound_File);
            else
                player_file = Sound_Path;

            return player_file;
        }

        //設定ファイルの場所をGet
        public static string GetSettingDirectory()
        {
            //設定ファイルの場所
            var config = ConfigurationManager.OpenExeConfiguration(
                ConfigurationUserLevel.PerUserRoamingAndLocal);
            return Path.GetDirectoryName(config.FilePath);
        }

        //アプリケーションの場所をGet
        public static string GetApplicationDirectory()
        {
            //アプリケーションの場所
            var tmp = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(tmp);
        }

        //ログファイル名をGet
        public static string GetLogfile(string dir, string filename)
        {
            var tmp = Path.GetFileNameWithoutExtension(filename) + "_" + System.DateTime.Now.ToString("yyMMdd_HHmmss") + ".log";
            return Path.Combine(dir, tmp);
        }

        public static string GetLiveUrl(string liveid)
        {
            return NicoLiveUrl + liveid;
        }

        public static string GetChannelUrl(string channelid)
        {
            return NicoChannelUrl + channelid;
        }

        public static string GetCommUrl(string channelid)
        {
            return NicoCommUrl + channelid;
        }

        public static string GetUserUrl(string userid)
        {
            return NicoUserUrl + userid;
        }

        public static string GetProviderType(string type)
        {
            var result = "？？";
            switch (type)
            {
                case "official":
                    result = "公式生放送";
                    break;
                case "channel":
                    result = "チャンネル";
                    break;
                case "community":
                    result = "コミュニティ";
                    break;
                case "user":
                    result = "コミュニティ";
                    break;
                case "cas":
                    result = "ニコキャス";
                    break;
                default:
                    break;
            }
            return result;

        }

    }

}
