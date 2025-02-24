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

	[Serializable]
	public class apTransform_Mesh
	{
		// Members
		//--------------------------------------------
		[SerializeField]
		public int _meshUniqueID = -1;

		[SerializeField]
		public int _transformUniqueID = -1;

		[SerializeField]
		public string _nickName = "";

		[NonSerialized]
		public apMesh _mesh = null;

		[SerializeField]
		public apMatrix _matrix = new apMatrix();//이건 기본 Static Matrix

		[SerializeField]
		public Color _meshColor2X_Default = new Color(0.5f, 0.5f, 0.5f, 1.0f);

		[SerializeField]
		public bool _isVisible_Default = true;



		[SerializeField]
		public int _depth = 0;

		[SerializeField]
		public int _level = 0;//Parent부터 내려오는 Level


		//Shader 정보
		[SerializeField]
		public apPortrait.SHADER_TYPE _shaderType = apPortrait.SHADER_TYPE.AlphaBlend;

		[SerializeField]
		public bool _isCustomShader = false;

		[SerializeField]
		public Shader _customShader = null;

		public enum RENDER_TEXTURE_SIZE
		{
			s_64, s_128, s_256, s_512, s_1024
		}

		[SerializeField]
		public RENDER_TEXTURE_SIZE _renderTexSize = RENDER_TEXTURE_SIZE.s_256;

		//추가 : Socket
		//Bake할때 소켓을 생성한다.
		[SerializeField]
		public bool _isSocket = false;


		//[SerializeField]
		//public Color _color = new Color(0.5f, 0.5f, 0.5f, 1.0f);//<<이걸 쓰는 곳이 없는데요?

		//계산용 변수
		///// <summary>Parent로부터 누적된 WorldMatrix. 자기 자신의 Matrix는 포함되지 않는다.</summary>
		//[NonSerialized]
		//public apMatrix3x3 _matrix_TF_Cal_Parent = apMatrix3x3.identity;

		////추가
		///// <summary>누적되지 않은 기본 Pivot Transform + Modifier 결과만 가지고 있는 값이다.</summary>
		//[NonSerialized]
		//public apMatrix3x3 _matrix_TF_Cal_Local = apMatrix3x3.identity;

		//World Transform을 구하기 위해선
		// World Transform = [Parent World] x [To Parent] x [Modified]

		[NonSerialized]
		public apMatrix _matrix_TF_ParentWorld = new apMatrix();

		[NonSerialized]
		public apMatrix _matrix_TF_ParentWorldWithoutMod = new apMatrix();

		[NonSerialized]
		public apMatrix _matrix_TF_ToParent = new apMatrix();

		[NonSerialized]
		public apMatrix _matrix_TF_LocalModified = new apMatrix();

		[NonSerialized]
		public apMatrix _matrix_TFResult_World = new apMatrix();

		[NonSerialized]
		public apMatrix _matrix_TFResult_WorldWithoutMod = new apMatrix();

		[NonSerialized]
		public apMatrix _invMatrix_TFResult_World = new apMatrix();

		[NonSerialized]
		public apMatrix _invMatrix_TFResult_WorldWithoutMod = new apMatrix();

		//Clipping Mask 관련
		//Child : -> 연결될 Parent MeshTransform을 저장한다. [여기서는 Depth는 Parent를 공유한다.]
		//Parent : -> 하위에 연결된 MeshTransform을 저장한다.
		[SerializeField]
		public bool _isClipping_Parent = false;

		[SerializeField]
		public bool _isClipping_Child = false;

		//Child Transform 일때 -> Parent 위주로 저장을 하자
		//public int _clipParentMeshTransformID = -1;
		[SerializeField]
		public int _clipIndexFromParent = -1;//렌더링 순서가 되는 Index : 1, 2, 3(1먼저 출력한다)

		[NonSerialized]
		public apTransform_Mesh _clipParentMeshTransform = null;

		//Parent Transform 일때
		[Serializable]
		public class ClipMeshSet
		{
			[SerializeField]
			public int _transformID = -1;

			[NonSerialized]
			public apTransform_Mesh _meshTransform = null;

			[NonSerialized]
			public apRenderUnit _renderUnit = null;

			/// <summary>
			/// 백업용 생성자
			/// </summary>
			public ClipMeshSet()
			{

			}

			public ClipMeshSet(int transformID)
			{
				_transformID = transformID;
				_meshTransform = null;
				_renderUnit = null;
			}

			public ClipMeshSet(apTransform_Mesh meshTransform, apRenderUnit renderUnit)
			{
				_transformID = meshTransform._transformUniqueID;
				_meshTransform = meshTransform;
				_renderUnit = renderUnit;
			}
		}

		[SerializeField]
		public List<ClipMeshSet> _clipChildMeshes = new List<ClipMeshSet>();



		[NonSerialized]
		public apRenderUnit _linkedRenderUnit = null;

		[SerializeField]
		public bool _isAlways2Side = false;//<<추가. 항상 양면 렌더링을 하는 경우

		//추가 9.25 : 그림자 옵션
		[SerializeField]
		public bool _isUsePortraitShadowOption = true;//<<그냥 apPortrait의 그림자 옵션을 따를 것인가
		[SerializeField]
		public apPortrait.SHADOW_CASTING_MODE _shadowCastingMode = apPortrait.SHADOW_CASTING_MODE.Off;
		[SerializeField]
		public bool _receiveShadow = false;

		
		//추가 19.6.9 : MaterialSet 옵션과 Property 초기화 옵션
		[SerializeField]
		public int _materialSetID = -1;

		[NonSerialized]
		public apMaterialSet _linkedMaterialSet = null;

		[SerializeField]
		public bool _isUseDefaultMaterialSet = true;//<<이게 True이면 MaterialSetID에 상관없이 Default 설정의 MatSet이 적용된다. 

		//Material Set을 사용하는 것과 별개로, 커스텀하게 Property를 설정할 수 있다.
		[Serializable]
		public class CustomMaterialProperty
		{
			[SerializeField]
			public string _name = "";

			public enum SHADER_PROP_TYPE
			{
				Float = 0, 
				Int = 1,
				Vector = 2,
				Texture = 3,
				Color = 4
			}

			[SerializeField]
			public SHADER_PROP_TYPE _propType = SHADER_PROP_TYPE.Float;

			[SerializeField]
			public float _value_Float = 0.0f;

			[SerializeField]
			public int _value_Int = 0;

			[SerializeField]
			public Vector4 _value_Vector = new Vector4(0, 0, 0, 0);

			[SerializeField]
			public Color _value_Color = new Color(0, 0, 0, 1);

			[SerializeField]
			public Texture _value_Texture = null;

			public CustomMaterialProperty()
			{

			}

			public void MakeEmpty()
			{
				_name = "<No Name>";
				_propType = SHADER_PROP_TYPE.Float;
				_value_Float = 0.0f;
				_value_Int = 0;
				_value_Vector = Vector4.zero;
				_value_Color = new Color(0, 0, 0, 1);
				_value_Texture = null;
			}

			public void CopyFromSrc(CustomMaterialProperty src)
			{
				_name = src._name;
				_propType = src._propType;
				_value_Float = src._value_Float;
				_value_Int = src._value_Int;
				_value_Vector = src._value_Vector;
				_value_Color = src._value_Color;
				_value_Texture = src._value_Texture;
			}
		}

		[SerializeField]
		public List<CustomMaterialProperty> _customMaterialProperties = new List<CustomMaterialProperty>();


		// Init
		//--------------------------------------------
		/// <summary>
		/// 백업용 생성자. 코드에서 호출하지 말자
		/// </summary>
		public apTransform_Mesh()
		{

		}

		public apTransform_Mesh(int uniqueID)
		{
			_transformUniqueID = uniqueID;
		}

		public void RegistIDToPortrait(apPortrait portrait)
		{
			portrait.RegistUniqueID(apIDManager.TARGET.Transform, _transformUniqueID);
		}


		// Functions
		//--------------------------------------------
		public void ReadyToCalculate()
		{
			_matrix.MakeMatrix();

			//변경
			//[Parent World x To Parent x Local TF] 조합으로 변경

			if (_matrix_TF_ParentWorld == null)					{ _matrix_TF_ParentWorld = new apMatrix(); }
			if (_matrix_TF_ParentWorldWithoutMod == null)		{ _matrix_TF_ParentWorldWithoutMod = new apMatrix(); }
			if (_matrix_TF_ToParent == null)					{ _matrix_TF_ToParent = new apMatrix(); }
			if (_matrix_TF_LocalModified == null)				{ _matrix_TF_LocalModified = new apMatrix(); }
			if (_matrix_TFResult_World == null)					{ _matrix_TFResult_World = new apMatrix(); }
			if (_matrix_TFResult_WorldWithoutMod == null)		{ _matrix_TFResult_WorldWithoutMod = new apMatrix(); }
			if (_invMatrix_TFResult_World == null)				{ _invMatrix_TFResult_World = new apMatrix(); }
			if (_invMatrix_TFResult_WorldWithoutMod == null)	{ _invMatrix_TFResult_WorldWithoutMod = new apMatrix(); }


			_matrix_TF_ParentWorld.SetIdentity();
			_matrix_TF_ParentWorldWithoutMod.SetIdentity();
			_matrix_TF_ToParent.SetIdentity();
			_matrix_TF_LocalModified.SetIdentity();

			//ToParent는 Pivot이므로 고정
			_matrix_TF_ToParent.SetMatrix(_matrix);

			_matrix_TFResult_World.SetIdentity();
			_matrix_TFResult_WorldWithoutMod.SetIdentity();
			_invMatrix_TFResult_World.SetIdentity();
			_invMatrix_TFResult_WorldWithoutMod.SetIdentity();

			//CalculatedLog.ReadyToRecord();

		}


		//public void SetModifiedTransform(apMatrix matrix_modified, apCalculatedLog calResultStack_CalLog)
		public void SetModifiedTransform(apMatrix matrix_modified)
		{
			_matrix_TF_LocalModified.SetMatrix(matrix_modified);
			//CalculatedLog.LinkLog_CalculateResultStackTF(calResultStack_CalLog);
		}

		/// <summary>
		/// Parent의 Matrix를 추가한다. (Parent x This)
		/// </summary>
		/// <param name="matrix_parentTransform"></param>
		//public void AddWorldMatrix_Parent(apMatrix3x3 matrix_parentTransform)
		public void AddWorldMatrix_Parent(apMatrix matrix_parentTransform, apMatrix matrix_parentTransformNoMod)
		{
			_matrix_TF_ParentWorld.SetMatrix(matrix_parentTransform);
			_matrix_TF_ParentWorldWithoutMod.SetMatrix(matrix_parentTransformNoMod);
		}

		public void MakeTransformMatrix()
		{
			//[R]
			_matrix_TFResult_World.RMultiply(_matrix_TF_ToParent);
			_matrix_TFResult_World.RMultiply(_matrix_TF_LocalModified);
			_matrix_TFResult_World.RMultiply(_matrix_TF_ParentWorld);

			_matrix_TFResult_WorldWithoutMod.RMultiply(_matrix_TF_ToParent);
			_matrix_TFResult_WorldWithoutMod.RMultiply(_matrix_TF_ParentWorld);

			//Inverse는 반대로 계산한다.
			_invMatrix_TFResult_World.SetMatrix(_matrix_TF_ParentWorld);
			_invMatrix_TFResult_World.RInverse(_matrix_TF_LocalModified);
			_invMatrix_TFResult_World.RInverse(_matrix_TF_ToParent);

			_invMatrix_TFResult_WorldWithoutMod.SetMatrix(_matrix_TF_ParentWorldWithoutMod);
			_invMatrix_TFResult_WorldWithoutMod.RInverse(_matrix_TF_ToParent);
		}



		// Clip 관련 코드
		//--------------------------------------------
		/// <summary>
		/// Parent 로서의 Clipping 세팅을 초기화한다. (Child일땐 초기화되지 않는다.)
		/// </summary>
		public void InitClipMeshAsParent()
		{
			_isClipping_Parent = false;
			if (_clipChildMeshes == null)
			{
				_clipChildMeshes = new List<ClipMeshSet>();
			}
			_clipChildMeshes.Clear();


			//미사용 코드
			//for (int i = 0; i < 3; i++)
			//{
			//	_clipChildMeshTransformIDs[i] = -1;
			//	_clipChildMeshTransforms[i] = null;
			//	_clipChildRenderUnits[i] = null;
			//}
		}


		private class RenderUnitTransformMeshSet
		{
			public apTransform_Mesh _meshTransform = null;
			public apRenderUnit _renderUnit = null;
			public RenderUnitTransformMeshSet(apTransform_Mesh meshTransform, apRenderUnit renderUnit)
			{
				_meshTransform = meshTransform;
				_renderUnit = renderUnit;
			}
		}
		public void SortClipMeshTransforms()
		{
			if (_isClipping_Parent)
			{
				List<RenderUnitTransformMeshSet> childList = new List<RenderUnitTransformMeshSet>();
				//for (int i = 0; i < _clipChildMeshTransforms.Count; i++)
				//{
				//	if (_clipChildMeshTransforms[i] != null)
				//	{
				//		if (!childList.Exists(delegate (RenderUnitTransformMeshSet a)
				//		{
				//			return a._meshTransform == _clipChildMeshTransforms[i];
				//		}))
				//		{
				//			childList.Add(
				//				new RenderUnitTransformMeshSet(_clipChildMeshTransforms[i],
				//												_clipChildRenderUnits[i]));
				//		}
				//	}
				//}
				for (int i = 0; i < _clipChildMeshes.Count; i++)
				{
					if (_clipChildMeshes[i]._meshTransform != null)
					{
						if (!childList.Exists(delegate (RenderUnitTransformMeshSet a)
						{
							return a._meshTransform == _clipChildMeshes[i]._meshTransform;
						}))
						{
							childList.Add(
								new RenderUnitTransformMeshSet(_clipChildMeshes[i]._meshTransform,
																_clipChildMeshes[i]._renderUnit));
						}
					}
				}

				childList.Sort(delegate (RenderUnitTransformMeshSet a, RenderUnitTransformMeshSet b)
				{
				//Depth의 오름차순
				return a._meshTransform._depth - b._meshTransform._depth;
				});

				if (childList.Count == 0)
				{
					_clipChildMeshes.Clear();

					//for (int i = 0; i < 3; i++)
					//{
					//	_clipChildMeshTransforms[i] = null;
					//	_clipChildMeshTransformIDs[i] = -1;
					//	_clipChildRenderUnits[i] = null;
					//}
					_isClipping_Parent = false;
				}
				else
				{
					_clipChildMeshes.Clear();

					//리스트 순서대로 다시 재배치하자
					for (int i = 0; i < childList.Count; i++)
					{
						_clipChildMeshes.Add(new ClipMeshSet(childList[i]._meshTransform, childList[i]._renderUnit));
						childList[i]._meshTransform._clipParentMeshTransform = this;
						childList[i]._meshTransform._clipIndexFromParent = i;
						childList[i]._meshTransform._isClipping_Child = true;
					}

					#region [미사용 코드]
					//이전 코드
					//for (int i = 0; i < 3; i++)
					//{
					//	if (i < childList.Count)
					//	{
					//		_clipChildMeshTransforms[i] = childList[i]._meshTransform;
					//		_clipChildMeshTransformIDs[i] = childList[i]._meshTransform._transformUniqueID;
					//		_clipChildRenderUnits[i] = childList[i]._renderUnit;

					//		_clipChildMeshTransforms[i]._isClipping_Child = true;
					//		_clipChildMeshTransforms[i]._clipIndexFromParent = i;
					//		_clipChildMeshTransforms[i]._clipParentMeshTransform = this;
					//	}
					//	else
					//	{
					//		_clipChildMeshTransforms[i] = null;
					//		_clipChildMeshTransformIDs[i] = -1;
					//		_clipChildRenderUnits[i] = null;
					//	}
					//} 
					#endregion
				}
			}
		}

		public int GetChildClippedMeshes()
		{
			if (!_isClipping_Parent)
			{
				return -1;
			}
			return _clipChildMeshes.Count;
			//int nID = 0;
			//for (int i = 0; i < 3; i++)
			//{
			//	if(_clipChildMeshTransformIDs[i] >= 0)
			//	{
			//		nID++;
			//	}
			//}
			//return nID;
		}

		public void AddClippedChildMesh(apTransform_Mesh meshTransform, apRenderUnit renderUnit)
		{
			_isClipping_Parent = true;

			if (_clipChildMeshes.Exists(delegate (ClipMeshSet a)
			 {
				 return a._meshTransform == meshTransform;
			 }))
			{
				//겹친다면 Pass
				SortClipMeshTransforms();
				return;
			}

			int clippIndex = _clipChildMeshes.Count;
			_clipChildMeshes.Add(new ClipMeshSet(meshTransform, renderUnit));
			meshTransform._isClipping_Child = true;
			meshTransform._clipIndexFromParent = clippIndex;

			#region [미사용 코드]
			//_clipChildMeshTransformIDs.Add(meshTransform._transformUniqueID);
			//_clipChildMeshTransforms.Add(meshTransform);
			//_clipChildRenderUnits.Add(renderUnit);

			//이전 코드 : 3개 고정
			//for (int i = 0; i < 3; i++)
			//{
			//	if(_clipChildMeshTransforms[i] == null)
			//	{
			//		_clipChildMeshTransforms[i] = meshTransform;
			//		_clipChildMeshTransformIDs[i] = meshTransform._transformUniqueID;
			//		_clipChildRenderUnits[i] = renderUnit;
			//		break;
			//	}
			//} 
			#endregion

			SortClipMeshTransforms();
		}

		// Get / Set
		//--------------------------------------------
	}

}