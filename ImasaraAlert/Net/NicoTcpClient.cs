using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ImasaraAlert.Net
{

    public class NicoTcpClient : IDisposable
    {

        private bool disposedValue = false; // 重複する呼び出しを検知するには

        private TcpClient _client = null;
        private NetworkStream _netstream = null;
        private CancellationTokenSource _tokenSource = null;
        private CancellationToken _token;

        public NicoTcpClient()
        {
        }

        ~NicoTcpClient()
        {
            this.Dispose();
        }


        public async Task ConnectAsync(string addr, int port)
        {

            try
            {
                // Establish the remote endpoint for the socket.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(addr);
                IPAddress ipAddress = ipHostInfo.AddressList[0];

                // Create a TCP/IP socket.  
                _client = new TcpClient();

                await _client.ConnectAsync(ipAddress, port);

                _netstream = _client.GetStream();
                _netstream.WriteTimeout = 10000;
                _netstream.ReadTimeout =  600000;

                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;

            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }

        public async Task SendAsync(string mes)
        {
            if (string.IsNullOrEmpty(mes)) return;

            try
            {
                // Write a message over the TCP Connection
                var ttt = Encoding.UTF8.GetBytes(mes);
                _token.ThrowIfCancellationRequested();
                if (!_netstream.CanWrite) return;
                await _netstream.WriteAsync(ttt, 0, ttt.Length, _token);
                Debug.WriteLine("SendAsync END");
                _token.ThrowIfCancellationRequested();
                if (!_netstream.CanWrite) return;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
            return;
        }

        public async Task<string> ReadAsync()
        {
            var res = string.Empty;
            var ttt = new Byte[_client.ReceiveBufferSize];

            try
            {
                // Read server response
                _token.ThrowIfCancellationRequested();
                if (!_netstream.CanRead) return res;
                var len = await _netstream.ReadAsync(ttt, 0, _client.ReceiveBufferSize, _token);
                Debug.WriteLine("ReadAsync END");
                _token.ThrowIfCancellationRequested();
                if (!_netstream.CanRead) return res;
                //Debug.WriteLine(len);
                if (len > 0)
                {
                    //var ttt2 = new Byte[len];
                    //Array.Copy(ttt, 0, ttt2, 0, len);
                    var ttt2 = (new ArraySegment<Byte>(ttt,0,len)).ToArray();
                    res = Encoding.UTF8.GetString(ttt2);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
            return res;
        }

        public void Cancel()
        {
            _tokenSource?.Cancel();

        }

        public bool IsCancelRequested()
        {
            return _token.IsCancellationRequested; 

        }

        public void Close()
        {
            try
            {
                if (!_token.IsCancellationRequested)
                {
                    _tokenSource?.Cancel();
                    Task.Delay(100);
                }
                _tokenSource?.Dispose();
                _tokenSource = null;

                _netstream?.Close();
                _netstream?.Dispose();
                _netstream = null;

                _client?.Close();
                _client?.Dispose();
                _client = null;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    _netstream?.Dispose();
                    _client?.Dispose();
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
