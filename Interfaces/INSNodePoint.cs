using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSCAD
{
	//個々の描画オブジェクトの操作を提供
  	public interface INSNodePoint
	{
		INSDrawObject GetClone();
		INSDrawObject GetOriginal();
		void Cancel();
		void Finish();
		void SetPosition(UnitPoint pos);
		void Undo();
		void Redo();
		void OnKeyDown(INSCanvas canvas, KeyEventArgs e);
	}
}
