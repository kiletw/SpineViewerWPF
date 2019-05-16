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
            App.AppXC = new WpfXnaControl.XnaControl();

            switch (App.GV.SpineVersion)
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
            }

            App.AppXC.Initialize += player.Initialize;
            App.AppXC.Update += player.Update;
            App.AppXC.LoadContent += player.LoadContent;
            App.AppXC.Draw += player.Draw;
            App.AppXC.Width = App.GV.FrameWidth;
            App.AppXC.Height = App.GV.FrameHeight;

            Frame.Children.Add(App.AppXC);

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
                if (App.GV.UseBG && App.GV.ControlBG)
                {
                    Common.SetBGXY(position.X, position.Y, App.mouseLocation.X, App.mouseLocation.Y);
                }
                else
                {
                    Common.SetXY(position.X, position.Y, App.mouseLocation.X, App.mouseLocation.Y);
                }
                App.mouseLocation = Mouse.GetPosition(this.Frame);
            }
        }

        private void Frame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            App.isPress = false;
        }

        private void Frame_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Player.Frame_MouseWheel(e);
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
    }
}
