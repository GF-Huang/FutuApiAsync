using System;

namespace FutuApiAsync {
    [Serializable]
    public class FutuClientException : Exception {
        public RetType RetType { get; }

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
