using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSCAD
{
	//Canvasの操作を提供
   	public interface INSCanvasOwner
	{
		void SetPositionInfo(UnitPoint unitpos);
        void SetSnapInfo(INSSnapPoint snap);
    }

	//screen 座標変換等を行う
	//viewに相当
	public interface INSCanvas
	{
		INSModel DataModel { get; }
		UnitPoint ScreenTopLeftToUnitPoint();
		UnitPoint ScreenBottomRightToUnitPoint();
		PointF ToScreen(UnitPoint unitpoint);
		float ToScreen(double unitvalue);
		double ToUnit(float screenvalue);
		UnitPoint ToUnit(PointF screenpoint);

		void Invalidate();
		INSDrawObject CurrentObject { get; }

		Rectangle ClientRectangle { get; }
		Graphics Graphics { get; }
		Pen CreatePen(Color color, float unitWidth);
		void DrawLine(INSCanvas canvas, Pen pen, UnitPoint p1, UnitPoint p2);
		void DrawArc(INSCanvas canvas, Pen pen, UnitPoint center, float radius, float beginangle, float angle);
	}


	public enum eDrawObjectMouseDown
	{
		Done,		// this draw object is complete
		DoneRepeat,	// this object is complete, but create new object of same type
		Continue,	// this object requires additional mouse inputs
	}



	public interface IEditToolOwner
	{
		void SetHint(string text);
	}

}
