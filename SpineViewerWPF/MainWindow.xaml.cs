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
using Microsoft.Xna.Framework;
using SpineViewerWPF.Windows;
using Microsoft.Xna.Framework.Graphics;

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
        public static Open open;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            Game game = new Game();
            //game.IsFixedTimeStep = true;
            this.Title = $"SpineViewerWPF      v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            MasterMain = this;
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,0,100);
            dispatcherTimer.Start();
            LoadSetting();

        }

        private void LoadSetting()
        {
            if (App.globalValues.Scale == 0)
                App.globalValues.Scale = 1;

            if (Properties.Settings.Default.LastSelectDir == "")
            {
                App.lastDir = App.rootDir;
            }
            else
            {
                App.lastDir = Properties.Settings.Default.LastSelectDir;
            }
            tb_Fps.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("Speed") });
            tb_Spine_Scale.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("Scale") });
            lb_ViewScale.SetBinding(ContentProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("ViewScale") });
            lb_ViewScale.ContentStringFormat = $"ViewScale：{0 * 100}%";
            lb_Width.SetBinding(ContentProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("FrameWidth") });
            lb_Height.SetBinding(ContentProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("FrameHeight") });
            tb_PosX.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("PosX") });
            tb_PosY.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("PosY") });
            tb_Rotation.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("Rotation") });

            tb_BG_PosX.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("PosBGX") });
            tb_BG_PosY.SetBinding(TextBox.TextProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("PosBGY") });
            chb_UseBG.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("UseBG") });
            lb_ImagePath.SetBinding(ContentProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("SelectBG") });
            chb_ControlBG.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("ControlBG") });
            lb_SpineVersion.SetBinding(ContentProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("SpineVersion") });

            chb_Alpha.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("Alpha") });
            chb_IsLoop.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("IsLoop") });
            chb_PreMultiplyAlpha.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("PreMultiplyAlpha") });
            chb_FilpX.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("FilpX") });
            chb_FilpY.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("FilpY") });
            chb_LessMemory.SetBinding(CheckBox.IsCheckedProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("UseCache") });

            TextCompositionManager.AddPreviewTextInputStartHandler(tb_Fps, tb_Fps_PreviewTextInput);
            sl_Loading.SetBinding(Slider.ValueProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("Lock") });
            lb_Loading.SetBinding(ContentProperty, new Binding() { Source = App.globalValues, Path = new PropertyPath("LoadingProcess") });
            GridAttributes.ColumnDefinitions[0].Width = new GridLength(34);
            gs_Control.Visibility = Visibility.Hidden;
            tc_Control.SelectedIndex = 4;

            App.mainWidth = this.ActualWidth;
            App.mainHeight = this.ActualHeight;
        }

        private void cb_AnimeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_AnimeList.SelectedIndex != -1)
            {
                if (cb_AnimeList.SelectedItem.ToString() != "")
                {
                    App.globalValues.SelectAnimeName = cb_AnimeList.SelectedItem.ToString();
                    App.globalValues.SetAnime = true;
                }
            }
        }

        public void UpdateSpine()
        {
            if (UC_Player != null)
            {
                UC_Player.ChangeSet();
            }
        }

        public static void SetCBAnimeName()
        {
            for (int i = 0; i < App.globalValues.AnimeList.Count; i++)
            {
                MasterMain.cb_AnimeList.Items.Add(App.globalValues.AnimeList[i]);
            }
            for (int i = 0; i < App.globalValues.SkinList.Count; i++)
            {
                MasterMain.cb_SkinList.Items.Add(App.globalValues.SkinList[i]);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            App.mainWidth = this.ActualWidth;
            App.mainHeight = this.ActualHeight;

            Player.Width = Math.Round(GridPlayer.ColumnDefinitions[1].ActualWidth + 60 - 2, 2);
            Player.Height = Math.Round(this.ActualHeight - 60, 2);

        }

        private void cb_SkinList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_SkinList.SelectedIndex != -1)
            {
                if (cb_SkinList.SelectedItem.ToString() != "")
                {
                    App.globalValues.SelectSkin = cb_SkinList.SelectedItem.ToString();
                    App.globalValues.SetSkin = true;
                }
            }
        }


        private void chb_IsLoop_Click(object sender, RoutedEventArgs e)
        {
            App.globalValues.SetAnime = true;
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
            open = new Open(this);
            open.Show();


        }

        public void LoadPlayer(string spineVersion)
        {
            Common.Reset();

            MasterMain.cb_AnimeList.Items.Clear();
            MasterMain.cb_SkinList.Items.Clear();
            if (Player.Content != null)
            {
                if (App.globalValues.SelectSpineVersion != spineVersion)
                {
                    App.globalValues.SelectSpineVersion = spineVersion;
                    App.isNew = true;
                    App.appXC.ContentManager.Dispose();
                    App.appXC.Initialize = null;
                    App.appXC.Update = null;
                    App.appXC.LoadContent = null;
                    App.appXC.Draw = null;
                    btn_PlayControl.Content = this.FindResource("img_pause");

                    DependencyObject xnaParent = ((UserControl)Player.Content).Parent;
                    if (xnaParent != null)
                    {
                        xnaParent.SetValue(ContentPresenter.ContentProperty, null);
                    }
                    Canvas oldCanvas = (Canvas)App.appXC.Parent;
                    if (oldCanvas != null)
                    {
                        oldCanvas.Children.Clear();
                    }
                    Player.Content = null;
                    UC_Player = new UCPlayer();
                    Player.Content = UC_Player;
                }
                else
                {
                    UC_Player.Reload();
                }
            }
            else
            {
                App.globalValues.SelectSpineVersion = spineVersion;
                UC_Player = new UCPlayer();
                Player.Content = UC_Player;
            }

        }






        private void cb_export_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_export_type.SelectedIndex != -1)
            {
                if (cb_export_type.SelectedItem.ToString() != "")
                {
                    App.globalValues.ExportType = ((ComboBoxItem)cb_export_type.SelectedItem).Content.ToString();
                }
            }
        }


        int tempSpeed = 0;
        private void btn_PlayControl_Click(object sender, RoutedEventArgs e)
        {
            if (App.globalValues.Speed == 0)
            {
                App.globalValues.Speed = tempSpeed;
                App.globalValues.TimeScale = 1;
                btn_PlayControl.Content = this.FindResource("img_pause");
            }
            else if (App.globalValues.Speed > 0)
            {
                tempSpeed = App.globalValues.Speed;
                App.globalValues.Speed = 0;
                App.globalValues.TimeScale = 0;
                btn_PlayControl.Content = this.FindResource("img_next");
            }
            else if (App.globalValues == null)
            {
                App.globalValues = new GlobalValue();
                tempSpeed = App.globalValues.Speed;
                App.globalValues.Speed = 0;
                App.globalValues.TimeScale = 0;
                btn_PlayControl.Content = this.FindResource("img_next");
            }
        }

        private void btn_CaptureControl_Click(object sender, RoutedEventArgs e)
        {
            Common.TakeScreenshot();
        }

        private void btn_RecodeControl_Click(object sender, RoutedEventArgs e)
        {
            if (!App.globalValues.IsRecoding)
            {
                switch (App.globalValues.ExportType)
                {
                    case "Png Sequence":

                        SaveFileDialog saveFileDialog = new SaveFileDialog();

                        saveFileDialog.Filter = "All files (*.*)|*.*";
                        saveFileDialog.RestoreDirectory = true;
                        saveFileDialog.FileName = "Save Path";

                        if (saveFileDialog.ShowDialog() == true)
                        {
                            App.globalValues.ExportPath = System.IO.Path.GetDirectoryName(saveFileDialog.FileName) + "\\";
                        }
                        else
                        {
                            return;
                        }

                        break;
                }



                if (!Directory.Exists(App.tempDirPath))
                    Directory.CreateDirectory(App.tempDirPath);

                Common.ClearCacheFile();
                App.recordImageCount = 1;
                App.globalValues.IsRecoding = true;
                App.globalValues.SetAnime = true;
                App.globalValues.SetSkin = true;
            }
        }

        private void Btn_SelectBG_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = App.lastDir;
            openFileDialog.Filter = "Png Files (*.png)|*.png|Jpeg Files (*.jpg)|*.jpg;";

            if (openFileDialog.ShowDialog() == true)
            {
                if (App.textureBG != null)
                {
                    App.textureBG.Dispose();
                    App.textureBG = null;
                }


                App.globalValues.SelectBG = openFileDialog.FileName;
                App.globalValues.PosBGX = 0;
                App.globalValues.PosBGY = 0;
                App.textureBG = Common.SetBG(App.globalValues.SelectBG);
            }

        }


        private void Tc_Control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tc_Control.SelectedIndex != 4 && tc_Control.SelectedIndex != -1)
            {
                GridAttributes.ColumnDefinitions[0].Width = new GridLength(App.globalValues.RedcodePanelWidth);
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
                GridAttributes.ColumnDefinitions[0].Width = new GridLength(App.globalValues.RedcodePanelWidth);
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
            App.globalValues.RedcodePanelWidth = Convert.ToInt32(GridAttributes.ColumnDefinitions[0].Width.Value);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.LastSelectDir = App.lastDir;
            Properties.Settings.Default.Save();
            if (open != null)
                open.Close();
        }

        private void mi_Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LastSelectDir = App.lastDir;
            Properties.Settings.Default.Save();
            if (open != null)
                open.Close();
            Application.Current.Shutdown();
        }



        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            if (App.graphicsDevice != null && App.graphicsDevice.GraphicsDeviceStatus == Microsoft.Xna.Framework.Graphics.GraphicsDeviceStatus.NotReset)
            {
                App.graphicsDevice.Reset();
            }
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
           if (App.graphicsDevice != null && App.graphicsDevice.GraphicsDeviceStatus == Microsoft.Xna.Framework.Graphics.GraphicsDeviceStatus.NotReset)
            {
                App.graphicsDevice.Reset();
            }
        }


    }
}
