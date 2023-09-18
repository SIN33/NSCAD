using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSCAD
{
	//tool i.e. draw line
	public interface INSEditTool
	{
		INSEditTool Clone();

		bool SupportSelection { get; }
		void SetHitObjects(UnitPoint mousepoint, List<INSDrawObject> list);

		void OnMouseMove(INSCanvas canvas, UnitPoint point);
		eDrawObjectMouseDown OnMouseDown(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint);
		void OnMouseUp(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint);
		void OnKeyDown(INSCanvas canvas, KeyEventArgs e);
		void Finished();
		void Undo();
		void Redo();
	}
}
