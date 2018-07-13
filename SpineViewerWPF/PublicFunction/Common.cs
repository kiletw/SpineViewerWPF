using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SpineViewerWPF;
using Microsoft.Win32;
using System.Drawing;
using AnimatedGif;
using Microsoft.Xna.Framework.Graphics;

public class Common
{
    public static string GetDirName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    public static string GetFileNameNoEx(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    public static bool IsBinaryData(string path)
    {
        if (File.Exists(path.Replace(".atlas", ".skel")))
            return true;
        else
            return false;
    }

    public static string GetSkelPath(string path)
    {
        return path.Replace(".atlas", ".skel");
    }

    public static string GetJsonPath(string path)
    {
        return path.Replace(".atlas", ".json");
    }

    public static void SaveToGif(List<MemoryStream> lms)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Gif Image|*.gif";
        saveFileDialog.Title = "Save a Gif File";

        GifQuality GQ = new GifQuality();

        switch(App.GV.GifQuality)
        {
            case "Default":
                GQ = GifQuality.Default;
                break;
            case "Bit8":
                GQ = GifQuality.Bit8;
                break;
            case "Bit4":
                GQ = GifQuality.Bit4;
                break;
            case "Grayscale":
                GQ = GifQuality.Grayscale;
                break;
            default:
                GQ = GifQuality.Default;
                break;
        }

        string fileName = GetFileNameNoEx(App.GV.SelectFile);
        if (App.GV.SelectAnimeName != "")
            fileName += $"_{App.GV.SelectAnimeName}";
        if (App.GV.SelectSkin != "")
            fileName += $"_{App.GV.SelectSkin}";

        saveFileDialog.FileName = fileName;
        saveFileDialog.ShowDialog();

        if (saveFileDialog.FileName != "")
        {
            using (AnimatedGifCreator gifCreator = AnimatedGif.AnimatedGif.Create(saveFileDialog.FileName, 1000 / App.GV.Speed))
            {
                foreach (MemoryStream img in lms)
                {
                    using (img)
                    {
                        gifCreator.AddFrame(Image.FromStream(img), GQ);
                        img.Dispose();
                    }
                }
            }
        }
        else
        {
            foreach (MemoryStream ms in lms)
            {
                ms.Dispose();
            }
        }
        lms.Clear();
    }

    public static void SaveToPng(Texture2D t2d)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Png Image|*.png";
        saveFileDialog.Title = "Save a Png File";
        string fileName = GetFileNameNoEx(App.GV.SelectFile);
        if (App.GV.SelectAnimeName != "")
            fileName += $"_{App.GV.SelectAnimeName}";
        if (App.GV.SelectSkin != "")
            fileName += $"_{App.GV.SelectSkin}";

        saveFileDialog.FileName = fileName;
        saveFileDialog.ShowDialog();

        if (saveFileDialog.FileName != "")
        {
            using (var fs = (FileStream)saveFileDialog.OpenFile())
            {
                t2d.SaveAsPng(fs, t2d.Width, t2d.Height);
            }
        }

    }
}


