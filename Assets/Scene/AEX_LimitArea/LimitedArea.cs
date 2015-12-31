using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dest.Math;
using Dest.Math.Ex;

using UnityEditor;
using Newtonsoft.Json;

namespace AEX
{

	public class LimitedArea : MonoBehaviour
	{
		public float UVScale;
		public Vector2 UVPosition;
		public float UIRotation;

		public Polygon2Ex Polygon;

		public void Build (Polygon2Ex polygon)
		{
			Polygon = polygon;
			BuildFillingMesh (polygon);
			BuildBorderMesh (polygon);
		}

		public void BuildFillingMesh (Polygon2Ex polygon)
		{
			if (polygon != null) {
				Polygon = polygon;
			}

			List<Vector2> vertice_list = new List<Vector2> ();
			List<int> triangle_points = new List<int> ();

			Polygon2Ex[] pols = Polygon.ToSimplePolygons ();
			foreach (Polygon2Ex pol in pols) {
				int offset_idx = vertice_list.Count;
				vertice_list.AddRange (pol.Vertices);

				int[] vid_list = pol.ToTrianglesVertice ();
				triangle_points.AddRange (vid_list.Select (_ => _ + offset_idx).ToArray ());
			}

			//Build the uv array
			var scale = UVScale != 0 ? (1f / UVScale) : 0;
			var matrix = Matrix4x4.TRS (-(Vector3)UVPosition, Quaternion.Euler (0, 0, UIRotation), new Vector3 (scale, scale, 1));
			var uv = new Vector2[vertice_list.Count];
			for (int i = 0; i < uv.Length; i++) {
				var p = matrix.MultiplyPoint (vertice_list [i]);
				uv [i] = new Vector2 (p.x, p.y);
			}

			Transform fillingTran = transform.FindChild ("Filling");
				
			MeshFilter meshFilter = fillingTran.GetComponent<MeshFilter> ();
			if (meshFilter == null) {
				meshFilter = fillingTran.gameObject.AddComponent<MeshFilter> ();
			}
			Mesh mesh = meshFilter.sharedMesh;

			if (mesh == null) {
				mesh = new Mesh ();
				mesh.name = "filling mesh";
				meshFilter.mesh = mesh;
			}

			//Update the mesh
			mesh.Clear ();
			mesh.vertices = vertice_list.Select (v2 => (Vector3)v2).ToArray ();
			mesh.uv = uv;
			mesh.triangles = triangle_points.ToArray ();
			mesh.RecalculateNormals ();
			mesh.Optimize ();
		}

		public void BuildBorderMesh (Polygon2Ex polygon)
		{
			Line2Ex line = new Line2Ex (Line2.CreateFromTwoPoints (polygon.Vertices [0], polygon.Vertices [1]));
			Line2Ex p_line = line.ToParallel (.1f);
			line.ShowGizmo ();
			p_line.ShowGizmo ();

			polygon.ToBorderExtendingPolygon ();
		}
	}

}