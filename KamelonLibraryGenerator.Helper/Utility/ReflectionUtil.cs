using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LDC.Generator.Kamelon.Utility
{
    public enum FieldStatus
    {
        Entity,
        Dto,
        Map,
        Service
    }

    public static class ReflectionUtil
    {
        public static Dictionary<string, string> GetFields<T>(T obj, FieldStatus status)
        {
            var fields = new Dictionary<string, string>();
            var properties = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var primitive = toPrimitive(property.PropertyType.Name);

                if (property.PropertyType.Name.Equals("String"))
                {
                    fields.Add(property.Name, primitive);
                }
                else
                {
                    if (property.PropertyType.IsClass)
                    {
                        switch (status)
                        {
                            case FieldStatus.Entity:
                                    fields.Add(property.Name, primitive);
                                break;
                            case FieldStatus.Dto:
                                    fields.Add(property.Name + "Id", "long");
                                break;
                            case FieldStatus.Map:
                                    fields.Add("#" + property.Name, primitive);
                                break;
                            case FieldStatus.Service:
                                    fields.Add("#" + property.Name, primitive);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(status), status, null);
                        }
                    }
                    else
                    {
                        fields.Add(property.Name, toPrimitive(property.PropertyType.Name));
                    }
                }

            }

            return fields;
        }

        public static string toPrimitive(string str)
        {
            if (str.Contains("Int64"))
                return "long";
            if (str.Contains("String"))
                return "string";
            if (str.Contains("Byte"))
                return "byte";
            if (str.Contains("SByte"))
                return "sbyte";
            if (str.Contains("Int16"))
                return "short";
            if (str.Contains("UInt16"))
                return "ushort";
            if (str.Contains("Int32"))
                return "int";
            if (str.Contains("UInt32"))
                return "uint";
            if (str.Contains("Int64"))
                return "long";
            if (str.Contains("UInt64"))
                return "ulong";
            if (str.Contains("Single"))
                return "float";
            if (str.Contains("Double"))
                return "double";
            if (str.Contains("Decimal"))
                return "decimal";
            
            return str.Split('.').LastOrDefault();
        }

        public static IList<string> GetUsingNamespace<T>(this T obj)
        {
            var list = new List<string>();
            

            var retVal = new Dictionary<string, string>();
            //  System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly
            var propertyInfos = obj.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            foreach (var info in propertyInfos)
            {

                switch (info.PropertyType.Name)
                {
                    case "String":
                        {

                            break;
                        }
                    default:
                        {
                            if (info.PropertyType.IsClass)
                            {

                                var str = info.GetType().Assembly.FullName.Replace("Entity", "Repository");
                                list.Add(str + "." + info.Name + "Repository");

                            }

                            break;
                        }
                }
                break;


            }

            return list;
        }
    }
}
