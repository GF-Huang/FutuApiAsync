using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutuApiAsync {
    /// <summary>
    /// 表示 <see cref="FutuQuoteClient"/> 或 <see cref="FutuTradeClient"/> 连接到网关时产生的异常。
    /// </summary>
    [Serializable]
    public class FutuClientConnectException : Exception {
        public long ErrorCode { get; }

        public FutuClientConnectException(long errorCode) { ErrorCode = errorCode; }
        public FutuClientConnectException(long errorCode, string message) : base(message) { ErrorCode = errorCode; }
        public FutuClientConnectException(long errorCode, string message, Exception inner) : base(message, inner) { ErrorCode = errorCode; }
        protected FutuClientConnectException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
