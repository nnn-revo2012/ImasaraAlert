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
    public class NicoRss : IDisposable
    {

        private bool disposedValue = false; // 重複する呼び出しを検知するには

        //Debug
        public bool IsDebug { get; set; }

        private NicoLiveNet _nLiveNet = null;         //WebClient
        private Form1 _fo;

        public NicoRss(Form1 fo, NicoLiveNet nLiveNet)
        {
            IsDebug = false;

            _nLiveNet = nLiveNet;
            _fo = fo;
        }

        ~NicoRss()
        {
            this.Dispose();
        }

        public async Task<List<GetStreamInfo>> ReadRssAsync()
        {
            var gsi = new List<GetStreamInfo>();
            Debug.WriteLine("ReadRss");
            //RSSを読み込む
            try
            {

            }
            catch (Exception Ex)
            {

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
