using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;

namespace NSCAD
{
	class NodePointLine : INSNodePoint
	{
		public enum ePoint
		{
			P1,
			P2,
		}
		static bool m_angleLocked = false;
		Line _owner;
		Line _clone;
		UnitPoint m_originalPoint;
		UnitPoint m_endPoint;
		ePoint m_pointId;
		public NodePointLine(Line owner, ePoint id)
		{
			_owner = owner;
			_clone = _owner.Clone() as Line;
			m_pointId = id;
			m_originalPoint = GetPoint(m_pointId);
		}
		#region INSNodePoint Members
		public INSDrawObject GetClone()
		{
			return _clone;
		}
		public INSDrawObject GetOriginal()
		{
			return _owner;
		}
		public void SetPosition(UnitPoint pos)
		{
			if (Control.ModifierKeys == Keys.Control)
				pos = HitUtil.OrthoPointD(OtherPoint(m_pointId), pos, 45);
			if (m_angleLocked || Control.ModifierKeys == (Keys)(Keys.Control | Keys.Shift))
				pos = HitUtil.NearestPointOnLine(_owner.P1, _owner.P2, pos, true);
			SetPoint(m_pointId, pos, _clone);
		}
		public void Finish()
		{
			m_endPoint = GetPoint(m_pointId);
			_owner.P1 = _clone.P1;
			_owner.P2 = _clone.P2;
			_clone = null;
		}
		public void Cancel()
		{
		}
		public void Undo()
		{
			SetPoint(m_pointId, m_originalPoint, _owner);
		}
		public void Redo()
		{
			SetPoint(m_pointId, m_endPoint, _owner);
		}
		public void OnKeyDown(INSCanvas canvas, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.L)
			{
				m_angleLocked = !m_angleLocked;
				e.Handled = true;
			}
		}
		#endregion
		protected UnitPoint GetPoint(ePoint pointid)
		{
			if (pointid == ePoint.P1)
				return _clone.P1;
			if (pointid == ePoint.P2)
				return _clone.P2;
			return _owner.P1;
		}
		protected UnitPoint OtherPoint(ePoint currentpointid)
		{
			if (currentpointid == ePoint.P1)
				return GetPoint(ePoint.P2);
			return GetPoint(ePoint.P1);
		}
		protected void SetPoint(ePoint pointid, UnitPoint point, Line line)
		{
			if (pointid == ePoint.P1)
				line.P1 = point;
			if (pointid == ePoint.P2)
				line.P2 = point;
		}
	}
	class Line : DrawObjectBase, INSDrawObject, INSSerialize
	{
		protected UnitPoint _p1, _p2;

		[XmlSerializable]
		public UnitPoint P1
		{
			get { return _p1; }
			set { _p1 = value; }
		}
		[XmlSerializable]
		public UnitPoint P2
		{
			get { return _p2; }
			set { _p2 = value; }
		}

		public static string ObjectType
		{
			get { return "line"; }
		}
		public Line()
		{
		}
		public Line(UnitPoint point, UnitPoint endpoint, float width, Color color)
		{
			P1 = point;
			P2 = endpoint;
			Width = width;
			Color = color;
			Selected = false;
		}
		public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, INSSnapPoint snap)
		{
			P1 = P2 = point;
			Width = layer.Width;
			Color = layer.Color;
			Selected = true;
		}

		static int ThresholdPixel = 6; //“üŒû
		static float ThresholdWidth(INSCanvas canvas, float objectwidth)
		{
			return ThresholdWidth(canvas, objectwidth, ThresholdPixel);
		}
		public static float ThresholdWidth(INSCanvas canvas, float objectwidth, float pixelwidth)
		{
			double minWidth = canvas.ToUnit(pixelwidth);
			double width = Math.Max(objectwidth / 2, minWidth);
			return (float)width;
		}
		public virtual void Copy(Line acopy)
		{
			base.Copy(acopy);
			_p1 = acopy._p1;
			_p2 = acopy._p2;
			Selected = acopy.Selected;
		}
		#region INSDrawObject Members Impl
		public virtual string Id
		{
			get { return ObjectType; }
		}
		public virtual INSDrawObject Clone()
		{
			Line l = new Line();
			l.Copy(this);
			return l;
		}
		public RectangleF GetBoundingRect(INSCanvas canvas)
		{
			float thWidth = ThresholdWidth(canvas, Width);
			return ScreenUtils.GetRect(_p1, _p2, thWidth);
		}

		UnitPoint MidPoint(INSCanvas canvas, UnitPoint p1, UnitPoint p2, UnitPoint hitpoint)
		{
			UnitPoint mid = HitUtil.LineMidpoint(p1, p2);
			float thWidth = ThresholdWidth(canvas, Width);
			if (HitUtil.CircleHitPoint(mid, thWidth, hitpoint))
				return mid;
			return UnitPoint.Empty;
		}
		public bool PointInObject(INSCanvas canvas, UnitPoint point)
		{
			float thWidth = ThresholdWidth(canvas, Width);
			return HitUtil.IsPointInLine(_p1, _p2, point, thWidth);
		}
		public bool ObjectInRectangle(INSCanvas canvas, RectangleF rect, bool anyPoint)
		{
			RectangleF boundingrect = GetBoundingRect(canvas);
			if (anyPoint)
				return HitUtil.LineIntersectWithRect(_p1, _p2, rect);
			return rect.Contains(boundingrect);
		}
		public virtual void Draw(INSCanvas canvas, RectangleF unitrect)
		{
			Color color = Color;
			Pen pen = canvas.CreatePen(color, Width);
			pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
			pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
			canvas.DrawLine(canvas, pen, _p1, _p2);
			if (Highlighted)
				canvas.DrawLine(canvas, DrawUtils.SelectedPen, _p1, _p2);
			if (Selected)
			{
				canvas.DrawLine(canvas, DrawUtils.SelectedPen, _p1, _p2);
				if (_p1.IsEmpty == false)
					DrawUtils.DrawNode(canvas, _p1);
				if (_p2.IsEmpty == false)
					DrawUtils.DrawNode(canvas, _p2);
			}
		}
		public virtual void OnMouseMove(INSCanvas canvas, UnitPoint point)
		{
			if (Control.ModifierKeys == Keys.Control)
				point = HitUtil.OrthoPointD(_p1, point, 45);
			_p2 = point;
		}
		public virtual eDrawObjectMouseDown OnMouseDown(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint)
		{
			Selected = false;
			if (snappoint is PerpendicularSnapPoint && snappoint.Owner is Line)
			{
				Line src = snappoint.Owner as Line;
				_p2 = HitUtil.NearestPointOnLine(src.P1, src.P2, _p1, true);
				return eDrawObjectMouseDown.DoneRepeat;
			}
			if (snappoint is PerpendicularSnapPoint && snappoint.Owner is Arc)
			{
				Arc src = snappoint.Owner as Arc;
				_p2 = HitUtil.NearestPointOnCircle(src.Center, src.Radius, _p1, 0);
				return eDrawObjectMouseDown.DoneRepeat;
			}
			if (Control.ModifierKeys == Keys.Control)
				point = HitUtil.OrthoPointD(_p1, point, 45);
			_p2 = point;
			return eDrawObjectMouseDown.DoneRepeat;
		}
		public void OnMouseUp(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint)
		{
		}
		public virtual void OnKeyDown(INSCanvas canvas, KeyEventArgs e)
		{
		}
		public UnitPoint RepeatStartingPoint
		{
			get { return _p2; }
		}
		public INSNodePoint NodePoint(INSCanvas canvas, UnitPoint point)
		{
			float thWidth = ThresholdWidth(canvas, Width);
			if (HitUtil.CircleHitPoint(_p1, thWidth, point))
				return new NodePointLine(this, NodePointLine.ePoint.P1);
			if (HitUtil.CircleHitPoint(_p2, thWidth, point))
				return new NodePointLine(this, NodePointLine.ePoint.P2);
			return null;
		}
		public INSSnapPoint SnapPoint(INSCanvas canvas, UnitPoint point, List<INSDrawObject> otherobjs, Type[] runningsnaptypes, Type usersnaptype)
		{
			float thWidth = ThresholdWidth(canvas, Width);
			if (runningsnaptypes != null)
			{
				foreach (Type snaptype in runningsnaptypes)
				{
					if (snaptype == typeof(VertextSnapPoint))
					{
						if (HitUtil.CircleHitPoint(_p1, thWidth, point))
							return new VertextSnapPoint(canvas, this, _p1);
						if (HitUtil.CircleHitPoint(_p2, thWidth, point))
							return new VertextSnapPoint(canvas, this, _p2);
					}
					if (snaptype == typeof(MidpointSnapPoint))
					{
						UnitPoint p = MidPoint(canvas, _p1, _p2, point);
						if (p != UnitPoint.Empty)
							return new MidpointSnapPoint(canvas, this, p);
					}
					if (snaptype == typeof(IntersectSnapPoint))
					{
						Line otherline = Utils.FindObjectTypeInList(this, otherobjs, typeof(Line)) as Line;
						if (otherline == null)
							continue;
						UnitPoint p = HitUtil.LinesIntersectPoint(_p1, _p2, otherline._p1, otherline._p2);
						if (p != UnitPoint.Empty)
							return new IntersectSnapPoint(canvas, this, p);
					}
				}
				return null;
			}

			if (usersnaptype == typeof(MidpointSnapPoint))
				return new MidpointSnapPoint(canvas, this, HitUtil.LineMidpoint(_p1, _p2));
			if (usersnaptype == typeof(IntersectSnapPoint))
			{
				Line otherline = Utils.FindObjectTypeInList(this, otherobjs, typeof(Line)) as Line;
				if (otherline == null)
					return null;
				UnitPoint p = HitUtil.LinesIntersectPoint(_p1, _p2, otherline._p1, otherline._p2);
				if (p != UnitPoint.Empty)
					return new IntersectSnapPoint(canvas, this, p);
			}
			if (usersnaptype == typeof(VertextSnapPoint))
			{
				double d1 = HitUtil.Distance(point, _p1);
				double d2 = HitUtil.Distance(point, _p2);
				if (d1 <= d2)
					return new VertextSnapPoint(canvas, this, _p1);
				return new VertextSnapPoint(canvas, this, _p2);
			}
			if (usersnaptype == typeof(NearestSnapPoint))
			{
				UnitPoint p = HitUtil.NearestPointOnLine(_p1, _p2, point);
				if (p != UnitPoint.Empty)
					return new NearestSnapPoint(canvas, this, p);
			}
			if (usersnaptype == typeof(PerpendicularSnapPoint))
			{
				UnitPoint p = HitUtil.NearestPointOnLine(_p1, _p2, point);
				if (p != UnitPoint.Empty)
					return new PerpendicularSnapPoint(canvas, this, p);
			}
			return null;
		}
		public void Move(UnitPoint offset)
		{
			_p1.X += offset.X;
			_p1.Y += offset.Y;
			_p2.X += offset.X;
			_p2.Y += offset.Y;
		}
		public string GetInfoAsString()
		{
			return string.Format("’¼ü{0},{1} - 2“_ŠÔ‹——£ = {2:f4} - Šp“x{3:f4}",
				P1.PosAsString(),
				P2.PosAsString(),
				HitUtil.Distance(P1, P2),
				HitUtil.RadiansToDegrees(HitUtil.LineAngleR(P1, P2, 0)));
		}
		#endregion
		#region ISerialize
		public void GetObjectData(XmlWriter wr)
		{
			wr.WriteStartElement("line");
			XmlUtil.WriteProperties(this, wr);
			wr.WriteEndElement();
		}
		public void AfterSerializedIn()
		{
		}
		#endregion

		public void ExtendLineToPoint(UnitPoint newpoint)
		{
			UnitPoint newlinepoint = HitUtil.NearestPointOnLine(P1, P2, newpoint, true);
			if (HitUtil.Distance(newlinepoint, P1) < HitUtil.Distance(newlinepoint, P2))
				P1 = newlinepoint;
			else
				P2 = newlinepoint;
		}
	}
	class LineEdit : Line, IObjectEditInstance
	{
		protected PerpendicularSnapPoint m_perSnap;
		protected TangentSnapPoint m_tanSnap;
		protected bool m_tanReverse = false;
		protected bool m_singleLineSegment = true;

		public override string Id
		{
			get
			{
				if (m_singleLineSegment)
					return "line";
				return "lines";
			}
		}
		public LineEdit(bool singleLine)
			: base()
		{
			m_singleLineSegment = singleLine;
		}

		public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, INSSnapPoint snap)
		{
			base.InitializeFromModel(point, layer, snap);
			m_perSnap = snap as PerpendicularSnapPoint;
			m_tanSnap = snap as TangentSnapPoint;
		}
		public override void OnMouseMove(INSCanvas canvas, UnitPoint point)
		{
			if (m_perSnap != null)
			{
				MouseMovePerpendicular(canvas, point);
				return;
			}
			if (m_tanSnap != null)
			{
				MouseMoveTangent(canvas, point);
				return;
			}
			base.OnMouseMove(canvas, point);
		}
		public override eDrawObjectMouseDown OnMouseDown(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint)
		{
			if (m_tanSnap != null && Control.MouseButtons == MouseButtons.Right)
			{
				ReverseTangent(canvas);
				return eDrawObjectMouseDown.Continue;
			}

			if (m_perSnap != null || m_tanSnap != null)
			{
				if (snappoint != null)
					point = snappoint.SnapPoint;
				OnMouseMove(canvas, point);
				if (m_singleLineSegment)
					return eDrawObjectMouseDown.Done;
				return eDrawObjectMouseDown.DoneRepeat;
			}
			eDrawObjectMouseDown result = base.OnMouseDown(canvas, point, snappoint);
			if (m_singleLineSegment)
				return eDrawObjectMouseDown.Done;
			return eDrawObjectMouseDown.DoneRepeat;
		}
		public override void OnKeyDown(INSCanvas canvas, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.R && m_tanSnap != null)
			{
				ReverseTangent(canvas);
				e.Handled = true;
			}
		}
		protected virtual void MouseMovePerpendicular(INSCanvas canvas, UnitPoint point)
		{
			if (m_perSnap.Owner is Line)
			{
				Line src = m_perSnap.Owner as Line;
				_p1 = HitUtil.NearestPointOnLine(src.P1, src.P2, point, true);
				_p2 = point;
			}
			if (m_perSnap.Owner is IArc)
			{
				IArc src = m_perSnap.Owner as IArc;
				_p1 = HitUtil.NearestPointOnCircle(src.Center, src.Radius, point, 0);
				_p2 = point;
			}
		}
		protected virtual void MouseMoveTangent(INSCanvas canvas, UnitPoint point)
		{
			if (m_tanSnap.Owner is IArc)
			{
				IArc src = m_tanSnap.Owner as IArc;
				_p1 = HitUtil.TangentPointOnCircle(src.Center, src.Radius, point, m_tanReverse);
				_p2 = point;
				if (_p1 == UnitPoint.Empty)
					_p2 = _p1 = src.Center;
			}
		}
		protected virtual void ReverseTangent(INSCanvas canvas)
		{
			m_tanReverse = !m_tanReverse;
			MouseMoveTangent(canvas, _p2);
			canvas.Invalidate();
		}

		public void Copy(LineEdit acopy)
		{
			base.Copy(acopy);
			m_perSnap = acopy.m_perSnap;
			m_tanSnap = acopy.m_tanSnap;
			m_tanReverse = acopy.m_tanReverse;
			m_singleLineSegment = acopy.m_singleLineSegment;
		}
		public override INSDrawObject Clone()
		{
			LineEdit l = new LineEdit(false);
			l.Copy(this);
			return l;
		}

		#region IObjectEditInstance
		public INSDrawObject GetDrawObject()
		{
			return new Line(P1, P2, Width, Color);
		}
		#endregion
	}
}
