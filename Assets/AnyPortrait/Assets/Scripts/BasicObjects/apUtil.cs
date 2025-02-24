﻿/*
*	Copyright (c) 2017-2020. RainyRizzle. All rights reserved
*	Contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of [AnyPortrait].
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of [Seungjik Lee].
*
*	Unless this file is downloaded from the Unity Asset Store or RainyRizzle homepage, 
*	this file and its users are illegal.
*	In that case, the act may be subject to legal penalties.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using AnyPortrait;

namespace AnyPortrait
{

	public static class apUtil
	{

		public static List<T> ResizeList<T>(List<T> srcList, int resizeSize)
		{
			if (resizeSize < 0)
			{
				return null;
			}
			List<T> resultList = new List<T>();
			for (int i = 0; i < resizeSize; i++)
			{
				if (i < srcList.Count)
				{
					resultList.Add(srcList[i]);
				}
				else
				{
					resultList.Add(default(T));
				}
			}

			return resultList;

		}

		// 색상 처리
		//------------------------------------------------------------------------------------------
		public static Color BlendColor_ITP(Color prevResult, Color nextResult, float nextWeight)
		{
			return (prevResult * (1.0f - nextWeight)) + (nextResult * nextWeight);
		}

		//public static Vector3 _color_2XTmp_Prev = new Vector3(0, 0, 0);
		//public static Vector3 _color_2XTmp_Next = new Vector3(0, 0, 0);

		public static Color BlendColor_Add(Color prevResult, Color nextResult, float nextWeight)
		{
			//_color_2XTmp_Prev.x = (float)(prevResult.r);
			//_color_2XTmp_Prev.y = (float)(prevResult.g);
			//_color_2XTmp_Prev.z = (float)(prevResult.b);

			//_color_2XTmp_Next.x = (float)(nextResult.r);
			//_color_2XTmp_Next.y = (float)(nextResult.g);
			//_color_2XTmp_Next.z = (float)(nextResult.b);

			//_color_2XTmp_Prev += (_color_2XTmp_Next * nextWeight);
			//_color_2XTmp_Next = _color_2XTmp_Prev * (1.0f - nextWeight) + ((_color_2XTmp_Prev + _color_2XTmp_Next) * nextWeight);



			//return new Color(	Mathf.Clamp01(_color_2XTmp_Prev.x + 0.5f),
			//					Mathf.Clamp01(_color_2XTmp_Prev.y + 0.5f),
			//					Mathf.Clamp01(_color_2XTmp_Prev.z + 0.5f),
			//					//Mathf.Clamp01(prevResult.a + (nextResult.a * nextWeight))
			//					Mathf.Clamp01(prevResult.a * (1.0f - nextWeight) + (prevResult.a * nextResult.a) * nextWeight)
			//				);

			//return new Color(	Mathf.Clamp01(_color_2XTmp_Next.x),
			//					Mathf.Clamp01(_color_2XTmp_Next.y),
			//					Mathf.Clamp01(_color_2XTmp_Next.z),
			//					//Mathf.Clamp01(prevResult.a + (nextResult.a * nextWeight))
			//					Mathf.Clamp01(prevResult.a * (1.0f - nextWeight) + (prevResult.a * nextResult.a) * nextWeight)
			//				);

			//return prevResult + (nextResult * nextWeight);

			nextResult.r = prevResult.r * (1.0f - nextWeight) + (Mathf.Clamp01(prevResult.r + nextResult.r - 0.5f) * nextWeight);
			nextResult.g = prevResult.g * (1.0f - nextWeight) + (Mathf.Clamp01(prevResult.g + nextResult.g - 0.5f) * nextWeight);
			nextResult.b = prevResult.b * (1.0f - nextWeight) + (Mathf.Clamp01(prevResult.b + nextResult.b - 0.5f) * nextWeight);
			//nextResult.a = prevResult.a * (1.0f - nextWeight) + (Mathf.Clamp01(prevResult.a + nextResult.a - 0.5f) * nextWeight);
			nextResult.a = prevResult.a * (1.0f - nextWeight) + (Mathf.Clamp01(prevResult.a * nextResult.a) * nextWeight);//Alpha는 Multiply 연산



			return nextResult;
		}


		//--------------------------------------------------------------------------------------------
		public static float AngleTo180(float angle)
		{
			while(angle > 180.0f)
			{
				angle -= 360.0f;
			}
			while(angle < -180.0f)
			{
				angle += 360.0f;
			}
			return angle;
		}

		public static float AngleTo360(float angle)
		{
			while(angle > 360.0f)
			{
				angle -= 360.0f;
			}
			while(angle < -360.0f)
			{
				angle += 360.0f;
			}
			return angle;
		}

		/// <summary>
		/// 360 좌표계의 원형 보간을 구한다. Weight가 0이면 angleA, Weight가 1이면 angleB가 리턴된다.
		/// 리턴되는 값은 +- 180 이내의 값으로 변환된다.
		/// </summary>
		/// <param name="angleA"></param>
		/// <param name="angleB"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public static float AngleSlerp(float angleA, float angleB, float weight)
		{
			angleA = AngleTo180(angleA);
			angleB = AngleTo180(angleB);
			if(angleA > angleB)
			{
				if(angleA > angleB + 180)
				{
					angleB += 360;
				}
			}
			else
			{
				if(angleB > angleA + 180)
				{
					angleA += 360;
				}
			}

			return AngleTo180(angleA * (1.0f - weight) + angleB * weight);
		}


		//---------------------------------------------------------------------------------------------------
		//문자열 압축
		public static string GetShortString(string strSrc, int length)
		{
			if(string.IsNullOrEmpty(strSrc))
			{
				return "";
			}

			if(strSrc.Length > length)
			{
				return strSrc.Substring(0, length) + "..";
			}
			return strSrc;
		}


		//추가 20.4.3 : 갱신 요청에 관련된 변수를 별도로 만든다.
		//---------------------------------------------------------------------------------------------------
		//public enum LINK_REFRESH_REQUEST_TYPE
		//{
		//	AllObjects,
		//	MeshGroup_AllAnimMods,
		//	MeshGroup_ExceptAnimMods,
		//	AnimClip,
		//}

		public enum LR_REQUEST__MESHGROUP
		{
			/// <summary>메시 그룹에 상관 없음 또는 모든 메시 그룹. 단 메시 그룹을 입력하면 RenderUnit을 갱신한다.</summary>
			AllMeshGroups,
			/// <summary>선택된 메시 그룹 1개만</summary>
			SelectedMeshGroup,
		}

		public enum LR_REQUEST__MODIFIER
		{
			/// <summary>모든 모디파이어</summary>
			AllModifiers,
			/// <summary>선택된 모디파이어만</summary>
			SelectedModifier,
			/// <summary>애니메이션 모디파이어는 제외. (메시 그룹 메뉴용)</summary>
			AllModifiers_ExceptAnimMods,
		}

		public enum LR_REQUEST__PSG
		{
			/// <summary>모든 ParamSetGroup들</summary>
			AllPSGs,
			/// <summary>
			/// (애니메이션 모디파이어인 경우) 선택된 애니메이션 클립에 대한 PSG만. 
			/// 그 외의 모디파이어는 모든 PSG를 대상 (애니메이션 메뉴용)
			/// </summary>
			SelectedAnimClipPSG_IfAnimModifier
		}

		public class LinkRefreshRequest
		{
			//Members
			private LR_REQUEST__MESHGROUP _request_MeshGroup = LR_REQUEST__MESHGROUP.AllMeshGroups;
			private apMeshGroup _meshGroup = null;

			private LR_REQUEST__MODIFIER _request_Modifier = LR_REQUEST__MODIFIER.AllModifiers;
			private apModifierBase _modifier = null;

			private LR_REQUEST__PSG _request_PSG = LR_REQUEST__PSG.AllPSGs;
			private apAnimClip _animClip = null;

			//Get
			public LR_REQUEST__MESHGROUP Request_MeshGroup { get { return _request_MeshGroup; } }
			public apMeshGroup MeshGroup { get { return _meshGroup; } }

			public LR_REQUEST__MODIFIER Request_Modifier { get { return _request_Modifier; } }
			public apModifierBase Modifier { get { return _modifier; } }

			public LR_REQUEST__PSG Request_PSG {  get { return _request_PSG; } }
			public apAnimClip AnimClip { get { return _animClip; } }


			public override string ToString()
			{
				return _request_MeshGroup.ToString() + " / " + _request_Modifier.ToString() + " / " + _request_PSG.ToString();
			}

			//public bool IsLinkAllObjects { get { return _requestType == LINK_REFRESH_REQUEST_TYPE.AllObjects; } }
			//public bool IsSkipAllAnimModifiers { get { return _requestType == LINK_REFRESH_REQUEST_TYPE.MeshGroup_ExceptAnimMods; } }
			//public bool IsSkipUnselectedAnimPSGs {  get { return _requestType == LINK_REFRESH_REQUEST_TYPE.AnimClip; } }

			//Init
			public LinkRefreshRequest()
			{
				Set_AllObjects(null);
			}

			//Functions
			/// <summary>
			/// 모든 메시 그룹과 모든 모디파이어에 대해서 Link. (주의 : 오래 걸림)
			/// 메시 그룹을 인자로 넣으면 해당 메시 그룹은 RenderUnit을 갱신한다.
			/// </summary>
			public LinkRefreshRequest Set_AllObjects(apMeshGroup curSelectedMeshGroup)
			{
				_request_MeshGroup = LR_REQUEST__MESHGROUP.AllMeshGroups;
				_meshGroup = curSelectedMeshGroup;

				_request_Modifier = LR_REQUEST__MODIFIER.AllModifiers;
				_modifier = null;

				_request_PSG = LR_REQUEST__PSG.AllPSGs;
				_animClip = null;
				return this;
			}

			/// <summary>
			/// 선택된 메시 그룹과, 메시 그룹의 모든 모디파이어, PSG에 대해 Link 
			/// </summary>
			public LinkRefreshRequest Set_MeshGroup_AllModifiers(apMeshGroup meshGroup)
			{
				if(meshGroup == null)
				{	
					return Set_AllObjects(null);
				}
				_request_MeshGroup = LR_REQUEST__MESHGROUP.SelectedMeshGroup;
				_meshGroup = meshGroup;

				_request_Modifier = LR_REQUEST__MODIFIER.AllModifiers;
				_modifier = null;

				_request_PSG = LR_REQUEST__PSG.AllPSGs;
				_animClip = null;
				return this;
			}

			/// <summary>
			/// 선택된 메시 그룹과 특정 모디파이어만 Link (PSG는 상관 없음) (메시 그룹 메뉴에서 특정 모디파이어 편집용)
			/// </summary>
			public LinkRefreshRequest Set_MeshGroup_Modifier(apMeshGroup meshGroup, apModifierBase modifier)
			{
				_request_MeshGroup = LR_REQUEST__MESHGROUP.SelectedMeshGroup;
				_meshGroup = meshGroup;

				_request_Modifier = LR_REQUEST__MODIFIER.SelectedModifier;
				_modifier = modifier;

				_request_PSG = LR_REQUEST__PSG.AllPSGs;
				_animClip = null;
				return this;
			}

			/// <summary>
			/// 선택된 메시 그룹과 Anim 모디파이어를 제외한 모든 모디파이어 (메시 그룹 메뉴 편집용)
			/// </summary>
			/// <param name="meshGroup"></param>
			/// <returns></returns>
			public LinkRefreshRequest Set_MeshGroup_ExceptAnimModifiers(apMeshGroup meshGroup)
			{
				_request_MeshGroup = LR_REQUEST__MESHGROUP.SelectedMeshGroup;
				_meshGroup = meshGroup;

				_request_Modifier = LR_REQUEST__MODIFIER.AllModifiers_ExceptAnimMods;
				_modifier = null;

				_request_PSG = LR_REQUEST__PSG.AllPSGs;
				_animClip = null;
				return this;
			}

			/// <summary>
			/// 선택된 애니메이션 클립과 이 애니메이션 클립에서 실행되는 PSG 및 Static-모디파이어들 (애니메이션 편집용)
			/// </summary>
			public LinkRefreshRequest Set_AnimClip(apAnimClip animClip)
			{
				_request_MeshGroup = LR_REQUEST__MESHGROUP.SelectedMeshGroup;
				_meshGroup = _animClip != null ? _animClip._targetMeshGroup : null;

				_request_Modifier = LR_REQUEST__MODIFIER.AllModifiers;
				_modifier = null;

				_request_PSG = LR_REQUEST__PSG.SelectedAnimClipPSG_IfAnimModifier;
				_animClip = animClip;
				return this;
			}
		}

		private static LinkRefreshRequest _linkRefreshRequest = new LinkRefreshRequest();
		/// <summary>
		/// 에디터에서 Link나 Refresh 함수를 쓸 때, 불필요한 객체로의 접근을 막는 요청을 위한 변수.
		/// </summary>
		public static LinkRefreshRequest LinkRefresh
		{
			get
			{
				if (_linkRefreshRequest == null) { _linkRefreshRequest = new LinkRefreshRequest(); }
				return _linkRefreshRequest;
			}
		}
	}

	

	/// <summary>
	/// 이 Attribute가 있다면 SerializedField라도 백업 대상에서 제외된다.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.All)]
	public class NonBackupField : System.Attribute
	{
		
	}

	/// <summary>
	/// 이 Attribute가 있다면 백업 시에 특정 값을 저장할 수 있다.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.All)]
	public class CustomBackupField : System.Attribute
	{
		private string _name;
		public string Name {  get { return _name; } }
		public CustomBackupField(string strName)
		{
			_name = strName;
		}
	}


	
	
	
}