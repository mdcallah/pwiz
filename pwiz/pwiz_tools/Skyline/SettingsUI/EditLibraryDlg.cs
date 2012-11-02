﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.SettingsUI
{
    public partial class EditLibraryDlg : FormEx
    {
        private LibrarySpec _librarySpec;
        private readonly IEnumerable<LibrarySpec> _existing;

        public EditLibraryDlg(IEnumerable<LibrarySpec> existing)
        {
            _existing = existing;

            InitializeComponent();

            textName.Focus();
        }

        public LibrarySpec LibrarySpec
        {
            get { return _librarySpec; }
            
            set
            {
                _librarySpec = value;
                if (_librarySpec == null)
                {
                    textName.Text = string.Empty;
                    textPath.Text = string.Empty;
                }
                else
                {
                    textName.Text = _librarySpec.Name;
                    textPath.Text = _librarySpec.FilePath;
                }                
            }
        }

        public void OkDialog()
        {
            var e = new CancelEventArgs();
            var helper = new MessageBoxHelper(this);

            string name;
            if (!helper.ValidateNameTextBox(e, textName, out name))
                return;

            // Allow updating the original modification
            if (LibrarySpec == null || !Equals(name, LibrarySpec.Name))
            {
                // But not any other existing modification
                foreach (LibrarySpec mod in _existing)
                {
                    if (Equals(name, mod.Name))
                    {
                        helper.ShowTextBoxError(textName, Resources.EditLibraryDlg_OkDialog_The_library__0__already_exists, name);
                        return;
                    }
                }
            }

            LibrarySpec librarySpec;
            String path = textPath.Text;

            if (!File.Exists(path))
            {
                MessageBox.Show(this, string.Format(Resources.EditLibraryDlg_OkDialog_The_file__0__does_not_exist, path), Program.Name);
                textPath.Focus();
                return;
            }
            if (FileEx.IsDirectory(path))
            {
                MessageBox.Show(this, string.Format(Resources.EditLibraryDlg_OkDialog_The_path__0__is_a_directory, path), Program.Name);
                textPath.Focus();
                return;
            }
            
            // Display an error message if the user is trying to add a BiblioSpec library,
            // and the library has the text "redundant" in the file name.
            if (path.EndsWith(BiblioSpecLiteSpec.EXT_REDUNDANT))
            {
                var message = TextUtil.LineSeparate(string.Format(Resources.EditLibraryDlg_OkDialog_The_file__0__appears_to_be_a_redundant_library, path),
                                                    Resources.EditLibraryDlg_OkDialog_Please_choose_a_non_redundant_library);
                MessageDlg.Show(this, string.Format(message, path));
                textPath.Focus();
                return;
            }

            string ext = Path.GetExtension(path);
            if (Equals(ext, BiblioSpecLiteSpec.EXT))
                librarySpec = new BiblioSpecLiteSpec(name, path);
            else if (Equals(ext, BiblioSpecLibSpec.EXT))
                librarySpec = new BiblioSpecLibSpec(name, path);
            else if (Equals(ext, XHunterLibSpec.EXT))
                librarySpec = new XHunterLibSpec(name, path);
            else if (Equals(ext, NistLibSpec.EXT))
                librarySpec = new NistLibSpec(name, path);
            else if (Equals(ext, SpectrastSpec.EXT))
                librarySpec = new SpectrastSpec(name, path);
            else
            {
                MessageDlg.Show(this, string.Format(Resources.EditLibraryDlg_OkDialog_The_file__0__is_not_a_supported_spectral_library_file_format, path));
                textPath.Focus();
                return;
            }

            _librarySpec = librarySpec;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            string fileName = GetLibraryPath(this, null);
            if (fileName != null)
                textPath.Text = fileName;
        }

        private void textPath_TextChanged(object sender, EventArgs e)
        {
            // CONSIDER: Statement completion
            if (File.Exists(textPath.Text))
                textPath.ForeColor = Color.Black;
            else
                textPath.ForeColor = Color.Red;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public static string GetLibraryPath(IWin32Window parent, string fileName)
        {
            using (var dlg = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.LibraryDirectory,
                CheckPathExists = true,
                SupportMultiDottedExtensions = true,
                DefaultExt = BiblioSpecLibSpec.EXT,
                Filter = TextUtil.FileDialogFiltersAll(TextUtil.FileDialogFilter(Resources.EditLibraryDlg_GetLibraryPath_Spectral_Libraries, BiblioSpecLiteSpec.EXT, XHunterLibSpec.EXT, NistLibSpec.EXT, SpectrastSpec.EXT),
                                                       TextUtil.FileDialogFilter(Resources.EditLibraryDlg_GetLibraryPath_Legacy_Libraries, BiblioSpecLibSpec.EXT))
            })
            {
                if (fileName != null)
                    dlg.FileName = fileName;

                if (dlg.ShowDialog(parent) != DialogResult.OK)
                    return null;

                Settings.Default.LibraryDirectory = Path.GetDirectoryName(dlg.FileName);
                return dlg.FileName;
            }
        }

        private void linkPeptideAtlas_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SpecLibLinkClicked(linkPeptideAtlas, LibraryLink.PEPTIDEATLAS.Link);
        }

        private void linkNIST_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SpecLibLinkClicked(linkNIST, LibraryLink.NIST.Link);
        }

        private void linkGPM_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SpecLibLinkClicked(linkGPM, LibraryLink.GPM.Link);
        }

        private void SpecLibLinkClicked(LinkLabel linkLabel, string link)
        {
            linkLabel.LinkVisited = true;
            WebHelpers.OpenLink(this, link);
        }

        private void EditLibraryDlg_Load(object sender, EventArgs e)
        {

        }
    }
}