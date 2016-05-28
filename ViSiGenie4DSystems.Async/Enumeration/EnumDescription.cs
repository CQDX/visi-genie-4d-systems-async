// Copyright (c) 2016 Michael Dorough
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ViSiGenie4DSystems.Async.Enumeration
{
    /// <summary>
    /// Supports convertings enum integers to human readable string representations
    /// Applicable for supporting XAML type converters in UI apps.
    /// Reference: http://stackoverflow.com/questions/2787506/get-enum-from-enum-attribute
    /// </summary>
    public class EnumDescription : System.Attribute
    {
        private string HumanReadable { get; set; }

        public EnumDescription(string value)
        {
            this.HumanReadable = value;
        }

        public string Value
        {
            get { return this.HumanReadable; }
        }
    }

    public static class EnumString
    {
        public static string GetStringValue(Enum value)
        {
            string output = null;
            Type type = value.GetType();
            FieldInfo fi = type.GetField(value.ToString());
            EnumDescription[] attrs = fi.GetCustomAttributes(typeof(EnumDescription), false) as EnumDescription[];
            if (attrs.Length > 0)
            {
                output = attrs[0].Value;
            }
            return output;
        }

        public static List<string> GetStringValues(Type enumeration)
        {

            List<string> enumList = new List<string>();

            Array arrayOfEnumValues = Enum.GetValues(enumeration);

            foreach (Enum element in arrayOfEnumValues)
            {
                FieldInfo fi = enumeration.GetField(element.ToString());
                if (null != fi)
                {
                    EnumDescription[] descriptions = fi.GetCustomAttributes(typeof(EnumDescription), true) as EnumDescription[];
                    if (descriptions.Length > 0)
                    {
                        enumList.Add(descriptions[0].Value);
                    }
                    else
                    {
                        enumList.Add("Undefined Description");
                    }
                }
            }
            return enumList;
        }

        /// <summary>
        /// See http://stackoverflow.com/questions/2787506/get-enum-from-enum-attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <returns></returns>
        public static T FromEnumStringValue<T>(this string description) where T : struct
        {
            try
            {
                return (T)typeof(T)
                    .GetFields()
                    .First(f => f.GetCustomAttributes<EnumDescription>()
                                 .Any(a => a.Value.Equals(description, StringComparison.OrdinalIgnoreCase))
                    )
                    .GetValue(null);
            }
            catch (System.InvalidOperationException)
            {
                //TODO: Need fix. What shoudl be returned if description is null?
                return (T)typeof(T).GetFields().First().GetCustomAttributes<EnumDescription>();
            }
        }
    }
}
