using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Dest.Math;
using Dest.Math.Ex;

namespace Dest.Math.Ex.Tests
{
	[ExecuteInEditMode]
	public class Test_Polygon2Ex : MonoBehaviour
	{
		public Transform[] Points;

		private void OnDrawGizmos ()
		{

			UnityEngine.Random.seed = this.GetInstanceID ();

			Polygon2Ex pol = new Polygon2Ex (GetComponent<PolygonCollider2D> ().points);

//			pol.GizmoColor = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
//			pol.ShowGizmo ();

			Triangle2Ex[] tris = pol.ToTriangles ();
			foreach (Triangle2Ex tri in tris) {
				tri.GizmoColor = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				tri.GizmoOffset = UnityEngine.Random.insideUnitCircle * .1f;
				tri.ShowGizmo ();
			}


//			Polygon2Ex[] pols = pol.ToSimplePolygons ();
//
//			foreach (Polygon2Ex s_pol in pols) {
//				s_pol.GizmoColor = new Color (UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
//				s_pol.GizmoOffset = UnityEngine.Random.insideUnitCircle * .1f;
//				s_pol.ShowGizmo ();
//			}
		}
	}
}
