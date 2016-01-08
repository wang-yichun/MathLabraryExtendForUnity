using UnityEngine;
using System;
using System.Collections;

namespace pogorock
{
	public static class Vector3_Extend
	{
		public static string Serialize (this Vector3 v)
		{
			return string.Format ("({0},{1},{2})", v.x, v.y, v.z);
		}

		public static Vector3 ParseSerialize (string data)
		{
			Vector3 v = Vector3.zero;
			try {
				string[] splited = data.Replace ("(", "").Replace (")", "").Split (new char[]{ ',' });

				v = new Vector3 (
					(float)Convert.ToDouble (splited [0]),
					(float)Convert.ToDouble (splited [1]),
					(float)Convert.ToDouble (splited [2])
				);
			} catch (Exception ex) {
				Debug.LogFormat ("Vector3_Extend.ParseSerialize: {0}", ex);
			}
			return v;
		}
	}
}
