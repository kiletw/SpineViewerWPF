using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spine3_6_39;
using SpineViewerWPF;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Player_3_6_39 : IPlayer
{
    private Skeleton skeleton;
    private AnimationState state;
    private SkeletonRenderer skeletonRenderer;
    private ExposedList<Animation> listAnimation;
    private ExposedList<Skin> listSkin;
    private Atlas atlas;
    private SkeletonData skeletonData;
    private AnimationStateData stateData;
    private SkeletonBinary binary;
    private SkeletonJson json;

    public void Initialize()
    {
        Player.Initialize(ref App.graphicsDevice, ref App.spriteBatch);
    }

    public void LoadContent(ContentManager contentManager)
    {
        skeletonRenderer = new SkeletonRenderer(App.graphicsDevice);
        skeletonRenderer.PremultipliedAlpha = App.GV.Alpha;

        atlas = new Atlas(App.GV.SelectFile, new XnaTextureLoader(App.graphicsDevice));

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

        if (App.isNew)
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

        if (App.isNew)
        {
            MainWindow.SetCBAnimeName();
        }
        App.isNew = false;

    }



    public void Update(GameTime gameTime)
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

    public void Draw()
    {
        if (App.GV.SpineVersion != "3.6.39" || App.GV.FileHash != skeleton.Data.Hash)
        {
            state = null;
            skeletonRenderer = null;
            return;
        }
        App.graphicsDevice.Clear(Color.Transparent);

        Player.DrawBG(ref App.spriteBatch);


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
        if (skeletonRenderer.Effect is BasicEffect)
        {
            ((BasicEffect)skeletonRenderer.Effect).Projection = Matrix.CreateOrthographicOffCenter(0, App.graphicsDevice.Viewport.Width, App.graphicsDevice.Viewport.Height, 0, 1, 0);
        }
        else
        {
            skeletonRenderer.Effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographicOffCenter(0, App.graphicsDevice.Viewport.Width, App.graphicsDevice.Viewport.Height, 0, 1, 0));
        }
        skeletonRenderer.Begin();
        skeletonRenderer.Draw(skeleton);
        skeletonRenderer.End();

        if (state != null)
        {
            TrackEntry entry = state.GetCurrent(0);
            if (entry != null)
            {
                if (App.GV.IsRecoding && App.GV.GifList != null && !entry.IsComplete)
                {
                    if (App.GV.GifList.Count == 0)
                    {
                        TrackEntry te = state.GetCurrent(0);
                        te.trackTime = 0;
                        App.GV.TimeScale = 1;
                        App.GV.Lock = 0;
                    }

                    App.GV.GifList.Add(Common.TakeRecodeScreenshot(App.graphicsDevice));
                }

                if (App.GV.IsRecoding && entry.IsComplete)
                {
                    state.TimeScale = 0;
                    App.GV.IsRecoding = false;
                    Common.RecodingEnd(entry.AnimationEnd);

                    state.TimeScale = 1;
                    App.GV.TimeScale = 1;
                }

                if (App.GV.TimeScale == 0)
                {
                    entry.TrackTime = entry.AnimationEnd * App.GV.Lock;
                    entry.TimeScale = 0;
                }
                else
                {
                    App.GV.Lock = entry.AnimationTime / entry.AnimationEnd;
                    entry.TimeScale = 1;
                }
                App.GV.LoadingProcess = $"{ Math.Round(entry.AnimationTime / entry.AnimationEnd * 100, 2)}%";
            }
        }


    }

    public void ChangeSet()
    {
        App.AppXC.ContentManager.Dispose();
        atlas.Dispose();
        atlas = null;
        App.AppXC.LoadContent.Invoke(App.AppXC.ContentManager);
    }

    public void SizeChange()
    {
        if (App.graphicsDevice != null)
            Player.UserControl_SizeChanged(ref App.graphicsDevice);
    }
}

