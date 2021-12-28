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

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                // TODO: WORK WITH FOLDERS
                foreach (string filepath in openFileDialog.FileNames)
                {
                    if (!model.ContainsFile(filepath))
                        model.AddFile(filepath);
                }
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
                    Text = file.FileName + " (" + string.Format("{0:0.00}", file.FileSizeMB) + "MB)",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness() { Left = 5, Right = 5 },
                    Tag = i
                };
                
                MenuItem item = new MenuItem()
                {
                    Header = "Remove"
                };
                item.Click += fileContextMenuRemove_Click;
                ContextMenu cm = new ContextMenu();
                cm.Items.Add(item);
                tb.ContextMenu = cm;

                listboxFiles.AddItem(tb);
                i++;
            }
            btnEncrypt.IsEnabled = model.NumFiles > 0;
            btnDecrypt.IsEnabled = model.NumFiles > 0;
            btnClearFiles.IsEnabled = model.NumFiles > 0;
        }

        private void fileContextMenuRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem item = sender as MenuItem;
                TextBlock parent = (item.Parent as ContextMenu).PlacementTarget as TextBlock;
                int idx = (int)parent.Tag;
                model.RemoveFile(idx);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            UpdateUI();
        }

        private void btnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string password = textboxPassword.Text.Trim();
            if (password.Length <= 0)
            {
                MessageBox.Show("A password is required.");
                return;
            }

            BackgroundWorkerTracker worker = new BackgroundWorkerTracker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWorkEncrypt;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(argument: new Tuple<string, bool>(password, checkboxEncryptFilename.IsChecked.Value));
            PrimaryWindow.IsEnabled = false;
        }

        private void worker_DoWorkEncrypt(object sender, DoWorkEventArgs e)
        {
            BackgroundWorkerTracker worker = sender as BackgroundWorkerTracker;
            Tuple<string, bool> args = e.Argument as Tuple<string, bool>;
            string password = args.Item1;
            bool encryptFilename = args.Item2;
            OutputBuffer buffer = new OutputBuffer(textblockOutput);
            WorkTracker tracker = new WorkTracker(worker, buffer);
            model.EncryptAllFiles(password, encryptFilename, tracker);
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("All files processed. See the output window for the status of each.");
            PrimaryWindow.IsEnabled = true;
            Reset();
        }

        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            string password = textboxPassword.Text.Trim();
            if (password.Length <= 0)
            {
                MessageBox.Show("A password is required.");
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
            OutputBuffer buffer = new OutputBuffer(textblockOutput);
            WorkTracker tracker = new WorkTracker(worker, buffer);
            model.DecryptAllFiles(password, tracker);
        }
    }
}
