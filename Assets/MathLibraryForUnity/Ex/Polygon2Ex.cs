using UnityEngine;

#if EX_GIZMO_ON
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Dest.Math.Ex
{
	public class Polygon2Ex : Polygon2
	{
		#region 构造函数

		public Polygon2Ex (int vertexCount) : base (vertexCount)
		{
			#if EX_GIZMO_ON
			InitGizmoParam ();
			#endif
		}

		public Polygon2Ex (Vector2[] vertices) : base (vertices)
		{
			#if EX_GIZMO_ON
			InitGizmoParam ();
			#endif
		}

		#endregion

		public Polygon2Ex[] ToSimplePolygons ()
		{
			List<List<Vector2>> vertices_list = ToolsPolygon.GetChildSimplifyPolygon (this.Vertices.ToList ());
			return vertices_list.Where (_ => _.Count >= 3).Select (_ => new Polygon2Ex (_.ToArray ())).ToArray ();
		}

		public Triangle2Ex[] ToTriangles ()
		{
			Polygon2Ex[] simple_pols = ToSimplePolygons ();
			List<Triangle2Ex> tri_list = new List<Triangle2Ex> ();

			foreach (Polygon2Ex pol in simple_pols) {
				int[] points = Triangulates.Points (pol.Vertices.ToList ());

				for (int idx = 0; idx < points.Length; idx += 3) {
					int idx0 = idx;
					int idx1 = idx + 1;
					int idx2 = idx + 2;

					tri_list.Add (new Triangle2Ex (pol.Vertices [points [idx0]], pol.Vertices [points [idx1]], pol.Vertices [points [idx2]]));
				}
			}

			return tri_list.ToArray ();
		}

		public int[] ToTrianglesVertice ()
		{
			return Triangulates.Points (this.Vertices.ToList ());
		}

		public Polygon2Ex ToBorderExtendingPolygon (float distance)
		{
			for (int i = 0; i < this.VertexCount; i++) {
				Vector2 prev_v = (i - 1 + this.VertexCount) % this.VertexCount;
				Vector2 this_v = i;
				Vector2 next_v = i % this.VertexCount;

//				Line2Ex
			}
		}

		#region Debug

		public Color GizmoColor;
		public Vector3 GizmoOffset;


		#if EX_GIZMO_ON
		public void InitGizmoParam ()
		{
			GizmoColor = Color.gray;
			GizmoOffset = Vector3.zero;
		}
		#endif

		public void ShowGizmo ()
		{
			#if EX_GIZMO_ON

			Handles.color = GizmoColor;

			Segment2[] s2a = this.ToSegmentArray ();

			int sg_idx = 0;
			foreach (Segment2 s in s2a) {
				Handles.ArrowCap (0, s.P0, Quaternion.FromToRotation (-Vector3.back, s.P1 - s.P0), 1f);
				Handles.DrawLine ((Vector3)s.P0, (Vector3)s.P1);

				GUIStyle gs = new GUIStyle ();
				gs.normal.textColor = GizmoColor;
				gs.hover.textColor = Color.white;

				Handles.Label (s.P0 + (Vector2)GizmoOffset, sg_idx.ToString (), gs);
				sg_idx++;
			}
			#endif
		}

		#endregion
	}
}