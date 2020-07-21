using Microsoft.Xna.Framework.Graphics;
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
        public static GlobalValue globalValues = new GlobalValue();
        public static string rootDir = Environment.CurrentDirectory;
        public static string lastDir = "";
        public static XnaControl appXC;
        public static Texture2D textureBG;

        public static bool isPress = false;
        public static bool isNew = true;
        public static System.Windows.Point mouseLocation;
        public static SpriteBatch spriteBatch;
        public static GraphicsDevice graphicsDevice;
        public static int recordImageCount;
        public static double canvasWidth = SystemParameters.WorkArea.Width;
        public static double canvasHeight = SystemParameters.WorkArea.Height;

    }
}
