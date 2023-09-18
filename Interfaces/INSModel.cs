using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NSCAD.INSCanvas;

namespace NSCAD
{
	public interface INSModel
	{
		float Zoom { get; set; }
		INSCanvasLayer BackgroundLayer { get; }
		INSCanvasLayer GridLayer { get; }
		INSCanvasLayer[] Layers { get; }
		INSCanvasLayer ActiveLayer { get; set; }
		INSCanvasLayer GetLayer(string id);
		INSDrawObject CreateObject(string type, UnitPoint point, INSSnapPoint snappoint);
		void AddObject(INSCanvasLayer layer, INSDrawObject drawobject);
		void DeleteObjects(IEnumerable<INSDrawObject> objects);
		void MoveObjects(UnitPoint offset, IEnumerable<INSDrawObject> objects);
		void CopyObjects(UnitPoint offset, IEnumerable<INSDrawObject> objects);
		void MoveNodes(UnitPoint position, IEnumerable<INSNodePoint> nodes);

		INSEditTool GetEditTool(string id);
		void AfterEditObjects(INSEditTool edittool);

		List<INSDrawObject> GetHitObjects(INSCanvas canvas, RectangleF selection, bool anyPoint);
		List<INSDrawObject> GetHitObjects(INSCanvas canvas, UnitPoint point);
		bool IsSelected(INSDrawObject drawobject);
		void AddSelectedObject(INSDrawObject drawobject);
		void RemoveSelectedObject(INSDrawObject drawobject);
		IEnumerable<INSDrawObject> SelectedObjects { get; }
		int SelectedCount { get; }
		void ClearSelectedObjects();

		INSSnapPoint SnapPoint(INSCanvas canvas, UnitPoint point, Type[] runningsnaptypes, Type usersnaptype);

		bool CanUndo();
		bool DoUndo();
		bool CanRedo();
		bool DoRedo();
	}
}
