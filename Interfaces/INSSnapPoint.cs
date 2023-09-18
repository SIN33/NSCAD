using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSCAD
{
	//snap proc
 	public interface INSSnapPoint
	{
		INSDrawObject	Owner			{ get; }
		UnitPoint	SnapPoint		{ get; }
		RectangleF	BoundingRect	{ get; }
		void Draw(INSCanvas canvas);
	}
}
