using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace NSCAD
{
	class LineShrinkExtendEditTool : INSEditTool
	{
		INSEditToolOwner _owner;
		public LineShrinkExtendEditTool(INSEditToolOwner owner)
		{
			_owner = owner;
			SetHint("Select line to extend");
		}
		void SetHint(string text)
		{
			if (_owner != null)
				if (text.Length > 0)
					_owner.SetHint("Extend Lines: " + text);
				else
					_owner.SetHint("");
		}
		public INSEditTool Clone()
		{
			LineShrinkExtendEditTool t = new LineShrinkExtendEditTool(_owner);
			// nothing that needs to be cloned
			return t;
		}
		Dictionary<Line, LinePoints> m_originalLines = new Dictionary<Line, LinePoints>();
		Dictionary<Line, LinePoints> m_modifiedLines = new Dictionary<Line, LinePoints>();

		public void OnMouseMove(INSCanvas canvas, UnitPoint point)
		{
		}

		public bool SupportSelection
		{
			get { return true; }
		}

		void ClearAll()
		{
			foreach (LinePoints p in m_originalLines.Values)
				p.Line.Highlighted = false;
			m_originalLines.Clear();
		}
		void AddLine(UnitPoint point, Line line)
		{
			if (m_originalLines.ContainsKey(line) == false)
			{
				line.Highlighted = true;
				LinePoints lp = new LinePoints();
				lp.SetLine(line);
				lp.MousePoint = point;
				m_originalLines.Add(line, lp);
			}
		}
		void RemoveLine(Line line)
		{
			if (m_originalLines.ContainsKey(line))
			{
				m_originalLines[line].Line.Highlighted = false;
				m_originalLines.Remove(line);
			}
		}
		public void SetHitObjects(UnitPoint point, List<INSDrawObject> list)
		{
			// called when obj are selected from selection rectangle
			// if list is empty, do nothing
			// if no shift or ctrl, then replace selection with items from list
			// if shift then append
			// if ctrl then toggle
			if (list == null)
				return;
			List<Line> lines = GetLines(list);
			if (lines.Count == 0)
				return;

			bool shift = Control.ModifierKeys == Keys.Shift;
			bool ctrl = Control.ModifierKeys == Keys.Control;

			if (shift == false && ctrl == false)
				ClearAll();

			if (ctrl == false) // append all lines, either no-key or shift
			{
				foreach (Line line in lines)
					AddLine(point, line);
			}
			if (ctrl)
			{
				foreach (Line line in lines)
				{
					if (m_originalLines.ContainsKey(line))
						RemoveLine(line);
					else
						AddLine(point, line);
				}
			}
			SetSelectHint();
		}
		void SetSelectHint()
		{
			if (m_originalLines.Count == 0)
				SetHint("Select line to extend");
			else
				SetHint("Select Line to extend line(s) to, or [Ctrl+click] to extend more lines");
		}
		List<Line> GetLines(List<INSDrawObject> objs)
		{
			List<Line> lines = new List<Line>();
			foreach (INSDrawObject obj in objs)
			{
				if (obj is Line)
					lines.Add((Line)obj);
			}
			return lines;
		}
		public eDrawObjectMouseDown OnMouseDown(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint)
		{
			List<INSDrawObject> drawitems = canvas.DataModel.GetHitObjects(canvas, point);
			List<Line> lines = GetLines(drawitems);

			// add to source lines
			if (m_originalLines.Count == 0 || Control.ModifierKeys == Keys.Shift)
			{
				foreach (Line line in lines)
					AddLine(point, line);
				SetSelectHint();
				return eDrawObjectMouseDown.Continue;
			}
			if (m_originalLines.Count == 0 || Control.ModifierKeys == Keys.Control)
			{
				foreach (Line line in lines)
				{
					if (m_originalLines.ContainsKey(line))
						RemoveLine(line);
					else
						AddLine(point, line);
				}
				SetSelectHint();
				return eDrawObjectMouseDown.Continue;
			}

			if (drawitems.Count == 0)
				return eDrawObjectMouseDown.Continue;

			// all lines have been added, now find edge to where to extend
			if (drawitems[0] is Line)
			{
				Line edge = (Line)drawitems[0];
				bool modified = false;
				foreach (LinePoints originalLp in m_originalLines.Values)
				{
					UnitPoint intersectpoint = HitUtil.LinesIntersectPoint(edge.P1, edge.P2, originalLp.Line.P1, originalLp.Line.P2);
					// lines intersect so shrink line
					if (intersectpoint != UnitPoint.Empty)
					{
						LinePoints lp = new LinePoints();
						lp.SetLine(originalLp.Line);
						lp.MousePoint = originalLp.MousePoint;
						m_modifiedLines.Add(lp.Line, lp);
						lp.SetNewPoints(lp.Line, lp.MousePoint, intersectpoint);
						modified = true;
						continue;
					}
					// lines do not intersect, find apparent intersect point on existing edge line
					if (intersectpoint == UnitPoint.Empty)
					{
						UnitPoint apprarentISPoint = HitUtil.FindApparentIntersectPoint(
							edge.P1,
							edge.P2,
							originalLp.Line.P1,
							originalLp.Line.P2,
							false,
							true);
						if (apprarentISPoint == UnitPoint.Empty)
							continue;

						modified = true;
						originalLp.Line.ExtendLineToPoint(apprarentISPoint);

						LinePoints lp = new LinePoints();
						lp.SetLine(originalLp.Line);
						lp.MousePoint = point;
						m_modifiedLines.Add(lp.Line, lp);
					}
				}
				if (modified)
					canvas.DataModel.AfterEditObjects(this);
				return eDrawObjectMouseDown.Done;
			}
			if (drawitems[0] is Arc)
			{
				Arc edge = (Arc)drawitems[0];
				foreach (LinePoints originalLp in m_originalLines.Values)
				{
				}
				bool modified = false;
			}
			return eDrawObjectMouseDown.Done;
		}
		public void OnMouseUp(INSCanvas canvas, UnitPoint point, INSSnapPoint snappoint)
		{
		}
		public void OnKeyDown(INSCanvas canvas, KeyEventArgs e)
		{
		}
		public void Finished()
		{
			SetHint("");
			foreach (LinePoints originalLp in m_originalLines.Values)
				originalLp.Line.Highlighted = false;
		}
		public void Undo()
		{
			foreach (LinePoints lp in m_originalLines.Values)
				lp.ResetLine();
		}
		public void Redo()
		{
			foreach (LinePoints lp in m_modifiedLines.Values)
				lp.ResetLine();
		}
	}
}