﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace SharpEncrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SharpEncryptModel model = new SharpEncryptModel();
        private Settings settings = new Settings();

        // property for when output text changes
        DependencyPropertyDescriptor dp = DependencyPropertyDescriptor.FromProperty(
             TextBlock.TextProperty,
             typeof(TextBlock));

        public MainWindow()
        {
            InitializeComponent();
            dp.AddValueChanged(textblockOutput, (sender, args) =>
            {
                scrollViewerOutput.ScrollToEnd();
            });
            UpdateUI();
        }

        public void SetIncomingPath(string path)
        {
            if (Util.IsDirectory(path))
            {
                if (!model.ContainsFile(path))
                    model.AddFolder(path);
            }
            else
            {
                if (!model.ContainsFile(path))
                    model.AddFile(path);
            }
            UpdateUI();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filepath in openFileDialog.FileNames)
                {
                    if (!model.ContainsFile(filepath))
                        model.AddFile(filepath);
                }
                UpdateUI();
            }
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                if (!model.ContainsFile(folderBrowserDialog.SelectedPath))
                    model.AddFolder(folderBrowserDialog.SelectedPath);
                UpdateUI();
            }
        }

        private void btnClearFiles_Click(object sender, RoutedEventArgs e)
        {
            model.ClearFiles();
            UpdateUI();
        }

        private void Reset()
        {
            model.RemoveCompleteFiles();
            textboxPassword.Text = "";
            progressBar.Value = progressBar.Minimum;
            UpdateUI();
        }

        private void UpdateUI()
        {
            listboxFiles.ClearItems();
            int i = 0;
            foreach (FileInfo file in model.Files)
            {
                TextBlock tb = new TextBlock()
                {
                    Text = file.GeneratePreviewText(),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness() { Left = 5, Right = 5 },
                    Tag = i
                };

                System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem()
                {
                    Header = "Remove"
                };
                item.Click += fileContextMenuRemove_Click;
                System.Windows.Controls.ContextMenu cm = new System.Windows.Controls.ContextMenu();
                cm.Items.Add(item);
                tb.ContextMenu = cm;

                listboxFiles.AddItem(tb);
                i++;
            }
            btnEncrypt.IsEnabled = model.NumFiles > 0;
            btnDecrypt.IsEnabled = model.NumFiles > 0;
            btnClearFiles.IsEnabled = model.NumFiles > 0;
            menuReg.IsChecked = RegistryManager.DoMenuItemsExist();
            menuLog.IsChecked = settings.LogToFile;
        }

        private void fileContextMenuRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
                TextBlock parent = (item.Parent as System.Windows.Controls.ContextMenu).PlacementTarget as TextBlock;
                int idx = (int)parent.Tag;
                model.RemoveFile(idx);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            UpdateUI();
        }

        private void btnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string password = textboxPassword.Text.Trim();
            if (password.Length <= 0)
            {
                System.Windows.MessageBox.Show("A password is required.");
                return;
            }

            BackgroundWorkerTracker worker = new BackgroundWorkerTracker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWorkEncrypt;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            EncryptOptions options = new EncryptOptions()
            {
                EncryptFilename = checkboxEncryptFilename.IsChecked.Value,
                EncryptDirectoryName = checkboxEncryptDirname.IsChecked.Value
            };
            worker.RunWorkerAsync(argument: new Tuple<string, EncryptOptions>(password, options));
            PrimaryWindow.IsEnabled = false;
        }

        private void worker_DoWorkEncrypt(object sender, DoWorkEventArgs e)
        {
            BackgroundWorkerTracker worker = sender as BackgroundWorkerTracker;
            Tuple<string, EncryptOptions> args = e.Argument as Tuple<string, EncryptOptions>;
            string password = args.Item1;
            EncryptOptions options = args.Item2;
            textblockOutput.LogToFile = settings.LogToFile;
            OutputBuffer buffer = new OutputBuffer(textblockOutput);
            WorkTracker tracker = new WorkTracker(worker, buffer);
            model.EncryptAllFiles(password, options, tracker);
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Windows.MessageBox.Show("All files processed. See the output window for the status of each.");
            PrimaryWindow.IsEnabled = true;
            textblockOutput.OperationComplete = true;
            Reset();
        }

        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            string password = textboxPassword.Text.Trim();
            if (password.Length <= 0)
            {
                System.Windows.MessageBox.Show("A password is required.");
                return;
            }

            BackgroundWorkerTracker worker = new BackgroundWorkerTracker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWorkDecrypt;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(argument: password);
            PrimaryWindow.IsEnabled = false;
        }

        private void worker_DoWorkDecrypt(object sender, DoWorkEventArgs e)
        {
            BackgroundWorkerTracker worker = sender as BackgroundWorkerTracker;
            string password = e.Argument as string;
            textblockOutput.LogToFile = settings.LogToFile;
            OutputBuffer buffer = new OutputBuffer(textblockOutput);
            WorkTracker tracker = new WorkTracker(worker, buffer);
            model.DecryptAllFiles(password, new EncryptOptions(), tracker);
        }

        private void Exit_App(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void menuReg_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
            try
            {
                if (item.IsChecked)
                    RegistryManager.CreateMenuItems();
                else
                    RegistryManager.DeleteMenuItems();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, ex.ToString());
                item.IsChecked = false;
            }
        }

        private void menuLog_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
            settings.LogToFile = item.IsChecked;
        }
    }
}
