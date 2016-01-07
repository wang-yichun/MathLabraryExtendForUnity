using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GizmoBelt : MonoBehaviour
{
	private const string AXIS_GROUP = "AxisGroup";
	private const string CHAIN_GROUP = "ChainGroup";

	public Transform AxisGroup;
	public Transform ChainGroup;
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
	private float step_length = .2f;
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
		ChainGroup = transform.FindChild (CHAIN_GROUP);

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
			foreach (Vector3 p in position_list) {
				Handles.CircleCap (0, p, Quaternion.identity, .01f);
			}
		}
		#endif
	}

	void OnDrawGizmos ()
	{
		Standardization ();
		SetupChildren ();
		RefreshParam ();


		RefreshChain ();
		DrawGizmos ();
	}

	List<Vector3> position_list;

	public void RefreshChain ()
	{
		position_list = new List<Vector3> ();
		for (float offset = start_offset2; offset < perimeter + start_offset2; offset += step_offset_length) {
			
			if (offset < AxisDistance) {
				Vector3 direction = p1 - p0;
				Vector3 dn = direction.normalized;
				float distance = offset;
				Vector3 p = p0_a + dn * distance;
				position_list.Add (p);

			} else if (offset < AxisDistance + HalfCircumference) {
				Vector3 va = p1_a - p1;
				float distance = offset - AxisDistance;
				float delta = 180f * distance / HalfCircumference;
				Vector3 v_delta = Quaternion.Euler (0f, 0f, -delta) * va;
				Vector3 p = p1 + v_delta;
				position_list.Add (p);

			} else if (offset < AxisDistance * 2f + HalfCircumference) {
				Vector3 direction = p0 - p1;
				Vector3 dn = direction.normalized;
				float distance = offset - AxisDistance - HalfCircumference;
				Vector3 p = p1_b + dn * distance;
				position_list.Add (p);

			} else {
				Vector3 vb = p0_b - p0;
				float distance = offset - AxisDistance * 2f - HalfCircumference;
				float delta = 180f * distance / HalfCircumference;
				Vector3 v_delta = Quaternion.Euler (0f, 0f, -delta) * vb;
				Vector3 p = p0 + v_delta;
				position_list.Add (p);

			}
		}
	}
}
