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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;

namespace pwiz.SkylineTestUtil
{
    public abstract class AbstractFunctionalTest
    {
        private const int SLEEP_INTERVAL = 100;
        private const int WAIT_TIME = 30000;    // 30 seconds
        private const int WAIT_CYCLES = WAIT_TIME/SLEEP_INTERVAL;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }
        private string[] _testFilesZips;
        public string TestFilesZip
        {
            get { return _testFilesZips[0]; }
            set { TestFilesZipPaths = new[] {value}; }
        }

        public string[] TestFilesZipPaths
        {
            get { return _testFilesZips; }
            set
            {
                string[] zipPaths = value;
                _testFilesZips = new string[zipPaths.Length];
                for (int i = 0; i < zipPaths.Length; i++)
                {
                    var zipPath = zipPaths[i];
                    if (zipPath.Substring(0, 8).ToLower().Equals("https://") || zipPath.Substring(0, 7).ToLower().Equals("http://"))
                    {
                        WebClient webClient = new WebClient();
                        string fileName = zipPath.Substring(zipPath.LastIndexOf('/') + 1);
                        string zipFileTestPath = Path.Combine(TestContext.TestDir, fileName);
                        webClient.DownloadFile(zipPath, zipFileTestPath);
                        _testFilesZips[i] = zipFileTestPath;
                    }
                    else
                        _testFilesZips[i] = zipPath;
                    
                }
            }
        }

        public string TestDirectoryName { get; set; }
        public TestFilesDir TestFilesDir
        {
            get { return TestFilesDirs[0]; }
            set { TestFilesDirs = new[] {value}; }
        }
        public TestFilesDir[] TestFilesDirs { get; set; }

        private readonly List<Exception> _testExceptions = new List<Exception>();
        private bool _testCompleted;

        public static SkylineWindow SkylineWindow { get { return Program.MainWindow; } }

        protected static T ShowDialog<T>(Action act) where T : Form
        {
            SkylineWindow.BeginInvoke(act);
            T dlg = WaitForOpenForm<T>();
            Assert.IsNotNull(dlg);
            return dlg;
        }

        protected static void RunUI(Action act)
        {
            SkylineWindow.Invoke(act);
        }

        protected static void RunDlg<T>(Action show, Action<T> act) where T : Form
        {
            RunDlg(show, false, act);
        }

        protected static void RunDlg<T>(Action show, bool waitForDocument, Action<T> act) where T : Form
        {
            var doc = SkylineWindow.Document;
            T dlg = ShowDialog<T>(show);
            RunUI(() => act(dlg));
            WaitForClosedForm(dlg);
            if (waitForDocument)
                WaitForDocumentChange(doc);
        }
        
        protected static void SelectNode(SrmDocument.Level level, int iNode)
        {
            var pathSelect = SkylineWindow.Document.GetPathTo((int)level, iNode);
            RunUI(() => SkylineWindow.SequenceTree.SelectedPath = pathSelect);
        }

        public static T FindOpenForm<T>() where T : Form
        {
            while (true) {
                try
                {
                    foreach (var form in Application.OpenForms)
                    {
                        var tForm = form as T;
                        if (tForm != null && tForm.Created)
                        {
                            return tForm;
                        }
                    }
                    return null;
                }
                catch (InvalidOperationException)
                {
                    // Application.OpenForms might be modified during the iteration.
                    // If that happens, go through the list again.
                }
            }
        }

        public static T WaitForOpenForm<T>() where T : Form
        {
            for (int i = 0; i < WAIT_CYCLES; i ++ )
            {
                T tForm = FindOpenForm<T>();
                if (tForm != null)
                    return tForm;

                Thread.Sleep(SLEEP_INTERVAL);
            }
            return null;
        }

        public static bool IsFormOpen(Form form)
        {
            foreach (var formOpen in Application.OpenForms)
            {
                if (ReferenceEquals(form, formOpen))
                {
                    return true;
                }
            }
            return false;
        }

        public static void WaitForClosedForm(Form formClose)
        {
            for (int i = 0; i < WAIT_CYCLES; i++)
            {
                bool isOpen = true;
                Program.MainWindow.Invoke(new Action(() => isOpen = IsFormOpen(formClose)));
                if (!isOpen)
                    return;
                Thread.Sleep(SLEEP_INTERVAL);
            }
        }

        public static SrmDocument WaitForDocumentChange(SrmDocument docCurrent)
        {
            // Make sure the document changes on the UI thread, since tests are mostly
            // interested in interacting with the document on the UI thread.
            Assert.IsTrue(WaitForConditionUI(() => !ReferenceEquals(docCurrent, SkylineWindow.DocumentUI)));
            return SkylineWindow.Document;
        }

        public static bool WaitForCondition(Func<bool> func)
        {
            return WaitForCondition(WAIT_TIME, func);
        }

        public static bool WaitForCondition(int millis, Func<bool> func)
        {
            int waitCycles = millis/SLEEP_INTERVAL;
            for (int i = 0; i < waitCycles; i ++)
            {
                if (func())
                    return true;
                Thread.Sleep(SLEEP_INTERVAL);
            }
            return false;
        }

        public static bool WaitForConditionUI(Func<bool> func)
        {
            for (int i = 0; i < WAIT_CYCLES; i++)
            {
                bool isCondition = false;
                Program.MainWindow.Invoke(new Action(() => isCondition = func()));                
                if (isCondition)
                    return true;
                Thread.Sleep(SLEEP_INTERVAL);
            }
            return false;
        }

        public static void WaitForGraphs()
        {
            WaitForConditionUI(() => !SkylineWindow.IsGraphUpdatePending);
        }

        public static void OkDialog(Form form, Action okAction)
        {
            form.Invoke(okAction);
            WaitForClosedForm(form);
        }

        /// <summary>
        /// Starts up Skyline, and runs the <see cref="DoTest"/> test method.
        /// </summary>
        protected void RunFunctionalTest()
        {
            var threadTest = new Thread(WaitForSkyline);
            threadTest.Start();
            Program.Main();
            threadTest.Join();
            if (_testExceptions.Count > 0)
            {
                Assert.Fail(_testExceptions[0].ToString());
            }
            Assert.IsTrue(_testCompleted);
        }

        private void WaitForSkyline()
        {
            try
            {
                while (Program.MainWindow == null || !Program.MainWindow.IsHandleCreated)
                {
                    Thread.Sleep(SLEEP_INTERVAL);
                }
                Settings.Default.Reset();

                if (TestFilesZipPaths == null)
                {
                    RunTest();
                }
                else
                {
                    TestFilesDirs = new TestFilesDir[TestFilesZipPaths.Length];
                    for (int i = 0; i < TestFilesZipPaths.Length; i++)
                    {
                        TestFilesDirs[i] = new TestFilesDir(TestContext, TestFilesZipPaths[i], TestDirectoryName);
                    }
                    RunTest();
                    foreach(TestFilesDir dir in TestFilesDirs)
                        dir.Dispose();
                }
            }
            catch (Exception x)
            {
                // An exception occurred outside RunTest
                _testExceptions.Add(x);
                EndTest();
            }
        }

        private void RunTest()
        {
            // Clean-up before running the test
            RunUI(() => 
                {
                    SkylineWindow.SequenceTree.InTest = true;
                    SkylineWindow.ModifyDocument("Set test settings",
                                                 doc => doc.ChangeSettings(SrmSettingsList.GetDefault()));
                });
            
            DoTest();
            EndTest();
        }

        private void EndTest()
        {
            var skylineWindow = Program.MainWindow;
            if (skylineWindow == null || !IsFormOpen(skylineWindow))
                return;

            // Release all resources by setting the document to something that
            // holds no file handles.
            var docNew = new SrmDocument(SrmSettingsList.GetDefault());
            RunUI(() => SkylineWindow.SwitchDocument(docNew, null));
            // Close the Skyline window
            _testCompleted = true;
            skylineWindow.Invoke(new Action(skylineWindow.Close));                        
        }

        protected abstract void DoTest();

        #region Modification helpers

        public static PeptideSettingsUI ShowPeptideSettings()
        {
            return ShowDialog<PeptideSettingsUI>(SkylineWindow.ShowPeptideSettingsUI);
        }

        public static EditListDlg<SettingsListBase<StaticMod>, StaticMod> ShowEditStaticModsDlg(PeptideSettingsUI peptideSettingsUI)
        {
            return ShowDialog<EditListDlg<SettingsListBase<StaticMod>, StaticMod>>(peptideSettingsUI.EditStaticMods);
        }

        public static EditListDlg<SettingsListBase<StaticMod>, StaticMod> ShowEditHeavyModsDlg(PeptideSettingsUI peptideSettingsUI)
        {
            return ShowDialog<EditListDlg<SettingsListBase<StaticMod>, StaticMod>>(peptideSettingsUI.EditHeavyMods);
        }

        public static EditStaticModDlg ShowAddModDlg(EditListDlg<SettingsListBase<StaticMod>, StaticMod> editModsDlg)
        {
            return ShowDialog<EditStaticModDlg>(editModsDlg.AddItem);
        }

        public static void AddStaticMod(StaticMod mod, PeptideSettingsUI peptideSettingsUI)
        {
            var editStaticModsDlg = ShowEditStaticModsDlg(peptideSettingsUI);
            AddMod(mod, editStaticModsDlg);
        }

        public static void AddHeavyMod(StaticMod mod, PeptideSettingsUI peptideSettingsUI)
        {
            var editStaticModsDlg = ShowEditHeavyModsDlg(peptideSettingsUI);
            AddMod(mod, editStaticModsDlg);
        }

        private static void AddMod(StaticMod mod, EditListDlg<SettingsListBase<StaticMod>, StaticMod> editModsDlg)
        {
            var addStaticModDlg = ShowAddModDlg(editModsDlg);
            RunUI(() =>
            {
                addStaticModDlg.Modification = mod;
                addStaticModDlg.OkDialog();
            });
            WaitForClosedForm(addStaticModDlg);

            RunUI(editModsDlg.OkDialog);
            WaitForClosedForm(editModsDlg);
        }

        public static void SetStaticModifications(Func<IList<string>, IList<string>> changeMods)
        {
            RunDlg<PeptideSettingsUI>(SkylineWindow.ShowPeptideSettingsUI, dlg =>
            {
                dlg.PickedStaticMods = changeMods(dlg.PickedStaticMods).ToArray();
                dlg.OkDialog();
            });
        }

        #endregion
    }
}