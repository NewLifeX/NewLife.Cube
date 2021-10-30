using System;

namespace NewLife.Cube.Charts
{
    public class NameValue
    {
        public String Name { get; set; }
        public Object Value { get; set; }

        public NameValue() { }
        public NameValue(String name, Object value)
        {
            Name = name;
            Value = value;
        }
    }
}