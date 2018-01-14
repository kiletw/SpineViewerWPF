using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfXnaControl;

namespace SpineViewerWPF
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static GlobalValue GV = new GlobalValue();
        public static string root = Environment.CurrentDirectory;
        public static string LastDir = "";
        public static XnaControl AppXC;
    }
}
