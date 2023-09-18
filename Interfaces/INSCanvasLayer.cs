using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSCAD
{
	//Canvasの画層
	public interface INSCanvasLayer
	{
		string Id { get; }
		void Draw(INSCanvas canvas, RectangleF unitrect);
		INSSnapPoint SnapPoint(INSCanvas canvas, UnitPoint point, List<INSDrawObject> otherobj);
		IEnumerable<INSDrawObject> Objects { get; }
		bool Enabled { get; set; }
		bool Visible { get; }
	}
}
