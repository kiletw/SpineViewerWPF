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
        pp.BackBufferWidth = (int)App.globalValues.FrameWidth;
        pp.BackBufferHeight = (int)App.globalValues.FrameHeight;
        App.appXC.RenderSize = new Size(pp.BackBufferWidth, pp.BackBufferHeight);
        graphicsDevice = App.appXC.GraphicsDevice;
        graphicsDevice.PresentationParameters.BackBufferWidth = (int)App.globalValues.FrameWidth;
        graphicsDevice.PresentationParameters.BackBufferHeight = (int)App.globalValues.FrameHeight;
        spriteBatch = new SpriteBatch(graphicsDevice);
    }

    public static void UserControl_SizeChanged(ref GraphicsDevice graphicsDevice)
    {
        //App.appXC.Width = App.globalValues.FrameWidth;
        //App.appXC.Height = App.globalValues.FrameHeight;
        //if (graphicsDevice != null)
        //{
        //    graphicsDevice.PresentationParameters.BackBufferWidth = (int)App.globalValues.FrameWidth;
        //    graphicsDevice.PresentationParameters.BackBufferHeight = (int)App.globalValues.FrameHeight;
        //}
    }

    public static void Frame_MouseWheel(MouseWheelEventArgs e)
    {

            if (e.Delta > 0)
            {
                App.globalValues.Scale += 0.02f;
            }
            else
            {
                if (App.globalValues.Scale > 0.04f)
                {
                    App.globalValues.Scale -= 0.02f;
                }
            }
       
    }

    public static void DrawBG(ref SpriteBatch spriteBatch)
    {
        if (App.globalValues.UseBG && App.textureBG != null)
        {
            spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend);
            spriteBatch.Draw(App.textureBG, new Rectangle((int)App.globalValues.PosBGX, (int)App.globalValues.PosBGY, App.textureBG.Width, App.textureBG.Height), Color.White);
            spriteBatch.End();
        }
    }

}
