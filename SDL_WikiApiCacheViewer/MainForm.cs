using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SDL_HelpBotLibrary.SDLWikiApi;

namespace SDL_WikiApiCacheViewer
{
    public partial class MainForm : Form
    {
        private const int FILE_LOAD_CHUNK_LENGTH = 4096;
        private bool loadingFile = false;
        private bool isLoaded = false;
        private SDLWikiApiCache _cache = new SDLWikiApiCache();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateFromLoadingState();
        }

        private void toolStripMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if(e.ClickedItem == toolButtonOpen)
            {
                var openFileDialog = new OpenFileDialog()
                {
                    Title = "Open SDLWikiApiCache File",
                    Filter = "SDLWikiApiCache JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    FileName = "SDLWikiApiCacheFile.json",
                    CheckFileExists = true, 
                    RestoreDirectory = true
                };

                if(DialogResult.OK == openFileDialog.ShowDialog(this))
                {
                    Task.Run(() => LoadFile(openFileDialog.FileName));
                }
            }
        }

        private void UpdateFromLoadingState()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateFromLoadingState));
            }
            else
            {
                splitContainer1.Visible = !loadingFile && isLoaded;
                listView.Enabled = !loadingFile && isLoaded;
                textBox.Enabled = !loadingFile && isLoaded;
                toolButtonOpen.Enabled = !loadingFile;
            }
        }

        private void LoadFile(string filePath)
        {
            loadingFile = true;
            UpdateFromLoadingState();

            StringBuilder stringBuilder = new StringBuilder();
            char[] readBuffer = new char[FILE_LOAD_CHUNK_LENGTH];
            FileInfo fileInfo = new FileInfo(filePath);
            int progressMax = (int)(fileInfo.Length / FILE_LOAD_CHUNK_LENGTH);
            Progress(0, 0, progressMax);
            Status($"Loading '{Path.GetFileName(filePath)}', please wait...");

            using (StreamReader reader = File.OpenText(filePath))
            {
                for(int chunkCount = 0; !reader.EndOfStream; chunkCount++)
                {
                    Thread.Yield();
                    int bytesRead = reader.Read(readBuffer, 0, FILE_LOAD_CHUNK_LENGTH);
                    stringBuilder.Append(readBuffer, 0, bytesRead);
                    Progress(chunkCount, maximum: progressMax);
                }
            }

            _cache = JsonConvert.DeserializeObject<SDLWikiApiCache>(stringBuilder.ToString());

            loadingFile = false;
            isLoaded = true;
            Status($"Finished loading '{Path.GetFileName(filePath)}'.");

            UpdateListView();
            UpdateFromLoadingState();
        }

        private void UpdateListView()
        {
            if (_cache == null) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateListView));
                return;
            }

            listView.BeginUpdate();

            listView.Columns.Clear();
            listView.Items.Clear();

            ColumnHeader[] columns = new ColumnHeader[]
            {
                new ColumnHeader() { Text = "Name", Width = 255 },
                new ColumnHeader() { Text = "Categories", Width = 228 },
                new ColumnHeader() { Text = "URI", Width = 336 }
            };
            listView.Columns.AddRange(columns);

            foreach(SDLWikiApiItem wikiItem in _cache.Enumerate())
            {
                ListViewItem listViewItem = new ListViewItem(wikiItem.Name)
                {
                    Tag = wikiItem
                };
                listViewItem.SubItems.Add(string.Join(", ", wikiItem.Categories));
                listViewItem.SubItems.Add(wikiItem.Uri.AbsoluteUri);
                listView.Items.Add(listViewItem);
            }

            listView.EndUpdate();
        }

        private void Status(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Status), new object[] { message });
            }
            else
            {
                toolStripStatusLabel.Text = message;
                statusStripMain.Update();
            }
        }

        private void Progress(int value, int minimum = 0, int maximum = 100)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int, int>(Progress), new object[] { value, minimum, maximum });
            }
            else
            {
                statusStripProgressBar.Minimum = minimum;
                statusStripProgressBar.Maximum = maximum;
                statusStripProgressBar.Value = value;
                statusStripMain.Update();
            }
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = listView.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
            if (selectedItem != null && selectedItem.Tag != null && selectedItem.Tag is SDLWikiApiItem)
            {
                SDLWikiApiItem wikiItem = selectedItem.Tag as SDLWikiApiItem;
                textBox.Text = wikiItem.RawText;
            }
            else
            {
                textBox.Text = string.Empty;
            }
        }
    }
}
