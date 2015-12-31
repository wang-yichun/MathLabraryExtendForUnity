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
	public class Line2Ex
	{
		public Line2 baseLine;

		//
		// Constructors
		//
		public Line2Ex (Line2 line)
		{
			baseLine = line;
			InitGizmoParam ();
		}

		public Line2Ex (ref Vector2 center, ref Vector2 direction) : this (new Line2 (ref center, ref direction))
		{
		}

		public Line2Ex (Vector2 center, Vector2 direction) : this (new Line2 (center, direction))
		{
		}

		public Line2Ex ToParallel (float distance)
		{
			Vector2 p_normalDirection = Vector3.Cross ((Vector3)baseLine.Direction.normalized, Vector3.back);
			Vector2 p_center = baseLine.Center + p_normalDirection * distance;
			Vector2 p_direction = baseLine.Direction;
			return new Line2Ex (p_center, p_direction);
		}

		#region Debug

		public Color GizmoColor;
		public Vector3 GizmoOffset;
		public float GizmoLength;
		
		#if EX_GIZMO_ON
		public void InitGizmoParam ()
		{
			GizmoColor = Color.gray;
			GizmoOffset = Vector3.zero;
			GizmoLength = 100f;
		}
		#endif
		
		public void ShowGizmo ()
		{
			#if EX_GIZMO_ON
		
			Handles.color = GizmoColor;
		
			Handles.DrawLine ((Vector3)(this.baseLine.Center + baseLine.Direction * GizmoLength),
				(Vector3)(this.baseLine.Center - baseLine.Direction * GizmoLength));

			#endif
		}

		#endregion
	}
}