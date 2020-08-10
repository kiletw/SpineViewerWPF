using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpineViewerWPF.Views
{
    /// <summary>
    /// Player.xaml 的互動邏輯
    /// </summary>
    public partial class UCPlayer : UserControl
    {
        public IPlayer player;


        public UCPlayer()
        {
            InitializeComponent();
            App.appXC = new WpfXnaControl.XnaControl();

            switch (App.globalValues.SelectSpineVersion)
            {
                case "2.1.08":
                    player = new Player_2_1_08();
                    break;
                case "2.1.25":
                    player = new Player_2_1_25();
                    break;
                case "3.1.07":
                    player = new Player_3_1_07();
                    break;
                case "3.4.02":
                    player = new Player_3_4_02();
                    break;
                case "3.5.51":
                    player = new Player_3_5_51();
                    break;
                case "3.6.32":
                    player = new Player_3_6_32();
                    break;
                case "3.6.39":
                    player = new Player_3_6_39();
                    break;
                case "3.6.53":
                    player = new Player_3_6_53();
                    break;
                case "3.7.83":
                    player = new Player_3_7_83();
                    break;
                case "3.8.95":
                    player = new Player_3_8_95();
                    break;
            }

            App.appXC.Initialize += player.Initialize;
            App.appXC.Update += player.Update;
            App.appXC.LoadContent += player.LoadContent;
            App.appXC.Draw += player.Draw;
            App.appXC.Width = App.globalValues.FrameWidth;
            App.appXC.Height = App.globalValues.FrameHeight;

            var transformGroup = (TransformGroup)Frame.RenderTransform;
            var tt = (TranslateTransform)transformGroup.Children.Where(x => x.GetType() == typeof(TranslateTransform)).FirstOrDefault();
            tt.X = (float)((App.mainWidth ) / 2 - (App.canvasWidth / 2) -10);
            tt.Y = (float)((App.mainHeight ) / 2 - (App.canvasHeight / 2)-40);

            Frame.Children.Add(App.appXC);

        }


        private void Frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            App.isPress = true;
            App.mouseLocation = Mouse.GetPosition(this.Frame);
        }

        private void Frame_MouseMove(object sender, MouseEventArgs e)
        {
            if (App.isPress)
            {
                System.Windows.Point position = Mouse.GetPosition(this.Frame);
                if (App.globalValues.UseBG && App.globalValues.ControlBG)
                {
                    Common.SetBGXY(position.X, position.Y, App.mouseLocation.X, App.mouseLocation.Y);
                }
                else if (Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    Common.SetXY(position.X, position.Y, App.mouseLocation.X, App.mouseLocation.Y);
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                   
                    var transformGroup = (TransformGroup)Frame.RenderTransform;
                    var tt = (TranslateTransform)transformGroup.Children.Where(x => x.GetType() == typeof(TranslateTransform)).FirstOrDefault();
                    tt.X = (float)(position.X + tt.X - App.mouseLocation.X);
                    tt.Y = (float)(position.Y + tt.Y - App.mouseLocation.Y);
                }
                    App.mouseLocation = Mouse.GetPosition(this.Frame);
            }
        }

        private void Frame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            App.isPress = false;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            player.SizeChange();
        }

        private void Frame_MouseLeave(object sender, MouseEventArgs e)
        {
            App.isPress = false;
        }

        public void ChangeSet()
        {
            player.ChangeSet();
        }

        public void Reload()
        {
            var transformGroupL = (TransformGroup)Frame.LayoutTransform;
            var st = (ScaleTransform)transformGroupL.Children.Where(x => x.GetType() == typeof(ScaleTransform)).FirstOrDefault();
            st.ScaleX = 1;
            st.ScaleY = 1;
            var transformGroupR = (TransformGroup)Frame.RenderTransform;
            var tt = (TranslateTransform)transformGroupR.Children.Where(x => x.GetType() == typeof(TranslateTransform)).FirstOrDefault();
            tt.X = (float)((App.mainWidth) / 2 - (App.canvasWidth / 2) - 10);
            tt.Y = (float)((App.mainHeight) / 2 - (App.canvasHeight / 2) - 40);
            Frame.Children.Remove(App.appXC);
            App.appXC.Initialize -= player.Initialize;
            App.appXC.Update -= player.Update;
            App.appXC.LoadContent -= player.LoadContent;
            App.appXC.Draw -= player.Draw;


            App.appXC.Initialize += player.Initialize;
            App.appXC.Update += player.Update;
            App.appXC.LoadContent += player.LoadContent;
            App.appXC.Draw += player.Draw;
            App.appXC.Width = App.globalValues.FrameWidth;
            App.appXC.Height = App.globalValues.FrameHeight;

            Frame.Children.Add(App.appXC);
            player.ChangeSet();
            MainWindow.SetCBAnimeName();
        }

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                Player.Frame_MouseWheel(e);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                double zoom = e.Delta > 0 ? .02 : -.02;
                var transformGroup = (TransformGroup)Frame.LayoutTransform;
                var st = (ScaleTransform)transformGroup.Children.Where(x => x.GetType() == typeof(ScaleTransform)).FirstOrDefault();
                App.globalValues.ViewScale += zoom;
                st.ScaleX = App.globalValues.ViewScale;
                st.ScaleY = App.globalValues.ViewScale;
                Frame.Width = Frame.ActualWidth;
                Frame.Height = Frame.ActualHeight;

            }
        }
    }
}
