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
	public partial class MainWin : Form
	{
		MenuItemManager _menuItems = null;
		DocumentForm _activeDocument = null; // def is DocumentForm.cs

		public MainWin()
		{
			UnitPoint p = HitUtil.CenterPointFrom3Points(new UnitPoint(0, 2), new UnitPoint(1.4142136f, 1.4142136f), new UnitPoint(2, 0));

			InitializeComponent();
			Text = "NSCAD";
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length == 2) // assume it points to a file
				OpenDocument(args[1]);
			else
				OpenDocument(string.Empty);

			_menuItems = new MenuItemManager(this);
			_menuItems.SetupStripPanels();
			SetupToolbars();

			Application.Idle += new EventHandler(OnIdle);
		}
		void SetupToolbars()
		{
			MenuItem mmitem = _menuItems.GetItem("New");
			mmitem.Text = "&新規作成";
			mmitem.Image = MenuImages16x16.Image(MenuImages16x16.eIndexes.NewDocument);
			mmitem.Click += new EventHandler(OnFileNew);
			mmitem.ToolTipText = "New document";

			mmitem = _menuItems.GetItem("Open");
			mmitem.Text = "&開く";
			mmitem.Image = MenuImages16x16.Image(MenuImages16x16.eIndexes.OpenDocument);
			mmitem.Click += new EventHandler(OnFileOpen);
			mmitem.ToolTipText = "Open document";

			mmitem = _menuItems.GetItem("Save");
			mmitem.Text = "&保存";
			mmitem.Image = MenuImages16x16.Image(MenuImages16x16.eIndexes.SaveDocument);
			mmitem.Click += new EventHandler(OnFileSave);
			mmitem.ToolTipText = "Save document";

			mmitem = _menuItems.GetItem("SaveAs");
			mmitem.Text = "名前を付けて保存";
			mmitem.Click += new EventHandler(OnFileSaveAs);

			mmitem = _menuItems.GetItem("Exit");
			mmitem.Text = "終了";
			mmitem.Click += new EventHandler(OnFileExit);

			ToolStrip strip = _menuItems.GetStrip("file");
			strip.Items.Add(_menuItems.GetItem("New").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Open").CreateButton());
			strip.Items.Add(_menuItems.GetItem("Save").CreateButton());

			ToolStripMenuItem menuitem = _menuItems.GetMenuStrip("file");
			menuitem.Text = "&ファイル";
			menuitem.DropDownItems.Add(_menuItems.GetItem("New").CreateMenuItem());
			menuitem.DropDownItems.Add(_menuItems.GetItem("Open").CreateMenuItem());
			menuitem.DropDownItems.Add(_menuItems.GetItem("Save").CreateMenuItem());
			menuitem.DropDownItems.Add(_menuItems.GetItem("SaveAs").CreateMenuItem());
			menuitem.DropDownItems.Add(new ToolStripSeparator());
			menuitem.DropDownItems.Add(_menuItems.GetItem("Exit").CreateMenuItem());
			_mainMenu.Items.Insert(0, menuitem);

			ToolStripPanel panel = _menuItems.GetStripPanel(DockStyle.Top);

			panel.Join(_menuItems.GetStrip("layer"));
			panel.Join(_menuItems.GetStrip("draw"));
			panel.Join(_menuItems.GetStrip("edit"));
			panel.Join(_menuItems.GetStrip("file"));
			panel.Join(_mainMenu);

			panel = _menuItems.GetStripPanel(DockStyle.Bottom);
			panel.Join(_menuItems.GetStatusStrip("status")); //trackerにより、非同期で値を表示する
		}
		void OnIdle(object sender, EventArgs e)
		{
			m_activeDocument = this.ActiveMdiChild as DocumentForm;
			if (m_activeDocument != null)
				m_activeDocument.UpdateUI();

		}
		DocumentForm m_activeDocument = null;
		protected override void OnMdiChildActivate(EventArgs e)
		{
			DocumentForm olddocument = m_activeDocument;
			base.OnMdiChildActivate(e);
			m_activeDocument = this.ActiveMdiChild as DocumentForm;
			foreach (Control ctrl in Controls)
			{
				if (ctrl is ToolStripPanel)
					((ToolStripPanel)ctrl).SuspendLayout();
			}
			if (m_activeDocument != null)
			{
				ToolStripManager.RevertMerge(_menuItems.GetStrip("edit"));
				ToolStripManager.RevertMerge(_menuItems.GetStrip("draw"));
				ToolStripManager.RevertMerge(_menuItems.GetStrip("layer"));
				ToolStripManager.RevertMerge(_menuItems.GetStrip("status"));
				ToolStripManager.RevertMerge(_menuItems.GetStrip("modify"));
				ToolStripManager.Merge(m_activeDocument.GetToolStrip("draw"), _menuItems.GetStrip("draw"));
				ToolStripManager.Merge(m_activeDocument.GetToolStrip("edit"), _menuItems.GetStrip("edit"));
				ToolStripManager.Merge(m_activeDocument.GetToolStrip("layer"), _menuItems.GetStrip("layer"));
				ToolStripManager.Merge(m_activeDocument.GetToolStrip("status"), _menuItems.GetStrip("status"));
			}
			foreach (Control ctrl in Controls)
			{
				if (ctrl is ToolStripPanel)
					((ToolStripPanel)ctrl).ResumeLayout();
			}
		}
		private void OnFileOpen(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Cad XML files (*.cadxml)|*.cadxml";
			if (dlg.ShowDialog(this) == DialogResult.OK)
				OpenDocument(dlg.FileName);
		}
		private void OnFileSave(object sender, EventArgs e)
		{
			DocumentForm doc = this.ActiveMdiChild as DocumentForm;
			if (doc != null)
				doc.Save();
		}
		private void OnFileNew(object sender, EventArgs e)
		{
			OpenDocument(string.Empty);
		}
		void OpenDocument(string filename)
		{
			//DocumentForm f = new DocumentForm(filename);
			DocumentForm f = new DocumentForm(/*filename*/);
			f.MdiParent = this;
			f.WindowState = FormWindowState.Maximized;
			f.Show();
		}

		private void OnFileSaveAs(object sender, EventArgs e)
		{
			DocumentForm doc = this.ActiveMdiChild as DocumentForm;
			if (doc != null)
				doc.SaveAs();
		}
		private void OnFileExit(object sender, EventArgs e)
		{
			Close();
		}
		private void OnUpdateMenuUI(object sender, EventArgs e)
		{
			return;
		}

		private void OnAbout(object sender, EventArgs e)
		{
			//About dlg = new About();
			//dlg.ShowDialog(this);
			return;
		}

		private void OnOptions(object sender, EventArgs e)
		{
			//DocumentForm doc = this.ActiveMdiChild as DocumentForm;
			//if (doc == null)
			//	return;

			//Options.OptionsDlg dlg = new Canvas.Options.OptionsDlg();
			//dlg.Config.Grid.CopyFromLayer(doc.Model.GridLayer as GridLayer);
			//dlg.Config.Background.CopyFromLayer(doc.Model.BackgroundLayer as BackgroundLayer);
			//foreach (DrawingLayer layer in doc.Model.Layers)
			//	dlg.Config.Layers.Add(new Options.OptionsLayer(layer));

			//ToolStripItem item = sender as ToolStripItem;
			//dlg.SelectPage(item.Tag);

			//if (dlg.ShowDialog(this) == DialogResult.OK)
			//{
			//	dlg.Config.Grid.CopyToLayer((GridLayer)doc.Model.GridLayer);
			//	dlg.Config.Background.CopyToLayer((BackgroundLayer)doc.Model.BackgroundLayer);
			//	foreach (Options.OptionsLayer optionslayer in dlg.Config.Layers)
			//	{
			//		DrawingLayer layer = (DrawingLayer)doc.Model.GetLayer(optionslayer.Layer.Id);
			//		if (layer != null)
			//			optionslayer.CopyToLayer(layer);
			//		else
			//		{
			//			// delete layer
			//		}
			//	}

			//	doc.Canvas.DoInvalidate(true);
			return;
		}
	}
}