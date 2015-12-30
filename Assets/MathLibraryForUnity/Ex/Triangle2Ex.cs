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
	public class Triangle2Ex
	{
		public Triangle2 baseTriangle;

		#region 构造函数

		public Triangle2Ex (Vector2 v0, Vector2 v1, Vector2 v2)
		{
			baseTriangle = new Triangle2 (v0, v1, v2);
			
			#if EX_GIZMO_ON
			InitGizmoParam ();
			#endif
		}

		public Triangle2Ex (ref Vector2 v0, ref Vector2 v1, ref Vector2 v2)
		{
			baseTriangle = new Triangle2 (ref v0, ref v1, ref v2);

			#if EX_GIZMO_ON
			InitGizmoParam ();
			#endif
		}

		public Triangle2Ex (Vector2[] vertices) : this (vertices [0], vertices [1], vertices [2])
		{
		}

		#endregion

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

			Segment2[] s2a = new Segment2[] {
				new Segment2 (baseTriangle.V0, baseTriangle.V1),
				new Segment2 (baseTriangle.V1, baseTriangle.V2),
				new Segment2 (baseTriangle.V2, baseTriangle.V0)
			};

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