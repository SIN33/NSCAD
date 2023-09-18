using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSCAD
{
  	class CursorCollection
	{
		Dictionary<object, Cursor> m_map = new Dictionary<object, Cursor>();
		public void AddCursor(object key, Cursor cursor)
		{
			m_map[key] = cursor;
		}

		public void AddCursor(object key, string resourcename)
		{
			string name = "Resources." + resourcename;
			Type type = GetType();
			Cursor cursor = new Cursor(GetType(), name);
			m_map[key] = cursor;
		}

		public Cursor GetCursor(object key)
		{
			if (m_map.ContainsKey(key))
				return m_map[key];
			return Cursors.Arrow;
		}
	}
}
