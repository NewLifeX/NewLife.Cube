using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewLife.Cube
{
    /// <summary>时间转换器</summary>
    /// <remarks>aspnetcore3.0默认不支持时间日期的json序列化</remarks>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        /// <summary>格式</summary>
        public String DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>读取</summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => DateTime.Parse(reader.GetString());

        /// <summary>写入</summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(DateTimeFormat));
    }
}