using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spine3_1_07;
using SpineViewerWPF;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Player_3_1_07 : IPlayer
{
    private Skeleton skeleton;
    private AnimationState state;
    private SkeletonMeshRenderer skeletonRenderer;
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
        skeletonRenderer = new SkeletonMeshRenderer(App.graphicsDevice);
        skeletonRenderer.PremultipliedAlpha = App.GV.Alpha;

        atlas = new Atlas(App.GV.SelectAtlasFile, new XnaTextureLoader(App.graphicsDevice));

        if (Common.IsBinaryData(App.GV.SelectSpineFile))
        {
            binary = new SkeletonBinary(atlas);
            binary.Scale = App.GV.Scale;
            skeletonData = binary.ReadSkeletonData(App.GV.SelectSpineFile);
        }
        else
        {
            json = new SkeletonJson(atlas);
            json.Scale = App.GV.Scale;
            skeletonData = json.ReadSkeletonData(App.GV.SelectSpineFile);
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
        if (App.GV.SpineVersion != "3.1.07" || App.GV.FileHash != skeleton.Data.Hash)
        {
            state = null;
            skeletonRenderer = null;
            return;
        }
        App.graphicsDevice.Clear(Color.Transparent);

        Player.DrawBG(ref App.spriteBatch);
        App.GV.TimeScale = (float)App.GV.Speed / 30f;

        state.Update((float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000f);

        state.Apply(skeleton);
        if (binary != null)
        {
            if (App.GV.Scale != binary.Scale)
            {
                binary.Scale = App.GV.Scale;
                skeletonData = binary.ReadSkeletonData(App.GV.SelectSpineFile);
                skeleton = new Skeleton(skeletonData);
            }
        }
        else if (json != null)
        {
            if (App.GV.Scale != json.Scale)
            {
                json.Scale = App.GV.Scale;
                skeletonData = json.ReadSkeletonData(App.GV.SelectSpineFile);
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


    }

    public void Draw()
    {

 

        if (state != null)
        {
            TrackEntry entry = state.GetCurrent(0);
            float speed = (float)App.GV.Speed / 30f;
            if (entry != null)
            {
                if (App.GV.IsRecoding && App.GV.GifList != null && entry.LastTime < entry.EndTime)
                {
                    if (App.GV.GifList.Count == 0)
                    {
                        TrackEntry te = state.GetCurrent(0);
                        te.Time = 0;
                        App.GV.TimeScale = speed;
                        App.GV.Lock = 0;
                    }

                    App.GV.GifList.Add(Common.TakeRecodeScreenshot(App.graphicsDevice));
                }

                if (App.GV.IsRecoding && entry.LastTime >= entry.EndTime)
                {
                    state.TimeScale = 0;
                    App.GV.IsRecoding = false;
                    Common.RecodingEnd(entry.EndTime);

                    state.TimeScale = speed;
                    App.GV.TimeScale = speed;
                }

                if (App.GV.TimeScale == 0)
                {
                    entry.Time = entry.EndTime * App.GV.Lock;
                    entry.TimeScale = 0;
                }
                else
                {
                    App.GV.Lock = (entry.LastTime % entry.EndTime) / entry.EndTime;
                    entry.TimeScale = App.GV.TimeScale;
                }
                App.GV.LoadingProcess = $"{ Math.Round((entry.Time % entry.EndTime) / entry.EndTime * 100, 2)}%";
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

