using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GizmoBelt : MonoBehaviour
{
	private const string AXIS_GROUP = "AxisGroup";
	private const string CHAIN = "Chain";

	public Transform AxisGroup;
	public GameObject Chain;
	public Transform[] Axises;

	public bool DoStandardization;
	public float Radius;

	private Vector3 pc;
	private Vector3 p0;
	private Vector3 p1;
	private Vector3 v01;
	private Vector3 v01n;
	private Vector3 vnn;
	private Vector3 pc_a;
	private Vector3 pc_b;
	private Vector3 p0_a;
	private Vector3 p0_b;
	private Vector3 p1_a;
	private Vector3 p1_b;

	private float perimeter;
	private float step_length = .3f;
	private float step_offset;
	private float step_offset_length;
	private int step_mult;

	private float start_offset = 0f;
	private float start_offset2;

	private float AxisDistance;
	private Vector3 CenterPosition;
	private float HalfCircumference;
	private float AxisesScaleRate = 2f;

	private void SetupChildren ()
	{
		AxisGroup = transform.FindChild (AXIS_GROUP);
		Chain = transform.FindChild (CHAIN).gameObject;

		Axises = new Transform[AxisGroup.childCount];
		for (int i = 0; i < AxisGroup.childCount; i++) {
			Axises [i] = AxisGroup.GetChild (i);
			Axises [i].localScale = Vector3.one * AxisesScaleRate * Radius;
		}
	}

	private void RefreshParam ()
	{
		p0 = Axises [0].transform.position;
		p1 = Axises [1].transform.position;
		CenterPosition = Vector3.Lerp (p0, p1, .5f);
		pc = CenterPosition;

		v01 = p1 - p0;
		v01n = v01.normalized;
		vnn = Quaternion.Euler (0f, 0f, 90f) * v01n;
		pc_a = pc + vnn;
		pc_b = pc - vnn;
		p0_a = p0 + vnn * Radius;
		p0_b = p0 - vnn * Radius;
		p1_a = p1 + vnn * Radius;
		p1_b = p1 - vnn * Radius;

		HalfCircumference = Mathf.PI * Radius;

		AxisDistance = Vector3.Distance (p0, p1);

		perimeter = 2f * AxisDistance + 2f * HalfCircumference;

		step_mult = (int)(perimeter / step_length);
		step_offset = perimeter - (float)step_mult * step_length;
		step_offset_length = step_length + step_offset / (float)step_mult;

		start_offset2 = start_offset % step_length;
	}

	private void Standardization ()
	{
		if (DoStandardization) {
			DoStandardization = false;
			AxisGroup.DetachChildren ();
			transform.position = CenterPosition;
			foreach (Transform axis in Axises) {
				axis.SetParent (AxisGroup);
			}
		}
	}

	private void DrawGizmos ()
	{
		#if UNITY_EDITOR

		Handles.color = Color.cyan;
		Handles.DrawLines (new Vector3 []{ p0, p1, pc_a, pc_b, p0_a, p0_b, p1_a, p1_b });
		Handles.CircleCap (0, p0, Quaternion.identity, 1f * Radius);
		Handles.CircleCap (0, p1, Quaternion.identity, 1f * Radius);

		Handles.color = Color.yellow;
		if (position_list != null) {
			for (int i = 0; i < position_list.Count; i++) {
				Vector3 p = position_list [i];
				Vector3 dir = direction_list [i];
				Handles.CircleCap (0, p, Quaternion.identity, .01f);
				Handles.ArrowCap (0, p, Quaternion.FromToRotation (-Vector3.back, dir), .1f);
			}
		}
		#endif
	}

	void OnDrawGizmos ()
	{
		Standardization ();
		SetupChildren ();
		RefreshParam ();


		RefreshChainData ();
		RenderChain ();
		DrawGizmos ();
	}

	List<Vector3> position_list;
	List<Vector3> direction_list;

	public void RefreshChainData ()
	{
		position_list = new List<Vector3> ();
		direction_list = new List<Vector3> ();
		for (float offset = start_offset2; offset < perimeter + start_offset2; offset += step_offset_length) {
			
			if (offset < AxisDistance) {
				Vector3 direction = p1 - p0;
				Vector3 dn = direction.normalized;
				float distance = offset;
				Vector3 p = p0_a + dn * distance;
				Vector3 va = p0_a - p0;
				position_list.Add (p);
				direction_list.Add (va.normalized);

			} else if (offset < AxisDistance + HalfCircumference) {
				Vector3 va = p1_a - p1;
				float distance = offset - AxisDistance;
				float delta = 180f * distance / HalfCircumference;
				Vector3 v_delta = Quaternion.Euler (0f, 0f, -delta) * va;
				Vector3 p = p1 + v_delta;
				position_list.Add (p);
				direction_list.Add (v_delta.normalized);

			} else if (offset < AxisDistance * 2f + HalfCircumference) {
				Vector3 direction = p0 - p1;
				Vector3 dn = direction.normalized;
				float distance = offset - AxisDistance - HalfCircumference;
				Vector3 p = p1_b + dn * distance;
				Vector3 vb = p1_b - p1;
				position_list.Add (p);
				direction_list.Add (vb.normalized);

			} else {
				Vector3 vb = p0_b - p0;
				float distance = offset - AxisDistance * 2f - HalfCircumference;
				float delta = 180f * distance / HalfCircumference;
				Vector3 v_delta = Quaternion.Euler (0f, 0f, -delta) * vb;
				Vector3 p = p0 + v_delta;
				position_list.Add (p);
				direction_list.Add (v_delta.normalized);

			}
		}
	}

	public float chain_unit_width = .32f;
	public float chain_unit_height = .16f;

	public void RenderChain ()
	{
		MeshFilter mf = Chain.GetComponent<MeshFilter> ();
		Mesh mesh = mf.sharedMesh;


		Vector3[] vertices = new Vector3[ position_list.Count * 4 ];
		Vector2[] uv = new Vector2[ position_list.Count * 4 ];
		int[] triangles = new int[ position_list.Count * 6 ];

		for (int i = 0; i < position_list.Count; i++) {
			Vector3 position = position_list [i];
			Vector3 direction = direction_list [i];

			Quaternion qua = Quaternion.FromToRotation (Vector3.up, direction);

			Vector2 v1 = qua * new Vector2 (chain_unit_width / 2f, chain_unit_height / 2f);
			Vector2 v2 = qua * new Vector2 (-chain_unit_width / 2f, chain_unit_height / 2f);
			Vector2 v3 = qua * new Vector2 (-chain_unit_width / 2f, -chain_unit_height / 2f);
			Vector2 v4 = qua * new Vector2 (chain_unit_width / 2f, -chain_unit_height / 2f);

			Vector2 p1 = (Vector2)position + v1;
			Vector2 p2 = (Vector2)position + v2;
			Vector2 p3 = (Vector2)position + v3;
			Vector2 p4 = (Vector2)position + v4;

			int pidx1 = i * 4;
			int pidx2 = pidx1 + 1;
			int pidx3 = pidx1 + 2;
			int pidx4 = pidx1 + 3;

			vertices [pidx1] = p1;
			vertices [pidx2] = p2;
			vertices [pidx3] = p3;
			vertices [pidx4] = p4;

			uv [pidx1] = new Vector2 (1f, 1f);
			uv [pidx2] = new Vector2 (0f, 1f);
			uv [pidx3] = new Vector2 (0f, 0f);
			uv [pidx4] = new Vector2 (1f, 0f);

			triangles [i * 6] = pidx1;
			triangles [i * 6 + 1] = pidx4;
			triangles [i * 6 + 2] = pidx2;
			triangles [i * 6 + 3] = pidx3;
			triangles [i * 6 + 4] = pidx2;
			triangles [i * 6 + 5] = pidx4;
		}

		mesh.Clear ();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;

	}
}
