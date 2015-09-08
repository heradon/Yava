﻿
// Yava.
// A simple, portable game/rom launcher.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Yava.Controls;
using Yava.FoldersFile;

namespace Yava
{
    [System.ComponentModel.DesignerCategory("")]
    internal class Yava : Form
    {
        // gui components:
        private readonly DoubleBufferedListView foldersListView;
        private readonly DoubleBufferedListView filesListView;

        // settings:
        private readonly String settingsFilepath;

        // folders:
        private readonly String foldersFilepath;
        private readonly FoldersFileReader foldersFileReader;

        /// <summary>
        /// Yava implementation.
        /// </summary>
        /// <param name="settingsFilepath">
        /// Path to the settings file to use.
        /// </param>
        /// <param name="foldersFilepath">
        /// Path to the folders file to use.
        /// </param>
        public Yava(String settingsFilepath, String foldersFilepath)
        {
            // this form:
            DoubleBuffered = true;
            MinimumSize = new Size(640, 480);
            ResizeEnd += OnResizeEnd;
            Text = "Yava launcher";

            // gui components:
            foldersListView = new DoubleBufferedListView();
            foldersListView.Columns.Add("Folders");
            foldersListView.Dock = DockStyle.Left;
            foldersListView.Font = new Font("Verdana", 9);
            foldersListView.FullRowSelect = true;
            foldersListView.ShowItemToolTips = true;
            foldersListView.View = View.Details;
            foldersListView.Width = 200;

            filesListView = new DoubleBufferedListView();
            filesListView.Columns.Add("Files");
            filesListView.Dock = DockStyle.Fill;
            filesListView.Font = new Font("Verdana", 9);
            filesListView.FullRowSelect = true;
            filesListView.ShowItemToolTips = true;
            filesListView.View = View.Details;
            filesListView.Width = 400;

            Splitter splitter = new Splitter();
            splitter.Dock = DockStyle.Left;
            splitter.SplitterMoved += OnSplitterMoved;

            Controls.Add(filesListView);
            Controls.Add(splitter);
            Controls.Add(foldersListView);

            // settings:
            this.settingsFilepath = settingsFilepath;

            // folders:
            this.foldersFilepath = foldersFilepath;
            this.foldersFileReader = new FoldersFileReader();

            // load folders and resize:
            LoadFolders();
            ListViewsResize();

            // wire listviews events:
            foldersListView.ItemSelectionChanged += OnFoldersListViewItemSelectionChanged;
        }

        ///
        /// Listviews:
        ///

        /// <summary>
        /// Resize the files and folder ListView first column according
        /// to their content.
        /// </summary>
        private void ListViewsResize()
        {
            foldersListView.BeginUpdate();
            filesListView.BeginUpdate();

            foldersListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.None);
            foldersListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

            filesListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.None);
            filesListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

            // ensure that the column headers are visible:
            foldersListView.Columns[0].Width = Math.Max(foldersListView.Columns[0].Width, 150);
            filesListView.Columns[0].Width = Math.Max(filesListView.Columns[0].Width, 150);

            foldersListView.EndUpdate();
            filesListView.EndUpdate();
        }

        /// <summary>
        /// Clear and disable both the folders and the files listviews.
        /// </summary>
        private void ListViewsDisable()
        {
            foldersListView.Items.Clear();
            filesListView.Items.Clear();

            foldersListView.Enabled = false;
            filesListView.Enabled = false;
        }

        ///
        /// Folders file:
        ///

        /// <summary>
        /// Open the folders file with the default program
        /// associated to the extension.
        /// </summary>
        private void FoldersFileOpen()
        {
            try
            {
                Process.Start(foldersFilepath);
            }
            catch (Exception exception)
            {
                String text = exception.Message;
                String caption = "Error openning folders file";
                MessageBox.Show(text, caption);
            }
        }

        ///
        /// Loading content:
        ///

        /// <summary>
        /// Open the folders file and parse the content
        /// adding each folder to the folders listview.
        /// </summary>
        private void LoadFolders()
        {
            List<ListViewItem> items = new List<ListViewItem>();

            try
            {
                List<Folder> folders = new List<Folder>();
                foldersFileReader.Read(foldersFilepath, folders);

                foreach (Folder folder in folders)
                {
                    ListViewItem item = new ListViewItem();

                    item.Tag = folder;
                    item.Text = folder.Name;
                    item.ToolTipText = folder.Path;

                    items.Add(item);
                }
            }

            // syntax or parsing error
            // show details and ask the user to edit:
            catch (FoldersFileReadError exception)
            {
                String text = String.Format(
                    "{0} \n" +
                    "Error at line {1}: {2} \n\n" +
                    "{3} \n\n" +
                    "Edit the file and press F5 to refresh. \n" +
                    "Do you want to open the folders file now?",
                    exception.FilePath,
                    exception.LineNumber,
                    exception.Message,
                    exception.Line
                );

                String caption = "Error reading folders file";
                if (Util.MessageBoxYesNo(text, caption))
                {
                    FoldersFileOpen();
                }
            }

            // io error
            // show the exception message:
            catch (Exception exception)
            {
                String caption = "Error opening folders file";
                String text = exception.Message;

                MessageBox.Show(text, caption, MessageBoxButtons.OK);
            }

            // clear previous content and update the listview:
            foldersListView.BeginUpdate();
            foldersListView.Items.Clear();
            foldersListView.Items.AddRange(items.ToArray());
            foldersListView.EndUpdate();
        }

        /// <summary>
        /// Populate the files listview using the selected
        /// folders files.
        /// </summary>
        private void LoadFiles()
        {
            List<ListViewItem> items = new List<ListViewItem>();
 
            try
            {
                foreach (ListViewItem selectedItem in foldersListView.SelectedItems)
                {
                    Folder folder = selectedItem.Tag as Folder;

                    foreach (String filepath in folder.GetFiles())
                    {
                        ListViewItem item = new ListViewItem();

                        item.Text = Path.GetFileName(filepath);
                        item.Tag = filepath;
                        item.ToolTipText = filepath;

                        items.Add(item);
                    }
                }
            }
            
            // io error
            // show the exception message:
            catch (Exception exception)
            {
                String caption = "Error loading folder files";
                String text = exception.Message;

                MessageBox.Show(text, caption, MessageBoxButtons.OK);
            }

            // clear previous content and update the listview:
            filesListView.BeginUpdate();
            filesListView.Items.Clear();
            filesListView.Items.AddRange(items.ToArray());
            filesListView.EndUpdate();
        }

        ///
        /// Events: resizing
        ///

        /// <summary>
        /// When the form is resized, auto-resize the ListViews column.
        /// </summary>
        private void OnResizeEnd(Object sender, EventArgs e)
        {
            ListViewsResize();
        }

        /// <summary>
        /// When the spliiter is moved, auto-resize the ListViews column.
        /// </summary>
        private void OnSplitterMoved(Object sender, EventArgs e)
        {
            ListViewsResize();
        }

        ///
        /// Events: listviews
        ///

        /// <summary>
        /// When the folder selection changes, load the appropriate files list.
        /// </summary>
        private void OnFoldersListViewItemSelectionChanged(Object sender, EventArgs e)
        {
            LoadFiles();
            ListViewsResize();
        }
    }
}

