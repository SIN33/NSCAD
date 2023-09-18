  using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Reflection;

namespace NSCAD
{

	public class DataModel : INSModel
	{
		static Dictionary<string, Type> m_toolTypes = new Dictionary<string,Type>();
		static public INSDrawObject NewDrawObject(string objecttype)
		{
			if (m_toolTypes.ContainsKey(objecttype))
			{
				string type = m_toolTypes[objecttype].ToString();
				return Assembly.GetExecutingAssembly().CreateInstance(type) as INSDrawObject;
			}
			return null;
		}

		Dictionary<string, INSDrawObject> m_drawObjectTypes = new Dictionary<string, INSDrawObject>(); 
		DrawObjectBase CreateObject(string objecttype)
		{
			if (m_drawObjectTypes.ContainsKey(objecttype))
			{
				return m_drawObjectTypes[objecttype].Clone() as DrawObjectBase;
			}
			return null;
		}


		Dictionary<string, INSEditTool> m_editTools = new Dictionary<string, INSEditTool>(); 
		public void AddEditTool(string key, INSEditTool tool)
		{
			m_editTools.Add(key, tool);
		}

		public bool IsDirty
		{
			get { return m_undoBuffer.Dirty; }
		}
		UndoRedoBuffer m_undoBuffer = new UndoRedoBuffer();
		
		UnitPoint m_centerPoint = UnitPoint.Empty;
		[XmlSerializable]
		public UnitPoint CenterPoint
		{
			get { return m_centerPoint; }
			set { m_centerPoint = value; }
		}

		float m_zoom = 1;
		GridLayer m_gridLayer = new GridLayer();
		BackgroundLayer m_backgroundLayer = new BackgroundLayer();
		List<INSCanvasLayer> m_layers = new List<INSCanvasLayer>();
		INSCanvasLayer m_activeLayer;
		Dictionary<INSDrawObject, bool> m_selection = new Dictionary<INSDrawObject, bool>();
		public DataModel()
		{
			m_toolTypes.Clear();
			m_toolTypes[Line.ObjectType] = typeof(Line);
			m_toolTypes[Circle.ObjectType] = typeof(Circle);
			m_toolTypes[Arc.ObjectType] = typeof(Arc);
			m_toolTypes[Arc3Point.ObjectType] = typeof(Arc3Point);
			DefaultLayer();
			m_centerPoint = new UnitPoint(0,0);
		}
		public void AddDrawTool(string key, INSDrawObject drawtool)
		{
			m_drawObjectTypes[key] = drawtool;
		}
		public void Save(string filename)
		{
			try
			{
				XmlTextWriter wr = new XmlTextWriter(filename, null);
				wr.Formatting = Formatting.Indented;
				wr.WriteStartElement("CanvasDataModel");
				m_backgroundLayer.GetObjectData(wr);
				m_gridLayer.GetObjectData(wr);
				foreach (INSCanvasLayer layer in m_layers)
				{
					if (layer is INSSerialize)
						((INSSerialize)layer).GetObjectData(wr);
				}
				XmlUtil.WriteProperties(this, wr);
				wr.WriteEndElement();
				wr.Close();
				m_undoBuffer.Dirty = false;
			}
			catch { }
		}
		public bool Load(string filename)
		{
			try
			{
				StreamReader sr = new StreamReader(filename);
				//XmlTextReader rd = new XmlTextReader(sr);
				XmlDocument doc = new XmlDocument();
				doc.Load(sr);
				sr.Dispose();
				XmlElement root = doc.DocumentElement;
				if (root.Name != "CanvasDataModel")
					return false;

				m_layers.Clear();
				m_undoBuffer.Clear();
				m_undoBuffer.Dirty = false;
				foreach (XmlElement childnode in root.ChildNodes)
				{
					if (childnode.Name == "backgroundlayer")
					{
						XmlUtil.ParseProperties(childnode, m_backgroundLayer);
						continue;
					}
					if (childnode.Name == "gridlayer")
					{
						XmlUtil.ParseProperties(childnode, m_gridLayer);
						continue;
					}
					if (childnode.Name == "layer")
					{
						DrawingLayer l = DrawingLayer.NewLayer(childnode as XmlElement);
						m_layers.Add(l);
					}
					if (childnode.Name == "property")
						XmlUtil.ParseProperty(childnode, this);
				}
				return true;
			}
			catch (Exception e)
			{
				DefaultLayer();
				Console.WriteLine("Load exception - {0}", e.Message);
			}
			return false;
		}
		void DefaultLayer()
		{
			m_layers.Clear();
			m_layers.Add(new DrawingLayer("layer0", "Hairline Layer", Color.White, 0.0f));
			m_layers.Add(new DrawingLayer("layer1", "0.005 Layer", Color.Red, 0.005f));
			m_layers.Add(new DrawingLayer("layer2", "0.025 Layer", Color.Green, 0.025f));
		}
		public INSDrawObject GetFirstSelected()
		{
			if (m_selection.Count > 0)
			{
				Dictionary<INSDrawObject, bool>.KeyCollection.Enumerator e = m_selection.Keys.GetEnumerator();
				e.MoveNext();
				return e.Current;
			}
			return null;
		}
		#region IModel Members
		[XmlSerializable]
		public float Zoom
		{
			get { return m_zoom; }
			set { m_zoom = value; }
		}
		public INSCanvasLayer BackgroundLayer
		{
			get { return m_backgroundLayer; }
		}
		public INSCanvasLayer GridLayer
		{
			get { return m_gridLayer; }
		}
		public INSCanvasLayer[] Layers
		{
			get { return m_layers.ToArray(); }
		}
		public INSCanvasLayer ActiveLayer
		{
			get
			{
				if (m_activeLayer == null)
					m_activeLayer = m_layers[0];
				return m_activeLayer;
			}
			set
			{
				m_activeLayer = value;
			}
		}
		public INSCanvasLayer GetLayer(string id)
		{
			foreach (INSCanvasLayer layer in m_layers)
			{
				if (layer.Id == id)
					return layer;
			}
			return null;
		}
		public INSDrawObject CreateObject(string type, UnitPoint point, INSSnapPoint snappoint)
		{
			DrawingLayer layer = ActiveLayer as DrawingLayer;
			if (layer.Enabled == false)
				return null;
			DrawObjectBase newobj = CreateObject(type);
			if (newobj != null)
			{
				newobj.Layer = layer;
				newobj.InitializeFromModel(point, layer, snappoint);
			}
			return newobj as INSDrawObject;
		}
		public void AddObject(INSCanvasLayer layer, INSDrawObject drawobject)
		{
			if (drawobject is IObjectEditInstance)
				drawobject = ((IObjectEditInstance)drawobject).GetDrawObject();
			if (m_undoBuffer.CanCapture)
				m_undoBuffer.AddCommand(new EditCommandAdd(layer, drawobject));
			((DrawingLayer)layer).AddObject(drawobject);
		}
		public void DeleteObjects(IEnumerable<INSDrawObject> objects)
		{
			EditCommandRemove undocommand = null;
			if (m_undoBuffer.CanCapture)
				undocommand = new EditCommandRemove();
			foreach (INSCanvasLayer layer in m_layers)
			{
				List<INSDrawObject> removedobjects = ((DrawingLayer)layer).DeleteObjects(objects);
				if (removedobjects != null && undocommand != null)
					undocommand.AddLayerObjects(layer, removedobjects);
			}
			if (undocommand != null)
				m_undoBuffer.AddCommand(undocommand);
		}
		public void MoveObjects(UnitPoint offset, IEnumerable<INSDrawObject> objects)
		{
			if (m_undoBuffer.CanCapture)
				m_undoBuffer.AddCommand(new EditCommandMove(offset, objects));
			foreach (INSDrawObject obj in objects)
				obj.Move(offset);
		}
		public void CopyObjects(UnitPoint offset, IEnumerable<INSDrawObject> objects)
		{
			ClearSelectedObjects();
			List<INSDrawObject> newobjects = new List<INSDrawObject>();
			foreach (INSDrawObject obj in objects)
			{
				INSDrawObject newobj = obj.Clone();
				newobjects.Add(newobj);
				newobj.Move(offset);
				((DrawingLayer)ActiveLayer).AddObject(newobj);
				AddSelectedObject(newobj);
			}
			if (m_undoBuffer.CanCapture)
				m_undoBuffer.AddCommand(new EditCommandAdd(ActiveLayer, newobjects));
		}
		public void AfterEditObjects(INSEditTool edittool)
		{
			edittool.Finished();
			if (m_undoBuffer.CanCapture)
				m_undoBuffer.AddCommand(new EditCommandEditTool(edittool));
		}
		public INSEditTool GetEditTool(string edittoolid)
		{
			if (m_editTools.ContainsKey(edittoolid))
				return m_editTools[edittoolid].Clone();
			return null;
		}
		public void MoveNodes(UnitPoint position, IEnumerable<INSNodePoint> nodes)
		{
			if (m_undoBuffer.CanCapture)
				m_undoBuffer.AddCommand(new EditCommandNodeMove(nodes));
			foreach (INSNodePoint node in nodes)
			{
				node.SetPosition(position);
				node.Finish();
			}
		}
		public List<INSDrawObject> GetHitObjects(INSCanvas canvas, RectangleF selection, bool anyPoint)
		{
			List<INSDrawObject> selected = new List<INSDrawObject>();
			foreach (INSCanvasLayer layer in m_layers)
			{
				if (layer.Visible == false)
					continue;
				foreach (INSDrawObject drawobject in layer.Objects)
				{
					if (drawobject.ObjectInRectangle(canvas, selection, anyPoint))
						selected.Add(drawobject);
				}
			}
			return selected;
		}
		public List<INSDrawObject> GetHitObjects(INSCanvas canvas, UnitPoint point)
		{
			List<INSDrawObject> selected = new List<INSDrawObject>();
			foreach (INSCanvasLayer layer in m_layers)
			{
				if (layer.Visible == false)
					continue;
				foreach (INSDrawObject drawobject in layer.Objects)
				{
					if (drawobject.PointInObject(canvas, point))
						selected.Add(drawobject);
				}
			}
			return selected;
		}
		public bool IsSelected(INSDrawObject drawobject)
		{
			return m_selection.ContainsKey(drawobject);
		}
		public void AddSelectedObject(INSDrawObject drawobject)
		{
			DrawObjectBase obj = drawobject as DrawObjectBase;
			RemoveSelectedObject(drawobject);
			m_selection[drawobject] = true;
			obj.Selected = true;
		}
		public void RemoveSelectedObject(INSDrawObject drawobject)
		{
			if (m_selection.ContainsKey(drawobject))
			{
				DrawObjectBase obj = drawobject as DrawObjectBase;
				obj.Selected = false;
				m_selection.Remove(drawobject);
			}
		}
		public IEnumerable<INSDrawObject> SelectedObjects
		{
			get
			{
				return m_selection.Keys;
			}
		}
		public int SelectedCount
		{
			get { return m_selection.Count; }
		}
		public void ClearSelectedObjects()
		{
			IEnumerable<INSDrawObject> x = SelectedObjects;
			foreach (INSDrawObject drawobject in x)
			{
				DrawObjectBase obj = drawobject as DrawObjectBase;
				obj.Selected = false;
			}
			m_selection.Clear();
		}
		public INSSnapPoint SnapPoint(INSCanvas canvas, UnitPoint point, Type[] runningsnaptypes, Type usersnaptype)
		{
			List<INSDrawObject> objects = GetHitObjects(canvas, point);
			if (objects.Count == 0)
				return null;

			foreach (INSDrawObject obj in objects)
			{
				INSSnapPoint snap = obj.SnapPoint(canvas, point, objects, runningsnaptypes, usersnaptype);
				if (snap != null)
					return snap;
			}
			return null;
		}

		public bool CanUndo()
		{
			return m_undoBuffer.CanUndo;
		}
		public bool DoUndo()
		{
			return m_undoBuffer.DoUndo(this);
		}
		public bool CanRedo()
		{
			return m_undoBuffer.CanRedo;

		}
		public bool DoRedo()
		{
			return m_undoBuffer.DoRedo(this);
		}
		#endregion
	}
}

