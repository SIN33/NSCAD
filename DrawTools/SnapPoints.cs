using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NSCAD
{
	class SnapPointBase : INSSnapPoint
	{
		private INSDrawObject	_owner;
		private UnitPoint		_snappoint;
		private RectangleF	_boundingRect;
		public INSDrawObject Owner
		{
			get { return _owner; }
		}
		public SnapPointBase(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
		{
			_owner = owner;
			_snappoint = snappoint;
			float size = (float)canvas.ToUnit(14);
			_boundingRect.X = (float)(snappoint.X - size / 2);
			_boundingRect.Y = (float)(snappoint.Y - size / 2);
			_boundingRect.Width = size;
			_boundingRect.Height = size;
		}
		#region INSSnapPoint Members Impl
		public virtual UnitPoint SnapPoint
		{
			get { return _snappoint; }
		}
		public virtual RectangleF BoundingRect
		{
			get { return _boundingRect; }
		}
		public virtual void Draw(INSCanvas canvas)
		{
		}
		#endregion

		protected void DrawPoint(INSCanvas canvas, Pen pen, Brush fillBrush)
		{
			Rectangle screenrect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(canvas, _boundingRect));
			canvas.Graphics.DrawRectangle(pen, screenrect);
			screenrect.X++;
			screenrect.Y++;
			screenrect.Width--;
			screenrect.Height--;
			if (fillBrush != null)
				canvas.Graphics.FillRectangle(fillBrush, screenrect);
		}
	}
	class GridSnapPoint : SnapPointBase
	{
		public GridSnapPoint(INSCanvas canvas, UnitPoint snappoint)
			: base(canvas, null, snappoint)
		{
		}
		#region INSSnapPoint Members
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.Gray, null);
		}
		#endregion
	}
	class VertextSnapPoint : SnapPointBase
	{
		public VertextSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.Blue, Brushes.YellowGreen);
		}
	}
	class MidpointSnapPoint : SnapPointBase
	{
		public MidpointSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
	class IntersectSnapPoint : SnapPointBase
	{
		public IntersectSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
	class NearestSnapPoint : SnapPointBase
	{
		public NearestSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		#region INSSnapPoint Members Impl
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
		#endregion
	}
	class QuadrantSnapPoint : SnapPointBase
	{
		public QuadrantSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
	class DivisionSnapPoint : SnapPointBase
	{
		public DivisionSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
	class CenterSnapPoint : SnapPointBase
	{
		public CenterSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
	class PerpendicularSnapPoint : SnapPointBase
	{
		public PerpendicularSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
	class TangentSnapPoint : SnapPointBase
	{
		public TangentSnapPoint(INSCanvas canvas, INSDrawObject owner, UnitPoint snappoint)
			: base(canvas, owner, snappoint)
		{
		}
		public override void Draw(INSCanvas canvas)
		{
			DrawPoint(canvas, Pens.White, Brushes.YellowGreen);
		}
	}
}
