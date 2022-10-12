using Futu.OpenApi;
using Futu.OpenApi.Pb;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutuApiAsync {
    public abstract class FutuClientBase : IDisposable, FTSPI_Conn {
        public event DisconnectedEventHandler? Disconnected;

        protected readonly FTAPI_Conn _connectionObject;
        // 这里的 Task 返回值用 bool 类型无实际意义，因为 .NET 5 开始才有非泛型版本的 TaskCompletionSource。
        private readonly TaskCompletionSource<bool> _connectTcs = new TaskCompletionSource<bool>();

        protected readonly ConcurrentDictionary<uint, TaskCompletionSource<object>> _requestTaskCompletionSources =
            new ConcurrentDictionary<uint, TaskCompletionSource<object>>();

        static FutuClientBase() => FTAPI.Init();

        protected FutuClientBase(string rsaKey) {
            _connectionObject = CreateConnectionObject();
            _connectionObject.SetClientInfo(nameof(FutuApiAsync), 1);
            _connectionObject.SetConnCallback(this);
            if (!string.IsNullOrWhiteSpace(rsaKey))
                _connectionObject.SetRSAPrivateKey(rsaKey);
        }

        #region Protected

        protected abstract FTAPI_Conn CreateConnectionObject();

        protected Task CreateRequestTask(uint serialNo) {
            var tsc = new TaskCompletionSource<object>();
            _requestTaskCompletionSources[serialNo] = tsc;
            return tsc.Task;
        }

        protected async Task<T> CreateRequestTask<T>(uint serialNo) {
            var tsc = new TaskCompletionSource<object>();
            _requestTaskCompletionSources[serialNo] = tsc;
            return (T)await tsc.Task.ConfigureAwait(false);
        }

        protected void HandleReply(uint serialNo, int retType, int errorCode, string retMsg, object? responseObject) {
            if (_requestTaskCompletionSources.TryRemove(serialNo, out var tcs)) {
                if (retType == ((int)RetType.Succeed))
                    tcs.SetResult(responseObject!);
                else
                    tcs.SetException(new FutuClientException((RetType)retType, errorCode, retMsg));
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// 初始化连接。
        /// </summary>
        /// <param name="ip">OpenD 监听的 IP 地址。</param>
        /// <param name="port">OpenD 监听的端口。</param>
        /// <param name="isEnableEncrypt">是否启用加密。</param>
        /// <returns></returns>
        public Task InitConnect(string ip = "127.0.0.1", int port = 11111, bool isEnableEncrypt = false) {
            if (port < 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), $"端口号超出范围：{port}，应该在 0 ~ 65535 之间。");

            if (!_connectionObject.InitConnect(ip, (ushort)port, isEnableEncrypt))
                throw new InvalidOperationException($"连接已关闭，请重新创建一个 {nameof(FutuQuoteClient)} 或 {nameof(FutuTradeClient)} 对象。");

            return _connectTcs.Task;
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    // TODO: 释放托管状态(托管对象)
                    _connectionObject.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~FutuClientBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region FTSPI_Conn

        void FTSPI_Conn.OnInitConnect(FTAPI_Conn client, long errCode, string desc) {
            if (errCode == 0)
                _connectTcs.SetResult(true);
            else
                _connectTcs.SetException(new FutuClientConnectException(errCode, desc));
        }

        void FTSPI_Conn.OnDisconnect(FTAPI_Conn client, long errCode) {
            Disconnected?.Invoke(this, errCode);
        }

        #endregion
    }

    public delegate void DisconnectedEventHandler(FutuClientBase client, long errorCode);
}
