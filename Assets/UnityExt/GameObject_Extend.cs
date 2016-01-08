using UnityEngine;
using System.Collections;

namespace pogorock
{
	public static class GameObject_Extend
	{
		public static TComponent EnsureComponent<TComponent> (this GameObject gameObject) where TComponent : Component
		{
			TComponent c = gameObject.GetComponent<TComponent> ();
			if (c == null) {
				c = gameObject.AddComponent<TComponent> ();
			}
			return c;
		}
	}
}