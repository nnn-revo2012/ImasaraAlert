﻿using System.Reflection;

namespace ImasaraAlert.Prop
{
    public class Ver
    {
        public static readonly string Version = "0.1.0.13";
        public static readonly string VerDate = "(2020/12/27)";

        public static string GetFullVersion()
        {
            return GetAssemblyName() + " Ver" + Version;
        }

        public static string GetAssemblyName()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return assembly.Name;
        }
    }
}