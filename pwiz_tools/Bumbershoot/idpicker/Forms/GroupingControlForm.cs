﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IDPicker.DataModel;

namespace IDPicker.Forms
{
    public partial class GroupingControlForm : Form
    {
        NHibernate.ISession session;
        private delegate void SortGroupsTreeDelegate();
        private Point groupsTreeViewRightClickPointToClient;
        List<TreeNode> _itemsRemoved = new List<TreeNode>();
        int _numberNewNodes = 0;

        public GroupingControlForm(NHibernate.ISession session)
        {
            InitializeComponent();

            this.session = session;

            var groupsDone = new List<SpectrumSourceGroup>();
            var spectra = session.QueryOver<SpectrumSource>().List<SpectrumSource>();
            Dictionary<int,List<SpectrumSource>> spectraDictionary = new Dictionary<int,List<SpectrumSource>>();

            foreach (SpectrumSource ss in spectra)
            {
                try
                {
                    if (!spectraDictionary.ContainsKey((int)ss.Group.Id))
                        spectraDictionary.Add((int)ss.Group.Id, new List<SpectrumSource>());
                    spectraDictionary[(int)ss.Group.Id].Add(ss);
                }
                catch
                {
                    MessageBox.Show("Groupless spectrum source detected");
                }
            }

            //add all normal groups
            var groups = session.QueryOver<SpectrumSourceGroup>().Where(g => g.Name != "Abandoned").List<SpectrumSourceGroup>();
            TreeNode groupNode = new TreeNode();

            groupNode.Tag = (from g in groups where g.Name == "/" select g).Single();
            groups.Remove(groupNode.Tag as SpectrumSourceGroup);
            groupNode.Text = (groupNode.Tag as SpectrumSourceGroup).Name;
            groupNode.ContextMenuStrip = cmRightClickGroupNode;

            AddAssociatedSpectra(ref groupNode, spectraDictionary);

            groupNode = FillBranch(groupNode, groups, spectraDictionary);

            tvGroups.Nodes.Add(groupNode);

            tvGroups.ExpandAll();

            //add abandoned groups
            var abandonedGroup = session.QueryOver<SpectrumSource>().JoinQueryOver(s => s.Group).Where(g => g.Name == "Abandoned").List<SpectrumSource>();
            if (abandonedGroup != null)
            {
                foreach (SpectrumSource ss in abandonedGroup)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = ss.Name;
                    lvi.Tag = ss;
                    lvNonGroupedFiles.Items.Add(lvi);
                }
            }
        }

        private void AddAssociatedSpectra(ref TreeNode groupNode, Dictionary<int, List<SpectrumSource>> spectraDictionary)
        {
            try
            {
                int groupID = (int)(groupNode.Tag as SpectrumSourceGroup).Id;

                if (spectraDictionary.ContainsKey(groupID))
                {
                    foreach (SpectrumSource ss in spectraDictionary[groupID])
                    {
                        TreeNode newNode = new TreeNode();

                        newNode.Text = ss.Name;
                        newNode.Tag = ss;
                        newNode.ImageIndex = 1;
                        newNode.SelectedImageIndex = 1;
                        newNode.ContextMenuStrip = cmRightClickFileNode;
                        groupNode.Nodes.Add(newNode);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Could not add spectra to branch");
            }
        }

        private TreeNode FillBranch(TreeNode groupNode, IList<SpectrumSourceGroup> groups, Dictionary<int, List<SpectrumSource>> spectraDictionary)
        {
            List<SpectrumSourceGroup> potentialChildren;
            string fullPath = (groupNode.Tag as SpectrumSourceGroup).Name;

            if (fullPath == "/")
                potentialChildren = (from g in groups
                                     where g.Name != fullPath &&
                                     !g.Name.Remove(0,1).Contains("/")
                                     select g).ToList<SpectrumSourceGroup>();
            else
                potentialChildren = (from g in groups
                                     where g.Name.Contains(fullPath + "/") &&
                                     !g.Name.Remove(0,fullPath.Length+1).Contains("/")
                                     select g).ToList<SpectrumSourceGroup>();

            foreach (SpectrumSourceGroup ssg in potentialChildren)
            {
                TreeNode newNode = new TreeNode();
                newNode.Text = ssg.Name.Substring(ssg.Name.LastIndexOf("/")+1);
                newNode.Tag = ssg;
                newNode.ImageIndex = 0;
                newNode.SelectedImageIndex = 0;
                groups.Remove(newNode.Tag as SpectrumSourceGroup);
                newNode.ContextMenuStrip = cmRightClickGroupNode;

                AddAssociatedSpectra(ref newNode, spectraDictionary);

                groupNode.Nodes.Add(this.FillBranch(newNode, groups, spectraDictionary));
            }

            return groupNode;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            List<TreeNode> spectraLocations= new List<TreeNode>();
            List<TreeNode> groupsToSave = new List<TreeNode>();
            TreeNode tempNode;

            NHibernate.ITransaction transaction = session.BeginTransaction();

            //find all groups still present
            getSprectrumSourceGroupsRecursively(tvGroups.Nodes[0], groupsToSave);

            //remove old groups and links
            session.CreateQuery("delete SpectrumSourceGroupLink s").ExecuteUpdate();
            //(session.CreateSQLQuery("Delete from SpectrumSourceGroupLinks")).ExecuteUpdate();
            IList<SpectrumSourceGroup> unusedGroups = session.QueryOver<SpectrumSourceGroup>().List<SpectrumSourceGroup>();
            
            foreach (TreeNode tn in groupsToSave)
                unusedGroups.Remove((tn.Tag as SpectrumSourceGroup));
            foreach (SpectrumSourceGroup ssg in unusedGroups)
                session.Delete(ssg);
            
            session.Flush();

            //save group layout
            foreach (TreeNode treeNode in groupsToSave)
                session.SaveOrUpdate(treeNode.Tag as SpectrumSourceGroup);
            session.Flush();

            //get new spectra locations
            getListOfSprectrumSourcesRecursively(tvGroups.Nodes[0], ref spectraLocations);

            //save locations
            foreach (TreeNode tn in spectraLocations)
            {
                tempNode = tn;
                (session.CreateSQLQuery("Update spectrumsource set group_ = " +
                    (tn.Parent.Tag as SpectrumSourceGroup).Id + " where ID = " +
                    (tn.Tag as SpectrumSource).Id)).ExecuteUpdate();

                while (tempNode.Parent != null)
                {
                    SaveGroupLink((tempNode.Parent.Tag as SpectrumSourceGroup), (tn.Tag as SpectrumSource));
                    tempNode = tempNode.Parent;
                }

                
            }
            session.Flush();

            //save abandoned spectrum sources
            if (lvNonGroupedFiles.Items.Count > 0)
            {
                SpectrumSourceGroup abandonedGroup = session.QueryOver<SpectrumSourceGroup>()
                    .Where(g => g.Name == "Abandoned").SingleOrDefault();
                if (abandonedGroup == null)
                {
                    abandonedGroup = new SpectrumSourceGroup();
                    abandonedGroup.Name = "Abandoned";
                    session.SaveOrUpdate(abandonedGroup);
                }
                foreach (ListViewItem lvi in lvNonGroupedFiles.Items)
                {
                    (session.CreateSQLQuery("Update spectrumsource set group_ = " +
                        abandonedGroup.Id + " where ID = " +
                        (lvi.Tag as SpectrumSource).Id)).ExecuteUpdate();
                }

                session.Flush();
            }

            transaction.Commit();

        }

        private void SaveGroupLink(SpectrumSourceGroup ssg, SpectrumSource ss)
        {
            //HACK: The commented out code produced "NonUniqueObjectException"
            (session.CreateSQLQuery(String.Format("insert into spectrumsourcegrouplink (group_,source) values ({0} , {1})", ssg.Id, ss.Id))).ExecuteUpdate();

            //SpectrumSourceGroupLink ssgl = new SpectrumSourceGroupLink();
            //ssgl.Group = ssg;
            //ssgl.Source = ss;

            //session.SaveOrUpdate(ssgl);
            //session.Flush();
        }

        /// <summary>
        /// DragDropEffects.Move only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvGroups_ItemDrag(object sender, ItemDragEventArgs e)
        {
            try
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
            catch (Exception exc)
            {
                //HandleExceptions(exc, ExceptionsDialogForm.ExceptionType.Error);
                MessageBox.Show(exc.ToString());
            }
        }

        /// <summary>
        /// Sort treeview after adding nodes - see GroupNodeSorter class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvGroups_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                // for some reason the default group name is not copied to the
                // e.label property in the event
                // so this is case for user adds node and accepts default name
                if (e.Label == null && e.Node.Text.StartsWith("New Group"))
                {
                    if (e.Node.Tag is SpectrumSourceGroup)
                        (e.Node.Tag as SpectrumSourceGroup).Name = e.Node.Text;
                    BeginInvoke(new SortGroupsTreeDelegate(sortGroupsTreeView));
                }
                else if (e.Label != null)
                {
                    if (e.Label != string.Empty && e.Label.IndexOfAny(new char[] { '/' }) == -1)
                    {
                        if (e.Node.Tag is SpectrumSourceGroup)
                            (e.Node.Tag as SpectrumSourceGroup).Name = e.Node.Text;
                        BeginInvoke(new SortGroupsTreeDelegate(sortGroupsTreeView));
                    }
                    else
                    {
                        e.CancelEdit = true;

                        throw new Exception("Invalid group name.\r\n\r\nGroup names cannot be empty or contain the following chars: '/'.");
                    }
                }

            }
            catch (Exception exc)
            {
                //HandleExceptions(exc, ExceptionsDialogForm.ExceptionType.Error);
                MessageBox.Show(exc.ToString());
            }


        }

        /// <summary>
        /// Used for delegate sort of groups treeview (afterlabeledit)
        /// </summary>
        private void sortGroupsTreeView()
        {
            try
            {
                tvGroups.Sort();
            }
            catch (Exception exc)
            {
                throw new Exception("Error sorting groups treeview\r\n", exc);
            }

        }

        /// <summary>
        /// Drop from within treevew, Drop from listviewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvGroups_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Effect == DragDropEffects.Move)
            {

                if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", true))
                {
                    Point pt = tvGroups.PointToClient(new Point(e.X, e.Y));

                    TreeNode destNode = tvGroups.GetNodeAt(pt);

                    TreeNode dragNode = e.Data.GetData("System.Windows.Forms.TreeNode", true) as TreeNode;

                    //InputFileTag destNodeTag = (destNode.Tag as InputFileTag);
                    //InputFileTag dragNodeTag = (dragNode.Tag as InputFileTag);

                    TreeNode newNode = dragNode.Clone() as TreeNode;

                    if (destNode != null && dragNode != null)
                    {

                        destNode.Nodes.Add(newNode);

                        newNode.ExpandAll();

                        if (destNode.Nodes.Count == 1)
                            destNode.Expand();

                        dragNode.Remove();

                        tvGroups.Sort();
                    }

                }

                else if (e.Data.GetDataPresent("System.Windows.Forms.ListViewItem", true))
                {
                    Point pt = tvGroups.PointToClient(new Point(e.X, e.Y));

                    TreeNode destNode = tvGroups.GetNodeAt(pt);

                    if (destNode != null)
                    {
                        if (destNode.Tag is SpectrumSourceGroup)
                        {
                            foreach (ListViewItem lvi in lvNonGroupedFiles.SelectedItems)
                            {
                                TreeNode newNode = new TreeNode(lvi.Text);

                                newNode.Tag = lvi.Tag;
                                newNode.ImageIndex = 1;
                                newNode.SelectedImageIndex = 1;
                                newNode.ContextMenuStrip = cmRightClickFileNode;

                                destNode.Nodes.Add(newNode);

                                if (destNode.Nodes.Count == 1)
                                {
                                    destNode.Expand();
                                }

                                lvNonGroupedFiles.Items.Remove(lvi);
                            } //end foreach selected ListViewItem

                            tvGroups.Sort();
                        } //end check for destination as group
                    } // end check for destination exists
                } //end check for coming from list box
            } // end check for movement available
        }

        /// <summary>
        /// Drag over tvGroups - Drag from within, Drag from listviewbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvGroups_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", true))
                {
                    Point pt = tvGroups.PointToClient(new Point(e.X, e.Y));

                    TreeNode destNode = tvGroups.GetNodeAt(pt);

                    TreeNode dragNode = e.Data.GetData("System.Windows.Forms.TreeNode", true) as TreeNode;

                    TreeNode newNode = dragNode.Clone() as TreeNode;

                    if (destNode != null && dragNode != null && destNode != dragNode)
                    {
                        //InputFileTag destTag = (destNode.Tag as InputFileTag);
                        //InputFileTag dragTag = (dragNode.Tag as InputFileTag);

                        // cannot drop onto files
                        if (destNode.Tag is SpectrumSource)
                        {
                            e.Effect = DragDropEffects.None;

                        }
                        else if (checkIfDestGroupAChildNodeOfMe(destNode, dragNode))
                        {
                            e.Effect = DragDropEffects.None;
                        }
                        else
                        {
                            e.Effect = DragDropEffects.Move;
                        }
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }

                }

                else if (e.Data.GetDataPresent("System.Windows.Forms.ListViewItem", true))
                {
                    Point pt = tvGroups.PointToClient(new Point(e.X, e.Y));

                    TreeNode destNode = tvGroups.GetNodeAt(pt);

                    if (destNode != null)
                    {
                        //InputFileTag destTag = destNode.Tag as InputFileTag;

                        if (destNode.Tag is SpectrumSourceGroup)
                        {
                            e.Effect = DragDropEffects.Move;
                        }
                        else
                        {
                            e.Effect = DragDropEffects.None;
                        }
                    }
                }

                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error handling drag and drop\r\n", exc);
            }

        }

        /// <summary>
        /// Allow delete (delete key) and rename (F2 key) of group nodes (not root or file)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvGroups_KeyDown(object sender, KeyEventArgs e)
        {
            TreeNode selNode = tvGroups.SelectedNode;

            try
            {
                if (e.KeyCode.Equals(Keys.Delete))
                {
                    RemoveGroupNode(selNode);
                }
                else if (e.KeyCode.Equals(Keys.F2) && (selNode.Tag is SpectrumSourceGroup))
                {
                    selNode.BeginEdit();
                }
            }
            catch (Exception exc)
            {
                //HandleExceptions(exc, ExceptionsDialogForm.ExceptionType.Error);
                MessageBox.Show(exc.ToString());
            }
        }

        /// <summary>
        /// Set location for determining node that was clicked on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvGroups_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    groupsTreeViewRightClickPointToClient = e.Location;
                }
            }
            catch (Exception exc)
            {
                //HandleExceptions(exc, ExceptionsDialogForm.ExceptionType.Error);
                MessageBox.Show(exc.ToString());
            }
        }


        /// <summary>
        /// Check to prevent group nodes from being dragged into their children
        /// group nodes.
        /// </summary>
        /// <returns></returns>
        private bool checkIfDestGroupAChildNodeOfMe(TreeNode destNode, TreeNode dragNode)
        {
            //InputFileTag destTag = (destNode.Tag as InputFileTag);
            //InputFileTag dragTag = (dragNode.Tag as InputFileTag);

            try
            {
                if (destNode.Tag is SpectrumSourceGroup && dragNode.Tag is SpectrumSourceGroup && destNode.Level > dragNode.Level)
                {
                    TreeNode currNode = destNode;

                    while (currNode.Parent != null)
                    {
                        if (currNode.Parent.Equals(dragNode))
                        {
                            return true;
                        }

                        currNode = currNode.Parent;
                    }

                    return false;

                }
                else
                {
                    return false;
                }

            }
            catch (Exception exc)
            {
                throw new Exception("Error validating group move: checkIfDestGroupAChildNodeOfMe()\r\n", exc);
            }

        }

        private void getListOfSprectrumSourcesRecursively(TreeNode treeNode, ref List<TreeNode> spectraNodes)
        {
            try
            {
                if (treeNode.Tag is SpectrumSource)
                {
                    spectraNodes.Add(treeNode);
                }
                else
                {
                    foreach (TreeNode subNode in treeNode.Nodes)
                    {
                        getListOfSprectrumSourcesRecursively(subNode, ref spectraNodes);
                    }
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error in retrieving spectra", exc);
            }

        }

        private void getSprectrumSourceGroupsRecursively(TreeNode treeNode, List<TreeNode> nodeList)
        {
            if (treeNode.Tag is SpectrumSourceGroup)
            {
                if (treeNode.FullPath.Length > 1)
                    (treeNode.Tag as SpectrumSourceGroup).Name = treeNode.FullPath.Replace("//","/").TrimEnd('/');
                else
                    (treeNode.Tag as SpectrumSourceGroup).Name = treeNode.FullPath;

                nodeList.Add(treeNode);

                foreach (TreeNode subNode in treeNode.Nodes)
                {
                    getSprectrumSourceGroupsRecursively(subNode, nodeList);
                }
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            
        }

        private void addGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selNode = tvGroups.GetNodeAt(groupsTreeViewRightClickPointToClient);

            _numberNewNodes++;

            SpectrumSourceGroup ssg = new SpectrumSourceGroup();
            ssg.Name = string.Format("New Group({0})", _numberNewNodes);
            TreeNode newNode = new TreeNode(string.Format("New Group({0})", _numberNewNodes));
            newNode.ImageIndex = 0;
            newNode.SelectedImageIndex = 0;
            newNode.ContextMenuStrip = cmRightClickGroupNode;
            newNode.Tag = ssg;

            selNode.Nodes.Add(newNode);
            newNode.BeginEdit();
        }

        private void GroupingControlForm_Load(object sender, EventArgs e)
        {
            ImageList imageList = new ImageList();

            imageList.Images.Add(Properties.Resources.XPfolder_closed);
            imageList.Images.Add(Properties.Resources.file);

            tvGroups.ImageList = imageList;
        }

        private void renameGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selNode = tvGroups.GetNodeAt(groupsTreeViewRightClickPointToClient);
            selNode.BeginEdit();
        }

        private void removeGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveGroupNode(tvGroups.GetNodeAt(groupsTreeViewRightClickPointToClient));
        }

        private void RemoveGroupNode(TreeNode selNode)
        {
            List<TreeNode> abandonedSpectraSources = new List<TreeNode>();
            getListOfSprectrumSourcesRecursively(selNode, ref abandonedSpectraSources);

            if (selNode.Parent != null)
                selNode.Parent.Nodes.Remove(selNode);
            else
                selNode.Nodes.Clear();

            foreach (TreeNode tn in abandonedSpectraSources)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = tn.Text;
                lvi.Tag = tn.Tag;
                lvNonGroupedFiles.Items.Add(lvi);
            }
        }

        private void RemoveFileNode(TreeNode selNode)
        {
            selNode.Parent.Nodes.Remove(selNode);

            ListViewItem lvi = new ListViewItem();
            lvi.Text = selNode.Text;
            lvi.Tag = selNode.Tag;
            lvNonGroupedFiles.Items.Add(lvi);
        }

        private void removeFileNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveFileNode(tvGroups.GetNodeAt(groupsTreeViewRightClickPointToClient));
        }

        private void lvNonGroupedFiles_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void lvNonGroupedFiles_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", true))
            {
                TreeNode dragNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");

                RemoveGroupNode(dragNode);

                tvGroups.Sort();
                //tvGroups.ExpandAll();

            }
        }

        private void lvNonGroupedFiles_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void lvNonGroupedFiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void lvNonGroupedFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                if (e.KeyCode == Keys.A)
                {
                    foreach (ListViewItem lvi in lvNonGroupedFiles.Items)
                    {
                        lvi.Selected = true;
                    }

                }

            }
        }

        private void tvGroups_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node.Text == "/")
            {
                MessageBox.Show("Cannot change name of root group");
                e.CancelEdit = true;
            }
        }

        private void miResetFiles_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to remove all groups?","Remove All",MessageBoxButtons.YesNo).Equals(DialogResult.Yes))
                RemoveGroupNode(tvGroups.Nodes[0]);
        }

        private void miExpandGroups_Click(object sender, EventArgs e)
        {
            if (tvGroups.SelectedNode != null)
            {
                if (tvGroups.SelectedNode.Tag is SpectrumSourceGroup)
                    tvGroups.SelectedNode.Expand();
            }
        }

        private void miCollapseGroups_Click(object sender, EventArgs e)
        {
            if (tvGroups.SelectedNode != null)
            {
                if (tvGroups.SelectedNode.Tag is SpectrumSourceGroup)
                    tvGroups.SelectedNode.Collapse();
            }
        }









    }
}