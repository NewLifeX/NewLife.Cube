using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NewLife.Collections;

namespace NewLife.Cube.Common
{
    /// <inheritdoc />
    public class CubeBodyValueProvider : IValueProvider
    {
        private readonly IValueProvider _valueProvider;
        private readonly NullableDictionary<String, Object> _body;

        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="valueProvider"></param>
        /// <param name="body"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public CubeBodyValueProvider(IValueProvider valueProvider, NullableDictionary<String, Object> body)
        {
            _valueProvider = valueProvider;
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// 是否包含指定前缀
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public Boolean ContainsPrefix(String prefix) => true;

        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ValueProviderResult GetValue(String key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (key.Length == 0) return ValueProviderResult.None;

            if (!_body.ContainsKey(key)) return _valueProvider.GetValue(key);

            var value = _body[key];

            return value == null ? _valueProvider.GetValue(key) : new ValueProviderResult(value.ToString());
        }
    }
}
