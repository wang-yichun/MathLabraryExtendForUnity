using UnityEngine;
using System.Collections;

namespace pogorock
{
	interface ICustomSerializable
	{
		// 序列化
		string DoSerialize ();

		// 应用序列化数据
		void Apply (string data);
	}
}