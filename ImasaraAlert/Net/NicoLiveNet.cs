using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Web;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ImasaraAlert.Prop;
using ImasaraAlert.Data;

namespace ImasaraAlert.Net
{

    public class GetAlertInfo
    {
        public string Status;
        public string Error;

        public string Addr { set; get; }
        public string Port { set; get; }
        public string Thread { set; get; }

        public GetAlertInfo()
        {
            this.Status = null;
            this.Error = null;
        }
    }

    public class NicoLiveNet : IDisposable
    {

        private bool disposedValue = false; // 重複する呼び出しを検知するには

        private WebClientEx _wc = null;

        private static Regex RgxComm = new Regex("<a +href=\"([^\"]*)\"[^>]*>\\s*<span +itemprop=\"name\">([^<]*)</span>\\s*</a>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex RgxUser = new Regex("<a +href=\"([^\"]*)\"[^>]*>\\s*<span +itemprop=\"member\">([^<]*)</span>\\s*</a>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex RgxChName = new Regex("開設</strong><br>\\s*<a +href=\"([^\"]*)\"[^>]*>([^<]*)</a>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex RgxChUser = new Regex("（提供:<strong><span +itemprop=\"name\">([^<]*)</span>", RegexOptions.Compiled | RegexOptions.Singleline);

        private class WebClientEx : WebClient
        {
            public CookieContainer cookieContainer = new CookieContainer();
            private int timeout;

            public WebClientEx(int timeout) : base()
            {
                this.timeout = timeout;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var wr = base.GetWebRequest(address);

                HttpWebRequest hwr = wr as HttpWebRequest;
                if (hwr != null)
                {
                    hwr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; //圧縮を有効化
                    hwr.CookieContainer = cookieContainer; //Cookie
                    hwr.Timeout = timeout;
                }
                return wr;
            }
        }

        //Debug
        public bool IsDebug { get; set; }

        public NicoLiveNet(CookieContainer cc)
        {
            IsDebug = false;

            var wc = new WebClientEx(60);
            _wc = wc;

            _wc.Encoding = Encoding.UTF8;
            _wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);
            _wc.cookieContainer = cc;
            if (IsDebug)
            {
                foreach (Cookie ck in cc.GetCookies(new Uri(Props.NicoDomain)))
                    Debug.WriteLine(ck.Name.ToString() + ": " + ck.Value.ToString());
                for (int i = 0; i < _wc.Headers.Count; i++)
                    Debug.WriteLine(_wc.Headers.GetKey(i).ToString() + ": " + _wc.Headers.Get(i));
            }

        }

        ~NicoLiveNet()
        {
            this.Dispose();
        }

        public async Task<GetStreamInfo> GetStreamInfo2Async(string liveid, string userid)
        {

            var gsi = new GetStreamInfo();
            gsi.Status = "fail";
            gsi.Error = "not_permitted";

            try
            {
                //データー取得
                gsi.LiveId = liveid;
                gsi.Provider_Id = userid;
                //ニコキャスかどうかの判定
                var providertype = "unama";
                gsi.Provider_Type = providertype;

                var html = await _wc.DownloadStringTaskAsync(Props.NicoLiveUrl + liveid);
                if (html.IndexOf("window.NicoGoogleTagManagerDataLayer = [];") > 0)
                {
                    //コミュ限定・有料放送
                    var ttt = Regex.Match(html, "<title>([^<]*)</title>", RegexOptions.Compiled).Groups[1].Value;
                    ttt = Regex.Replace(ttt, "(.*) - (ニコニコ生放送|実験放送)$", "$1");
                    gsi.Title = WebUtility.HtmlDecode(ttt);
                    ttt = Regex.Match(html, "<meta +name=\"description\" +content=\"([^\"]*)\"/>", RegexOptions.Compiled).Groups[1].Value;
                    gsi.Description = WebUtility.HtmlDecode(ttt);
                    ttt = Regex.Match(html, "<meta +itemprop=\"thumbnail\" +content=\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                    gsi.Community_Thumbnail = WebUtility.HtmlDecode(ttt);

                    gsi.Community_Only = "1";

                    ttt = RgxComm.Match(html).Groups[1].Value;
                    gsi.Community_Id = GetStreamInfo.GetChNo(ttt);
                    ttt = RgxComm.Match(html).Groups[2].Value;
                    gsi.Community_Title = WebUtility.HtmlDecode(ttt);

                    gsi.Provider_Id = providertype;
                    gsi.Provider_Name = "公式生放送";
                    if (providertype == "user")
                    {
                        ttt = RgxUser.Match(html).Groups[1].Value;
                        gsi.Provider_Id = GetStreamInfo.GetChNo(ttt);
                        ttt = RgxUser.Match(html).Groups[2].Value;
                        gsi.Provider_Name = WebUtility.HtmlDecode(ttt);
                    }
                    else if (providertype == "channel" || providertype == "official")
                    {
                        ttt = RgxChName.Match(html).Groups[1].Value;
                        gsi.Community_Id = GetStreamInfo.GetChNo(ttt);
                        ttt = RgxChName.Match(html).Groups[2].Value;
                        gsi.Community_Title = WebUtility.HtmlDecode(ttt);

                        ttt = RgxChUser.Match(html).Groups[1].Value;
                        if (!string.IsNullOrEmpty(ttt))
                            gsi.Provider_Name = WebUtility.HtmlDecode(ttt);
                    }
                    gsi.Status = "ok";
                }
                else
                {
                    var ttt = WebUtility.HtmlDecode(Regex.Match(html, "<script +id=\"embedded-data\" +data-props=\"([^\"]*)\"></script>", RegexOptions.Compiled).Groups[1].Value);
                    //Clipboard.SetText(ttt);
                    var dprops = JObject.Parse(ttt);
                    ttt = Regex.Match(html, "<title>([^<]*)</title>", RegexOptions.Compiled).Groups[1].Value;
                    ttt = Regex.Replace(ttt, "(.*) - (ニコニコ生放送|実験放送)$", "$1");
                    gsi.Title = WebUtility.HtmlDecode(ttt);
                    //Clipboard.SetText(dprops.ToString());
                    var dprogram = (JObject)dprops["program"];
                    //gsi.LiveId = dprogram["nicoliveProgramId"].ToString();
                    //gsi.Title = dprogram["title"].ToString();
                    gsi.Description = dprogram["description"].ToString();
                    gsi.Provider_Id = providertype;
                    gsi.Provider_Name = "公式生放送";
                    gsi.Community_Thumbnail = dprogram["thumbnail"]["small"].ToString();
                    JToken aaa;
                    if (dprogram.TryGetValue("supplier", out aaa))
                    {
                        gsi.Provider_Name = dprogram["supplier"]["name"].ToString();
                        if (providertype == "user")
                            gsi.Provider_Id = GetStreamInfo.GetChNo(dprogram["supplier"]["pageUrl"].ToString());
                    }
                    gsi.Community_Only = dprogram["isFollowerOnly"].ToString();
                    gsi.Community_Id = providertype;
                    gsi.Community_Title = "公式生放送";
                    if (dprops["socialGroup"].Count() > 0)
                    {
                        gsi.Community_Id = dprops["socialGroup"]["id"].ToString();
                        gsi.Community_Title = dprops["socialGroup"]["name"].ToString();
                        //gsi.Community_Thumbnail = dprops["socialGroup"]["thumbnailSmallImageUrl"].ToString();
                    }
                    gsi.Status = "ok";
                }
            }
            catch (WebException Ex)
            {
                DebugWrite.WriteWebln(nameof(GetStreamInfo2Async), Ex);
                gsi.Error = Ex.Status.ToString();
                return gsi;
            }
            catch (Exception Ex) //その他のエラー
            {
                DebugWrite.Writeln(nameof(GetStreamInfo2Async), Ex);
                gsi.Error = Ex.Message;
                return gsi;
            }

            return gsi;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    _wc?.Dispose();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            //GC.SuppressFinalize(this);
        }

    }
}
