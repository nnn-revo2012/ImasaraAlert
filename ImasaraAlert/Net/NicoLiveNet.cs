using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
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

    static class TimeoutExtention
    {
        public static async Task Timeout(this Task task, int timeout)
        {
            var delay = Task.Delay(timeout);
            if (await Task.WhenAny(task, delay) == delay)
            {
                throw new TimeoutException();
            }
        }

        public static async Task<T> Timeout<T>(this Task<T> task, int timeout)
        {
            await ((Task)task).Timeout(timeout);
            return await task;
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
            public int timeout;

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

            var wc = new WebClientEx();
            _wc = wc;

            _wc.Encoding = Encoding.UTF8;
            _wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);
            _wc.timeout = 60000;
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

        public async Task<List<GetStreamInfo>> ReadRssAsync(string url, string cate, DateTime now)
        {
            var min_time = now.AddMinutes(-5);
            var max_time = now.AddMinutes(5);
            var lgsi = new List<GetStreamInfo>();
            if (string.IsNullOrEmpty(url)) return lgsi;

            try
            {
                var doc = new XmlDocument();
                var i = 1;
                var end_flg = false;
                while (i < 10 && end_flg == false)
                {
                    var rssurl = string.Format(url, cate, i.ToString());
                    var xhtml = await _wc.DownloadStringTaskAsync(rssurl).Timeout(_wc.timeout);
                    if (string.IsNullOrEmpty(xhtml)) break;
                    doc.LoadXml(xhtml);
                    var nt = doc.NameTable;
                    var nsmgr = new XmlNamespaceManager(nt);
                    var root = doc.SelectSingleNode("rss");
                    nsmgr.AddNamespace("media", root.GetNamespaceOfPrefix("media"));
                    nsmgr.AddNamespace("nicolive", root.GetNamespaceOfPrefix("nicolive"));
                    nsmgr.AddNamespace("dc", root.GetNamespaceOfPrefix("dc"));
                    var items = doc.SelectNodes("rss/channel/item", nsmgr);
                    DateTime stime;
                    foreach (XmlNode item in items)
                    {
                        var gsi = new GetStreamInfo();
                        gsi.Title = item.SelectSingleNode("title", nsmgr).InnerText;
                        gsi.LiveId = item.SelectSingleNode("guid", nsmgr).InnerText;
                        if (DateTime.TryParse(item.SelectSingleNode("pubDate", nsmgr).InnerText, out stime))
                        {
                            if (stime > max_time)
                            {
                                Debug.WriteLine(gsi.LiveId + ": FutureTime " + stime.ToString());
                                stime = stime.AddMinutes(-30);
                            }
                        }
                        gsi.Col12 = stime;
                        gsi.Start_Time = stime.ToString();
                        Debug.WriteLine(gsi.LiveId + ": " + gsi.Start_Time.ToString());
                        gsi.Description = item.SelectSingleNode("description", nsmgr).InnerText;
                        gsi.Community_Thumbnail = item.SelectSingleNode("media:thumbnail", nsmgr).Attributes["url"].InnerText;
                        gsi.Community_Title = item.SelectSingleNode("nicolive:community_name", nsmgr).InnerText;
                        gsi.Community_Id = item.SelectSingleNode("nicolive:community_id", nsmgr).InnerText;
                        gsi.Community_Only = item.SelectSingleNode("nicolive:member_only", nsmgr).InnerText;
                        gsi.Provider_Type = item.SelectSingleNode("nicolive:type", nsmgr).InnerText;
                        gsi.Provider_Name = item.SelectSingleNode("nicolive:owner_name", nsmgr).InnerText;
                        var cates = item.SelectNodes("category", nsmgr);
                        if (cates.Count > 0)
                            gsi.Col15 = cates.Item(0).InnerText;
                        if (stime < min_time)
                        {
                            end_flg = true;
                            break;
                        }
                        lgsi.Add(gsi);
                    }
                    i++;
                    await Task.Delay(1000);
                }
            }
            catch (WebException Ex)
            {
                DebugWrite.WriteWebln(nameof(ReadRssAsync), Ex);
                return lgsi;
            }
            catch (Exception Ex) //その他のエラー
            {
                DebugWrite.Writeln(nameof(ReadRssAsync), Ex);
                return lgsi;
            }
            return lgsi;
        }

        public async Task<System.Drawing.Image> CreateImageAsync(string url)
        {
            System.Drawing.Image img = null;
            try
            {
                using (var wc = new WebClientEx())
                {
                    wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);
                    wc.timeout = 60000;
                    using (var fs = await wc.OpenReadTaskAsync(url).Timeout(wc.timeout))
                    {
                        img = System.Drawing.Image.FromStream(fs);
                    }
                }
            }
            catch (WebException Ex)
            {
                if (Ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var errres = (HttpWebResponse)Ex.Response;
                    if (errres != null)
                    {
                        if (errres.StatusCode == HttpStatusCode.NotFound)
                        {
                            using (var fs = new FileStream(Props.GetDefaultThumbnail("comm"),
                                                           FileMode.Open,
                                                           FileAccess.Read))
                            {
                                img = System.Drawing.Image.FromStream(fs);
                            }
                            return img;
                        }
                    }
                }
                DebugWrite.WriteWebln(nameof(CreateImageAsync), Ex);
                return img;
            }
            catch (Exception Ex) //その他のエラー
            {
                DebugWrite.Writeln(nameof(CreateImageAsync), Ex);
                return img;
            }

            return img;
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

                var html = await _wc.DownloadStringTaskAsync(Props.NicoLiveUrl + liveid).Timeout(_wc.timeout);
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
