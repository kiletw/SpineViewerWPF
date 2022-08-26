using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
//using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        App.globalValues.PosX = 0;
        App.globalValues.PosY = 0;
        App.globalValues.Scale = 1;
        App.globalValues.ViewScale = 1;
        App.globalValues.SelectAnimeName = "";
        App.globalValues.SelectSkin = "";
        App.globalValues.SetSkin = false;
        App.globalValues.SetAnime = false;
        App.globalValues.Rotation = 0;
        App.globalValues.UseBG = false;
        App.globalValues.SelectBG = "";
        App.globalValues.ControlBG = false;
        App.globalValues.TimeScale = 1;
        App.globalValues.Lock = 0f;
        App.globalValues.IsRecoding = false;
        App.globalValues.FilpX = false;
        App.globalValues.FilpY = false;
        App.globalValues.PosBGX = 0;
        App.globalValues.PosBGY = 0;
        if (App.textureBG != null)
            App.textureBG.Dispose();

        if (App.globalValues.AnimeList != null)
            App.globalValues.AnimeList.Clear();
        if (App.globalValues.SkinList != null)
            App.globalValues.SkinList.Clear();

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
        if (File.Exists(path.Replace(".atlas", ".skel")) && path.IndexOf(".skel") > -1)
            return true;
        else
            return false;
    }

    public static bool CheckSpineFile(string path)
    {
        if (File.Exists(path.Replace(".atlas", ".skel")))
        {

            App.globalValues.SelectSpineFile = path.Replace(".atlas", ".skel");
            return true;
        }
        else if (File.Exists(path.Replace(".atlas", ".json")))
        {
            App.globalValues.SelectSpineFile = path.Replace(".atlas", ".json");
            return true;
        }
        else
        {
            App.globalValues.SelectSpineFile = "";
            return false;
        }
          
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
            using (System.Drawing.Image image = System.Drawing.Image.FromStream(fileStream))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    return Texture2D.FromStream(App.appXC.GraphicsDevice, ms);
                }
            }
        }

    }

    public static void SetXY(double MosX, double MosY, double oldX, double oldY)
    {
        App.globalValues.PosX = (float)(MosX + App.globalValues.PosX - oldX);
        App.globalValues.PosY = (float)(MosY + App.globalValues.PosY - oldY);
    }

    public static void SetBGXY(double MosX, double MosY, double oldX, double oldY)
    {
        App.globalValues.PosBGX = (float)(MosX + App.globalValues.PosBGX - oldX);
        App.globalValues.PosBGY = (float)(MosY + App.globalValues.PosBGY - oldY);
    }


    public static void RecodingEnd(float AnimationEnd)
    {
        if(App.globalValues.ExportType == "Gif")
        {
            Thread t = new Thread(() =>
            {
                if (!App.globalValues.UseCache)
                {
                    List<MemoryStream> lms = new List<MemoryStream>();
                    for (int i = 0; i < App.globalValues.GifList.Count; i++)
                    {
                        MemoryStream ms = new MemoryStream();
                        App.globalValues.GifList[i].SaveAsPng(ms, App.globalValues.GifList[i].Width, App.globalValues.GifList[i].Height);
                        lms.Add(ms);
                        App.globalValues.GifList[i].Dispose();
                    }
                    Common.SaveToGif(lms, AnimationEnd);
                    App.globalValues.GifList.Clear();
                    GC.Collect();
                }
                else
                {
                    Common.SaveToGif2(AnimationEnd);
                }

            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

    }

    public static void SaveToGif(List<MemoryStream> lms, float time = 0)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Gif Image|*.gif";
        saveFileDialog.Title = "Save a Gif File";

        string fileName = GetFileNameNoEx(App.globalValues.SelectAtlasFile);
        if (App.globalValues.SelectAnimeName != "")
            fileName += $"_{App.globalValues.SelectAnimeName}";
        if (App.globalValues.SelectSkin != "")
            fileName += $"_{App.globalValues.SelectSkin}";

        saveFileDialog.FileName = fileName;
        saveFileDialog.ShowDialog();
        int delay = 0;
        if (time == 0)
        {
            delay = 1000 / App.globalValues.Speed;
        }
        else
        {
            delay = (int)(time * 1000 * (App.globalValues.Speed / 30f) / lms.Count);
        }
        
        if (saveFileDialog.FileName != "")
        {

            SixLabors.ImageSharp.Image img = null;

            SixLabors.ImageSharp.Image<Rgba32> gif = new SixLabors.ImageSharp.Image<Rgba32>(Convert.ToInt32(App.globalValues.FrameWidth), Convert.ToInt32(App.globalValues.FrameHeight));

            for (int i = 0; i < lms.Count; ++i)
            {
                img = SixLabors.ImageSharp.Image.Load(lms[i].ToArray());
                gif.Frames.AddFrame(img.Frames[0]);
                img.Dispose();
            }


            foreach (ImageFrame frame in gif.Frames)
            {
                GifFrameMetadata meta = frame.Metadata.GetGifMetadata(); // Get or create if none.

                meta.FrameDelay = delay / 10; // Set to 30/100 of a second.
                meta.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            }

            GifMetadata gifMetadata = gif.Metadata.GetGifMetadata();
            gifMetadata.RepeatCount = App.globalValues.IsLoop == true ? (ushort)0 : (ushort)1;
            gif.Frames.RemoveFrame(0);
            using (FileStream fs = File.Create(saveFileDialog.FileName))
            {
                gif.SaveAsGif(fs, new GifEncoder() { ColorTableMode = GifColorTableMode.Global });
            }

        }
        else
        {
            foreach (MemoryStream ms in lms)
            {
                ms.Dispose();
            }

        }
        foreach (Texture2D ms in App.globalValues.GifList)
        {
            ms.Dispose();
        }
        lms.Clear();
        GC.Collect();

    }

    public static void SaveToGif2(float time = 0)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Gif Image|*.gif";
        saveFileDialog.Title = "Save a Gif File";

        string fileName = GetFileNameNoEx(App.globalValues.SelectAtlasFile);
        if (App.globalValues.SelectAnimeName != "")
            fileName += $"_{App.globalValues.SelectAnimeName}";
        if (App.globalValues.SelectSkin != "")
            fileName += $"_{App.globalValues.SelectSkin}";

        saveFileDialog.FileName = fileName;
        saveFileDialog.ShowDialog();

        string[] pngList = Directory.GetFiles($"{App.rootDir}\\Temp\\", "*.png",SearchOption.TopDirectoryOnly);


        int delay = 0;
        if (time == 0)
        {
            delay = 1000 / App.globalValues.Speed;
        }
        else
        {
            delay = (int)(time * 1000 * (App.globalValues.Speed / 30f) / pngList.Length);
        }

        if (saveFileDialog.FileName != "")
        {

            SixLabors.ImageSharp.Image img = null;

            SixLabors.ImageSharp.Image<Rgba32> gif = new SixLabors.ImageSharp.Image<Rgba32>(Convert.ToInt32(App.globalValues.FrameWidth), Convert.ToInt32(App.globalValues.FrameHeight));

            for (int i = 0; i < pngList.Length; ++i)
            {
                img = SixLabors.ImageSharp.Image.Load(pngList[i]);
                gif.Frames.AddFrame(img.Frames[0]);
                img.Dispose();
            }

           
            foreach (ImageFrame frame in gif.Frames)
            {
                GifFrameMetadata meta = frame.Metadata.GetGifMetadata(); // Get or create if none.
                
                meta.FrameDelay = delay/10; // Set to 30/100 of a second.
                meta.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            }

            GifMetadata gifMetadata = gif.Metadata.GetGifMetadata();
            gifMetadata.RepeatCount = App.globalValues.IsLoop == true ? (ushort)0 : (ushort)1;
            gif.Frames.RemoveFrame(0);
            using (FileStream fs = File.Create(saveFileDialog.FileName))
            {
                gif.SaveAsGif(fs,new GifEncoder() { ColorTableMode =  GifColorTableMode.Global});
            }

        }
        else
        {
        }
        ClearCacheFile();

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
        string fileName = GetFileNameNoEx(App.globalValues.SelectAtlasFile);
        if (App.globalValues.SelectAnimeName != "")
            fileName += $"_{App.globalValues.SelectAnimeName}";
        if (App.globalValues.SelectSkin != "")
            fileName += $"_{App.globalValues.SelectSkin}";

        saveFileDialog.FileName = fileName;
        Nullable<bool> result = saveFileDialog.ShowDialog();

        if (result == true)
        {
            using (var fs = (FileStream)saveFileDialog.OpenFile())
            {
                texture2D.SaveAsPng(fs, texture2D.Width, texture2D.Height);
            }
        }

    }

    public static void TakeRecodeScreenshot(GraphicsDevice _graphicsDevice)
    {
        var wpfRenderTarget = (RenderTarget2D)_graphicsDevice.GetRenderTargets()[0].RenderTarget;
        int[] screenData = new int[_graphicsDevice.PresentationParameters.BackBufferWidth * _graphicsDevice.PresentationParameters.BackBufferHeight];
        _graphicsDevice.SetRenderTarget(null);
        wpfRenderTarget.GetData(screenData);

        Texture2D texture = new Texture2D(_graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight, false, _graphicsDevice.PresentationParameters.BackBufferFormat);

        texture.SetData(screenData);
        if (!App.globalValues.UseCache && App.globalValues.ExportType == "Gif")
        {
            App.globalValues.GifList.Add(texture);
            App.recordImageCount++;
        }
        else
        {
            string fileName = GetFileNameNoEx(App.globalValues.SelectAtlasFile);
            if (App.globalValues.SelectAnimeName != "")
                fileName += $"_{App.globalValues.SelectAnimeName}";
            if (App.globalValues.SelectSkin != "")
                fileName += $"_{App.globalValues.SelectSkin}";

            string exportDir = App.tempDirPath;
            if(App.globalValues.ExportType == "Png Sequence")
            {
                exportDir = App.globalValues.ExportPath + "\\";
            }
            using (FileStream fs = new FileStream($"{exportDir}{fileName}_{App.recordImageCount.ToString().PadLeft(7,'0')}.png"
                ,FileMode.Create))
            {
                texture.SaveAsPng(fs, _graphicsDevice.PresentationParameters.BackBufferWidth
                    , _graphicsDevice.PresentationParameters.BackBufferHeight);
            }
            App.recordImageCount++;
            texture.Dispose();
        }
    }

    public static void TakeScreenshot()
    {
        float bakTimeScale = App.globalValues.TimeScale;

        if (App.appXC.GraphicsDevice == null)
        {
            MessageBox.Show("No Content！");
            return;
        }

        GraphicsDevice _graphicsDevice = App.appXC.GraphicsDevice;
        App.globalValues.TimeScale = 0;
        using (RenderTarget2D renderTarget = new RenderTarget2D(_graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight))
        {
            _graphicsDevice.Textures[0] = null;
            _graphicsDevice.SetRenderTarget(renderTarget);
            App.appXC.Draw();
            GameTime gameTime = new GameTime();
            App.appXC.Update(gameTime);
            _graphicsDevice.Viewport = new Viewport(0, 0, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
            _graphicsDevice.SetRenderTarget(null);
            int[] screenData = new int[_graphicsDevice.PresentationParameters.BackBufferWidth * _graphicsDevice.PresentationParameters.BackBufferHeight];
            renderTarget.GetData(screenData);
            Texture2D texture = new Texture2D(_graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight, false, _graphicsDevice.PresentationParameters.BackBufferFormat);
            texture.SetData(screenData);
            Common.SaveToPng(texture);
        }
        App.globalValues.TimeScale = bakTimeScale;
    }


    public static void ClearCacheFile()
    {
        string[] fileList = Directory.GetFiles($"{App.rootDir}\\Temp\\", "*.*", SearchOption.AllDirectories);
        if (fileList.Length > 0)
        {
            foreach(string path in fileList)
            {
                File.Delete(path);
            }
        }
    }

    public static void SetInitLocation(float height)
    {
        if (App.isNew)
        {
            App.globalValues.PosX = Convert.ToSingle(App.globalValues.FrameWidth / 2f);
            App.globalValues.PosY = Convert.ToSingle((height + App.globalValues.FrameHeight) / 2f);
        }
    }

}


