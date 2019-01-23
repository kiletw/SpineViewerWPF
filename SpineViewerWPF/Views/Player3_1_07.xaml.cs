using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Spine3_1_07;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpineViewerWPF.Views
{
    /// <summary>
    /// Player.xaml 的互動邏輯
    /// </summary>
    public partial class Player3_1_07 : UserControl
    {
        private SpriteBatch spriteBatch;
        private GraphicsDevice graphicsDevice;
        private Skeleton skeleton;
        private AnimationState state;
        private SkeletonMeshRenderer skeletonRenderer;
        private System.Windows.Point mouseLocation;
        private bool isPress = false;
        private bool isNew = true;
        private ExposedList<Animation> listAnimation;
        private ExposedList<Skin> listSkin;
        private Atlas atlas;
        private SkeletonData skeletonData;
        private AnimationStateData stateData;
        private SkeletonBinary binary;
        private SkeletonJson json;

        public Player3_1_07()
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
            Player.Initialize(ref graphicsDevice,ref spriteBatch);
        }

        private void LoadContent(ContentManager contentManager)
        {
            skeletonRenderer = new SkeletonMeshRenderer(graphicsDevice);
            skeletonRenderer.PremultipliedAlpha = App.GV.Alpha;

            atlas = new Atlas(App.GV.SelectFile, new XnaTextureLoader(graphicsDevice));

            if (Common.IsBinaryData(App.GV.SelectFile))
            {
                binary = new SkeletonBinary(atlas);
                binary.Scale = App.GV.Scale;
                skeletonData = binary.ReadSkeletonData(Common.GetSkelPath(App.GV.SelectFile));
            }
            else
            {
                json = new SkeletonJson(atlas);
                json.Scale = App.GV.Scale;
                skeletonData = json.ReadSkeletonData(Common.GetJsonPath(App.GV.SelectFile));
            }
            skeleton = new Skeleton(skeletonData);

            if (isNew)
            {
                App.GV.PosX = Convert.ToSingle(App.GV.FrameWidth / 2f);
                App.GV.PosY = Convert.ToSingle((skeleton.Data.Height + App.GV.FrameHeight) / 2f);
            }
            App.GV.FileHash = skeleton.Data.Hash;

            stateData = new AnimationStateData(skeleton.Data);

            state = new AnimationState(stateData);

            List<string> AnimationNames = new List<string>();
            listAnimation = state.Data.skeletonData.Animations;
            foreach (Animation An in listAnimation)
            {
                AnimationNames.Add(An.name);
            }
            App.GV.AnimeList = AnimationNames;

            List<string> SkinNames = new List<string>();
            listSkin = state.Data.skeletonData.Skins;
            foreach (Skin Sk in listSkin)
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
                state.SetAnimation(0, state.Data.skeletonData.animations.Items[0].name, App.GV.IsLoop);
            }

            if (isNew)
            {
                MainWindow.SetCBAnimeName();
            }
            isNew = false;

        }



        private void Update(GameTime gameTime)
        {
            if (App.GV.SelectAnimeName != "" && App.GV.SetAnime)
            {
                state.ClearTracks();
                skeleton.SetToSetupPose();
                state.SetAnimation(0, App.GV.SelectAnimeName, App.GV.IsLoop);
                App.GV.SetAnime = false;
            }

            if (App.GV.SelectSkin != "" && App.GV.SetSkin)
            {
                skeleton.SetSkin(App.GV.SelectSkin);
                skeleton.SetSlotsToSetupPose();
                App.GV.SetSkin = false;
            }


        }

        private void Draw()
        {
            if (App.GV.SpineVersion != "3.1.07" || App.GV.FileHash != skeleton.Data.Hash)
            {
                state = null;
                skeletonRenderer = null;
                return;
            }
            graphicsDevice.Clear(Color.Transparent);

            Player.DrawBG(ref spriteBatch);


            state.Update(App.GV.Speed / 1000f);
            state.Apply(skeleton);
            state.TimeScale = App.GV.TimeScale;
            if (binary != null)
            {
                if (App.GV.Scale != binary.Scale)
                {
                    binary.Scale = App.GV.Scale;
                    skeletonData = binary.ReadSkeletonData(Common.GetSkelPath(App.GV.SelectFile));
                    skeleton = new Skeleton(skeletonData);
                }
            }
            else if (json != null)
            {
                if (App.GV.Scale != json.Scale)
                {
                    json.Scale = App.GV.Scale;
                    skeletonData = json.ReadSkeletonData(Common.GetJsonPath(App.GV.SelectFile));
                    skeleton = new Skeleton(skeletonData);
                }
            }

            skeleton.X = App.GV.PosX;
            skeleton.Y = App.GV.PosY;
            skeleton.FlipX = App.GV.FilpX;
            skeleton.FlipY = App.GV.FilpY;



            skeleton.RootBone.Rotation = App.GV.Rotation;
            skeleton.UpdateWorldTransform();
            skeletonRenderer.PremultipliedAlpha = App.GV.Alpha;
            skeletonRenderer.Begin();
            skeletonRenderer.Draw(skeleton);
            skeletonRenderer.End();

            if (state != null)
            {
                TrackEntry entry = state.GetCurrent(0);
                if (entry != null)
                {
                    if (App.GV.IsRecoding && App.GV.GifList != null && entry.LastTime < entry.EndTime)
                    {
                        if (App.GV.GifList.Count == 0)
                        {
                            TrackEntry te = state.GetCurrent(0);
                            te.Time = 0;
                            App.GV.TimeScale = 1;
                            App.GV.Lock = 0;
                        }

                        App.GV.GifList.Add(Common.TakeRecodeScreenshot(graphicsDevice));
                    }

                    if (App.GV.IsRecoding && entry.LastTime >= entry.EndTime)
                    {
                        state.TimeScale = 0;
                        App.GV.IsRecoding = false;
                        Common.RecodingEnd(entry.EndTime);

                        state.TimeScale = 1;
                        App.GV.TimeScale = 1;
                    }

                    if (App.GV.TimeScale == 0)
                    {
                        entry.Time = entry.EndTime * App.GV.Lock;
                        entry.TimeScale = 0;
                    }
                    else
                    {
                        App.GV.Lock = (entry.LastTime % entry.EndTime) / entry.EndTime;
                        entry.TimeScale = 1;
                    }
                    App.GV.LoadingProcess = $"{ Math.Round((entry.Time % entry.EndTime) / entry.EndTime * 100, 2)}%";
                }
            }


        }

        private void Frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isPress = true;
            mouseLocation = Mouse.GetPosition(this.Frame);
        }

        private void Frame_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPress)
            {
                System.Windows.Point position = Mouse.GetPosition(this.Frame);
                if (App.GV.UseBG && App.GV.ControlBG)
                {
                    Common.SetBGXY(position.X, position.Y, this.mouseLocation.X, this.mouseLocation.Y);
                }
                else
                {
                    Common.SetXY(position.X, position.Y, this.mouseLocation.X, this.mouseLocation.Y);
                }
                mouseLocation = Mouse.GetPosition(this.Frame);
            }
        }

        public void ChangeSet()
        {
            App.AppXC.ContentManager.Dispose();
            atlas.Dispose();
            atlas = null;
            App.AppXC.LoadContent.Invoke(App.AppXC.ContentManager);
        }

        private void Frame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isPress = false;
        }

        private void Frame_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Player.Frame_MouseWheel(e);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Player.UserControl_SizeChanged(ref graphicsDevice);
        }

        private void Frame_MouseLeave(object sender, MouseEventArgs e)
        {
            isPress = false;
        }
    }
}
