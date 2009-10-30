using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DigitalRune.Windows.Docking;
using MSGraph;
using ZedGraph;

using System.Diagnostics;
using SpyTools;

namespace seems
{
	public partial class GraphForm : DockableForm, IDataView
	{
        #region IDataView Members
        public IList<ManagedDataSource> Sources
        {
            get
            {
                List<ManagedDataSource> sources = new List<ManagedDataSource>();
                for( int i = 0; i < paneList.Count; ++i )
                {
                    Pane logicalPane = paneList[i];

                    foreach( GraphItem item in logicalPane )
                        sources.Add( item.Source );
                }
                return sources;
            }
        }

        public IList<GraphItem> DataItems
        {
            get
            {
                List<GraphItem> graphItems = new List<GraphItem>();
                for( int i = 0; i < paneList.Count; ++i )
                {
                    Pane logicalPane = paneList[i];

                    foreach( GraphItem item in logicalPane )
                        graphItems.Add( item );
                }
                return graphItems;
            }
        }
        #endregion

		public MSGraph.MSGraphControl ZedGraphControl { get { return msGraphControl; } }

        MSGraphPane focusedPane = null;
        CurveItem focusedItem = null;

        private EventSpy spy;

        /// <summary>
        /// Occurs when the FocusedItem property changes;
        /// usually caused by a left click near a different MSGraphItem
        /// </summary>
        public event EventHandler ItemGotFocus;

        private void OnItemGotFocus( GraphForm graphForm, EventArgs eventArgs )
        {
            if( ItemGotFocus != null )
                ItemGotFocus( graphForm, eventArgs );
        }

        private void setFocusedItem( CurveItem item )
        {
            if( item != focusedItem )
            {
                focusedItem = item;
                OnItemGotFocus( this, EventArgs.Empty );
            }
        }

        /// <summary>
        /// Gets the MSGraphPane that was last focused on within the MSGraphControl
        /// </summary>
        public MSGraphPane FocusedPane { get { return focusedPane; } }

        /// <summary>
        /// If FocusedPane has a single item, it will return that;
        /// If the last left mouse click was less than ZedGraph.GraphPane.Default.NearestTol
        /// from a point, it will return the item containing that point;
        /// Otherwise returns the first item in the FocusedPane
        /// </summary>
        public CurveItem FocusedItem { get { return focusedItem; } }

        private PaneList paneList;
        public PaneList PaneList
        {
            get { return paneList; }
            set
            {
                paneList = value;
                Refresh();
            }
        }

        private ZedGraph.PaneLayout paneLayout;
        public ZedGraph.PaneLayout PaneListLayout
        {
            get { return paneLayout; }
            set
            {
                ZedGraph.PaneLayout oldLayout = paneLayout;
                paneLayout = value;
                if( oldLayout != paneLayout )
                    Refresh();
            }
        }

		public GraphForm()
		{
			InitializeComponent();

            spy = new EventSpy( "GraphForm", this );
            //spy.DumpEvents( this.GetType() );
            spy.SpyEvent += new SpyEventHandler( seemsForm.LogSpyEvent );

            paneList = new PaneList();
            paneLayout = PaneLayout.SingleColumn;

            msGraphControl.MasterPane.InnerPaneGap = 1;
            msGraphControl.MouseDownEvent += new ZedGraphControl.ZedMouseEventHandler( msGraphControl_MouseDownEvent );
            msGraphControl.MouseMoveEvent += new ZedGraphControl.ZedMouseEventHandler( msGraphControl_MouseMoveEvent );

            msGraphControl.ZoomButtons = MouseButtons.Left;
            msGraphControl.ZoomModifierKeys = Keys.None;
            msGraphControl.ZoomButtons2 = MouseButtons.None;

            msGraphControl.UnzoomButtons = new MSGraphControl.MouseButtonClicks( MouseButtons.Middle );
            msGraphControl.UnzoomModifierKeys = Keys.None;
            msGraphControl.UnzoomButtons2 = new MSGraphControl.MouseButtonClicks( MouseButtons.None );

            msGraphControl.UnzoomAllButtons = new MSGraphControl.MouseButtonClicks( MouseButtons.Left, 2 );
            msGraphControl.UnzoomAllButtons2 = new MSGraphControl.MouseButtonClicks( MouseButtons.None );

            msGraphControl.PanButtons = MouseButtons.Left;
            msGraphControl.PanModifierKeys = Keys.Control;
            msGraphControl.PanButtons2 = MouseButtons.None;

            msGraphControl.ContextMenuBuilder += new MSGraphControl.ContextMenuBuilderEventHandler( GraphForm_ContextMenuBuilder );

            ContextMenuStrip dummyMenu = new ContextMenuStrip();
            dummyMenu.Opening += new CancelEventHandler( foo_Opening );
            TabPageContextMenuStrip = dummyMenu;
		}

        void foo_Opening( object sender, CancelEventArgs e )
        {
            // close the active form when the tab page strip is right-clicked
            Close();
        }

        bool msGraphControl_MouseMoveEvent( ZedGraphControl sender, MouseEventArgs e )
        {
            MSGraphPane hoverPane = sender.MasterPane.FindPane( e.Location ) as MSGraphPane;
            if( hoverPane == null )
                return false;

            CurveItem nearestCurve;
            int nearestIndex;

            //change the cursor if the mouse is sufficiently close to a point
            if( hoverPane.FindNearestPoint( e.Location, out nearestCurve, out nearestIndex ) )
            {
                msGraphControl.Cursor = Cursors.SizeAll;
            } else
            {
                msGraphControl.Cursor = Cursors.Default;
            }
            return false;
        }

        bool msGraphControl_MouseDownEvent( ZedGraphControl sender, MouseEventArgs e )
        {
            // keep track of MSGraphItem nearest the last left click
            Point pos = MousePosition;
            focusedPane = sender.MasterPane.FindPane( e.Location ) as MSGraphPane;
            if( focusedPane == null )
                return false;

            CurveItem nearestCurve; int nearestIndex;
            focusedPane.FindNearestPoint( e.Location, out nearestCurve, out nearestIndex );
            if( nearestCurve == null )
                setFocusedItem( sender.MasterPane[0].CurveList[0] );
            else
                setFocusedItem( nearestCurve );
            return false;
        }

        void GraphForm_ContextMenuBuilder( ZedGraphControl sender,
                                           ContextMenuStrip menuStrip,
                                           Point mousePt,
                                           MSGraphControl.ContextMenuObjectState objState )
        {
            if( sender.MasterPane.PaneList.Count > 1 )
            {
                ToolStripMenuItem layoutMenu = new ToolStripMenuItem( "Stack Layout", null,
                        new ToolStripItem[]
                    {
                        new ToolStripMenuItem("Single Column", null, GraphForm_StackLayoutSingleColumn),
                        new ToolStripMenuItem("Single Row", null, GraphForm_StackLayoutSingleRow),
                        new ToolStripMenuItem("Grid", null, GraphForm_StackLayoutGrid)
                    }
                    );
                menuStrip.Items.Add( layoutMenu );

                ToolStripMenuItem syncMenuItem = new ToolStripMenuItem( "Synchronize Zoom/Pan", null, GraphForm_SyncZoomPan );
                syncMenuItem.Checked = msGraphControl.IsSynchronizeXAxes;
                menuStrip.Items.Add( syncMenuItem );
            }
        }

        void GraphForm_StackLayoutSingleColumn( object sender, EventArgs e )
        {
            PaneListLayout = PaneLayout.SingleColumn;
        }

        void GraphForm_StackLayoutSingleRow( object sender, EventArgs e )
        {
            PaneListLayout = PaneLayout.SingleRow;
        }

        void GraphForm_StackLayoutGrid( object sender, EventArgs e )
        {
            PaneListLayout = PaneLayout.ForceSquare;
        }

        void GraphForm_SyncZoomPan( object sender, EventArgs e )
        {
            msGraphControl.IsSynchronizeXAxes = !msGraphControl.IsSynchronizeXAxes;
        }

        public override void Refresh()
        {
            MasterPane mp = msGraphControl.MasterPane;
            
            if( mp.PaneList.Count != paneList.Count )
            {
                mp.PaneList.Clear();
                foreach( Pane logicalPane in paneList )
                {
                    MSGraphPane pane = new MSGraphPane();
                    pane.IsFontsScaled = false;
                    mp.Add( pane );
                }
                //mp.SetLayout( msGraphControl.CreateGraphics(), paneLayout );
            } else
            {
                for( int i=0; i < paneList.Count; ++i )
                {
                    MSGraphPane pane = mp.PaneList[i] as MSGraphPane;
                    pane.CurveList.Clear();
                    pane.GraphObjList.Clear();
                }
            }

            for( int i = 0; i < paneList.Count; ++i )
            {
                Pane logicalPane = paneList[i];
                MSGraphPane pane = mp.PaneList[i] as MSGraphPane;
                pane.IsFontsScaled = false;

                foreach( GraphItem item in logicalPane )
                {
                    msGraphControl.AddGraphItem( pane, item );
                }

                if( mp.PaneList.Count > 1 )
                {
                    //if( i < paneList.Count - 1 )
                    {
                        pane.XAxis.Title.IsVisible = false;
                        pane.XAxis.Scale.IsVisible = false;
                        pane.Margin.Bottom = 0;
                        pane.Margin.Top = 2;
                    }/* else
                    {
                        pane.XAxis.Title.IsVisible = true;
                        pane.XAxis.Scale.IsVisible = true;
                    }*/
                    pane.YAxis.Title.IsVisible = false;
                    pane.YAxis.Scale.IsVisible = false;
                    pane.YAxis.Scale.SetupScaleData( pane, pane.YAxis );
                } else
                {
                    pane.XAxis.IsVisible = true;
                    pane.XAxis.Title.IsVisible = true;
                    pane.XAxis.Scale.IsVisible = true;
                    pane.YAxis.Title.IsVisible = true;
                    pane.YAxis.Scale.IsVisible = true;
                }

                if( logicalPane.Count == 1 )
                {
                    pane.Legend.IsVisible = false;
                } else
                {
                    pane.Legend.IsVisible = true;
                    pane.Legend.Position = ZedGraph.LegendPos.TopCenter;

                    ZedGraph.ColorSymbolRotator rotator = new ColorSymbolRotator();
                    foreach( CurveItem item in pane.CurveList )
                    {
                        item.Color = rotator.NextColor;
                    }
                }

                if( paneList.Count > 0 && paneList[0].Count > 0 )
                {
                    this.Text = paneList[0][0].Id;
                    this.TabText = Regex.Replace( this.Text, "\\S+=", "" ).Replace(' ', '.');
                }

                if( pane.XAxis.Scale.MaxAuto )
                    msGraphControl.RestoreScale( pane );
                else
                    pane.AxisChange();
            }

            mp.SetLayout( msGraphControl.CreateGraphics(), paneLayout );

            /*if( isOverlay )
            {
                pane.Legend.IsVisible = true;
                pane.Legend.Position = ZedGraph.LegendPos.TopCenter;
                for( int i = 0; i < pane.CurveList.Count; ++i )
                {
                    pane.CurveList[i].Color = overlayColors[i];
                    ( pane.CurveList[i] as ZedGraph.LineItem ).Line.Width = 2;
                }
            } else
            {
                pane.Legend.IsVisible = false;
                currentGraphItem = chromatogram;
            }*/

            //msGraphControl.RestoreScale( pane );
            //msGraphControl.ZoomOutAll( pane );

            /*bool isScaleAuto = !pane.IsZoomed;

            if( isScaleAuto )
                pointList.SetScale( bins, pointList[0].X, pointList[pointList.Count - 1].X );
            else
                pointList.SetScale( bins, pane.XAxis.Scale.Min, pane.XAxis.Scale.Max );*/

            // String.Format( "{0} - {1}", currentDataSource.Name, chromatogram.Id )

            if( mp.PaneList.Count > 0 &&
                ( focusedPane == null ||
                  !mp.PaneList.Contains( focusedPane ) ) )
                focusedPane = mp.PaneList[0] as MSGraphPane;

            if( mp.PaneList.Count > 0 &&
                mp.PaneList[0].CurveList.Count > 0 &&
                ( focusedItem == null ||
                  !focusedPane.CurveList.Contains( focusedItem ) ) )
                setFocusedItem( mp.PaneList[0].CurveList[0] );

            msGraphControl.Refresh();
        }


		private Color[] overlayColors = new Color[]
		{
			Color.Red, Color.Blue, Color.Green, Color.Purple, Color.Brown,
			Color.Magenta, Color.Cyan, Color.LightGreen, Color.Beige,
			Color.DarkRed, Color.DarkBlue, Color.DarkGreen, Color.DeepPink
		};

		private void GraphForm_ResizeBegin( object sender, EventArgs e )
		{
			SuspendLayout();
			msGraphControl.Visible = false;
		}

		private void GraphForm_ResizeEnd( object sender, EventArgs e )
		{
			ResumeLayout();
			msGraphControl.Visible = true;
			Refresh();
		}
    }

    public class Pane : List<GraphItem>
    {
    }

    public class PaneList : List<Pane>
    {
    }

    public static class Extensions
    {
        /// <summary>
        /// Converts the integer X and Y coordinates into a floating-point PointF.
        /// </summary>
        public static PointF ToPointF( this Point point )
        {
            return new PointF( (float) point.X, (float) point.Y );
        }
    }
}