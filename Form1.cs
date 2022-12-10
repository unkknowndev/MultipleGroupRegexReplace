using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WK.Libraries.BetterFolderBrowserNS;

namespace MultipleGroupRegexReplace
{
    public partial class Form1 : Form
    {
        private List<Control> _controlsState = new List<Control>();
        private int _occurrencesReplaced;
        private Regex _regex;
        private BackgroundWorker _worker;

        public Form1()
        {
            InitializeComponent();
        }

        private void ToggleUIEnabled()
        {
            if (_controlsState.Any())
            {
                // Turn back on because they are disabled
                foreach (var control in _controlsState) control.Enabled = true;
                _controlsState.Clear();
            }
            else
            {
                // Turn off because they are enabled
                _controlsState = Controls.OfType<Control>().Where(x => Enabled).ToList();
                foreach (var control in _controlsState) control.Enabled = false;
            }
        }

        private void UpdateFileProgress(string currentFile, long currentProgress, long currentFileLength,
            int currentIndex, int fileCount)
        {
            var currentFileProgressPercent = Utils.CalculatePercentage(currentProgress, currentFileLength);

            progressBar1.Maximum = 100;
            progressBar2.Maximum = fileCount;

            // For instantly updating progressbar
            progressBar1.Value = Convert.ToInt32(currentFileProgressPercent);
            if (progressBar1.Value > 0)
                progressBar1.Value--;
            progressBar1.Value = Convert.ToInt32(currentFileProgressPercent);

            progressBar2.Value = currentIndex;
            label1.Text = (Math.Truncate(currentFileProgressPercent * 100) / 100).ToString("0.00") + "%";
            label2.Text =
                (Math.Truncate(Utils.CalculatePercentage(currentIndex, fileCount) * 100) / 100).ToString("0.00") + "%";
            label3.Text = currentFile;
        }

        private void ProcessFiles(string[] files)
        {
            // Init worker
            _worker = new BackgroundWorker { WorkerReportsProgress = true };
            _occurrencesReplaced = 0;
            var regexGroupName = comboBox1.GetItemText(comboBox1.SelectedIndex);

            // Disable UI
            ToggleUIEnabled();

            // Set progressbars up
            UpdateFileProgress("No file", 0, 1, 0, 1);

            // Assign work
            _worker.DoWork += (sender, args) =>
            {
                // Read file line by line without loading everything into memory (aka large file support)
                for (int i = 0; i < files.Length; i++)
                {
                    var tempFilePath = Path.GetTempFileName(); // Get temp file to write contents to
                    using (var newFileStream = File.OpenWrite(tempFilePath)) // Create new temp file and open stream
                    {
                        using (var newFileWriter = new StreamWriter(newFileStream)) // Open filestream to write to file
                        {
                            using (var oldFileStream =
                                   File.OpenRead(files[i])) // Open file to replace lines in filestream
                            {
                                using (var sr = new StreamReader(oldFileStream)) // Open filestream to read file
                                {
                                    string line;
                                    while ((line = sr.ReadLine()) != null)
                                    {
                                        // Regex replace
                                        var newString = _regex.ReplaceGroup(line, regexGroupName, textBox2.Text);
                                        if (newString != line)
                                        {
                                            // Add to temp file
                                            newFileWriter.WriteLine(newString);
                                            _occurrencesReplaced++;
                                        }
                                        else
                                        {
                                            // Add to temp file
                                            newFileWriter.WriteLine(line);
                                        }

                                        // Update progress
                                        _worker.ReportProgress(0,
                                            new object[]
                                            {
                                                Path.GetFileName(files[i]), oldFileStream.Position,
                                                oldFileStream.Length, i, files.Length
                                            });
                                    }
                                }
                            }
                        }
                    }

                    // Replace old file with temp
                    File.Delete(files[i]);
                    File.Move(tempFilePath, files[i]);
                }
            };

            // Set progress event
            _worker.ProgressChanged += (sender, args) =>
            {
                var progressState = (object[])args.UserState;
                UpdateFileProgress((string)progressState[0], (long)progressState[1], (long)progressState[2],
                    (int)progressState[3],
                    Convert.ToInt32(progressState[4]));
            };

            // Set on completed event
            _worker.RunWorkerCompleted += (sender, args) =>
            {
                // Show msgbox with info
                MessageBox.Show("Files processed: " + files.Length + Environment.NewLine + "Occurrences replaced: " +
                                _occurrencesReplaced);

                // Reset progressbar
                UpdateFileProgress("No file", 0, 1, 0, 1);

                // Enable UI
                ToggleUIEnabled();
            };

            // Start backgroundworker
            _worker.RunWorkerAsync();
        }

        private void fileBtn_Click(object sender, EventArgs e)
        {
            var senderButton = sender as Button;

            if (senderButton == fileBtn)
            {
                OpenFileDialog dlg = new OpenFileDialog { Multiselect = true };

                // Parse filters
                var extensionFilters = textBox3.Text.Split(',');
                for (int i = 0; i < extensionFilters.Length; i++) extensionFilters[i] = "*" + extensionFilters[i];

                dlg.Filter = "Selected Extensions(" + string.Join(";", extensionFilters) + ")|" +
                             string.Join(";", extensionFilters) + "|All files (*.*)|*.*";

                if (dlg.ShowDialog() == DialogResult.OK) ProcessFiles(dlg.FileNames);
            }
            else
            {
                BetterFolderBrowser dlg = new BetterFolderBrowser { Multiselect = true };
                var subfolderMsgBoxResult =
                    MessageBox.Show("Do you want to include all subfolders and their files too?", "",
                        MessageBoxButtons.YesNoCancel);

                switch (subfolderMsgBoxResult)
                {
                    case DialogResult.Yes:
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            var foldersFiles = new List<string>();
                            foreach (var dlgSelectedFolder in dlg.SelectedFolders)
                                if (!string.IsNullOrWhiteSpace(textBox3.Text))
                                    foldersFiles.AddRange(Directory
                                        .GetFiles(dlgSelectedFolder, "*.*", SearchOption.AllDirectories).Where(file =>
                                            textBox3.Text.Split(',').Any(x =>
                                                file.EndsWith(x, StringComparison.OrdinalIgnoreCase))));
                                else
                                    foldersFiles.AddRange(Directory
                                        .GetFiles(dlgSelectedFolder, "*.*", SearchOption.AllDirectories));

                            ProcessFiles(foldersFiles.ToArray());
                        }

                        break;
                    case DialogResult.No:
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            var foldersFiles = new List<string>();
                            foreach (var dlgSelectedFolder in dlg.SelectedFolders)
                                if (!string.IsNullOrWhiteSpace(textBox3.Text))
                                    foldersFiles.AddRange(Directory
                                        .GetFiles(dlgSelectedFolder, "*.*", SearchOption.TopDirectoryOnly).Where(file =>
                                            textBox3.Text.Split(',').Any(x =>
                                                file.EndsWith(x, StringComparison.OrdinalIgnoreCase))));
                                else
                                    foldersFiles.AddRange(Directory
                                        .GetFiles(dlgSelectedFolder, "*.*", SearchOption.TopDirectoryOnly));

                            ProcessFiles(foldersFiles.ToArray());
                        }

                        break;
                }
            }
        }

        private bool IsRegexValid(string input, out Regex regex)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(input))
                {
                    regex = new Regex(input);
                    regex.IsMatch("");
                    return true;
                }
            }
            catch (Exception e)
            {
            }

            regex = null;
            return false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();

            if (IsRegexValid(textBox1.Text, out var regex))
            {
                _regex = regex;
                textBox1.BackColor = SystemColors.Window;

                comboBox1.Items.Clear();
                foreach (var group in regex.GetGroupNames())
                    comboBox1.Items.Add(group);

                fileBtn.Enabled = true;
                folderBtn.Enabled = true;
                comboBox1.Enabled = true;
                comboBox1.SelectedIndex = 0;
            }
            else
            {
                textBox1.BackColor = Color.Red;
                fileBtn.Enabled = false;
                folderBtn.Enabled = false;
                comboBox1.Enabled = false;
                comboBox1.SelectedIndex = -1;
                comboBox1.Text = null;
            }
        }

        private void folderBtn_Click(object sender, EventArgs e)
        {
            fileBtn_Click(folderBtn, null);
        }
    }
}