using AnimatedGif;
using Microsoft.Win32;
using Microsoft.Xna.Framework.Graphics;
using SpineViewerWPF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

public class Common
{
    public static void Reset()
    {
        App.GV.PosX = 0;
        App.GV.PosY = 0;
        App.GV.Scale = 1;
        App.GV.SelectAnimeName = "";
        App.GV.SelectSkin = "";
        App.GV.SetSkin = false;
        App.GV.SetAnime = false;
        App.GV.Rotation = 0;
        App.GV.UseBG = false;
        App.GV.SelectBG = "";
        App.GV.ControlBG = false;
        App.GV.TimeScale = 1;
        App.GV.Lock = 0f;
        App.GV.IsRecoding = false;
        App.GV.FilpX = false;
        App.GV.FilpY = false;
        App.GV.PosBGX = 0;
        App.GV.PosBGY = 0;
        if (App.TextureBG != null)
            App.TextureBG.Dispose();

        if (App.GV.AnimeList != null)
            App.GV.AnimeList.Clear();
        if (App.GV.SkinList != null)
            App.GV.SkinList.Clear();

    }


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

    public static bool CheckSpineFile(string path)
    {
        if (File.Exists(path.Replace(".atlas", ".skel")))
            return true;
        else if (File.Exists(path.Replace(".atlas", ".json")))
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


    public static Texture2D SetBG(string path)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (Image image = Image.FromStream(fileStream))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    return Texture2D.FromStream(App.AppXC.GraphicsDevice, ms);
                }
            }
        }

    }

    public static void SetXY(double MosX, double MosY, double oldX, double oldY)
    {
        App.GV.PosX = (float)(MosX + App.GV.PosX - oldX);
        App.GV.PosY = (float)(MosY + App.GV.PosY - oldY);
    }

    public static void SetBGXY(double MosX, double MosY, double oldX, double oldY)
    {
        App.GV.PosBGX = (float)(MosX + App.GV.PosBGX - oldX);
        App.GV.PosBGY = (float)(MosY + App.GV.PosBGY - oldY);
    }

    public static void RecodingEnd(float AnimationEnd)
    {

        Thread t = new Thread(() =>
        {
            List<MemoryStream> lms = new List<MemoryStream>();
            for (int i = 0; i < App.GV.GifList.Count; i++)
            {
                MemoryStream ms = new MemoryStream();
                App.GV.GifList[i].SaveAsPng(ms, App.GV.GifList[i].Width, App.GV.GifList[i].Height);
                lms.Add(ms);
                App.GV.GifList[i].Dispose();
            }
            Common.SaveToGif(lms, AnimationEnd);
            App.GV.GifList.Clear();
            GC.Collect();
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();



    }

    public static void SaveToGif(List<MemoryStream> lms, float time = 0)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Gif Image|*.gif";
        saveFileDialog.Title = "Save a Gif File";

        GifQuality GQ = new GifQuality();

        switch (App.GV.GifQuality)
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
        int delay = 0;
        if (time == 0)
        {
            delay = 1000 / App.GV.Speed;
        }
        else
        {
            delay = (int)(time * 1000 / lms.Count);

        }

        if (saveFileDialog.FileName != "")
        {
            using (AnimatedGifCreator gifCreator = AnimatedGif.AnimatedGif.Create(saveFileDialog.FileName, delay, App.GV.IsLoop == true ? 0 : 1))
            {
                foreach (MemoryStream msimg in lms)
                {
                    using (msimg)
                    {
                        using(Image img = Image.FromStream(msimg))
                        {
                            gifCreator.AddFrame(img, -1, GQ);
                        }
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
        GC.Collect();

    }

    public static BitmapSource SourceFrom(MemoryStream stream, int? size = null)
    {

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

        if (size.HasValue)
            bitmapImage.DecodePixelHeight = size.Value;

        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        bitmapImage.Freeze(); 
        return bitmapImage;

    }

    public static void SaveToPng(Texture2D texture2D)
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
                texture2D.SaveAsPng(fs, texture2D.Width, texture2D.Height);
            }
        }

    }

    public static Texture2D TakeRecodeScreenshot(GraphicsDevice _graphicsDevice)
    {
        var wpfRenderTarget = (RenderTarget2D)_graphicsDevice.GetRenderTargets()[0].RenderTarget;
        int[] screenData = new int[_graphicsDevice.PresentationParameters.BackBufferWidth * _graphicsDevice.PresentationParameters.BackBufferHeight];
        _graphicsDevice.SetRenderTarget(null);
        wpfRenderTarget.GetData(screenData);

        Texture2D texture = new Texture2D(_graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight, false, _graphicsDevice.PresentationParameters.BackBufferFormat);

        texture.SetData(screenData);
        return texture;
    }

    public static void TakeScreenshot()
    {
        float bakTimeScale = App.GV.TimeScale;

        if (App.AppXC.GraphicsDevice == null)
        {
            MessageBox.Show("No Content！");
            return;
        }

        GraphicsDevice _graphicsDevice = App.AppXC.GraphicsDevice;
        App.GV.TimeScale = 0;
        using (RenderTarget2D renderTarget = new RenderTarget2D(_graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight))
        {
            _graphicsDevice.SetRenderTarget(renderTarget);
            App.AppXC.Draw();
            _graphicsDevice.Viewport = new Viewport(0, 0, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
            _graphicsDevice.SetRenderTarget(null);
            int[] screenData = new int[_graphicsDevice.PresentationParameters.BackBufferWidth * _graphicsDevice.PresentationParameters.BackBufferHeight];
            renderTarget.GetData(screenData);
            Texture2D texture = new Texture2D(_graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight, false, _graphicsDevice.PresentationParameters.BackBufferFormat);
            texture.SetData(screenData);
            Common.SaveToPng(texture);
        }
        App.GV.TimeScale = bakTimeScale;
    }
}


