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
	public class Segment2Ex
	{
		public Segment2 baseSegment;

		//
		// Constructors
		//
		public Segment2Ex (Segment2 segment)
		{
			baseSegment = segment;

			#if EX_GIZMO_ON
			InitGizmoParam ();
			#endif
		}

		public Segment2Ex (Vector2 center, Vector2 direction, float extent) : this (new Segment2 (center, direction, extent))
		{
		}

		public Segment2Ex (ref Vector2 center, ref Vector2 direction, float extent) : this (new Segment2 (ref center, ref direction, extent))
		{
		}

		public Segment2Ex (Vector2 p0, Vector2 p1) : this (new Segment2 (p0, p1))
		{
		}

		public Segment2Ex (ref Vector2 p0, ref Vector2 p1) : this (new Segment2 (ref p0, ref p1))
		{
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

			Handles.DrawLine (baseSegment.P0, baseSegment.P1);

//			Handles.

			#endif
		}

		#endregion
	}

}