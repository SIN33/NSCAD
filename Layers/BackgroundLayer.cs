using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml;

namespace NSCAD
{
	//îwåiêF
	public class BackgroundLayer : INSCanvasLayer, INSSerialize
	{
		Font m_font = new System.Drawing.Font("Arial Black", 25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
		SolidBrush _brush = new SolidBrush(Color.FromArgb(50, 200, 200, 200));
		SolidBrush _backgroundBrush;

		Color m_color = Color.Black;
		[XmlSerializable]
		public Color Color
		{
			get { return m_color; }
			set
			{
				m_color = value;
				_backgroundBrush = new SolidBrush(m_color);
			}
		}

		public BackgroundLayer()
		{
			_backgroundBrush = new SolidBrush(m_color);
		}

		#region ICanvasLayer Members
		public void Draw(INSCanvas canvas, RectangleF unitrect)
		{
			RectangleF r = ScreenUtils.ToScreenNormalized(canvas, unitrect);
			canvas.Graphics.FillRectangle(_backgroundBrush, r);
			StringFormat f = new StringFormat();
			f.Alignment = StringAlignment.Center;
			PointF centerpoint = new PointF(r.Width / 2, r.Height / 2);
			canvas.Graphics.TranslateTransform(centerpoint.X, centerpoint.Y);
			canvas.Graphics.RotateTransform(-15);
			canvas.Graphics.ResetTransform();
		}
		public PointF SnapPoint(PointF unitmousepoint)
		{
			return PointF.Empty;
		}
		public string Id
		{
			get { return "background"; }
		}

		INSSnapPoint INSCanvasLayer.SnapPoint(INSCanvas canvas, UnitPoint point, List<INSDrawObject> otherobj)
		{
			throw new Exception("The method or operation is not implemented");
		}

		public IEnumerable<INSDrawObject> Objects
		{
			get { return null; }
		}
		public bool Enabled
		{
			get { return false; }
			set {;}
		}
		public bool Visible
		{
			get { return true; }
			set { ;}
		}
		#endregion
		#region INSSerialize
		public void GetObjectData(XmlWriter wr)
		{
			wr.WriteStartElement("backgroundlayer");
			XmlUtil.WriteProperties(this, wr);
			wr.WriteEndElement();
		}
		public void AfterSerializedIn()
		{
		}
		#endregion
	}
}
