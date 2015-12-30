using UnityEngine;
using System.Collections;

namespace Dest.Math.Ex
{
	public enum LineIntersectType
	{
		None = -1,
		//没有交点
		OnBothExtend = 0,
		//在两条直线的延长线上
		OnFirstLine = 1,
		//在第一条直线上 p0-p1
		OnSecondLine = 2,
		//在第二条直线上 p2-p3
		OnBothLine = 3
		//在第两条直线上
	}

	public static class ToolsMathUnity
	{
	
		/// <summary>
		/// 判断两个浮点数字是否相等
		/// 注意,不能用该函数判断浮点数是否为0.应用IsZero
		/// </summary>
		/// <param name="r1"></param>
		/// <param name="r2"></param>
		/// <param name="tolerance">精度</param>
		/// <returns></returns>
		public static bool IsRealEqual (float r1, float r2, float tolerance)
		{
			if (Mathf.Abs (r2) <= tolerance) {
				if (r1 > tolerance) {
					return false;
				}
			} else {
				if (Mathf.Abs (r1 / r2) - 1.0 > tolerance) {
					return false;
				}
			}
			return true;
		}

		public static bool IsRealEqual (float r1, float r2)
		{
			float tolerance = GlobalVariables._realTolerance;
			if (Mathf.Abs (r2) <= tolerance) {
				if (r1 > tolerance) {
					return false;
				}
			} else {
				if (Mathf.Abs (r1 / r2) - 1.0 > tolerance) {
					return false;
				}
			}
			return true;
		}

		public static bool IsZero (float r, float tolerance)
		{
			return (r <= tolerance && r >= -tolerance) ? true : false;
		}

		public static bool IsZero (float r)
		{
			float tolerance = GlobalVariables._realTolerance;
			return (r <= tolerance && r >= -tolerance) ? true : false;
		}

		public static LineIntersectType LineXLine (Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, ref Vector2 xpt)
		{
			/*
        **  Get the intersection of the line defined by p0 and p1 with the
        **  line defined by p2 and p3.  Sets xpt to the intersection point
        **  if return value is non-negative. 
        **
        **  Returns:
        **       None = Lines are paralell or the points are the same.
        **       OnBothExtend = Intersection is not on either segment.
        **       OnFirstLine = Intersection is on the 1st segment (p0-p1) but not the 2nd.
        **       OnSecondLine = Intersection is on the 2nd segment (p2-p3) but not the 1st.
        **       OnBothLine = Intersection is on both line segments.
        */
			float a1 = p1.y - p0.y;
			float b1 = p0.x - p1.x;
			float c1 = a1 * p0.x + b1 * p0.y;
		
			float a2 = p3.y - p2.y;
			float b2 = p2.x - p3.x;
			float c2 = a2 * p2.x + b2 * p2.y;
		
			float denominator = a1 * b2 - a2 * b1;
			if (IsZero (denominator)) {
				//paralell
				return LineIntersectType.None;
			} else {
				int on1 = 1;
				int on2 = 1;
			
				xpt.x = (b2 * c1 - b1 * c2) / denominator;
				xpt.y = (a1 * c2 - a2 * c1) / denominator;
				Vector2 vecMin = Vector2.Min (p0, p1);
				Vector2 vecMax = Vector2.Max (p0, p1);
				on1 = (vecMin.x <= xpt.x && xpt.x <= vecMax.x &&
				vecMin.y <= xpt.y && xpt.y <= vecMax.y) ? 1 : 0;
				vecMin = Vector2.Min (p2, p3);
				vecMax = Vector2.Max (p2, p3);
				on2 = (vecMin.x <= xpt.x && xpt.x <= vecMax.x &&
				vecMin.y <= xpt.y && xpt.y <= vecMax.y) ? 1 : 0;
				int result = on1 + 2 * on2;
				if (0 == result) {
					return LineIntersectType.OnBothExtend;
				} else if (1 == result) {
					return LineIntersectType.OnFirstLine;
				} else if (2 == result) {
					return LineIntersectType.OnSecondLine;
				} else {// 3==result
					return LineIntersectType.OnBothLine;
				}
			}
		}
	}

	public static class GlobalVariables
	{
		public const float _realTolerance = 1.0e-11f;
	}
}