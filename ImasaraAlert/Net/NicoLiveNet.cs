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

    public class GetStreamInfo
    {
        public string Status;
        public string Error;

        private string provider_type;
        private string community_only;

        public string Provider_Type
        {
            set { provider_type = value; }
            get { return Props.GetProviderType(provider_type); }
        }
        public string Title { set; get; }
        public string Provider_Name { set; get; }
        public string Community_Title { set; get; }
        public string Description { set; get; }
        public DateTime Start_Time { set; get; }
        public string LiveId { set; get; }
        public string Community_Id { set; get; }
        public string Provider_Id { set; get; }
        public string Community_Thumbnail { set; get; }
        public string Community_Only
        {
            set { community_only = value; }
            get { return (community_only == "1") ? "限定" : ""; }
        }

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
            private CookieContainer cookieContainer = new CookieContainer();

            protected override WebRequest GetWebRequest(Uri address)
            {
                var wr = base.GetWebRequest(address);

                HttpWebRequest hwr = wr as HttpWebRequest;
                if (hwr != null)
                {
                    hwr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; //圧縮を有効化
                    hwr.CookieContainer = cookieContainer; //Cookie
                }
                return wr;
            }
        }

        
        public NicoLiveNet()
        {}

        ~NicoLiveNet()
        {
            this.Dispose();
        }

        public async Task<GetAlertInfo> GetAlertInfoAsync()
        {

            var gai = new GetAlertInfo();
            gai.Status = "fail";
            gai.Error = "notfound";

            try
            {
                //データー取得
                using (WebClientEx wc = new WebClientEx())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);

                    var html = await wc.DownloadDataTaskAsync(Props.NicoGetAlertInfo);
                    var doc = new XmlDocument();
                    doc.LoadXml(Encoding.UTF8.GetString(html));
                    gai.Status = doc.DocumentElement.GetAttribute("status");
                    if (gai.Status != "ok")
                    {
                        //エラーメッセージを入れてリターン
                        gai.Error = doc.GetElementsByTagName("code").Item(0).InnerText;
                        return gai;
                    }

                    var nodes = doc.GetElementsByTagName("ms");
                    if (nodes.Count > 0)
                    {
                        gai.Addr = nodes.Item(0)["addr"].InnerText;
                        gai.Port = nodes.Item(0)["port"].InnerText;
                        gai.Thread = nodes.Item(0)["thread"].InnerText;
                    }
                }

            } catch (WebException Ex)
            {
                //
                gai.Error = Ex.Status.ToString();
                return gai;
            } catch (Exception Ex)
            {
                gai.Error = Ex.Message;
               return gai;
            }

            return gai;
        }

        public async Task<GetStreamInfo> GetStreamInfoAsync(string liveid, string userid)
        {

            var gsi = new GetStreamInfo();
            gsi.Status = "fail";
            gsi.Error = "not_permitted";

            try
            {
                //データー取得
                using (WebClientEx wc = new WebClientEx())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);

                    var html = await wc.DownloadDataTaskAsync(Props.NicoGetStreamInfo+liveid);
                    var doc = new XmlDocument();
                    doc.LoadXml(Encoding.UTF8.GetString(html));
                    gsi.Status = doc.DocumentElement.GetAttribute("status");
                    if (gsi.Status != "ok")
                    {
                        //エラーメッセージを入れてリターン
                        gsi.Error = doc.GetElementsByTagName("code").Item(0).InnerText;
                        return gsi;
                    }
                    gsi.LiveId = doc.SelectSingleNode("/getstreaminfo/request_id").InnerText;
                    gsi.Title = WebUtility.HtmlDecode(doc.SelectSingleNode("/getstreaminfo/streaminfo/title").InnerText);
                    gsi.Description = WebUtility.HtmlDecode(doc.SelectSingleNode("/getstreaminfo/streaminfo/description").InnerText);
                    gsi.Provider_Type = doc.SelectSingleNode("/getstreaminfo/streaminfo/provider_type").InnerText;
                    gsi.Community_Id = doc.SelectSingleNode("/getstreaminfo/streaminfo/default_community").InnerText;
                    gsi.Community_Only = doc.SelectSingleNode("/getstreaminfo/streaminfo/member_only").InnerText;
                    gsi.Community_Title = WebUtility.HtmlDecode(doc.SelectSingleNode("/getstreaminfo/communityinfo/name").InnerText);
                    gsi.Community_Thumbnail = doc.SelectSingleNode("/getstreaminfo/communityinfo/thumbnail").InnerText;

                    html = await wc.DownloadDataTaskAsync(Props.NicoUserInfo+userid);
                    doc.LoadXml(Encoding.UTF8.GetString(html));
                    gsi.Provider_Id = doc.SelectSingleNode("/response/user/id").InnerText;
                    gsi.Provider_Name = WebUtility.HtmlDecode(doc.SelectSingleNode("/response/user/nickname").InnerText);
                }

            } catch (WebException Ex)
            {
                //
                gsi.Error = Ex.Status.ToString();
                return gsi;
            } catch (Exception Ex)
            {
                gsi.Error = Ex.Message;
               return gsi;
            }

            return gsi;
        }

        public async Task<GetStreamInfo> GetStreamInfo2Async(string liveid, string userid)
        {

            var gsi = new GetStreamInfo();
            gsi.Status = "fail";
            gsi.Error = "not_permitted";

            try
            {
                //データー取得
                using (WebClientEx wc = new WebClientEx())
                {
                    gsi.LiveId = liveid;
                    gsi.Provider_Id = userid;
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add(HttpRequestHeader.UserAgent, Props.UserAgent);
                    //ニコキャスかどうかの判定
                    var html = await wc.DownloadStringTaskAsync(Props.NicoCasApi + liveid);
                    var providertype = Regex.Match(html, "\"programType\":\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                    gsi.Provider_Type = providertype;

                    html = await wc.DownloadStringTaskAsync(Props.NicoLiveUrl + liveid);
                    if (html.IndexOf("window.NicoGoogleTagManagerDataLayer = [];") > 0)
                    {
                        //コミュ限定・有料放送
                        if (providertype != "cas")
                        {
                            providertype = Regex.Match(html, "content.content_type *= *'([^']*)';", RegexOptions.Compiled).Groups[1].Value;
                            gsi.Provider_Type = providertype;
                        }
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
                        if (providertype != "cas")
                        {
                            providertype = Regex.Match(html, "\"content_type\":\"([^\"]*)\"", RegexOptions.Compiled).Groups[1].Value;
                            gsi.Provider_Type = providertype;
                        }
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
                        if (providertype == "cas")
                        {
                            gsi.Community_Thumbnail = dprogram["thumbnail"]["imageUrl"].ToString();
                            gsi.Provider_Name = dprops["broadcaster"]["nickname"].ToString();
                            gsi.Provider_Id = dprops["broadcaster"]["id"].ToString();
                        }
                        else
                        {
                            gsi.Community_Thumbnail = dprogram["thumbnail"]["small"].ToString();
                            JToken aaa;
                            if (dprogram.TryGetValue("supplier", out aaa))
                            {
                                gsi.Provider_Name = dprogram["supplier"]["name"].ToString();
                                if (providertype == "user")
                                    gsi.Provider_Id = GetStreamInfo.GetChNo(dprogram["supplier"]["pageUrl"].ToString());
                            }
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

            }
            catch (WebException Ex)
            {
                //
                gsi.Error = Ex.Status.ToString();
                return gsi;
            }
            catch (Exception Ex)
            {
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
