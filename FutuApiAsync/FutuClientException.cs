using System;

namespace FutuApiAsync {
    /// <summary>
    /// 表示调用 <see cref="FutuQuoteClient"/> 或 <see cref="FutuTradeClient"/> 接口时产生的异常。
    /// </summary>
    [Serializable]
    public class FutuClientException : Exception {
        /// <summary>
        /// 返回结果。
        /// </summary>
        public RetType RetType { get; }

        /// <summary>
        /// 错误代码。
        /// </summary>
        public int ErrorCode { get; }

        public FutuClientException(RetType type, int errorCode, string message) : base(message) {
            RetType = type;
            ErrorCode = errorCode;
        }

        public FutuClientException(RetType type, int errorCode, string message, Exception inner) : base(message, inner) {
            RetType = type;
            ErrorCode = errorCode;
        }

        protected FutuClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public enum RetType {
        Succeed = 0,
        Failed = -1,
        TimeOut = -100,
        Unknown = -400,
    }
}
