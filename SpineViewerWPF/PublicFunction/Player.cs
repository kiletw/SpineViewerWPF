using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpineViewerWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


public class Player
{
    public static void Initialize(ref GraphicsDevice graphicsDevice, ref SpriteBatch spriteBatch)
    {
        PresentationParameters pp = new PresentationParameters();
        pp.BackBufferWidth = (int)App.GV.FrameWidth;
        pp.BackBufferHeight = (int)App.GV.FrameHeight;
        App.AppXC.RenderSize = new Size(pp.BackBufferWidth, pp.BackBufferHeight);
        graphicsDevice = App.AppXC.GraphicsDevice;
        graphicsDevice.PresentationParameters.BackBufferWidth = (int)App.GV.FrameWidth;
        graphicsDevice.PresentationParameters.BackBufferHeight = (int)App.GV.FrameHeight;
        spriteBatch = new SpriteBatch(graphicsDevice);
    }

    public static void UserControl_SizeChanged(ref GraphicsDevice graphicsDevice)
    {
        App.AppXC.Width = App.GV.FrameWidth;
        App.AppXC.Height = App.GV.FrameHeight;
        if (graphicsDevice != null)
        {
            graphicsDevice.PresentationParameters.BackBufferWidth = (int)App.GV.FrameWidth;
            graphicsDevice.PresentationParameters.BackBufferHeight = (int)App.GV.FrameHeight;
        }
    }

    public static void Frame_MouseWheel(MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            App.GV.Scale += 0.02f;
        }
        else
        {
            if (App.GV.Scale > 0.04f)
            {
                App.GV.Scale -= 0.02f;
            }
        }
    }

    public static void DrawBG(ref SpriteBatch spriteBatch)
    {
        if (App.GV.UseBG && App.TextureBG != null)
        {
            spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend);
            spriteBatch.Draw(App.TextureBG, new Rectangle((int)App.GV.PosBGX, (int)App.GV.PosBGY, App.TextureBG.Width, App.TextureBG.Height), Color.White);
            spriteBatch.End();
        }
    }

}
