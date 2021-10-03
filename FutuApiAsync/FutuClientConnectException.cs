using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutuApiAsync {
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
