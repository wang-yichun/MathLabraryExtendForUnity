using UnityEngine;
using System.Collections;

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

	private float AxisDistance;
	private Vector3 CenterPosition;
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
		Vector3 p0 = Axises [0].transform.position;
		Vector3 p1 = Axises [1].transform.position;
		CenterPosition = Vector3.Lerp (p0, p1, .5f);
		AxisDistance = Vector3.Distance (p0, p1);
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
		Vector3 pc = CenterPosition;
		Vector3 p0 = Axises [0].transform.position;
		Vector3 p1 = Axises [1].transform.position;
		Vector3 v01 = p1 - p0;
		Vector3 v01n = v01.normalized;
		Vector3 vnn = Quaternion.Euler (0f, 0f, 90f) * v01n;
		Vector3 pc_a = pc + vnn;
		Vector3 pc_b = pc - vnn;
		Vector3 p0_a = p0 + vnn;
		Vector3 p0_b = p0 - vnn;
		Vector3 p1_a = p1 + vnn;
		Vector3 p1_b = p1 - vnn;

		Handles.color = Color.cyan;
		Handles.DrawLines (new Vector3 []{ p0, p1, pc_a, pc_b, p0_a, p0_b, p1_a, p1_b });
		Handles.CircleCap (0, p0, Quaternion.identity, 1f * Radius);
		Handles.CircleCap (0, p1, Quaternion.identity, 1f * Radius);

		#endif
	}

	void OnDrawGizmos ()
	{
		Standardization ();
		SetupChildren ();
		RefreshParam ();

		DrawGizmos ();
	}


}
