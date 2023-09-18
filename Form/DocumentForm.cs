using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSCAD
{
    public partial class DocumentForm : Form, INSCanvasOwner, INSEditToolOwner
    {

        Canvas _canvas;
		DataModel _data;

		MenuItemManager _menuItems = new MenuItemManager();

		public DocumentForm(/*string filename*/)
        {
            InitializeComponent();
            Text = "<New Document>";
            _data = new DataModel();
            //if (filename.Length > 0 && File.Exists(filename) && _data.Load(filename))
            //{
            //	Text = filename;
            //	m_filename = filename;
            //}

            _canvas = new Canvas(this, _data); //描画コントロール
			_canvas.Dock = DockStyle.Fill;
			Controls.Add(_canvas);
			_canvas.SetCenter(new UnitPoint(0, 0));
			_canvas.RunningSnaps = new Type[] 
				{
				typeof(VertextSnapPoint),
				typeof(MidpointSnapPoint),
				typeof(IntersectSnapPoint),
				typeof(QuadrantSnapPoint),
				typeof(CenterSnapPoint),
				typeof(DivisionSnapPoint),
				};

			_canvas.AddQuickSnapType(Keys.N, typeof(NearestSnapPoint));
			_canvas.AddQuickSnapType(Keys.M, typeof(MidpointSnapPoint));
			_canvas.AddQuickSnapType(Keys.I, typeof(IntersectSnapPoint));
			_canvas.AddQuickSnapType(Keys.V, typeof(VertextSnapPoint));
			_canvas.AddQuickSnapType(Keys.P, typeof(PerpendicularSnapPoint));
			_canvas.AddQuickSnapType(Keys.Q, typeof(QuadrantSnapPoint));
			_canvas.AddQuickSnapType(Keys.C, typeof(CenterSnapPoint));
			_canvas.AddQuickSnapType(Keys.T, typeof(TangentSnapPoint));
			_canvas.AddQuickSnapType(Keys.D, typeof(DivisionSnapPoint));

			_canvas.KeyDown += new KeyEventHandler(OnCanvasKeyDown);
            SetupMenuItems();
            SetupDrawTools();
            SetupLayerToolstrip();
            UpdateLayerUI();

            MenuStrip menuitem = new MenuStrip();
            menuitem.Items.Add(_menuItems.GetMenuStrip("edit"));
            menuitem.Items.Add(_menuItems.GetMenuStrip("draw"));
            menuitem.Visible = false;
            Controls.Add(menuitem);
            this.MainMenuStrip = menuitem;
        }


        protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			_canvas.SetCenter(_data.CenterPoint);
		}

			void SetupMenuItems()
		{
			MenuItem mmitem = _menuItems.GetItem("Undo");
			mmitem.Text = "元に戻す";
			mmitem.Image = MenuImages16x16.Image(MenuImages16x16.eIndexes.Undo);
			mmitem.ToolTipText = "Undo (Ctrl-Z)";
			mmitem.Click += new EventHandler(OnUndo);
			mmitem.ShortcutKeys = Shortcut.CtrlZ;

			mmitem = _menuItems.GetItem("Redo");
			mmitem.Text = "やり直し";
			mmitem.ToolTipText = "Undo (Ctrl-Y)";
			mmitem.Image = MenuImages16x16.Image(MenuImages16x16.eIndexes.Redo);
			mmitem.Click += new EventHandler(OnRedo);
			mmitem.ShortcutKeys = Shortcut.CtrlY;

			mmitem = _menuItems.GetItem("Select");
			mmitem.Text = "選択";
			mmitem.ToolTipText = "Select (Esc)";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Select);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.ShortcutKeyDisplayString = "Esc";
			mmitem.SingleKey = Keys.Escape;
			mmitem.Tag = "select";

			mmitem = _menuItems.GetItem("Pan"); //pan
			mmitem.Text = "画面移動";
			mmitem.ToolTipText = "Pan (P)";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Pan);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.ShortcutKeyDisplayString = "P";
			mmitem.SingleKey = Keys.P;
			mmitem.Tag = "pan";

			mmitem = _menuItems.GetItem("Move");
			mmitem.Text = "移動";
			mmitem.ToolTipText = "Move (M)";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Move);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.ShortcutKeyDisplayString = "M";
			mmitem.SingleKey = Keys.M;
			mmitem.Tag = "move";

			ToolStrip strip = _menuItems.GetStrip("edit");
			strip.Items.Add(_menuItems.GetItem("Select").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Pan").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Move").CreateButton());
			strip.Items.Add(new ToolStripSeparator());
			strip.Items.Add(_menuItems.GetItem("Undo").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Redo").CreateButton());

			ToolStripMenuItem menu = _menuItems.GetMenuStrip("edit");
			menu.MergeAction = System.Windows.Forms.MergeAction.Insert;
			menu.MergeIndex = 1;
			menu.Text = "編集";
			menu.DropDownItems.Add(_menuItems.GetItem("Undo").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("Redo").CreateMenuItem());
			menu.DropDownItems.Add(new ToolStripSeparator());
			menu.DropDownItems.Add(_menuItems.GetItem("Select").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("Pan").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("Move").CreateMenuItem());
		}
		void SetupDrawTools()
		{
			//作図ツール
			MenuItem mmitem = _menuItems.GetItem("Lines");
			mmitem.Text = "直線";
			mmitem.ToolTipText = "Lines (L)";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Line);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.SingleKey = Keys.L;
			mmitem.ShortcutKeyDisplayString = "L";
			mmitem.Tag = "lines";
			_data.AddDrawTool(mmitem.Tag.ToString(), new LineEdit(false));

			mmitem = _menuItems.GetItem("Line");
			mmitem.Text = "直線(線分)";
			mmitem.ToolTipText = "Single line (S)";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Line);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.SingleKey = Keys.S;
			mmitem.ShortcutKeyDisplayString = "S";
			mmitem.Tag = "singleline";
			_data.AddDrawTool(mmitem.Tag.ToString(), new LineEdit(true));

			mmitem = _menuItems.GetItem("Circle2P");
			mmitem.Text = "円 (2点)";
			mmitem.ToolTipText = "Circle 2 point";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Circle2P);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.Tag = "circle2P";
			_data.AddDrawTool(mmitem.Tag.ToString(), new Circle(Arc.eArcType.type2point));

			mmitem = _menuItems.GetItem("CircleCR");
			mmitem.Text = "円(中心-半径)";
			mmitem.ToolTipText = "Circle Center-Radius";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.CircleCR);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.SingleKey = Keys.C;
			mmitem.ShortcutKeyDisplayString = "C";
			mmitem.Tag = "circleCR";
			_data.AddDrawTool(mmitem.Tag.ToString(), new Circle(Arc.eArcType.typeCenterRadius));

			mmitem = _menuItems.GetItem("Arc2P");
			mmitem.Text = "円弧(2点)";
			mmitem.ToolTipText = "Arc 2 point";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.Arc2P);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.Tag = "arc2P";
			_data.AddDrawTool(mmitem.Tag.ToString(), new Arc(Arc.eArcType.type2point));


			mmitem = _menuItems.GetItem("ArcCR");
			mmitem.Text = "円弧(中心-半径)";
			mmitem.ToolTipText = "Arc Center-Radius";
			mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.ArcCR);
			mmitem.Click += new EventHandler(OnToolSelect);
			mmitem.SingleKey = Keys.A;
			mmitem.ShortcutKeyDisplayString = "A";
			mmitem.Tag = "arcCR";
			_data.AddDrawTool(mmitem.Tag.ToString(), new Arc(Arc.eArcType.typeCenterRadius));

			ToolStrip strip = _menuItems.GetStrip("draw");
			strip.Items.Add(_menuItems.GetItem("Lines").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Circle2P").CreateButton());
			strip.Items.Add(_menuItems.GetItem("CircleCR").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Arc2P").CreateButton());
			strip.Items.Add(_menuItems.GetItem("ArcCR").CreateButton());

			ToolStripMenuItem menu = _menuItems.GetMenuStrip("draw");
			menu.MergeAction = System.Windows.Forms.MergeAction.Insert;
			menu.MergeIndex = 2;
			menu.Text = "作図コマンド";
			menu.DropDownItems.Add(_menuItems.GetItem("Lines").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("Line").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("Circle2P").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("CircleCR").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("Arc2P").CreateMenuItem());
			menu.DropDownItems.Add(_menuItems.GetItem("ArcCR").CreateMenuItem());
		}

		ToolStripStatusLabel m_mousePosLabel = new ToolStripStatusLabel();
		ToolStripStatusLabel m_snapInfoLabel = new ToolStripStatusLabel();
		ToolStripStatusLabel m_drawInfoLabel = new ToolStripStatusLabel();
		ToolStripComboBox m_layerCombo = new ToolStripComboBox();
		void SetupLayerToolstrip()
		{
			StatusStrip status = _menuItems.GetStatusStrip("status");
			m_mousePosLabel.AutoSize = true;
			m_mousePosLabel.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right;
			m_mousePosLabel.Size = new System.Drawing.Size(110, 17);
			status.Items.Add(m_mousePosLabel);

			m_snapInfoLabel.AutoSize = true;
			m_snapInfoLabel.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right;
			m_snapInfoLabel.Size = new System.Drawing.Size(200, 17);
			status.Items.Add(m_snapInfoLabel);

			//m_drawInfoLabel.AutoSize = true;
			m_drawInfoLabel.Spring = true;
			m_drawInfoLabel.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right;
			m_drawInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
			m_drawInfoLabel.Size = new System.Drawing.Size(200, 17);
			status.Items.Add(m_drawInfoLabel);

			ToolStrip strip = _menuItems.GetStrip("layer");
			strip.Items.Add(new ToolStripLabel("Active Layer"));

			m_layerCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			int index = 1;
			foreach (DrawingLayer layer in _data.Layers)
			{
				string name = string.Format("({0}) - {1}", index, layer.Name);

				MenuItem mmitem = _menuItems.GetItem(name);
				mmitem.Text = name;
				mmitem.Image = DrawToolsImages16x16.Image(DrawToolsImages16x16.eIndexes.ArcCR);
				mmitem.Click += new EventHandler(OnLayerSelect);
				mmitem.SingleKey = Keys.D0 + index;
				mmitem.Tag = new NameObject<DrawingLayer>(mmitem.Text, layer);

				m_layerCombo.Items.Add(new NameObject<DrawingLayer>(mmitem.Text, layer));
				m_layerCombo.SelectedIndexChanged += mmitem.Click;
				
				index++;
			}
			strip.Items.Add(m_layerCombo);
		}
		public ToolStrip GetToolStrip(string id)
		{
			return _menuItems.GetStrip(id);
		}
		public void Save()
		{
			UpdateData();
			//if (m_filename.Length == 0)
			//	SaveAs();
			//else
			//	_data.Save(m_filename);
		}
		public void SaveAs()
		{
			UpdateData();
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = "Cad XML files (*.cadxml)|*.cadxml";
			dlg.OverwritePrompt = true;
			//if (m_filename.Length > 0)
			//	dlg.FileName = m_filename;
			//if (dlg.ShowDialog(this) == DialogResult.OK)
			//{
			//	m_filename = dlg.FileName;
			//	_data.Save(m_filename);
			//	Text = m_filename;
			//}
		}
		public Canvas Canvas
		{
			get { return _canvas ;}
		}
		public DataModel Model
		{
			get { return _data; }
		}
		void UpdateData()
		{
			// update any additional properties of data which is not part of the interface
			_data.CenterPoint = _canvas.GetCenter();
		}
		void OnToolSelect(object sender, System.EventArgs e)
		{
			string toolid = string.Empty;
			bool fromKeyboard = false;
			if (sender is MenuItem) // from keyboard
			{
				toolid = ((MenuItem)sender).Tag.ToString();
				fromKeyboard = true;
			}
			if (sender is ToolStripItem) // from menu or toolbar
			{
				toolid = ((ToolStripItem)sender).Tag.ToString();
			}
			if (toolid == "select")
			{
				_canvas.CommandEscape();
				return;
			}
			if (toolid == "pan")
			{
				_canvas.CommandPan();
				return;
			}
			if (toolid == "move")
			{
				// if from keyboard then handle immediately, if from mouse click then only switch mode
				_canvas.CommandMove(fromKeyboard);
				return;
			}
			_canvas.CommandSelectDrawTool(toolid);
		}
		void OnEditToolSelect(object sender, System.EventArgs e)
		{
			string toolid = string.Empty;
			//bool fromKeyboard = false;
			if (sender is MenuItem) // from keyboard
			{
				toolid = ((MenuItem)sender).Tag.ToString();
				//fromKeyboard = true;
			}
			if (sender is ToolStripItem) // from menu or toolbar
			{
				toolid = ((ToolStripItem)sender).Tag.ToString();
			}
			_canvas.CommandEdit(toolid);
		}
		void UpdateLayerUI()
		{
			NameObject<DrawingLayer> selitem = m_layerCombo.SelectedItem as NameObject<DrawingLayer>;
			if (selitem == null || selitem.Object != _data.ActiveLayer)
			{
				foreach (NameObject<DrawingLayer> obj in m_layerCombo.Items)
				{
					if (obj.Object == _data.ActiveLayer)
						m_layerCombo.SelectedItem = obj;
				}
			}
		}
		void OnLayerSelect(object sender, System.EventArgs e)
		{
			NameObject<DrawingLayer> obj = null;
			if (sender is ToolStripComboBox)
				obj = ((ToolStripComboBox)sender).SelectedItem as NameObject<DrawingLayer>;
			if (sender is MenuItem)
				obj = ((MenuItem)sender).Tag as NameObject<DrawingLayer>;
			if (obj == null)
				return;
			_data.ActiveLayer = obj.Object as DrawingLayer;
			_canvas.DoInvalidate(true);
			UpdateLayerUI();
		}
		void OnUndo(object sender, System.EventArgs e)
		{
			if (_data.DoUndo())
				_canvas.DoInvalidate(true);
		}
		void OnRedo(object sender, System.EventArgs e)
		{
			if (_data.DoRedo())
				_canvas.DoInvalidate(true);
		}
		public void UpdateUI()
		{
			_menuItems.GetItem("Undo").Enabled = _data.CanUndo();
			_menuItems.GetItem("Redo").Enabled = _data.CanRedo();
			_menuItems.GetItem("Move").Enabled = _data.SelectedCount > 0;
		}
		void OnCanvasKeyDown(object sender, KeyEventArgs e)
		{
			if (Control.ModifierKeys != Keys.None)
				return;

			MenuItem item = _menuItems.FindFromSingleKey(e.KeyCode);
			if (item != null && item.Click != null)
			{
				item.Click(item, null);
				e.Handled = true;
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (_data.IsDirty)
			{
				//string s = "Save Changes to " + Path.GetFileName(_filename) + "?";
				//DialogResult result = MessageBox.Show(this, s, "NSCAD", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				//if (result == DialogResult.Cancel)
				//{
				//	e.Cancel = true;
				//	return;
				//}
				//if (result == DialogResult.Yes)
				//	Save();
			}
			_menuItems.DisableAll();
			base.OnFormClosing(e);
		}
		#region ICanvasOwner Members
		public void SetPositionInfo(UnitPoint unitpos)
		{
			m_mousePosLabel.Text = unitpos.PosAsString();
			string s = string.Empty;
			if (_data.SelectedCount == 1 || _canvas.NewObject != null)
			{
				INSDrawObject obj = _data.GetFirstSelected();
				if (obj == null)
					obj = _canvas.NewObject;
				if (obj != null)
					s = obj.GetInfoAsString();
			}
			if (m_toolHint.Length > 0)
				s = m_toolHint;
			if (s != m_drawInfoLabel.Text)
				m_drawInfoLabel.Text = s;
		}
		public void SetSnapInfo(INSSnapPoint snap)
		{
			m_snapHint = string.Empty;
			if (snap != null)
				m_snapHint = string.Format("スナップ　{0}", snap.SnapPoint.PosAsString());
			m_snapInfoLabel.Text = m_snapHint;
		}
		#endregion
		#region IEditToolOwner
		public void SetHint(string text)
		{
			m_toolHint = text;
			m_drawInfoLabel.Text = m_toolHint;
			//SetHint();
		}
		#endregion
		string m_toolHint = string.Empty;
		string m_snapHint = string.Empty;
	}

}
