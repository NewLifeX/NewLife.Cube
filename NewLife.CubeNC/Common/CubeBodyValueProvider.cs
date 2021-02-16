using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NewLife.Collections;

namespace NewLife.Cube.Common
{
    /// <inheritdoc />
    public class CubeBodyValueProvider : IValueProvider
    {
        private readonly IValueProvider _valueProvider;
        private readonly NullableDictionary<String, Object> _body;

        public CubeBodyValueProvider(IValueProvider valueProvider, NullableDictionary<String, Object> body)
        {
            _valueProvider = valueProvider;
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public Boolean ContainsPrefix(String prefix) => true;

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
