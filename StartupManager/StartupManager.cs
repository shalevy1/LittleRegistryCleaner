﻿/*
    Little Registry Cleaner
    Copyright (C) 2008 Little Apps (http://www.littleapps.co.cc/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Common_Tools.TreeViewAdv.Tree;

namespace Little_Registry_Cleaner.StartupManager
{
    public partial class StartupManager : Form
    {
        private TreeModel treeModel = new TreeModel();

        public StartupManager()
        {
            InitializeComponent();
        }

        private void StartupManager_Load(object sender, EventArgs e)
        {
            this.treeViewAdv1.Model = this.treeModel;
            LoadStartupFiles();
        }

        /// <summary>
        /// Loads files that load on startup
        /// </summary>
        private void LoadStartupFiles()
        {
            // Clear old list
            this.treeModel.Nodes.Clear();

            // Adds registry keys
            try
            {
                // all user keys
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunEx"));
                LoadRegistryAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"));

                // current user keys
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunEx"));
                LoadRegistryAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"));
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            // Adds startup folders
            AddStartupFolder(Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP));
            AddStartupFolder(Utils.GetSpecialFolderPath(Utils.CSIDL_COMMON_STARTUP));

            // Expands treeview
            this.treeViewAdv1.ExpandAll();
            this.treeViewAdv1.AutoResizeColumns();
        }

        /// <summary>
        /// Loads registry sub key into tree view
        /// </summary>
        private void LoadRegistryAutoRun(RegistryKey regKey)
        {

            if (regKey == null)
                return;

            if (regKey.ValueCount <= 0)
                return;

            StartupManagerNode nodeRoot = new StartupManagerNode();

            nodeRoot.Section = regKey.Name;   

            foreach (string strItem in regKey.GetValueNames())
            {
                string strFilePath = regKey.GetValue(strItem) as string;

                if (!string.IsNullOrEmpty(strFilePath))
                {
                    // Get file arguments
                    string strFile = "", strArgs = "";
                    if (!Utils.FileExists(strFilePath))
                    {
                        Utils.ExtractArguments(strFilePath, out strFile, out strArgs);

                        if (!Utils.FileExists(strFile))
                        {
                            strFile = strArgs = "";
                            Utils.ExtractArguments(strFilePath, out strFile, out strArgs);
                        }
                    }
                    else
                        strFile = string.Copy(strFilePath);

                    StartupManagerNode node = new StartupManagerNode();

                    node.Item = strItem;
                    node.Path = strFile;
                    node.Args = strArgs;

                    Icon ico = Utils.ExtractIcon(strFile, true);
                    if (ico != null)
                        node.Image = (Image)ico.ToBitmap().Clone();
                    else
                        node.Image = (Image)SystemIcons.WinLogo.ToBitmap();

                    nodeRoot.Nodes.Add(node);
                }
            }

            this.treeModel.Nodes.Add(nodeRoot);
        }

        /// <summary>
        /// Loads startup folder into tree view
        /// </summary>
        private void AddStartupFolder(string strFolder)
        {

            try
            {
                if (string.IsNullOrEmpty(strFolder) || !Directory.Exists(strFolder))
                    return;

                StartupManagerNode nodeRoot = new StartupManagerNode();
                nodeRoot.Section = strFolder;

                foreach (string strShortcut in Directory.GetFiles(strFolder))
                {
                    string strShortcutName = Path.GetFileName(strShortcut);
                    string strFilePath, strFileArgs;

                    if (Path.GetExtension(strShortcut) == ".lnk")
                    {
                        Utils.ResolveShortcut(strShortcut, out strFilePath, out strFileArgs);

                        StartupManagerNode node = new StartupManagerNode();
                        node.Item = strShortcutName;
                        node.Path = strFilePath;
                        node.Args = strFileArgs;

                        Icon ico = Utils.ExtractIcon(strFilePath, true);
                        if (ico != null)
                            node.Image = (Image)ico.ToBitmap().Clone();
                        else
                            node.Image = (Image)SystemIcons.WinLogo.ToBitmap();

                        nodeRoot.Nodes.Add(node);
                    }
                }

                if (nodeRoot.Nodes.Count <= 0)
                    return;

                this.treeModel.Nodes.Add(nodeRoot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            LoadStartupFiles();
        }

        private void toolStripButtonAdd_Click(object sender, EventArgs e)
        {
            NewRunItem nrv = new NewRunItem();
            if (nrv.ShowDialog(this) == DialogResult.OK)
                this.LoadStartupFiles();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (this.treeViewAdv1.SelectedNodes.Count > 0)
            {
                if (MessageBox.Show(this, "Are you sure you want to remove this startup program?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    StartupManagerNode node = this.treeViewAdv1.SelectedNode.Tag as StartupManagerNode;

                    string strSection = (node.Parent as StartupManagerNode).Section;

                    if (Directory.Exists(strSection))
                    {
                        // Startup folder
                        string strPath = Path.Combine(strSection, node.Item);

                        if (File.Exists(strPath))
                            File.Delete(strPath);
                    }
                    else
                    {
                        // Registry key
                        string strMainKey = strSection.Substring(0, strSection.IndexOf('\\'));
                        string strSubKey = strSection.Substring(strSection.IndexOf('\\') + 1);
                        RegistryKey rk = Utils.RegOpenKey(strMainKey, strSubKey, true);

                        if (rk != null)
                            rk.DeleteValue(node.Item);

                        rk.Close();
                    }

                    MessageBox.Show(this, "Removed selected startup program", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }

            LoadStartupFiles();
        }

        private void toolStripButtonEdit_Click(object sender, EventArgs e)
        {
            if (this.treeViewAdv1.SelectedNodes.Count > 0)
            {
                StartupManagerNode node = this.treeViewAdv1.SelectedNode.Tag as StartupManagerNode;

                string strSection = (node.Parent as StartupManagerNode).Section;

                EditRunItem frmEditRunItem = new EditRunItem(node.Item, strSection, node.Path, node.Args);
                if (frmEditRunItem.ShowDialog(this) == DialogResult.OK)
                    LoadStartupFiles();
            }
        }

        private void toolStripButtonView_Click(object sender, EventArgs e)
        {
            if (this.treeViewAdv1.SelectedNodes.Count > 0)
            {
                string strItem = (this.treeViewAdv1.SelectedNode.Tag as StartupManagerNode).Item;
                string strPath = (this.treeViewAdv1.SelectedNode.Parent.Tag as StartupManagerNode).Section;

                if (strPath.StartsWith("HKEY"))
                {
                    RegEditGo.GoTo(strPath, strItem);
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", strPath);
                }
            }
        }

        private void toolStripButtonRun_Click(object sender, EventArgs e)
        {
            if (this.treeViewAdv1.SelectedNodes.Count > 0)
            {
                if (MessageBox.Show(this, "Are you sure you want to run this program?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string strFilepath = (this.treeViewAdv1.SelectedNode.Tag as StartupManagerNode).Path;
                    string strFileArgs = (this.treeViewAdv1.SelectedNode.Tag as StartupManagerNode).Args;

                    System.Diagnostics.Process.Start(strFilepath, strFileArgs);

                    MessageBox.Show(this, "Attempted to run: " + strFilepath, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    #region "Startup Manager Node"
    class StartupManagerNode : Node
    {
        private string strSection = "";
        public string Section
        {
            get { return strSection; }
            set { strSection = value; }
        }

        private string strItem = "";
        public string Item
        {
            get { return strItem; }
            set { strItem = value; }
        }

        private string strPath = "";
        public string Path
        {
            get { return strPath; }
            set { strPath = value; }
        }

        private string strArgs = "";
        public string Args
        {
            get { return strArgs; }
            set { strArgs = value; }
        }

        private string _path = "";
        public string ItemPath
        {
            get { return _path; }
            set { _path = value; }
        }

        public StartupManagerNode() : base()
        {

        }
    }
    #endregion
}