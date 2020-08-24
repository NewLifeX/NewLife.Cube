using System;

namespace NewLife.Cube.ViewModels
{
    /// <summary>错误模型</summary>
    public class ErrorModel
    {
        /// <summary>请求标识</summary>
        public String RequestId { get; set; }

        /// <summary>资源地址</summary>
        public Uri Uri { get; set; }

        /// <summary>异常信息</summary>
        public Exception Exception { get; set; }
    }
}