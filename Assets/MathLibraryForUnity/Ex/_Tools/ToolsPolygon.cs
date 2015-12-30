using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Dest.Math.Ex
{
	public static class ToolsPolygon
	{
		public const float RealTolerance = .05f;

		/// <summary>
		/// 求一个多边形被一个曲线切割成两个多边形,扩展:带有切割宽度
		/// </summary>
		/// <returns><c>true</c>, 成功切割, <c>false</c> 切割失败.</returns>
		/// <param name="polygon">输入源多边形.</param>
		/// <param name="curve">输入原曲线.</param>
		/// <param name="res_poly1">输出切割后多边形1.</param>
		/// <param name="res_poly2">输出切割后多边形2.</param>
		/// <param name="Distance">切割宽度.</param>
		public static bool BreakPolygon2D_Ex (List<Vector2> polygon, List<Vector2> curve, out List<Vector2> res_poly1, out List<Vector2> res_poly2, float Distance)
		{
			res_poly1 = null;
			res_poly2 = null;

			List<Vector2> curve_r = new List<Vector2> ();
			if (ParallelCurve (curve, Distance, out curve_r) == false) {
				return false;
			}
			if (AmendCurveWithPolygon2D (polygon, ref curve_r) == false) {
				return false;
			}

			List<Vector2> curve_l = new List<Vector2> ();
			if (ParallelCurve (curve, -Distance, out curve_l) == false) {
				return false;
			}
			if (AmendCurveWithPolygon2D (polygon, ref curve_l) == false) {
				return false;
			}

			List<Vector2> res_r1 = new List<Vector2> ();
			List<Vector2> res_r2 = new List<Vector2> ();

			List<Vector2> res_l1 = new List<Vector2> ();
			List<Vector2> res_l2 = new List<Vector2> ();

			bool reverse_r;
			if (BreakPolygon2D (polygon, curve_r, out res_r1, out res_r2, out reverse_r) == false) {
				return false;
			}
			bool reverse_l;
			if (BreakPolygon2D (polygon, curve_l, out res_l1, out res_l2, out reverse_l) == false) {
				return false;
			}

			if (reverse_r && res_r2.Count < 3) {
				return false;
			}

			if (!reverse_r && res_r1.Count < 3) {
				return false;
			}

			if (reverse_l && res_l1.Count < 3) {
				return false;
			}

			if (!reverse_l && res_l2.Count < 3) {
				return false;
			}

			if (reverse_l && reverse_r) {
				res_poly1 = res_r2;
				res_poly2 = res_l1;
			} else if (!reverse_l && !reverse_r) {
				res_poly1 = res_r1;
				res_poly2 = res_l2;
			} else if (!reverse_l && reverse_r) {
				res_poly1 = res_r2;
				res_poly2 = res_l2;
			} else if (reverse_l && !reverse_r) {
				res_poly1 = res_r1;
				res_poly2 = res_l1;
			}

			return true;
		}

		public class MPoint
		{
			public enum MPointType
			{
				// 原始
				original_start,
				original,
				original_end,

				// 延长
				extend_head,
				extend_end,

				// 交点
				intersection_original,
				intersection_extend
			}

			public MPointType type;
			public Vector2 point;
		}

		public static bool AmendCurveWithPolygon2D (List<Vector2> polygon, ref List<Vector2> curve)
		{
			// I. 扩展首尾
			float extraLength = 10f; // 扩展长度
			Vector2 v10n = (curve [0] - curve [1]).normalized;
			Vector2 exFront = curve [0] + v10n * extraLength;
			Vector2 vz10n = (curve [curve.Count - 1] - curve [curve.Count - 2]).normalized;
			Vector2 exBack = curve [curve.Count - 1] + vz10n * extraLength;

			curve.Insert (0, exFront);
			curve.Add (exBack);

			// II.制造mcurves
			List< List<MPoint> > mcurves = new List< List<MPoint> > ();
			for (int i = 0; i < curve.Count; i++) {
				if (i != curve.Count - 1) {
					// 不是最后一个点
					Vector2 cp1 = curve [i];
					Vector2 cp2 = curve [i + 1];

					List<MPoint> subMCurve = new List<MPoint> () { };
					bool isExtend = false;
					if (i == 0) {
						isExtend = true;
						subMCurve.Add (new MPoint () {
							type = MPoint.MPointType.extend_head,
							point = cp1
						});
					} else if (i == 1) {
						subMCurve.Add (new MPoint () {
							type = MPoint.MPointType.original_start,
							point = cp1
						});
					} else if (i == curve.Count - 2) {
						isExtend = true;
						subMCurve.Add (new MPoint () {
							type = MPoint.MPointType.original_end,
							point = cp1
						});
					} else {
						subMCurve.Add (new MPoint () {
							type = MPoint.MPointType.original,
							point = cp1
						});
					}

					MPoint subBasePoint = subMCurve [0];

					List<Vector2> xList = new List<Vector2> ();
					List<Vector2[]> XSegmentInPolygonList;
					List<int> IdxInPolygonList;
					if (SegmentXPolygon (new Vector2[]{ cp1, cp2 }, polygon, out XSegmentInPolygonList, out xList, out IdxInPolygonList)) {
						subMCurve.AddRange (xList.Select (_ => new MPoint () { 
							type = isExtend ? MPoint.MPointType.intersection_extend : MPoint.MPointType.intersection_original,
							point = _
						}));
					}

					subMCurve.Sort ((a, b) => Vector2.Distance (a.point, subBasePoint.point).CompareTo (Vector2.Distance (b.point, subBasePoint.point)));

					mcurves.Add (subMCurve);
				} else {
					// 最后一个点
					mcurves.Add (new List<MPoint> () { 
						new MPoint () {
							type = MPoint.MPointType.extend_end,
							point = curve [i]
						}
					});
				}
			}

			// II.test
			curve.Clear ();

			List<MPoint> fullCurve = new List<MPoint> ();
			foreach (List<MPoint> mcurve in mcurves) {
				fullCurve.AddRange (mcurve);
			}

			bool originalStartCheckOver = false;
			for (int i = 0; i < fullCurve.Count; i++) {
				MPoint mPoint = fullCurve [i];

				//  找到第一个原始初始点,那么这个点的前一个列入结果起始点
				if (mPoint.type == MPoint.MPointType.original_start) {
					originalStartCheckOver = true;

					// 向前检查, 是否有交点存在(1个 or 2个及以上)
					int xCountFront = 0;
					List<MPoint> fList = new List<MPoint> ();
					for (int j = i - 1; j >= 0; j--) {
						MPoint mPointFrontCheck = fullCurve [j];
						if (mPointFrontCheck.type == MPoint.MPointType.intersection_extend) {
							xCountFront++;
							fList.Add (mPointFrontCheck);
						}
					}

					if (fList.Count % 2 != 0) {
						curve.Add (fList [0].point);
					}
				}

				if (curve.Count > 0 || originalStartCheckOver && mPoint.type == MPoint.MPointType.intersection_original) {
					curve.Add (mPoint.point);
				}

				if (mPoint.type == MPoint.MPointType.intersection_original || mPoint.type == MPoint.MPointType.intersection_extend) {
					if (curve.Count > 1) {
						break;
					}
				}
			}

			if (curve.Count == 0)
				return false;

			ExtendCurve (curve, .01f);

			return true;
		}

		/// <summary>
		/// 求一个多边形被一个曲线切割成两个多边形
		/// </summary>
		/// <returns><c>true</c>, 成功切割, <c>false</c> 切割失败.</returns>
		/// <param name="polygon">输入源多边形.</param>
		/// <param name="curve">输入原曲线.</param>
		/// <param name="res_poly1">输出切割后多边形1.</param>
		/// <param name="res_poly2">输出切割后多边形2.</param>
		public static bool BreakPolygon2D (List<Vector2> polygon, List<Vector2> curve, out List<Vector2> res_poly1, out List<Vector2> res_poly2, out bool reverse)
		{
			res_poly1 = null;
			res_poly2 = null;
			reverse = false;

			Polygon2 pol = new Polygon2 (polygon.ToArray ());

			Orientations or;
			if (pol.IsConvex (out or)) {
				if (or == Orientations.CW) {
					polygon.Reverse ();
				} else if (or == Orientations.None) {
					return false;
				}
			} else {
				return false;
			}

//			if (Ethan.ETools.Triangulates.Area (polygon) > 0f) {
//				polygon.Reverse ();
//			}

			List<string> exceptionList = new List<string> ();

			Vector2 A1 = new Vector2 ();
//			Vector2 B1 = new Vector2 ();
			Vector2 C1 = new Vector2 ();
//			Vector2 D1 = new Vector2 ();
			Vector2 X1 = new Vector2 ();
			int idxInCurve1 = 0;
			int idxInPolygon1 = 0;

//			Vector2 A2 = new Vector2 ();
//			Vector2 B2 = new Vector2 ();
//			Vector2 C2 = new Vector2 ();
//			Vector2 D2 = new Vector2 ();
			Vector2 X2 = new Vector2 ();
			int idxInCurve2 = 0;
			int idxInPolygon2 = 0;

			int CrossCount = 0;

			int SpecialMode = 0;

			// STEP 1. 找到相交点
			for (int i = 0; i < polygon.Count; i++) {
				Vector2 p1 = polygon [i];
				Vector2 p2 = polygon [(i + 1) % polygon.Count];

				List<Vector2[]> XSegmentInCurveList = new List<Vector2[]> ();
				List<Vector2> XList = new List<Vector2> ();
				List<int> IdxInCurveList = new List<int> ();

				if (SegmentXCurve (
					    new Vector2[]{ p1, p2 },
					    curve,
					    out XSegmentInCurveList,
					    out XList,
					    out IdxInCurveList
				    )) {

					CrossCount++;
					if (CrossCount >= 3) {
						exceptionList.Add ("curve cross polygon 3 or more times");
						break;
					}

					if (XList.Count >= 3) {
						exceptionList.Add ("A segment in polygon cross curve 3 or more times");
						break;
					} else if (XList.Count == 2) {

						SpecialMode = 1;

						A1 = p1;
//						B1 = p2;
						C1 = XSegmentInCurveList [0] [0];
//						D1 = XSegmentInCurveList [0] [1];
						X1 = XList [0];
						idxInCurve1 = IdxInCurveList [0];
						idxInPolygon1 = i;

//						A2 = p1;
//						B2 = p2;
//						C2 = XSegmentInCurveList [1] [0];
//						D2 = XSegmentInCurveList [1] [1];
						X2 = XList [1];
						idxInCurve2 = IdxInCurveList [1];
						idxInPolygon2 = i;

						break;
					}

					if (CrossCount == 1) {
						A1 = p1;
//						B1 = p2;
						C1 = XSegmentInCurveList [0] [0];
//						D1 = XSegmentInCurveList [0] [1];
						X1 = XList [0];
						idxInCurve1 = IdxInCurveList [0];
						idxInPolygon1 = i;

					} else if (CrossCount == 2) {
//						A2 = p1;
//						B2 = p2;
//						C2 = XSegmentInCurveList [0] [0];
//						D2 = XSegmentInCurveList [0] [1];
						X2 = XList [0];
						idxInCurve2 = IdxInCurveList [0];
						idxInPolygon2 = i;
					} else {
						exceptionList.Add ("Will never be got here, if see this at exception list, there must be sth. wrong.");
					}
				}
			}

			if (exceptionList.Count > 0) {
				return false;
			}

			// STEP 2. 判断顺切or逆切
			if (idxInCurve1 > idxInCurve2) {
				reverse = true;
			} else if (idxInCurve1 < idxInCurve2) {
				reverse = false;
			} else {
				SpecialMode = 2; // Curve中的一个线段切到了polygon的两个边
				if (Vector2.Distance (C1, X1) > Vector2.Distance (C1, X2)) {
					reverse = true;
				} else {
					reverse = false;
				}
			}

			if (SpecialMode == 1) {
				// 多边形上的一条边上有两个点
				if (Vector2.Distance (A1, X1) > Vector2.Distance (A1, X2)) {
					reverse = true;
				} else {
					reverse = false;
				}
			}

			// STEP 3. 构造多边形
			res_poly1 = new List<Vector2> ();
			res_poly2 = new List<Vector2> ();

			// 普通模式
			for (int i = 0; i < polygon.Count; i++) {

				if (i <= idxInPolygon1 || i > idxInPolygon2) {

					res_poly1.Add (polygon [i]);

					if (i == idxInPolygon1) {
						if (SpecialMode == 1 && reverse) {
							res_poly1.Add (X2);
						} else {
							res_poly1.Add (X1);
						}

						if (SpecialMode == 2) {
							res_poly1.Add (X2);
						} else {
							if (!reverse) {
								for (int j = idxInCurve1 + 1; j <= idxInCurve2; j++) {
									res_poly1.Add (curve [j]);

									if (j == idxInCurve2) {
										res_poly1.Add (X2);
									}
								}
							} else {
								// todo:
								if (SpecialMode == 0) {
									for (int j = idxInCurve1; j > idxInCurve2; j--) {
										res_poly1.Add (curve [j]);

										if (j == idxInCurve2 + 1) {
											res_poly1.Add (X2);
										}
									}
								} else {
									for (int j = idxInCurve2; j > idxInCurve1; j--) {
										res_poly1.Add (curve [j]);
										if (j == idxInCurve1 + 1) {
											res_poly1.Add (X1);
										}
									}
								}
							}
						}
					}
				} else {

					if (SpecialMode == 2 && i == idxInPolygon1 + 1) {
						res_poly2.Add (X1);
					}

					res_poly2.Add (polygon [i]);

					if (i == idxInPolygon2) {
						res_poly2.Add (X2);

						if (!reverse) {
							for (int j = idxInCurve2; j > idxInCurve1; j--) {
								res_poly2.Add (curve [j]);

								if (j == idxInCurve1 + 1) {
									res_poly2.Add (X1);
								}
							}
						} else {
							// todo:
							for (int j = idxInCurve2 + 1; j <= idxInCurve1; j++) {
								res_poly2.Add (curve [j]);

								if (j == idxInCurve1) {
									res_poly2.Add (X1);
								}
							}
						}
					}
				}
			}

			if (SpecialMode == 1) {
				if (!reverse) {
					res_poly2.Add (X2);
					for (int j = idxInCurve2; j > idxInCurve1; j--) {
						res_poly2.Add (curve [j]);

						if (j == idxInCurve1 + 1) {
							res_poly2.Add (X1);
						}
					}
				} else {
					res_poly2.Add (X2);
					for (int j = idxInCurve2; j > idxInCurve1; j--) {
						res_poly2.Add (curve [j]);

						if (j == idxInCurve1 + 1) {
							res_poly2.Add (X1);
						}
					}
				}
			}
//
//			if (SpecialMode == 2) {
//				if (reverse) {
////					List<Vector2> tempList = res_poly1;
////					res_poly1 = res_poly2;
////					res_poly2 = tempList;
//					reverse = false;
//				}
//			}


			return true;
		}

		/// <summary>
		/// 求线段与曲线(线段组)的交点
		/// 可能有多个交点
		/// </summary>
		/// <returns><c>true</c>, 有交点, <c>false</c> 无交点</returns>
		/// <param name="Segment">输入线段</param>
		/// <param name="Curve">输入曲线</param>
		/// <param name="XSegmentInCurve">曲线中相交线段组.</param>
		/// <param name="X">交点(与相交线段组索引对应)</param>
		/// <param name="X">对应的交点位于Curve中的第几段</param>
		public static bool SegmentXCurve (Vector2[] Segment, List<Vector2> Curve, out List<Vector2[]> XSegmentInCurveList, out List<Vector2> XList, out List<int> IdxInCurveList)
		{

			Vector2 sp1 = Segment [0];
			Vector2 sp2 = Segment [1];

			Vector2 x = new Vector2 ();

			XList = new List<Vector2> ();
			XSegmentInCurveList = new List<Vector2[]> ();
			IdxInCurveList = new List<int> ();


			for (int i = 0; i < Curve.Count - 1; i++) {
				Vector2 cp1 = Curve [i];
				Vector2 cp2 = Curve [i + 1];

				LineIntersectType intersectType = ToolsMathUnity.LineXLine (sp1, sp2, cp1, cp2, ref x);

				if (intersectType == LineIntersectType.OnBothLine) {
					Vector2[] line = new Vector2[2] { cp1, cp2 };
					XSegmentInCurveList.Add (line);
					XList.Add (x);
					IdxInCurveList.Add (i);
				}
			}

			if (XList.Count == 0) {
				return false;
			}
			return true;
		}

		public static bool SegmentXPolygon (Vector2[] Segment, List<Vector2> Polygon, out List<Vector2[]> XSegmentInPolygonList, out List<Vector2> XList, out List<int> IdxInPolygonList)
		{
			Vector2 sp1 = Segment [0];
			Vector2 sp2 = Segment [1];

			Vector2 x = new Vector2 ();

			XList = new List<Vector2> ();
			XSegmentInPolygonList = new List<Vector2[]> ();
			IdxInPolygonList = new List<int> ();

			for (int i = 0; i < Polygon.Count; i++) {
				Vector2 cp1 = Polygon [i];
				Vector2 cp2 = Polygon [(i + 1) % Polygon.Count];

				LineIntersectType intersectType = ToolsMathUnity.LineXLine (sp1, sp2, cp1, cp2, ref x);

				if (intersectType == LineIntersectType.OnBothLine) {
					Vector2[] line = new Vector2[2] { cp1, cp2 };
					XSegmentInPolygonList.Add (line);
					XList.Add (x);
					IdxInPolygonList.Add (i);
				}
			}

			if (XList.Count == 0) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// 向两端延长曲线
		/// </summary>
		/// <returns>延长后的结果曲线</returns>
		/// <param name="Curve">输入原曲线</param>
		/// <param name="ExLength">延长的长度</param>
		public static List<Vector2> ExtendCurve (List<Vector2> Curve, float ExLength = 100.0f)
		{
			if (Curve.Count >= 2) {
				Vector2 frontVec = (Curve [0] - Curve [1]).normalized;
				Vector2 frontExPoint = frontVec * ExLength + Curve [0];

				Vector2 backVec = (Curve [Curve.Count - 1] - Curve [Curve.Count - 2]).normalized;
				Vector2 backExPoint = backVec * ExLength + Curve [Curve.Count - 1];

				Curve.RemoveAt (0);
				Curve.Insert (0, frontExPoint);
				Curve.RemoveAt (Curve.Count - 1);
				Curve.Add (backExPoint);
			}

			return Curve;
		}

		public static List<Vector2> TrimCurve (List<Vector2> Curve, int count = 1)
		{
			if (Curve.Count >= count * 2 + 1) {
				for (int i = 0; i < count; i++) {
					Curve.RemoveAt (0);
					Curve.RemoveAt (Curve.Count - 1);
				}
			}
			return Curve;
		}

		/// <summary>
		/// 求曲线的平行线
		/// </summary>
		/// <returns><c>true</c>, 可求出结果, <c>false</c> 无法求出结果.</returns>
		/// <param name="Curve">Curve.</param>
		/// <param name="Distance">Distance.</param>
		/// <param name="ResultCurve">Result curve.</param>
		public static bool ParallelCurve (List<Vector2> Curve, float Distance, out List<Vector2> ResultCurve)
		{

			ResultCurve = null;

			if (Curve.Count < 2) {
				return false;
			}

			ResultCurve = new List<Vector2> ();

			for (int i = 0; i < Curve.Count; i++) {

				Vector2 A1, A2, A3;
				Vector2 B1, B2, B3;

				if (i == 0) {
					A2 = Curve [i];
					A3 = Curve [i + 1];
					Vector2 v23 = A3 - A2;
					Vector3 nv_v3 = Vector3.Cross (new Vector3 (v23.x, v23.y).normalized, new Vector3 (0, 0, 1));
					Vector2 nv = new Vector2 (nv_v3.x, nv_v3.y);
					Vector2 vAB = nv * Distance;
					B2 = A2 + vAB;

					ResultCurve.Add (B2);

				} else if (i == Curve.Count - 1) {
					A1 = Curve [i - 1];
					A2 = Curve [i];
					Vector2 v12 = A2 - A1;
					Vector3 nv_v3 = Vector3.Cross (new Vector3 (v12.x, v12.y).normalized, new Vector3 (0, 0, 1));
					Vector2 nv = new Vector2 (nv_v3.x, nv_v3.y);
					Vector2 vAB = nv * Distance;
					B2 = A2 + vAB;
					ResultCurve.Add (B2);

				} else {
					A1 = Curve [i - 1];
					A2 = Curve [i];
					A3 = Curve [i + 1];
					Vector2 v12 = A2 - A1;
					Vector2 v23 = A3 - A2;

					// 夹角过小按平行线处理
					float angle = Vector2.Angle (v12, v23);
					if (angle < RealTolerance) {
						Vector3 nv_v3_1 = Vector3.Cross (new Vector3 (v12.x, v12.y).normalized, new Vector3 (0, 0, 1));
						Vector2 nv_1 = new Vector2 (nv_v3_1.x, nv_v3_1.y);
						Vector2 vAB1 = nv_1 * Distance;
						B2 = A2 + vAB1;
						ResultCurve.Add (B2);

					} else {

						Vector3 nv_v3_1 = Vector3.Cross (new Vector3 (v12.x, v12.y).normalized, new Vector3 (0, 0, 1));
						Vector2 nv_1 = new Vector2 (nv_v3_1.x, nv_v3_1.y);
						Vector2 vAB1 = nv_1 * Distance;
						B1 = A1 + vAB1;
						Vector2 B2a = A2 + vAB1;
					
						Vector3 nv_v3_2 = Vector3.Cross (new Vector3 (v23.x, v23.y).normalized, new Vector3 (0, 0, 1));
						Vector2 nv_2 = new Vector2 (nv_v3_2.x, nv_v3_2.y);
						Vector2 vAB2 = nv_2 * Distance;
						Vector2 B2b = A2 + vAB2;
						B3 = A3 + vAB2;
					
						B2 = new Vector2 ();
						if (ToolsMathUnity.LineXLine (B1, B2a, B2b, B3, ref B2) != LineIntersectType.None) {
							ResultCurve.Add (B2);
						} else {
							B2 = A2 + vAB1;
							ResultCurve.Add (B2);
						}
					}
				}
			}
			return true;
		}

		/// <summary>
		/// 多边形点顺逆方向描述
		/// </summary>
		public enum PolygonType
		{
			None,
			CW,
			CCW
		}

		/// <summary>
		/// 计算多边形点列表的描述顺逆方向
		/// </summary>
		/// <returns>顺逆方向枚举.</returns>
		/// <param name="polygon">输入多边形点列表.</param>
		public static PolygonType CalcPolygonType (List<Vector2> polygon)
		{

			List<Vector2> centerList = polygon;
			double angleSum = 0;

			for (int i = 0; i < centerList.Count; i++) {
				Vector2 thisP = centerList [i];
				Vector2 nextP = centerList [(i + 1) % centerList.Count];
				Vector2 nextP2 = centerList [(i + 2) % centerList.Count];

				Vector2 thisV = nextP - thisP;
				Vector2 nextV = nextP2 - nextP;

				float angle = Vector2.Angle (thisV, nextV);
				Vector3 crossRes = Vector3.Cross (thisV, nextV).normalized;

				angleSum += angle * crossRes.z;
			}
				
			bool commonShape = Mathf.Abs (Mathf.Abs ((float)angleSum) - 360f) < 1.0f;
			if (commonShape) {
				if (angleSum > 0) {
					return PolygonType.CCW;
				} else {
					return PolygonType.CW;
				}
			}
			return PolygonType.None;
		}

		public static bool MakePolygonCW (ref List<Vector2> polygon)
		{
			PolygonType pt = CalcPolygonType (polygon);
			if (pt == PolygonType.CW) {
				return false;
			} else if (pt == PolygonType.CCW) {
				polygon.Reverse ();
				return true;
			} else {
				return false;
			}
		}

		public class IntersectionInfo
		{
			public int Idx1;
			public int Idx2;
			public bool Idx1IsEnter;
			public Vector2 X;
			public int IdxDistance;
		}

		public class MonotoneListInfo
		{
			public List<int> MonotoneList;
			public bool ValueIncrease;
			public bool IsActive;
			public int CurI;
			public bool Open;
		}

		public struct V2IDX
		{
			public int idx;
			public float x;
			public float y;
		}

		public static List<IntersectionInfo> PolygonIntersection (List<Vector2> polygon)
		{
			List<IntersectionInfo> result = new List<IntersectionInfo> ();

			if (polygon.Count == 0) {
				return result;
			}

			// STEP 1. 求单调链
			List<MonotoneListInfo> MonotoneListInfoList = new List<MonotoneListInfo> ();

			MonotoneListInfo currentMLI = new MonotoneListInfo () {
				MonotoneList = new List<int> ()
			};

			for (int i0 = 0; i0 <= polygon.Count; i0++) {

				int i = i0 % polygon.Count;

				if (currentMLI.MonotoneList.Count == 0) {
					currentMLI.MonotoneList.Add (i);
				} else if (currentMLI.MonotoneList.Count == 1) {
					if (polygon [i].x > polygon [currentMLI.MonotoneList [0]].x) {
						currentMLI.MonotoneList.Add (i);
						currentMLI.ValueIncrease = true;
					} else {
						currentMLI.MonotoneList.Insert (0, i);
						currentMLI.ValueIncrease = false;
					}
				} else {
					if (currentMLI.ValueIncrease) {
						Vector2 maxV = polygon [currentMLI.MonotoneList [currentMLI.MonotoneList.Count - 1]];
						if (polygon [i].x >= maxV.x) {
							currentMLI.MonotoneList.Add (i);
						} else {
							MonotoneListInfoList.Add (currentMLI);
							currentMLI = new MonotoneListInfo () {
								MonotoneList = new List<int> () { i, (i - 1 + polygon.Count) % polygon.Count },
								ValueIncrease = !currentMLI.ValueIncrease
							};
						}
					} else {
						Vector2 minV = polygon [currentMLI.MonotoneList [0]];
						if (polygon [i].x <= minV.x) {
							currentMLI.MonotoneList.Insert (0, i);
						} else {
							MonotoneListInfoList.Add (currentMLI);
							currentMLI = new MonotoneListInfo () {
								MonotoneList = new List<int> () { (i - 1 + polygon.Count) % polygon.Count, i },
								ValueIncrease = !currentMLI.ValueIncrease
							};
						}
					}
				}

				#region Polygon单调链最后处理
				// Curve与Polygon的差别在于此,对于循环到最后一个的处理方式有所区别
				if (i0 == polygon.Count) {
					MonotoneListInfoList.Add (currentMLI);
					// 循环到了第一个
					if (MonotoneListInfoList.Count % 2 != 0) {
						if (MonotoneListInfoList [0].ValueIncrease) {
							MonotoneListInfoList [0].MonotoneList.RemoveAt (0);
							MonotoneListInfoList [0].MonotoneList.InsertRange (0, MonotoneListInfoList [MonotoneListInfoList.Count - 1].MonotoneList);
						} else {
							MonotoneListInfoList [0].MonotoneList.RemoveAt (MonotoneListInfoList [0].MonotoneList.Count - 1);
							MonotoneListInfoList [0].MonotoneList.AddRange (MonotoneListInfoList [MonotoneListInfoList.Count - 1].MonotoneList);
							MonotoneListInfoList.RemoveAt (MonotoneListInfoList.Count - 1);
						}	
					}
				}
				#endregion
			}

			// SETP 2. 计算扫描线表

			List<V2IDX> v2idxlist = new List<V2IDX> ();

			for (int i = 0; i < polygon.Count; i++) {
				v2idxlist.Add (new V2IDX () {
					idx = i,
					x = polygon [i].x,
					y = polygon [i].y
				});
			}

			v2idxlist.Sort ((a, b) => {
				return a.x.CompareTo (b.x);
			});

//			Debug.Log (Newtonsoft.Json.JsonConvert.SerializeObject (v2idxlist));
//
//			string mlil = Newtonsoft.Json.JsonConvert.SerializeObject (MonotoneListInfoList);
//			Debug.Log (mlil);

			// STEP 3. 扫描找交点

			for (int i = 0; i < v2idxlist.Count; i++) {

				int idx = v2idxlist [i].idx;

//				Debug.Log ("idx: " + idx);

				// SETP 3.1 设置活动单调链和当前边
				for (int j = 0; j < MonotoneListInfoList.Count; j++) {
					MonotoneListInfo monoInfo = MonotoneListInfoList [j];

					if (monoInfo.CurI + 1 == monoInfo.MonotoneList.Count) {
						continue;
					}

					if (monoInfo.MonotoneList [monoInfo.CurI + 1] == idx) {
						monoInfo.CurI++;

						if (monoInfo.CurI == monoInfo.MonotoneList.Count - 1) {
							monoInfo.Open = false;
						}
					}
						
					if (monoInfo.MonotoneList [monoInfo.CurI] == idx && monoInfo.CurI != monoInfo.MonotoneList.Count - 1) {
						monoInfo.Open = true;
						monoInfo.IsActive = true;
					} else {
						monoInfo.IsActive = false;
					}
				}

//				Debug.Log (Newtonsoft.Json.JsonConvert.SerializeObject (MonotoneListInfoList));



				// STEP 3.2 当前活动边与当前非活动边之间的相交性
				for (int k = 0; k < MonotoneListInfoList.Count; k++) {
					MonotoneListInfo monoInfo1 = MonotoneListInfoList [k];
					if (monoInfo1.Open && monoInfo1.IsActive) {

						Vector2 p1 = polygon [monoInfo1.MonotoneList [monoInfo1.CurI]];
						Vector2 p2 = polygon [monoInfo1.MonotoneList [monoInfo1.CurI + 1]];

						for (int l = 0; l < MonotoneListInfoList.Count; l++) {
							MonotoneListInfo monoInfo2 = MonotoneListInfoList [l];
							if (monoInfo2.Open && monoInfo2.IsActive == false) {

								Vector2 p3 = polygon [monoInfo2.MonotoneList [monoInfo2.CurI]];
								Vector2 p4 = polygon [monoInfo2.MonotoneList [monoInfo2.CurI + 1]];

								if (monoInfo1.MonotoneList [monoInfo1.CurI + 1] == monoInfo2.MonotoneList [monoInfo2.CurI + 1]) {
									continue;
								}

								Vector2 x = new Vector2 ();
								LineIntersectType intersectType = ToolsMathUnity.LineXLine (p1, p2, p3, p4, ref x);

								if (intersectType == LineIntersectType.OnBothLine) {

									IntersectionInfo intersectionInfo = new IntersectionInfo ();

									if (monoInfo1.ValueIncrease) {
										intersectionInfo.Idx1 = monoInfo1.MonotoneList [monoInfo1.CurI];
									} else {
										intersectionInfo.Idx1 = monoInfo1.MonotoneList [monoInfo1.CurI + 1];
									}

									if (monoInfo2.ValueIncrease) {
										intersectionInfo.Idx2 = monoInfo2.MonotoneList [monoInfo2.CurI];
									} else {
										intersectionInfo.Idx2 = monoInfo2.MonotoneList [monoInfo2.CurI + 1];
									}

									int dis1, dis2;
									if (intersectionInfo.Idx1 > intersectionInfo.Idx2) {
										dis1 = intersectionInfo.Idx1 - intersectionInfo.Idx2;
										dis2 = (intersectionInfo.Idx2 - intersectionInfo.Idx1 + polygon.Count) % polygon.Count;
										if (dis1 > dis2) {
											intersectionInfo.Idx1IsEnter = true;
											intersectionInfo.IdxDistance = dis2;
										} else {
											intersectionInfo.Idx1IsEnter = false;
											intersectionInfo.IdxDistance = dis1;
										}
									} else {
										dis1 = intersectionInfo.Idx2 - intersectionInfo.Idx1;
										dis2 = (intersectionInfo.Idx1 - intersectionInfo.Idx2 + polygon.Count) % polygon.Count;
										if (dis1 > dis2) {
											intersectionInfo.Idx1IsEnter = false;
											intersectionInfo.IdxDistance = dis2;
										} else {
											intersectionInfo.Idx1IsEnter = true;
											intersectionInfo.IdxDistance = dis1;
										}
									}

//									intersectionInfo.IdxDistance = Math.Min (dis1, dis2);

									intersectionInfo.X = x;

									if (AlreadyHaveIntersection (result, intersectionInfo) == false) {
										result.Add (intersectionInfo);
									}
								}
							}

						}
					}
				}
			}

//			Debug.Log (Newtonsoft.Json.JsonConvert.SerializeObject (result));

			return result;
		}

		public static bool AlreadyHaveIntersection (List<IntersectionInfo> intersectionInfoList, IntersectionInfo intersectionInfo)
		{
			int count = intersectionInfoList.Where (ii => {
				return ii.Idx1 == intersectionInfo.Idx1 && ii.Idx2 == intersectionInfo.Idx2;
			}).Count ();

			return count > 0;
		}

		public static List<IntersectionInfo> CurveIntersection (List<Vector2> polygon)
		{
			List<IntersectionInfo> result = new List<IntersectionInfo> ();

			if (polygon.Count == 0) {
				return result;
			}

			// STEP 1. 求单调链
			List<MonotoneListInfo> MonotoneListInfoList = new List<MonotoneListInfo> ();

			MonotoneListInfo currentMLI = new MonotoneListInfo () {
				MonotoneList = new List<int> ()
			};

			for (int i0 = 0; i0 <= polygon.Count; i0++) {

				int i = i0 % polygon.Count;

				if (currentMLI.MonotoneList.Count == 0) {
					currentMLI.MonotoneList.Add (i);
				} else if (currentMLI.MonotoneList.Count == 1) {
					if (polygon [i].x > polygon [currentMLI.MonotoneList [0]].x) {
						currentMLI.MonotoneList.Add (i);
						currentMLI.ValueIncrease = true;
					} else {
						currentMLI.MonotoneList.Insert (0, i);
						currentMLI.ValueIncrease = false;
					}
				} else {
					if (currentMLI.ValueIncrease) {
						Vector2 maxV = polygon [currentMLI.MonotoneList [currentMLI.MonotoneList.Count - 1]];
						if (polygon [i].x >= maxV.x) {
							currentMLI.MonotoneList.Add (i);
						} else {
							MonotoneListInfoList.Add (currentMLI);
							currentMLI = new MonotoneListInfo () {
								MonotoneList = new List<int> () { i, (i - 1 + polygon.Count) % polygon.Count },
								ValueIncrease = !currentMLI.ValueIncrease
							};
						}
					} else {
						Vector2 minV = polygon [currentMLI.MonotoneList [0]];
						if (polygon [i].x <= minV.x) {
							currentMLI.MonotoneList.Insert (0, i);
						} else {
							MonotoneListInfoList.Add (currentMLI);
							currentMLI = new MonotoneListInfo () {
								MonotoneList = new List<int> () { (i - 1 + polygon.Count) % polygon.Count, i },
								ValueIncrease = !currentMLI.ValueIncrease
							};
						}
					}
				}
					
				#region Curve单调链最后处理
				// Curve与Polygon的差别在于此,对于循环到最后一个的处理方式有所区别
				if (i0 == polygon.Count) {
					MonotoneListInfoList.Add (currentMLI);
					// 循环到了第一个
					if (MonotoneListInfoList [MonotoneListInfoList.Count - 1].MonotoneList.Count == 2) {
						MonotoneListInfoList.RemoveAt (MonotoneListInfoList.Count - 1);
					} else {
						if (MonotoneListInfoList [MonotoneListInfoList.Count - 1].ValueIncrease) {
							MonotoneListInfoList [MonotoneListInfoList.Count - 1].MonotoneList.RemoveAt (MonotoneListInfoList [MonotoneListInfoList.Count - 1].MonotoneList.Count - 1);
						} else {
							MonotoneListInfoList [MonotoneListInfoList.Count - 1].MonotoneList.RemoveAt (0);
						}
					}
				}
				#endregion
			}

			// SETP 2. 计算扫描线表

			List<V2IDX> v2idxlist = new List<V2IDX> ();

			for (int i = 0; i < polygon.Count; i++) {
				v2idxlist.Add (new V2IDX () {
					idx = i,
					x = polygon [i].x,
					y = polygon [i].y
				});
			}

			v2idxlist.Sort ((a, b) => {
				return a.x.CompareTo (b.x);
			});

			//			Debug.Log (Newtonsoft.Json.JsonConvert.SerializeObject (v2idxlist));
			//
			//			string mlil = Newtonsoft.Json.JsonConvert.SerializeObject (MonotoneListInfoList);
			//			Debug.Log (mlil);

			// STEP 3. 扫描找交点

			for (int i = 0; i < v2idxlist.Count; i++) {

				int idx = v2idxlist [i].idx;

				//				Debug.Log ("idx: " + idx);

				// SETP 3.1 设置活动单调链和当前边
				for (int j = 0; j < MonotoneListInfoList.Count; j++) {
					MonotoneListInfo monoInfo = MonotoneListInfoList [j];

					if (monoInfo.CurI + 1 == monoInfo.MonotoneList.Count) {
						continue;
					}

					if (monoInfo.MonotoneList [monoInfo.CurI + 1] == idx) {
						monoInfo.CurI++;

						if (monoInfo.CurI == monoInfo.MonotoneList.Count - 1) {
							monoInfo.Open = false;
						}
					}

					if (monoInfo.MonotoneList [monoInfo.CurI] == idx && monoInfo.CurI != monoInfo.MonotoneList.Count - 1) {
						monoInfo.Open = true;
						monoInfo.IsActive = true;
					} else {
						monoInfo.IsActive = false;
					}
				}

				//				Debug.Log (Newtonsoft.Json.JsonConvert.SerializeObject (MonotoneListInfoList));



				// STEP 3.2 当前活动边与当前非活动边之间的相交性
				for (int k = 0; k < MonotoneListInfoList.Count; k++) {
					MonotoneListInfo monoInfo1 = MonotoneListInfoList [k];
					if (monoInfo1.Open && monoInfo1.IsActive) {

						Vector2 p1 = polygon [monoInfo1.MonotoneList [monoInfo1.CurI]];
						Vector2 p2 = polygon [monoInfo1.MonotoneList [monoInfo1.CurI + 1]];

						for (int l = 0; l < MonotoneListInfoList.Count; l++) {
							MonotoneListInfo monoInfo2 = MonotoneListInfoList [l];
							if (monoInfo2.Open && monoInfo2.IsActive == false) {

								Vector2 p3 = polygon [monoInfo2.MonotoneList [monoInfo2.CurI]];
								Vector2 p4 = polygon [monoInfo2.MonotoneList [monoInfo2.CurI + 1]];

								if (monoInfo1.MonotoneList [monoInfo1.CurI + 1] == monoInfo2.MonotoneList [monoInfo2.CurI + 1]) {
									continue;
								}

								Vector2 x = new Vector2 ();
								LineIntersectType intersectType = ToolsMathUnity.LineXLine (p1, p2, p3, p4, ref x);

								if (intersectType == LineIntersectType.OnBothLine) {

									IntersectionInfo intersectionInfo = new IntersectionInfo ();

									if (monoInfo1.ValueIncrease) {
										intersectionInfo.Idx1 = monoInfo1.MonotoneList [monoInfo1.CurI];
									} else {
										intersectionInfo.Idx1 = monoInfo1.MonotoneList [monoInfo1.CurI + 1];
									}

									if (monoInfo2.ValueIncrease) {
										intersectionInfo.Idx2 = monoInfo2.MonotoneList [monoInfo2.CurI];
									} else {
										intersectionInfo.Idx2 = monoInfo2.MonotoneList [monoInfo2.CurI + 1];
									}

									int dis1, dis2;
									if (intersectionInfo.Idx1 > intersectionInfo.Idx2) {
										dis1 = intersectionInfo.Idx1 - intersectionInfo.Idx2;
										dis2 = (intersectionInfo.Idx2 - intersectionInfo.Idx1 + polygon.Count) % polygon.Count;
										if (dis1 > dis2) {
											intersectionInfo.Idx1IsEnter = true;
											intersectionInfo.IdxDistance = dis2;
										} else {
											intersectionInfo.Idx1IsEnter = false;
											intersectionInfo.IdxDistance = dis1;
										}
									} else {
										dis1 = intersectionInfo.Idx2 - intersectionInfo.Idx1;
										dis2 = (intersectionInfo.Idx1 - intersectionInfo.Idx2 + polygon.Count) % polygon.Count;
										if (dis1 > dis2) {
											intersectionInfo.Idx1IsEnter = false;
											intersectionInfo.IdxDistance = dis2;
										} else {
											intersectionInfo.Idx1IsEnter = true;
											intersectionInfo.IdxDistance = dis1;
										}
									}

									//									intersectionInfo.IdxDistance = Math.Min (dis1, dis2);

									intersectionInfo.X = x;

									result.Add (intersectionInfo);
								}
							}

						}
					}
				}
			}

			//			Debug.Log (Newtonsoft.Json.JsonConvert.SerializeObject (result));

			return result;
		}

		public enum Way
		{
			ADD,
			DELETE
		}

		public class PolygonPointhandleInfo
		{
			public Way way;
			public Vector2 add_point;
		}

		public static List<Vector2> GetMaxSimplifyPolygon (List<Vector2> polygon)
		{
			List< List<Vector2> > polygonLists = GetChildSimplifyPolygon (polygon);
			polygonLists.Sort ((a, b) => {
				return a.Count.CompareTo (b.Count);
			});

			return polygonLists.Last ();
		}

		// 获取简单子多边形
		public static List< List<Vector2> > GetChildSimplifyPolygon (List<Vector2> polygon)
		{

			List<IntersectionInfo> infoList = PolygonIntersection (polygon);

			if (infoList.Count == 0) {
				return new List< List<Vector2> > () {
					polygon
				};
			}

//			Debug.Log (JsonConvert.SerializeObject (infoList.Select ((a, idx) => new {Id = idx, a.Idx1, a.Idx2, P = a.X.ToString ()}).ToList ()));


			// 建立交点插入字典
			Dictionary< int, List<VertexWithId> > IID = new Dictionary<int, List<VertexWithId>> ();
//			foreach (IntersectionInfo info in infoList) {
			for (int id = 0; id < infoList.Count; id++) {
				IntersectionInfo info = infoList [id];

				if (IID.ContainsKey (info.Idx1)) {
					IID [info.Idx1].Add (new VertexWithId () { Point = info.X, Id = id });
				} else {
					IID.Add (info.Idx1, new List<VertexWithId> () { new VertexWithId () { Point = info.X, Id = id } });
				}
				if (IID.ContainsKey (info.Idx2)) {
					IID [info.Idx2].Add (new VertexWithId () { Point = info.X, Id = id });
				} else {
					IID.Add (info.Idx2, new List<VertexWithId> () { new VertexWithId () { Point = info.X, Id = id } });
				}
			}

			// 排序字典项的点
			foreach (KeyValuePair< int, List<VertexWithId> > item in IID) {
				Vector2 p = polygon [item.Key];
				item.Value.Sort ((a, b) => {
					return Vector2.SqrMagnitude (a.Point - p).CompareTo (Vector2.SqrMagnitude (b.Point - p));
				});
			}

//			Debug.Log (JsonConvert.SerializeObject (IID.Select (a => new {Key = a.Key, vecList = a.Value.Select (b => new {P = b.Point.ToString (), ID = b.Id}).ToList ()}).ToList ()));


			// 生成完全定点处理链
			List<VertexHandleItem> VHIL = new List<VertexHandleItem> ();
			for (int i = 0; i < polygon.Count; i++) {
				Vector2 p = polygon [i];
				VHIL.Add (new VertexHandleItem () {
					Point = p,
					IsIntersection = false,
					closeFlag = false,
					xid = -1
				});

				if (IID.ContainsKey (i)) {
					VHIL.AddRange (IID [i].Select (a => new VertexHandleItem () {
						Point = a.Point,
						IsIntersection = true,
						closeFlag = false,
						xid = a.Id
					}).ToList ());
				}
			}

//			Debug.Log (JsonConvert.SerializeObject (VHIL.Select ((_, idx) => new {ID = idx, Point = _.Point.ToString (), _.IsIntersection, _.closeFlag, _.xid}).ToList ()));

			int m = 0;
			int n = 0;
//			bool loopFlag = true;
			int startIdx = -1;

			List< List<int> > resultIdxList = new List< List<int> > ();
			List<int> currentList;

			while (m != startIdx) {
				
				VertexHandleItem vhi_m = VHIL [m];

				if (vhi_m.closeFlag == false) {

					if (vhi_m.IsIntersection) {

						if (startIdx == -1) {
							startIdx = m;
						}

						currentList = new List<int> () { m };
						vhi_m.closeFlag = true;

						n = m + 1;
						if (n == VHIL.Count) {
							n = 0;
						}
							
						VertexHandleItem vhi_n;
						do {
							vhi_n = VHIL [n];

							if (vhi_n.IsIntersection) {
							
								if (vhi_n.xid == vhi_m.xid) {
									// 同一个点, 结束这个环
									resultIdxList.Add (currentList);

									break;
								} else {
									// 不同的点, 跳转到后面的同一个xid处继续

									int j = n + 1;
									if (j == VHIL.Count) {
										j = 0;
									}
									do {
										if (VHIL [j].xid == vhi_n.xid) {
											break;
										}
										j++;
										if (j == VHIL.Count) {
											j = 0;
										}

									} while(true);

									n = j;

									currentList.Add (n);
									vhi_n = VHIL [n];
									vhi_n.closeFlag = true;

								}

							} else {
								// 不是交点
								currentList.Add (n);
								vhi_n.closeFlag = true;


							}

							n++;
							if (n == VHIL.Count) {
								n = 0;
							}
						} while(true);

					}
				}


				m++;
				if (m == VHIL.Count) {
					m = 0;
				}
			}

//			Debug.Log (JsonConvert.SerializeObject (resultIdxList));

			// 整理出最终输出
			List< List<Vector2> > resultList = new List<List<Vector2>> ();

			foreach (List<int> innerList in resultIdxList) {
				List<Vector2> resultInnerList = new List<Vector2> ();
				foreach (int idx in innerList) {
					resultInnerList.Add (VHIL [idx].Point);
				}
				resultList.Add (resultInnerList);
			}

			return resultList;
		}

		public class VertexHandleItem
		{
			public Vector2 Point;
			public bool IsIntersection;

			// 处理完毕标记
			public bool closeFlag;
			public int xid;
		}

		public class VertexWithId
		{
			public Vector2 Point;
			public int Id;
		}

	}


}