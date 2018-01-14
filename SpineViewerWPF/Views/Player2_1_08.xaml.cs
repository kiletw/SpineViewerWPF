using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Spine2_1_08;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfXnaControl;


namespace SpineViewerWPF.Views
{
    /// <summary>
    /// Player.xaml 的互動邏輯
    /// </summary>
    public partial class Player2_1_08 : UserControl
    {
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _graphicsDevice;
        Skeleton skeleton;
        AnimationState state;
        SkeletonBounds bounds = new SkeletonBounds();
        SkeletonMeshRenderer skeletonRenderer;
        private System.Windows.Point MouseP;
        private bool isPress = false;
        private bool isNew = true;
        private bool isRecoding = false;
        List<Texture2D> gifList;

        public Player2_1_08()
        {
            InitializeComponent();
            App.AppXC.Initialize += Initialize;
            App.AppXC.Update += Update;
            App.AppXC.LoadContent += LoadContent;
            App.AppXC.Draw += Draw;
            App.AppXC.Width = App.GV.FrameWidth;
            App.AppXC.Height = App.GV.FrameHeight;

            Frame.Children.Add(App.AppXC);

        }

        private void Initialize()
        {
            _graphicsDevice = App.AppXC.GraphicsDevice;
            _graphicsDevice.PresentationParameters.BackBufferWidth = (int)App.GV.FrameWidth;
            _graphicsDevice.PresentationParameters.BackBufferHeight = (int)App.GV.FrameHeight;
            _spriteBatch = new SpriteBatch(_graphicsDevice);
        }

        private void LoadContent(ContentManager contentManager)
        {
            skeletonRenderer = new SkeletonMeshRenderer(_graphicsDevice);
            skeletonRenderer.PremultipliedAlpha = App.GV.Alpha;

            Atlas atlas = new Atlas(App.GV.SelectFile, new XnaTextureLoader(_graphicsDevice));
            SkeletonData skeletonData;

            SkeletonJson json = new SkeletonJson(atlas);
            json.Scale = 1;
            skeletonData = json.ReadSkeletonData(Common.GetJsonPath(App.GV.SelectFile));


            skeleton = new Skeleton(skeletonData);

            App.GV.PosX = skeleton.Data.Width;
            App.GV.PosY = skeleton.Data.Height;
            App.GV.FileHash = skeleton.Data.Hash;

            AnimationStateData stateData = new AnimationStateData(skeleton.Data);

            state = new AnimationState(stateData);

            if (isRecoding)
            {
                state.Start += State_Start;
                state.Complete += State_Complete;
            }


            List<string> AnimationNames = new List<string>();
            List<Animation> LA = state.Data.skeletonData.Animations;
            foreach (Animation An in LA)
            {
                AnimationNames.Add(An.name);
            }
            App.GV.AnimeList = AnimationNames;

            List<string> SkinNames = new List<string>();
            List<Skin> LS = state.Data.skeletonData.Skins;
            foreach (Skin Sk in LS)
            {
                SkinNames.Add(Sk.name);
            }
            App.GV.SkinList = SkinNames;

            if (App.GV.SelectAnimeName != "")
            {
                state.SetAnimation(0, App.GV.SelectAnimeName, App.GV.IsLoop);
            }
            else
            {
                state.SetAnimation(0, state.Data.skeletonData.animations[0].name, App.GV.IsLoop);
            }

            if (isNew)
            {
                MainWindow.SetCBAnimeName();
            }
            isNew = false;

        }

        private void State_Complete(AnimationState state, int trackIndex, int loopCount)
        {
            isRecoding = false;

            List<MemoryStream> lms = new List<MemoryStream>();
            for (int i = 0; i < gifList.Count; i++)
            {
                MemoryStream ms = new MemoryStream();
                gifList[i].SaveAsPng(ms, gifList[i].Width, gifList[i].Height);
                lms.Add(ms);
            }
            Common.SaveToGif(lms);
            gifList.Clear();
            ChangeSet();
        }

        private void Update(GameTime gameTime)
        {
            if (isRecoding && gifList != null)
            {
                gifList.Add(TakeScreenshot());
            }
        }

        private void Draw()
        {
            if (App.GV.SpineVersion != "2.1.08" || App.GV.FileHash != skeleton.Data.Hash)
            {
                state = null;
                
                skeletonRenderer = null;
                return;
            }
            _graphicsDevice.Clear(Color.Transparent);
            state.Update(App.GV.Speed / 1000f);
            state.Apply(skeleton);
            state.TimeScale = App.GV.TimeScale;
            skeleton.X = App.GV.PosX;
            skeleton.Y = App.GV.PosY;
            if (App.GV.SelectSkin != "" && App.GV.SetSkin)
            {
                skeleton.SetSkin(App.GV.SelectSkin);
                App.GV.SetSkin = false;
            }
            if (App.GV.SelectAnimeName != "" && App.GV.SetAnime)
            {
                state.ClearTracks();
                state.SetAnimation(0, App.GV.SelectAnimeName, App.GV.IsLoop);
                App.GV.SetAnime = false;
            }
            skeleton.UpdateWorldTransform();
            skeletonRenderer.PremultipliedAlpha = App.GV.Alpha;
            skeletonRenderer.Begin();
            skeletonRenderer.Draw(skeleton);
            skeletonRenderer.End();
        }

        private void State_Start(AnimationState state, int trackIndex)
        {
            gifList = new List<Texture2D>();
        }

        private void Frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isPress = true;
            MouseP = Mouse.GetPosition(this.Frame);
        }

        private void Frame_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPress)
            {
                System.Windows.Point position = Mouse.GetPosition(this.Frame);
                SetXY(position.X, position.Y, this.MouseP.X, this.MouseP.Y);
                MouseP = Mouse.GetPosition(this.Frame);
            }
        }

        public void SetXY(double MosX, double MosY, double oldX, double oldY)
        {
            App.GV.PosX = (float)(MosX + App.GV.PosX - oldX);
            App.GV.PosY = (float)(MosY + App.GV.PosY - oldY);
        }

        public void ChangeSet()
        {
            App.AppXC.LoadContent.Invoke(App.AppXC.ContentManager);
        }

        private void Frame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isPress = false;
        }

        public Texture2D TakeScreenshot()
        {
            RenderTarget2D screenshot = new RenderTarget2D(_graphicsDevice, (int)App.AppXC.Width, (int)App.AppXC.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            _graphicsDevice.SetRenderTarget(screenshot);
            Draw();
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.Clear(new Microsoft.Xna.Framework.Color(0, 0, 0, 0));

            return screenshot;
        }

        private void btn_Capture_Click(object sender, RoutedEventArgs e)
        {
            bool IsPause = false;
            if (App.GV.TimeScale == 0)
            {
                IsPause = true;
            }
            else
            {
                App.GV.TimeScale = 0;
            }
            using (Texture2D t2d = TakeScreenshot())
            {
                Common.SaveToPng(t2d);
            }

            if (!IsPause)
            {
                App.GV.TimeScale = 1;
            }
        }

        private void btn_Pause_Click(object sender, RoutedEventArgs e)
        {
            App.GV.TimeScale = 0;
        }

        private void btn_Play_Click(object sender, RoutedEventArgs e)
        {
            App.GV.TimeScale = 1;
        }

        private void Frame_MouseWheel(object sender, MouseWheelEventArgs e)
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
            Atlas atlas = new Atlas(App.GV.SelectFile, new XnaTextureLoader(_graphicsDevice));
            SkeletonData skeletonData;

            SkeletonJson json = new SkeletonJson(atlas);
            json.Scale = App.GV.Scale;
            skeletonData = json.ReadSkeletonData(Common.GetJsonPath(App.GV.SelectFile));


            skeleton = new Skeleton(skeletonData);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            App.AppXC.Width = App.GV.FrameWidth;
            App.AppXC.Height = App.GV.FrameHeight;
            if (_graphicsDevice != null)
            {
                _graphicsDevice.PresentationParameters.BackBufferWidth = (int)App.GV.FrameWidth;
                _graphicsDevice.PresentationParameters.BackBufferHeight = (int)App.GV.FrameHeight;
            }

        }

        private void btn_Recode_Click(object sender, RoutedEventArgs e)
        {
            isRecoding = true;
            ChangeSet();
        }
    }
}
