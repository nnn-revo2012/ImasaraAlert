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

        public NicoLiveNet()
        {
            IsDebug = false;

            var wc = new WebClientEx();
            _wc = wc;

            _wc.Encoding = Encoding.UTF8;
            _wc.Proxy = null;
            _wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);
            _wc.timeout = 30000;
            _wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

            if (IsDebug)
            {
                //foreach (Cookie ck in cc.GetCookies(new Uri(Props.NicoDomain)))
                //    Debug.WriteLine(ck.Name.ToString() + ": " + ck.Value.ToString());
                for (int i = 0; i < _wc.Headers.Count; i++)
                    Debug.WriteLine(_wc.Headers.GetKey(i).ToString() + ": " + _wc.Headers.Get(i));
            }

        }

        ~NicoLiveNet()
        {
            this.Dispose();
        }

        //UnixTime(msec)文字列をDateTimeに変換
        private static DateTime GetUnixMsecToDateTime(string unixmsec)
        {
            DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unix;
            string ttt = unixmsec;
            if (unixmsec.Length > 3)
                ttt = unixmsec.Substring(0, unixmsec.Length - 3);
            long.TryParse(ttt, out unix);
            return UNIX_EPOCH.AddSeconds(unix).ToLocalTime();
        }

        public async Task<IList<GetStreamInfo>> ReadCateApiAsync(string url, string cate, DateTime now, int mintime)
        {
            var min_time = now.AddMinutes(-(double)mintime);
            //var max_time = now.AddMinutes(5);
            var lgsi = new List<GetStreamInfo>();
            if (string.IsNullOrEmpty(url)) return lgsi;

            try
            {
                var i = 0;
                var end_flg = false;
                while (i < 9 && end_flg == false)
                {
                    var cateurl = string.Format(url, cate, i.ToString());
                    var xhtml = await _wc.DownloadStringTaskAsync(cateurl).Timeout(_wc.timeout);
                    if (string.IsNullOrEmpty(xhtml)) break;

                    var data = JObject.Parse(xhtml);
                    if ((string )data["meta"]["status"] != "200" ||
                        data["data"].Count() < 1)
                    {
                            end_flg = true; break;
                    }
                    foreach (var item in data["data"])
                    {
                        if ((string )item["liveCycle"] != "ON_AIR")
                            continue;
                        var gsi = new GetStreamInfo();
                        gsi.Title = (string )item["title"];
                        gsi.LiveId = (string )item["id"];
                        gsi.Col12 = GetUnixMsecToDateTime(item["beginAt"].ToString());
                        gsi.Start_Time = gsi.Col12.ToString();
                        Debug.WriteLine(gsi.LiveId + ": " + gsi.Start_Time.ToString());
                        gsi.Description = "";
                        gsi.Community_Thumbnail = (string )item["socialGroup"]["thumbnailUrl"];
                        gsi.Community_Title = (string )item["socialGroup"]["name"];
                        gsi.Community_Id = (string )item["socialGroup"]["id"];
                        gsi.Community_Only = item["isFollowerOnly"].ToString().ToLower();
                        gsi.Provider_Type = (string )item["providerType"];
                        gsi.Provider_Name = (string )item["programProvider"]["name"];
                        gsi.Provider_Id = "";
                        if ((string )item["providerType"] == "community")
                            gsi.Provider_Id = (string )item["programProvider"]["id"];
                        gsi.Col15 = cate;
                        if (gsi.Col12 < min_time)
                        {
                            end_flg = true; break;
                        }
                        lgsi.Add(gsi);
                    }
                    i++;
                    await Task.Delay(1000);
                }
            }
            catch (WebException Ex)
            {
                DebugWrite.WriteWebln(nameof(ReadCateApiAsync), Ex);
                return lgsi;
            }
            catch (Exception Ex) //その他のエラー
            {
                DebugWrite.Writeln(nameof(ReadCateApiAsync), Ex);
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
                    wc.Proxy = null;
                    wc.timeout = 30000;
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

        public async Task<string> GetCommNameAsync(string commid)
        {
            string result = null;
            if (string.IsNullOrEmpty(commid)) return result;

            string html = null;
            try
            {
                //データー取得
                html = await _wc.DownloadStringTaskAsync(Props.NicoCommUrl + commid).Timeout(_wc.timeout);
                //< meta property = "og:title" content = "プログラムを作ってみるコミュニティ-ニコニコミュニティ" >
                result = Regex.Match(html, "\"og:title\" *content *= *\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                result = Regex.Replace(result, "(.*)-ニコニコミュニティ$", "$1");
            }
            catch (WebException Ex)
            {
                if (Ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var errres = (HttpWebResponse)Ex.Response;
                    if (errres != null)
                    {
                        if (errres.StatusCode == HttpStatusCode.Forbidden) //403
                        {
                            //データー取得
                            //< meta property = "og:title" content = "プログラムを作ってみるコミュニティ-ニコニコミュニティ" >
                            using (var ds = errres.GetResponseStream())
                            using (var sr = new StreamReader(ds))
                            {
                                var rs = sr.ReadToEnd();
                                result = Regex.Match(rs, "\"og:title\" *content *= *\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                                result = Regex.Replace(result, "(.*)-ニコニコミュニティ$", "$1");
                                return result;
                            }
                        }
                    }
                }
                DebugWrite.WriteWebln(nameof(GetCommNameAsync), Ex);
                return result;
            }
            catch (Exception Ex) //その他のエラー
            {
                DebugWrite.Writeln(nameof(GetCommNameAsync), Ex);
                return result;
            }
            return result;
        }

        public async Task<string> GetChNameAsync(string chid)
        {
            string result = null;
            if (string.IsNullOrEmpty(chid)) return result;

            string html = null;
            try
            {
                //データー取得
                html = await _wc.DownloadStringTaskAsync(Props.NicoChannelUrl + chid).Timeout(_wc.timeout);
                //< meta property = "og:title" content = "旅部(旅部) - ニコニコチャンネル:バラエティ" >
                result = Regex.Match(html, "\"og:title\" *content *= *\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                result = Regex.Replace(result, "(.*) - ニコニコチャンネル:(.*)$", "$1");
            }
            catch (WebException Ex)
            {
                DebugWrite.WriteWebln(nameof(GetChNameAsync), Ex);
                return result;
            }
            catch (Exception Ex) //その他のエラー
            {
                DebugWrite.Writeln(nameof(GetChNameAsync), Ex);
                return result;
            }
            return result;
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
                providertype = Regex.Match(html, "\"content_type\":\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                gsi.Provider_Type = providertype;
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
