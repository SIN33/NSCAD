using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace NSCAD
{
	public class DrawingLayer : INSCanvasLayer, INSSerialize
	{
		List<INSDrawObject> _objects = new List<INSDrawObject>();
		Dictionary<INSDrawObject, bool> _objectMap = new Dictionary<INSDrawObject, bool>();
		string _id;
		string _name = "<Layer>";
		Color _color;
		float _width = 0.00f;
		bool _enabled = true;
		bool _visible = true;
		[XmlSerializable]
		public Color Color
		{
			get { return _color; }
			set { _color = value; }
		}
		[XmlSerializable]
		public float Width
		{
			get { return _width; }
			set { _width = value; }
		}
		[XmlSerializable]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		public DrawingLayer(string id, string name, Color color, float width)
		{
			_id = id;
			_name = name;
			_color = color;
			_width = width;
		}

		public void AddObject(INSDrawObject drawobject)
		{
			if (_objectMap.ContainsKey(drawobject))
				return; // this should never happen
			if (drawobject is DrawObjectBase)
				((DrawObjectBase)drawobject).Layer = this;
			_objects.Add(drawobject);
			_objectMap[drawobject] = true;
		}
		public List<INSDrawObject> DeleteObjects(IEnumerable<INSDrawObject> objects)
		{
			if (Enabled == false)
				return null;
			List<INSDrawObject> removedobjects = new List<INSDrawObject>();
			// first remove from map only
			foreach (INSDrawObject obj in objects)
			{
				if (_objectMap.ContainsKey(obj))
				{
					_objectMap.Remove(obj);
					removedobjects.Add(obj);
				}
			}
			// need some smart algorithm here to either remove from existing list or build a new list
			// for now I will just ise the removed count;
			if (removedobjects.Count == 0)
				return null;
			if (removedobjects.Count < 10) // remove from existing list
			{
				foreach (INSDrawObject obj in removedobjects)
					_objects.Remove(obj);
			}
			else // else build new list;
			{
				List<INSDrawObject> newlist = new List<INSDrawObject>();
				foreach (INSDrawObject obj in _objects)
				{
					if (_objectMap.ContainsKey(obj))
						newlist.Add(obj);
				}
				_objects.Clear();
				_objects = newlist;
			}
			return removedobjects;
		}
		public int Count
		{
			get { return _objects.Count; }
		}
		public void Copy(DrawingLayer acopy, bool includeDrawObjects)
		{
			if (includeDrawObjects)
				throw new Exception("not supported yet");
			_id = acopy._id;
			_name = acopy._name;
			_color = acopy._color;
			_width = acopy._width;
			_enabled = acopy._enabled;
			_visible = acopy._visible;
		}
		#region ICanvasLayer Members

		public void Draw(INSCanvas canvas, RectangleF unitrect)
		{
			Tracing.StartTrack(Program.TracePaint);
			int cnt = 0;
			foreach (INSDrawObject drawobject in _objects)
			{
				var obj = drawobject as DrawObjectBase;
				if (obj is INSDrawObject && ((INSDrawObject)obj).ObjectInRectangle(canvas, unitrect, true) == false)
					continue;
				bool sel = obj.Selected;
				bool high = obj.Highlighted;
				obj.Selected = false;
				drawobject.Draw(canvas, unitrect);
				obj.Selected = sel;
				obj.Highlighted = high;
				cnt++;
			}
			Tracing.EndTrack(Program.TracePaint, "Draw Layer {0}, ObjCount {1}, Painted ObjCount {2}", Id, _objects.Count, cnt);
		}
		public PointF SnapPoint(PointF unitmousepoint)
		{
			return PointF.Empty;
		}
		public string Id
		{
			get { return _id; }
		}
		public INSSnapPoint SnapPoint(INSCanvas canvas, UnitPoint point, List<INSDrawObject> otherobj)
		{
			foreach (INSDrawObject obj in _objects)
			{
				INSSnapPoint sp = obj.SnapPoint(canvas, point, otherobj, null, null);
				if (sp != null)
					return sp;
			}
			return null;
		}
		public IEnumerable<INSDrawObject> Objects
		{
			get { return _objects; }
		}
		[XmlSerializable]
		public bool Enabled
		{
			get { return _enabled && _visible; }
			set { _enabled = value; }
		}
		[XmlSerializable]
		public bool Visible
		{
			get { return _visible; }
			set { _visible = value; }
		}
		#endregion
		#region XML Serialize
		public void GetObjectData(XmlWriter wr)
		{
			wr.WriteStartElement("layer");
			wr.WriteAttributeString("Id", _id);
			XmlUtil.WriteProperties(this, wr);
			wr.WriteStartElement("items");
			foreach (INSDrawObject drawobj in _objects)
			{
				if (drawobj is INSSerialize)
					((INSSerialize)drawobj).GetObjectData(wr);
			}
			wr.WriteEndElement();
			wr.WriteEndElement();
		}
		public void AfterSerializedIn()
		{
		}
		public static DrawingLayer NewLayer(XmlElement xmlelement)
		{
			string id = xmlelement.GetAttribute("Id");
			if (id.Length == 0)
				id = Guid.NewGuid().ToString();
			DrawingLayer layer = new DrawingLayer(id, string.Empty, Color.White, 0.0f);
			foreach (XmlElement node in xmlelement.ChildNodes)
			{
				XmlUtil.ParseProperty(node, layer);
				if (node.Name == "items")
				{
					foreach (XmlElement itemnode in node.ChildNodes)
					{
						object item = DataModel.NewDrawObject(itemnode.Name);
						if (item == null)
							continue;

						if (item != null)
							XmlUtil.ParseProperties(itemnode, item);
						
						if (item is INSSerialize)
						   ((INSSerialize)item).AfterSerializedIn();

						if (item is INSDrawObject)
							layer.AddObject(item as INSDrawObject);
					}
				}
			}
			return layer;
		}
		#endregion
	}
}
