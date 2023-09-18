using NSCAD;
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
	//document form状につける、描画部分はこの部分を使用
	public partial class Canvas : UserControl
	{
		enum eCommandType
		{
			select,
			pan,
			move,
			draw,
			edit,
			editNode,
		}

		INSCanvasOwner		_owner = null;
		INSModel				_model = null;
		INSSnapPoint			_snappoint = null;
		INSDrawObject			_newObject = null;
		INSEditTool			_editTool = null;
        CursorCollection _cursors = new CursorCollection();
        MoveHelper _moveHelper = null;
        NodeMoveHelper _nodeMoveHelper = null;
        SelectionRectangle _selection = null;
        CanvasWrapper		_canvaswrapper;
		Bitmap				_staticImage = null;
		Type[]				_runningSnapTypes = null;
		PointF				_mousedownPoint;
		string				_drawObjectId = string.Empty;
		string				_editToolId = string.Empty;
		eCommandType		_commandType = eCommandType.select;
		bool				_staticDirty = true;
		bool				_runningSnaps = true;
		
		public Type[] RunningSnaps
		{
			get { return _runningSnapTypes; }
			set { _runningSnapTypes = value; }
		}
		public bool RunningSnapsEnabled
		{
			get { return _runningSnaps; }
			set { _runningSnaps = value; }
		}

		System.Drawing.Drawing2D.SmoothingMode	m_smoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
		public System.Drawing.Drawing2D.SmoothingMode SmoothingMode
		{
			get { return m_smoothingMode; }
			set { m_smoothingMode = value;}
		}

		public INSModel Model
		{
			get { return _model; }
			set { _model = value; }
		}

		public Canvas(INSCanvasOwner owner, INSModel datamodel)
		{
			_canvaswrapper = new CanvasWrapper(this);
			_owner = owner;
			_model = datamodel;

			InitializeComponent();
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			_commandType = eCommandType.select;
		    _cursors.AddCursor(eCommandType.select, Cursors.Arrow);
			_cursors.AddCursor(eCommandType.draw, Cursors.Cross);
			_cursors.AddCursor(eCommandType.pan, Cursors.Hand);
			_cursors.AddCursor(eCommandType.move, Cursors.SizeAll);
			_cursors.AddCursor(eCommandType.edit, Cursors.Cross);
			UpdateCursor();

			_moveHelper = new MoveHelper(this);
			_nodeMoveHelper = new NodeMoveHelper(_canvaswrapper);
		}

		public UnitPoint GetMousePoint()
		{
			Point point = this.PointToClient(Control.MousePosition);
			return ToUnit(point);
		}

		public void SetCenter(UnitPoint unitPoint)
		{
			PointF point = ToScreen(unitPoint);
			m_lastCenterPoint = unitPoint;
			SetCenterScreen(point, false);
		}

		public void SetCenter()
		{
			Point point = this.PointToClient(Control.MousePosition);
			SetCenterScreen(point, true);
		}

		public UnitPoint GetCenter()
		{
			return ToUnit(new PointF(this.ClientRectangle.Width/2, this.ClientRectangle.Height/2));
		}

		protected  void SetCenterScreen(PointF screenPoint, bool setCursor)
		{
			float centerX = ClientRectangle.Width / 2;
			m_panOffset.X += centerX - screenPoint.X;
			
			float centerY = ClientRectangle.Height / 2;
			m_panOffset.Y += centerY - screenPoint.Y;

			if (setCursor)
				Cursor.Position = this.PointToScreen(new Point((int)centerX, (int)centerY));
			DoInvalidate(true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//CommonTools.Tracing.StartTrack(Program.TracePaint);
			ClearPens();
			e.Graphics.SmoothingMode = m_smoothingMode;
			CanvasWrapper dc = new CanvasWrapper(this, e.Graphics, ClientRectangle);
			Rectangle cliprectangle = e.ClipRectangle;
			if (_staticImage == null)
			{
				cliprectangle = ClientRectangle;
				_staticImage = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
				_staticDirty = true;
			}
			RectangleF r = ScreenUtils.ToUnitNormalized(dc, cliprectangle);
			if (float.IsNaN(r.Width) || float.IsInfinity(r.Width))
			{
				r = ScreenUtils.ToUnitNormalized(dc, cliprectangle);
			}
			if (_staticDirty)
			{
				_staticDirty = false;
				CanvasWrapper dcStatic = new CanvasWrapper(this, Graphics.FromImage(_staticImage), ClientRectangle);
				dcStatic.Graphics.SmoothingMode = m_smoothingMode;
				_model.BackgroundLayer.Draw(dcStatic, r);
				if (_model.GridLayer.Enabled)
					_model.GridLayer.Draw(dcStatic, r);

				PointF nullPoint = ToScreen(new UnitPoint(0, 0));
				dcStatic.Graphics.DrawLine(Pens.Blue, nullPoint.X - 10, nullPoint.Y, nullPoint.X + 10, nullPoint.Y);
				dcStatic.Graphics.DrawLine(Pens.Blue, nullPoint.X, nullPoint.Y - 10, nullPoint.X, nullPoint.Y + 10);

				INSCanvasLayer[] layers = _model.Layers;
				for (int layerindex = layers.Length - 1; layerindex >= 0; layerindex--)
				{
					if (layers[layerindex] != _model.ActiveLayer && layers[layerindex].Visible)
						layers[layerindex].Draw(dcStatic, r);
				}
				if (_model.ActiveLayer != null)
					_model.ActiveLayer.Draw(dcStatic, r);

				dcStatic.Dispose();
			}
			e.Graphics.DrawImage(_staticImage, cliprectangle, cliprectangle, GraphicsUnit.Pixel);
			
			foreach (INSDrawObject drawobject in _model.SelectedObjects) //DBをすべて描画
				drawobject.Draw(dc, r);

			if (_newObject != null)
				_newObject.Draw(dc, r);
			
			if (_snappoint != null)
				_snappoint.Draw(dc);
			
			if (_selection != null)
			{
				_selection.Reset();
				_selection.SetMousePoint(e.Graphics, this.PointToClient(Control.MousePosition));
			}
			if (_moveHelper.IsEmpty == false)
				_moveHelper.DrawObjects(dc, r);

			if (_nodeMoveHelper.IsEmpty == false)
				_nodeMoveHelper.DrawObjects(dc, r);
			dc.Dispose();
			ClearPens();
			//CommonTools.Tracing.EndTrack(Program.TracePaint, "OnPaint complete");
		}
		void RepaintStatic(Rectangle r)
		{
			if (_staticImage == null)
				return;
			Graphics dc = Graphics.FromHwnd(Handle);
			if (r.X < 0) r.X = 0;
			if (r.X > _staticImage.Width) r.X = 0;
			if (r.Y < 0) r.Y = 0;
			if (r.Y > _staticImage.Height) r.Y = 0;
			
			if (r.Width > _staticImage.Width || r.Width < 0)
				r.Width = _staticImage.Width;
			if (r.Height > _staticImage.Height || r.Height < 0)
				r.Height = _staticImage.Height;
			dc.DrawImage(_staticImage, r, r, GraphicsUnit.Pixel);
			dc.Dispose();
		}
		void RepaintSnappoint(INSSnapPoint snappoint)
		{
			if (snappoint == null)
				return;
			CanvasWrapper dc = new CanvasWrapper(this, Graphics.FromHwnd(Handle), ClientRectangle);
			snappoint.Draw(dc);
			dc.Graphics.Dispose();
			dc.Dispose();
		}
		void RepaintObject(INSDrawObject obj)
		{
			if (obj == null)
				return;
			CanvasWrapper dc = new CanvasWrapper(this, Graphics.FromHwnd(Handle), ClientRectangle);
			RectangleF invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(dc, obj.GetBoundingRect(dc)));
			obj.Draw(dc, invalidaterect);
			dc.Graphics.Dispose();
			dc.Dispose();
		}
		public void DoInvalidate(bool dostatic, RectangleF rect)
		{
			if (dostatic)
				_staticDirty = true;
			Invalidate(ScreenUtils.ConvertRect(rect));
		}
		public void DoInvalidate(bool dostatic)
		{
			if (dostatic)
				_staticDirty = true;
			Invalidate();
		}
		public INSDrawObject NewObject
		{
			get { return _newObject; }
		}
		protected void HandleSelection(List<INSDrawObject> selected)
		{
			bool add = Control.ModifierKeys == Keys.Shift;
			bool toggle = Control.ModifierKeys == Keys.Control;
			bool invalidate = false;
			bool anyoldsel = false;
			int selcount = 0;
			if (selected != null)
				selcount = selected.Count;
			foreach(INSDrawObject obj in _model.SelectedObjects)
			{
				anyoldsel = true;
				break;
			}
			if (toggle && selcount > 0)
			{
				invalidate = true;
				foreach (INSDrawObject obj in selected)
				{
					if (_model.IsSelected(obj))
						_model.RemoveSelectedObject(obj);
					else
						_model.AddSelectedObject(obj);
				}
			}
			if (add && selcount > 0)
			{
				invalidate = true;
				foreach (INSDrawObject obj in selected)
					_model.AddSelectedObject(obj);
			}
			if (add == false && toggle == false && selcount > 0)
			{
				invalidate = true;
				_model.ClearSelectedObjects();
				foreach (INSDrawObject obj in selected)
					_model.AddSelectedObject(obj);
			}
			if (add == false && toggle == false && selcount == 0 && anyoldsel)
			{
				invalidate = true;
				_model.ClearSelectedObjects();
			}

			if (invalidate)
				DoInvalidate(false);
		}
		void FinishNodeEdit()
		{
			_commandType = eCommandType.select;
			_snappoint = null;
		}

		protected virtual void HandleMouseDownWhenDrawing(UnitPoint mouseunitpoint, INSSnapPoint snappoint)
		{
			if (_commandType == eCommandType.draw)
			{
				if (_newObject == null)
				{
					_newObject = _model.CreateObject(_drawObjectId, mouseunitpoint, snappoint);
					DoInvalidate(false, _newObject.GetBoundingRect(_canvaswrapper));
				}
				else
				{
					if (_newObject != null)
					{
						eDrawObjectMouseDown result = _newObject.OnMouseDown(_canvaswrapper, mouseunitpoint, snappoint);
						switch (result)
						{
							case eDrawObjectMouseDown.Done:
								_model.AddObject(_model.ActiveLayer, _newObject);
								_newObject = null;
								DoInvalidate(true);
								break;
							case eDrawObjectMouseDown.DoneRepeat:
								_model.AddObject(_model.ActiveLayer, _newObject);
								_newObject = _model.CreateObject(_newObject.Id, _newObject.RepeatStartingPoint, null);
								DoInvalidate(true);
								break;
							case eDrawObjectMouseDown.Continue:
								break;
						}
					}
				}
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			_mousedownPoint = new PointF(e.X, e.Y); // used when panning
			m_dragOffset = new PointF(0,0);

			UnitPoint mousepoint = ToUnit(_mousedownPoint);
			if (_snappoint != null)
				mousepoint = _snappoint.SnapPoint;
			
			if (_commandType == eCommandType.editNode)
			{
				bool handled = false;
				if (_nodeMoveHelper.HandleMouseDown(mousepoint, ref handled))
				{
					FinishNodeEdit();
					base.OnMouseDown(e);
					return;
				}
			}
			if (_commandType == eCommandType.select)
			{
				bool handled = false;
				if (_nodeMoveHelper.HandleMouseDown(mousepoint, ref handled))
				{
					_commandType = eCommandType.editNode;
					_snappoint = null;
					base.OnMouseDown(e);
					return;
				}
				_selection = new SelectionRectangle(_mousedownPoint);
			}
			if (_commandType == eCommandType.move)
			{
				_moveHelper.HandleMouseDownForMove(mousepoint, _snappoint);
			}
			if (_commandType == eCommandType.draw)
			{
				HandleMouseDownWhenDrawing(mousepoint, null);
				DoInvalidate(true);
			}
			if (_commandType == eCommandType.edit)
			{
				if (_editTool == null)
					_editTool = _model.GetEditTool(_editToolId);
				if (_editTool != null)
				{
					if (_editTool.SupportSelection)
						_selection = new SelectionRectangle(_mousedownPoint);

					eDrawObjectMouseDown mouseresult = _editTool.OnMouseDown(_canvaswrapper, mousepoint, _snappoint);
					if (mouseresult == eDrawObjectMouseDown.Done)
					{
						_editTool.Finished();
						_editTool = _model.GetEditTool(_editToolId); // continue with new tool
						
						if (_editTool.SupportSelection)
							_selection = new SelectionRectangle(_mousedownPoint);
					}
				}
				DoInvalidate(true);
				UpdateCursor();
			}
			base.OnMouseDown(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (_commandType == eCommandType.pan)
			{
				m_panOffset.X += m_dragOffset.X;
				m_panOffset.Y += m_dragOffset.Y;
				m_dragOffset = new PointF(0, 0);
			}

			List<INSDrawObject> hitlist = null;
			Rectangle screenSelRect = Rectangle.Empty;
			if (_selection != null)
			{
				screenSelRect = _selection.ScreenRect();
				RectangleF selectionRect = _selection.Selection(_canvaswrapper);
				if (selectionRect != RectangleF.Empty)
				{
					// is any selection rectangle. use it for selection
					hitlist = _model.GetHitObjects(_canvaswrapper, selectionRect, _selection.AnyPoint());
					DoInvalidate(true);
				}
				else
				{
					// else use mouse point
					UnitPoint mousepoint = ToUnit(new PointF(e.X, e.Y));
					hitlist = _model.GetHitObjects(_canvaswrapper, mousepoint);
				}
				_selection = null;
			}
			if (_commandType == eCommandType.select)
			{
				if (hitlist != null)
					HandleSelection(hitlist);
			}
			if (_commandType == eCommandType.edit && _editTool != null)
			{
				UnitPoint mousepoint = ToUnit(_mousedownPoint);
				if (_snappoint != null)
					mousepoint = _snappoint.SnapPoint;
				if (screenSelRect != Rectangle.Empty)
					_editTool.SetHitObjects(mousepoint, hitlist);
				_editTool.OnMouseUp(_canvaswrapper, mousepoint, _snappoint);
			}
			if (_commandType == eCommandType.draw && _newObject != null)
			{
				UnitPoint mousepoint = ToUnit(_mousedownPoint);
				if (_snappoint != null)
					mousepoint = _snappoint.SnapPoint;
				_newObject.OnMouseUp(_canvaswrapper, mousepoint, _snappoint);
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (_selection != null)
			{
				Graphics dc = Graphics.FromHwnd(Handle);
				_selection.SetMousePoint(dc, new PointF(e.X, e.Y));
				dc.Dispose();
				return;
			}

			if (_commandType == eCommandType.pan && e.Button == MouseButtons.Left)
			{
				m_dragOffset.X = -(_mousedownPoint.X - e.X);
				m_dragOffset.Y = -(_mousedownPoint.Y - e.Y);
				m_lastCenterPoint = CenterPointUnit();
				DoInvalidate(true);
			}
			UnitPoint mousepoint;
			UnitPoint unitpoint = ToUnit(new PointF(e.X, e.Y));
			if (_commandType == eCommandType.draw || _commandType == eCommandType.move || _nodeMoveHelper.IsEmpty == false)
			{
				Rectangle invalidaterect = Rectangle.Empty;
				INSSnapPoint newsnap = null;
				mousepoint = GetMousePoint();
				if (RunningSnapsEnabled)
					newsnap = _model.SnapPoint(_canvaswrapper, mousepoint, _runningSnapTypes, null);
				if (newsnap == null)
					newsnap = _model.GridLayer.SnapPoint(_canvaswrapper, mousepoint, null);
				if ((_snappoint != null) && ((newsnap == null) || (newsnap.SnapPoint != _snappoint.SnapPoint) || _snappoint.GetType() != newsnap.GetType()))
				{
					invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(_canvaswrapper, _snappoint.BoundingRect));
					invalidaterect.Inflate(2, 2);
					RepaintStatic(invalidaterect); // remove old snappoint
					_snappoint = newsnap;
				}
				if (_commandType == eCommandType.move)
					Invalidate(invalidaterect);

				if (_snappoint == null)
					_snappoint = newsnap;
			}
			_owner.SetPositionInfo(unitpoint);
			_owner.SetSnapInfo(_snappoint);


			//UnitPoint mousepoint;
			if (_snappoint != null)
				mousepoint = _snappoint.SnapPoint;
			else
				mousepoint = GetMousePoint();

			if (_newObject != null)
			{
				Rectangle invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(_canvaswrapper, _newObject.GetBoundingRect(_canvaswrapper)));
				invalidaterect.Inflate(2, 2);
				RepaintStatic(invalidaterect);

				_newObject.OnMouseMove(_canvaswrapper, mousepoint);
				RepaintObject(_newObject);
			}
			if (_snappoint != null)
				RepaintSnappoint(_snappoint);

			if (_moveHelper.HandleMouseMoveForMove(mousepoint))
				Refresh(); //Invalidate();

			RectangleF rNoderect = _nodeMoveHelper.HandleMouseMoveForNode(mousepoint);
			if (rNoderect != RectangleF.Empty)
			{
				Rectangle invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(_canvaswrapper, rNoderect));
				RepaintStatic(invalidaterect);

				CanvasWrapper dc = new CanvasWrapper(this, Graphics.FromHwnd(Handle), ClientRectangle);
				dc.Graphics.Clip = new Region(ClientRectangle);
				//m_nodeMoveHelper.DrawOriginalObjects(dc, rNoderect);
				_nodeMoveHelper.DrawObjects(dc, rNoderect);
				if (_snappoint != null)
					RepaintSnappoint(_snappoint);

				dc.Graphics.Dispose();
				dc.Dispose();
			}
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			UnitPoint p = GetMousePoint();
			float wheeldeltatick = 120;
			float zoomdelta = (1.25f * (Math.Abs(e.Delta) / wheeldeltatick));
			if (e.Delta < 0)
				_model.Zoom = _model.Zoom / zoomdelta;
			else
				_model.Zoom = _model.Zoom * zoomdelta;
			SetCenterScreen(ToScreen(p), true);
			DoInvalidate(true);
			base.OnMouseWheel(e);
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (m_lastCenterPoint != UnitPoint.Empty && Width != 0)
				SetCenterScreen(ToScreen(m_lastCenterPoint), false);
			m_lastCenterPoint = CenterPointUnit();
			_staticImage = null;
			DoInvalidate(true);
		}

		UnitPoint m_lastCenterPoint;
		PointF m_panOffset = new PointF(25, -25);
		PointF m_dragOffset = new PointF(0, 0);
		float m_screenResolution = 96;

		PointF Translate(UnitPoint point)
		{
			return point.Point;
		}
		float ScreenHeight()
		{
			return (float)(ToUnit(this.ClientRectangle.Height) / _model.Zoom);
		}

		#region INSCanvas Impl
		public UnitPoint CenterPointUnit()
		{
			UnitPoint p1 = ScreenTopLeftToUnitPoint();
			UnitPoint p2 = ScreenBottomRightToUnitPoint();
			UnitPoint center = new UnitPoint();
			center.X = (p1.X + p2.X) / 2;
			center.Y = (p1.Y + p2.Y) / 2;
			return center;
		}
		public UnitPoint ScreenTopLeftToUnitPoint()
		{
			return ToUnit(new PointF(0, 0));
		}
		public UnitPoint ScreenBottomRightToUnitPoint()
		{
			return ToUnit(new PointF(this.ClientRectangle.Width, this.ClientRectangle.Height));
		}
		public PointF ToScreen(UnitPoint point)
		{
			PointF transformedPoint = Translate(point);
			transformedPoint.Y = ScreenHeight() - transformedPoint.Y;
			transformedPoint.Y *= m_screenResolution * _model.Zoom;
			transformedPoint.X *= m_screenResolution * _model.Zoom;

			transformedPoint.X += m_panOffset.X + m_dragOffset.X;
			transformedPoint.Y += m_panOffset.Y + m_dragOffset.Y;
			return transformedPoint;
		}
		public float ToScreen(double value)
		{
			return (float)(value * m_screenResolution * _model.Zoom);
		}
		public double ToUnit(float screenvalue)
		{
			return (double)screenvalue / (double)(m_screenResolution * _model.Zoom);
		}
		public UnitPoint ToUnit(PointF screenpoint)
		{
			float panoffsetX = m_panOffset.X + m_dragOffset.X;
			float panoffsetY = m_panOffset.Y + m_dragOffset.Y;
			float xpos = (screenpoint.X - panoffsetX) / (m_screenResolution * _model.Zoom);
			float ypos = ScreenHeight() - ((screenpoint.Y - panoffsetY)) / (m_screenResolution * _model.Zoom);
			return new UnitPoint(xpos, ypos);
		}
		public Pen CreatePen(Color color, float unitWidth)
		{
			return GetPen(color, ToScreen(unitWidth));
		}
		public void DrawLine(INSCanvas canvas, Pen pen, UnitPoint p1, UnitPoint p2)
		{
			PointF tmpp1 = ToScreen(p1);
			PointF tmpp2 = ToScreen(p2);
			canvas.Graphics.DrawLine(pen, tmpp1, tmpp2);
		}
		public void DrawArc(INSCanvas canvas, Pen pen, UnitPoint center, float radius, float startAngle, float sweepAngle)
		{
			PointF p1 = ToScreen(center);
			radius = (float)Math.Round(ToScreen(radius));
			RectangleF r = new RectangleF(p1, new SizeF());
			r.Inflate(radius, radius);
			if (radius > 0 && radius < 1e8f )
				canvas.Graphics.DrawArc(pen, r, -startAngle, -sweepAngle);
		}

		#endregion

		Dictionary<float, Dictionary<Color, Pen>> m_penCache = new Dictionary<float,Dictionary<Color,Pen>>();
		Pen GetPen(Color color, float width)
		{
			if (m_penCache.ContainsKey(width) == false)
				m_penCache[width] = new Dictionary<Color,Pen>();
			if (m_penCache[width].ContainsKey(color) == false)
				m_penCache[width][color] = new Pen(color, width);
			return m_penCache[width][color];
		}
		void ClearPens()
		{
			m_penCache.Clear();
		}
		
		void UpdateCursor()
		{
			Cursor = _cursors.GetCursor(_commandType);
		}

		Dictionary<Keys, Type> m_QuickSnap = new Dictionary<Keys,Type>();
		public void AddQuickSnapType(Keys key, Type snaptype)
		{
			m_QuickSnap.Add(key, snaptype);
		}

		public void CommandSelectDrawTool(string drawobjectid)
		{
			CommandEscape();
			_model.ClearSelectedObjects();
			_commandType = eCommandType.draw;
			_drawObjectId = drawobjectid;
			UpdateCursor();
		}
		public void CommandEscape()
		{
			bool dirty = (_newObject != null) || (_snappoint != null);
			_newObject = null;
			_snappoint = null;
			if (_editTool != null)
				_editTool.Finished();
			_editTool	= null;
			_commandType = eCommandType.select;
			_moveHelper.HandleCancelMove();
			_nodeMoveHelper.HandleCancelMove();
			DoInvalidate(dirty);
			UpdateCursor();
		}
		public void CommandPan()
		{
			if (_commandType == eCommandType.select || _commandType == eCommandType.move)
				_commandType = eCommandType.pan;
			UpdateCursor();
		}
		public void CommandMove(bool handleImmediately)
		{
			if (_model.SelectedCount > 0)
			{
				if (handleImmediately && _commandType == eCommandType.move)
					_moveHelper.HandleMouseDownForMove(GetMousePoint(), _snappoint);
				_commandType = eCommandType.move;
				UpdateCursor();
			}
		}
		public void CommandDeleteSelected()
		{
			_model.DeleteObjects(_model.SelectedObjects);
			_model.ClearSelectedObjects();
			DoInvalidate(true);
			UpdateCursor();
		}
		public void CommandEdit(string editid)
		{
			CommandEscape();
			_model.ClearSelectedObjects();
			_commandType = eCommandType.edit;
			_editToolId = editid;
			_editTool = _model.GetEditTool(_editToolId);
			UpdateCursor();
		}
		void HandleQuickSnap(KeyEventArgs e)
		{
			if (_commandType == eCommandType.select || _commandType == eCommandType.pan)
				return;
			INSSnapPoint p = null;
			UnitPoint mousepoint = GetMousePoint();
			if (m_QuickSnap.ContainsKey(e.KeyCode))
				p = _model.SnapPoint(_canvaswrapper, mousepoint, null, m_QuickSnap[e.KeyCode]);
			if (p != null)
			{
				if (_commandType == eCommandType.draw)
				{
					HandleMouseDownWhenDrawing(p.SnapPoint, p);
					if (_newObject != null)
						_newObject.OnMouseMove(_canvaswrapper, GetMousePoint());
					DoInvalidate(true);
					e.Handled = true;
				}
				if (_commandType == eCommandType.move)
				{
					_moveHelper.HandleMouseDownForMove(p.SnapPoint, p);
					e.Handled = true;
				}
				if (_nodeMoveHelper.IsEmpty == false)
				{
					bool handled = false;
					_nodeMoveHelper.HandleMouseDown(p.SnapPoint, ref handled);
					FinishNodeEdit();
					e.Handled = true;
				}
				if (_commandType == eCommandType.edit)
				{
				}
			}
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			HandleQuickSnap(e);

			if (_nodeMoveHelper.IsEmpty == false)
			{
				_nodeMoveHelper.OnKeyDown(_canvaswrapper, e);
				if (e.Handled)
					return;
			}
			base.OnKeyDown(e);
			if (e.Handled)
			{
				UpdateCursor();
				return;
			}
			if (_editTool != null)
			{
				_editTool.OnKeyDown(_canvaswrapper, e);
				if (e.Handled)
					return;
			}
			if (_newObject != null)
			{
				_newObject.OnKeyDown(_canvaswrapper, e);
				if (e.Handled)
					return;
			}
			foreach (INSDrawObject obj in _model.SelectedObjects)
			{
				obj.OnKeyDown(_canvaswrapper, e);
				if (e.Handled)
					return;
			}

			if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
			{
				if (e.KeyCode == Keys.G)
				{
					_model.GridLayer.Enabled = !_model.GridLayer.Enabled;
					DoInvalidate(true);
				}
				if (e.KeyCode == Keys.S)
				{
					RunningSnapsEnabled = !RunningSnapsEnabled;
					if (!RunningSnapsEnabled)
						_snappoint = null;
					DoInvalidate(false);
				}
				return;
			}

			if (e.KeyCode == Keys.Escape)
			{
				CommandEscape();
			}
			if (e.KeyCode == Keys.P)
			{
				CommandPan();
			}
			if (e.KeyCode == Keys.S)
			{
				RunningSnapsEnabled = !RunningSnapsEnabled;
				if (!RunningSnapsEnabled)
					_snappoint = null;
				DoInvalidate(false);
			}
			if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
			{
				int layerindex = (int)e.KeyCode - (int)Keys.D1;
				if (layerindex >=0 && layerindex < _model.Layers.Length)
				{
					_model.ActiveLayer = _model.Layers[layerindex];
					DoInvalidate(true);
				}
			}
			if (e.KeyCode == Keys.Delete)
			{
				CommandDeleteSelected();
			}
			if (e.KeyCode == Keys.O)
			{
				CommandEdit("linesmeet");
			}
			UpdateCursor();
		}
	}

	//INSCanvas impl
  	public struct CanvasWrapper : INSCanvas
	{
		Canvas m_canvas; 
		Graphics m_graphics;
		Rectangle m_rect;
		public CanvasWrapper(Canvas canvas)
		{
			m_canvas = canvas;
			m_graphics = null;
			m_rect = new Rectangle();
		}
		public CanvasWrapper(Canvas canvas, Graphics graphics, Rectangle clientrect)
		{
			m_canvas = canvas;
			m_graphics = graphics;
			m_rect = clientrect;
		}
		public INSModel Model
		{
			get { return m_canvas.Model; }
		}

		public Canvas Canvas
		{
			get { return m_canvas; }
		}

		public void Dispose()
		{
			m_graphics = null;
		}

		#region INSCanvas Members
		public INSModel DataModel
		{
			get { return m_canvas.Model; }
		}
		public UnitPoint ScreenTopLeftToUnitPoint()
		{
			return m_canvas.ScreenTopLeftToUnitPoint();
		}
		public UnitPoint ScreenBottomRightToUnitPoint()
		{
			return m_canvas.ScreenBottomRightToUnitPoint();
		}
		public PointF ToScreen(UnitPoint unitpoint)
		{
			return m_canvas.ToScreen(unitpoint);
		}
		public float ToScreen(double unitvalue)
		{
			return m_canvas.ToScreen(unitvalue);
		}
		public double ToUnit(float screenvalue)
		{
			return m_canvas.ToUnit(screenvalue);
		}
		public UnitPoint ToUnit(PointF screenpoint)
		{
			return m_canvas.ToUnit(screenpoint);
		}
		public Graphics Graphics
		{
			get { return m_graphics; }
		}
		public Rectangle ClientRectangle
		{
			get { return m_rect; }
			set { m_rect = value; }
		}
		public Pen CreatePen(Color color, float unitWidth)
		{
			return m_canvas.CreatePen(color, unitWidth);
		}
		public void DrawLine(INSCanvas canvas, Pen pen, UnitPoint p1, UnitPoint p2)
		{
			m_canvas.DrawLine(canvas, pen, p1, p2);
		}
		public void DrawArc(INSCanvas canvas, Pen pen, UnitPoint center, float radius, float beginangle, float angle)
		{
			m_canvas.DrawArc(canvas, pen, center, radius, beginangle, angle);
		}

		//再描画
		public void Invalidate()
		{
			m_canvas.DoInvalidate(false);
		}
		public INSDrawObject CurrentObject
		{
			get { return m_canvas.NewObject; }
		}
		#endregion
	}
}
