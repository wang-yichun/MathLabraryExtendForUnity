using UnityEngine;
using System.Collections;
using Dest.Math;
using Dest.Math.Ex;
using AEX;

namespace AEX
{

	[ExecuteInEditMode]
	public class Test_LimitedArea : MonoBehaviour
	{

		public LimitedArea LimitedArea;

		// Use this for initialization
		void Start ()
		{
			Polygon2Ex pol = new Polygon2Ex (GetComponent<PolygonCollider2D> ().points);
			LimitedArea.Build (pol);
		}

		private void OnDrawGizmos ()
		{
			Polygon2Ex pol = new Polygon2Ex (GetComponent<PolygonCollider2D> ().points);
			LimitedArea.Build (pol);
		}
	}
}