using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpineViewerWPF.Windows
{
    /// <summary>
    /// Open.xaml 的互動邏輯
    /// </summary>
    public partial class Open : Window
    {

        private MainWindow _window;

        public Open(MainWindow main)
        {
            InitializeComponent();

            _window = main;
            string spineVersion = App.globalValues.SelectSpineVersion.ToString();
            if (spineVersion != "")
            {
                cb_Version.SelectedValue = spineVersion;
            }

            
            tb_Canvas_X.Text = App.canvasWidth.ToString();
            tb_Canvas_Y.Text = App.canvasHeight.ToString();
        }

        private void btn_Altas_Open_Click(object sender, RoutedEventArgs e)
        {
           bool isSelect = SelectFile("Spine Altas File (*.atlas)|*.atlas;", tb_Atlas_File);

            if (isSelect)
            {
                App.globalValues.SelectAtlasFile = tb_Atlas_File.Text;
                if (!Common.CheckSpineFile(App.globalValues.SelectAtlasFile))
                {
                    MessageBox.Show("Can not found Spine Json or Binary file！");

                    bool isSelectSp = SelectFile("Spine Json File (*.json)|*.json|Spine Binary File (*.skel)|*.skel", tb_JS_file);
                    if (isSelectSp)
                    {
                        App.globalValues.SelectSpineFile = tb_JS_file.Text;
                    }
                }
                else
                {
                    tb_JS_file.Text = App.globalValues.SelectSpineFile;
                }
            }
            


        }

        private void btn_JS_Open_Click(object sender, RoutedEventArgs e)
        {
           bool isSelect =  SelectFile("Spine Json File (*.json)|*.json|Spine Binary File (*.skel)|*.skel", tb_JS_file);
            if (isSelect)
            {
                tb_JS_file.Text = App.globalValues.SelectSpineFile;
            }
        }

        private bool SelectFile(string filter,TextBox textBox)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (Directory.Exists(App.lastDir))
            {
                openFileDialog.InitialDirectory = App.lastDir;
            }
            else
            {
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;
            }
            openFileDialog.Filter = filter; ;
            if (openFileDialog.ShowDialog() == true)
            {
                textBox.Text = openFileDialog.FileName;
                App.lastDir = Common.GetDirName(openFileDialog.FileName);
                return true;
            }
            return false;

        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            if (cb_Version.SelectedIndex == -1)
            {
                System.Windows.MessageBox.Show("Please Select Spine Version！");
                return;
            }
            if(tb_Atlas_File.Text.Trim() == "")
            {
                System.Windows.MessageBox.Show("Please Select Atlas File！");
                return;
            }
            if (tb_JS_file.Text.Trim() == "")
            {
                System.Windows.MessageBox.Show("Please Select Json or Skel File！");
                return;
            }

            double setWidth;
            double setHeight;
            if (!double.TryParse(tb_Canvas_X.Text,out setWidth) || !double.TryParse(tb_Canvas_Y.Text, out setHeight))
            {
                System.Windows.MessageBox.Show("Please Set Currect Canvas Value！");
                return;
            }
            App.globalValues.FrameWidth = setWidth;
            App.globalValues.FrameHeight = setHeight;
            App.canvasWidth = setWidth;
            App.canvasHeight = setHeight;

            _window.LoadPlayer(cb_Version.SelectionBoxItem.ToString());
            this.Close();

        }
    }
}
