using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Cube.ViewModels
{
    /// <summary>错误模型</summary>
    public class ErrorModel
    {
        public String RequestId { get; set; }

        public Uri Uri { get; set; }

        public Exception Exception { get; set; }
    }
}
