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
using SpineViewerWPF.Views;
using Microsoft.Win32;
using WpfXnaControl;
using System.Text.RegularExpressions;
using System.IO;

namespace SpineViewerWPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow MasterMain;
        public static ContentControl MasterControl;
        public static UCPlayer UC_Player;

        public MainWindow()
        {
            InitializeComponent();
            MasterMain = this;
            LoadSetting();

        }

        private void LoadSetting()
        {
            if (App.GV.Scale == 0)
                App.GV.Scale = 1;

            if (Properties.Settings.Default.LastSelectDir == "")
            {
                App.LastDir = App.RootDir;
            }
            else
            {
                App.LastDir = Properties.Settings.Default.LastSelectDir;
            }
            tb_Fps.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("Speed") });
            tb_Spine_Scale.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("Scale") });
            tb_Scale.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("Scale"), StringFormat = "{0}%" });
            lb_Width.SetBinding(ContentProperty, new Binding() { Source = App.GV, Path = new PropertyPath("FrameWidth") });
            lb_Height.SetBinding(ContentProperty, new Binding() { Source = App.GV, Path = new PropertyPath("FrameHeight") });
            tb_PosX.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("PosX") });
            tb_PosY.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("PosY") });
            tb_Rotation.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("Rotation") });

            tb_BG_PosX.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("PosBGX") });
            tb_BG_PosY.SetBinding(TextBox.TextProperty, new Binding() { Source = App.GV, Path = new PropertyPath("PosBGY") });
            chb_UseBG.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("UseBG") });
            lb_ImagePath.SetBinding(ContentProperty, new Binding() { Source = App.GV, Path = new PropertyPath("SelectBG") });
            chb_ControlBG.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("ControlBG") });


            chb_Alpha.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("Alpha") });
            chb_IsLoop.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("IsLoop") });
            chb_PreMultiplyAlpha.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("PreMultiplyAlpha") });
            chb_FilpX.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("FilpX") });
            chb_FilpY.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.GV, Path = new PropertyPath("FilpY") });


            TextCompositionManager.AddPreviewTextInputStartHandler(tb_Fps, tb_Fps_PreviewTextInput);
            sl_Loading.SetBinding(Slider.ValueProperty, new Binding() { Source = App.GV, Path = new PropertyPath("Lock") });
            lb_Loading.SetBinding(ContentProperty, new Binding() { Source = App.GV, Path = new PropertyPath("LoadingProcess") });
            GridAttributes.ColumnDefinitions[0].Width = new GridLength(34);
            gs_Control.Visibility = Visibility.Hidden;
            tc_Control.SelectedIndex = 4;

        }

        private void cb_AnimeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_AnimeList.SelectedIndex != -1)
            {
                if (cb_AnimeList.SelectedItem.ToString() != "")
                {
                    App.GV.SelectAnimeName = cb_AnimeList.SelectedItem.ToString();
                    App.GV.SetAnime = true;
                }
            }
        }

        public void UpdateSpine()
        {
            if(UC_Player != null)
            {
                UC_Player.ChangeSet();
            }
        }

        public static void SetCBAnimeName()
        {
            for (int i = 0; i < App.GV.AnimeList.Count; i++)
            {
                MasterMain.cb_AnimeList.Items.Add(App.GV.AnimeList[i]);
            }

            for (int i = 0; i < App.GV.SkinList.Count; i++)
            {
                MasterMain.cb_SkinList.Items.Add(App.GV.SkinList[i]);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            App.GV.FrameWidth = Math.Round(GridPlayer.ColumnDefinitions[1].ActualWidth + 60 - 2, 2);
            App.GV.FrameHeight = Math.Round(this.ActualHeight - 60, 2);
            Player.Width = App.GV.FrameWidth;
            Player.Height = App.GV.FrameHeight;

        }

        private void cb_SkinList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SkinList.SelectedIndex != -1)
            {
                if (cb_SkinList.SelectedItem.ToString() != "")
                {
                    App.GV.SelectSkin = cb_SkinList.SelectedItem.ToString();
                    App.GV.SetSkin = true;
                }
            }
        }


        private void chb_IsLoop_Click(object sender, RoutedEventArgs e)
        {
            App.GV.SetAnime = true;
        }
        private void chb_PreMultiplyAlpha_Click(object sender, RoutedEventArgs e)
        {
            UpdateSpine();
        }



        private void tb_Fps_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regExp = new Regex(@"\d");
            string singleValue = e.Text;
            e.Handled = !regExp.Match(singleValue).Success;
        }

        private void loadFileToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (Directory.Exists(App.LastDir))
            {
                openFileDialog.InitialDirectory = App.LastDir;
            }
            else
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
            }
           
            openFileDialog.Filter = "Spine Altas Files (*.atlas)|*.atlas;";

            if (cb_Version.SelectedIndex == -1)
            {
                MessageBox.Show("Please Select Spine Version！");
                return;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                Common.Reset();
                App.GV.SelectFile = openFileDialog.FileName;
                App.LastDir = Common.GetDirName(openFileDialog.FileName);

                if (!Common.CheckSpineFile(App.GV.SelectFile))
                {
                    MessageBox.Show("Can not found Spine Json or Binary file！");
                    return;
                }


                MasterMain.cb_AnimeList.Items.Clear();
                MasterMain.cb_SkinList.Items.Clear();
                if (Player.Content != null)
                {
                    App.isNew = true;
                    App.AppXC.ContentManager.Dispose();
                    App.AppXC.Initialize = null;
                    App.AppXC.Update = null;
                    App.AppXC.LoadContent = null;
                    App.AppXC.Draw = null;
                    btn_PlayControl.Content = this.FindResource("img_pause");

                    DependencyObject xnaParent = ((UserControl)Player.Content).Parent;
                    if (xnaParent != null)
                    {
                        xnaParent.SetValue(ContentPresenter.ContentProperty, null);
                    }
                    Canvas oldCanvas = (Canvas)App.AppXC.Parent;
                    if (oldCanvas != null)
                    {
                        oldCanvas.Children.Clear();
                    }
                    Player.Content = null;
                    UC_Player = null;
                }
                App.GV.SpineVersion = cb_Version.SelectionBoxItem.ToString();
                UC_Player = new UCPlayer();
                Player.Content = UC_Player;

            }
        }

        private void cb_gif_q_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_gif_q.SelectedIndex != -1)
            {
                if (cb_gif_q.SelectedItem.ToString() != "")
                {
                    App.GV.GifQuality = ((ComboBoxItem)cb_gif_q.SelectedItem).Content.ToString();
                }
            }
        }

        private void btn_PlayControl_Click(object sender, RoutedEventArgs e)
        {
            if (App.GV.TimeScale == 0)
            {
                App.GV.TimeScale = 1;
                btn_PlayControl.Content = this.FindResource("img_pause"); 
            }
            else if (App.GV.TimeScale == 1)
            {
                App.GV.TimeScale = 0;
                btn_PlayControl.Content = this.FindResource("img_next"); 
            }
        }

        private void btn_CaptureControl_Click(object sender, RoutedEventArgs e)
        {
            Common.TakeScreenshot();
        }

        private void btn_RecodeControl_Click(object sender, RoutedEventArgs e)
        {
            if (!App.GV.IsRecoding)
            {
                App.GV.IsRecoding = true;
                App.GV.SetAnime = true;
                App.GV.SetSkin = true;
            }
        }

        private void Btn_SelectBG_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = App.LastDir;
            openFileDialog.Filter = "Png Files (*.png)|*.png|Jpeg Files (*.jpg)|*.jpg;";

            if (openFileDialog.ShowDialog() == true)
            {
                if (App.TextureBG != null)
                {
                    App.TextureBG.Dispose();
                    App.TextureBG = null;
                }
                   

                App.GV.SelectBG = openFileDialog.FileName;
                App.GV.PosBGX = 0;
                App.GV.PosBGY = 0;
                App.TextureBG = Common.SetBG(App.GV.SelectBG);
            }

        }


        private void Tc_Control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tc_Control.SelectedIndex != 4 && tc_Control.SelectedIndex != -1)
            {
                GridAttributes.ColumnDefinitions[0].Width = new GridLength(App.GV.RedcodePanelWidth);
                gs_Control.Visibility = Visibility.Visible;
                btn_Exporer.Visibility = Visibility.Visible;
            }
            else if (tc_Control.SelectedIndex == 4)
            {
                gs_Control.Visibility = Visibility.Hidden;
                btn_Exporer.Visibility = Visibility.Hidden;
            }
        }

        private void Btn_Exporer_Click(object sender, RoutedEventArgs e)
        {
            if (Convert.ToInt32(GridAttributes.ColumnDefinitions[0].Width.Value) <= 34)
            {
                GridAttributes.ColumnDefinitions[0].Width = new GridLength(App.GV.RedcodePanelWidth);
                gs_Control.Visibility = Visibility.Visible;
            }
            else
            {
                GridAttributes.ColumnDefinitions[0].Width = new GridLength(34);
                gs_Control.Visibility = Visibility.Hidden;
                tc_Control.SelectedIndex = 4;
            }

        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            App.GV.RedcodePanelWidth = Convert.ToInt32(GridAttributes.ColumnDefinitions[0].Width.Value);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.LastSelectDir = App.LastDir;
            Properties.Settings.Default.Save();
        }

    }
}
