using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSCAD
{
	//描画オブジェクト
	public interface INSDrawObject
	{
		string Id { get; }
		INSDrawObject Clone();
		bool PointInObject(INSCanvas canvas, UnitPoint point);
		bool ObjectInRectangle(INSCanvas canvas, RectangleF rect, bool anyPoint);
		void Draw(INSCanvas canvas, RectangleF unitrect);
		RectangleF GetBoundingRect(INSCanvas canvas);
		void OnMouseMove(INSCanvas canvas, UnitPoint point);
		eDrawObjectMouseDown OnMouseDown(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint);
		void OnMouseUp(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint);
		void OnKeyDown(INSCanvas canvas, KeyEventArgs e);
		UnitPoint RepeatStartingPoint { get; }
		INSNodePoint NodePoint(INSCanvas canvas, UnitPoint point);
		INSSnapPoint SnapPoint(INSCanvas canvas, UnitPoint point, List<INSDrawObject> otherobj, Type[] runningsnaptypes, Type usersnaptype);
		void Move(UnitPoint offset);

		string GetInfoAsString();
	}
}
