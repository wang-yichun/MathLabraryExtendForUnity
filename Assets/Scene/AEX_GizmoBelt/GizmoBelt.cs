using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using pogorock;

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 0164

namespace pogorock
{
	public class GizmoBelt : MonoBehaviour, ICustomSerializable
	{
		private const string AXIS_GROUP = "AxisGroup";
		private const string CHAIN = "Chain";
		private const string CHAIN_PHYSICS = "ChainPhysics";

		public bool DoDrawGizmos;
		public bool DoStandardization;
		public bool DoTrySerialize;
		public string DoTryApply = "";

		public Transform AxisGroup;
		public GameObject Chain;
		public GameObject ChainPhysics;
		public Transform[] Axises;
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

		private float start_offset = -99.5f;
		private float start_offset2;

		private float AxisDistance;
		private Vector3 CenterPosition;
		private float HalfCircumference;
		private float AxisesScaleRate = 2f;

		private void SetupChildren ()
		{
			AxisGroup = transform.FindChild (AXIS_GROUP);
			Chain = transform.FindChild (CHAIN).gameObject;

			Transform ChainPhysicsTran = transform.FindChild (CHAIN_PHYSICS);
			if (ChainPhysicsTran == null) {
				ChainPhysics = new GameObject ("ChainPhysics");
				ChainPhysics.transform.SetParent (this.transform);
				ChainPhysics.AddComponent<BoxCollider2D> ();
				ChainPhysics.AddComponent<Rigidbody2D> ();
			}

			Axises = new Transform[AxisGroup.childCount];
			for (int i = 0; i < AxisGroup.childCount; i++) {
				Axises [i] = AxisGroup.GetChild (i);
				Axises [i].localScale = Vector3.one * AxisesScaleRate * Radius;

				Axises [i].gameObject.EnsureComponent<CircleCollider2D> ();
			}
		}

		private void RefreshParam ()
		{
			p0 = Axises [0].transform.localPosition;
			p1 = Axises [1].transform.localPosition;
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
		}

		private void Standardization ()
		{
			if (DoStandardization) {
				DoStandardization = false;

				AxisGroup.DetachChildren ();

				transform.position = transform.localToWorldMatrix.MultiplyPoint (CenterPosition);

				foreach (Transform axis in Axises) {
					axis.SetParent (AxisGroup);
				}

				Standardization_Physics ();
			}
		}

		private void Standardization_Physics ()
		{
			Axises:
			{
				for (int i = 0; i < 2; i++) {
					Transform axis = Axises [i];
					CircleCollider2D cc2d = axis.gameObject.EnsureComponent<CircleCollider2D> ();
					cc2d.radius = .5f + chain_unit_height / Radius / 4f;
					cc2d.usedByEffector = true;
					Rigidbody2D r2d = axis.gameObject.EnsureComponent<Rigidbody2D> ();
					r2d.isKinematic = true;
					SurfaceEffector2D se2d = axis.gameObject.EnsureComponent<SurfaceEffector2D> ();
					se2d.speed = Speed;
				}
			}

			ChainPhysics:
			{
				BoxCollider2D bc2d = ChainPhysics.EnsureComponent<BoxCollider2D> ();
				bc2d.size = new Vector2 (AxisDistance, Radius * 2f + chain_unit_height);
				ChainPhysics.transform.localPosition = Vector3.zero;
				ChainPhysics.transform.localScale = Vector3.one;
				ChainPhysics.transform.right = p0 - p1;
				bc2d.usedByEffector = true;
				Rigidbody2D r2d = ChainPhysics.EnsureComponent<Rigidbody2D> ();
				r2d.isKinematic = true;
				SurfaceEffector2D se2d = ChainPhysics.EnsureComponent<SurfaceEffector2D> ();
				se2d.speed = Speed;
			}

		}

		private void DrawGizmos ()
		{
			#if UNITY_EDITOR
			if (DoDrawGizmos) {

				Matrix4x4 trs = Matrix4x4.TRS (transform.position, Quaternion.identity, Vector3.one);
				Handles.matrix *= trs;

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

				Handles.matrix *= trs.inverse;
			}
			#endif
		}

		void OnDrawGizmos ()
		{
			TryApply ();
			
			Standardization ();
			SetupChildren ();
			RefreshParam ();
	
			RefreshChainData ();
			RenderChain ();
			DrawGizmos ();

			TrySerialize ();
		}

		List<Vector3> position_list;
		List<Vector3> direction_list;

		public void RefreshChainData ()
		{

			start_offset2 = start_offset % step_length;

			position_list = new List<Vector3> ();
			direction_list = new List<Vector3> ();

			for (float offset = start_offset2 - step_offset_length; offset <= perimeter + start_offset2 + step_offset_length; offset += step_offset_length) {
			
				if (offset < AxisDistance && offset >= 0) {
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
					if (distance < 0)
						continue;
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

				} else if (offset <= perimeter + start_offset2 + step_offset_length || offset < 0f) {
					Vector3 vb = p0_b - p0;
					float distance;
					if (offset < 0) {
						distance = offset + perimeter - AxisDistance * 2f - HalfCircumference;
					} else {
						distance = offset - AxisDistance * 2f - HalfCircumference;
					}
					if (distance > HalfCircumference)
						continue;
					float delta = 180f * distance / HalfCircumference;
					Vector3 v_delta = Quaternion.Euler (0f, 0f, -delta) * vb;
					Vector3 p = p0 + v_delta;
					position_list.Add (p);
					direction_list.Add (v_delta.normalized);

				}
			}
		}

		private float chain_unit_width = .32f;
		private float chain_unit_height = .16f;

		public void RenderChain ()
		{
			MeshFilter mf = Chain.GetComponent<MeshFilter> ();
			Mesh mesh = mf.sharedMesh;
			if (mesh == null) {
				mesh = new Mesh ();
			}

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
			mesh.Optimize ();
			mesh.name = "chain_unit";

			mf.sharedMesh = mesh;
		}

		public float Speed;

		void Start ()
		{
			Standardization ();
			SetupChildren ();
			RefreshParam ();

			RefreshChainData ();
			RenderChain ();
		}

		private float AngularSpeedRate = 120f;

		void Update ()
		{
			start_offset += Speed * Time.deltaTime;
			for (int i = 0; i < 2; i++) {
				Axises [i].FindChild ("roll").Rotate (0f, 0f, -Speed * Time.deltaTime * AngularSpeedRate);
			}

			RefreshChainData ();
			RenderChain ();
		}

		#region ICustomSerializable implementation

		private string Json;

		public void TrySerialize ()
		{
			if (DoTrySerialize) {
				DoTrySerialize = false;

				DoSerialize ();
				Debug.LogFormat ("Name/InstanceID: {0} / {1}, Json:\n{2}", name, GetInstanceID (), Json);
			}	
		}

		public string TryApply ()
		{
			if (string.IsNullOrEmpty (DoTryApply) == false) {
				Apply (DoTryApply);
				DoTryApply = "";
			}
			return null;
		}

		public string DoSerialize ()
		{
			string[] axises = new string[2];
			for (int i = 0; i < 2; i++) {
				axises [i] = Axises [i].localPosition.Serialize ();
			}

			var data = new {
				Pos = transform.localPosition.Serialize (),
				Radius = Radius,
				Speed = Speed,
				Axises = axises
			};
			Json = JsonConvert.SerializeObject (data, Formatting.Indented);
			return Json;
		}

		public void Apply (string data)
		{
			Json = data;
			Hashtable ht = JsonConvert.DeserializeObject<Hashtable> (Json);

			this.transform.localPosition.ParseSerialize (Convert.ToString (ht ["Pos"]));
			this.Radius = (float)Convert.ToDouble (ht ["Radius"]);
			this.Speed = (float)Convert.ToDouble (ht ["Speed"]);

			var axises = JsonConvert.DeserializeObject<string[]> (ht ["Axises"].ToString ());

			for (int i = 0; i < 2; i++) {
				Axises [i].localPosition.ParseSerialize (axises [i]);
			}

			this.DoStandardization = true;
		}

		#endregion
	}

}