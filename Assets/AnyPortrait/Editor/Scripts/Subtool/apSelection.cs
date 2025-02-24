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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnyPortrait
{

	/// <summary>
	/// Editor
	/// </summary>
	public class apSelection
	{
		// Members
		//-------------------------------------
		public apEditor _editor = null;
		public apEditor Editor { get { return _editor; } }

		public enum SELECTION_TYPE
		{
			None,
			ImageRes,
			Mesh,
			Face,
			MeshGroup,
			Animation,
			Overall,
			Param
		}

		private SELECTION_TYPE _selectionType = SELECTION_TYPE.None;

		public SELECTION_TYPE SelectionType { get { return _selectionType; } }

		private apPortrait _portrait = null;
		private apRootUnit _rootUnit = null;
		private apTextureData _image = null;
		private apMesh _mesh = null;
		private apMeshGroup _meshGroup = null;
		private apControlParam _param = null;
		private apAnimClip _animClip = null;

		//Overall 선택시, 선택가능한 AnimClip 리스트
		private List<apAnimClip> _rootUnitAnimClips = new List<apAnimClip>();
		private apAnimClip _curRootUnitAnimClip = null;


		//Texture 선택시
		private Texture2D _imageImported = null;
		private TextureImporter _imageImporter = null;

		//Anim Clip 내에서 단일 선택시
		private apAnimTimeline _subAnimTimeline = null;//<<타임라인 단일 선택시
		private apAnimTimelineLayer _subAnimTimelineLayer = null;//타임 라인의 레이어 단일 선택시
		private apAnimKeyframe _subAnimKeyframe = null;//단일 선택한 키프레임
		private apAnimKeyframe _subAnimWorkKeyframe = null;//<<자동으로 선택되는 키프레임이다. "현재 프레임"에 위치한 "레이어의 프레임"이다.
		private bool _isAnimTimelineLayerGUIScrollRequest = false;


		private List<apAnimKeyframe> _subAnimKeyframeList = new List<apAnimKeyframe>();//여러개의 키프레임을 선택한 경우 (주로 복불/이동 할때)
		private EX_EDIT _exAnimEditingMode = EX_EDIT.None;//<애니메이션 수정 작업을 하고 있는가
		public EX_EDIT ExAnimEditingMode { get { if (IsAnimEditable) { return _exAnimEditingMode; } return EX_EDIT.None; } }

		//레이어에 상관없이 키프레임을 관리하고자 하는 경우
		//Common 선택 -> 각각의 Keyframe 선택 (O)
		//각각의 Keyframe 선택 -> Common 선택 (X)
		//각각의 Keyframe 선택 -> 해당 FrameIndex의 모든 Keyframe이 선택되었는지 확인 -> Common 선택 (O)
		private List<apAnimCommonKeyframe> _subAnimCommonKeyframeList = new List<apAnimCommonKeyframe>();
		private List<apAnimCommonKeyframe> _subAnimCommonKeyframeList_Selected = new List<apAnimCommonKeyframe>();//<<선택된 Keyframe만 따로 표시한다.

		public List<apAnimCommonKeyframe> AnimCommonKeyframes { get { return _subAnimCommonKeyframeList; } }
		public List<apAnimCommonKeyframe> AnimCommonKeyframes_Selected { get { return _subAnimCommonKeyframeList_Selected; } }

		//추가 3.30 : 키프레임들을 동시에 편집하고자 하는 경우
		public apTimelineCommonCurve _animTimelineCommonCurve = new apTimelineCommonCurve();


		//애니메이션 선택 잠금
		private bool _isAnimSelectionLock = false;


		private apTransform_Mesh _subMeshTransformOnAnimClip = null;
		private apTransform_MeshGroup _subMeshGroupTransformOnAnimClip = null;
		private apControlParam _subControlParamOnAnimClip = null;

		//AnimClip에서 ModMesh를 선택하고 Vertex 수정시
		private apModifiedMesh _modMeshOfAnim = null;
		private apRenderUnit _renderUnitOfAnim = null;
		private ModRenderVert _modRenderVertOfAnim = null;
		private List<ModRenderVert> _modRenderVertListOfAnim = new List<ModRenderVert>();//<<1개만 선택해도 리스트엔 들어가있다.
		private List<ModRenderVert> _modRenderVertListOfAnim_Weighted = new List<ModRenderVert>();//<<Soft Selection, Blur, Volume 등에 포함되는 "Weight가 포함된 리스트"
		private apModifiedBone _modBoneOfAnim = null;


		/// <summary>애니메이션 수정 작업이 가능한가?</summary>
		private bool IsAnimEditable
		{
			get
			{
				if (_selectionType != SELECTION_TYPE.Animation || _animClip == null || _subAnimTimeline == null)
				{
					return false;
				}
				if (_animClip._targetMeshGroup == null)
				{
					return false;
				}
				return true;
			}
		}
		public bool IsAnimPlaying
		{
			get
			{
				if (AnimClip == null)
				{
					return false;
				}
				return AnimClip.IsPlaying_Editor;
			}
		}




		public apModifiedMesh ModMeshOfAnim { get { if (_selectionType == SELECTION_TYPE.Animation) { return _modMeshOfAnim; } return null; } }
		public apModifiedBone ModBoneOfAnim { get { if (_selectionType == SELECTION_TYPE.Animation) { return _modBoneOfAnim; } return null; } }
		public apRenderUnit RenderUnitOfAnim { get { if (_selectionType == SELECTION_TYPE.Animation) { return _renderUnitOfAnim; } return null; } }
		public ModRenderVert ModRenderVertOfAnim { get { if (_selectionType == SELECTION_TYPE.Animation) { return _modRenderVertOfAnim; } return null; } }
		public List<ModRenderVert> ModRenderVertListOfAnim { get { if (_selectionType == SELECTION_TYPE.Animation) { return _modRenderVertListOfAnim; } return null; } }
		public List<ModRenderVert> ModRenderVertListOfAnim_Weighted { get { if (_selectionType == SELECTION_TYPE.Animation) { return _modRenderVertListOfAnim_Weighted; } return null; } }

		public Vector2 ModRenderVertsCenterPosOfAnim
		{
			get
			{
				if (_selectionType != SELECTION_TYPE.Animation || _modRenderVertListOfAnim.Count == 0)
				{
					return Vector2.zero;
				}
				Vector2 centerPos = Vector2.zero;
				for (int i = 0; i < _modRenderVertListOfAnim.Count; i++)
				{
					centerPos += _modRenderVertListOfAnim[i]._renderVert._pos_World;
				}
				centerPos /= _modRenderVertListOfAnim.Count;
				return centerPos;
			}
		}


		//Bone


		private apTransform_Mesh _subMeshTransformInGroup = null;
		private apTransform_MeshGroup _subMeshGroupTransformInGroup = null;

		private apModifierBase _modifier = null;

		//Modifier 작업시 선택하는 객체들
		private apModifierParamSet _paramSetOfMod = null;


		private apModifiedMesh _modMeshOfMod = null;
		private apModifiedBone _modBoneOfMod = null;//<추가
		private apRenderUnit _renderUnitOfMod = null;

		//추가
		//modBone으로 등록 가능한 apBone 리스트
		private List<apBone> _modRegistableBones = new List<apBone>();
		public List<apBone> ModRegistableBones { get { return _modRegistableBones; } }

		//Mod Vert와 Render Vert는 동시에 선택이 된다.
		public class ModRenderVert
		{
			public apModifiedVertex _modVert = null;
			public apRenderVertex _renderVert = null;
			//추가
			//ModVert가 아니라 ModVertRig가 매칭되는 경우도 있다.
			//Gizmo에서 주로 사용하는데 에러 안나게 주의할 것
			public apModifiedVertexRig _modVertRig = null;

			public apModifiedVertexWeight _modVertWeight = null;


			/// <summary>
			/// SoftSelection, Blur, Volume등의 "편집 과정에서의 Weight"를 임시로 결정하는 경우의 값
			/// </summary>
			public float _vertWeightByTool = 1.0f;

			public ModRenderVert(apModifiedVertex modVert, apRenderVertex renderVert)
			{
				_modVert = modVert;
				_modVertRig = null;
				_modVertWeight = null;

				_renderVert = renderVert;
				_vertWeightByTool = 1.0f;

			}

			public ModRenderVert(apModifiedVertexRig modVertRig, apRenderVertex renderVert)
			{
				_modVert = null;
				_modVertRig = modVertRig;
				_modVertWeight = null;

				_renderVert = renderVert;
				_vertWeightByTool = 1.0f;
			}

			public ModRenderVert(apModifiedVertexWeight modVertWeight, apRenderVertex renderVert)
			{
				_modVert = null;
				_modVertRig = null;
				_modVertWeight = modVertWeight;

				_renderVert = renderVert;
				_vertWeightByTool = _modVertWeight._weight;//<<이건 갱신해야할 것
			}

			//다음 World 좌표값을 받아서 ModifiedVertex의 값을 수정하자
			public void SetWorldPosToModifier_VertLocal(Vector2 nextWorldPos)
			{
				//NextWorld Pos에서 -> [VertWorld] -> [MeshTransform] -> Vert Local 적용 후의 좌표 -> Vert Local 적용 전의 좌표 
				//적용 전-후의 좌표 비교 = 그 차이값을 ModVert에 넣자
				apMatrix3x3 matToAfterVertLocal = (_renderVert._matrix_Cal_VertWorld * _renderVert._matrix_MeshTransform).inverse;
				Vector2 nextLocalMorphedPos = matToAfterVertLocal.MultiplyPoint(nextWorldPos);
				Vector2 beforeLocalMorphedPos = (_renderVert._matrix_Cal_VertLocal * _renderVert._matrix_Static_Vert2Mesh).MultiplyPoint(_renderVert._vertex._pos);

				_modVert._deltaPos.x += (nextLocalMorphedPos.x - beforeLocalMorphedPos.x);
				_modVert._deltaPos.y += (nextLocalMorphedPos.y - beforeLocalMorphedPos.y);
			}
		}

		//버텍스에 대해서
		//단일 선택일때
		//복수개의 선택일때
		private ModRenderVert _modRenderVertOfMod = null;
		private List<ModRenderVert> _modRenderVertListOfMod = new List<ModRenderVert>();//<<1개만 선택해도 리스트엔 들어가있다.
		private List<ModRenderVert> _modRenderVertListOfMod_Weighted = new List<ModRenderVert>();//<<Soft Selection, Blur, Volume 등에 포함되는 "Weight가 포함된 리스트"


		//메시/메시그룹 트랜스폼에 대해서
		//복수 선택도 가능하게 해주자
		private List<apTransform_Mesh> _subMeshTransformListInGroup = new List<apTransform_Mesh>();
		private List<apTransform_MeshGroup> _subMeshGroupTransformListInGroup = new List<apTransform_MeshGroup>();



		/// <summary>Modifier에서 현재 선택중인 ParamSetGroup [주의 : Animated Modifier에서는 이 값을 사용하지 말고 다른 값을 사용해야한다]</summary>
		private apModifierParamSetGroup _subEditedParamSetGroup = null;

		/// <summary>Animated Modifier에서 현재 선택중인 ParamSetGroup의 Pack. [주의 : Animataed Modifier에서만 사용가능하다]</summary>
		private apModifierParamSetGroupAnimPack _subEditedParamSetGroupAnimPack = null;


		public apPortrait Portrait { get { return _portrait; } }

		public apRootUnit RootUnit { get { if (_selectionType == SELECTION_TYPE.Overall && _portrait != null) { return _rootUnit; } return null; } }
		public List<apAnimClip> RootUnitAnimClipList { get { if (_selectionType == SELECTION_TYPE.Overall && _portrait != null) { return _rootUnitAnimClips; } return null; } }
		public apAnimClip RootUnitAnimClip { get { if (_selectionType == SELECTION_TYPE.Overall && _portrait != null) { return _curRootUnitAnimClip; } return null; } }


		public apTextureData TextureData { get { if (_selectionType == SELECTION_TYPE.ImageRes) { return _image; } return null; } }
		public apMesh Mesh { get { if (_selectionType == SELECTION_TYPE.Mesh) { return _mesh; } return null; } }
		public apMeshGroup MeshGroup { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _meshGroup; } return null; } }
		public apControlParam Param { get { if (_selectionType == SELECTION_TYPE.Param) { return _param; } return null; } }
		public apAnimClip AnimClip { get { if (_selectionType == SELECTION_TYPE.Animation) { return _animClip; } return null; } }

		//Mesh Group에서 서브 선택
		//Mesh/MeshGroup Transform
		public apTransform_Mesh SubMeshInGroup { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _subMeshTransformInGroup; } return null; } }
		public apTransform_MeshGroup SubMeshGroupInGroup { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _subMeshGroupTransformInGroup; } return null; } }

		//ParamSetGroup / ParamSetGroupAnimPack
		/// <summary>Modifier에서 현재 선택중인 ParamSetGroup [주의 : Animated Modifier에서는 이 값을 사용하지 말고 다른 값을 사용해야한다]</summary>
		public apModifierParamSetGroup SubEditedParamSetGroup { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _subEditedParamSetGroup; } return null; } }
		/// <summary>Animated Modifier에서 현재 선택중인 ParamSetGroup의 Pack. [주의 : Animataed Modifier에서만 사용가능하다]</summary>
		public apModifierParamSetGroupAnimPack SubEditedParamSetGroupAnimPack { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _subEditedParamSetGroupAnimPack; } return null; } }


		//MeshGroup Setting에서 Pivot을 바꿀 때
		private bool _isMeshGroupSetting_ChangePivot = false;
		public bool IsMeshGroupSettingChangePivot { get { return _isMeshGroupSetting_ChangePivot; } }

		//현재 선택된 Modifier
		public apModifierBase Modifier { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _modifier; } return null; } }

		//Modifier 작업식 선택하는 객체들
		public apModifierParamSet ParamSetOfMod { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _paramSetOfMod; } return null; } }
		public apModifiedMesh ModMeshOfMod { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _modMeshOfMod; } return null; } }

		public apModifiedBone ModBoneOfMod { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _modBoneOfMod; } return null; } }

		public apRenderUnit RenderUnitOfMod { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _renderUnitOfMod; } return null; } }

		//public apModifiedVertex ModVertOfMod { get { if(_selectionType == SELECTION_TYPE.MeshGroup) { return _modVertOfMod; } return null; } }
		//public apRenderVertex RenderVertOfMod { get { if(_selectionType == SELECTION_TYPE.MeshGroup) { return _renderVertOfMod; } return null; } }
		public ModRenderVert ModRenderVertOfMod { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _modRenderVertOfMod; } return null; } }
		public List<ModRenderVert> ModRenderVertListOfMod { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _modRenderVertListOfMod; } return null; } }
		public List<ModRenderVert> ModRenderVertListOfMod_Weighted { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _modRenderVertListOfMod_Weighted; } return null; } }

		public Vector2 ModRenderVertsCenterPosOfMod
		{
			get
			{
				if (_selectionType != SELECTION_TYPE.MeshGroup || _modRenderVertListOfMod.Count == 0)
				{
					return Vector2.zero;
				}
				Vector2 centerPos = Vector2.zero;
				for (int i = 0; i < _modRenderVertListOfMod.Count; i++)
				{
					centerPos += _modRenderVertListOfMod[i]._renderVert._pos_World;
				}
				centerPos /= _modRenderVertListOfMod.Count;
				return centerPos;
			}
		}

		//public apControlParam ControlParamOfMod { get { if(_selectionType == SELECTION_TYPE.MeshGroup) { return _subControlParamOfMod; } return null; } }
		//public apControlParam ControlParamEditingMod { get { if(_selectionType == SELECTION_TYPE.MeshGroup) { return _subControlParamEditingMod; } return null; } }

		//Mesh Group을 본격적으로 수정할 땐, 다른 기능이 잠겨야 한다.
		public enum EX_EDIT_KEY_VALUE
		{
			None,//<<별 제한없이 컨트롤 가능하며 별도의 UI가 등장하지 않는다.
			ModMeshAndParamKey_ModVert,
			ParamKey_ModMesh,
			ParamKey_Bone
		}
		//private bool _isExclusiveModifierEdit = false;//<true이면 몇가지 기능이 잠긴다.
		private EX_EDIT_KEY_VALUE _exEditKeyValue = EX_EDIT_KEY_VALUE.None;
		public EX_EDIT_KEY_VALUE ExEditMode { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _exEditKeyValue; } return EX_EDIT_KEY_VALUE.None; } }

		/// <summary>
		/// Modifier / Animation 작업시 다른 Modifier/AnimLayer를 제외시킬 것인가에 대한 타입.
		/// </summary>
		public enum EX_EDIT
		{
			None,
			/// <summary>수동으로 제한시키지 않는한 최소한의 제한만 작동하는 모드</summary>
			General_Edit,
			/// <summary>수동으로 제한하여 1개의 Modifier(ParamSet)/AnimLayer만 허용하는 모드</summary>
			ExOnly_Edit,
		}
		private EX_EDIT _exclusiveEditing = EX_EDIT.None;//해당 모드에서 제한적 에디팅 중인가
		public EX_EDIT ExEditingMode { get { if (_selectionType == SELECTION_TYPE.MeshGroup) { return _exclusiveEditing; } return EX_EDIT.None; } }



		//선택잠금
		private bool _isSelectionLock = false;
		public bool IsSelectionLock { get { return _isSelectionLock; } }


		public bool IsExEditable
		{
			get
			{
				if (_selectionType != SELECTION_TYPE.MeshGroup)
				{
					return false;
				}

				if (_meshGroup == null || _modifier == null)
				{
					return false;
				}

				switch (ExEditMode)
				{
					case EX_EDIT_KEY_VALUE.None:
						return false;

					case EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert:
						return (ExKey_ModParamSetGroup != null) && (ExKey_ModParamSet != null)
							//&& (ExKey_ModMesh != null)
							;

					case EX_EDIT_KEY_VALUE.ParamKey_Bone:
					case EX_EDIT_KEY_VALUE.ParamKey_ModMesh:
						return (ExKey_ModParamSetGroup != null) && (ExKey_ModParamSet != null);

					default:
						Debug.LogError("TODO : IsExEditable에 정의되지 않는 타입이 들어왔습니다. [" + ExEditMode + "]");
						break;
				}
				return false;
			}
		}

		//키값으로 사용할 것 - 키로 사용하는 것들
		public apModifierParamSetGroup ExKey_ModParamSetGroup
		{
			get
			{
				if (ExEditMode == EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert
					|| ExEditMode == EX_EDIT_KEY_VALUE.ParamKey_Bone
					|| ExEditMode == EX_EDIT_KEY_VALUE.ParamKey_ModMesh)
				{
					return SubEditedParamSetGroup;
				}
				return null;
			}
		}

		public apModifierParamSet ExKey_ModParamSet
		{
			get
			{
				if (ExEditMode == EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert
					|| ExEditMode == EX_EDIT_KEY_VALUE.ParamKey_Bone
					|| ExEditMode == EX_EDIT_KEY_VALUE.ParamKey_ModMesh)
				{
					return ParamSetOfMod;
				}
				return null;
			}
		}

		public apModifiedMesh ExKey_ModMesh
		{
			get
			{
				if (ExEditMode == EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert)
				{
					return ModMeshOfMod;
				}
				return null;
			}
		}

		public apModifiedMesh ExValue_ModMesh
		{
			get
			{
				if (ExEditMode == EX_EDIT_KEY_VALUE.ParamKey_ModMesh)
				{
					return ModMeshOfMod;
				}
				return null;
			}
		}

		public ModRenderVert ExValue_ModVert
		{
			get
			{
				if (ExEditMode == EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert)
				{
					return ModRenderVertOfMod;
				}
				return null;
			}
		}
		//TODO : 여러개가 선택되었다면?


		//리깅 전용 변수 
		private bool _rigEdit_isBindingEdit = false;//Rig 작업중인가
		private bool _rigEdit_isTestPosing = false;//Rig 중에 Test Pose를 제어하고 있는가
												   //삭제 19.7.31 > Editor로 변수를 옮겼다.
												   //public enum RIGGING_EDIT_VIEW_MODE
												   //{
												   //	WeightColorOnly,
												   //	WeightWithTexture,
												   //}
												   //public RIGGING_EDIT_VIEW_MODE _rigEdit_viewMode = RIGGING_EDIT_VIEW_MODE.WeightWithTexture;
												   //public bool _rigEdit_isBoneColorView = true;



		private float _rigEdit_setWeightValue = 0.5f;
		private float _rigEdit_scaleWeightValue = 0.95f;
		public bool _rigEdit_isAutoNormalize = true;
		public bool IsRigEditTestPosing { get { return _rigEdit_isTestPosing; } }
		public bool IsRigEditBinding { get { return _rigEdit_isBindingEdit; } }

		//추가 19.7.24
		//리깅툴 v2

		//리깅툴 중 Weight를 설정하는 패널의 모드
		public enum RIGGING_WEIGHT_TOOL_MODE
		{
			NumericTool,
			BrushTool
		}
		private RIGGING_WEIGHT_TOOL_MODE _rigEdit_WeightToolMode = RIGGING_WEIGHT_TOOL_MODE.NumericTool;

		public enum RIGGING_BRUSH_TOOL_MODE
		{
			None,
			Add,
			Multiply,
			Blur
		}
		private RIGGING_BRUSH_TOOL_MODE _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
		public RIGGING_BRUSH_TOOL_MODE RiggingBrush_Mode
		{
			get
			{
				if (_rigEdit_WeightToolMode == RIGGING_WEIGHT_TOOL_MODE.NumericTool)
				{
					return RIGGING_BRUSH_TOOL_MODE.None;
				}
				return _rigEdit_BrushToolMode;
			}
		}
		private int _rigEdit_BrushRadius = 50;
		private float _rigEdit_BrushIntensity_Add = 0.1f;//초당 가중치 더하는 정도
		private float _rigEdit_BrushIntensity_Multiply = 1.1f;//초당 가중치 곱하는 정도
		private int _rigEdit_BrushIntensity_Blur = 50;//초당 가중치 블러 강도 (중심 기준)
		public float RiggingBrush_Radius { get { return _rigEdit_BrushRadius; } }
		public float RiggingBrush_Intensity_Add { get { return _rigEdit_BrushIntensity_Add; } }
		public float RiggingBrush_Intensity_Multiply { get { return _rigEdit_BrushIntensity_Multiply; } }
		public float RiggingBrush_Intensity_Blur { get { return (float)_rigEdit_BrushIntensity_Blur; } }
		public void ResetRiggingBrushMode()
		{
			_rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
		}



		private float _physics_setWeightValue = 0.5f;
		private float _physics_scaleWeightValue = 0.95f;
		private float _physics_windSimulationScale = 1000.0f;
		private Vector2 _physics_windSimulationDir = new Vector2(1.0f, 0.5f);

		//추가
		//본 생성시 Width를 한번 수정했으면 그 값이 이어지도록 한다.
		//단, Parent -> Child로 추가되서 자동으로 변경되는 경우는 제외 (직접 수정하는 경우만 적용)
		public int _lastBoneShapeWidth = 30;
		public bool _isLastBoneShapeWidthChanged = false;

		//Mesh Edit 변수
		private float _meshEdit_zDepthWeight = 0.5f;

		//추가 : 5.22 새로 생성한 Mesh인 경우 Setting탭이 나와야 한다.
		public List<apMesh> _createdNewMeshes = new List<apMesh>();//<<<Portrait가 바뀌면 초기화한다.



		/// <summary>
		/// Rigging 시에 "현재 Vertex에 연결된 Bone 정보"를 저장한다.
		/// 복수의 Vertex를 선택할 경우를 대비해서 몇가지 변수가 추가
		/// </summary>
		public class VertRigData
		{
			public apBone _bone = null;
			public int _nRig = 0;
			public float _weight = 0.0f;
			public float _weight_Min = 0.0f;
			public float _weight_Max = 0.0f;
			public VertRigData(apBone bone, float weight)
			{
				_bone = bone;
				_nRig = 1;
				_weight = weight;
				_weight_Min = _weight;
				_weight_Max = _weight;
			}
			public void AddRig(float weight)
			{
				_weight = ((_weight * _nRig) + weight) / (_nRig + 1);
				_nRig++;
				_weight_Min = Mathf.Min(weight, _weight_Min);
				_weight_Max = Mathf.Max(weight, _weight_Max);
			}
		}
		private List<VertRigData> _rigEdit_vertRigDataList = new List<VertRigData>();

		// 애니메이션 선택 정보
		public apAnimTimeline AnimTimeline { get { if (AnimClip != null) { return _subAnimTimeline; } return null; } }
		public apAnimTimelineLayer AnimTimelineLayer { get { if (AnimClip != null) { return _subAnimTimelineLayer; } return null; } }
		public apAnimKeyframe AnimKeyframe { get { if (AnimClip != null) { return _subAnimKeyframe; } return null; } }
		public apAnimKeyframe AnimWorkKeyframe { get { if (AnimTimelineLayer != null) { return _subAnimWorkKeyframe; } return null; } }
		public List<apAnimKeyframe> AnimKeyframes { get { if (AnimClip != null) { return _subAnimKeyframeList; } return null; } }
		public bool IsAnimKeyframeMultipleSelected { get { if (AnimClip != null) { return _subAnimKeyframeList.Count > 1; } return false; } }
		//public bool IsAnimAutoKey						{ get { return _isAnimAutoKey; } }
		//public bool IsAnimEditing { get { return _isAnimEditing; } }//<<ExEditing으로 변경
		public bool IsSelectedKeyframe(apAnimKeyframe keyframe)
		{
			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null)
			{
				Debug.LogError("Not Animation Type");
				return false;
			}
			return _subAnimKeyframeList.Contains(keyframe);
		}

		public void CancelAnimEditing() { _exAnimEditingMode = EX_EDIT.None; _isAnimSelectionLock = false; }


		public enum ANIM_SINGLE_PROPERTY_UI { Value, Curve }
		public ANIM_SINGLE_PROPERTY_UI _animPropertyUI = ANIM_SINGLE_PROPERTY_UI.Value;

		public enum ANIM_SINGLE_PROPERTY_CURVE_UI { Prev, Next }
		public ANIM_SINGLE_PROPERTY_CURVE_UI _animPropertyCurveUI = ANIM_SINGLE_PROPERTY_CURVE_UI.Next;

		//추가 19.12.31 : 다중 키프레임의 커브 속성
		public enum ANIM_MULTI_PROPERTY_CURVE_UI { Prev, Middle, Next }
		public ANIM_MULTI_PROPERTY_CURVE_UI _animPropertyCurveUI_Multi = ANIM_MULTI_PROPERTY_CURVE_UI.Next;

		public apTransform_Mesh SubMeshTransformOnAnimClip { get { if (AnimClip != null) { return _subMeshTransformOnAnimClip; } return null; } }
		public apTransform_MeshGroup SubMeshGroupTransformOnAnimClip { get { if (AnimClip != null) { return _subMeshGroupTransformOnAnimClip; } return null; } }
		public apControlParam SubControlParamOnAnimClip { get { if (AnimClip != null) { return _subControlParamOnAnimClip; } return null; } }

		public bool IsAnimSelectionLock { get { if (AnimClip != null) { return _isAnimSelectionLock; } return false; } }

		private float _animKeyframeAutoScrollTimer = 0.0f;

		//Bone 편집
		private apBone _bone = null;//현재 선택한 Bone (어떤 모드에서든지 참조 가능)
		public apBone Bone { get { return _bone; } }

		private bool _isBoneDefaultEditing = false;
		public bool IsBoneDefaultEditing { get { return _isBoneDefaultEditing; } }

		public enum BONE_EDIT_MODE
		{
			None,
			SelectOnly,
			Add,
			SelectAndTRS,
			Link
		}
		private BONE_EDIT_MODE _boneEditMode = BONE_EDIT_MODE.None;
		//public BONE_EDIT_MODE BoneEditMode { get { if (!_isBoneDefaultEditing) { return BONE_EDIT_MODE.None; } return _boneEditMode; } }
		public BONE_EDIT_MODE BoneEditMode { get { return _boneEditMode; } }

		public enum MESHGROUP_CHILD_HIERARCHY { ChildMeshes, Bones }
		public MESHGROUP_CHILD_HIERARCHY _meshGroupChildHierarchy = MESHGROUP_CHILD_HIERARCHY.ChildMeshes;
		public MESHGROUP_CHILD_HIERARCHY _meshGroupChildHierarchy_Anim = MESHGROUP_CHILD_HIERARCHY.ChildMeshes;

		//추가 20.3.28 : 리깅 작업시, (또는 그 외의 상황에서)
		//현재 작업 대상인 본들을 리스트로 만들어서, 그 외의 본들을 반투명하게 만드는게 작업에 효과적이다.
		private apRenderUnit _prevRenderUnit_CheckLinkedToModBones = null;
		private List<apBone> _linkedToModBones = new List<apBone>();
		/// <summary>현재 모디파이어 (주로 리깅)에 연결되었던 본들.</summary>
		public List<apBone> LinkedToModifierBones { get { return _linkedToModBones; } }


		// 통계 GUI
		private bool _isStatisticsNeedToRecalculate = true;//재계산이 필요하다.
		private bool _isStatisticsAvailable = false;

		private int _statistics_NumMesh = 0;
		private int _statistics_NumVert = 0;
		private int _statistics_NumEdge = 0;
		private int _statistics_NumTri = 0;
		private int _statistics_NumClippedMesh = 0;
		private int _statistics_NumClippedVert = 0;//클리핑은 따로 계산(Parent+Child)
		private int _statistics_NumTimelineLayer = 0;
		private int _statistics_NumKeyframes = 0;
		private int _statistics_NumBones = 0;//추가 19.12.25


		//추가 : Ex Edit를 위한 RenderUnit Flag 갱신시, 중복 처리를 막기 위함
		private apMeshGroup _prevExFlag_MeshGroup = null;
		private apModifierBase _prevExFlag_Modifier = null;
		private apModifierParamSetGroup _prevExFlag_ParamSetGroup = null;
		private apAnimClip _prevExFlag_AnimClip = null;



		//캡쳐 변수
		private enum CAPTURE_MODE
		{
			None,
			Capturing_Thumbnail,//<<썸네일 캡쳐중
			Capturing_ScreenShot,//<<ScreenShot 캡쳐중
			Capturing_GIF_Animation,//GIF 애니메이션 캡쳐중
			Capturing_MP4_Animation,//추가 : MP4 애니메이션 캡쳐중
			Capturing_Spritesheet
		}
		private CAPTURE_MODE _captureMode = CAPTURE_MODE.None;
		private object _captureLoadKey = null;
		private string _capturePrevFilePath_Directory = "";
		private apAnimClip _captureSelectedAnimClip = null;
		private bool _captureGIF_IsProgressDialog = false;

		private bool _captureSprite_IsAnimClipInit = false;
		private List<apAnimClip> _captureSprite_AnimClips = new List<apAnimClip>();
		private List<bool> _captureSprite_AnimClipFlags = new List<bool>();


		//추가 19.6.10 : MeshTransform의 속성 관련
		private string[] _shaderMode_Names = new string[] { "Material Set", "Custom Shader" };
		private const int MESH_SHADER_MODE__MATERIAL_SET = 0;
		private const int MESH_SHADER_MODE__CUSTOM_SHADER = 1;

		//추가 19.6.29 : TmpWorkVisible 변경 여부를 검사하자.
		private bool _isTmpWorkVisibleChanged_Meshes = false;
		private bool _isTmpWorkVisibleChanged_Bones = false;
		public void SetTmpWorkVisibleChanged(bool isAnyMeshChanged, bool isAnyBoneChanged)
		{
			_isTmpWorkVisibleChanged_Meshes = isAnyMeshChanged;
			_isTmpWorkVisibleChanged_Bones = isAnyBoneChanged;
		}
		public bool IsTmpWorkVisibleChanged_Meshes { get { return _isTmpWorkVisibleChanged_Meshes; } }
		public bool IsTmpWorkVisibleChanged_Bones { get { return _isTmpWorkVisibleChanged_Bones; } }

		//기타 지역 변수 대용으로 쓰이는 변수들
		private object _prevSelectedAnimObject = null;
		private object _prevSelectedAnimTimeline = null;
		private object _prevSelectedAnimTimelineLayer = null;
		private bool _isIgnoreAnimTimelineGUI = false;
		private bool _isFoldUI_AnimationTimelineLayers = false;


		private object _loadKey_ImportAnimClipRetarget = null;
		private object _loadKey_SelectPhysicsParam = null;

		private object _loadKey_SelectControlParamToPhyWind = null;
		private object _loadKey_SelectControlParamToPhyGravity = null;

		private object _physicModifier_prevSelectedTransform = null;
		private bool _physicModifier_prevIsContained = false;

		private object _riggingModifier_prevSelectedTransform = null;
		private bool _riggingModifier_prevIsContained = false;
		private int _riggingModifier_prevNumBoneWeights = 0;
		private int _riggingModifier_prevInfoMode = -1;

		private object _loadKey_SinglePoseImport_Mod = null;
		private object _loadKey_AddControlParam = null;

		private Vector2 _scrollBottom_Status = Vector2.zero;

		private object _loadKey_DuplicateBone = null;

		private object _loadKey_SelectControlParamForIKController = null;

		private object _loadKey_SelectBone = null;

		private apBone _prevBone_BoneProperty = null;
		private int _prevChildBoneCount = 0;

		private object _loadKey_SelectOtherMeshTransformForCopyingSettings = null;

		private object _loadKey_SelectMaterialSetOfMeshTransform = null;
		private apTransform_Mesh _tmp_PrevMeshTransform_MeshGroupSettingUI = null;

		private apAnimKeyframe _tmpPrevSelectedAnimKeyframe = null;
		private object _loadKey_SinglePoseImport_Anim = null;

		private bool _isTimelineWheelDrag = false;
		private Vector2 _prevTimelineWheelDragPos = Vector2.zero;
		private Vector2 _scrollPos_BottomAnimationRightProperty = Vector2.zero;

		private Vector2 _scroll_Timeline = new Vector2();
		//private float _scroll_Timeline_DummyY = 0.0f;
		private bool _isScrollingTimelineY = false;//타임라인의 Y스크롤 중인가.
		//private const string GUI_NAME_TIMELINE_SCROLL_Y = "GUI_TimlineScroll_Y";
		//private const string GUI_NAME_TIMELINE_SCROLL_X = "GUI_TimlineScroll_X";
		

		private object _loadKey_AddModifier = null;
		private object _loadKey_OnBoneStructureLoaded = null;
		private object _loadKey_SelectTextureDataToMesh = null;
		private object _loadKey_OnSelectControlParamPreset = null;
		private apControlParam _prevParam = null;
		private object _loadKey_SelectMeshGroupToAnimClip = null;
		private object _loadKey_AddTimelineToAnimClip = null;
		private object _loadKey_SelectTextureAsset = null;
		private object _loadKey_SelectBonesForAutoRig = null;
		private object _loadKey_MigrateMeshTransform = null;

		private int _timlineGUIWidth = -1;



		//추가 19.11.20 : GUIContent들 (Wrapper)
		private apGUIContentWrapper _guiContent_StepCompleted = null;
		private apGUIContentWrapper _guiContent_StepUncompleted = null;
		private apGUIContentWrapper _guiContent_StepUnUsed = null;


		private apGUIContentWrapper _guiContent_imgValueUp = null;
		private apGUIContentWrapper _guiContent_imgValueDown = null;
		private apGUIContentWrapper _guiContent_imgValueLeft = null;
		private apGUIContentWrapper _guiContent_imgValueRight = null;

		private apGUIContentWrapper _guiContent_MeshProperty_ResetVerts = null;
		private apGUIContentWrapper _guiContent_MeshProperty_RemoveMesh = null;
		private apGUIContentWrapper _guiContent_MeshProperty_ChangeImage = null;
		private apGUIContentWrapper _guiContent_MeshProperty_AutoLinkEdge = null;
		private apGUIContentWrapper _guiContent_MeshProperty_Draw_MakePolygones = null;
		private apGUIContentWrapper _guiContent_MeshProperty_MakePolygones = null;
		private apGUIContentWrapper _guiContent_MeshProperty_RemoveAllVertices = null;
		private apGUIContentWrapper _guiContent_MeshProperty_HowTo_MouseLeft = null;
		private apGUIContentWrapper _guiContent_MeshProperty_HowTo_MouseMiddle = null;
		private apGUIContentWrapper _guiContent_MeshProperty_HowTo_MouseRight = null;
		private apGUIContentWrapper _guiContent_MeshProperty_HowTo_KeyDelete = null;
		private apGUIContentWrapper _guiContent_MeshProperty_HowTo_KeyCtrl = null;
		private apGUIContentWrapper _guiContent_MeshProperty_HowTo_KeyShift = null;
		private apGUIContentWrapper _guiContent_MeshProperty_Texture = null;

		private apGUIContentWrapper _guiContent_Bottom2_Physic_WindON = null;
		private apGUIContentWrapper _guiContent_Bottom2_Physic_WindOFF = null;

		private apGUIContentWrapper _guiContent_Image_RemoveImage = null;
		private apGUIContentWrapper _guiContent_Animation_SelectMeshGroupBtn = null;
		private apGUIContentWrapper _guiContent_Animation_AddTimeline = null;
		private apGUIContentWrapper _guiContent_Animation_RemoveAnimation = null;
		private apGUIContentWrapper _guiContent_Animation_TimelineUnit_AnimMod = null;
		private apGUIContentWrapper _guiContent_Animation_TimelineUnit_ControlParam = null;

		private apGUIContentWrapper _guiContent_Overall_SelectedAnimClp = null;
		private apGUIContentWrapper _guiContent_Overall_MakeThumbnail = null;
		private apGUIContentWrapper _guiContent_Overall_TakeAScreenshot = null;
		private apGUIContentWrapper _guiContent_Overall_AnimItem = null;
		private apGUIContentWrapper _guiContent_Overall_Unregister = null;

		private apGUIContentWrapper _guiContent_Param_Presets = null;
		private apGUIContentWrapper _guiContent_Param_RemoveParam = null;
		private apGUIContentWrapper _guiContent_Param_IconPreset = null;

		private apGUIContentWrapper _guiContent_MeshGroupProperty_RemoveMeshGroup = null;
		private apGUIContentWrapper _guiContent_MeshGroupProperty_RemoveAllBones = null;
		private apGUIContentWrapper _guiContent_MeshGroupProperty_ModifierLayerUnit = null;
		private apGUIContentWrapper _guiContent_MeshGroupProperty_SetRootUnit = null;
		private apGUIContentWrapper _guiContent_MeshGroupProperty_AddModifier = null;

		private apGUIContentWrapper _guiContent_Bottom_Animation_TimelineLayerInfo = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_RemoveKeyframes = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_RemoveNumKeyframes = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_Fit = null;

		private apGUIContentWrapper _guiContent_Right_MeshGroup_MaterialSet = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_CustomShader = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_MatSetName = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_CopySettingToOtherMeshes = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_RiggingIconAndText = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_ParamIconAndText = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_RemoveBone = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_RemoveModifier = null;

		private apGUIContentWrapper _guiContent_Modifier_ParamSetItem = null;
		private apGUIContentWrapper _guiContent_Modifier_AddControlParameter = null;
		private apGUIContentWrapper _guiContent_CopyTextIcon = null;
		private apGUIContentWrapper _guiContent_PasteTextIcon = null;
		private apGUIContentWrapper _guiContent_Modifier_RigExport = null;
		private apGUIContentWrapper _guiContent_Modifier_RigImport = null;
		private apGUIContentWrapper _guiContent_Modifier_RemoveFromKeys = null;
		private apGUIContentWrapper _guiContent_Modifier_AddToKeys = null;
		private apGUIContentWrapper _guiContent_Modifier_AnimIconText = null;
		private apGUIContentWrapper _guiContent_Modifier_RemoveFromRigging = null;
		private apGUIContentWrapper _guiContent_Modifier_AddToRigging = null;
		private apGUIContentWrapper _guiContent_Modifier_AddToPhysics = null;
		private apGUIContentWrapper _guiContent_Modifier_RemoveFromPhysics = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_NameIcon = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Basic = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Stretchiness = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Inertia = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Restoring = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Viscosity = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Gravity = null;
		private apGUIContentWrapper _guiContent_Modifier_PhysicsSetting_Wind = null;
		private apGUIContentWrapper _guiContent_Right_Animation_ExportImportAnim = null;
		private apGUIContentWrapper _guiContent_Right_Animation_AllObjectToLayers = null;
		private apGUIContentWrapper _guiContent_Right_Animation_RemoveTimeline = null;
		private apGUIContentWrapper _guiContent_Right_Animation_AddTimelineLayerToEdit = null;
		private apGUIContentWrapper _guiContent_Right_Animation_RemoveTimelineLayer = null;
		private apGUIContentWrapper _guiContent_Bottom_EditMode_CommonIcon = null;
		private apGUIContentWrapper _guiContent_Icon_ModTF_Pos = null;
		private apGUIContentWrapper _guiContent_Icon_ModTF_Rot = null;
		private apGUIContentWrapper _guiContent_Icon_ModTF_Scale = null;
		private apGUIContentWrapper _guiContent_Icon_Mod_Color = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_MeshIcon = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_MeshGroupIcon = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_ModIcon = null;
		private apGUIContentWrapper _guiContent_Right_MeshGroup_AnimIcon = null;
		private apGUIContentWrapper _guiContent_Right_Animation_TimelineIcon_AnimWithMod = null;
		private apGUIContentWrapper _guiContent_Right_Animation_TimelineIcon_AnimWithControlParam = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_FirstFrame = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_PrevFrame = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_Play = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_Pause = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_NextFrame = null;
		private apGUIContentWrapper _guiContent_Bottom_Animation_LastFrame = null;

		private apGUIContentWrapper _guiContent_MakeMesh_PointCount_X = null;
		private apGUIContentWrapper _guiContent_MakeMesh_PointCount_Y = null;
		private apGUIContentWrapper _guiContent_MakeMesh_AutoGenPreview = null;
		private apGUIContentWrapper _guiContent_MakeMesh_GenerateMesh = null;

		private apGUIContentWrapper _guiContent_AnimKeyframeProp_PrevKeyLabel = null;
		private apGUIContentWrapper _guiContent_AnimKeyframeProp_NextKeyLabel = null;

		private apGUIContentWrapper _guiContent_Right2MeshGroup_ObjectProp_Name = null;
		private apGUIContentWrapper _guiContent_Right2MeshGroup_ObjectProp_Type = null;
		private apGUIContentWrapper _guiContent_Right2MeshGroup_ObjectProp_NickName = null;

		private apGUIContentWrapper _guiContent_MaterialSet_ON = null;
		private apGUIContentWrapper _guiContent_MaterialSet_OFF = null;

		private apGUIContentWrapper _guiContent_Right2MeshGroup_MaskParentName = null;
		private apGUIContentWrapper _guiContent_Right2MeshGroup_DuplicateTransform = null;
		private apGUIContentWrapper _guiContent_Right2MeshGroup_MigrateTransform = null;
		private apGUIContentWrapper _guiContent_Right2MeshGroup_DetachObject = null;


		private apGUIContentWrapper _guiContent_ModProp_ParamSetTarget_Name = null;
		private apGUIContentWrapper _guiContent_ModProp_ParamSetTarget_StatusText = null;

		private apGUIContentWrapper _guiContent_ModProp_Rigging_VertInfo = null;
		private apGUIContentWrapper _guiContent_ModProp_Rigging_BoneInfo = null;

		private apGUIContentWrapper _guiContent_RiggingBoneWeightLabel = null;
		private apGUIContentWrapper _guiContent_RiggingBoneWeightBoneName = null;


		private apGUIContentWrapper _guiContent_PhysicsGroupID_None = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_1 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_2 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_3 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_4 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_5 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_6 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_7 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_8 = null;
		private apGUIContentWrapper _guiContent_PhysicsGroupID_9 = null;

		private apGUIContentWrapper _guiContent_Right2_Animation_TargetObjectName = null;


		//TODO : GUIContent 추가시 ResetGUIContents() 함수에 초기화 코드를 추가할 것

		private GUIStyle _guiStyle_RigIcon_Lock = null;


		private apStringWrapper _strWrapper_64 = null;
		private apStringWrapper _strWrapper_128 = null;
		private string[] _imageColorSpaceNames = new string[] { "Gamma", "Linear" };
		private string[] _imageQualityNames = new string[] { "Compressed [Low Quality]", "Compressed [Default]", "Compressed [High Quality]", "Uncompressed" };
		private string[] _captureSpritePackSizeNames = new string[] { "256", "512", "1024", "2048", "4096" };





















		// Init
		//-------------------------------------
		public apSelection(apEditor editor)
		{
			_editor = editor;
			Clear();
		}

		public void Clear()
		{
			_selectionType = SELECTION_TYPE.None;

			_portrait = null;
			_image = null;
			_mesh = null;
			_meshGroup = null;
			_param = null;
			_modifier = null;
			_animClip = null;

			_bone = null;

			_subMeshTransformInGroup = null;
			_subMeshGroupTransformInGroup = null;
			_subEditedParamSetGroup = null;
			_subEditedParamSetGroupAnimPack = null;

			_exEditKeyValue = EX_EDIT_KEY_VALUE.None;
			_exclusiveEditing = EX_EDIT.None;
			_isSelectionLock = false;

			_renderUnitOfMod = null;
			//_renderVertOfMod = null;

			_modRenderVertOfMod = null;
			_modRenderVertListOfMod.Clear();
			_modRenderVertListOfMod_Weighted.Clear();

			_subMeshTransformListInGroup.Clear();
			_subMeshGroupTransformListInGroup.Clear();

			_isMeshGroupSetting_ChangePivot = false;

			_subAnimTimeline = null;
			_subAnimTimelineLayer = null;
			_subAnimKeyframe = null;
			_subAnimWorkKeyframe = null;

			_subMeshTransformOnAnimClip = null;
			_subMeshGroupTransformOnAnimClip = null;
			_subControlParamOnAnimClip = null;

			_subAnimKeyframeList.Clear();
			//_isAnimEditing = false;
			_exAnimEditingMode = EX_EDIT.None;
			//_isAnimAutoKey = false;
			_isAnimSelectionLock = false;

			_animTimelineCommonCurve.Clear();//추가 3.30

			_subAnimCommonKeyframeList.Clear();
			_subAnimCommonKeyframeList_Selected.Clear();


			_modMeshOfAnim = null;
			_modBoneOfAnim = null;
			_renderUnitOfAnim = null;
			_modRenderVertOfAnim = null;
			_modRenderVertListOfAnim.Clear();
			_modRenderVertListOfAnim_Weighted.Clear();

			_isBoneDefaultEditing = false;


			_rigEdit_isBindingEdit = false;//Rig 작업중인가
			_rigEdit_isTestPosing = false;//Rig 중에 Test Pose를 제어하고 있는가
										  //_rigEdit_viewMode = RIGGING_EDIT_VIEW_MODE.WeightWithTexture//<<이건 초기화 안된다.

			_imageImported = null;
			_imageImporter = null;

			_createdNewMeshes.Clear();

			_linkedToModBones.Clear();
			_prevRenderUnit_CheckLinkedToModBones = null;


			//추가 20.4.13 : VisibilityController 추가됨
			Editor.VisiblityController.ClearAll();
		}


		// Functions
		//-------------------------------------
		public void SetPortrait(apPortrait portrait)
		{
			if (portrait != _portrait)
			{
				Clear();
				_portrait = portrait;
			}

			apSnapShotManager.I.Clear();

			if (_portrait != null)
			{
				try
				{
					if (apEditorUtil.IsPrefab(_portrait.gameObject))
					{
						//Prefab 해제 안내
						if (EditorUtility.DisplayDialog(
										Editor.GetText(TEXT.DLG_PrefabDisconn_Title),
										Editor.GetText(TEXT.DLG_PrefabDisconn_Body),
										Editor.GetText(TEXT.Okay)))
						{
							apEditorUtil.DisconnectPrefab(_portrait);
						}
					}


				}
				catch (Exception ex)
				{
					Debug.LogError("Prefab Check Error : " + ex);
				}

			}
			//통계 재계산 요청
			SetStatisticsRefresh();
		}


		public void SetNone()
		{
			_selectionType = SELECTION_TYPE.None;

			//_portrait = null;
			_rootUnit = null;
			_rootUnitAnimClips.Clear();
			_curRootUnitAnimClip = null;

			_image = null;
			_mesh = null;
			_meshGroup = null;
			_param = null;
			_animClip = null;

			_subMeshTransformInGroup = null;
			_subMeshGroupTransformInGroup = null;

			_subMeshTransformListInGroup.Clear();
			_subMeshGroupTransformListInGroup.Clear();

			_modifier = null;

			_isMeshGroupSetting_ChangePivot = false;

			_paramSetOfMod = null;
			_modMeshOfMod = null;
			//_modVertOfMod = null;
			_modBoneOfMod = null;
			_modRegistableBones.Clear();
			//_subControlParamOfMod = null;
			//_subControlParamEditingMod = null;
			_subEditedParamSetGroup = null;
			_subEditedParamSetGroupAnimPack = null;

			_exEditKeyValue = EX_EDIT_KEY_VALUE.None;
			_exclusiveEditing = EX_EDIT.None;
			_isSelectionLock = false;

			_renderUnitOfMod = null;
			//_renderVertOfMod = null;

			_modRenderVertOfMod = null;
			_modRenderVertListOfMod.Clear();
			_modRenderVertListOfMod_Weighted.Clear();


			_subAnimTimeline = null;
			_subAnimTimelineLayer = null;
			_subAnimKeyframe = null;
			_subAnimWorkKeyframe = null;

			_subMeshTransformOnAnimClip = null;
			_subMeshGroupTransformOnAnimClip = null;
			_subControlParamOnAnimClip = null;

			_subAnimKeyframeList.Clear();
			//_isAnimEditing = false;
			_exAnimEditingMode = EX_EDIT.None;
			//_isAnimAutoKey = false;
			_isAnimSelectionLock = false;

			_animTimelineCommonCurve.Clear();//추가 3.30

			_subAnimCommonKeyframeList.Clear();
			_subAnimCommonKeyframeList_Selected.Clear();

			_modMeshOfAnim = null;
			_modBoneOfAnim = null;
			_renderUnitOfAnim = null;
			_modRenderVertOfAnim = null;
			_modRenderVertListOfAnim.Clear();
			_modRenderVertListOfAnim_Weighted.Clear();

			_bone = null;
			_isBoneDefaultEditing = false;

			_rigEdit_isBindingEdit = false;//Rig 작업중인가
			_rigEdit_isTestPosing = false;//Rig 중에 Test Pose를 제어하고 있는가

			_imageImported = null;
			_imageImporter = null;

			SetBoneRiggingTest();
			Editor.Hierarchy_MeshGroup.ResetSubUnits();

			if (Editor._portrait != null)
			{
				for (int i = 0; i < Editor._portrait._animClips.Count; i++)
				{
					Editor._portrait._animClips[i]._isSelectedInEditor = false;
				}
			}

			Editor.Gizmos.RevertFFDTransformForce();//<추가

			//기즈모 일단 초기화
			Editor.Gizmos.Unlink();
			Editor._blurEnabled = false;

			apEditorUtil.ReleaseGUIFocus();

			apEditorUtil.ResetUndo(Editor);//메뉴가 바뀌면 Undo 기록을 초기화한다.

			//통계 재계산 요청
			SetStatisticsRefresh();

			//스크롤 초기화
			Editor.ResetScrollPosition(false, true, true, true, true);

			//Onion 초기화
			Editor.Onion.Clear();

			//Capture 변수 초기화
			_captureSelectedAnimClip = null;
			_captureMode = CAPTURE_MODE.None;
			_captureLoadKey = null;
			_captureSelectedAnimClip = null;

			//_captureGIF_IsLoopAnimation = false;
			//_captureGIF_IsAnimFirstFrame = false;
			//_captureGIF_CurAnimFrame = 0;
			//_captureGIF_StartAnimFrame = 0;
			//_captureGIF_LastAnimFrame = 0;
			//_captureGIF_CurAnimLoop = 0;
			//_captureGIF_AnimLoopCount = 0;
			//_captureGIF_CurAnimProcess = 0;
			//_captureGIF_TotalAnimProcess = 0;
			//_captureGIF_GifAnimQuality = 0;
			_captureGIF_IsProgressDialog = false;

			_captureSprite_IsAnimClipInit = false;
			_captureSprite_AnimClips.Clear();
			_captureSprite_AnimClipFlags.Clear();
			//_captureSprite_CurAnimClipIndex = 0;
			//_captureSprite_CurFrame = 0;
			//_captureSprite_StartFrame = 0;
			//_captureSprite_LastFrame = 0;
			//_captureSprite_IsLoopAnimation = false;
			//_captureSprite_CurFPS = 0;
			//_captureSprite_TotalAnimFrames = 0;
			//_captureSprite_CurAnimFrameOnTotal = 0;


			//Mesh Edit 모드도 초기화
			//수정 > 일단 없던 일로
			//Editor._meshEditMode = apEditor.MESH_EDIT_MODE.Setting;
			//Editor._meshEditZDepthView = apEditor.MESH_EDIT_RENDER_MODE.Normal;
			//Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge;

			//당장 다음 1프레임은 쉰다.
			Editor.RefreshClippingGL();


			//애니메이션 Auto Key도 False
			Editor._isAnimAutoKey = false;

			_isScrollingTimelineY = false;

			//추가 : 8.22 : 
			Editor.MeshGenerator.Clear();

			//미러도 초기화
			Editor._meshEditMirrorMode = apEditor.MESH_EDIT_MIRROR_MODE.None;
			Editor.MirrorSet.Clear();
			Editor.MirrorSet.ClearMovedVertex();

			//추가 : Hierarchy SortMode 비활성화
			Editor.TurnOffHierarchyOrderEdit();

			_linkedToModBones.Clear();
			_prevRenderUnit_CheckLinkedToModBones = null;
		}


		public void SetImage(apTextureData image)
		{
			SetNone();

			_selectionType = SELECTION_TYPE.ImageRes;

			_image = image;

			//이미지의 Asset 정보는 매번 갱신한다. (언제든 바뀔 수 있으므로)
			if (image._image != null)
			{
				string fullPath = AssetDatabase.GetAssetPath(image._image);
				//Debug.Log("Image Path : " + fullPath);

				if (string.IsNullOrEmpty(fullPath))
				{
					image._assetFullPath = "";
					//image._isPSDFile = false;
				}
				else
				{
					image._assetFullPath = fullPath;
					//if (fullPath.Contains(".psd") || fullPath.Contains(".PSD"))
					//{
					//	image._isPSDFile = true;
					//}
					//else
					//{
					//	image._isPSDFile = false;
					//}
				}
			}
			else
			{
				//주의
				//만약 assetFullPath가 유효하다면 그걸 이용하자
				bool isRestoreImageFromPath = false;
				if (!string.IsNullOrEmpty(image._assetFullPath))
				{
					Texture2D restoreImage = AssetDatabase.LoadAssetAtPath<Texture2D>(image._assetFullPath);
					if (restoreImage != null)
					{
						isRestoreImageFromPath = true;
						image._image = restoreImage;
						//사라진 이미지를 경로로 복구했다. [" + image._assetFullPath + "]
					}
				}
				if (!isRestoreImageFromPath)
				{
					image._assetFullPath = "";
				}
				//image._isPSDFile = false;
			}

			//통계 재계산 요청
			SetStatisticsRefresh();
		}

		public void SetMesh(apMesh mesh)
		{
			SetNone();

			_selectionType = SELECTION_TYPE.Mesh;

			_mesh = mesh;
			//_prevMesh_Name = _mesh._name;

			//통계 재계산 요청
			SetStatisticsRefresh();

			//새로 생성된 Mesh라면 탭을 Setting으로 변경
			if (mesh != null && _createdNewMeshes.Contains(mesh))
			{
				_createdNewMeshes.Remove(mesh);
				Editor._meshEditMode = apEditor.MESH_EDIT_MODE.Setting;
			}



			//현재 MeshEditMode에 따라서 Gizmo 처리를 해야한다.
			switch (Editor._meshEditMode)
			{
				case apEditor.MESH_EDIT_MODE.Setting:
					Editor.Gizmos.Unlink();
					break;

				case apEditor.MESH_EDIT_MODE.MakeMesh:
					{
						Editor.Gizmos.Unlink();
						//변경 : MakeMesh 중 Gizmo가 사용되는 서브 툴이 있다.
						switch (Editor._meshEditeMode_MakeMesh)
						{
							case apEditor.MESH_EDIT_MODE_MAKEMESH.TRS:
								//TRS는 기즈모를 등록해야한다.
								Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshTRS());
								break;

							case apEditor.MESH_EDIT_MODE_MAKEMESH.AutoGenerate:
								//Auto Gen도 Control Point를 제어하는 기즈모가 있다.
								Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshAutoGen());
								break;
						}
					}

					break;

				case apEditor.MESH_EDIT_MODE.Modify:
					Editor.Gizmos.Unlink();
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshEdit_Modify());
					break;

				case apEditor.MESH_EDIT_MODE.PivotEdit:
					Editor.Gizmos.Unlink();
					break;
			}

			Editor.MeshGenerator.CheckAndSetMesh(mesh);
		}

		public void SetMeshGroup(apMeshGroup meshGroup)
		{
			SetNone();

			_selectionType = SELECTION_TYPE.MeshGroup;

			bool isChanged = false;
			if (_meshGroup != meshGroup)
			{
				isChanged = true;
			}
			_meshGroup = meshGroup;
			//_prevMeshGroup_Name = _meshGroup._name;

			//_meshGroup.SortRenderUnits(true);//Sort를 다시 해준다. (RenderUnit 세팅때문)
			//Mesh Group을 선택하면 이 초기화를 전부 실행해야한다.


			//Debug.LogError(">>>> Select MeshGroup [" + _meshGroup._name + "] >>>>>>");

			//이전 > 이렇게 작성하는건 안전하지만 무의미한 코드도 같이 실행된다.
			//_meshGroup.SetDirtyToReset();
			//_meshGroup.SetDirtyToSort();
			//_meshGroup.RefreshForce(true);//Depth 바뀌었다고 강제한다.

			//변경 20.4.4 : 아래와 같이 호출하자
			apUtil.LinkRefresh.Set_MeshGroup_ExceptAnimModifiers(_meshGroup);

			_meshGroup.SetDirtyToReset();
			_meshGroup.RefreshForce(true, 0.0f, apUtil.LinkRefresh);



			Editor._meshGroupEditMode = apEditor.MESHGROUP_EDIT_MODE.Setting;

			if (isChanged)
			{
				//이전
				//_meshGroup.LinkModMeshRenderUnits();
				//_meshGroup.RefreshModifierLink();

				//변경 20.4.4
				_meshGroup.RefreshModifierLink(apUtil.LinkRefresh);


				_meshGroup._modifierStack.InitModifierCalculatedValues();//<<값 초기화

				_meshGroup._modifierStack.RefreshAndSort(true);
				Editor.Gizmos.RevertFFDTransformForce();
			}

			
			//Debug.LogError("--------------------------------------------------");
			

			//렌더 유닛/본의 작업용 임시 Visibility를 설정하자.
			//이전
			//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_meshGroup, true, true, true);

			//_meshGroup.ResetBoneGUIVisible();//

			//변경 20.4.13 : Visibility Controller를 이용하자
			//동기화 후에 옵션에 따라 초기화를 하자
			Editor.VisiblityController.SyncMeshGroup(_meshGroup);
			Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	_meshGroup, 
																apEditorController.RESET_VISIBLE_ACTION.RestoreByOption, 
																apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);





			//추가 19.7.27 : 본의 RigLock도 해제한다.
			Editor.Controller.ResetBoneRigLock(meshGroup);

			Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshGroupSetting());

			SetModifierExclusiveEditing(EX_EDIT.None);
			SetModifierExclusiveEditKeyLock(false);
			SetModifierEditMode(EX_EDIT_KEY_VALUE.None);

			//통계 재계산 요청
			SetStatisticsRefresh();

			Editor.Hierarchy_MeshGroup.ResetSubUnits();
			Editor.Hierarchy_MeshGroup.RefreshUnits();
		}




		public void SetSubMeshInGroup(apTransform_Mesh subMeshTransformInGroup)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup)
			{
				_subMeshTransformInGroup = null;

				_renderUnitOfMod = null;
				//_renderVertOfMod = null;

				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();

				_subMeshTransformListInGroup.Clear();
				_subMeshGroupTransformListInGroup.Clear();

				return;
			}

			bool isChanged = (_subMeshTransformInGroup != subMeshTransformInGroup);

			

			_subMeshTransformInGroup = subMeshTransformInGroup;
			_subMeshGroupTransformInGroup = null;

			_subMeshTransformListInGroup.Clear();
			_subMeshTransformListInGroup.Add(_subMeshTransformInGroup);//<<MeshTransform 한개만 넣어주자

			_subMeshGroupTransformListInGroup.Clear();

			//여기서 만약 Modifier 선택중이며, 특정 ParamKey를 선택하고 있다면
			//자동으로 ModifierMesh를 선택해보자
			AutoSelectModMeshOrModBone();

			if (isChanged)
			{
				Editor.Gizmos.RevertFFDTransformForce();
			}
		}

		public void SetSubMeshGroupInGroup(apTransform_MeshGroup subMeshGroupTransformInGroup)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup)
			{
				_subMeshTransformInGroup = null;
				_subMeshGroupTransformInGroup = null;

				_renderUnitOfMod = null;
				//_renderVertOfMod = null;

				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();

				_subMeshTransformListInGroup.Clear();
				_subMeshGroupTransformListInGroup.Clear();
				return;
			}

			bool isChanged = (_subMeshGroupTransformInGroup != subMeshGroupTransformInGroup);

			_subMeshTransformInGroup = null;
			_subMeshGroupTransformInGroup = subMeshGroupTransformInGroup;


			_subMeshTransformListInGroup.Clear();
			_subMeshGroupTransformListInGroup.Clear();
			_subMeshGroupTransformListInGroup.Add(_subMeshGroupTransformInGroup);//<<MeshGroupTransform 한개만 넣어주자

			//여기서 만약 Modifier 선택중이며, 특정 ParamKey를 선택하고 있다면
			//자동으로 ModifierMesh를 선택해보자
			AutoSelectModMeshOrModBone();

			if (isChanged)
			{
				Editor.Gizmos.RevertFFDTransformForce();
			}
		}




		//추가 20.4.11 : Transform 계열 모디파이어 편집시 Mesh/MeshGroup Transform / Bone을 한번에 선택한다.
		//(Gizmo 이벤트와 Hierarchy에서 호출하자)
		//Riggin 모디파이어에서는 호출하지 말자
		/// <summary>
		/// Transform 계열 모디파이어에서 오브젝트를 호출하는 함수. Mesh/MeshGroup Transform이나 Bone을 배타적으로 선택한다.
		/// </summary>
		/// <param name="meshTransform"></param>
		/// <param name="meshGroupTransform"></param>
		/// <param name="bone"></param>
		public void SetSubObjectInGroup(apTransform_Mesh meshTransform,
										apTransform_MeshGroup meshGroupTransform,
										apBone bone)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup)
			{
				_subMeshTransformInGroup = null;
				_subMeshGroupTransformInGroup = null;
				_bone = null;

				_renderUnitOfMod = null;
				//_renderVertOfMod = null;

				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();

				_subMeshTransformListInGroup.Clear();
				_subMeshGroupTransformListInGroup.Clear();

				return;
			}

			
			bool isChanged = false;
			if (meshTransform != null)
			{
				isChanged = (_subMeshTransformInGroup != meshTransform)
					|| (_subMeshGroupTransformInGroup != null)
					|| (_bone != null);
			}
			else if (meshGroupTransform != null)
			{
				isChanged = (_subMeshTransformInGroup != null)
					|| (_subMeshGroupTransformInGroup != meshGroupTransform)
					|| (_bone != null);
			}
			else if (bone != null)
			{
				isChanged = (_subMeshTransformInGroup != null)
					|| (_subMeshGroupTransformInGroup != null)
					|| (_bone != bone);
			}
			else
			{
				//다 null이다.
				isChanged = (_subMeshTransformInGroup != null)
							|| (_subMeshGroupTransformInGroup != null)
							|| (_bone != null);
			}


			if (isChanged)
			{
				Editor.Gizmos.RevertFFDTransformForce();
			}

			if(meshTransform != null)
			{
				_subMeshTransformInGroup = meshTransform;
				_subMeshGroupTransformInGroup = null;
				_bone = null;
			}
			else if(meshGroupTransform != null)
			{
				_subMeshTransformInGroup = null;
				_subMeshGroupTransformInGroup = meshGroupTransform;
				_bone = null;
			}
			else if(bone != null)
			{
				_subMeshTransformInGroup = null;
				_subMeshGroupTransformInGroup = null;
				_bone = bone;
			}
			else
			{
				//다 null이다.
				_subMeshTransformInGroup = null;
				_subMeshGroupTransformInGroup = null;
				_bone = null;
			}


			if (SelectionType == SELECTION_TYPE.MeshGroup &&
				Modifier != null)
			{
				AutoSelectModMeshOrModBone();
			}
			if (SelectionType == SELECTION_TYPE.Animation && AnimClip != null)
			{
				AutoSelectAnimTimelineLayer(false);
			}

			

			apEditorUtil.ReleaseGUIFocus();
		}






		public void SetModifier(apModifierBase modifier)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup)
			{
				_modifier = null;
				return;
			}

			bool isChanged = false;
			if (_modifier != modifier || modifier == null)
			{
				_paramSetOfMod = null;
				_modMeshOfMod = null;
				//_modVertOfMod = null;
				_modBoneOfMod = null;
				_modRegistableBones.Clear();
				//_subControlParamOfMod = null;
				//_subControlParamEditingMod = null;
				_subEditedParamSetGroup = null;
				_subEditedParamSetGroupAnimPack = null;

				_renderUnitOfMod = null;
				//_renderVertOfMod = null;

				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();

				_exEditKeyValue = EX_EDIT_KEY_VALUE.None;
				_exclusiveEditing = EX_EDIT.None;

				_modifier = modifier;
				isChanged = true;

				_rigEdit_isBindingEdit = false;//Rig 작업중인가
				_rigEdit_isTestPosing = false;//Rig 중에 Test Pose를 제어하고 있는가
				_rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
				Editor.Gizmos.EndBrush();

				SetBoneRiggingTest();

				//스크롤 초기화 (오른쪽과 아래쪽)
				Editor.ResetScrollPosition(false, false, false, true, true);

			}

			_modifier = modifier;

			if (modifier != null)
			{
				if ((int)(modifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.VertexPos) != 0)
				{
					SetModifierEditMode(EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert);
				}
				else
				{
					SetModifierEditMode(EX_EDIT_KEY_VALUE.ParamKey_ModMesh);
				}
				#region [미사용 코드]
				//switch (modifier.CalculatedValueType)
				//{
				//	case apCalculatedResultParam.CALCULATED_VALUE_TYPE.Vertex:
				//	case apCalculatedResultParam.CALCULATED_VALUE_TYPE.Vertex_World:
				//		SetModifierEditMode(EXCLUSIVE_EDIT_MODE.ModMeshAndParamKey_ModVert);
				//		break;

				//	case apCalculatedResultParam.CALCULATED_VALUE_TYPE.MeshGroup_Transform:
				//		SetModifierEditMode(EXCLUSIVE_EDIT_MODE.ParamKey_ModMesh);
				//		break;

				//	case apCalculatedResultParam.CALCULATED_VALUE_TYPE.MeshGroup_Color:
				//		SetModifierEditMode(EXCLUSIVE_EDIT_MODE.ParamKey_ModMesh);
				//		break;

				//	default:
				//		Debug.LogError("TODO : Modfier -> ExEditMode 세팅 필요");
				//		break;
				//} 
				#endregion


				//ParamSetGroup이 선택되어 있다면 Modifier와의 유효성 체크
				bool isSubEditedParamSetGroupInit = false;
				if (_subEditedParamSetGroup != null)
				{
					if (!_modifier._paramSetGroup_controller.Contains(_subEditedParamSetGroup))
					{
						isSubEditedParamSetGroupInit = true;

					}
				}
				else if (_subEditedParamSetGroupAnimPack != null)
				{
					if (!_modifier._paramSetGroupAnimPacks.Contains(_subEditedParamSetGroupAnimPack))
					{
						isSubEditedParamSetGroupInit = true;
					}
				}
				if (isSubEditedParamSetGroupInit)
				{
					_paramSetOfMod = null;
					_modMeshOfMod = null;
					//_modVertOfMod = null;
					_modBoneOfMod = null;
					_modRegistableBones.Clear();
					//_subControlParamOfMod = null;
					//_subControlParamEditingMod = null;
					_subEditedParamSetGroup = null;
					_subEditedParamSetGroupAnimPack = null;

					_renderUnitOfMod = null;
					//_renderVertOfMod = null;

					_modRenderVertOfMod = null;
					_modRenderVertListOfMod.Clear();
					_modRenderVertListOfMod_Weighted.Clear();
				}



				if (MeshGroup != null)
				{
					//Exclusive 모두 해제
					MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					
					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, false, true, true);
					
					//변경 20.4.13 : 
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff,
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);



					RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, true);//<<추가
				}


				//각 타입에 따라 Gizmo를 넣어주자
				if (_modifier is apModifier_Morph)
				{
					//Morph
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Modifier_Morph());
				}
				else if (_modifier is apModifier_TF)
				{
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Modifier_TF());
				}
				else if (_modifier is apModifier_Rigging)
				{
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Modifier_Rigging());
					_rigEdit_isTestPosing = false;//Modifier를 선택하면 TestPosing은 취소된다.

					SetBoneRiggingTest();
				}
				else if (_modifier is apModifier_Physic)
				{
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Modifier_Physics());
				}
				else
				{
					if (!_modifier.IsAnimated)
					{
						Debug.LogError("Modifier를 선택하였으나 Animation 타입이 아닌데도 Gizmo에 지정되지 않은 타입 : " + _modifier.GetType());
					}
					//아니면 말고 >> Gizmo 초기화
					Editor.Gizmos.Unlink();
				}

				//AutoSelect하기 전에
				//현재 타입이 Static이라면
				//ParamSetGroup/ParamSet은 자동으로 선택한다.
				//ParamSetGroup, ParamSet은 각각 한개씩 존재한다.
				if (_modifier.SyncTarget == apModifierParamSetGroup.SYNC_TARGET.Static)
				{
					apModifierParamSetGroup paramSetGroup = null;
					apModifierParamSet paramSet = null;
					if (_modifier._paramSetGroup_controller.Count == 0)
					{
						Editor.Controller.AddStaticParamSetGroupToModifier();
					}

					paramSetGroup = _modifier._paramSetGroup_controller[0];

					if (paramSetGroup._paramSetList.Count == 0)
					{
						paramSet = new apModifierParamSet();
						paramSet.LinkParamSetGroup(paramSetGroup);
						paramSetGroup._paramSetList.Add(paramSet);
					}

					paramSet = paramSetGroup._paramSetList[0];

					SetParamSetGroupOfModifier(paramSetGroup);
					SetParamSetOfModifier(paramSet);
				}
				else if (!_modifier.IsAnimated)
				{
					if (_subEditedParamSetGroup == null)
					{
						if (_modifier._paramSetGroup_controller.Count > 0)
						{
							//마지막으로 입력된 PSG를 선택
							SetParamSetGroupOfModifier(_modifier._paramSetGroup_controller[_modifier._paramSetGroup_controller.Count - 1]);
						}
					}
					//맨 위의 ParamSetGroup을 선택하자
				}

				if (_modifier.SyncTarget == apModifierParamSetGroup.SYNC_TARGET.Controller)
				{
					//옵션이 허용하는 경우 (19.6.28 변경)
					if (Editor._isAutoSwitchControllerTab_Mod)
					{
						Editor.SetLeftTab(apEditor.TAB_LEFT.Controller);
					}
				}
			}
			else
			{
				if (MeshGroup != null)
				{
					//Exclusive 모두 해제
					MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					
					
					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, false, true, true);
					
					//변경 20.4.13
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff, 
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);


					RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, true);//<<추가
				}

				SetModifierEditMode(EX_EDIT_KEY_VALUE.None);

				//아니면 말고 >> Gizmo 초기화
				Editor.Gizmos.Unlink();
			}


			RefreshModifierExclusiveEditing();//<<Mod Lock 갱신
			AutoSelectModMeshOrModBone();

			if (isChanged)
			{
				Editor.Gizmos.RevertFFDTransformForce();
			}

			//추가 : MeshGroup Hierarchy를 갱신합시다.
			Editor.Hierarchy_MeshGroup.RefreshUnits();
		}




		public void SetParamSetGroupOfModifier(apModifierParamSetGroup paramSetGroup)
		{
			//AnimPack 선택은 여기서 무조건 해제된다.
			_subEditedParamSetGroupAnimPack = null;

			if (_selectionType != SELECTION_TYPE.MeshGroup || _modifier == null)
			{
				_subEditedParamSetGroup = null;
				return;
			}
			bool isCheck = false;

			bool isChangedTarget = (_subEditedParamSetGroup != paramSetGroup);
			if (_subEditedParamSetGroup != paramSetGroup)
			{
				_paramSetOfMod = null;
				_modMeshOfMod = null;
				//_modVertOfMod = null;
				_modBoneOfMod = null;
				//_subControlParamOfMod = null;
				//_subControlParamEditingMod = null;

				_renderUnitOfMod = null;
				//_renderVertOfMod = null;

				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();

				//_exclusiveEditMode = EXCLUSIVE_EDIT_MODE.None;
				//_isExclusiveEditing = false;

				if (ExEditingMode == EX_EDIT.ExOnly_Edit)
				{
					//SetModifierExclusiveEditing(false);
					SetModifierExclusiveEditing(EX_EDIT.None);
				}

				isCheck = true;
			}
			_subEditedParamSetGroup = paramSetGroup;

			if (isCheck && SubEditedParamSetGroup != null)
			{
				bool isChanged = SubEditedParamSetGroup.RefreshSync();
				if (isChanged)
				{
					apUtil.LinkRefresh.Set_MeshGroup_Modifier(MeshGroup, _modifier);

					MeshGroup.LinkModMeshRenderUnits(apUtil.LinkRefresh);//<<이걸 먼저 선언한다.
					MeshGroup.RefreshModifierLink(apUtil.LinkRefresh);
				}
			}

			//추가 : MeshGroup Hierarchy를 갱신합시다.
			Editor.Hierarchy_MeshGroup.RefreshUnits();

			RefreshModifierExclusiveEditing();//<<Mod Lock 갱신
			AutoSelectModMeshOrModBone();

			if (isChangedTarget)
			{
				Editor.Gizmos.RevertFFDTransformForce();
			}
		}

		#region [미사용 함수]
		///// <summary>
		///// Animated Modifier인 경우, ParamSetGroup 대신 ParamSetGroupAnimPack을 선택하고 보여준다.
		///// </summary>
		//public void SetParamSetGroupAnimPackOfModifier(apModifierParamSetGroupAnimPack paramSetGroupAnimPack)
		//{
		//	//일반 선택은 여기서 무조건 해제된다.
		//	_subEditedParamSetGroup = null;

		//	if (_selectionType != SELECTION_TYPE.MeshGroup || _modifier == null)
		//	{
		//		_subEditedParamSetGroupAnimPack = null;
		//		return;
		//	}
		//	bool isCheck = false;

		//	bool isChangedTarget = (_subEditedParamSetGroupAnimPack != paramSetGroupAnimPack);
		//	if (_subEditedParamSetGroupAnimPack != paramSetGroupAnimPack)
		//	{
		//		_paramSetOfMod = null;
		//		_modMeshOfMod = null;
		//		//_modVertOfMod = null;
		//		_modBoneOfMod = null;
		//		//_subControlParamOfMod = null;
		//		//_subControlParamEditingMod = null;

		//		_renderUnitOfMod = null;
		//		//_renderVertOfMod = null;

		//		_modRenderVertOfMod = null;
		//		_modRenderVertListOfMod.Clear();
		//		_modRenderVertListOfMod_Weighted.Clear();


		//		//_exclusiveEditMode = EXCLUSIVE_EDIT_MODE.None;
		//		//_isExclusiveEditing = false;
		//		if (ExEditingMode == EX_EDIT.ExOnly_Edit)
		//		{
		//			//SetModifierExclusiveEditing(false);
		//			SetModifierExclusiveEditing(EX_EDIT.None);
		//		}

		//		//SetModifierExclusiveEditing(false);

		//		isCheck = true;
		//	}
		//	_subEditedParamSetGroupAnimPack = paramSetGroupAnimPack;

		//	if (isCheck && SubEditedParamSetGroup != null)
		//	{
		//		bool isChanged = SubEditedParamSetGroup.RefreshSync();
		//		if (isChanged)
		//		{
		//			MeshGroup.LinkModMeshRenderUnits();//<<이걸 먼저 선언한다.
		//			MeshGroup.RefreshModifierLink();
		//		}
		//	}

		//	AutoSelectModMeshOrModBone();

		//	if (isChangedTarget)
		//	{
		//		Editor.Gizmos.RefreshFFDTransformForce();
		//	}
		//} 
		#endregion



		public void SetParamSetOfModifier(apModifierParamSet paramSetOfMod, bool isIgnoreExEditable = false)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup || _modifier == null)
			{
				_paramSetOfMod = null;
				return;
			}

			bool isChanged = false;
			if (_paramSetOfMod != paramSetOfMod)
			{

				//_subControlParamOfMod = null;
				//_subControlParamEditingMod = null;
				//_modMeshOfMod = null;
				//_modVertOfMod = null;
				//_modBoneOfMod = null;
				//_renderUnitOfMod = null;
				//_renderVertOfMod = null;
				isChanged = true;
			}
			_paramSetOfMod = paramSetOfMod;

			RefreshModifierExclusiveEditing(isIgnoreExEditable);//<<Mod Lock 갱신

			AutoSelectModMeshOrModBone();

			if (isChanged)
			{
				//Editor.Gizmos.RefreshFFDTransformForce();//<추가
				Editor.Gizmos.RevertTransformObjects(null);//<<변경 : Refresh -> Revert (강제)
			}
		}

		/// <summary>
		/// MeshGroup->Modifier->ParamSetGroup을 선택한 상태에서 ParamSet을 선택하지 않았다면,
		/// Modifier의 종류에 따라 ParamSet을 선택한다. (라고 하지만 Controller 입력 타입만 해당한다..)
		/// </summary>
		public void AutoSelectParamSetOfModifier()
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _portrait == null
				|| _meshGroup == null
				|| _modifier == null
				|| _subEditedParamSetGroup == null
				|| _paramSetOfMod != null)//<<ParamSet이 이미 선택되어도 걍 리턴한다.
			{
				return;
			}

			apEditorUtil.ReleaseGUIFocus();

			apModifierParamSet targetParamSet = null;
			switch (_modifier.SyncTarget)
			{
				case apModifierParamSetGroup.SYNC_TARGET.Controller:
					{
						if (_subEditedParamSetGroup._keyControlParam != null)
						{
							apControlParam controlParam = _subEditedParamSetGroup._keyControlParam;
							//해당 ControlParam이 위치한 곳과 같은 값을 가지는 ParamSet이 있으면 이동한다.
							switch (_subEditedParamSetGroup._keyControlParam._valueType)
							{
								case apControlParam.TYPE.Int:
									{
										targetParamSet = _subEditedParamSetGroup._paramSetList.Find(delegate (apModifierParamSet a)
										{
											return controlParam._int_Cur == a._conSyncValue_Int;
										});

										//선택할만한게 있으면 아예 Control Param값을 동기화
										if (targetParamSet != null)
										{
											controlParam._int_Cur = targetParamSet._conSyncValue_Int;
										}
									}
									break;

								case apControlParam.TYPE.Float:
									{
										float fSnapSize = Mathf.Abs(controlParam._float_Max - controlParam._float_Min) / controlParam._snapSize;
										targetParamSet = _subEditedParamSetGroup._paramSetList.Find(delegate (apModifierParamSet a)
										{
											return Mathf.Abs(controlParam._float_Cur - a._conSyncValue_Float) < (fSnapSize * 0.25f);
										});

										//선택할만한게 있으면 아예 Control Param값을 동기화
										if (targetParamSet != null)
										{
											controlParam._float_Cur = targetParamSet._conSyncValue_Float;
										}
									}
									break;

								case apControlParam.TYPE.Vector2:
									{
										float vSnapSizeX = Mathf.Abs(controlParam._vec2_Max.x - controlParam._vec2_Min.x) / controlParam._snapSize;
										float vSnapSizeY = Mathf.Abs(controlParam._vec2_Max.y - controlParam._vec2_Min.y) / controlParam._snapSize;

										targetParamSet = _subEditedParamSetGroup._paramSetList.Find(delegate (apModifierParamSet a)
										{
											return Mathf.Abs(controlParam._vec2_Cur.x - a._conSyncValue_Vector2.x) < (vSnapSizeX * 0.25f)
												&& Mathf.Abs(controlParam._vec2_Cur.y - a._conSyncValue_Vector2.y) < (vSnapSizeY * 0.25f);
										});

										//선택할만한게 있으면 아예 Control Param값을 동기화
										if (targetParamSet != null)
										{
											controlParam._vec2_Cur = targetParamSet._conSyncValue_Vector2;
										}
									}
									break;
							}
						}
					}
					break;
				default:
					//그 외에는.. 적용되는게 없어요
					break;
			}

			if (targetParamSet != null)
			{
				_paramSetOfMod = targetParamSet;

				AutoSelectModMeshOrModBone();

				//Editor.RefreshControllerAndHierarchy();
				Editor.Gizmos.RevertFFDTransformForce();//<추가
			}

		}

		// Mod-Mesh, Vert, Bone 선택
		public bool SetModMeshOfModifier(apModifiedMesh modMeshOfMod)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup || _modifier == null || _paramSetOfMod == null)
			{
				_modMeshOfMod = null;
				_modBoneOfMod = null;
				return false;
			}

			if (_modMeshOfMod != modMeshOfMod)
			{
				//_modVertOfMod = null;
				//_renderVertOfMod = null;
				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();
			}
			_modMeshOfMod = modMeshOfMod;
			_modBoneOfMod = null;
			return true;

		}

		public bool SetModBoneOfModifier(apModifiedBone modBoneOfMod)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup || _modifier == null || _paramSetOfMod == null)
			{
				_modMeshOfMod = null;
				_modBoneOfMod = null;
				return false;
			}

			_modBoneOfMod = modBoneOfMod;

			_modMeshOfMod = null;
			_modRenderVertOfMod = null;
			_modRenderVertListOfMod.Clear();
			_modRenderVertListOfMod_Weighted.Clear();

			apEditorUtil.ReleaseGUIFocus();

			return true;
		}

		/// <summary>
		/// Mod-Render Vertex를 선택한다. [Modifier 수정작업시]
		/// ModVert, ModVertRig, ModVertWeight 중 값 하나를 넣어줘야 한다.
		/// </summary>
		/// <param name="modVertOfMod"></param>
		/// <param name="renderVertOfMod"></param>
		public void SetModVertexOfModifier(apModifiedVertex modVertOfMod, apModifiedVertexRig modVertRigOfMod, apModifiedVertexWeight modVertWeight, apRenderVertex renderVertOfMod)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _paramSetOfMod == null
				|| _modMeshOfMod == null)
			{
				//_modVertOfMod = null;
				//_renderVertOfMod = null;
				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();
				return;
			}

			AutoSelectModMeshOrModBone();

			bool isInitReturn = false;
			if (renderVertOfMod == null)
			{
				isInitReturn = true;
			}
			else if (modVertOfMod == null && modVertRigOfMod == null && modVertWeight == null)
			{
				isInitReturn = true;
			}

			//if (modVertOfMod == null || renderVertOfMod == null)
			if (isInitReturn)
			{
				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();
				return;
			}


			//_modVertOfMod = modVertOfMod;
			//_renderVertOfMod = renderVertOfMod;
			bool isChangeModVert = false;
			//기존의 ModRenderVert를 유지할 것인가 또는 새로 선택(생성)할 것인가
			if (_modRenderVertOfMod != null)
			{
				if (_modRenderVertOfMod._renderVert != renderVertOfMod)
				{
					isChangeModVert = true;
				}
				else if (modVertOfMod != null)
				{
					if (_modRenderVertOfMod._modVert != modVertOfMod)
					{
						isChangeModVert = true;
					}
				}
				else if (modVertRigOfMod != null)
				{
					if (_modRenderVertOfMod._modVertRig != modVertRigOfMod)
					{
						isChangeModVert = true;
					}
				}
				else if (modVertWeight != null)
				{
					if (_modRenderVertOfMod._modVertWeight != modVertWeight)
					{
						isChangeModVert = true;
					}
				}
			}
			else
			{
				isChangeModVert = true;
			}

			if (isChangeModVert)
			{
				if (modVertOfMod != null)
				{
					//Vert
					_modRenderVertOfMod = new ModRenderVert(modVertOfMod, renderVertOfMod);
				}
				else if (modVertRigOfMod != null)
				{
					//VertRig
					_modRenderVertOfMod = new ModRenderVert(modVertRigOfMod, renderVertOfMod);
				}
				else
				{
					//VertWeight
					_modRenderVertOfMod = new ModRenderVert(modVertWeight, renderVertOfMod);
				}

				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod.Add(_modRenderVertOfMod);

				_modRenderVertListOfMod_Weighted.Clear();
			}
		}

		/// <summary>
		/// Mod-Render Vertex를 추가한다. [Modifier 수정작업시]
		/// ModVert, ModVertRig, ModVertWeight 중 값 하나를 넣어줘야 한다.
		/// </summary>
		public void AddModVertexOfModifier(apModifiedVertex modVertOfMod, apModifiedVertexRig modVertRigOfMod, apModifiedVertexWeight modVertWeight, apRenderVertex renderVertOfMod)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _paramSetOfMod == null
				|| _modMeshOfMod == null)
			{
				return;
			}

			//AutoSelectModMesh();//<<여기선 생략

			if (renderVertOfMod == null)
			{
				return;
			}

			if (modVertOfMod == null && modVertRigOfMod == null && modVertWeight == null)
			{
				//셋다 없으면 안된다.
				return;
			}
			bool isExistSame = _modRenderVertListOfMod.Exists(delegate (ModRenderVert a)
			{
				return a._renderVert == renderVertOfMod
				|| (a._modVert == modVertOfMod && modVertOfMod != null)
				|| (a._modVertRig == modVertRigOfMod && modVertRigOfMod != null)
				|| (a._modVertWeight == modVertWeight && modVertWeight != null);
			});

			if (!isExistSame)
			{
				ModRenderVert newModRenderVert = null;
				//ModVert에 연동할지, ModVertRig와 연동할지 결정한다.
				if (modVertOfMod != null)
				{
					newModRenderVert = new ModRenderVert(modVertOfMod, renderVertOfMod);
				}
				else if (modVertRigOfMod != null)
				{
					newModRenderVert = new ModRenderVert(modVertRigOfMod, renderVertOfMod);
				}
				else
				{
					newModRenderVert = new ModRenderVert(modVertWeight, renderVertOfMod);
				}

				_modRenderVertListOfMod.Add(newModRenderVert);

				if (_modRenderVertListOfMod.Count == 1)
				{
					_modRenderVertOfMod = newModRenderVert;
				}
			}
		}



		/// <summary>
		/// Mod-Render Vertex를 삭제한다. [Modifier 수정작업시]
		/// ModVert, ModVertRig, ModVertWeight 중 값 하나를 넣어줘야 한다.
		/// </summary>
		public void RemoveModVertexOfModifier(apModifiedVertex modVertOfMod, apModifiedVertexRig modVertRigOfMod, apModifiedVertexWeight modVertWeight, apRenderVertex renderVertOfMod)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _paramSetOfMod == null
				|| _modMeshOfMod == null)
			{
				return;
			}

			//AutoSelectModMesh();//<<여기선 생략

			if (renderVertOfMod == null)
			{
				return;
			}
			if (modVertOfMod == null && modVertRigOfMod == null && modVertWeight == null)
			{
				//셋다 없으면 안된다.
				return;
			}
			//if (modVertOfMod == null || renderVertOfMod == null)
			//{
			//	return;
			//}

			_modRenderVertListOfMod.RemoveAll(delegate (ModRenderVert a)
			{
				return a._renderVert == renderVertOfMod
				|| (a._modVert == modVertOfMod && modVertOfMod != null)
				|| (a._modVertRig == modVertRigOfMod && modVertRigOfMod != null)
				|| (a._modVertWeight == modVertWeight && modVertWeight != null);
			});

			if (_modRenderVertListOfMod.Count == 1)
			{
				_modRenderVertOfMod = _modRenderVertListOfMod[0];
			}
			else if (_modRenderVertListOfMod.Count == 0)
			{
				_modRenderVertOfMod = null;
			}
			else if (!_modRenderVertListOfAnim.Contains(_modRenderVertOfMod))
			{
				_modRenderVertOfMod = null;
				_modRenderVertOfMod = _modRenderVertListOfMod[0];
			}
		}




		//MeshTransform(MeshGroupT)이 선택되어있다면 자동으로 ParamSet 내부의 ModMesh를 선택한다.
		public void AutoSelectModMeshOrModBone()
		{
			//0. ParamSet까지 선택이 안되었다면 아무것도 선택 불가
			//1. ModMesh를 선택할 수 있는가
			//2. ModMesh의 유효한 선택이 없다면 ModBone 선택이 가능한가
			//거기에 맞게 처리
			apEditorUtil.ReleaseGUIFocus();

			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _meshGroup == null
				|| _modifier == null
				|| _paramSetOfMod == null
				|| _subEditedParamSetGroup == null
				)
			{
				//아무것도 선택하지 못할 경우
				_modMeshOfMod = null;
				_modBoneOfMod = null;
				//_modVertOfMod = null;
				_renderUnitOfMod = null;
				//_renderVertOfMod = null;

				_modRegistableBones.Clear();

				_modRenderVertOfMod = null;
				_modRenderVertListOfMod.Clear();
				_modRenderVertListOfMod_Weighted.Clear();
				//Debug.LogError("AutoSelectModMesh -> Clear 1");

				_linkedToModBones.Clear();//ModMesh와 연결된 본들도 리셋
				_prevRenderUnit_CheckLinkedToModBones = null;

				return;
			}

			//1. ModMesh부터 선택하자
			bool isModMeshSelected = false;

			if (_subMeshTransformInGroup != null || _subMeshGroupTransformInGroup != null)
			{
				//bool isModMeshValid = false;
				for (int i = 0; i < _paramSetOfMod._meshData.Count; i++)
				{
					apModifiedMesh modMesh = _paramSetOfMod._meshData[i];
					if (_subMeshTransformInGroup != null)
					{
						if (modMesh._transform_Mesh == _subMeshTransformInGroup)
						{
							if (SetModMeshOfModifier(modMesh))
							{
								//isModMeshValid = true;
								isModMeshSelected = true;
							}
							break;
						}
					}
					else if (_subMeshGroupTransformInGroup != null)
					{
						if (modMesh._transform_MeshGroup == _subMeshGroupTransformInGroup)
						{
							if (SetModMeshOfModifier(modMesh))
							{
								//isModMeshValid = true;
								isModMeshSelected = true;
							}
							break;
						}
					}
				}

				if (!isModMeshSelected)
				{
					//선택된 ModMesh가 없네용..
					_modMeshOfMod = null;
					_renderUnitOfMod = null;

					_modRenderVertOfMod = null;
					_modRenderVertListOfMod.Clear();
					_modRenderVertListOfMod_Weighted.Clear();
				}



				if (_subMeshTransformInGroup != null)
				{
					apRenderUnit nextSelectUnit = MeshGroup.GetRenderUnit(_subMeshTransformInGroup);
					if (nextSelectUnit != _renderUnitOfMod)
					{
						//_modVertOfMod = null;
						//_renderVertOfMod = null;
						_modRenderVertOfMod = null;
						_modRenderVertListOfMod.Clear();
						_modRenderVertListOfMod_Weighted.Clear();
					}
					_renderUnitOfMod = nextSelectUnit;
				}
				else if (_subMeshGroupTransformInGroup != null)
				{
					apRenderUnit nextSelectUnit = MeshGroup.GetRenderUnit(_subMeshGroupTransformInGroup);
					if (nextSelectUnit != _renderUnitOfMod)
					{
						//_modVertOfMod = null;
						//_renderVertOfMod = null;
						_modRenderVertOfMod = null;
						_modRenderVertListOfMod.Clear();
						_modRenderVertListOfMod_Weighted.Clear();
					}
					_renderUnitOfMod = nextSelectUnit;
				}
				else
				{
					_modMeshOfMod = null;
					//_modVertOfMod = null;
					_renderUnitOfMod = null;
					//_renderVertOfMod = null;

					_modRenderVertOfMod = null;
					_modRenderVertListOfMod.Clear();
					_modRenderVertListOfMod_Weighted.Clear();
					//Debug.LogError("AutoSelectModMesh -> Clear 2");
					isModMeshSelected = false;
				}
			}

			if (!isModMeshSelected)
			{
				_modMeshOfMod = null;
			}
			else
			{
				_modBoneOfMod = null;
			}

			//2. ModMesh 선택한게 없다면 ModBone을 선택해보자
			if (!isModMeshSelected)
			{
				_modBoneOfMod = null;

				if (Bone != null)
				{
					//선택한 Bone이 있다면
					for (int i = 0; i < _paramSetOfMod._boneData.Count; i++)
					{
						apModifiedBone modBone = _paramSetOfMod._boneData[i];
						if (modBone._bone == Bone)
						{
							if (SetModBoneOfModifier(modBone))
							{
								break;
							}
						}
					}
				}
			}

			//추가
			//ModBone으로 선택 가능한 Bone 리스트를 만들어준다.
			_modRegistableBones.Clear();

			for (int i = 0; i < _paramSetOfMod._boneData.Count; i++)
			{
				_modRegistableBones.Add(_paramSetOfMod._boneData[i]._bone);
			}

			//추가 : 이 ModMesh와 연결된 본들을 리스트로 모으자
			CheckLinkedToModMeshBones(false);

			//MeshGroup Hierarchy를 갱신합시다.
			Editor.Hierarchy_MeshGroup.RefreshUnits();
		}



		/// <summary>
		/// 추가 20.3.28 : 현재 작업의 대상이 되는 본들을 리스트로 저장한다.
		/// 리깅 모디파이어를 선택한 상태에서 ModMesh를 
		/// </summary>
		public void CheckLinkedToModMeshBones(bool isForce)
		{
			if (_modifier == null ||
				_subMeshTransformInGroup == null ||
				_modMeshOfMod == null ||
				_renderUnitOfMod == null)
			{
				if (_linkedToModBones.Count > 0)
				{
					_linkedToModBones.Clear();
				}
				_prevRenderUnit_CheckLinkedToModBones = null;
				return;
			}


			if (_modifier.ModifierType != apModifierBase.MODIFIER_TYPE.Rigging
				|| !_modMeshOfMod._isMeshTransform
				|| _modMeshOfMod._transform_Mesh == null
				|| _renderUnitOfMod._meshTransform == null
				|| _renderUnitOfMod._meshTransform != _modMeshOfMod._transform_Mesh)
			{
				if (_linkedToModBones.Count > 0)
				{
					_linkedToModBones.Clear();
				}
				_prevRenderUnit_CheckLinkedToModBones = null;
				return;
			}

			if (!isForce
				&& _prevRenderUnit_CheckLinkedToModBones == _renderUnitOfMod)
			{
				//렌더 유닛이 이전 처리와 동일하다면 패스하자.
				//Debug.LogWarning("Pass >>>");
				return;
			}

			_prevRenderUnit_CheckLinkedToModBones = _renderUnitOfMod;
			_linkedToModBones.Clear();

			if (_modMeshOfMod._vertRigs == null ||
				_modMeshOfMod._vertRigs.Count == 0)
			{
				return;
			}



			int nVert = _modMeshOfMod._vertRigs.Count;
			apModifiedVertexRig curModVertRig = null;

			int nWeightPairs = 0;
			apBone curBone = null;

			for (int iVert = 0; iVert < nVert; iVert++)
			{
				curModVertRig = _modMeshOfMod._vertRigs[iVert];
				if (curModVertRig == null)
				{
					continue;
				}
				nWeightPairs = curModVertRig._weightPairs.Count;

				for (int iPair = 0; iPair < nWeightPairs; iPair++)
				{
					curBone = curModVertRig._weightPairs[iPair]._bone;
					if (!_linkedToModBones.Contains(curBone))
					{
						//리깅으로 등록된 본을 리스트에 넣는다.
						_linkedToModBones.Add(curBone);
					}
				}
			}
			//Debug.Log("RigData 갱신 [" + _linkedToModBones.Count + "]");

		}

		/// <summary>
		/// 추가 20.3.29
		/// 에디터의 옵션 (_rigGUIOption_NoLinkedBoneVisibility)에 따라 "모디파이어에 연결되지 않은" 본들을 렌더링이나 선택에서 제외할 필요가 있는데, 그때 LinkedToModifierBones를 이용해야한다.
		/// 본을 렌더링하거나 선택할 때 이 함수의 값이 true라면 LinkedToModifierBones를 이용하자
		/// </summary>
		/// <returns></returns>
		public bool IsCheckableToLinkedToModifierBones()
		{
			//1. 리깅 화면에서 리깅 편집 중일때
			if (SelectionType == apSelection.SELECTION_TYPE.MeshGroup
						&& Modifier != null
						&& IsRigEditBinding
						&& ModMeshOfMod != null
						&& RenderUnitOfMod != null
						&& LinkedToModifierBones.Count > 0
						)
			{
				//모디파이어와 모드 메시가 선택된 상태일 때.
				if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
				{
					return true;
				}
			}
			return false;
		}








		//모디파이어 편집 모드 설정

		public bool SetModifierEditMode(EX_EDIT_KEY_VALUE editMode)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null)
			{
				_exEditKeyValue = EX_EDIT_KEY_VALUE.None;
				_exclusiveEditing = EX_EDIT.None;
				return false;
			}

			if (_exEditKeyValue != editMode)
			{
				_exclusiveEditing = EX_EDIT.None;
				_isSelectionLock = false;

				if (MeshGroup != null)
				{
					//Exclusive 모두 해제
					MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					
					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, false, true, true);

					//변경 20.4.13
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff,
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);


					RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, false);//<<추가
				}
			}
			_exEditKeyValue = editMode;

			RefreshModifierExclusiveEditing();//<<Mod Lock 갱신

			Editor.RefreshControllerAndHierarchy(false);

			return true;
		}


		/// <summary>
		/// Modifier 편집시 Mod Lock을 갱신한다.
		/// SetModifierExclusiveEditing() 함수를 호출하는 것과 같으나,
		/// Lock-Unlock이 전환되지는 않는다.
		/// </summary>
		public void RefreshModifierExclusiveEditing(bool isIgnoreExEditable = false)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _subEditedParamSetGroup == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				_exclusiveEditing = EX_EDIT.None;
			}


			SetModifierExclusiveEditing(_exclusiveEditing, isIgnoreExEditable);
		}

		//모디파이어의 Exclusive Editing (Modifier Lock)
		public bool SetModifierExclusiveEditing(EX_EDIT exclusiveEditing, bool isIgnoreExEditable = false)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _subEditedParamSetGroup == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				_exclusiveEditing = EX_EDIT.None;
				return false;
			}



			bool isExEditable = IsExEditable;
			if (MeshGroup == null || Modifier == null || SubEditedParamSetGroup == null)
			{
				isExEditable = false;
			}

			//기존
			if (!isIgnoreExEditable)
			{
				if (isExEditable)
				{
					_exclusiveEditing = exclusiveEditing;
				}
				else
				{
					_exclusiveEditing = EX_EDIT.None;
				}
			}
			else
			{
				//추가 3.31 : ExEditing 모드를 유지하는 옵션 추가
				_exclusiveEditing = exclusiveEditing;
			}




			bool isModLock_ColorUpdate = Editor.GetModLockOption_ColorPreview(_exclusiveEditing);
			bool isModLock_OtherMod = Editor.GetModLockOption_CalculateIfNotAddedOther(_exclusiveEditing);

			//작업중인 Modifier 외에는 일부 제외를 하자
			switch (_exclusiveEditing)
			{
				case EX_EDIT.None:
					//모든 Modifier를 활성화한다.
					{
						if (MeshGroup != null)
						{
							//Exclusive 모두 해제
							MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
							
							//이전
							//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, false, true, true);
							
							//변경 20.4.13
							Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																				apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff,
																				apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);


							RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, false);//<<추가
						}

						//_modVertOfMod = null;
						//_renderVertOfMod = null;

						_modRenderVertOfMod = null;
						_modRenderVertListOfMod.Clear();
						_modRenderVertListOfMod_Weighted.Clear();
					}
					break;

				case EX_EDIT.General_Edit:
					//연동 가능한 Modifier를 활성화한다. (Mod Unlock)
					MeshGroup._modifierStack.SetExclusiveModifierInEditingGeneral(_modifier, isModLock_ColorUpdate, isModLock_OtherMod);
					RefreshMeshGroupExEditingFlags(MeshGroup, _modifier, SubEditedParamSetGroup, null, false);//<<추가
					break;

				case EX_EDIT.ExOnly_Edit:
					//작업중인 Modifier만 활성화한다. (Mod Lock)
					MeshGroup._modifierStack.SetExclusiveModifierInEditing(_modifier, SubEditedParamSetGroup, isModLock_ColorUpdate);
					RefreshMeshGroupExEditingFlags(MeshGroup, _modifier, SubEditedParamSetGroup, null, false);//<<추가
					break;
			}

			Editor.RefreshControllerAndHierarchy(false);

			return true;
		}



		//Ex Bone 렌더링용 함수
		//많은 내용이 빠져있다.
		public bool SetModifierExclusiveEditing_Tmp(EX_EDIT exclusiveEditing)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _subEditedParamSetGroup == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				_exclusiveEditing = EX_EDIT.None;
				return false;
			}



			bool isExEditable = IsExEditable;
			if (MeshGroup == null || Modifier == null || SubEditedParamSetGroup == null)
			{
				isExEditable = false;
			}

			if (isExEditable)
			{
				_exclusiveEditing = exclusiveEditing;
			}
			else
			{
				_exclusiveEditing = EX_EDIT.None;
			}

			bool isModLock_ColorUpdate = Editor.GetModLockOption_ColorPreview(_exclusiveEditing);
			bool isModLock_OtherMod = Editor.GetModLockOption_CalculateIfNotAddedOther(_exclusiveEditing);

			//작업중인 Modifier 외에는 일부 제외를 하자
			switch (_exclusiveEditing)
			{
				case EX_EDIT.None:
					//모든 Modifier를 활성화한다.
					{
						if (MeshGroup != null)
						{
							//Exclusive 모두 해제
							MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
							//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup);
							RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, false);//<<추가
						}

						//_modVertOfMod = null;
						//_renderVertOfMod = null;

						//_modRenderVertOfMod = null;
						//_modRenderVertListOfMod.Clear();
						//_modRenderVertListOfMod_Weighted.Clear();
					}
					break;

				case EX_EDIT.General_Edit:
					//연동 가능한 Modifier를 활성화한다. (Mod Unlock)
					MeshGroup._modifierStack.SetExclusiveModifierInEditingGeneral(_modifier, isModLock_ColorUpdate, isModLock_OtherMod);
					RefreshMeshGroupExEditingFlags(MeshGroup, _modifier, SubEditedParamSetGroup, null, false);//<<추가
					break;

				case EX_EDIT.ExOnly_Edit:
					//작업중인 Modifier만 활성화한다. (Mod Lock)
					MeshGroup._modifierStack.SetExclusiveModifierInEditing(_modifier, SubEditedParamSetGroup, isModLock_ColorUpdate);
					RefreshMeshGroupExEditingFlags(MeshGroup, _modifier, SubEditedParamSetGroup, null, false);//<<추가
					break;
			}

			//Editor.RefreshControllerAndHierarchy();

			return true;
		}


		/// <summary>
		/// 특정 메시그룹의 RenderUnit과 Bone의 Ex Edit에 대한 Flag를 갱신한다.
		/// Ex Edit가 변경되는 모든 시점에서 이 함수를 호출한다.
		/// AnimClip이 선택되어 있다면 animClip이 null이 아닌 값을 넣어준다.
		/// AnimClip이 없다면 ParamSetGroup이 있어야 한다. (Static 타입 제외)
		/// 둘다 null이라면 Ex Edit가 아닌 것으로 처리한다.
		/// Child MeshGroup으로 재귀적으로 호출한다.
		/// </summary>
		/// <param name="targetModifier"></param>
		/// <param name="targetAnimClip"></param>
		public void RefreshMeshGroupExEditingFlags(apMeshGroup targetMeshGroup,
													apModifierBase targetModifier,
													apModifierParamSetGroup targetParamSetGroup,
													apAnimClip targetAnimClip,
													bool isForce,
													bool isRecursiveCall = false)
		{
			//재귀적인 호출이 아니라면
			if (!isRecursiveCall)
			{
				if (!isForce)
				{
					if (targetMeshGroup == _prevExFlag_MeshGroup
						&& targetModifier == _prevExFlag_Modifier
						&& targetParamSetGroup == _prevExFlag_ParamSetGroup
						&& targetAnimClip == _prevExFlag_AnimClip)
					{
						//바뀐게 없다.
						// 전부 null이라면 => 그래도 실행
						// 하나라도 null이 아니라면 => 중복 실행이다.
						if (targetMeshGroup != null ||
							targetModifier != null ||
							targetParamSetGroup != null ||
							targetAnimClip != null)
						{
							//Debug.LogError("중복 요청");
							return;
						}
					}
				}

				_prevExFlag_MeshGroup = targetMeshGroup;
				_prevExFlag_Modifier = targetModifier;
				_prevExFlag_ParamSetGroup = targetParamSetGroup;
				_prevExFlag_AnimClip = targetAnimClip;
			}

			//Debug.Log("RefreshMeshGroupExEditingFlags "
			//	+ "- Mod (" + (targetModifier != null ? "O" : "X") + ")"
			//	+ " / PSG (" + (targetParamSetGroup != null ? "O" : "X") + ")"
			//	+ " / Anim (" + (targetAnimClip != null ? "O" : "X") + ")"
			//	);

			if (targetMeshGroup == null)
			{
				//Debug.LogError("Target MeshGroup is Null");
				return;
			}
			apRenderUnit renderUnit = null;
			apBone bone = null;
			apModifierParamSetGroup paramSetGroup = null;
			bool isExMode = (targetModifier != null);
			bool isMeshTF = false;
			bool isMeshGroupTF = false;

			//if (targetModifier != null)
			//{
			//	Debug.Log("Is Animated : " + targetModifier.IsAnimated);
			//}

			//RenderUnit (MeshTransform / MeshGroupTransform)을 체크하자
			for (int i = 0; i < targetMeshGroup._renderUnits_All.Count; i++)
			{
				renderUnit = targetMeshGroup._renderUnits_All[i];

				isMeshTF = (renderUnit._meshTransform != null);
				isMeshGroupTF = (renderUnit._meshGroupTransform != null);

				if (!isExMode)
				{
					//Ex Mode가 아니다. (기본값)
					renderUnit._exCalculateMode = apRenderUnit.EX_CALCULATE.Normal;
					//Debug.Log("Render Unit : " + renderUnit.Name + " -- Normal");
				}
				else
				{
					//Ex Mode이다.
					//(포함 여부 체크해야함)
					bool isContained = false;
					for (int iPSG = 0; iPSG < targetModifier._paramSetGroup_controller.Count; iPSG++)
					{
						paramSetGroup = targetModifier._paramSetGroup_controller[iPSG];
						if (targetModifier.IsAnimated)
						{
							if (targetAnimClip != null && paramSetGroup._keyAnimClip != targetAnimClip)
							{
								//AnimClip 타입의 Modifier의 경우, 
								//현재 AnimClip과 같아야 한다.
								continue;
							}
						}
						else if (targetModifier.SyncTarget != apModifierParamSetGroup.SYNC_TARGET.Static)
						{
							//AnimType은 아니고 Static 타입도 아닌 경우
							if (targetParamSetGroup != null && paramSetGroup != targetParamSetGroup)
							{
								//ParamSetGroup이 다르다면 패스
								continue;
							}
						}


						if (isMeshTF)
						{
							if (paramSetGroup.IsMeshTransformContain(renderUnit._meshTransform))
							{
								//MeshTransform이 포함된당.
								isContained = true;
								break;
							}
						}
						else if (isMeshGroupTF)
						{
							if (paramSetGroup.IsMeshGroupTransformContain(renderUnit._meshGroupTransform))
							{
								//MeshGroupTransform이 포함된당.
								isContained = true;
								break;
							}
						}

					}

					if (isContained)
					{
						//ExEdit에 포함되었다.
						renderUnit._exCalculateMode = apRenderUnit.EX_CALCULATE.ExAdded;
						//Debug.Log("Render Unit : " + renderUnit.Name + " -- ExAdded");
					}
					else
					{
						//ExEdit에 포함되지 않았다.
						renderUnit._exCalculateMode = apRenderUnit.EX_CALCULATE.ExNotAdded;
						//Debug.Log("Render Unit : " + renderUnit.Name + " -- Ex Not Added");
					}
				}
			}

			//Bone도 체크하자
			//<BONE_EDIT> >> <CHECK> 이건 일단 그대로 두자. 밑에서 Recursive하게 체크를 하닝께
			for (int i = 0; i < targetMeshGroup._boneList_All.Count; i++)
			{
				bone = targetMeshGroup._boneList_All[i];

				if (!isExMode)
				{
					//Ex Mode가 아니다. (기본값)
					bone._exCalculateMode = apBone.EX_CALCULATE.Normal;
				}
				else
				{
					//Ex Mode이다.
					//(포함 여부 체크해야함)
					bool isContained = false;
					for (int iPSG = 0; iPSG < targetModifier._paramSetGroup_controller.Count; iPSG++)
					{
						paramSetGroup = targetModifier._paramSetGroup_controller[iPSG];
						if (targetModifier.IsAnimated && targetAnimClip != null && paramSetGroup._keyAnimClip != targetAnimClip)
						{
							//AnimClip 타입의 Modifier의 경우, 
							//현재 AnimClip과 같아야 한다.
							continue;
						}

						if (paramSetGroup.IsBoneContain(bone))
						{
							//Bone이 포함된당.
							isContained = true;
							break;
						}
					}

					if (isContained)
					{
						//ExEdit에 포함되었다.
						bone._exCalculateMode = apBone.EX_CALCULATE.ExAdded;
					}
					else
					{
						//ExEdit에 포함되지 않았다.
						bone._exCalculateMode = apBone.EX_CALCULATE.ExNotAdded;
					}
				}
			}

			if (targetMeshGroup._childMeshGroupTransforms != null &&
				targetMeshGroup._childMeshGroupTransforms.Count > 0)
			{
				for (int i = 0; i < targetMeshGroup._childMeshGroupTransforms.Count; i++)
				{
					apMeshGroup childMeshGroup = targetMeshGroup._childMeshGroupTransforms[i]._meshGroup;
					if (childMeshGroup != targetMeshGroup)
					{
						RefreshMeshGroupExEditingFlags(childMeshGroup, targetModifier, targetParamSetGroup, targetAnimClip, isForce, true);
					}
				}
			}


		}








		/// <summary>
		/// 단축키 [A]를 눌러서 Editing 상태를 토글하자
		/// </summary>
		/// <param name="paramObject"></param>
		public void OnHotKeyEvent_ToggleModifierEditing(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				return;
			}

			ToggleRigEditBinding();
		}

		private void ToggleRigEditBinding()
		{
			bool isRiggingModifier = (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging);

			if (isRiggingModifier)
			{
				//1. Rigging 타입의 Modifier인 경우
				_rigEdit_isBindingEdit = !_rigEdit_isBindingEdit;
				_rigEdit_isTestPosing = false;

				//작업중인 Modifier 외에는 일부 제외를 하자
				if (_rigEdit_isBindingEdit)
				{
					MeshGroup._modifierStack.SetExclusiveModifierInEditing(_modifier, SubEditedParamSetGroup, false);

					//변경 3.23 : 선택 잠금을 무조건 켜는게 아니라, 에디터 설정에 따라 켤지 말지 결정한다.
					//true 또는 변경 없음 (false가 아님)
					if (Editor._isSelectionLockOption_RiggingPhysics)
					{
						_isSelectionLock = true;
					}

				}
				else
				{
					if (MeshGroup != null)
					{
						//Exclusive 모두 해제
						MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
						
						//이전
						//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, false, true, true);

						//변경 20.4.13
						Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																			apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff, 
																			apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);

						RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, false);//<<추가
					}
					_isSelectionLock = false;
				}

				_rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
				Editor.Gizmos.EndBrush();

				AutoSelectModMeshOrModBone();//<<추가

				//추가 19.7.27 : 본의 RigLock을 해제
				Editor.Controller.ResetBoneRigLock(MeshGroup);
			}
			else
			{
				//2. 일반 Modifier일때
				EX_EDIT nextResult = EX_EDIT.None;
				if (_exclusiveEditing == EX_EDIT.None && IsExEditable)
				{
					//None -> ExOnly로 바꾼다.
					//General은 특별한 경우
					nextResult = EX_EDIT.ExOnly_Edit;
				}
				//if (IsExEditable || !isNextResult)
				//{
				//	//SetModifierExclusiveEditing(isNextResult);
				//}
				SetModifierExclusiveEditing(nextResult);
				if (nextResult == EX_EDIT.ExOnly_Edit)
				{
					//변경 3.23 : 선택 잠금을 무조건 켜는게 아니라, 에디터 설정에 따라 켤지 말지 결정한다.
					//true 또는 변경 없음 (false가 아님)
					//모디파이어의 종류에 따라서 다른 옵션을 적용
					if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Physic)
					{
						if (Editor._isSelectionLockOption_RiggingPhysics)
						{
							_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
						}
					}
					else if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Morph ||
						Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.AnimatedMorph)
					{
						if (Editor._isSelectionLockOption_Morph)
						{
							_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
						}
					}
					else
					{
						if (Editor._isSelectionLockOption_Transform)
						{
							_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
						}
					}

				}
				else
				{
					_isSelectionLock = false;//Editing 해제시 Lock 해제
				}
			}


			Editor.RefreshControllerAndHierarchy(false);
		}

		/// <summary>
		/// 단축키 [S]에 의해서도 SelectionLock(Modifier)를 바꿀 수 있다.
		/// </summary>
		public void OnHotKeyEvent_ToggleExclusiveEditKeyLock(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				return;
			}
			_isSelectionLock = !_isSelectionLock;
		}

		/// <summary>
		/// 단축키 [D]에 의해서 ModifierLock(Modifier)을 바꿀 수 있다.
		/// </summary>
		/// <param name="paramObject"></param>
		public void OnHotKeyEvent_ToggleExclusiveModifierLock(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				return;
			}

			if (IsExEditable && _exclusiveEditing != EX_EDIT.None)
			{
				//None이 아닐때
				//General <-> Exclusive 사이에서 토글
				EX_EDIT nextEditMode = EX_EDIT.ExOnly_Edit;
				if (_exclusiveEditing == EX_EDIT.ExOnly_Edit)
				{
					nextEditMode = EX_EDIT.General_Edit;
				}
				SetModifierExclusiveEditing(nextEditMode);//<<이거 수정해야한다.
			}
		}





		public void SetModifierExclusiveEditKeyLock(bool isLock)
		{
			if (_selectionType != SELECTION_TYPE.MeshGroup
				|| _modifier == null
				|| _exEditKeyValue == EX_EDIT_KEY_VALUE.None)
			{
				_isSelectionLock = false;
				return;
			}
			_isSelectionLock = isLock;
		}

		//이 함수가 호출되면 첫번째 RootUnit을 자동으로 호출한다.
		public void SetOverallDefault()
		{
			if (_portrait == null)
			{
				return;
			}

			if (_portrait._rootUnits.Count == 0)
			{
				SetNone();

				Editor.Gizmos.Unlink();
				//Editor.RefreshControllerAndHierarchy(true);
				//Editor.RefreshTimelineLayers(true);
				return;
			}

			//첫번째 유닛을 호출
			SetOverall(_portrait._rootUnits[0]);
		}

		public void SetOverall(apRootUnit rootUnit)
		{
			SetNone();

			if (_rootUnit != rootUnit)
			{
				_curRootUnitAnimClip = null;
			}

			_rootUnitAnimClips.Clear();

			_selectionType = SELECTION_TYPE.Overall;

			_exAnimEditingMode = EX_EDIT.None;
			_isAnimSelectionLock = false;
			SetModifierExclusiveEditing(EX_EDIT.None);
			SetModifierExclusiveEditKeyLock(false);
			SetModifierEditMode(EX_EDIT_KEY_VALUE.None);

			_rigEdit_isBindingEdit = false;
			_rigEdit_isTestPosing = false;



			if (rootUnit != null)
			{
				//Debug.LogError(">>>> Select RootUnit [" + rootUnit.Name + "] >>>>>>");

				_rootUnit = rootUnit;

				//이 RootUnit에 적용할 AnimClip이 뭐가 있는지 확인하자
				for (int i = 0; i < _portrait._animClips.Count; i++)
				{
					apAnimClip animClip = _portrait._animClips[i];
					if (_rootUnit._childMeshGroup == animClip._targetMeshGroup)
					{
						_rootUnitAnimClips.Add(animClip);//<<연동되는 AnimClip이다.
					}
				}

				if (_rootUnit._childMeshGroup != null)
				{
					//Mesh Group을 선택하면 이 초기화를 전부 실행해야한다.
					//이전 > 이렇게 작성하는건 안전하지만 무의미한 코드도 실행된다.
					//_rootUnit._childMeshGroup.SetDirtyToReset();
					//_rootUnit._childMeshGroup.SetDirtyToSort();
					//_rootUnit._childMeshGroup.RefreshForce(true);

					//_rootUnit._childMeshGroup.LinkModMeshRenderUnits();
					//_rootUnit._childMeshGroup.RefreshModifierLink();

					//변경 20.4.4 : 아래와 같이 호출하자
					apMeshGroup rootMeshGroup = _rootUnit._childMeshGroup;
					
					apUtil.LinkRefresh.Set_MeshGroup_AllModifiers(rootMeshGroup);
					rootMeshGroup.SetDirtyToReset();//<<이게 있어야 마스크 메시가 제대로 설정된다.
					rootMeshGroup.RefreshForce(true, 0.0f, apUtil.LinkRefresh);
					
					//rootMeshGroup.RefreshForce(true, 0.0f, apUtil.LinkRefresh.Set_AllObjects(rootMeshGroup));
					rootMeshGroup.RefreshModifierLink(apUtil.LinkRefresh);



					_rootUnit._childMeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					_rootUnit._childMeshGroup._modifierStack.InitModifierCalculatedValues();//<<값 초기화

					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_rootUnit._childMeshGroup, true, true, true);

					//변경 20.4.13
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	_rootUnit._childMeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.ResetForceAndNoSync, 
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);

					_rootUnit._childMeshGroup._modifierStack.RefreshAndSort(true);
					_rootUnit._childMeshGroup.ResetBoneGUIVisible();


					RefreshMeshGroupExEditingFlags(_rootUnit._childMeshGroup, null, null, null, false);//<<추가
					_isSelectionLock = false;

				}

				//Debug.LogError("--------------------------------------------------");
			}

			if (_curRootUnitAnimClip != null)
			{
				if (!_rootUnitAnimClips.Contains(_curRootUnitAnimClip))
				{
					_curRootUnitAnimClip = null;//<<이건 포함되지 않습니더
				}
			}

			if (_curRootUnitAnimClip != null)
			{
				_curRootUnitAnimClip._isSelectedInEditor = true;
			}


			Editor.Gizmos.Unlink();

			//통계 재계산 요청
			SetStatisticsRefresh();

		}





		public void SetParam(apControlParam controlParam)
		{
			SetNone();

			_selectionType = SELECTION_TYPE.Param;

			_param = controlParam;

			//통계 재계산 요청
			SetStatisticsRefresh();
		}

		public void SetAnimClip(apAnimClip animClip)
		{
			SetNone();

			if (_selectionType != SELECTION_TYPE.Animation
				|| _animClip != animClip
				|| _animClip == null)
			{
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.All, null);
			}

			bool isResetInfo = false;

			for (int i = 0; i < Editor._portrait._animClips.Count; i++)
			{
				Editor._portrait._animClips[i]._isSelectedInEditor = false;
			}

			bool isChanged = false;
			if (_animClip != animClip)
			{
				_animClip = animClip;



				_subAnimTimeline = null;
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;
				_subAnimWorkKeyframe = null;

				_subMeshTransformOnAnimClip = null;
				_subMeshGroupTransformOnAnimClip = null;
				_subControlParamOnAnimClip = null;

				_subAnimKeyframeList.Clear();
				//_isAnimEditing = false;
				_exAnimEditingMode = EX_EDIT.None;
				//_isAnimAutoKey = false;
				_isAnimSelectionLock = false;

				_subAnimCommonKeyframeList.Clear();
				_subAnimCommonKeyframeList_Selected.Clear();

				_animTimelineCommonCurve.Clear();//추가 3.30

				_modMeshOfAnim = null;
				_modBoneOfAnim = null;
				_renderUnitOfAnim = null;
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();

				isResetInfo = true;
				isChanged = true;


				if (_animClip._targetMeshGroup != null)
				{
					//Debug.LogError(">>>> Select Anim Clip [" + _animClip._name + "] >>>>>>");
					
					//Mesh Group을 선택하면 이 초기화를 전부 실행해야한다.
					
					
					
					//이전 > 이렇게 작성하는건 안전하지만 무의미한 코드도 실행된다.
					//_animClip._targetMeshGroup.SetDirtyToReset();
					//_animClip._targetMeshGroup.SetDirtyToSort();
					//_animClip._targetMeshGroup.RefreshForce(true);
					
					//변경 20.4.3 : 이렇게 직접 호출하자
					apUtil.LinkRefresh.Set_AnimClip(_animClip);

					_animClip._targetMeshGroup.SetDirtyToReset();
					_animClip._targetMeshGroup.RefreshForce(true, 0.0f, apUtil.LinkRefresh);//<<이게 맞다

					//이전
					//_animClip._targetMeshGroup.LinkModMeshRenderUnits();
					//_animClip._targetMeshGroup.RefreshModifierLink();
					
					//변경 20.4.3
					//_animClip._targetMeshGroup.LinkModMeshRenderUnits(_animClip);//>이 함수는 RefreshForce(true..) > ResetRenderUnits에 포함되어 있다.
					_animClip._targetMeshGroup.RefreshModifierLink(apUtil.LinkRefresh);

					_animClip._targetMeshGroup._modifierStack.InitModifierCalculatedValues();//<<값 초기화

					_animClip._targetMeshGroup._modifierStack.RefreshAndSort(true);



					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_animClip._targetMeshGroup, true, true, true);
					//_animClip._targetMeshGroup.ResetBoneGUIVisible();

					//변경 20.4.13 : VisibilityController를 이용하여 작업용 출력 여부를 초기화 및 복구하자
					//동기화 후 옵션에 따라 결정
					Editor.VisiblityController.SyncMeshGroup(_animClip._targetMeshGroup);
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	_animClip._targetMeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.RestoreByOption, 
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);



					//변경 20.4.3 : 위에서 RefreshForce를 하는 코드를 지우고 여기서 갱신을 한다.
					//_animClip._targetMeshGroup.RefreshForce(true);


					//Debug.LogError("--------------------------------------------------");
				}

				_animClip.Pause_Editor();

				Editor.Gizmos.RevertFFDTransformForce();

			}
			//else
			//{
			//	//같은 거라면?
			//	//패스
			//}
			_animClip = animClip;
			_animClip._isSelectedInEditor = true;

			_selectionType = SELECTION_TYPE.Animation;
			//_prevAnimClipName = _animClip._name;

			if (isChanged && _animClip != null)
			{
				//타임라인을 자동으로 선택해주자
				if (_animClip._timelines.Count > 0)
				{
					apAnimTimeline firstTimeline = _animClip._timelines[0];
					SetAnimTimeline(firstTimeline, true, true, false);
				}
			}

			AutoSelectAnimWorkKeyframe();

			if (isResetInfo)
			{
				//Sync를 한번 돌려주자
				_animPropertyUI = ANIM_SINGLE_PROPERTY_UI.Value;
				_animPropertyCurveUI = ANIM_SINGLE_PROPERTY_CURVE_UI.Next;
				_animPropertyCurveUI_Multi = ANIM_MULTI_PROPERTY_CURVE_UI.Next;
				Editor.Controller.AddAndSyncAnimClipToModifier(_animClip);//<<여기서 Modifier의 Ex 설정을 한다.
			}

			Editor.RefreshTimelineLayers((isResetInfo ? apEditor.REFRESH_TIMELINE_REQUEST.All : apEditor.REFRESH_TIMELINE_REQUEST.Timelines | apEditor.REFRESH_TIMELINE_REQUEST.LinkKeyframeAndModifier),
											null
											);

			Editor.Hierarchy_AnimClip.ResetSubUnits();
			Editor.Hierarchy_AnimClip.RefreshUnits();

			//Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Modifier_Morph());

			SetAnimClipGizmoEvent(isResetInfo);//Gizmo 이벤트 연결
			RefreshAnimEditing(true);

			//통계 재계산 요청
			SetStatisticsRefresh();

			//Common Keyframe을 갱신하자
			RefreshCommonAnimKeyframes();
		}

		/// <summary>
		/// AnimClip 상태에서 현재 상태에 맞는 GizmoEvent를 등록한다.
		/// </summary>
		private void SetAnimClipGizmoEvent(bool isForceReset)
		{
			if (_animClip == null)
			{
				Editor.Gizmos.Unlink();
				return;
			}

			if (isForceReset)
			{
				Editor.Gizmos.Unlink();

			}

			if (AnimTimeline == null)
			{
				//타임라인이 없으면 선택만 가능하다
				Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet__Animation_OnlySelectTransform());
			}
			else
			{
				switch (AnimTimeline._linkType)
				{
					case apAnimClip.LINK_TYPE.AnimatedModifier:
						if (AnimTimeline._linkedModifier != null)
						{
							if ((int)(AnimTimeline._linkedModifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.VertexPos) != 0)
							{
								//Vertex와 관련된 Modifier다.
								Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet__Animation_EditVertex());
							}
							else
							{
								//Transform과 관련된 Modifier다.
								Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet__Animation_EditTransform());
							}
						}
						else
						{
							Debug.LogError("Error : 선택된 Timeline의 Modifier가 연결되지 않음");
							Editor.Gizmos.Unlink();
						}
						break;

					//이거 삭제하고, 
					//GetEventSet__Animation_EditTransform에서 Bone을 제어하는 코드를 추가하자
					//case apAnimClip.LINK_TYPE.Bone:
					//	Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet__Animation_EditBone());
					//	break;

					case apAnimClip.LINK_TYPE.ControlParam:
						//Control Param일땐 선택만 가능
						Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet__Animation_OnlySelectTransform());
						break;

					default:
						Debug.LogError("TODO : 알 수 없는 Timeline LinkType [" + AnimTimeline._linkType + "]");
						Editor.Gizmos.Unlink();
						break;
				}
			}

		}


		/// <summary>
		/// Animation 편집시 - AnimClip -> Timeline 을 선택한다. (단일 선택)
		/// </summary>
		/// <param name="timeLine"></param>
		public void SetAnimTimeline(apAnimTimeline timeLine,
										bool isKeyframeSelectReset,
										bool isIgnoreLock = false,
										bool isAutoChangeLeftTab = true)
		{
			//통계 재계산 요청
			SetStatisticsRefresh();

			if (!isIgnoreLock)
			{
				//현재 작업중 + Lock이 걸리면 바꾸지 못한다.
				if (ExAnimEditingMode != EX_EDIT.None && IsAnimSelectionLock)
				{
					return;
				}
			}



			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null ||
				timeLine == null ||
				!_animClip.IsTimelineContain(timeLine))
			{
				_subAnimTimeline = null;
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;
				_subAnimWorkKeyframe = null;

				_subAnimKeyframeList.Clear();
				_exAnimEditingMode = EX_EDIT.None;
				//_isAnimAutoKey = false;
				_isAnimSelectionLock = false;

				_animTimelineCommonCurve.Clear();//추가 3.30

				_modMeshOfAnim = null;
				_modBoneOfAnim = null;
				_renderUnitOfAnim = null;
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();

				AutoSelectAnimWorkKeyframe();
				RefreshAnimEditing(true);

				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Timelines | apEditor.REFRESH_TIMELINE_REQUEST.LinkKeyframeAndModifier, null);
				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

				//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"

				return;
			}

			if (_subAnimTimeline != timeLine)
			{
				_subAnimTimelineLayer = null;
				_subAnimWorkKeyframe = null;

				if (isKeyframeSelectReset)
				{
					_subAnimKeyframe = null;

					_subAnimKeyframeList.Clear();

					_animTimelineCommonCurve.Clear();//추가 3.30
				}

				_modMeshOfAnim = null;
				_modBoneOfAnim = null;
				_renderUnitOfAnim = null;
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();

				AutoSelectAnimWorkKeyframe();

				//Editing에서 바꿀 수 있으므로 AnimEditing를 갱신한다.
				RefreshAnimEditing(true);

				//스크롤 초기화 (오른쪽2)
				Editor.ResetScrollPosition(false, false, false, true, false);
			}

			_subAnimTimeline = timeLine;


			AutoSelectAnimTimelineLayer(false, isAutoChangeLeftTab);

			Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, null);//<<Timeline을 선택하는 것 만으로는 크게 바뀌는게 없다.

			SetAnimClipGizmoEvent(false);//Gizmo 이벤트 연결

			//추가 : MeshGroup Hierarchy를 갱신합시다.
			Editor.Hierarchy_MeshGroup.RefreshUnits();
			Editor.Hierarchy_AnimClip.RefreshUnits();

			//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"
																										   //Editor.SetGUIVisible("AnimationRight2GUI_Timeline_Layers", false);

			apEditorUtil.ReleaseGUIFocus();
		}

		public void SetAnimTimelineLayer(apAnimTimelineLayer timelineLayer, bool isKeyframeSelectReset, bool isAutoSelectTargetObject = false, bool isIgnoreLock = false)
		{
			apAnimTimeline prevTimeline = _subAnimTimeline;

			//처리 후 이전 레이어
			//통계 재계산 요청
			SetStatisticsRefresh();

			//현재 작업중+Lock이 걸리면 바꾸지 못한다.
			if (!isIgnoreLock)
			{
				if (ExAnimEditingMode != EX_EDIT.None && IsAnimSelectionLock)
				{
					return;
				}
			}

			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null ||
				_subAnimTimeline == null ||
				timelineLayer == null ||
				!_subAnimTimeline.IsTimelineLayerContain(timelineLayer)
				)
			{
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;

				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();//추가 3.30

				_modMeshOfAnim = null;
				_modBoneOfAnim = null;
				_renderUnitOfAnim = null;
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();

				AutoSelectAnimWorkKeyframe();

				//Editing에서 바꿀 수 있으므로 AnimEditing를 갱신한다.
				RefreshAnimEditing(true);

				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.All, null);
				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

				return;
			}



			if (_subAnimTimelineLayer != timelineLayer && isKeyframeSelectReset)
			{
				_subAnimKeyframe = null;
				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();//추가 3.30

				_subAnimTimelineLayer = timelineLayer;



				AutoSelectAnimWorkKeyframe();

				RefreshAnimEditing(true);
			}

			_subAnimTimelineLayer = timelineLayer;

			if (isAutoSelectTargetObject)
			{
				AutoSelectAnimTargetObject();
			}

			//선택하는 것 만으로는 변경되는게 없다.
			Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, null);

			SetAnimClipGizmoEvent(false);//Gizmo 이벤트 연결

			//만약 처리 이전-이후의 타임라인이 그대로라면 GUI가 깜빡이는걸 막자
			if (prevTimeline == _subAnimTimeline)
			{
				_isIgnoreAnimTimelineGUI = true;
			}

			apEditorUtil.ReleaseGUIFocus();
		}

		/// <summary>
		/// Timeline GUI에서 Keyframe을 선택한다.
		/// AutoSelect를 켜면 선택한 Keyframe에 맞게 다른 TimelineLayer / Timeline을 선택한다.
		/// 단일 선택이므로 "다중 선택"은 항상 현재 선택한 것만 가지도록 한다.
		/// </summary>
		/// <param name="keyframe"></param>
		/// <param name="isTimelineAutoSelect"></param>
		public void SetAnimKeyframe(apAnimKeyframe keyframe, bool isTimelineAutoSelect, apGizmos.SELECT_TYPE selectType, bool isSelectLoopDummy = false)
		{
			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null)
			{
				_subAnimTimeline = null;
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;

				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();//추가 3.30

				AutoSelectAnimWorkKeyframe();//<Work+Mod 자동 연결

				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.All, null);
				return;
			}

			apAnimTimeline prevTimeline = _subAnimTimeline;

			if (selectType != apGizmos.SELECT_TYPE.New)
			{
				List<apAnimKeyframe> singleKeyframes = new List<apAnimKeyframe>();
				if (keyframe != null)
				{
					singleKeyframes.Add(keyframe);
				}

				SetAnimMultipleKeyframe(singleKeyframes, selectType, isTimelineAutoSelect);
				return;
			}

			if (keyframe == null)
			{
				_subAnimKeyframe = null;
				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();//추가 3.30

				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결
				return;
			}

			bool isKeyframeChanged = (keyframe != _subAnimKeyframe);

			if (isTimelineAutoSelect)
			{

				//Layer가 선택되지 않았거나, 선택된 Layer에 포함되지 않을 때
				apAnimTimelineLayer parentLayer = keyframe._parentTimelineLayer;
				if (parentLayer == null)
				{
					_subAnimKeyframe = null;
					_subAnimKeyframeList.Clear();

					_animTimelineCommonCurve.Clear();//추가 3.30

					AutoSelectAnimWorkKeyframe();
					return;
				}
				apAnimTimeline parentTimeline = parentLayer._parentTimeline;
				if (parentTimeline == null || !_animClip.IsTimelineContain(parentTimeline))
				{
					//유효하지 않은 타임라인일때
					_subAnimKeyframe = null;
					_subAnimKeyframeList.Clear();

					_animTimelineCommonCurve.Clear();//추가 3.30

					SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

					AutoSelectAnimWorkKeyframe();
					return;
				}

				//자동으로 체크해주자
				_subAnimTimeline = parentTimeline;
				_subAnimTimelineLayer = parentLayer;

				_subAnimKeyframe = keyframe;

				_subAnimKeyframeList.Clear();
				_subAnimKeyframeList.Add(keyframe);

				_animTimelineCommonCurve.Clear();//추가 3.30


				AutoSelectAnimWorkKeyframe();
				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, parentLayer);//<<변경 19.5.21 : 선택하는 것 만으로는 변경되지 않도록
			}
			else
			{
				//TimelineLayer에 있는 키프레임만 선택할 때
				if (_subAnimTimeline == null ||
					_subAnimTimelineLayer == null)
				{
					_subAnimKeyframe = null;
					_subAnimKeyframeList.Clear();

					_animTimelineCommonCurve.Clear();//추가 3.30

					SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결
					return;//처리 못함
				}


				if (_subAnimTimelineLayer.IsKeyframeContain(keyframe))
				{
					//Layer에 포함된 Keyframe이다.

					_subAnimKeyframe = keyframe;
					_subAnimKeyframeList.Clear();
					_subAnimKeyframeList.Add(_subAnimKeyframe);
				}
				else
				{
					//Layer에 포함되지 않은 Keyframe이다. => 처리 못함
					_subAnimKeyframe = null;
					_subAnimKeyframeList.Clear();
				}
				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결


				_animTimelineCommonCurve.Clear();//추가 3.30

			}

			_subAnimKeyframe._parentTimelineLayer.SortAndRefreshKeyframes();


			//키프레임 선택시 자동으로 Frame을 이동한다.
			if (_subAnimKeyframe != null)
			{
				int selectedFrameIndex = _subAnimKeyframe._frameIndex;
				if (_animClip.IsLoop &&
					(selectedFrameIndex < _animClip.StartFrame || selectedFrameIndex > _animClip.EndFrame))
				{
					selectedFrameIndex = _subAnimKeyframe._loopFrameIndex;
				}

				if (selectedFrameIndex >= _animClip.StartFrame
					&& selectedFrameIndex <= _animClip.EndFrame)
				{
					_animClip.SetFrame_Editor(selectedFrameIndex);
					Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
				}


				SetAutoAnimScroll();
			}

			if (isKeyframeChanged)
			{
				AutoSelectAnimTargetObject();
			}

			AutoSelectAnimWorkKeyframe();//<Work+Mod 자동 연결
			SetAnimClipGizmoEvent(isKeyframeChanged);//Gizmo 이벤트 연결

			//Common Keyframe을 갱신하자
			RefreshCommonAnimKeyframes();

			//만약 처리 이전-이후의 타임라인이 그대로라면 GUI가 깜빡이는걸 막자
			if (prevTimeline == _subAnimTimeline)
			{
				_isIgnoreAnimTimelineGUI = true;
			}

			apEditorUtil.ReleaseGUIFocus();
		}


		/// <summary>
		/// Keyframe 다중 선택을 한다.
		/// 이때는 Timeline, Timelinelayer는 변동이 되지 않는다. (다만 다중 선택시에는 Timeline, Timelinelayer를 별도로 수정하지 못한다)
		/// </summary>
		/// <param name="keyframes"></param>
		/// <param name="selectType"></param>
		public void SetAnimMultipleKeyframe(List<apAnimKeyframe> keyframes, apGizmos.SELECT_TYPE selectType, bool isTimelineAutoSelect)
		{
			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null)
			{
				_subAnimTimeline = null;
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;

				_subAnimKeyframeList.Clear();

				AutoSelectAnimWorkKeyframe();//<Work+Mod 자동 연결

				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

				_animTimelineCommonCurve.Clear();//추가 3.30

				return;
			}

			apAnimKeyframe curKeyframe = null;
			if (selectType == apGizmos.SELECT_TYPE.New)
			{
				_subAnimWorkKeyframe = null;
				_subAnimKeyframe = null;
				_subAnimKeyframeList.Clear();
			}

			_animTimelineCommonCurve.Clear();//추가 3.30

			//공통의 타임라인을 가지는가
			apAnimTimeline commonTimeline = null;
			apAnimTimelineLayer commonTimelineLayer = null;



			if (isTimelineAutoSelect)
			{
				List<apAnimKeyframe> checkCommonKeyframes = new List<apAnimKeyframe>();
				if (selectType == apGizmos.SELECT_TYPE.Add ||
					selectType == apGizmos.SELECT_TYPE.New)
				{
					for (int i = 0; i < keyframes.Count; i++)
					{
						checkCommonKeyframes.Add(keyframes[i]);
					}
				}

				if (selectType == apGizmos.SELECT_TYPE.Add ||
					selectType == apGizmos.SELECT_TYPE.Subtract)
				{
					//기존에 선택했던것도 추가하자
					for (int i = 0; i < _subAnimKeyframeList.Count; i++)
					{
						checkCommonKeyframes.Add(_subAnimKeyframeList[i]);
					}
				}

				if (selectType == apGizmos.SELECT_TYPE.Subtract)
				{
					//기존에 선택했던 것에서 빼자
					for (int i = 0; i < keyframes.Count; i++)
					{
						checkCommonKeyframes.Remove(keyframes[i]);
					}
				}


				for (int i = 0; i < checkCommonKeyframes.Count; i++)
				{
					curKeyframe = checkCommonKeyframes[i];
					if (commonTimelineLayer == null)
					{
						commonTimelineLayer = curKeyframe._parentTimelineLayer;
						commonTimeline = commonTimelineLayer._parentTimeline;
					}
					else
					{
						if (commonTimelineLayer != curKeyframe._parentTimelineLayer)
						{
							commonTimelineLayer = null;
							break;
						}
					}
				}
			}

			for (int i = 0; i < keyframes.Count; i++)
			{
				curKeyframe = keyframes[i];
				if (curKeyframe == null ||
					curKeyframe._parentTimelineLayer == null ||
					curKeyframe._parentTimelineLayer._parentAnimClip != _animClip)
				{
					continue;
				}

				if (selectType == apGizmos.SELECT_TYPE.Add ||
					selectType == apGizmos.SELECT_TYPE.New)
				{
					//Debug.Log("Add");
					if (!_subAnimKeyframeList.Contains(curKeyframe))
					{
						_subAnimKeyframeList.Add(curKeyframe);
					}
				}
				else
				{
					_subAnimKeyframeList.Remove(curKeyframe);
				}
			}

			if (_subAnimKeyframeList.Count > 0)
			{
				if (!_subAnimKeyframeList.Contains(_subAnimKeyframe))
				{
					_subAnimKeyframe = _subAnimKeyframeList[0];
				}
			}
			else
			{
				_subAnimKeyframe = null;
			}

			if (isTimelineAutoSelect)
			{

				if (commonTimelineLayer != null)
				{
					if (commonTimelineLayer != _subAnimTimelineLayer)
					{
						_subAnimTimelineLayer = commonTimelineLayer;

						if (ExAnimEditingMode == EX_EDIT.None)
						{
							_subAnimTimeline = commonTimeline;
						}

						Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, commonTimelineLayer);
					}
				}
				else
				{
					_subAnimTimelineLayer = null;
					if (ExAnimEditingMode == EX_EDIT.None)
					{
						_subAnimTimeline = null;
					}

					Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, null);
				}


			}
			else
			{
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, null);
			}

			List<apAnimTimelineLayer> refreshLayer = new List<apAnimTimelineLayer>();
			for (int i = 0; i < _subAnimKeyframeList.Count; i++)
			{
				if (!refreshLayer.Contains(_subAnimKeyframeList[i]._parentTimelineLayer))
				{
					refreshLayer.Add(_subAnimKeyframeList[i]._parentTimelineLayer);
				}
			}
			for (int i = 0; i < refreshLayer.Count; i++)
			{
				refreshLayer[i].SortAndRefreshKeyframes();
			}



			//키프레임 선택시 자동으로 Frame을 이동한다.
			//단, 공통 프레임이 있는 경우에만 이동한다.
			if (_subAnimKeyframeList.Count > 0 && selectType == apGizmos.SELECT_TYPE.New)
			{
				bool isCommonKeyframe = true;

				int selectedFrameIndex = -1;
				for (int iKey = 0; iKey < _subAnimKeyframeList.Count; iKey++)
				{
					apAnimKeyframe subKeyframe = _subAnimKeyframeList[iKey];
					if (iKey == 0)
					{
						selectedFrameIndex = subKeyframe._frameIndex;
						isCommonKeyframe = true;
					}
					else
					{
						if (subKeyframe._frameIndex != selectedFrameIndex)
						{
							//선택한 키프레임이 다 다르군요. 자동 이동 포기
							isCommonKeyframe = false;
							break;
						}
					}

				}
				if (isCommonKeyframe)
				{
					//모든 키프레임이 공통의 프레임을 갖는다.
					//이동하자
					if (_animClip.IsLoop &&
						(selectedFrameIndex < _animClip.StartFrame || selectedFrameIndex > _animClip.EndFrame))
					{
						selectedFrameIndex = _subAnimKeyframe._loopFrameIndex;
					}

					if (selectedFrameIndex >= _animClip.StartFrame
						&& selectedFrameIndex <= _animClip.EndFrame)
					{
						_animClip.SetFrame_Editor(selectedFrameIndex);
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}


					SetAutoAnimScroll();
				}
			}


			AutoSelectAnimTargetObject();

			AutoSelectAnimWorkKeyframe();//<Work+Mod 자동 연결

			SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

			//Common Keyframe을 갱신하자
			RefreshCommonAnimKeyframes();

			//추가 3.30 : 여러개의 키프레임들을 선택했을 때, CommonCurve도 갱신하자
			_animTimelineCommonCurve.Clear();//추가 3.30
			if (_subAnimKeyframeList.Count > 1)
			{
				_animTimelineCommonCurve.SetKeyframes(_subAnimKeyframeList);
			}

			apEditorUtil.ReleaseGUIFocus();
		}

		public void AutoRefreshCommonCurve()
		{
			//추가 3.30 : 여러개의 키프레임들을 선택했을 때, CommonCurve도 갱신하자
			_animTimelineCommonCurve.Clear();//추가 3.30
			if (_subAnimKeyframeList != null && _subAnimKeyframeList.Count > 1)
			{
				_animTimelineCommonCurve.SetKeyframes(_subAnimKeyframeList);
			}
		}


		private void AutoSelectAnimTargetObject()
		{
			//자동으로 타겟을 정하자
			_subControlParamOnAnimClip = null;
			_subMeshTransformOnAnimClip = null;
			_subMeshGroupTransformOnAnimClip = null;


			if (_subAnimTimelineLayer != null && _subAnimTimelineLayer._parentTimeline != null)
			{
				apAnimTimeline parentTimeline = _subAnimTimelineLayer._parentTimeline;
				switch (parentTimeline._linkType)
				{
					case apAnimClip.LINK_TYPE.AnimatedModifier:
						{
							switch (_subAnimTimelineLayer._linkModType)
							{
								case apAnimTimelineLayer.LINK_MOD_TYPE.MeshTransform:
									if (_subAnimTimelineLayer._linkedMeshTransform != null)
									{
										_subMeshTransformOnAnimClip = _subAnimTimelineLayer._linkedMeshTransform;
									}
									break;

								case apAnimTimelineLayer.LINK_MOD_TYPE.MeshGroupTransform:
									if (_subAnimTimelineLayer._linkedMeshGroupTransform != null)
									{
										_subMeshGroupTransformOnAnimClip = _subAnimTimelineLayer._linkedMeshGroupTransform;
									}
									break;

								case apAnimTimelineLayer.LINK_MOD_TYPE.Bone:
									if (_subAnimTimelineLayer._linkedBone != null)
									{
										_bone = _subAnimTimelineLayer._linkedBone;
									}
									break;

								case apAnimTimelineLayer.LINK_MOD_TYPE.None:
									break;
							}
						}
						break;


					case apAnimClip.LINK_TYPE.ControlParam:
						if (_subAnimTimelineLayer._linkedControlParam != null)
						{
							_subControlParamOnAnimClip = _subAnimTimelineLayer._linkedControlParam;
						}
						break;

					default:
						Debug.LogError("에러 : 알 수 없는 타입 : [" + parentTimeline._linkType + "]");
						break;
				}
			}
		}

		//---------------------------------------------------------------

		/// <summary>
		/// Keyframe의 변동사항이 있을때 Common Keyframe을 갱신한다.
		/// </summary>
		public void RefreshCommonAnimKeyframes()
		{


			if (_animClip == null)
			{
				_subAnimCommonKeyframeList.Clear();
				_subAnimCommonKeyframeList_Selected.Clear();
				return;
			}

			//0. 전체 Keyframe과 FrameIndex를 리스트로 모은다.
			List<int> commFrameIndexList = new List<int>();
			List<apAnimKeyframe> totalKeyframes = new List<apAnimKeyframe>();
			apAnimTimeline timeline = null;
			apAnimTimelineLayer timelineLayer = null;
			apAnimKeyframe keyframe = null;
			for (int iTimeline = 0; iTimeline < _animClip._timelines.Count; iTimeline++)
			{
				timeline = _animClip._timelines[iTimeline];
				for (int iLayer = 0; iLayer < timeline._layers.Count; iLayer++)
				{
					timelineLayer = timeline._layers[iLayer];
					for (int iKeyframe = 0; iKeyframe < timelineLayer._keyframes.Count; iKeyframe++)
					{
						keyframe = timelineLayer._keyframes[iKeyframe];

						//키프레임과 프레임 인덱스를 저장
						totalKeyframes.Add(keyframe);

						if (!commFrameIndexList.Contains(keyframe._frameIndex))
						{
							commFrameIndexList.Add(keyframe._frameIndex);
						}
					}
				}
			}

			//기존의 AnimCommonKeyframe에서 불필요한 것들을 먼저 없애고, 일단 Keyframe을 클리어한다.
			_subAnimCommonKeyframeList.RemoveAll(delegate (apAnimCommonKeyframe a)
			{
				//공통적으로 존재하지 않는 FrameIndex를 가진다면 삭제
				return !commFrameIndexList.Contains(a._frameIndex);
			});

			for (int i = 0; i < _subAnimCommonKeyframeList.Count; i++)
			{
				_subAnimCommonKeyframeList[i].Clear();
				_subAnimCommonKeyframeList[i].ReadyToAdd();
			}




			//1. Keyframe들의 공통 Index를 먼저 가져온다.
			for (int iKF = 0; iKF < totalKeyframes.Count; iKF++)
			{
				keyframe = totalKeyframes[iKF];

				apAnimCommonKeyframe commonKeyframe = GetCommonKeyframe(keyframe._frameIndex);

				if (commonKeyframe == null)
				{
					commonKeyframe = new apAnimCommonKeyframe(keyframe._frameIndex);
					commonKeyframe.ReadyToAdd();

					_subAnimCommonKeyframeList.Add(commonKeyframe);
				}

				//Common Keyframe에 추가한다.
				commonKeyframe.AddAnimKeyframe(keyframe, _subAnimKeyframeList.Contains(keyframe));
			}


			_subAnimCommonKeyframeList_Selected.Clear();

			//선택된 Common Keyframe만 처리한다.
			for (int i = 0; i < _subAnimCommonKeyframeList.Count; i++)
			{
				if (_subAnimCommonKeyframeList[i]._isSelected)
				{
					_subAnimCommonKeyframeList_Selected.Add(_subAnimCommonKeyframeList[i]);
				}
			}



		}

		public apAnimCommonKeyframe GetCommonKeyframe(int frameIndex)
		{
			return _subAnimCommonKeyframeList.Find(delegate (apAnimCommonKeyframe a)
			{
				return a._frameIndex == frameIndex;
			});
		}


		public void SetAnimCommonKeyframe(apAnimCommonKeyframe commonKeyframe, apGizmos.SELECT_TYPE selectType)
		{
			List<apAnimCommonKeyframe> commonKeyframes = new List<apAnimCommonKeyframe>();
			commonKeyframes.Add(commonKeyframe);
			SetAnimCommonKeyframes(commonKeyframes, selectType);
		}

		/// <summary>
		/// SetAnimKeyframe과 비슷하지만 CommonKeyframe을 선택하여 다중 선택을 한다.
		/// SelectionType에 따라서 다르게 처리를 한다.
		/// TimelineAutoSelect는 하지 않는다.
		/// </summary>
		public void SetAnimCommonKeyframes(List<apAnimCommonKeyframe> commonKeyframes, apGizmos.SELECT_TYPE selectType)
		{
			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null)
			{
				_subAnimTimeline = null;
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;

				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();

				_subAnimCommonKeyframeList.Clear();
				_subAnimCommonKeyframeList_Selected.Clear();

				AutoSelectAnimWorkKeyframe();//<Work+Mod 자동 연결

				SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.All, null);
				return;
			}

			if (selectType == apGizmos.SELECT_TYPE.New)
			{

				//New라면 다른 AnimKeyframe은 일단 취소해야하므로..
				_subAnimKeyframe = null;
				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();

				//Refresh는 처리 후 일괄적으로 한다.

				//New에선
				//일단 모든 CommonKeyframe의 Selected를 false로 돌린다.
				for (int i = 0; i < _subAnimCommonKeyframeList.Count; i++)
				{
					_subAnimCommonKeyframeList[i]._isSelected = false;
				}
				_subAnimCommonKeyframeList_Selected.Clear();
			}



			apAnimCommonKeyframe commonKeyframe = null;
			for (int iCK = 0; iCK < commonKeyframes.Count; iCK++)
			{
				commonKeyframe = commonKeyframes[iCK];
				if (selectType == apGizmos.SELECT_TYPE.New ||
					selectType == apGizmos.SELECT_TYPE.Add)
				{

					commonKeyframe._isSelected = true;
					for (int iSubKey = 0; iSubKey < commonKeyframe._keyframes.Count; iSubKey++)
					{
						apAnimKeyframe keyframe = commonKeyframe._keyframes[iSubKey];
						//Add / New에서는 리스트에 더해주자
						if (!_subAnimKeyframeList.Contains(keyframe))
						{
							_subAnimKeyframeList.Add(keyframe);
						}
					}
				}
				else
				{
					//Subtract에서는 선택된 걸 제외한다.
					commonKeyframe._isSelected = false;

					for (int iSubKey = 0; iSubKey < commonKeyframe._keyframes.Count; iSubKey++)
					{
						apAnimKeyframe keyframe = commonKeyframe._keyframes[iSubKey];

						_subAnimKeyframeList.Remove(keyframe);
					}
				}
			}

			if (_subAnimKeyframeList.Count > 0)
			{
				if (!_subAnimKeyframeList.Contains(_subAnimKeyframe))
				{
					_subAnimKeyframe = _subAnimKeyframeList[0];
				}

				if (_subAnimKeyframeList.Count == 1)
				{
					_subAnimTimelineLayer = _subAnimKeyframe._parentTimelineLayer;
					_subAnimTimeline = _subAnimTimelineLayer._parentTimeline;
				}
				else
				{
					_subAnimTimelineLayer = null;
					//_subAnimTimeline//<<이건 건들지 않는다.
				}
			}
			else
			{
				_subAnimKeyframe = null;
			}

			//Common Keyframe을 갱신하자
			RefreshCommonAnimKeyframes();


			Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, null);

			List<apAnimTimelineLayer> refreshLayer = new List<apAnimTimelineLayer>();
			for (int i = 0; i < _subAnimKeyframeList.Count; i++)
			{
				if (!refreshLayer.Contains(_subAnimKeyframeList[i]._parentTimelineLayer))
				{
					refreshLayer.Add(_subAnimKeyframeList[i]._parentTimelineLayer);
				}
			}
			for (int i = 0; i < refreshLayer.Count; i++)
			{
				refreshLayer[i].SortAndRefreshKeyframes();
			}

			AutoSelectAnimWorkKeyframe();//<Work+Mod 자동 연결
			SetAnimClipGizmoEvent(true);//Gizmo 이벤트 연결

			//추가 3.30 : 키프레임들이 여러개 선택된 경우, CommonCurve를 갱신한다.
			_animTimelineCommonCurve.Clear();
			if (_subAnimKeyframeList.Count > 1)
			{
				_animTimelineCommonCurve.SetKeyframes(_subAnimKeyframeList);
			}



		}
		//---------------------------------------------------------------


		private void SetAnimEditingToggle()
		{
			if (ExAnimEditingMode != EX_EDIT.None)
			{
				//>> Off
				//_isAnimEditing = false;
				_exAnimEditingMode = EX_EDIT.None;
				//_isAnimAutoKey = false;
				_isAnimSelectionLock = false;
			}
			else
			{
				if (IsAnimEditable)
				{
					//_isAnimEditing = true;//<<편집 시작!
					//_isAnimAutoKey = false;
					_exAnimEditingMode = EX_EDIT.ExOnly_Edit;//<<배타적 Mod 선택이 기본값이다.

					//변경 3.23 : 선택 잠금을 무조건 켜는게 아니라, 옵션에 따라 켜거나 그대로 둘지 결정한다.
					if (_subAnimTimeline._linkType == apAnimClip.LINK_TYPE.ControlParam)
					{
						if (Editor._isSelectionLockOption_ControlParamTimeline)
						{
							_isAnimSelectionLock = true;//기존의 False에서 True로 변경
						}
					}
					else
					{
						if (_subAnimTimeline._linkedModifier != null)
						{
							if (_subAnimTimeline._linkedModifier.ModifierType == apModifierBase.MODIFIER_TYPE.AnimatedMorph)
							{
								if (Editor._isSelectionLockOption_Morph)
								{
									_isAnimSelectionLock = true;//기존의 False에서 True로 변경
								}
							}
							else
							{
								if (Editor._isSelectionLockOption_Transform)
								{
									_isAnimSelectionLock = true;//기존의 False에서 True로 변경
								}
							}
						}
						else
						{
							//에러 : 모디파이어를 알 수 없다.
							_isAnimSelectionLock = true;//기존의 False에서 True로 변경
						}
					}


					bool isVertexTarget = false;
					bool isControlParamTarget = false;
					bool isTransformTarget = false;
					bool isBoneTarget = false;

					//현재 객체가 현재 Timeline에 맞지 않다면 선택을 해제해야한다.
					if (_subAnimTimeline._linkType == apAnimClip.LINK_TYPE.ControlParam)
					{
						isControlParamTarget = true;
					}
					else if (_subAnimTimeline._linkedModifier != null)
					{
						if ((int)(_subAnimTimeline._linkedModifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.VertexPos) != 0)
						{
							isVertexTarget = true;
							isTransformTarget = true;
						}
						else if (_subAnimTimeline._linkedModifier.IsTarget_Bone)
						{
							isTransformTarget = true;
							isBoneTarget = true;
						}
						else
						{
							isTransformTarget = true;
						}
					}
					else
					{
						//?? 뭘 선택할까요.
						Debug.LogError("Anim Toggle Error : Animation Modifier 타입인데 Modifier가 연결 안됨");
					}

					if (!isVertexTarget)
					{
						_modRenderVertOfAnim = null;
						_modRenderVertListOfAnim.Clear();
					}
					if (!isControlParamTarget)
					{
						_subControlParamOnAnimClip = null;
					}
					if (!isTransformTarget)
					{
						_subMeshTransformOnAnimClip = null;
						_subMeshGroupTransformOnAnimClip = null;
					}
					if (!isBoneTarget)
					{
						_bone = null;
					}


				}
			}


			RefreshAnimEditing(true);

			Editor.RefreshControllerAndHierarchy(false);
			Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Timelines | apEditor.REFRESH_TIMELINE_REQUEST.Info, null);
		}

		public bool SetAnimExclusiveEditing_Tmp(EX_EDIT exEditing, bool isGizmoReset)
		{
			if (!IsAnimEditable && exEditing != EX_EDIT.None)
			{
				//편집중이 아니라면 None으로 강제한다.
				exEditing = EX_EDIT.None;
				return false;
			}


			if (_exAnimEditingMode == exEditing)
			{
				return true;
			}

			_exAnimEditingMode = exEditing;
			//if(_exAnimEditingMode == EX_EDIT.None)
			//{
			//	_isAnimLock = false;
			//}
			//else
			//{
			//	_isAnimLock = true;
			//}


			//Editing 상태에 따라 Refresh 코드가 다르다
			if (ExAnimEditingMode != EX_EDIT.None)
			{

				bool isModLock_ColorUpdate = Editor.GetModLockOption_ColorPreview(ExAnimEditingMode);
				bool isModLock_OtherMod = Editor.GetModLockOption_CalculateIfNotAddedOther(ExAnimEditingMode);


				//현재 선택한 타임라인에 따라서 Modifier를 On/Off할지 결정한다.
				bool isExclusiveActive = false;
				if (_subAnimTimeline != null)
				{
					if (_subAnimTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier)
					{
						if (_subAnimTimeline._linkedModifier != null && _animClip._targetMeshGroup != null)
						{
							if (ExAnimEditingMode == EX_EDIT.ExOnly_Edit)
							{
								//현재의 AnimTimeline에 해당하는 ParamSet만 선택하자
								List<apModifierParamSetGroup> exParamSetGroups = new List<apModifierParamSetGroup>();
								List<apModifierParamSetGroup> linkParamSetGroups = _subAnimTimeline._linkedModifier._paramSetGroup_controller;
								for (int iP = 0; iP < linkParamSetGroups.Count; iP++)
								{
									apModifierParamSetGroup linkPSG = linkParamSetGroups[iP];
									if (linkPSG._keyAnimTimeline == _subAnimTimeline &&
										linkPSG._keyAnimClip == _animClip)
									{
										exParamSetGroups.Add(linkPSG);
									}
								}

								//Debug.Log("Set Anim Editing > Exclusive Enabled [" + _subAnimTimeline._linkedModifier.DisplayName + "]");

								_animClip._targetMeshGroup._modifierStack.SetExclusiveModifierInEditing_MultipleParamSetGroup(
																			_subAnimTimeline._linkedModifier,
																			exParamSetGroups,
																			isModLock_ColorUpdate);
								isExclusiveActive = true;
							}
							else if (ExAnimEditingMode == EX_EDIT.General_Edit)
							{
								//추가 : General Edit 모드
								//선택한 것과 허용되는 Modifier는 모두 허용한다.
								_animClip._targetMeshGroup._modifierStack.SetExclusiveModifierInEditing_MultipleParamSetGroup_General(
																			_subAnimTimeline._linkedModifier,
																			_animClip,
																			isModLock_ColorUpdate,
																			isModLock_OtherMod);
								isExclusiveActive = true;
							}
						}
					}
				}

				if (!isExclusiveActive)
				{
					//Modifier와 연동된게 아니라면
					if (_animClip._targetMeshGroup != null)
					{
						_animClip._targetMeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
						//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_animClip._targetMeshGroup);
						RefreshMeshGroupExEditingFlags(_animClip._targetMeshGroup, null, null, null, false);//<<추가
					}
				}
				else
				{
					RefreshMeshGroupExEditingFlags(_animClip._targetMeshGroup, _subAnimTimeline._linkedModifier, null, _animClip, false);//<<추가
				}
			}
			else
			{
				//모든 Modifier의 Exclusive 선택을 해제하고 모두 활성화한다.
				if (_animClip._targetMeshGroup != null)
				{
					_animClip._targetMeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_animClip._targetMeshGroup);
					RefreshMeshGroupExEditingFlags(_animClip._targetMeshGroup, null, null, null, false);//<<추가
				}
			}

			return true;
		}


		/// <summary>
		/// Mod Lock을 갱신한다.
		/// Animation Clip 선택시 이걸 호출한다.
		/// SetAnimEditingLayerLockToggle() 함수를 다시 호출한 것과 같다.
		/// </summary>
		public void RefreshAnimEditingLayerLock()
		{
			if (_animClip == null ||
				SelectionType != SELECTION_TYPE.Animation)
			{
				return;
			}

			if (ExAnimEditingMode == EX_EDIT.None)
			{
				_exAnimEditingMode = EX_EDIT.None;
			}

			RefreshAnimEditing(true);
		}

		private void SetAnimEditingLayerLockToggle()
		{
			if (ExAnimEditingMode == EX_EDIT.None)
			{
				return;
			}

			if (ExAnimEditingMode == EX_EDIT.ExOnly_Edit)
			{
				_exAnimEditingMode = EX_EDIT.General_Edit;
			}
			else
			{
				_exAnimEditingMode = EX_EDIT.ExOnly_Edit;
			}

			RefreshAnimEditing(true);
		}

		/// <summary>
		/// 애니메이션 작업 도중 타임라인 추가/삭제, 키프레임 추가/삭제/이동과 같은 변동사항이 있을때 호출되어야 하는 함수
		/// </summary>
		public void RefreshAnimEditing(bool isGizmoEventReset)
		{
			//Debug.Log("RefreshAnimEditing");
			if (_animClip == null)
			{
				return;
			}

			//Editing 상태에 따라 Refresh 코드가 다르다
			if (ExAnimEditingMode != EX_EDIT.None)
			{

				bool isModLock_ColorUpdate = Editor.GetModLockOption_ColorPreview(ExAnimEditingMode);
				bool isModLock_OtherMod = Editor.GetModLockOption_CalculateIfNotAddedOther(ExAnimEditingMode);


				//현재 선택한 타임라인에 따라서 Modifier를 On/Off할지 결정한다.
				bool isExclusiveActive = false;
				if (_subAnimTimeline != null)
				{
					if (_subAnimTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier)
					{
						if (_subAnimTimeline._linkedModifier != null && _animClip._targetMeshGroup != null)
						{
							if (ExAnimEditingMode == EX_EDIT.ExOnly_Edit)
							{
								//현재의 AnimTimeline에 해당하는 ParamSet만 선택하자
								List<apModifierParamSetGroup> exParamSetGroups = new List<apModifierParamSetGroup>();
								List<apModifierParamSetGroup> linkParamSetGroups = _subAnimTimeline._linkedModifier._paramSetGroup_controller;
								for (int iP = 0; iP < linkParamSetGroups.Count; iP++)
								{
									apModifierParamSetGroup linkPSG = linkParamSetGroups[iP];
									if (linkPSG._keyAnimTimeline == _subAnimTimeline &&
										linkPSG._keyAnimClip == _animClip)
									{
										exParamSetGroups.Add(linkPSG);
									}
								}

								//Debug.Log("Set Anim Editing > Exclusive Enabled [" + _subAnimTimeline._linkedModifier.DisplayName + "]");

								_animClip._targetMeshGroup._modifierStack.SetExclusiveModifierInEditing_MultipleParamSetGroup(
																			_subAnimTimeline._linkedModifier,
																			exParamSetGroups,
																			isModLock_ColorUpdate);
								isExclusiveActive = true;
							}
							else if (ExAnimEditingMode == EX_EDIT.General_Edit)
							{
								//추가 : General Edit 모드
								//선택한 것과 허용되는 Modifier는 모두 허용한다.
								_animClip._targetMeshGroup._modifierStack.SetExclusiveModifierInEditing_MultipleParamSetGroup_General(
																			_subAnimTimeline._linkedModifier,
																			_animClip,
																			isModLock_ColorUpdate,
																			isModLock_OtherMod);
								isExclusiveActive = true;
							}
						}
					}
				}

				if (!isExclusiveActive)
				{
					//Modifier와 연동된게 아니라면
					if (_animClip._targetMeshGroup != null)
					{
						_animClip._targetMeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
						
						//이전
						//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_animClip._targetMeshGroup, false, true, true);

						//변경 20.4.13
						Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	_animClip._targetMeshGroup,
																			apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff,
																			apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);


						RefreshMeshGroupExEditingFlags(_animClip._targetMeshGroup, null, null, null, false);//<<추가
					}
				}
				else
				{
					RefreshMeshGroupExEditingFlags(_animClip._targetMeshGroup, _subAnimTimeline._linkedModifier, null, _animClip, false);//<<추가
				}
			}
			else
			{
				//모든 Modifier의 Exclusive 선택을 해제하고 모두 활성화한다.
				if (_animClip._targetMeshGroup != null)
				{
					_animClip._targetMeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					
					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(_animClip._targetMeshGroup, false, true, true);

					//변경 20.4.13
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	_animClip._targetMeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff, 
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);



					RefreshMeshGroupExEditingFlags(_animClip._targetMeshGroup, null, null, null, false);//<<추가
				}
			}

			AutoSelectAnimTimelineLayer(false);
			Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Info, null);
			SetAnimClipGizmoEvent(isGizmoEventReset);
		}






		/// <summary>
		/// Is Auto Scroll 옵션이 켜져있으면 스크롤을 자동으로 선택한다.
		/// 재생중에도 스크롤을 움직인다.
		/// </summary>
		public void SetAutoAnimScroll()
		{
			int curFrame = 0;
			int startFrame = 0;
			int endFrame = 0;
			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null || _timlineGUIWidth <= 0)
			{
				return;
			}
			if (!_animClip.IsPlaying_Editor)
			{
				return;
			}

			curFrame = _animClip.CurFrame;
			startFrame = _animClip.StartFrame;
			endFrame = _animClip.EndFrame;

			int widthPerFrame = Editor.WidthPerFrameInTimeline;
			int nFrames = Mathf.Max((endFrame - startFrame) + 1, 1);
			int widthForTotalFrame = nFrames * widthPerFrame;
			int widthForScrollFrame = widthForTotalFrame;

			//화면에 보여지는 프레임 범위는?
			int startFrame_Visible = (int)((float)(_scroll_Timeline.x / (float)widthPerFrame) + startFrame);
			int endFrame_Visible = (int)(((float)_timlineGUIWidth / (float)widthPerFrame) + startFrame_Visible);

			int marginFrame = 10;
			int targetFrame = -1;


			startFrame_Visible += marginFrame;
			endFrame_Visible -= marginFrame;

			//"이동해야할 범위와 실제로 이동되는 범위는 다르다"
			if (curFrame < startFrame_Visible)
			{
				//커서가 화면 왼쪽에 붙도록 하자
				targetFrame = curFrame - marginFrame;
			}
			else if (curFrame > endFrame_Visible)
			{
				//커서가 화면 오른쪽에 붙도록 하자
				targetFrame = (curFrame + marginFrame) - (int)((float)_timlineGUIWidth / (float)widthPerFrame);
			}
			else
			{
				return;
			}

			targetFrame -= startFrame;
			float nextScroll = Mathf.Clamp((targetFrame * widthPerFrame), 0, widthForScrollFrame);

			_scroll_Timeline.x = nextScroll;
		}

		/// <summary>
		/// 마우스 편집 중에 스크롤을 자동으로 해야하는 경우
		/// AnimClip의 프레임은 수정하지 않는다. (마우스 위치에 따른 TargetFrame을 넣어주자)
		/// </summary>
		public void AutoAnimScrollWithoutFrameMoving(int requestFrame, int marginFrame)
		{

			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null || _timlineGUIWidth <= 0)
			{
				return;
			}

			int startFrame = _animClip.StartFrame;
			int endFrame = _animClip.EndFrame;

			if (requestFrame < startFrame)
			{
				requestFrame = startFrame;
			}
			else if (requestFrame > endFrame)
			{
				requestFrame = endFrame;
			}

			int widthPerFrame = Editor.WidthPerFrameInTimeline;
			int nFrames = Mathf.Max((endFrame - startFrame) + 1, 1);
			int widthForTotalFrame = nFrames * widthPerFrame;
			int widthForScrollFrame = widthForTotalFrame;

			//화면에 보여지는 프레임 범위는?
			int startFrame_Visible = (int)((float)(_scroll_Timeline.x / (float)widthPerFrame) + startFrame);
			int endFrame_Visible = (int)(((float)_timlineGUIWidth / (float)widthPerFrame) + startFrame_Visible);

			//int marginFrame = 10;


			startFrame_Visible += marginFrame;
			endFrame_Visible -= marginFrame;

			int targetFrame = 0;

			//"이동해야할 범위와 실제로 이동되는 범위는 다르다"
			if (requestFrame < startFrame_Visible)
			{
				//커서가 화면 왼쪽에 붙도록 하자
				targetFrame = requestFrame - marginFrame;
			}
			else if (requestFrame > endFrame_Visible)
			{
				//커서가 화면 오른쪽에 붙도록 하자
				targetFrame = (requestFrame + marginFrame) - (int)((float)_timlineGUIWidth / (float)widthPerFrame);
			}
			else
			{
				return;
			}

			targetFrame -= startFrame;
			float nextScroll = Mathf.Clamp((targetFrame * widthPerFrame), 0, widthForScrollFrame);

			_scroll_Timeline.x = nextScroll;
		}

		/// <summary>
		/// AnimClip 작업을 위해 MeshTransform을 선택한다.
		/// 해당 데이터가 Timeline에 없어도 선택 가능하다.
		/// </summary>
		/// <param name="meshTransform"></param>
		public void SetSubMeshTransformForAnimClipEdit(apTransform_Mesh meshTransform, bool isAutoSelectAnimTimelineLayer, bool isAutoTimelineUIScroll)
		{
			if (meshTransform != null)
			{
				_bone = null;
			}
			_subMeshGroupTransformOnAnimClip = null;
			_subControlParamOnAnimClip = null;

			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null)
			{
				_subMeshTransformOnAnimClip = null;
				return;
			}
			_subMeshTransformOnAnimClip = meshTransform;

			if (isAutoSelectAnimTimelineLayer)
			{
				AutoSelectAnimTimelineLayer(isAutoTimelineUIScroll);
			}
		}

		/// <summary>
		/// AnimClip 작업을 위해 MeshGroupTransform을 선택한다.
		/// 해당 데이터가 Timeline에 없어도 선택 가능하다.
		/// </summary>
		/// <param name="meshGroupTransform"></param>
		public void SetSubMeshGroupTransformForAnimClipEdit(apTransform_MeshGroup meshGroupTransform, bool isAutoSelectAnimTimelineLayer, bool isAutoTimelineUIScroll)
		{
			if (meshGroupTransform != null)
			{
				_bone = null;
			}
			_subMeshTransformOnAnimClip = null;
			_subControlParamOnAnimClip = null;

			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null)
			{
				_subMeshGroupTransformOnAnimClip = null;
				return;
			}

			_subMeshGroupTransformOnAnimClip = meshGroupTransform;

			if (isAutoSelectAnimTimelineLayer)
			{
				AutoSelectAnimTimelineLayer(isAutoTimelineUIScroll);
			}
		}

		/// <summary>
		/// AnimClip 작업을 위해 Control Param을 선택한다.
		/// 해당 데이터가 Timeline에 없어도 선택 가능하다
		/// </summary>
		/// <param name="controlParam"></param>
		public void SetSubControlParamForAnimClipEdit(apControlParam controlParam, bool isAutoSelectAnimTimelineLayer, bool isAutoTimelineUIScroll)
		{
			_bone = null;
			_subMeshTransformOnAnimClip = null;
			_subMeshGroupTransformOnAnimClip = null;

			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null)
			{
				_subControlParamOnAnimClip = null;
				return;
			}

			_subControlParamOnAnimClip = controlParam;
			if (isAutoSelectAnimTimelineLayer)
			{
				AutoSelectAnimTimelineLayer(isAutoTimelineUIScroll);
			}
		}

		/// <summary>
		/// 선택된 객체(Transform/Bone/ControlParam) 중에서 "현재 타임라인"이 선택할 수 있는 객체를 리턴한다.
		/// </summary>
		/// <returns></returns>
		public object GetSelectedAnimTimelineObject()
		{
			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null ||
				_subAnimTimeline == null)
			{
				return null;
			}

			switch (_subAnimTimeline._linkType)
			{
				case apAnimClip.LINK_TYPE.AnimatedModifier:
					if (SubMeshTransformOnAnimClip != null)
					{
						return SubMeshTransformOnAnimClip;
					}
					if (SubMeshGroupTransformOnAnimClip != null)
					{
						return SubMeshGroupTransformOnAnimClip;
					}
					if (Bone != null)
					{
						return Bone;
					}
					break;

				case apAnimClip.LINK_TYPE.ControlParam:
					if (SubControlParamOnAnimClip != null)
					{
						return SubControlParamOnAnimClip;
					}
					break;

			}
			return null;
		}


		/// <summary>
		/// 현재 선택한 Sub 객체 (Transform, Bone, ControlParam)에 따라서
		/// 자동으로 Timeline의 Layer를 선택해준다.
		/// </summary>
		/// <param name="isAutoChangeLeftTabToControlParam">이 값이 True이면 Timeline이 ControlParam 타입일때 자동으로 왼쪽 탭이 Controller로 바뀐다.</param>
		public void AutoSelectAnimTimelineLayer(bool isAutoTimelineScroll, bool isAutoChangeLeftTabToControlParam = true)
		{
			//수정 :
			//Timeline을 선택하지 않았다 하더라도 자동으로 선택을 할 수 있다.
			//수정작업중이 아니며 + 해당 오브젝트를 포함하는 Layer를 가진 Timeline의 개수가 1개일 땐 그것을 선택한다.
			if (_selectionType != SELECTION_TYPE.Animation ||
				_animClip == null)
			{
				// 아예 작업 불가
				_subAnimTimeline = null;
				_subAnimTimelineLayer = null;
				_subAnimKeyframe = null;
				_subAnimWorkKeyframe = null;

				_subAnimKeyframeList.Clear();

				_animTimelineCommonCurve.Clear();//<<추가

				AutoSelectAnimWorkKeyframe();

				//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"
																											   //Editor.SetGUIVisible("AnimationRight2GUI_Timeline_Layers", false);
				return;
			}

			//apAnimTimelineLayer prevTimelineLayer = _subAnimTimelineLayer;

			if (_subAnimTimeline == null)
			{

				//1. 선택한 Timeline이 없네용
				//아예 새로 찾아야 한다.
				_subAnimWorkKeyframe = null;

				bool isFindTimeline = false;
				object selectedObject = null;


				if (SubMeshTransformOnAnimClip != null)
				{
					selectedObject = SubMeshTransformOnAnimClip;
				}
				else if (SubMeshGroupTransformOnAnimClip != null)
				{
					selectedObject = SubMeshGroupTransformOnAnimClip;
				}
				else if (Bone != null)
				{
					selectedObject = Bone;
				}
				else if (SubControlParamOnAnimClip != null)
				{
					selectedObject = SubControlParamOnAnimClip;
				}

				if (selectedObject != null && ExAnimEditingMode == EX_EDIT.None)
				{
					//선택 대상이 될법한 Timeline들을 찾자
					List<apAnimTimelineLayer> resultTimelineLayers = new List<apAnimTimelineLayer>();

					//Control Param을 제외하고 Timeline을 찾자 >> 변경 컨트롤 파라미터도 포함
					for (int i = 0; i < _animClip._timelines.Count; i++)
					{
						apAnimTimeline curTimeline = _animClip._timelines[i];

						if (curTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier
								&& curTimeline._linkedModifier == null)
						{
							continue;
						}

						apAnimTimelineLayer nextLayer = curTimeline.GetTimelineLayer(selectedObject);
						if (nextLayer != null)
						{
							resultTimelineLayers.Add(nextLayer);
						}
					}

					if (resultTimelineLayers.Count == 1)
					{
						//한개인 경우에만 선택이 가능하다
						isFindTimeline = true;

						apAnimTimelineLayer nextLayer = resultTimelineLayers[0];
						_subAnimTimeline = nextLayer._parentTimeline;
						SetAnimTimelineLayer(nextLayer, false);

						AutoSelectAnimWorkKeyframe();

						//여기서는 아예 Work Keyframe 뿐만아니라 Keyframe으로도 선택을 한다.
						SetAnimKeyframe(AnimWorkKeyframe, false, apGizmos.SELECT_TYPE.New);

						_modRegistableBones.Clear();//<<이것도 갱신해주자 [타임라인에 등록된 Bone]
						if (_subAnimTimeline != null)
						{
							for (int i = 0; i < _subAnimTimeline._layers.Count; i++)
							{
								apAnimTimelineLayer timelineLayer = _subAnimTimeline._layers[i];
								if (timelineLayer._linkedBone != null)
								{
									_modRegistableBones.Add(timelineLayer._linkedBone);
								}
							}

						}


						//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
						Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
						Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
						Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"
					}
				}

				if (!isFindTimeline)
				{
					//결국 Timeline을 찾지 못했다.
					_subAnimTimeline = null;
					_subAnimTimelineLayer = null;
					_subAnimKeyframe = null;
					_subAnimWorkKeyframe = null;

					_subAnimKeyframeList.Clear();

					_animTimelineCommonCurve.Clear();//<<추가

					AutoSelectAnimWorkKeyframe();

					//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"

					return;
				}
			}
			else
			{
				//2. 선택한 Timeline이 있으면 거기서 찾자
				_subAnimWorkKeyframe = null;

				//timeline이 ControlParam계열이라면 에디터의 탭을 변경
				if (_subAnimTimeline._linkType == apAnimClip.LINK_TYPE.ControlParam
					&& isAutoChangeLeftTabToControlParam)
				{
					//옵션이 허용하는 경우 (19.6.28 변경)
					if (Editor._isAutoSwitchControllerTab_Anim)
					{
						Editor.SetLeftTab(apEditor.TAB_LEFT.Controller);
					}
				}

				//삭제 19.11.22 : 자동 스크롤은 제한적으로 한다. (외부에서 오브젝트 선택한 경우에 한해서만)
				//자동으로 스크롤을 해주자
				//_isAnimTimelineLayerGUIScrollRequest = true;


				//다중 키프레임 작업중에는 단일 선택 불가
				if (_subAnimKeyframeList.Count > 1)
				{
					AutoSelectAnimWorkKeyframe();

					//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"
																												   //Editor.SetGUIVisible("AnimationRight2GUI_Timeline_Layers", false);

					return;
				}

				object selectedObject = GetSelectedAnimTimelineObject();
				if (selectedObject == null)
				{
					AutoSelectAnimWorkKeyframe();
					if (AnimWorkKeyframe == null)
					{
						SetAnimKeyframe(null, false, apGizmos.SELECT_TYPE.New);
					}

					//Debug.LogError("Object Select -> 선택된 객체가 없다.");

					//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"
																												   //Editor.SetGUIVisible("AnimationRight2GUI_Timeline_Layers", false);

					return;//선택된게 없다면 일단 패스
				}


				apAnimTimelineLayer nextLayer = null;
				nextLayer = _subAnimTimeline.GetTimelineLayer(selectedObject);
				if (nextLayer != null)
				{
					SetAnimTimelineLayer(nextLayer, false);
				}

				#region [미사용 코드]
				////만약 이미 선택된 레이어가 있다면 "유효"한지 테스트한다
				//bool isLayerIsAlreadySelected = false;
				//if (_subAnimTimelineLayer != null)
				//{
				//	if (_subAnimTimelineLayer.IsContainTargetObject(selectedObject))
				//	{
				//		//현재 레이어에 포함되어 있다면 패스
				//		//AutoSelectAnimWorkKeyframe();
				//		//return;
				//		isLayerIsAlreadySelected = true;
				//	}
				//}
				//apAnimTimelineLayer nextLayer = null;
				//if (!isLayerIsAlreadySelected)
				//{
				//	nextLayer = _subAnimTimeline.GetTimelineLayer(selectedObject);

				//	if (nextLayer != null)
				//	{
				//		SetAnimTimelineLayer(nextLayer, false);
				//	}
				//}
				//else
				//{
				//	nextLayer = _subAnimTimelineLayer;
				//} 
				#endregion


				AutoSelectAnimWorkKeyframe();

				//여기서는 아예 Work Keyframe 뿐만아니라 Keyframe으로도 선택을 한다.
				SetAnimKeyframe(AnimWorkKeyframe, false, apGizmos.SELECT_TYPE.New);

				_modRegistableBones.Clear();//<<이것도 갱신해주자 [타입라인에 등록된 Bone]
				if (_subAnimTimeline != null)
				{
					for (int i = 0; i < _subAnimTimeline._layers.Count; i++)
					{
						apAnimTimelineLayer timelineLayer = _subAnimTimeline._layers[i];
						if (timelineLayer._linkedBone != null)
						{
							_modRegistableBones.Add(timelineLayer._linkedBone);
						}
					}

				}

				//우측 Hierarchy GUI 변동이 있을 수 있으니 리셋
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Meshes, false);//"GUI Anim Hierarchy Delayed - Meshes"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__Bone, false);//"GUI Anim Hierarchy Delayed - Bone"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.GUI_Anim_Hierarchy_Delayed__ControlParam, false);//"GUI Anim Hierarchy Delayed - ControlParam"
																											   //Editor.SetGUIVisible("AnimationRight2GUI_Timeline_Layers", false);
			}

			//변경 19.11.22 : 제한적 상황에서만 자동 스크롤
			if (isAutoTimelineScroll)
			{
				//Debug.Log("자동 스크롤 ?? >> [데이터 존재 : " + (_subAnimTimelineLayer != null) + " / 바뀌었는가 : " + (_subAnimTimelineLayer != prevTimelineLayer) + "]");
				//자동으로 타임라인 UI을 스크롤한다면
				if (_subAnimTimelineLayer != null
					//&& _subAnimTimelineLayer != prevTimelineLayer
					)
				{
					_isAnimTimelineLayerGUIScrollRequest = true;//자동 스크롤을 켜자
																//Debug.Log("자동 스크롤 시작");
				}
			}
		}

		/// <summary>
		/// 현재 재생중인 프레임에 맞게 WorkKeyframe을 자동으로 선택한다.
		/// 키프레임을 바꾸거나 레이어를 바꿀때 자동으로 호출한다.
		/// 수동으로 선택하는 키프레임과 다르다.
		/// </summary>
		public void AutoSelectAnimWorkKeyframe()
		{
			Editor.Gizmos.SetUpdate();

			//apAnimKeyframe prevWorkKeyframe = _subAnimWorkKeyframe;
			if (_subAnimTimelineLayer == null || IsAnimPlaying)//<<플레이 중에는 모든 선택이 초기화된다.
			{

				if (_subAnimWorkKeyframe != null)
				{
					_subAnimWorkKeyframe = null;
					_modMeshOfAnim = null;
					_modBoneOfAnim = null;
					_renderUnitOfAnim = null;
					_modRenderVertOfAnim = null;
					_modRenderVertListOfAnim.Clear();
					_modRenderVertListOfAnim_Weighted.Clear();

					//추가 : 기즈모 갱신이 필요한 경우 (주로 FFD)
					Editor.Gizmos.RevertFFDTransformForce();
				}

				Editor.Hierarchy_AnimClip.RefreshUnits();
				return;
			}
			int curFrame = _animClip.CurFrame;
			_subAnimWorkKeyframe = _subAnimTimelineLayer.GetKeyframeByFrameIndex(curFrame);

			if (_subAnimWorkKeyframe == null)
			{

				_modMeshOfAnim = null;
				_modBoneOfAnim = null;
				_renderUnitOfAnim = null;
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();

				Editor.Gizmos.RevertFFDTransformForce();//<기즈모 갱신

				Editor.Hierarchy_AnimClip.RefreshUnits();
				return;
			}

			bool isResetMod = true;
			//if (_subAnimWorkKeyframe != prevWorkKeyframe)//강제
			{
				if (_subAnimTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier)
				{

					if (_subAnimTimeline._linkedModifier != null)
					{

						apModifierParamSet targetParamSet = _subAnimTimeline.GetModifierParamSet(_subAnimTimelineLayer, _subAnimWorkKeyframe);
						if (targetParamSet != null)
						{
							if (targetParamSet._meshData.Count > 0)
							{

								isResetMod = false;
								//중요!
								//>>여기서 Anim용 ModMesh를 선택한다.<<
								_modMeshOfAnim = targetParamSet._meshData[0];
								if (_modMeshOfAnim._transform_Mesh != null)
								{
									_renderUnitOfAnim = _animClip._targetMeshGroup.GetRenderUnit(_modMeshOfAnim._transform_Mesh);
								}
								else if (_modMeshOfAnim._transform_MeshGroup != null)
								{
									_renderUnitOfAnim = _animClip._targetMeshGroup.GetRenderUnit(_modMeshOfAnim._transform_MeshGroup);
								}
								else
								{
									_renderUnitOfAnim = null;
								}

								_modRenderVertOfAnim = null;
								_modRenderVertListOfAnim.Clear();
								_modRenderVertListOfAnim_Weighted.Clear();

								_modBoneOfAnim = null;//<<Mod Bone은 선택 해제
							}
							else if (targetParamSet._boneData.Count > 0)
							{

								isResetMod = false;

								//ModBone이 있다면 그걸 선택하자
								_modBoneOfAnim = targetParamSet._boneData[0];
								_renderUnitOfAnim = _modBoneOfAnim._renderUnit;
								if (_modBoneOfAnim != null)
								{
									_bone = _modBoneOfAnim._bone;
								}


								//Mod Mesh 변수는 초기화
								_modMeshOfAnim = null;
								_modRenderVertOfAnim = null;
								_modRenderVertListOfAnim.Clear();
								_modRenderVertListOfAnim_Weighted.Clear();
							}
						}
					}
				}
			}

			if (isResetMod)
			{
				_modMeshOfAnim = null;
				_modBoneOfAnim = null;
				_renderUnitOfAnim = null;
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();
			}

			Editor.Gizmos.RevertFFDTransformForce();//<기즈모 갱신

			Editor.Hierarchy_AnimClip.RefreshUnits();
		}


		/// <summary>
		/// Anim 편집시 모든 선택된 오브젝트를 해제한다.
		/// </summary>
		public void UnselectAllObjectsOfAnim()
		{
			_modRenderVertOfAnim = null;
			_modRenderVertListOfAnim.Clear();
			_modRenderVertListOfAnim_Weighted.Clear();

			_bone = null;
			_modBoneOfAnim = null;
			_modMeshOfAnim = null;

			_subControlParamOnAnimClip = null;
			_subMeshTransformOnAnimClip = null;
			_subMeshGroupTransformOnAnimClip = null;

			SetAnimTimelineLayer(null, true, false, true);//TImelineLayer의 선택을 취소해야 AutoSelect가 정상작동한다.
			AutoSelectAnimTimelineLayer(false);
		}

		/// <summary>
		/// Mod-Render Vertex를 선택한다. [Animation 수정작업시]
		/// </summary>
		/// <param name="modVertOfAnim">Modified Vertex of Anim Keyframe</param>
		/// <param name="renderVertOfAnim">Render Vertex of Anim Keyframe</param>
		public void SetModVertexOfAnim(apModifiedVertex modVertOfAnim, apRenderVertex renderVertOfAnim)
		{
			if (_selectionType != SELECTION_TYPE.Animation
				|| _animClip == null
				|| AnimWorkKeyframe == null
				|| ModMeshOfAnim == null)
			{
				return;
			}

			if (modVertOfAnim == null || renderVertOfAnim == null)
			{
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();
				return;
			}

			if (ModMeshOfAnim != modVertOfAnim._modifiedMesh)
			{
				_modRenderVertOfAnim = null;
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim_Weighted.Clear();
				return;
			}
			bool isChangeModVert = false;
			if (_modRenderVertOfAnim != null)
			{
				if (_modRenderVertOfAnim._modVert != modVertOfAnim || _modRenderVertOfAnim._renderVert != renderVertOfAnim)
				{
					isChangeModVert = true;
				}
			}
			else
			{
				isChangeModVert = true;
			}

			if (isChangeModVert)
			{
				_modRenderVertOfAnim = new ModRenderVert(modVertOfAnim, renderVertOfAnim);
				_modRenderVertListOfAnim.Clear();
				_modRenderVertListOfAnim.Add(_modRenderVertOfAnim);

				_modRenderVertListOfAnim_Weighted.Clear();

			}
		}



		/// <summary>
		/// Mod-Render Vertex를 추가한다. [Animation 수정작업시]
		/// </summary>
		public void AddModVertexOfAnim(apModifiedVertex modVertOfAnim, apRenderVertex renderVertOfAnim)
		{
			if (_selectionType != SELECTION_TYPE.Animation
				|| _animClip == null
				|| AnimWorkKeyframe == null
				|| ModMeshOfAnim == null)
			{
				return;
			}

			if (modVertOfAnim == null || renderVertOfAnim == null)
			{
				//추가/제거없이 생략
				return;
			}

			bool isExistSame = _modRenderVertListOfAnim.Exists(delegate (ModRenderVert a)
			{
				return a._modVert == modVertOfAnim || a._renderVert == renderVertOfAnim;
			});

			if (!isExistSame)
			{
				//새로 생성+추가해야할 필요가 있다.
				ModRenderVert newModRenderVert = new ModRenderVert(modVertOfAnim, renderVertOfAnim);
				_modRenderVertListOfAnim.Add(newModRenderVert);

				if (_modRenderVertListOfAnim.Count == 1)
				{
					_modRenderVertOfAnim = newModRenderVert;
				}
			}
		}

		/// <summary>
		/// Mod-Render Vertex를 삭제한다. [Animation 수정작업시]
		/// </summary>
		public void RemoveModVertexOfAnim(apModifiedVertex modVertOfAnim, apRenderVertex renderVertOfAnim)
		{
			if (_selectionType != SELECTION_TYPE.Animation
				|| _animClip == null
				|| AnimWorkKeyframe == null
				|| ModMeshOfAnim == null)
			{
				return;
			}

			if (modVertOfAnim == null || renderVertOfAnim == null)
			{
				//추가/제거없이 생략
				return;
			}

			_modRenderVertListOfAnim.RemoveAll(delegate (ModRenderVert a)
			{
				return a._modVert == modVertOfAnim || a._renderVert == renderVertOfAnim;
			});

			if (_modRenderVertListOfAnim.Count == 1)
			{
				_modRenderVertOfAnim = _modRenderVertListOfAnim[0];
			}
			else if (_modRenderVertListOfAnim.Count == 0)
			{
				_modRenderVertOfAnim = null;
			}
			else if (!_modRenderVertListOfAnim.Contains(_modRenderVertOfAnim))
			{
				_modRenderVertOfAnim = null;
				_modRenderVertOfAnim = _modRenderVertListOfAnim[0];
			}

		}





		public void SetBone(apBone bone)
		{
			if (bone != null)
			{
				if (SelectionType == SELECTION_TYPE.MeshGroup
					&& Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Bone)
				{
					//만약 MeshGroup 메뉴 -> Bone 탭일 때
					//현재 선택한 meshGroup의 Bone이 아닌 Sub Bone이라면 > 선택 취소
					if (MeshGroup != bone._meshGroup)
					{
						bone = null;
					}
				}
			}
			if (_bone != bone)
			{
				apEditorUtil.ReleaseGUIFocus();
			}
			_bone = bone;
			if (SelectionType == SELECTION_TYPE.MeshGroup &&
				Modifier != null)
			{
				AutoSelectModMeshOrModBone();
			}
			if (SelectionType == SELECTION_TYPE.Animation && AnimClip != null)
			{
				AutoSelectAnimTimelineLayer(false);
			}
		}

		/// <summary>
		/// AnimClip 작업시 Bone을 선택하면 SetBone대신 이 함수를 호출한다.
		/// </summary>
		/// <param name="bone"></param>
		public void SetBoneForAnimClip(apBone bone, bool isAutoSelectTimelineLayer, bool isAutoTimelineUIScroll)
		{
			_bone = bone;

			if (bone != null)
			{
				_subControlParamOnAnimClip = null;
				_subMeshTransformOnAnimClip = null;
				_subMeshGroupTransformOnAnimClip = null;
			}

			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null)
			{
				_bone = null;
				return;
			}

			SetAnimTimelineLayer(null, true);//TImelineLayer의 선택을 취소해야 AutoSelect가 정상작동한다.

			if (isAutoSelectTimelineLayer)
			{
				AutoSelectAnimTimelineLayer(isAutoTimelineUIScroll);
			}

			if (_bone != bone && bone != null)
			{
				//bone은 유지하자
				_bone = bone;
				_modBoneOfAnim = null;
			}
		}

		/// <summary>
		/// isEditing : Default Matrix를 수정하는가
		/// isBoneMenu : 현재 Bone Menu인가
		/// </summary>
		/// <param name="isEditing"></param>
		/// <param name="isBoneMenu"></param>
		public void SetBoneEditing(bool isEditing, bool isBoneMenu)
		{
			//bool isChanged = _isBoneDefaultEditing != isEditing;

			_isBoneDefaultEditing = isEditing;

			//if (isChanged)
			{
				if (_isBoneDefaultEditing)
				{
					SetBoneEditMode(BONE_EDIT_MODE.SelectAndTRS, isBoneMenu);
					//Debug.LogError("TODO : Default Bone Tranform을 활성화할 때에는 다른 Rig Modifier를 꺼야한다.");

					//Editor.Gizmos.LinkObject()
				}
				else
				{
					if (isBoneMenu)
					{
						SetBoneEditMode(BONE_EDIT_MODE.SelectOnly, isBoneMenu);
					}
					else
					{
						SetBoneEditMode(BONE_EDIT_MODE.None, isBoneMenu);
					}
					//Debug.LogError("TODO : Default Bone Tranform을 종료할 때에는 다른 Rig Modifier를 켜야한다.");
				}
			}
		}

		public void SetBoneEditMode(BONE_EDIT_MODE boneEditMode, bool isBoneMenu)
		{
			_boneEditMode = boneEditMode;

			if (!_isBoneDefaultEditing)
			{
				if (isBoneMenu)
				{
					_boneEditMode = BONE_EDIT_MODE.SelectOnly;
				}
				else
				{
					_boneEditMode = BONE_EDIT_MODE.None;
				}
			}

			Editor.Controller.SetBoneEditInit();
			//Gizmo 이벤트를 설정하자
			switch (_boneEditMode)
			{
				case BONE_EDIT_MODE.None:
					Editor.Gizmos.Unlink();
					break;

				case BONE_EDIT_MODE.SelectOnly:
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Bone_SelectOnly());
					break;

				case BONE_EDIT_MODE.SelectAndTRS:
					//Select에서는 Gizmo 이벤트를 받는다.
					//Transform 제어를 해야하기 때문
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_Bone_Default());
					break;

				case BONE_EDIT_MODE.Add:
					Editor.Gizmos.Unlink();
					break;

				case BONE_EDIT_MODE.Link:
					Editor.Gizmos.Unlink();
					break;
			}
		}

		/// <summary>
		/// Rigging시 Pose Test를 하는지 여부를 설정한다.
		/// 모든 MeshGroup에 대해서 설정한다.
		/// _rigEdit_isTestPosing값을 먼저 설정한다.
		/// </summary>
		public void SetBoneRiggingTest()
		{
			if (Editor._portrait == null)
			{
				return;
			}
			for (int i = 0; i < Editor._portrait._meshGroups.Count; i++)
			{
				apMeshGroup meshGroup = Editor._portrait._meshGroups[i];
				meshGroup.SetBoneRiggingTest(_rigEdit_isTestPosing);
			}
		}

		/// <summary>
		/// Rigging시, Test중인 Pose를 리셋한다.
		/// </summary>
		public void ResetRiggingTestPose()
		{
			if (Editor._portrait == null)
			{
				return;
			}
			for (int i = 0; i < Editor._portrait._meshGroups.Count; i++)
			{
				apMeshGroup meshGroup = Editor._portrait._meshGroups[i];
				meshGroup.ResetRiggingTestPose();
			}
			Editor.RefreshControllerAndHierarchy(false);
			Editor.SetRepaint();
		}



		// Editor View
		//-------------------------------------
		public bool DrawEditor(int width, int height)
		{
			if (_portrait == null)
			{
				return false;
			}

			EditorGUILayout.Space();

			switch (_selectionType)
			{
				case SELECTION_TYPE.None:
					Draw_None(width, height);
					break;

				case SELECTION_TYPE.ImageRes:
					Draw_ImageRes(width, height);
					break;
				case SELECTION_TYPE.Mesh:
					Draw_Mesh(width, height);
					break;
				case SELECTION_TYPE.Face:
					Draw_Face(width, height);
					break;
				case SELECTION_TYPE.MeshGroup:
					Draw_MeshGroup(width, height);
					break;
				case SELECTION_TYPE.Animation:
					Draw_Animation(width, height);
					break;
				case SELECTION_TYPE.Overall:
					Draw_Overall(width, height);
					break;
				case SELECTION_TYPE.Param:
					Draw_Param(width, height);
					break;
			}

			EditorGUILayout.Space();

			return true;
		}


		public void DrawEditor_Header(int width, int height)
		{
			switch (_selectionType)
			{
				case SELECTION_TYPE.None:
					DrawTitle(Editor.GetUIWord(UIWORD.NotSelected), width, height);//"Not Selected"
					break;

				case SELECTION_TYPE.ImageRes:
					DrawTitle(Editor.GetUIWord(UIWORD.Image), width, height);//"Image"
					break;
				case SELECTION_TYPE.Mesh:
					DrawTitle(Editor.GetUIWord(UIWORD.Mesh), width, height);//"Mesh"
					break;
				case SELECTION_TYPE.Face:
					DrawTitle("Face", width, height);
					break;
				case SELECTION_TYPE.MeshGroup:
					DrawTitle(Editor.GetUIWord(UIWORD.MeshGroup), width, height);//"Mesh Group"
					break;
				case SELECTION_TYPE.Animation:
					DrawTitle(Editor.GetUIWord(UIWORD.AnimationClip), width, height);//"Animation Clip"
					break;
				case SELECTION_TYPE.Overall:
					DrawTitle(Editor.GetUIWord(UIWORD.RootUnit), width, height);//"Root Unit"
					break;
				case SELECTION_TYPE.Param:
					DrawTitle(Editor.GetUIWord(UIWORD.ControlParameter), width, height);//"Control Parameter"
					break;
			}
		}


		private void Draw_None(int width, int height)
		{
			EditorGUILayout.Space();
		}

		private void Draw_ImageRes(int width, int height)
		{
			EditorGUILayout.Space();

			apTextureData textureData = _image;
			if (textureData == null)
			{
				SetNone();
				return;
			}

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ImageAsset));//"Image Asset"

			//textureData._image = EditorGUILayout.ObjectField(textureData._image, typeof(Texture2D), true, GUILayout.Width(width), GUILayout.Height(50)) as Texture2D;
			Texture2D nextImage = EditorGUILayout.ObjectField(textureData._image, typeof(Texture2D), true) as Texture2D;

			if (GUILayout.Button(Editor.GetUIWord(UIWORD.SelectImage), apGUILOFactory.I.Height(30)))//"Select Image"
			{
				_loadKey_SelectTextureAsset = apDialog_SelectTextureAsset.ShowDialog(Editor, textureData, OnTextureAssetSelected);
			}

			if (textureData._image != nextImage)
			{
				//이미지가 추가되었다.
				if (nextImage != null)
				{
					//Undo
					apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Image_SettingChanged, Editor, Editor._portrait, textureData._image, false);

					textureData._image = nextImage;//이미지 추가
					textureData._name = textureData._image.name;
					textureData._width = textureData._image.width;
					textureData._height = textureData._image.height;

					//이미지 에셋의 Path를 확인하고, PSD인지 체크한다.
					if (textureData._image != null)
					{
						string fullPath = AssetDatabase.GetAssetPath(textureData._image);
						//Debug.Log("Image Path : " + fullPath);

						if (string.IsNullOrEmpty(fullPath))
						{
							textureData._assetFullPath = "";
							//textureData._isPSDFile = false;
						}
						else
						{
							textureData._assetFullPath = fullPath;
							//if (fullPath.Contains(".psd") || fullPath.Contains(".PSD"))
							//{
							//	textureData._isPSDFile = true;
							//}
							//else
							//{
							//	textureData._isPSDFile = false;
							//}
						}
					}
					else
					{
						textureData._assetFullPath = "";
						//textureData._isPSDFile = false;
					}
				}
				//Editor.Hierarchy.RefreshUnits();
				Editor.RefreshControllerAndHierarchy(false);
			}

			EditorGUILayout.Space();


			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Name));//"Name"
			string nextName = EditorGUILayout.DelayedTextField(textureData._name);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Size));//"Size"

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Width), apGUILOFactory.I.Width(40));//"Width"
			int nextWidth = EditorGUILayout.DelayedIntField(textureData._width);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Height), apGUILOFactory.I.Width(40));//"Height"
			int nextHeight = EditorGUILayout.DelayedIntField(textureData._height);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();


			//변경값이 있으면 Undo 등록하고 변경
			if (!string.Equals(nextName, textureData._name) ||
				nextWidth != textureData._width ||
				nextHeight != textureData._height)
			{
				//Undo
				apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Image_SettingChanged, Editor, Editor._portrait, textureData, false);

				textureData._name = nextName;
				textureData._width = nextWidth;
				textureData._height = nextHeight;

				Editor.RefreshControllerAndHierarchy(false);
			}

			GUILayout.Space(20);
			if (textureData._image != null)
			{
				if (textureData._image != _imageImported || _imageImporter == null)
				{
					string path = AssetDatabase.GetAssetPath(textureData._image);
					_imageImported = textureData._image;
					_imageImporter = (TextureImporter)TextureImporter.GetAtPath(path);
				}
			}
			else
			{
				_imageImported = null;
				_imageImporter = null;
			}



			//텍스쳐 설정을 할 수 있다.
			if (_imageImporter != null)
			{
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(20);

				bool prev_sRGB = _imageImporter.sRGBTexture;
				TextureImporterCompression prev_compressed = _imageImporter.textureCompression;

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ColorSpace));

				if (_strWrapper_64 == null)
				{
					_strWrapper_64 = new apStringWrapper(64);
				}
				_strWrapper_64.Clear();
				_strWrapper_64.Append(apStringFactory.I.Bracket_1_L, false);
				_strWrapper_64.AppendSpace(1, false);
				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Current), false);
				_strWrapper_64.AppendSpace(1, false);
				_strWrapper_64.Append(apStringFactory.I.Colon, false);
				_strWrapper_64.AppendSpace(1, false);
				if (apEditorUtil.IsGammaColorSpace())
				{
					_strWrapper_64.Append(apStringFactory.I.Gamma, false);
				}
				else
				{
					_strWrapper_64.Append(apStringFactory.I.Linear, false);
				}
				_strWrapper_64.AppendSpace(1, false);
				_strWrapper_64.Append(apStringFactory.I.Bracket_1_R, true);

				//EditorGUILayout.LabelField("( " + Editor.GetUIWord(UIWORD.Current) + " : " + (apEditorUtil.IsGammaColorSpace() ? "Gamma" : "Linear") + " )");
				EditorGUILayout.LabelField(_strWrapper_64.ToString());

				//sRGB True => Gamma Color Space이다.
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(24));
				GUILayout.Space(5);
				int iColorSpace = _imageImporter.sRGBTexture ? 0 : 1;
				int nextColorSpace = EditorGUILayout.Popup(iColorSpace, _imageColorSpaceNames);
				if (nextColorSpace != iColorSpace)
				{
					if (nextColorSpace == 0)
					{
						//Gamma : sRGB 사용
						_imageImporter.sRGBTexture = true;
					}
					else
					{
						//Linear : sRGB 사용 안함
						_imageImporter.sRGBTexture = false;
					}
				}
				EditorGUILayout.EndHorizontal();



				GUILayout.Space(5);
				int prevQuality = 0;
				if (_imageImporter.textureCompression == TextureImporterCompression.CompressedLQ)
				{
					prevQuality = 0;
				}
				else if (_imageImporter.textureCompression == TextureImporterCompression.Compressed)
				{
					prevQuality = 1;
				}
				else if (_imageImporter.textureCompression == TextureImporterCompression.CompressedHQ)
				{
					prevQuality = 2;
				}
				else if (_imageImporter.textureCompression == TextureImporterCompression.Uncompressed)
				{
					prevQuality = 3;
				}

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Compression));//"Compression"
				int nextQuality = EditorGUILayout.Popup(prevQuality, _imageQualityNames);

				GUILayout.Space(5);
				bool prevMipmap = _imageImporter.mipmapEnabled;
				_imageImporter.mipmapEnabled = EditorGUILayout.Toggle(Editor.GetUIWord(UIWORD.UseMipmap), _imageImporter.mipmapEnabled);//"Use Mipmap"

				if (nextQuality != prevQuality)
				{
					switch (nextQuality)
					{
						case 0://ComLQ
							_imageImporter.textureCompression = TextureImporterCompression.CompressedLQ;
							break;

						case 1://Com
							_imageImporter.textureCompression = TextureImporterCompression.Compressed;
							break;

						case 2://ComHQ
							_imageImporter.textureCompression = TextureImporterCompression.CompressedHQ;
							break;

						case 3://Uncom
							_imageImporter.textureCompression = TextureImporterCompression.Uncompressed;
							break;
					}
				}

				GUILayout.Space(5);

				//추가 : Read/Write 옵션 확인
				bool prevReadWrite = _imageImporter.isReadable;
				_imageImporter.isReadable = EditorGUILayout.Toggle(Editor.GetUIWord(UIWORD.TextureRW), _imageImporter.isReadable);//"Use Mipmap"




				if (_imageImporter.isReadable)
				{
					//경고 메시지
					//GUIStyle guiStyle_Info = new GUIStyle(GUI.skin.box);
					//guiStyle_Info.alignment = TextAnchor.MiddleCenter;
					//guiStyle_Info.normal.textColor = apEditorUtil.BoxTextColor;

					Color prevColor = GUI.backgroundColor;

					GUI.backgroundColor = new Color(1.0f, 0.6f, 0.5f, 1.0f);
					GUILayout.Box(Editor.GetUIWord(UIWORD.WarningTextureRWNeedToDisabledForOpt), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(48));

					GUI.backgroundColor = prevColor;
				}

				if (nextQuality != prevQuality ||
					_imageImporter.sRGBTexture != prev_sRGB ||
					_imageImporter.mipmapEnabled != prevMipmap ||
					_imageImporter.isReadable != prevReadWrite)
				{

					_imageImporter.SaveAndReimport();
					_imageImporter = null;
					_imageImported = null;
					AssetDatabase.Refresh();
				}

				GUILayout.Space(20);
			}


			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(20);

			if (GUILayout.Button(Editor.GetUIWord(UIWORD.RefreshImageProperty), apGUILOFactory.I.Height(30)))//"Refresh Image Property"
			{
				//Undo
				apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Image_SettingChanged, Editor, Editor._portrait, textureData, false);

				if (textureData._image != null)
				{
					textureData._name = textureData._image.name;
					textureData._width = textureData._image.width;
					textureData._height = textureData._image.height;
				}
				else
				{
					textureData._name = "";
					textureData._width = 0;
					textureData._height = 0;
				}
				//Editor.Hierarchy.RefreshUnits();
				Editor.RefreshControllerAndHierarchy(false);
			}

			// Remove
			GUILayout.Space(30);



			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveImage),
			//										Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
			//										),

			if (_guiContent_Image_RemoveImage == null)
			{
				_guiContent_Image_RemoveImage = new apGUIContentWrapper();
				_guiContent_Image_RemoveImage.ClearText(false);
				_guiContent_Image_RemoveImage.AppendSpaceText(2, false);
				_guiContent_Image_RemoveImage.AppendText(Editor.GetUIWord(UIWORD.RemoveImage), true);
				_guiContent_Image_RemoveImage.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}

			//변경
			if (GUILayout.Button(_guiContent_Image_RemoveImage.Content, apGUILOFactory.I.Height(24)))//"  Remove Image"
			{

				//bool isResult = EditorUtility.DisplayDialog("Remove Image", "Do you want to remove [" + textureData._name + "]?", "Remove", "Cancel");

				//Texture를 삭제하면 영향을 받는 메시들을 확인하자
				string strDialogInfo = Editor.Controller.GetRemoveItemMessage(
															_portrait,
															textureData,
															5,
															Editor.GetTextFormat(TEXT.RemoveImage_Body, textureData._name),
															Editor.GetText(TEXT.DLG_RemoveItemChangedWarning));

				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveImage_Title),
																strDialogInfo,
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel));


				if (isResult)
				{
					Editor.Controller.RemoveTexture(textureData);
					//_portrait._textureData.Remove(textureData);

					SetNone();
				}
				//Editor.Hierarchy.RefreshUnits();
				Editor.RefreshControllerAndHierarchy(false);
			}
		}



		private void OnTextureAssetSelected(bool isSuccess, apTextureData targetTextureData, object loadKey, Texture2D resultTexture2D)
		{
			if (_loadKey_SelectTextureAsset != loadKey || !isSuccess)
			{
				_loadKey_SelectTextureAsset = null;
				return;
			}
			_loadKey_SelectTextureAsset = null;
			if (targetTextureData == null)
			{
				return;
			}

			//Undo
			apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Image_SettingChanged, Editor, Editor._portrait, targetTextureData, false);

			targetTextureData._image = resultTexture2D;
			//이미지가 추가되었다.
			if (targetTextureData._image != null)
			{


				targetTextureData._name = targetTextureData._image.name;
				targetTextureData._width = targetTextureData._image.width;
				targetTextureData._height = targetTextureData._image.height;

				//이미지 에셋의 Path를 확인하고, PSD인지 체크한다.
				if (targetTextureData._image != null)
				{
					string fullPath = AssetDatabase.GetAssetPath(targetTextureData._image);
					//Debug.Log("Image Path : " + fullPath);

					if (string.IsNullOrEmpty(fullPath))
					{
						targetTextureData._assetFullPath = "";
						//targetTextureData._isPSDFile = false;
					}
					else
					{
						targetTextureData._assetFullPath = fullPath;
					}
				}
				else
				{
					targetTextureData._assetFullPath = "";
					//targetTextureData._isPSDFile = false;
				}
			}
			//Editor.Hierarchy.RefreshUnits();
			Editor.RefreshControllerAndHierarchy(false);
		}





		//private bool _isShowTextureDataList = false;
		private void Draw_Mesh(int width, int height)
		{
			//GUILayout.Box("Mesh", GUILayout.Width(width), GUILayout.Height(30));
			//DrawTitle("Mesh", width);
			EditorGUILayout.Space();

			if (_mesh == null)
			{
				SetNone();
				return;
			}

			//탭
			bool isEditMeshMode_None = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.Setting);
			bool isEditMeshMode_MakeMesh = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.MakeMesh);
			bool isEditMeshMode_Modify = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.Modify);

			//bool isEditMeshMode_AddVertex = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.AddVertex);
			//bool isEditMeshMode_LinkEdge = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.LinkEdge);

			bool isEditMeshMode_Pivot = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.PivotEdit);
			//bool isEditMeshMode_Volume = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.VolumeWeight);
			//bool isEditMeshMode_Physic = (Editor._meshEditMode == apEditor.MESH_EDIT_MODE.PhysicWeight);

			int subTabWidth = (width / 2) - 5;
			int subTabHeight = 24;
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));
			//int tabBtnHeight = 30;
			GUILayout.Space(5);


			//" Setting"
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Setting), 1, Editor.GetUIWord(UIWORD.Setting), isEditMeshMode_None, true, subTabWidth, subTabHeight, apStringFactory.I.SettingsOfMesh))//"Settings of Mesh"
			{
				if (!isEditMeshMode_None)
				{
					Editor.Controller.CheckMeshEdgeWorkRemained();
					Editor._meshEditMode = apEditor.MESH_EDIT_MODE.Setting;

					Editor.Gizmos.Unlink();
				}
			}


			//" Make Mesh"
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MeshEditMenu), 1, Editor.GetUIWord(UIWORD.MakeMesh), isEditMeshMode_MakeMesh, true, subTabWidth, subTabHeight, apStringFactory.I.MakeVerticesAndPolygons))//"Make Vertices and Polygons"
			{
				if (!isEditMeshMode_MakeMesh)
				{
					Editor.Controller.CheckMeshEdgeWorkRemained();
					Editor._meshEditMode = apEditor.MESH_EDIT_MODE.MakeMesh;
					Editor.Controller.StartMeshEdgeWork();
					Editor.VertController.SetMesh(_mesh);
					Editor.VertController.UnselectVertex();

					Editor.Gizmos.Unlink();
				}
			}


			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));
			GUILayout.Space(5);



			//" Pivot"
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_PivotMenu), 1, Editor.GetUIWord(UIWORD.Pivot), isEditMeshMode_Pivot, true, subTabWidth, subTabHeight, apStringFactory.I.EditPivotOfMesh))//"Edit Pivot of Mesh"
			{
				if (!isEditMeshMode_Pivot)
				{
					Editor.Controller.CheckMeshEdgeWorkRemained();
					Editor._meshEditMode = apEditor.MESH_EDIT_MODE.PivotEdit;

					Editor.Gizmos.Unlink();
				}
			}


			//" Modify"
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ModifyMenu), 1, Editor.GetUIWord(UIWORD.Modify), isEditMeshMode_Modify, true, subTabWidth, subTabHeight, apStringFactory.I.ModifyVertices))//"Modify Vertices"
			{
				if (!isEditMeshMode_Modify)
				{
					Editor.Controller.CheckMeshEdgeWorkRemained();
					Editor._meshEditMode = apEditor.MESH_EDIT_MODE.Modify;

					Editor.Gizmos.Unlink();
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshEdit_Modify());
				}
			}


			EditorGUILayout.EndHorizontal();



			switch (Editor._meshEditMode)
			{
				case apEditor.MESH_EDIT_MODE.Setting:
					MeshProperty_None(width, height);
					break;

				case apEditor.MESH_EDIT_MODE.Modify:
					MeshProperty_Modify(width, height);
					break;

				case apEditor.MESH_EDIT_MODE.MakeMesh:
					MeshProperty_MakeMesh(width, height);
					break;
				//case apEditor.MESH_EDIT_MODE.AddVertex:
				//	MeshProperty_AddVertex(width, height);
				//	break;

				//case apEditor.MESH_EDIT_MODE.LinkEdge:
				//	MeshProperty_LinkEdge(width, height);
				//	break;

				case apEditor.MESH_EDIT_MODE.PivotEdit:
					MeshProperty_Pivot(width, height);
					break;

					//case apEditor.MESH_EDIT_MODE.VolumeWeight:
					//	MeshProperty_Volume(width, height);
					//	break;

					//case apEditor.MESH_EDIT_MODE.PhysicWeight:
					//	MeshProperty_Physic(width, height);
					//	break;
			}
		}

		private void Draw_Face(int width, int height)
		{
			//GUILayout.Box("Face", GUILayout.Width(width), GUILayout.Height(30));
			//DrawTitle("Face", width);
			EditorGUILayout.Space();

		}

		private void Draw_MeshGroup(int width, int height)
		{
			//DrawTitle("Mesh Group", width);
			EditorGUILayout.Space();

			if (_meshGroup == null)
			{
				SetNone();
				return;
			}

			bool isEditMeshGroupMode_Setting = (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Setting);
			bool isEditMeshGroupMode_Bone = (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Bone);
			bool isEditMeshGroupMode_Modifier = (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Modifier);
			int subTabWidth = (width / 2) - 4;
			int subTabHeight = 24;
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));
			GUILayout.Space(5);

			//" Setting"
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Setting), 1, Editor.GetUIWord(UIWORD.Setting), isEditMeshGroupMode_Setting, true, subTabWidth, subTabHeight, apStringFactory.I.SettingsOfMeshGroup))//"Settings of Mesh Group"
			{
				if (!isEditMeshGroupMode_Setting)
				{
					Editor._meshGroupEditMode = apEditor.MESHGROUP_EDIT_MODE.Setting;


					SetBone(null);
					SetModMeshOfModifier(null);
					SetSubMeshGroupInGroup(null);
					SetSubMeshInGroup(null);
					SetModifier(null);

					SetBoneEditing(false, false);//Bone 처리는 종료 

					//Gizmo 컨트롤 방식을 Setting에 맞게 바꾸자
					Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshGroupSetting());



					SetModifierEditMode(EX_EDIT_KEY_VALUE.None);

					_rigEdit_isBindingEdit = false;
					_rigEdit_isTestPosing = false;
					SetBoneRiggingTest();

					//스크롤 초기화 (오른쪽2)
					Editor.ResetScrollPosition(false, false, false, true, false);
				}
			}

			//" Bone"
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Bone), 1, Editor.GetUIWord(UIWORD.Bone), isEditMeshGroupMode_Bone, true, subTabWidth, subTabHeight, apStringFactory.I.BonesOfMeshGroup))//"Bones of Mesh Group"
			{
				if (!isEditMeshGroupMode_Bone)
				{
					Editor._meshGroupEditMode = apEditor.MESHGROUP_EDIT_MODE.Bone;

					SetBone(null);
					SetModMeshOfModifier(null);
					SetSubMeshGroupInGroup(null);
					SetSubMeshInGroup(null);
					SetModifier(null);

					//일단 Gizmo 초기화
					Editor.Gizmos.Unlink();

					_meshGroupChildHierarchy = MESHGROUP_CHILD_HIERARCHY.Bones;//하단 UI도 변경

					SetModifierEditMode(EX_EDIT_KEY_VALUE.ParamKey_Bone);

					_rigEdit_isBindingEdit = false;
					_rigEdit_isTestPosing = false;
					SetBoneRiggingTest();

					SetBoneEditing(false, true);

					//스크롤 초기화 (오른쪽2)
					Editor.ResetScrollPosition(false, false, false, true, false);
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));
			GUILayout.Space(5);

			//Modifer
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Modifier), 1, Editor.GetUIWord(UIWORD.Modifier), isEditMeshGroupMode_Modifier, true, width - 5, subTabHeight, apStringFactory.I.ModifiersOfMeshGroup))//"Modifiers of Mesh Group"
			{
				if (!isEditMeshGroupMode_Modifier)
				{
					SetBone(null);
					SetBoneEditing(false, false);//Bone 처리는 종료 

					Editor._meshGroupEditMode = apEditor.MESHGROUP_EDIT_MODE.Modifier;

					bool isSelectMod = false;
					if (Modifier == null)
					{
						//이전에 선택했던 Modifier가 없다면..
						if (_meshGroup._modifierStack != null)
						{
							if (_meshGroup._modifierStack._modifiers.Count > 0)
							{
								//맨 위의 Modifier를 자동으로 선택해주자
								int nMod = _meshGroup._modifierStack._modifiers.Count;
								apModifierBase lastMod = _meshGroup._modifierStack._modifiers[nMod - 1];
								SetModifier(lastMod);
								isSelectMod = true;
							}
						}
					}
					else
					{
						SetModifier(Modifier);

						isSelectMod = true;
					}

					if (!isSelectMod)
					{
						SetModifier(null);
					}

					//스크롤 초기화 (오른쪽2)
					Editor.ResetScrollPosition(false, false, false, true, false);

				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			if (Editor._meshGroupEditMode != apEditor.MESHGROUP_EDIT_MODE.Setting)
			{
				_isMeshGroupSetting_ChangePivot = false;
			}

			switch (Editor._meshGroupEditMode)
			{
				case apEditor.MESHGROUP_EDIT_MODE.Setting:
					MeshGroupProperty_Setting(width, height);
					break;

				case apEditor.MESHGROUP_EDIT_MODE.Bone:
					MeshGroupProperty_Bone(width, height);
					break;

				case apEditor.MESHGROUP_EDIT_MODE.Modifier:
					MeshGroupProperty_Modify(width, height);
					break;
			}
		}





		//private string _prevAnimClipName = "";


		private void Draw_Animation(int width, int height)
		{
			//GUILayout.Box("Animation", GUILayout.Width(width), GUILayout.Height(30));
			//DrawTitle("Animation", width);
			EditorGUILayout.Space();

			if (_animClip == null)
			{
				SetNone();
				return;
			}

			//왼쪽엔 기본 세팅/ 우측 (Right2)엔 편집 도구들 + 생성된 Timeline리스트
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Name));//"Name"

			string nextAnimClipName = EditorGUILayout.DelayedTextField(_animClip._name, apGUILOFactory.I.Width(width));

			if (!string.Equals(nextAnimClipName, _animClip._name))
			{
				_animClip._name = nextAnimClipName;
				Editor.RefreshControllerAndHierarchy(false);
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Info, null);
			}



			GUILayout.Space(5);
			//MeshGroup에 연동해야한다.


			Color prevColor = GUI.backgroundColor;

			bool isValidMeshGroup = false;
			if (_animClip._targetMeshGroup != null && _animClip._targetMeshGroup._parentMeshGroup == null)
			{
				//유효한 메시 그룹은
				//- null이 아니고
				//- Root 여야 한다.
				isValidMeshGroup = true;
			}

			//추가 19.11.20
			if (_guiContent_Animation_SelectMeshGroupBtn == null)
			{
				_guiContent_Animation_SelectMeshGroupBtn = new apGUIContentWrapper();
				_guiContent_Animation_SelectMeshGroupBtn.ClearText(false);
				_guiContent_Animation_SelectMeshGroupBtn.AppendSpaceText(1, false);
				_guiContent_Animation_SelectMeshGroupBtn.AppendText(Editor.GetUIWord(UIWORD.SelectMeshGroup), true);
				_guiContent_Animation_SelectMeshGroupBtn.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_MeshGroup));
			}



			if (_animClip._targetMeshGroup == null)
			{
				//GUI.color = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				//GUILayout.Box("Linked Mesh Group\n[ None ]", guiStyle_Box, GUILayout.Width(width), GUILayout.Height(40));
				//GUI.color = prevColor;

				//GUILayout.Space(2);

				//" Select MeshGroup"
				if (GUILayout.Button(_guiContent_Animation_SelectMeshGroupBtn.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35)))
				{
					_loadKey_SelectMeshGroupToAnimClip = apDialog_SelectLinkedMeshGroup.ShowDialog(Editor, _animClip, OnSelectMeshGroupToAnimClip);
				}
			}
			else
			{
				//GUI.color = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				//GUILayout.Box("Linked Mesh Group\n[ " + _animClip._targetMeshGroup._name +" ]", guiStyle_Box, GUILayout.Width(width), GUILayout.Height(40));
				//GUI.color = prevColor;

				//GUILayout.Space(2);

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.TargetMeshGroup));//"Target Mesh Group"
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

				if (isValidMeshGroup)
				{
					GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				}
				else
				{
					//유효하지 않다면 붉은 색
					GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);
				}

				if (_strWrapper_64 == null)
				{
					_strWrapper_64 = new apStringWrapper(64);
				}
				if (_animClip._targetMeshGroup._name.Length > 16)
				{
					_strWrapper_64.Clear();
					_strWrapper_64.Append(_animClip._targetMeshGroup._name.Substring(0, 14), false);
					_strWrapper_64.Append(apStringFactory.I.Dot2, true);
				}
				else
				{
					_strWrapper_64.Clear();
					_strWrapper_64.Append(_animClip._targetMeshGroup._name, true);
				}

				//string strMeshGroupName = _animClip._targetMeshGroup._name;
				//if (strMeshGroupName.Length > 16)
				//{
				//	//이름이 너무 기네용.
				//	strMeshGroupName = strMeshGroupName.Substring(0, 14) + "..";
				//}

				GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - (80 + 2)), apGUILOFactory.I.Height(18));
				GUI.backgroundColor = prevColor;

				if (GUILayout.Button(Editor.GetUIWord(UIWORD.Change), apGUILOFactory.I.Width(80)))//"Change"
				{
					_loadKey_SelectMeshGroupToAnimClip = apDialog_SelectLinkedMeshGroup.ShowDialog(Editor, _animClip, OnSelectMeshGroupToAnimClip);
				}
				EditorGUILayout.EndHorizontal();

				//추가 19.8.23 : 유효하지 않다면 Box로 만들자.
				if (!isValidMeshGroup)
				{
					GUILayout.Space(2);

					GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);

					GUILayout.Box(Editor.GetUIWord(UIWORD.AnimLinkedToInvalidMeshGroup), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));

					GUI.backgroundColor = prevColor;
				}

				GUILayout.Space(5);
				if (GUILayout.Button(Editor.GetUIWord(UIWORD.Duplicate), apGUILOFactory.I.Width(width)))//"Duplicate"
				{
					Editor.Controller.DuplicateAnimClip(_animClip);
					Editor.RefreshControllerAndHierarchy(true);
				}
				GUILayout.Space(5);

				//Timeline을 추가하자
				//Timeline은 ControlParam, Modifier, Bone에 연동된다.
				//TimelineLayer은 각 Timeline에서 어느 Transform(Mesh/MeshGroup), Bone, ControlParam 에 적용 될지를 결정한다.

				//" Add Timeline"
				if (_guiContent_Animation_AddTimeline == null)
				{
					_guiContent_Animation_AddTimeline = new apGUIContentWrapper();
					_guiContent_Animation_AddTimeline.ClearText(false);
					_guiContent_Animation_AddTimeline.AppendSpaceText(1, false);
					_guiContent_Animation_AddTimeline.AppendText(Editor.GetUIWord(UIWORD.AddTimeline), true);
					_guiContent_Animation_AddTimeline.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddTimeline));
				}


				if (GUILayout.Button(_guiContent_Animation_AddTimeline.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
				{
					_loadKey_AddTimelineToAnimClip = apDialog_AddAnimTimeline.ShowDialog(Editor, _animClip, OnAddTimelineToAnimClip);
				}

				//등록된 Timeline 리스트를 보여주자
				GUILayout.Space(10);
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(2);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Timelines), apGUILOFactory.I.Height(25));//"Timelines"

				//GUIStyle guiStyle_None = new GUIStyle(GUIStyle.none);
				GUILayout.Button(apStringFactory.I.None, apGUIStyleWrapper.I.None, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20));//<레이아웃 정렬을 위한의미없는 숨은 버튼
				EditorGUILayout.EndHorizontal();


				if (_guiContent_Animation_TimelineUnit_AnimMod == null)
				{
					_guiContent_Animation_TimelineUnit_AnimMod = new apGUIContentWrapper();
					_guiContent_Animation_TimelineUnit_AnimMod.ClearText(true);
					_guiContent_Animation_TimelineUnit_AnimMod.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithMod));
				}

				if (_guiContent_Animation_TimelineUnit_ControlParam == null)
				{
					_guiContent_Animation_TimelineUnit_ControlParam = new apGUIContentWrapper();
					_guiContent_Animation_TimelineUnit_ControlParam.ClearText(true);
					_guiContent_Animation_TimelineUnit_ControlParam.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithControlParam));
				}


				//등록된 Modifier 리스트를 출력하자
				if (_animClip._timelines.Count > 0)
				{
					for (int i = 0; i < _animClip._timelines.Count; i++)
					{
						DrawTimelineUnit(_animClip._timelines[i], width, 25);
					}
				}
			}

			GUILayout.Space(20);

			apEditorUtil.GUI_DelimeterBoxH(width - 10);

			//등등
			GUILayout.Space(30);

			//"  Remove Animation"
			if (_guiContent_Animation_RemoveAnimation == null)
			{
				_guiContent_Animation_RemoveAnimation = new apGUIContentWrapper();
				_guiContent_Animation_RemoveAnimation.ClearText(false);
				_guiContent_Animation_RemoveAnimation.AppendSpaceText(2, false);
				_guiContent_Animation_RemoveAnimation.AppendText(Editor.GetUIWord(UIWORD.RemoveAnimation), true);
				_guiContent_Animation_RemoveAnimation.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}


			if (GUILayout.Button(_guiContent_Animation_RemoveAnimation.Content, apGUILOFactory.I.Height(24)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove Animation", "Do you want to remove [" + _animClip._name + "]?", "Remove", "Cancel");
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveAnimClip_Title),
																Editor.GetTextFormat(TEXT.RemoveAnimClip_Body, _animClip._name),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel));
				if (isResult)
				{
					Editor.Controller.RemoveAnimClip(_animClip);

					SetNone();
					Editor.RefreshControllerAndHierarchy(true);
					//Editor.RefreshTimelineLayers(true);
				}
			}
		}



		private void OnSelectMeshGroupToAnimClip(bool isSuccess, object loadKey, apMeshGroup meshGroup, apAnimClip targetAnimClip)
		{
			if (!isSuccess || _loadKey_SelectMeshGroupToAnimClip != loadKey
				|| meshGroup == null || _animClip != targetAnimClip)
			{
				_loadKey_SelectMeshGroupToAnimClip = null;
				return;
			}

			_loadKey_SelectMeshGroupToAnimClip = null;

			//추가 3.29 : 누군가의 자식 메시 그룹인 경우
			if (meshGroup._parentMeshGroup != null)
			{
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.DLG_ChildMeshGroupAndAnimClip_Title),
																Editor.GetText(TEXT.DLG_ChildMeshGroupAndAnimClip_Body),
																Editor.GetText(TEXT.Okay),
																Editor.GetText(TEXT.Cancel)
																);

				if (!isResult)
				{
					return;
				}
			}

			if (_animClip._targetMeshGroup != null)
			{
				if (_animClip._targetMeshGroup == meshGroup)
				{
					//바뀐게 없다 => Pass
					return;
				}

				//추가 19.8.20 : 데이터가 유지될 수도 있다.
				//조건 : 대상이 기존의 메시 그룹의 최상위 부모여야 한다. ( 그 대상은 Parent가 없다. )
				if (meshGroup._parentMeshGroup == null && IsRootParentMeshGroup(meshGroup, _animClip._targetMeshGroup))
				{
					int iBtn = EditorUtility.DisplayDialogComplex(Editor.GetText(TEXT.DLG_MigrateAnimationDataToParentMeshGroup_Title),
																	Editor.GetText(TEXT.DLG_MigrateAnimationDataToParentMeshGroup_Body),
																	Editor.GetText(TEXT.Keep_data),
																	Editor.GetText(TEXT.Clear_data),
																	Editor.GetText(TEXT.Cancel)
																	);

					if (iBtn == 0)
					{
						//데이터 유지 > 별도의 함수
						Editor.Controller.MigrateAnimClipToMeshGroup(_animClip, meshGroup);
						return;
					}
					else if (iBtn == 1)
					{
						//데이터 삭제 > 리턴없이 그냥 진행
					}
					else
					{
						//취소
						return;
					}
				}
				else
				{
					//그 외의 경우
					//bool isResult = EditorUtility.DisplayDialog("Is Change Mesh Group", "Is Change Mesh Group?", "Change", "Cancel");
					bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.AnimClipMeshGroupChanged_Title),
																	Editor.GetText(TEXT.AnimClipMeshGroupChanged_Body),
																	Editor.GetText(TEXT.Okay),
																	Editor.GetText(TEXT.Cancel)
																	);
					if (!isResult)
					{
						//기존 것에서 변경을 하지 않는다 => Pass
						return;
					}
				}


			}

			//Undo
			//apEditorUtil.SetRecord_PortraitMeshGroupAndAllModifiers(apUndoGroupData.ACTION.Anim_SetMeshGroup, Editor, Editor._portrait, meshGroup, null, false);

			//변경 20.3.19 : 되돌아갈때를 위해서 원래 두개의 메시 그룹과 해당 모든 모디파이어를 저장해야하지만, 그냥 다 하자.
			apEditorUtil.SetRecord_PortraitAllMeshGroupAndAllModifiers(apUndoGroupData.ACTION.Anim_SetMeshGroup, Editor, Editor._portrait, meshGroup, false);


			//기존의 Timeline이 있다면 다 날리자

			//_isAnimAutoKey = false;
			//_isAnimEditing = false;
			_exAnimEditingMode = EX_EDIT.None;
			_isAnimSelectionLock = false;

			SetAnimTimeline(null, true);
			SetSubMeshTransformForAnimClipEdit(null, true, false);//하나만 null을 하면 모두 선택이 취소된다.

			_animClip._timelines.Clear();//<<그냥 클리어
			bool isChanged = _animClip._targetMeshGroup != meshGroup;
			_animClip._targetMeshGroup = meshGroup;
			_animClip._targetMeshGroupID = meshGroup._uniqueID;


			if (meshGroup != null)
			{
				meshGroup._modifierStack.RefreshAndSort(true);
				meshGroup.ResetBoneGUIVisible();
			}
			if (isChanged)
			{
				//MeshGroup 선택 후 초기화
				if (_animClip._targetMeshGroup != null)
				{
					//이전 방식
					//_animClip._targetMeshGroup.SetDirtyToReset();
					//_animClip._targetMeshGroup.SetDirtyToSort();
					////_animClip._targetMeshGroup.SetAllRenderUnitForceUpdate();
					//_animClip._targetMeshGroup.RefreshForce(true);

					//_animClip._targetMeshGroup.LinkModMeshRenderUnits();
					//_animClip._targetMeshGroup.RefreshModifierLink();

					//Debug.LogError("TODO : Check 이거 정상 작동되나");
					apUtil.LinkRefresh.Set_AnimClip(_animClip);
					
					_animClip._targetMeshGroup.SetDirtyToReset();
					_animClip._targetMeshGroup.RefreshForce(true, 0.0f, apUtil.LinkRefresh);
					_animClip._targetMeshGroup.RefreshModifierLink(apUtil.LinkRefresh);

					_animClip._targetMeshGroup._modifierStack.RefreshAndSort(true);
				}


				Editor.Hierarchy_AnimClip.ResetSubUnits();
			}
			Editor.RefreshControllerAndHierarchy(true);

		}

		//두개의 MeshGroup의 관계가 재귀적인 부자관계인지 확인
		private bool IsRootParentMeshGroup(apMeshGroup parentMeshGroup, apMeshGroup childMeshGroup)
		{
			if (childMeshGroup._parentMeshGroup == null)
			{
				return false;
			}
			int cnt = 0;
			apMeshGroup curMeshGroup = childMeshGroup;

			while (true)
			{
				if (curMeshGroup == null) { return false; }
				if (curMeshGroup._parentMeshGroup == null) { return false; }

				if (curMeshGroup._parentMeshGroup == parentMeshGroup)
				{
					//성공
					return true;
				}

				//에러 검출
				if (curMeshGroup._parentMeshGroup == curMeshGroup ||
					curMeshGroup._parentMeshGroup == childMeshGroup)
				{
					return false;
				}

				//1레벨 위로 
				curMeshGroup = curMeshGroup._parentMeshGroup;
				cnt++;

				if (cnt > 100)
				{
					//100레벨이나 위로 올라갔다고? > 에러
					Debug.LogError("AnyPortrait : IsRootParentMeshGroup Error");
					break;
				}
			}
			return false;
		}



		//Dialog 이벤트에 의해서 Timeline을 추가하자
		private void OnAddTimelineToAnimClip(bool isSuccess, object loadKey, apAnimClip.LINK_TYPE linkType, int modifierUniqueID, apAnimClip targetAnimClip)
		{
			if (!isSuccess || _loadKey_AddTimelineToAnimClip != loadKey ||
				_animClip != targetAnimClip)
			{
				_loadKey_AddTimelineToAnimClip = null;
				return;
			}

			_loadKey_AddTimelineToAnimClip = null;

			Editor.Controller.AddAnimTimeline(linkType, modifierUniqueID, targetAnimClip);
		}


		private void DrawTimelineUnit(apAnimTimeline timeline, int width, int height)
		{
			Rect lastRect = GUILayoutUtility.GetLastRect();
			//Color textColor = GUI.skin.label.normal.textColor;
			GUIStyle curGUIStyle = null;//<<최적화된 코드
			if (AnimTimeline == timeline)
			{
				Color prevColor = GUI.backgroundColor;

				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
					//textColor = Color.cyan;
				}
				else
				{
					GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
					//textColor = Color.white;
				}

				GUI.Box(new Rect(lastRect.x, lastRect.y + height, width + 15, height), apStringFactory.I.None);
				GUI.backgroundColor = prevColor;

				curGUIStyle = apGUIStyleWrapper.I.None_White2Cyan;
			}
			else
			{
				curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;
			}

			//GUIStyle guiStyle_None = new GUIStyle(GUIStyle.none);
			//guiStyle_None.normal.textColor = textColor;

			//이전
			//apImageSet.PRESET iconType = apImageSet.PRESET.Anim_WithMod;
			//switch (timeline._linkType)
			//{
			//	case apAnimClip.LINK_TYPE.AnimatedModifier:
			//		iconType = apImageSet.PRESET.Anim_WithMod;
			//		break;

			//	case apAnimClip.LINK_TYPE.ControlParam:
			//		iconType = apImageSet.PRESET.Anim_WithControlParam;
			//		break;
			//}

			//변경
			apGUIContentWrapper curGUIContentWrapper = timeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier ? _guiContent_Animation_TimelineUnit_AnimMod : _guiContent_Animation_TimelineUnit_ControlParam;

			curGUIContentWrapper.ClearText(false);
			curGUIContentWrapper.AppendSpaceText(1, false);
			curGUIContentWrapper.AppendText(timeline.DisplayName, true);

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height));
			GUILayout.Space(10);

			//이전
			//if (GUILayout.Button(new GUIContent(" " + timeline.DisplayName, Editor.ImageSet.Get(iconType)), guiStyle_None, GUILayout.Width(width - 40), GUILayout.Height(height)))
			if (GUILayout.Button(curGUIContentWrapper.Content, curGUIStyle, apGUILOFactory.I.Width(width - 40), apGUILOFactory.I.Height(height)))
			{
				SetAnimTimeline(timeline, true);
				SetAnimTimelineLayer(null, true);
				SetAnimKeyframe(null, false, apGizmos.SELECT_TYPE.New);
			}

			Texture2D activeBtn = null;
			bool isActiveMod = false;
			if (timeline._isActiveInEditing)
			{
				activeBtn = Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Active);
				isActiveMod = true;
			}
			else
			{
				activeBtn = Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Deactive);
				isActiveMod = false;
			}
			if (GUILayout.Button(activeBtn, curGUIStyle, apGUILOFactory.I.Width(height), apGUILOFactory.I.Height(height)))
			{
				//일단 토글한다.
				timeline._isActiveInEditing = !isActiveMod;
			}
			EditorGUILayout.EndHorizontal();
		}





		private void Draw_Overall(int width, int height)
		{
			//GUILayout.Box("Overall", GUILayout.Width(width), GUILayout.Height(30));
			//DrawTitle("Overall", width);
			EditorGUILayout.Space();

			apRootUnit rootUnit = RootUnit;
			if (rootUnit == null)
			{
				SetNone();
				return;
			}

			Color prevColor = GUI.backgroundColor;

			//Setting / Capture Tab
			bool isRootUnitTab_Setting = (Editor._rootUnitEditMode == apEditor.ROOTUNIT_EDIT_MODE.Setting);
			bool isRootUnitTab_Capture = (Editor._rootUnitEditMode == apEditor.ROOTUNIT_EDIT_MODE.Capture);

			int subTabWidth = (width / 2) - 5;
			int subTabHeight = 24;
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));
			GUILayout.Space(5);

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Setting), 1, Editor.GetUIWord(UIWORD.Setting), isRootUnitTab_Setting, true, subTabWidth, subTabHeight, apStringFactory.I.SettingsOfRootUnit))//"Settings of Root Unit"
			{
				if (!isRootUnitTab_Setting)
				{
					Editor._rootUnitEditMode = apEditor.ROOTUNIT_EDIT_MODE.Setting;
				}
			}

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Capture_Tab), 1, Editor.GetUIWord(UIWORD.Capture), isRootUnitTab_Capture, true, subTabWidth, subTabHeight, apStringFactory.I.CapturingTheScreenShot))//"Capturing the screenshot"
			{
				if (!isRootUnitTab_Capture)
				{
					if (apVersion.I.IsDemo)
					{
						//추가 : 데모 버전일 때에는 Capture 기능을 사용할 수 없다.
						EditorUtility.DisplayDialog(
										Editor.GetText(TEXT.DemoLimitation_Title),
										Editor.GetText(TEXT.DemoLimitation_Body),
										Editor.GetText(TEXT.Okay));
					}
					else
					{
						Editor._rootUnitEditMode = apEditor.ROOTUNIT_EDIT_MODE.Capture;
					}
				}
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			if (Editor._rootUnitEditMode == apEditor.ROOTUNIT_EDIT_MODE.Setting)
			{
				//1. Setting 메뉴
				//------------------------------------------------
				//1. 연결된 MeshGroup 설정 (+ 해제)
				apMeshGroup targetMeshGroup = rootUnit._childMeshGroup;

				if (_strWrapper_64 == null)
				{
					_strWrapper_64 = new apStringWrapper(64);
				}
				_strWrapper_64.Clear();

				//string strMeshGroupName = "";
				Color bgColor = Color.black;
				if (targetMeshGroup != null)
				{
					//strMeshGroupName = "[" + targetMeshGroup._name + "]";
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
					_strWrapper_64.Append(targetMeshGroup._name, false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

					bgColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				}
				else
				{
					//strMeshGroupName = "Error! No MeshGroup Linked";

					_strWrapper_64.Append(apStringFactory.I.ErrorNoMeshGroupLinked, true);

					bgColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				}
				GUI.backgroundColor = bgColor;

				//GUIStyle guiStyleBox = new GUIStyle(GUI.skin.box);
				//guiStyleBox.alignment = TextAnchor.MiddleCenter;
				//guiStyleBox.normal.textColor = apEditorUtil.BoxTextColor;

				GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;

				GUILayout.Space(20);
				apEditorUtil.GUI_DelimeterBoxH(width - 10);
				GUILayout.Space(20);

				//2. 애니메이션 제어

				apAnimClip curAnimClip = RootUnitAnimClip;
				bool isAnimClipAvailable = (curAnimClip != null);


				Texture2D icon_FirstFrame = Editor.ImageSet.Get(apImageSet.PRESET.Anim_FirstFrame);
				Texture2D icon_PrevFrame = Editor.ImageSet.Get(apImageSet.PRESET.Anim_PrevFrame);

				Texture2D icon_NextFrame = Editor.ImageSet.Get(apImageSet.PRESET.Anim_NextFrame);
				Texture2D icon_LastFrame = Editor.ImageSet.Get(apImageSet.PRESET.Anim_LastFrame);

				Texture2D icon_PlayPause = null;
				if (curAnimClip != null)
				{
					if (curAnimClip.IsPlaying_Editor) { icon_PlayPause = Editor.ImageSet.Get(apImageSet.PRESET.Anim_Pause); }
					else { icon_PlayPause = Editor.ImageSet.Get(apImageSet.PRESET.Anim_Play); }
				}
				else
				{
					icon_PlayPause = Editor.ImageSet.Get(apImageSet.PRESET.Anim_Play);
				}

				int btnSize = 30;
				int btnWidth_Play = 45;
				int btnWidth_PrevNext = 35;
				int btnWidth_FirstLast = (width - (btnWidth_Play + btnWidth_PrevNext * 2 + 4 * 3 + 5)) / 2;
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(btnSize));
				GUILayout.Space(2);
				if (apEditorUtil.ToggledButton_2Side(icon_FirstFrame, false, isAnimClipAvailable, btnWidth_FirstLast, btnSize))
				{
					if (curAnimClip != null)
					{
						curAnimClip.SetFrame_Editor(curAnimClip.StartFrame);
						curAnimClip.Pause_Editor();
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
				}
				if (apEditorUtil.ToggledButton_2Side(icon_PrevFrame, false, isAnimClipAvailable, btnWidth_PrevNext, btnSize))
				{
					if (curAnimClip != null)
					{
						int prevFrame = curAnimClip.CurFrame - 1;
						if (prevFrame < curAnimClip.StartFrame && curAnimClip.IsLoop)
						{
							prevFrame = curAnimClip.EndFrame;
						}
						curAnimClip.SetFrame_Editor(prevFrame);
						curAnimClip.Pause_Editor();
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
				}
				if (apEditorUtil.ToggledButton_2Side(icon_PlayPause, false, isAnimClipAvailable, btnWidth_Play, btnSize))
				{
					if (curAnimClip != null)
					{
						if (curAnimClip.IsPlaying_Editor)
						{
							curAnimClip.Pause_Editor();
						}
						else
						{
							if (curAnimClip.CurFrame == curAnimClip.EndFrame &&
								!curAnimClip.IsLoop)
							{
								curAnimClip.SetFrame_Editor(curAnimClip.StartFrame);
							}

							curAnimClip.Play_Editor();
						}
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.


					}
				}
				if (apEditorUtil.ToggledButton_2Side(icon_NextFrame, false, isAnimClipAvailable, btnWidth_PrevNext, btnSize))
				{
					if (curAnimClip != null)
					{
						int nextFrame = curAnimClip.CurFrame + 1;
						if (nextFrame > curAnimClip.EndFrame && curAnimClip.IsLoop)
						{
							nextFrame = curAnimClip.StartFrame;
						}
						curAnimClip.SetFrame_Editor(nextFrame);
						curAnimClip.Pause_Editor();
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
				}
				if (apEditorUtil.ToggledButton_2Side(icon_LastFrame, false, isAnimClipAvailable, btnWidth_FirstLast, btnSize))
				{
					if (curAnimClip != null)
					{
						curAnimClip.SetFrame_Editor(curAnimClip.EndFrame);
						curAnimClip.Pause_Editor();
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
				}

				EditorGUILayout.EndHorizontal();

				int curFrame = 0;
				int startFrame = 0;
				int endFrame = 10;
				if (curAnimClip != null)
				{
					curFrame = curAnimClip.CurFrame;
					startFrame = curAnimClip.StartFrame;
					endFrame = curAnimClip.EndFrame;
				}
				int sliderFrame = EditorGUILayout.IntSlider(curFrame, startFrame, endFrame, apGUILOFactory.I.Width(width));
				if (sliderFrame != curFrame)
				{
					if (curAnimClip != null)
					{
						curAnimClip.SetFrame_Editor(sliderFrame);
						curAnimClip.Pause_Editor();
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
				}

				GUILayout.Space(5);

				//추가 : 자동 플레이하는 AnimClip을 선택한다.
				bool isAutoPlayAnimClip = false;
				if (curAnimClip != null)
				{
					isAutoPlayAnimClip = (_portrait._autoPlayAnimClipID == curAnimClip._uniqueID);
				}
				if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.AutoPlayEnabled), Editor.GetUIWord(UIWORD.AutoPlayDisabled), isAutoPlayAnimClip, curAnimClip != null, width, 25))//"Auto Play Enabled", "Auto Play Disabled"
				{
					if (curAnimClip != null)
					{
						apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Portrait_SettingChanged, Editor, _portrait, null, false);

						if (_portrait._autoPlayAnimClipID == curAnimClip._uniqueID)
						{
							//선택됨 -> 선택 해제
							_portrait._autoPlayAnimClipID = -1;

						}
						else
						{
							//선택 해제 -> 선택
							_portrait._autoPlayAnimClipID = curAnimClip._uniqueID;
						}


					}

				}


				GUILayout.Space(20);
				apEditorUtil.GUI_DelimeterBoxH(width - 10);
				GUILayout.Space(20);

				//3. 애니메이션 리스트
				List<apAnimClip> subAnimClips = RootUnitAnimClipList;
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AnimationClips), apGUILOFactory.I.Width(width));//"Animation Clips"
				GUILayout.Space(5);
				if (subAnimClips != null && subAnimClips.Count > 0)
				{
					apAnimClip nextSelectedAnimClip = null;

					//GUIStyle guiNone = new GUIStyle(GUIStyle.none);
					//guiNone.normal.textColor = GUI.skin.label.normal.textColor;

					//GUIStyle guiSelected = new GUIStyle(GUIStyle.none);
					//if (EditorGUIUtility.isProSkin)
					//{
					//	guiSelected.normal.textColor = Color.cyan;
					//}
					//else
					//{
					//	guiSelected.normal.textColor = Color.white;
					//}



					Rect lastRect = GUILayoutUtility.GetLastRect();

					int scrollWidth = width - 20;

					//Texture2D icon_Anim = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation);

					GUIStyle curGUIStyle = null;//최적화된 코드
					for (int i = 0; i < subAnimClips.Count; i++)
					{
						//GUIStyle curGUIStyle = guiNone;
						curGUIStyle = null;

						apAnimClip subAnimClip = subAnimClips[i];
						if (subAnimClip == curAnimClip)
						{
							lastRect = GUILayoutUtility.GetLastRect();

							if (EditorGUIUtility.isProSkin)
							{
								GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
							}
							else
							{
								GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
							}

							//int offsetHeight = 20 + 3;
							int offsetHeight = 1 + 3;
							if (i == 0)
							{
								offsetHeight = 4 + 3;
							}

							GUI.Box(new Rect(lastRect.x, lastRect.y + offsetHeight, scrollWidth + 35, 24), apStringFactory.I.None);

							GUI.backgroundColor = prevColor;

							//curGUIStyle = guiSelected;
							curGUIStyle = apGUIStyleWrapper.I.None_White2Cyan;
						}
						else
						{
							curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;
						}

						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
						GUILayout.Space(5);

						if (_guiContent_Overall_SelectedAnimClp == null)
						{
							_guiContent_Overall_SelectedAnimClp = new apGUIContentWrapper();
							_guiContent_Overall_SelectedAnimClp.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation));
						}
						_guiContent_Overall_SelectedAnimClp.ClearText(false);
						_guiContent_Overall_SelectedAnimClp.AppendSpaceText(1, false);
						_guiContent_Overall_SelectedAnimClp.AppendText(subAnimClip._name, true);


						//이전
						//if (GUILayout.Button(new GUIContent(" " + subAnimClip._name, icon_Anim),
						//				curGUIStyle,
						//				GUILayout.Width(scrollWidth - 5), GUILayout.Height(24)))

						//변경
						if (GUILayout.Button(_guiContent_Overall_SelectedAnimClp.Content,
												curGUIStyle,
												apGUILOFactory.I.Width(scrollWidth - 5), apGUILOFactory.I.Height(24)))
						{
							nextSelectedAnimClip = subAnimClip;
						}
						EditorGUILayout.EndHorizontal();
						GUILayout.Space(4);

					}

					if (nextSelectedAnimClip != null)
					{
						for (int i = 0; i < Editor._portrait._animClips.Count; i++)
						{
							Editor._portrait._animClips[i]._isSelectedInEditor = false;
						}

						_curRootUnitAnimClip = nextSelectedAnimClip;
						_curRootUnitAnimClip.LinkEditor(Editor._portrait);
						_curRootUnitAnimClip.RefreshTimelines(null);//<<모든 타임라인 Refresh
						_curRootUnitAnimClip.SetFrame_Editor(_curRootUnitAnimClip.StartFrame);
						_curRootUnitAnimClip.Pause_Editor();

						_curRootUnitAnimClip._isSelectedInEditor = true;


						//통계 재계산 요청
						SetStatisticsRefresh();

						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.

						//Debug.Log("Select Root Unit Anim Clip : " + _curRootUnitAnimClip._name);
					}
				}



				GUILayout.Space(20);
				apEditorUtil.GUI_DelimeterBoxH(width - 10);
				GUILayout.Space(20);
				//MainMesh에서 해제

				if(_guiContent_Overall_Unregister == null)
				{
					_guiContent_Overall_Unregister = new apGUIContentWrapper();
				}
				_guiContent_Overall_Unregister.ClearText(false);
				_guiContent_Overall_Unregister.AppendSpaceText(2, false);
				_guiContent_Overall_Unregister.AppendText(Editor.GetUIWord(UIWORD.UnregistRootUnit), true);
				_guiContent_Overall_Unregister.SetImage(_editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
				

				if (GUILayout.Button(	_guiContent_Overall_Unregister.Content,
										apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))//"Unregist Root Unit"
				{
					//Debug.LogError("TODO : MainMeshGroup 해제");
					apMeshGroup targetRootMeshGroup = rootUnit._childMeshGroup;
					if (targetRootMeshGroup != null)
					{
						apEditorUtil.SetRecord_PortraitMeshGroup(apUndoGroupData.ACTION.Portrait_SetMeshGroup, Editor, _portrait, targetRootMeshGroup, null, false, true);

						_portrait._mainMeshGroupIDList.Remove(targetRootMeshGroup._uniqueID);
						_portrait._mainMeshGroupList.Remove(targetRootMeshGroup);

						_portrait._rootUnits.Remove(rootUnit);

						SetNone();

						Editor.RefreshControllerAndHierarchy(false);
						Editor.SetHierarchyFilter(apEditor.HIERARCHY_FILTER.RootUnit, true);
					}
				}
			}
			else
			{
				//2. Capture 메뉴
				//-------------------------------------------

				//>>여기서부터
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));
				GUILayout.Space(5);
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Capture_Thumbnail),
												1, Editor.GetUIWord(UIWORD.CaptureTabThumbnail),
												Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.Thumbnail, true, subTabWidth, subTabHeight,
												apStringFactory.I.MakeAThumbnail))//"Make a Thumbnail"
				{
					//"Thumbnail"
					Editor._rootUnitCaptureMode = apEditor.ROOTUNIT_CAPTURE_MODE.Thumbnail;
				}

				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Capture_Image),
												1, Editor.GetUIWord(UIWORD.CaptureTabScreenshot),
												Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.ScreenShot, true, subTabWidth, subTabHeight,
												apStringFactory.I.MakeAScreenshot))//"Make a Screenshot"
				{
					//"Screen Shot"
					Editor._rootUnitCaptureMode = apEditor.ROOTUNIT_CAPTURE_MODE.ScreenShot;
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(subTabHeight));

				GUILayout.Space(5);

				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Capture_GIF),
												1, Editor.GetUIWord(UIWORD.CaptureTabGIFAnim),
												Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.GIFAnimation, true, subTabWidth, subTabHeight,
												apStringFactory.I.MakeAGIFAnimation))//"Make a GIF Animation"
				{
					//"GIF Anim"
					Editor._rootUnitCaptureMode = apEditor.ROOTUNIT_CAPTURE_MODE.GIFAnimation;
				}

				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Capture_Sprite),
												1, Editor.GetUIWord(UIWORD.CaptureTabSpritesheet),
												Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet, true, subTabWidth, subTabHeight,
												apStringFactory.I.MakeSpriteSheets))//"Make Spritesheets"
				{
					//"Spritesheet"
					Editor._rootUnitCaptureMode = apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet;
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(10);


				int settingWidth_Label = 80;
				int settingWidth_Value = width - (settingWidth_Label + 8);

				//각 캡쳐별로 설정을 한다.
				//공통 설정도 있고 아닌 경우도 있다.

				//Setting
				//------------------------
				EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Setting));//"Setting"
				GUILayout.Space(5);

				//Position
				//------------------------
				EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Position));//"Position"

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(apStringFactory.I.X, apGUILOFactory.I.Width(settingWidth_Label));
				int posX = EditorGUILayout.DelayedIntField(Editor._captureFrame_PosX, apGUILOFactory.I.Width(settingWidth_Value));
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(apStringFactory.I.Y, apGUILOFactory.I.Width(settingWidth_Label));
				int posY = EditorGUILayout.DelayedIntField(Editor._captureFrame_PosY, apGUILOFactory.I.Width(settingWidth_Value));
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(5);

				//Capture Size
				//------------------------
				//Thumbnail인 경우 Width만 설정한다. (Height는 자동 계산)
				EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_CaptureSize));//"Capture Size"

				//Src Width
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				//"Width"
				EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Width), apGUILOFactory.I.Width(settingWidth_Label));
				int srcSizeWidth = EditorGUILayout.DelayedIntField(Editor._captureFrame_SrcWidth, apGUILOFactory.I.Width(settingWidth_Value));
				EditorGUILayout.EndHorizontal();


				int srcSizeHeight = Editor._captureFrame_SrcHeight;
				//Src Height : Tumbnail이 아닌 경우만
				if (Editor._rootUnitCaptureMode != apEditor.ROOTUNIT_CAPTURE_MODE.Thumbnail)
				{
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Height), apGUILOFactory.I.Width(settingWidth_Label));//"Height"

					srcSizeHeight = EditorGUILayout.DelayedIntField(Editor._captureFrame_SrcHeight, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();
				}

				if (srcSizeWidth < 8) { srcSizeWidth = 8; }
				if (srcSizeHeight < 8) { srcSizeHeight = 8; }

				GUILayout.Space(5);

				//File Size
				//-------------------------------
				int dstSizeWidth = Editor._captureFrame_DstWidth;
				int dstSizeHeight = Editor._captureFrame_DstHeight;
				int spriteUnitSizeWidth = Editor._captureFrame_SpriteUnitWidth;
				int spriteUnitSizeHeight = Editor._captureFrame_SpriteUnitHeight;

				apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE spritePackImageWidth = Editor._captureSpritePackImageWidth;
				apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE spritePackImageHeight = Editor._captureSpritePackImageHeight;
				apEditor.CAPTURE_SPRITE_TRIM_METHOD spriteTrimSize = Editor._captureSpriteTrimSize;
				int spriteMargin = Editor._captureFrame_SpriteMargin;
				bool isPhysicsEnabled = Editor._captureFrame_IsPhysics;

				//Screenshot / GIF Animation은 Dst Image Size를 결정한다.
				if (Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.ScreenShot ||
					Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.GIFAnimation)
				{
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_ImageSize));//"Image Size"

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Width"
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Width), apGUILOFactory.I.Width(settingWidth_Label));
					dstSizeWidth = EditorGUILayout.DelayedIntField(Editor._captureFrame_DstWidth, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Height"
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Height), apGUILOFactory.I.Width(settingWidth_Label));
					dstSizeHeight = EditorGUILayout.DelayedIntField(Editor._captureFrame_DstHeight, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();

					GUILayout.Space(5);
				}
				else if (Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet)
				{
					//Sprite Sheet는 Capture Unit과 Pack Image 사이즈, 압축 방식을 결정한다.
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ImageSizePerFrame));//"Image Size"
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Width"
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Width), apGUILOFactory.I.Width(settingWidth_Label));
					spriteUnitSizeWidth = EditorGUILayout.DelayedIntField(Editor._captureFrame_SpriteUnitWidth, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Height"
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Height), apGUILOFactory.I.Width(settingWidth_Label));
					spriteUnitSizeHeight = EditorGUILayout.DelayedIntField(Editor._captureFrame_SpriteUnitHeight, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();

					GUILayout.Space(5);



					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SizeofSpritesheet));//"Size of Sprite Sheet"
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Width"
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Width), apGUILOFactory.I.Width(settingWidth_Label));
					spritePackImageWidth = (apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE)EditorGUILayout.Popup((int)Editor._captureSpritePackImageWidth, _captureSpritePackSizeNames, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Height"
					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_Height), apGUILOFactory.I.Width(settingWidth_Label));
					spritePackImageHeight = (apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE)EditorGUILayout.Popup((int)Editor._captureSpritePackImageHeight, _captureSpritePackSizeNames, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();

					if ((int)spritePackImageWidth < 0) { spritePackImageWidth = apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s256; }
					else if ((int)spritePackImageWidth > 4) { spritePackImageWidth = apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s4096; }

					if ((int)spritePackImageHeight < 0) { spritePackImageHeight = apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s256; }
					else if ((int)spritePackImageHeight > 4) { spritePackImageHeight = apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s4096; }

					GUILayout.Space(5);

					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SpriteSizeCompression));//"Image size compression method"
					spriteTrimSize = (apEditor.CAPTURE_SPRITE_TRIM_METHOD)EditorGUILayout.EnumPopup(Editor._captureSpriteTrimSize, apGUILOFactory.I.Width(width));

					GUILayout.Space(5);

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					//"Width"
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SpriteMargin), apGUILOFactory.I.Width(settingWidth_Label));//"Margin"
					spriteMargin = EditorGUILayout.DelayedIntField(Editor._captureFrame_SpriteMargin, apGUILOFactory.I.Width(settingWidth_Value));
					EditorGUILayout.EndHorizontal();

				}



				//Color와 물리와 AspectRatio
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
				GUILayout.Space(5);

				EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_BGColor), apGUILOFactory.I.Width(settingWidth_Label));//"BG Color"
				Color prevCaptureColor = Editor._captureFrame_Color;
				try
				{
					Editor._captureFrame_Color = EditorGUILayout.ColorField(Editor._captureFrame_Color, apGUILOFactory.I.Width(settingWidth_Value));
				}
				catch (Exception) { }
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(5);

				if (Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.GIFAnimation ||
					Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet)
				{
					//GIF, Spritesheet인 경우 물리 효과를 정해야 한다.
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					GUILayout.Space(5);

					EditorGUILayout.LabelField(Editor.GetText(TEXT.DLG_CaptureIsPhysics), apGUILOFactory.I.Width(width - (10 + 30)));
					isPhysicsEnabled = EditorGUILayout.Toggle(Editor._captureFrame_IsPhysics, apGUILOFactory.I.Width(30));
					EditorGUILayout.EndHorizontal();
					GUILayout.Space(5);
				}

				GUILayout.Space(5);

				if (Editor._rootUnitCaptureMode != apEditor.ROOTUNIT_CAPTURE_MODE.Thumbnail)
				{
					//Thumbnail이 아니라면 Aspect Ratio가 중요하다
					//Aspect Ratio
					if (apEditorUtil.ToggledButton_2Side(Editor.GetText(TEXT.DLG_FixedAspectRatio), Editor.GetText(TEXT.DLG_NotFixedAspectRatio), Editor._isCaptureAspectRatioFixed, true, width, 20))
					{
						Editor._isCaptureAspectRatioFixed = !Editor._isCaptureAspectRatioFixed;

						if (Editor._isCaptureAspectRatioFixed)
						{
							//AspectRatio를 굳혔다.
							//Dst계열 변수를 Src에 맞춘다.
							//Height를 고정, Width를 맞춘다.
							if (Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet)
							{
								//Spritesheet라면 Unit 사이즈를 변경
								Editor._captureFrame_SpriteUnitWidth = apEditorUtil.GetAspectRatio_Width(
																							Editor._captureFrame_SpriteUnitHeight,
																							Editor._captureFrame_SrcWidth,
																							Editor._captureFrame_SrcHeight);
								spriteUnitSizeWidth = Editor._captureFrame_SpriteUnitWidth;
							}
							else
							{
								//Screenshot과 GIF Animation이라면 Dst 사이즈를 변경
								Editor._captureFrame_DstWidth = apEditorUtil.GetAspectRatio_Width(
																							Editor._captureFrame_DstHeight,
																							Editor._captureFrame_SrcWidth,
																							Editor._captureFrame_SrcHeight);
								dstSizeWidth = Editor._captureFrame_DstWidth;
							}

						}

						Editor.SaveEditorPref();
						apEditorUtil.ReleaseGUIFocus();
					}

					GUILayout.Space(5);
				}




				//AspectRatio를 맞추어보자
				if (Editor._isCaptureAspectRatioFixed)
				{
					if (Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.ScreenShot ||
						Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.GIFAnimation)
					{
						//Screenshot / GIFAnimation은 Src, Dst를 서로 맞춘다.
						if (srcSizeWidth != Editor._captureFrame_SrcWidth)
						{
							//Width가 바뀌었다. => Height를 맞추자
							srcSizeHeight = apEditorUtil.GetAspectRatio_Height(srcSizeWidth, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
							//>> Dst도 바꾸자 => Width
							dstSizeWidth = apEditorUtil.GetAspectRatio_Width(dstSizeHeight, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
						}
						else if (srcSizeHeight != Editor._captureFrame_SrcHeight)
						{
							//Height가 바뀌었다. => Width를 맞추자
							srcSizeWidth = apEditorUtil.GetAspectRatio_Width(srcSizeHeight, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
							//>> Dst도 바꾸자 => Height
							dstSizeHeight = apEditorUtil.GetAspectRatio_Height(dstSizeWidth, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
						}
						else if (dstSizeWidth != Editor._captureFrame_DstWidth)
						{
							//Width가 바뀌었다. => Height를 맞추자
							dstSizeHeight = apEditorUtil.GetAspectRatio_Height(dstSizeWidth, Editor._captureFrame_DstWidth, Editor._captureFrame_DstHeight);
							//>> Src도 바꾸다 => Width
							srcSizeWidth = apEditorUtil.GetAspectRatio_Width(srcSizeHeight, Editor._captureFrame_DstWidth, Editor._captureFrame_DstHeight);
						}
						else if (dstSizeHeight != Editor._captureFrame_DstHeight)
						{
							//Height가 바뀌었다. => Width를 맞추자
							dstSizeWidth = apEditorUtil.GetAspectRatio_Width(dstSizeHeight, Editor._captureFrame_DstWidth, Editor._captureFrame_DstHeight);
							//>> Dst도 바꾸자 => Height
							srcSizeHeight = apEditorUtil.GetAspectRatio_Height(srcSizeWidth, Editor._captureFrame_DstWidth, Editor._captureFrame_DstHeight);
						}
					}
					else if (Editor._rootUnitCaptureMode == apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet)
					{
						//Sprite sheet는 Src, Unit을 맞춘다.
						if (srcSizeWidth != Editor._captureFrame_SrcWidth)
						{
							//Width가 바뀌었다. => Height를 맞추자
							srcSizeHeight = apEditorUtil.GetAspectRatio_Height(srcSizeWidth, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
							//>> Dst도 바꾸자 => Width
							spriteUnitSizeWidth = apEditorUtil.GetAspectRatio_Width(spriteUnitSizeHeight, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
						}
						else if (srcSizeHeight != Editor._captureFrame_SrcHeight)
						{
							//Height가 바뀌었다. => Width를 맞추자
							srcSizeWidth = apEditorUtil.GetAspectRatio_Width(srcSizeHeight, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
							//>> Dst도 바꾸자 => Height
							spriteUnitSizeHeight = apEditorUtil.GetAspectRatio_Height(spriteUnitSizeWidth, Editor._captureFrame_SrcWidth, Editor._captureFrame_SrcHeight);
						}
						else if (spriteUnitSizeWidth != Editor._captureFrame_SpriteUnitWidth)
						{
							//Width가 바뀌었다. => Height를 맞추자
							spriteUnitSizeHeight = apEditorUtil.GetAspectRatio_Height(spriteUnitSizeWidth, Editor._captureFrame_SpriteUnitWidth, Editor._captureFrame_SpriteUnitHeight);
							//>> Src도 바꾸다 => Width
							srcSizeWidth = apEditorUtil.GetAspectRatio_Width(srcSizeHeight, Editor._captureFrame_SpriteUnitWidth, Editor._captureFrame_SpriteUnitHeight);
						}
						else if (spriteUnitSizeHeight != Editor._captureFrame_SpriteUnitHeight)
						{
							//Height가 바뀌었다. => Width를 맞추자
							spriteUnitSizeWidth = apEditorUtil.GetAspectRatio_Width(spriteUnitSizeHeight, Editor._captureFrame_SpriteUnitWidth, Editor._captureFrame_SpriteUnitHeight);
							//>> Dst도 바꾸자 => Height
							srcSizeHeight = apEditorUtil.GetAspectRatio_Height(srcSizeWidth, Editor._captureFrame_SpriteUnitWidth, Editor._captureFrame_SpriteUnitHeight);
						}
					}
				}

				if (posX != Editor._captureFrame_PosX
					|| posY != Editor._captureFrame_PosY
					|| srcSizeWidth != Editor._captureFrame_SrcWidth
					|| srcSizeHeight != Editor._captureFrame_SrcHeight
					|| dstSizeWidth != Editor._captureFrame_DstWidth
					|| dstSizeHeight != Editor._captureFrame_DstHeight
					|| spriteUnitSizeWidth != Editor._captureFrame_SpriteUnitWidth
					|| spriteUnitSizeHeight != Editor._captureFrame_SpriteUnitHeight
					|| spritePackImageWidth != Editor._captureSpritePackImageWidth
					|| spritePackImageHeight != Editor._captureSpritePackImageHeight
					|| spriteTrimSize != Editor._captureSpriteTrimSize
					|| spriteMargin != Editor._captureFrame_SpriteMargin
					|| isPhysicsEnabled != Editor._captureFrame_IsPhysics
					)
				{
					Editor._captureFrame_PosX = posX;
					Editor._captureFrame_PosY = posY;

					if (srcSizeWidth < 10) { srcSizeWidth = 10; }
					if (srcSizeHeight < 10) { srcSizeHeight = 10; }
					Editor._captureFrame_SrcWidth = srcSizeWidth;
					Editor._captureFrame_SrcHeight = srcSizeHeight;

					if (dstSizeWidth < 10) { dstSizeWidth = 10; }
					if (dstSizeHeight < 10) { dstSizeHeight = 10; }
					Editor._captureFrame_DstWidth = dstSizeWidth;
					Editor._captureFrame_DstHeight = dstSizeHeight;

					if (spriteUnitSizeWidth < 10) { spriteUnitSizeWidth = 10; }
					if (spriteUnitSizeHeight < 10) { spriteUnitSizeHeight = 10; }
					Editor._captureFrame_SpriteUnitWidth = spriteUnitSizeWidth;
					Editor._captureFrame_SpriteUnitHeight = spriteUnitSizeHeight;

					Editor._captureSpritePackImageWidth = spritePackImageWidth;
					Editor._captureSpritePackImageHeight = spritePackImageHeight;
					Editor._captureSpriteTrimSize = spriteTrimSize;

					if (spriteMargin < 0) { spriteMargin = 0; }
					Editor._captureFrame_SpriteMargin = spriteMargin;

					Editor._captureFrame_IsPhysics = isPhysicsEnabled;

					Editor.SaveEditorPref();
					apEditorUtil.ReleaseGUIFocus();
				}

				if (Mathf.Abs(prevCaptureColor.r - Editor._captureFrame_Color.r) > 0.01f
					|| Mathf.Abs(prevCaptureColor.g - Editor._captureFrame_Color.g) > 0.01f
					|| Mathf.Abs(prevCaptureColor.b - Editor._captureFrame_Color.b) > 0.01f
					|| Mathf.Abs(prevCaptureColor.a - Editor._captureFrame_Color.a) > 0.01f)
				{
					_editor.SaveEditorPref();
					//색상은 GUIFocus를 null로 만들면 안되기에..
				}

				GUILayout.Space(10);
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(10);

				if (_guiContent_Overall_MakeThumbnail == null)
				{
					_guiContent_Overall_MakeThumbnail = new apGUIContentWrapper();
					_guiContent_Overall_MakeThumbnail.ClearText(false);
					_guiContent_Overall_MakeThumbnail.AppendSpaceText(1, false);
					_guiContent_Overall_MakeThumbnail.AppendText(_editor.GetText(TEXT.DLG_MakeThumbnail), true);
					_guiContent_Overall_MakeThumbnail.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Capture_ExportThumb));
				}

				if (_guiContent_Overall_TakeAScreenshot == null)
				{
					_guiContent_Overall_TakeAScreenshot = new apGUIContentWrapper();
					_guiContent_Overall_TakeAScreenshot.ClearText(false);
					_guiContent_Overall_TakeAScreenshot.AppendSpaceText(1, false);
					_guiContent_Overall_TakeAScreenshot.AppendText(_editor.GetText(TEXT.DLG_TakeAScreenshot), true);
					_guiContent_Overall_TakeAScreenshot.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Capture_ExportScreenshot));
				}

				switch (Editor._rootUnitCaptureMode)
				{
					case apEditor.ROOTUNIT_CAPTURE_MODE.Thumbnail:
						{
							//1. 썸네일 캡쳐
							EditorGUILayout.LabelField(_editor.GetText(TEXT.DLG_ThumbnailCapture));//"Thumbnail Capture"
							GUILayout.Space(5);
							string prev_ImageFilePath = _editor._portrait._imageFilePath_Thumbnail;

							//Preview 이미지
							GUILayout.Box(_editor._portrait._thumbnailImage, GUI.skin.label, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(width / 2));

							//File Path
							GUILayout.Space(5);
							EditorGUILayout.LabelField(_editor.GetText(TEXT.DLG_FilePath));//"File Path"
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
							GUILayout.Space(5);
							_editor._portrait._imageFilePath_Thumbnail = EditorGUILayout.TextField(_editor._portrait._imageFilePath_Thumbnail, apGUILOFactory.I.Width(width - (68)));
							if (GUILayout.Button(_editor.GetText(TEXT.DLG_Change), apGUILOFactory.I.Width(60)))//"Change"
							{
								string fileName = EditorUtility.SaveFilePanelInProject("Thumbnail File Path", _editor._portrait.name + "_Thumb.png", "png", "Please Enter a file name to save Thumbnail to");
								if (!string.IsNullOrEmpty(fileName))
								{
									_editor._portrait._imageFilePath_Thumbnail = fileName;
									apEditorUtil.ReleaseGUIFocus();
								}
							}
							EditorGUILayout.EndHorizontal();

							if (!_editor._portrait._imageFilePath_Thumbnail.Equals(prev_ImageFilePath))
							{
								//경로가 바뀌었다. -> 저장
								apEditorUtil.SetEditorDirty();

							}

							//썸네일 만들기 버튼
							if (GUILayout.Button(_guiContent_Overall_MakeThumbnail.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
							{
								if (string.IsNullOrEmpty(_editor._portrait._imageFilePath_Thumbnail))
								{
									//EditorUtility.DisplayDialog("Thumbnail Creating Failed", "File Name is Empty", "Close");
									EditorUtility.DisplayDialog(_editor.GetText(TEXT.ThumbCreateFailed_Title),
																	_editor.GetText(TEXT.ThumbCreateFailed_Body_NoFile),
																	_editor.GetText(TEXT.Close)
																	);
								}
								else
								{
									//RequestExport(EXPORT_TYPE.Thumbnail);//<<이전 코드
									StartMakeThumbnail();//<<새로운 코드

								}
							}
						}
						break;

					case apEditor.ROOTUNIT_CAPTURE_MODE.ScreenShot:
						{
							//2. 스크린샷 캡쳐
							EditorGUILayout.LabelField(_editor.GetText(TEXT.DLG_ScreenshotCapture));//"Screenshot Capture"
							GUILayout.Space(5);

							if (GUILayout.Button(_guiContent_Overall_TakeAScreenshot.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
							{
								if (CheckComputeShaderSupportedForScreenCapture())//추가 : 캡쳐 처리 가능한지 확인
								{
									StartTakeScreenShot();
								}
							}
						}
						break;

					case apEditor.ROOTUNIT_CAPTURE_MODE.GIFAnimation:
						{
							//3. GIF 애니메이션 캡쳐
							EditorGUILayout.LabelField(_editor.GetText(TEXT.DLG_GIFAnimation));//"GIF Animation"
							GUILayout.Space(5);

							List<apAnimClip> subAnimClips = RootUnitAnimClipList;

							string animName = _editor.GetText(TEXT.DLG_NotAnimation);
							Color animBGColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);
							if (_captureSelectedAnimClip != null)
							{
								animName = _captureSelectedAnimClip._name;
								animBGColor = new Color(0.7f, 1.0f, 0.7f, 1.0f);
							}

							Color prevGUIColor = GUI.backgroundColor;
							//GUIStyle guiStyleBox = new GUIStyle(GUI.skin.box);
							//guiStyleBox.alignment = TextAnchor.MiddleCenter;
							//guiStyleBox.normal.textColor = apEditorUtil.BoxTextColor;

							GUI.backgroundColor = animBGColor;

							GUILayout.Box(animName, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

							GUI.backgroundColor = prevGUIColor;

							GUILayout.Space(5);


							bool isDrawProgressBar = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_GIF_ProgressBar);//"Capture GIF ProgressBar"
							bool isDrawGIFAnimClips = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_GIF_Clips);//"Capture GIF Clips"
							try
							{
								if (_captureMode != CAPTURE_MODE.None)
								{
									if (isDrawProgressBar)
									{
										//캡쳐 중에는 다른 UI 제어 불가

										if (_captureMode == CAPTURE_MODE.Capturing_GIF_Animation
											|| _captureMode == CAPTURE_MODE.Capturing_MP4_Animation)
										{
											EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SpriteGIFWait));//"Please wait until finished. - TODO"

											float barRatio = Editor.SeqExporter.ProcessRatio;
											string barLabel = (int)(Mathf.Clamp01(barRatio) * 100.0f) + " %";

											string strTitleText = (_captureMode == CAPTURE_MODE.Capturing_GIF_Animation) ? "Exporting to GIF" : "Exporting to MP4";

											bool isCancel = EditorUtility.DisplayCancelableProgressBar(strTitleText, "Processing... " + barLabel, barRatio);
											_captureGIF_IsProgressDialog = true;
											if (isCancel)
											{
												//취소 버튼을 눌렀다.
												Editor.SeqExporter.RequestStop();
												apEditorUtil.ReleaseGUIFocus();
											}
										}
									}

								}
								else
								{
									if (_captureGIF_IsProgressDialog)
									{
										EditorUtility.ClearProgressBar();
										_captureGIF_IsProgressDialog = false;
									}

									if (isDrawGIFAnimClips)
									{
										GUILayout.Space(10);
										apEditorUtil.GUI_DelimeterBoxH(width);
										GUILayout.Space(10);

										EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.CaptureScreenPosZoom));//"Screen Position and Zoom"
										GUILayout.Space(5);

										//화면 위치
										int width_ScreenPos = ((width - (10 + 30)) / 2) - 20;
										GUILayout.Space(5);
										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
										GUILayout.Space(4);
										EditorGUILayout.LabelField(apStringFactory.I.X, apGUILOFactory.I.Width(15));
										Editor._captureSprite_ScreenPos.x = EditorGUILayout.DelayedFloatField(Editor._captureSprite_ScreenPos.x, apGUILOFactory.I.Width(width_ScreenPos));
										EditorGUILayout.LabelField(apStringFactory.I.Y, apGUILOFactory.I.Width(15));
										Editor._captureSprite_ScreenPos.y = EditorGUILayout.DelayedFloatField(Editor._captureSprite_ScreenPos.y, apGUILOFactory.I.Width(width_ScreenPos));
										//GUIStyle guiStyle_SetBtn = new GUIStyle(GUI.skin.button);
										//guiStyle_SetBtn.margin = GUI.skin.textField.margin;

										if (GUILayout.Button(apStringFactory.I.Set, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(18)))//"Set"
										{
											Editor._scroll_MainCenter = Editor._captureSprite_ScreenPos * 0.01f;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										EditorGUILayout.EndHorizontal();
										//Zoom

										Rect lastRect = GUILayoutUtility.GetLastRect();
										lastRect.x += 5;
										lastRect.y += 25;
										lastRect.width = width - (30 + 10 + 60 + 10);
										lastRect.height = 20;

										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
										GUILayout.Space(6);
										GUILayout.Space(width - (30 + 10 + 60));
										//Editor._captureSprite_ScreenZoom = EditorGUILayout.IntSlider(Editor._captureSprite_ScreenZoom, 0, Editor._zoomListX100.Length - 1, GUILayout.Width(width - (30 + 10 + 40)));
										float fScreenZoom = GUI.HorizontalSlider(lastRect, Editor._captureSprite_ScreenZoom, 0, Editor._zoomListX100.Length - 1);
										Editor._captureSprite_ScreenZoom = Mathf.Clamp((int)fScreenZoom, 0, Editor._zoomListX100.Length - 1);

										EditorGUILayout.LabelField(Editor._zoomListX100_Label[Editor._captureSprite_ScreenZoom], apGUILOFactory.I.Width(60));
										if (GUILayout.Button(apStringFactory.I.Set, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(18)))//"Set"
										{
											Editor._iZoomX100 = Editor._captureSprite_ScreenZoom;
											if (Editor._iZoomX100 < 0)
											{
												Editor._iZoomX100 = 0;
											}
											else if (Editor._iZoomX100 >= Editor._zoomListX100.Length)
											{
												Editor._iZoomX100 = Editor._zoomListX100.Length - 1;
											}
											Editor._captureSprite_ScreenZoom = Editor._iZoomX100;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										EditorGUILayout.EndHorizontal();

										//"Focus To Center"
										if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.CaptureMoveToCenter), Editor.GetUIWord(UIWORD.CaptureMoveToCenter), false, true, width, 20))
										{
											Editor._scroll_MainCenter = Vector2.zero;
											Editor._captureSprite_ScreenPos = Vector2.zero;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}
										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(4);

										if (_strWrapper_64 == null)
										{
											_strWrapper_64 = new apStringWrapper(64);
										}

										_strWrapper_64.Clear();
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.CaptureZoom), false);
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(apStringFactory.I.Minus, true);

										//"Zoom -"
										if (apEditorUtil.ToggledButton_2Side(_strWrapper_64.ToString(), _strWrapper_64.ToString(), false, true, width / 2 - 2, 20))
										{
											Editor._iZoomX100--;
											if (Editor._iZoomX100 < 0) { Editor._iZoomX100 = 0; }
											Editor._captureSprite_ScreenZoom = Editor._iZoomX100;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										_strWrapper_64.Clear();
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.CaptureZoom), false);
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(apStringFactory.I.Plus, true);

										//"Zoom +"
										if (apEditorUtil.ToggledButton_2Side(_strWrapper_64.ToString(), _strWrapper_64.ToString(), false, true, width / 2 - 2, 20))
										{
											Editor._iZoomX100++;
											if (Editor._iZoomX100 >= Editor._zoomListX100.Length) { Editor._iZoomX100 = Editor._zoomListX100.Length - 1; }
											Editor._captureSprite_ScreenZoom = Editor._iZoomX100;
											Editor.SaveEditorPref();
										}
										EditorGUILayout.EndHorizontal();


										GUILayout.Space(10);
										apEditorUtil.GUI_DelimeterBoxH(width);
										GUILayout.Space(10);

										//Quality의 Min : 0, Max : 246이다.

										//변경 11.4 : GIF 저장 퀄리티를 4개의 타입으로 나누고, Maximum 추가
										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(5);

										if (_strWrapper_64 == null)
										{
											_strWrapper_64 = new apStringWrapper(64);
										}

										_strWrapper_64.Clear();
										_strWrapper_64.Append(apStringFactory.I.GIF, false);
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Quality), true);

										EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(100));
										apEditor.CAPTURE_GIF_QUALITY gifQulity = (apEditor.CAPTURE_GIF_QUALITY)EditorGUILayout.EnumPopup(_editor._captureFrame_GIFQuality, apGUILOFactory.I.Width(width - (5 + 100 + 5)));

										if (gifQulity != _editor._captureFrame_GIFQuality)
										{
											_editor._captureFrame_GIFQuality = gifQulity;
											_editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										EditorGUILayout.EndHorizontal();

										GUILayout.Space(5);
										EditorGUILayout.LabelField(_editor.GetText(TEXT.DLG_LoopCount), apGUILOFactory.I.Width(width));//"Loop Count"
										int loopCount = EditorGUILayout.DelayedIntField(_editor._captureFrame_GIFSampleLoopCount, apGUILOFactory.I.Width(width));
										if (loopCount != _editor._captureFrame_GIFSampleLoopCount)
										{
											loopCount = Mathf.Clamp(loopCount, 1, 10);
											_editor._captureFrame_GIFSampleLoopCount = loopCount;
											_editor.SaveEditorPref();
										}

										GUILayout.Space(5);

										_strWrapper_64.Clear();
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(_editor.GetText(TEXT.DLG_TakeAGIFAnimation), true);

										//string strTakeAGIFAnimation = " " + _editor.GetText(TEXT.DLG_TakeAGIFAnimation);

										//"Take a GIF Animation", "Take a GIF Animation"
										if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Capture_ExportGIF), _strWrapper_64.ToString(), _strWrapper_64.ToString(), false, (_captureSelectedAnimClip != null), width, 30))
										{
											if (CheckComputeShaderSupportedForScreenCapture())//추가 : 캡쳐 처리 가능한지 확인
											{
												StartGIFAnimation();
											}
										}


#if UNITY_2017_4_OR_NEWER
										_strWrapper_64.Clear();
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(_editor.GetText(TEXT.DLG_ExportMP4), true);

										//string strTakeAMP4Animation = " " + _editor.GetText(TEXT.DLG_ExportMP4);

										if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Capture_ExportMP4), _strWrapper_64.ToString(), _strWrapper_64.ToString(), false, (_captureSelectedAnimClip != null), width, 30))
										{
											if (CheckComputeShaderSupportedForScreenCapture())//추가 : 캡쳐 처리 가능한지 확인
											{
												StartMP4Animation();
											}
										}
#endif

										GUILayout.Space(10);


										_strWrapper_64.Clear();
										_strWrapper_64.AppendSpace(2, false);
										_strWrapper_64.Append(_editor.GetText(TEXT.DLG_AnimationClips), true);

										//"Animation Clips"
										GUILayout.Button(_strWrapper_64.ToString(), apGUIStyleWrapper.I.None_LabelColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));//투명 버튼

										//애니메이션 클립 리스트를 만들어야 한다.
										if (subAnimClips.Count > 0)
										{

											if (_guiContent_Overall_AnimItem == null)
											{
												_guiContent_Overall_AnimItem = new apGUIContentWrapper();
												_guiContent_Overall_AnimItem.ClearText(true);
												_guiContent_Overall_AnimItem.SetImage(_editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation));
											}

											GUIStyle curGUIStyle = null;//최적화 코드

											apAnimClip nextSelectedAnimClip = null;
											for (int i = 0; i < subAnimClips.Count; i++)
											{
												apAnimClip animClip = subAnimClips[i];

												if (animClip == _captureSelectedAnimClip)
												{
													lastRect = GUILayoutUtility.GetLastRect();
													prevCaptureColor = GUI.backgroundColor;

													if (EditorGUIUtility.isProSkin)
													{
														GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
													}
													else
													{
														GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
													}

													GUI.Box(new Rect(lastRect.x, lastRect.y + 20, width + 20, 20), apStringFactory.I.None);
													GUI.backgroundColor = prevGUIColor;

													curGUIStyle = apGUIStyleWrapper.I.None_White2Cyan;
												}
												else
												{
													curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;
												}



												EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width - 50));
												GUILayout.Space(15);

												//이전
												//if (GUILayout.Button(new GUIContent(" " + animClip._name, iconImage), curGUIStyle, GUILayout.Width(width - 35), GUILayout.Height(20)))

												//변경
												_guiContent_Overall_AnimItem.ClearText(false);
												_guiContent_Overall_AnimItem.AppendSpaceText(1, false);
												_guiContent_Overall_AnimItem.AppendText(animClip._name, true);

												if (GUILayout.Button(_guiContent_Overall_AnimItem.Content, curGUIStyle, apGUILOFactory.I.Width(width - 35), apGUILOFactory.I.Height(20)))
												{
													nextSelectedAnimClip = animClip;
												}

												EditorGUILayout.EndHorizontal();
											}

											if (nextSelectedAnimClip != null)
											{
												for (int i = 0; i < _editor._portrait._animClips.Count; i++)
												{
													_editor._portrait._animClips[i]._isSelectedInEditor = false;
												}

												nextSelectedAnimClip.LinkEditor(_editor._portrait);
												nextSelectedAnimClip.RefreshTimelines(null);
												nextSelectedAnimClip.SetFrame_Editor(nextSelectedAnimClip.StartFrame);
												nextSelectedAnimClip.Pause_Editor();
												nextSelectedAnimClip._isSelectedInEditor = true;

												_captureSelectedAnimClip = nextSelectedAnimClip;

												_editor._portrait._animPlayManager.SetAnimClip_Editor(_captureSelectedAnimClip);

												Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
											}
										}
									}
								}
							}
							catch (Exception)
							{
								//Debug.LogError("GUI Exception : " + ex);
								//Debug.Log("Capture Mode : " + _captureMode);
								//Debug.Log("isDrawProgressBar : " + isDrawProgressBar);
								//Debug.Log("isDrawGIFAnimClips : " + isDrawGIFAnimClips);
								//Debug.Log("Event : " + Event.current.type);
							}

							Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_GIF_ProgressBar, _captureMode != CAPTURE_MODE.None);//"Capture GIF ProgressBar"
							Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_GIF_Clips, _captureMode == CAPTURE_MODE.None);//"Capture GIF Clips"
						}
						break;

					case apEditor.ROOTUNIT_CAPTURE_MODE.SpriteSheet:
						{
							//4. 스프라이트 시트 캡쳐
							EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SpriteSheet));//"Sprite Sheet"
							GUILayout.Space(5);

							bool isDrawProgressBar = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_Spritesheet_ProgressBar);//"Capture Spritesheet ProgressBar"
							bool isDrawSpritesheetSettings = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_Spritesheet_Settings);//"Capture Spritesheet Settings"

							try
							{
								if (_captureMode != CAPTURE_MODE.None)
								{
									if (isDrawProgressBar)
									{
										//캡쳐 중에는 다른 UI 제어 불가

										if (_captureMode == CAPTURE_MODE.Capturing_Spritesheet)
										{
											EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SpriteGIFWait));//"Please wait until finished. - TODO"
																											   //Rect lastRect = GUILayoutUtility.GetLastRect();

											//Rect barRect = new Rect(lastRect.x + 10, lastRect.y + 30, width - 20, 20);
											//float barRatio = (float)(_captureGIF_CurAnimProcess) / (float)(_captureGIF_TotalAnimProcess);

											float barRatio = Editor.SeqExporter.ProcessRatio;
											string barLabel = (int)(Mathf.Clamp01(barRatio) * 100.0f) + " %";

											//EditorGUI.ProgressBar(barRect, barRatio, barLabel);
											bool isCancel = EditorUtility.DisplayCancelableProgressBar("Exporting to Sprite Sheet", "Processing... " + barLabel, barRatio);
											_captureGIF_IsProgressDialog = true;
											if (isCancel)
											{
												Editor.SeqExporter.RequestStop();
												apEditorUtil.ReleaseGUIFocus();
											}
										}
									}
								}
								else
								{
									if (_captureGIF_IsProgressDialog)
									{
										EditorUtility.ClearProgressBar();
										_captureGIF_IsProgressDialog = false;
									}

									if (isDrawSpritesheetSettings)
									{
										List<apAnimClip> subAnimClips = RootUnitAnimClipList;

										//그 전에 AnimClip 갱신부터
										if (!_captureSprite_IsAnimClipInit)
										{
											_captureSprite_AnimClips.Clear();
											_captureSprite_AnimClipFlags.Clear();
											for (int i = 0; i < subAnimClips.Count; i++)
											{
												_captureSprite_AnimClips.Add(subAnimClips[i]);
												_captureSprite_AnimClipFlags.Add(false);
											}

											_captureSprite_IsAnimClipInit = true;
										}

										Color prevGUIColor = GUI.backgroundColor;
										//GUIStyle guiStyleBox = new GUIStyle(GUI.skin.box);
										//guiStyleBox.alignment = TextAnchor.MiddleCenter;
										//guiStyleBox.normal.textColor = apEditorUtil.BoxTextColor;

										GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f, 1.0f);

										if (_strWrapper_128 == null)
										{
											_strWrapper_128 = new apStringWrapper(128);
										}

										_strWrapper_128.Clear();
										_strWrapper_128.Append(Editor.GetUIWord(UIWORD.ExpectedNumSprites), false);
										_strWrapper_128.Append("\n", false);

										//string strNumOfSprites = Editor.GetUIWord(UIWORD.ExpectedNumSprites) + "\n";//"Expected number of sprites - TODO\n";

										int spriteTotalSize_X = 0;
										int spriteTotalSize_Y = 0;
										switch (Editor._captureSpritePackImageWidth)
										{
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s256: spriteTotalSize_X = 256; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s512: spriteTotalSize_X = 512; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s1024: spriteTotalSize_X = 1024; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s2048: spriteTotalSize_X = 2048; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s4096: spriteTotalSize_X = 4096; break;
											default: spriteTotalSize_X = 256; break;
										}

										switch (Editor._captureSpritePackImageHeight)
										{
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s256: spriteTotalSize_Y = 256; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s512: spriteTotalSize_Y = 512; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s1024: spriteTotalSize_Y = 1024; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s2048: spriteTotalSize_Y = 2048; break;
											case apEditor.CAPTURE_SPRITE_PACK_IMAGE_SIZE.s4096: spriteTotalSize_Y = 4096; break;
											default: spriteTotalSize_Y = 256; break;
										}
										//X축 개수
										int numXOfSprite = -1;
										if (Editor._captureFrame_SpriteUnitWidth > 0 || Editor._captureFrame_SpriteUnitWidth < spriteTotalSize_X)
										{
											numXOfSprite = spriteTotalSize_X / Editor._captureFrame_SpriteUnitWidth;
										}

										//Y축 개수
										int numYOfSprite = -1;
										if (Editor._captureFrame_SpriteUnitHeight > 0 || Editor._captureFrame_SpriteUnitHeight < spriteTotalSize_Y)
										{
											numYOfSprite = spriteTotalSize_Y / Editor._captureFrame_SpriteUnitHeight;
										}
										if (numXOfSprite <= 0 || numYOfSprite <= 0)
										{
											//strNumOfSprites += Editor.GetUIWord(UIWORD.InvalidSpriteSizeSettings);//"Invalid size settings";

											_strWrapper_128.Append(Editor.GetUIWord(UIWORD.InvalidSpriteSizeSettings), true);

											GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);
										}
										else
										{
											//strNumOfSprites += numXOfSprite + " X " + numYOfSprite;

											_strWrapper_128.Append(numXOfSprite, false);
											_strWrapper_128.AppendSpace(1, false);
											_strWrapper_128.Append(apStringFactory.I.X, false);
											_strWrapper_128.AppendSpace(1, false);
											_strWrapper_128.Append(numYOfSprite, true);
										}
										GUILayout.Box(_strWrapper_128.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40));

										GUI.backgroundColor = prevGUIColor;

										GUILayout.Space(5);



										//Export Format
										int width_ToggleLabel = width - (10 + 30);
										EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ExportMetaFile));//"Export Meta File - TODO"
										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(5);
										EditorGUILayout.LabelField(apStringFactory.I.XML, apGUILOFactory.I.Width(width_ToggleLabel));//"XML"
										bool isMetaXML = EditorGUILayout.Toggle(Editor._captureSpriteMeta_XML, apGUILOFactory.I.Width(30));
										EditorGUILayout.EndHorizontal();

										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(5);
										EditorGUILayout.LabelField(apStringFactory.I.JSON, apGUILOFactory.I.Width(width_ToggleLabel));//"JSON"
										bool isMetaJSON = EditorGUILayout.Toggle(Editor._captureSpriteMeta_JSON, apGUILOFactory.I.Width(30));
										EditorGUILayout.EndHorizontal();

										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(5);
										EditorGUILayout.LabelField(apStringFactory.I.TXT, apGUILOFactory.I.Width(width_ToggleLabel));//"TXT"
										bool isMetaTXT = EditorGUILayout.Toggle(Editor._captureSpriteMeta_TXT, apGUILOFactory.I.Width(30));
										EditorGUILayout.EndHorizontal();

										if (isMetaXML != Editor._captureSpriteMeta_XML
											|| isMetaJSON != Editor._captureSpriteMeta_JSON
											|| isMetaTXT != Editor._captureSpriteMeta_TXT
											)
										{
											Editor._captureSpriteMeta_XML = isMetaXML;
											Editor._captureSpriteMeta_JSON = isMetaJSON;
											Editor._captureSpriteMeta_TXT = isMetaTXT;

											_editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										GUILayout.Space(5);

										EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.CaptureScreenPosZoom));//"Screen Position and Zoom"
										GUILayout.Space(5);

										//화면 위치
										int width_ScreenPos = ((width - (10 + 30)) / 2) - 20;
										GUILayout.Space(5);
										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
										GUILayout.Space(4);
										EditorGUILayout.LabelField(apStringFactory.I.X, apGUILOFactory.I.Width(15));
										Editor._captureSprite_ScreenPos.x = EditorGUILayout.DelayedFloatField(Editor._captureSprite_ScreenPos.x, apGUILOFactory.I.Width(width_ScreenPos));
										EditorGUILayout.LabelField(apStringFactory.I.Y, apGUILOFactory.I.Width(15));
										Editor._captureSprite_ScreenPos.y = EditorGUILayout.DelayedFloatField(Editor._captureSprite_ScreenPos.y, apGUILOFactory.I.Width(width_ScreenPos));

										//GUIStyle guiStyle_SetBtn = new GUIStyle(GUI.skin.button);
										//guiStyle_SetBtn.margin = GUI.skin.textField.margin;

										if (GUILayout.Button(apStringFactory.I.Set, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(18)))
										{
											Editor._scroll_MainCenter = Editor._captureSprite_ScreenPos * 0.01f;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										EditorGUILayout.EndHorizontal();
										//Zoom

										Rect lastRect = GUILayoutUtility.GetLastRect();
										lastRect.x += 5;
										lastRect.y += 25;
										lastRect.width = width - (30 + 10 + 60 + 10);
										lastRect.height = 20;

										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
										GUILayout.Space(6);
										GUILayout.Space(width - (30 + 10 + 60));
										//Editor._captureSprite_ScreenZoom = EditorGUILayout.IntSlider(Editor._captureSprite_ScreenZoom, 0, Editor._zoomListX100.Length - 1, GUILayout.Width(width - (30 + 10 + 40)));
										float fScreenZoom = GUI.HorizontalSlider(lastRect, Editor._captureSprite_ScreenZoom, 0, Editor._zoomListX100.Length - 1);
										Editor._captureSprite_ScreenZoom = Mathf.Clamp((int)fScreenZoom, 0, Editor._zoomListX100.Length - 1);

										EditorGUILayout.LabelField(Editor._zoomListX100_Label[Editor._captureSprite_ScreenZoom], apGUILOFactory.I.Width(60));
										if (GUILayout.Button(apStringFactory.I.Set, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(18)))
										{
											Editor._iZoomX100 = Editor._captureSprite_ScreenZoom;
											if (Editor._iZoomX100 < 0)
											{
												Editor._iZoomX100 = 0;
											}
											else if (Editor._iZoomX100 >= Editor._zoomListX100.Length)
											{
												Editor._iZoomX100 = Editor._zoomListX100.Length - 1;
											}
											Editor._captureSprite_ScreenZoom = Editor._iZoomX100;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										EditorGUILayout.EndHorizontal();

										//"Focus To Center"
										if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.CaptureMoveToCenter), Editor.GetUIWord(UIWORD.CaptureMoveToCenter), false, true, width, 20))
										{
											Editor._scroll_MainCenter = Vector2.zero;
											Editor._captureSprite_ScreenPos = Vector2.zero;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}
										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(4);


										if (_strWrapper_64 == null)
										{
											_strWrapper_64 = new apStringWrapper(64);
										}

										_strWrapper_64.Clear();
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.CaptureZoom), false);
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(apStringFactory.I.Minus, true);

										//"Zoom -"
										if (apEditorUtil.ToggledButton_2Side(_strWrapper_64.ToString(), _strWrapper_64.ToString(), false, true, width / 2 - 2, 20))
										{
											Editor._iZoomX100--;
											if (Editor._iZoomX100 < 0) { Editor._iZoomX100 = 0; }
											Editor._captureSprite_ScreenZoom = Editor._iZoomX100;
											Editor.SaveEditorPref();
											apEditorUtil.ReleaseGUIFocus();
										}

										_strWrapper_64.Clear();
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.CaptureZoom), false);
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(apStringFactory.I.Plus, true);

										//"Zoom +"
										if (apEditorUtil.ToggledButton_2Side(_strWrapper_64.ToString(), _strWrapper_64.ToString(), false, true, width / 2 - 2, 20))
										{
											Editor._iZoomX100++;
											if (Editor._iZoomX100 >= Editor._zoomListX100.Length) { Editor._iZoomX100 = Editor._zoomListX100.Length - 1; }
											Editor._captureSprite_ScreenZoom = Editor._iZoomX100;
											Editor.SaveEditorPref();
										}
										EditorGUILayout.EndHorizontal();

										GUILayout.Space(10);
										apEditorUtil.GUI_DelimeterBoxH(width);
										GUILayout.Space(10);

										int nAnimClipToExport = 0;
										for (int i = 0; i < _captureSprite_AnimClipFlags.Count; i++)
										{
											if (_captureSprite_AnimClipFlags[i])
											{
												nAnimClipToExport++;
											}
										}

										//string strTakeSpriteSheets = " " + Editor.GetUIWord(UIWORD.CaptureExportSpriteSheets);//"Export Sprite Sheets";
										//string strTakeSequenceFiles = " " + Editor.GetUIWord(UIWORD.CaptureExportSeqFiles);//"Export Sequence Files";

										_strWrapper_64.Clear();
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.CaptureExportSpriteSheets), true);

										if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Capture_ExportSprite), _strWrapper_64.ToString(), _strWrapper_64.ToString(), false, (numXOfSprite > 0 && numYOfSprite > 0 && nAnimClipToExport > 0), width, 30))
										{
											if (CheckComputeShaderSupportedForScreenCapture())//추가 : 캡쳐 처리 가능한지 확인
											{
												StartSpriteSheet(false);
											}
										}

										_strWrapper_64.Clear();
										_strWrapper_64.AppendSpace(1, false);
										_strWrapper_64.Append(Editor.GetUIWord(UIWORD.CaptureExportSeqFiles), true);

										if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Capture_ExportSequence), _strWrapper_64.ToString(), _strWrapper_64.ToString(), false, (nAnimClipToExport > 0), width, 25))
										{
											if (CheckComputeShaderSupportedForScreenCapture())//추가 : 캡쳐 처리 가능한지 확인
											{
												StartSpriteSheet(true);
											}
										}
										GUILayout.Space(5);

										GUILayout.Space(10);
										apEditorUtil.GUI_DelimeterBoxH(width);
										GUILayout.Space(10);

										EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
										GUILayout.Space(4);
										//"Select All"
										if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.CaptureSelectAll), Editor.GetUIWord(UIWORD.CaptureSelectAll), false, true, width / 2 - 2, 20))
										{
											for (int i = 0; i < _captureSprite_AnimClipFlags.Count; i++)
											{
												_captureSprite_AnimClipFlags[i] = true;
											}
										}
										//"Deselect All"
										if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.CaptureDeselectAll), Editor.GetUIWord(UIWORD.CaptureDeselectAll), false, true, width / 2 - 2, 20))
										{
											for (int i = 0; i < _captureSprite_AnimClipFlags.Count; i++)
											{
												_captureSprite_AnimClipFlags[i] = false;
											}
										}
										EditorGUILayout.EndHorizontal();

										GUILayout.Space(10);

										//애니메이션 클립별로 "Export"할 것인지 지정
										//GUIStyle guiStyle_None = new GUIStyle(GUIStyle.none);
										//guiStyle_None.normal.textColor = GUI.skin.label.normal.textColor;


										//"Animation Clips"
										_strWrapper_64.Clear();
										_strWrapper_64.AppendSpace(2, false);
										_strWrapper_64.Append(_editor.GetText(TEXT.DLG_AnimationClips), true);

										GUILayout.Button(_strWrapper_64.ToString(), apGUIStyleWrapper.I.None_LabelColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));//투명 버튼



										//애니메이션 클립 리스트를 만들어야 한다.
										if (subAnimClips.Count > 0)
										{
											if (_guiContent_Overall_AnimItem == null)
											{
												_guiContent_Overall_AnimItem = new apGUIContentWrapper();
												_guiContent_Overall_AnimItem.ClearText(true);
												_guiContent_Overall_AnimItem.SetImage(_editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation));
											}

											//Texture2D iconImage = _editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation);

											for (int i = 0; i < subAnimClips.Count; i++)
											{
												//GUIStyle curGUIStyle = guiStyle_None;

												apAnimClip animClip = subAnimClips[i];

												//if (animClip == _captureSelectedAnimClip)
												//{
												//	Rect lastRect = GUILayoutUtility.GetLastRect();
												//	prevCaptureColor = GUI.backgroundColor;

												//	if (EditorGUIUtility.isProSkin)
												//	{
												//		GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
												//	}
												//	else
												//	{
												//		GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
												//	}

												//	GUI.Box(new Rect(lastRect.x, lastRect.y + 20, width + 20, 20), "");
												//	GUI.backgroundColor = prevGUIColor;

												//	curGUIStyle = guiStyle_Selected;
												//}

												EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width - 50));
												GUILayout.Space(15);

												//이전
												//if (GUILayout.Button(new GUIContent(" " + animClip._name, iconImage), curGUIStyle, GUILayout.Width((width - 35) - 35), GUILayout.Height(20)))

												//변경
												_guiContent_Overall_AnimItem.ClearText(false);
												_guiContent_Overall_AnimItem.AppendSpaceText(1, false);
												_guiContent_Overall_AnimItem.AppendText(animClip._name, true);

												if (GUILayout.Button(_guiContent_Overall_AnimItem.Content, apGUIStyleWrapper.I.None_LabelColor, apGUILOFactory.I.Width((width - 35) - 35), apGUILOFactory.I.Height(20)))
												{
													//nextSelectedAnimClip = animClip;
												}
												_captureSprite_AnimClipFlags[i] = EditorGUILayout.Toggle(_captureSprite_AnimClipFlags[i], apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(20));

												EditorGUILayout.EndHorizontal();
											}


										}
									}
								}
							}
							catch (Exception ex)
							{
								Debug.LogError("GUI Exception : " + ex);

							}

							Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_Spritesheet_ProgressBar, _captureMode != CAPTURE_MODE.None);//"Capture Spritesheet ProgressBar"
							Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Capture_Spritesheet_Settings, _captureMode == CAPTURE_MODE.None);//"Capture Spritesheet Settings"
						}
						break;
				}

				//이것도 Thumbnail이 아닌 경우
				//Screenshot + GIF Animation : _captureFrame_DstWidth x _captureFrame_DstHeight을 사용한다.
				//Sprite Sheet : 단위 유닛 크기 / 전체 이미지 파일 크기 (_captureFrame_DstWidth)의 두가지를 이용한다.

				//1) Setting
				//Position + Capture Size + File Size / BG Color / Aspect Ratio Fixed

				//<Export 방식은 탭으로 구분한다>
				//2) Thumbnail
				// Size (Width) Preview / Path + Change / Make Thumbnail
				//3) Screen Shot
				// Size (Width / Height x Src + Dst) / Take a Screenshot
				//4) GIF Animation
				// Size (Width / Height x Src + Dst) Animation Clip Name / Quality / Loop Count / Animation Clips / Take a GIF Animation + ProgressBar
				//5) Sprite
				//- Size (개별 캡쳐 크기 / 전체 이미지 크기 / 
				//- 출력 방식 : 스프라이트 시트 Only / Sprite + XML / Sprite + JSON
				//- 
			}
		}


		private void StartMakeThumbnail()
		{
			_captureMode = CAPTURE_MODE.Capturing_Thumbnail;

			//썸네일 크기
			int thumbnailWidth = 256;
			int thumbnailHeight = 128;

			float preferAspectRatio = (float)thumbnailWidth / (float)thumbnailHeight;

			float srcAspectRatio = (float)_editor._captureFrame_SrcWidth / (float)_editor._captureFrame_SrcHeight;
			//긴쪽으로 캡쳐 크기를 맞춘다.
			int srcThumbWidth = _editor._captureFrame_SrcWidth;
			int srcThumbHeight = _editor._captureFrame_SrcHeight;

			//AspectRatio = W / H
			if (srcAspectRatio < preferAspectRatio)
			{
				//가로가 더 길군요. 가로를 자릅시다.
				//H = W / AspectRatio;
				srcThumbHeight = (int)((srcThumbWidth / preferAspectRatio) + 0.5f);
			}
			else
			{
				//세로가 더 길군요. 세로를 자릅시다.
				//W = AspectRatio * H
				srcThumbWidth = (int)((srcThumbHeight * preferAspectRatio) + 0.5f);
			}

			//Request를 만든다.
			apScreenCaptureRequest newRequest = new apScreenCaptureRequest();
			_captureLoadKey = newRequest.MakeScreenShot(OnThumbnailCaptured,
														_editor,
														_editor.Select.RootUnit._childMeshGroup,
														(int)(_editor._captureFrame_PosX + apGL.WindowSizeHalf.x),
														(int)(_editor._captureFrame_PosY + apGL.WindowSizeHalf.y),
														srcThumbWidth, srcThumbHeight,
														thumbnailWidth, thumbnailHeight,
														Editor._scroll_MainCenter, Editor._iZoomX100,
														_editor._captureFrame_Color, 0, "");

			//에디터에 대신 렌더링해달라고 요청을 합시다.
			Editor.ScreenCaptureRequest(newRequest);
			Editor.SetRepaint();
		}


		// 2. PNG 스크린샷
		private void StartTakeScreenShot()
		{
			try
			{
				string defFileName = "ScreenShot_" + DateTime.Now.Year + "" + DateTime.Now.Month + "" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + ".png";
				string saveFilePath = EditorUtility.SaveFilePanel("Save Screenshot as PNG", _capturePrevFilePath_Directory, defFileName, "png");
				if (!string.IsNullOrEmpty(saveFilePath))
				{
					_captureMode = CAPTURE_MODE.Capturing_ScreenShot;

					//Request를 만든다.
					apScreenCaptureRequest newRequest = new apScreenCaptureRequest();
					_captureLoadKey = newRequest.MakeScreenShot(OnScreeenShotCaptured,
																_editor,
																_editor.Select.RootUnit._childMeshGroup,
																(int)(_editor._captureFrame_PosX + apGL.WindowSizeHalf.x),
																(int)(_editor._captureFrame_PosY + apGL.WindowSizeHalf.y),
																_editor._captureFrame_SrcWidth, _editor._captureFrame_SrcHeight,
																_editor._captureFrame_DstWidth, _editor._captureFrame_DstHeight,
																Editor._scroll_MainCenter, Editor._iZoomX100,
																_editor._captureFrame_Color, 0, saveFilePath);

					//에디터에 대신 렌더링해달라고 요청을 합시다.
					Editor.ScreenCaptureRequest(newRequest);
					Editor.SetRepaint();
				}
			}
			catch (Exception)
			{

			}
		}


		//3. GIF 애니메이션 만들기
		private void StartGIFAnimation()
		{
			if (_captureSelectedAnimClip == null || _editor.Select.RootUnit._childMeshGroup == null)
			{
				return;
			}

			string defFileName = "GIF_" + DateTime.Now.Year + "" + DateTime.Now.Month + "" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + ".gif";
			string saveFilePath = EditorUtility.SaveFilePanel("Save GIF Animation", _capturePrevFilePath_Directory, defFileName, "gif");
			if (!string.IsNullOrEmpty(saveFilePath))
			{
				//변경 11.4 : GIF 퀄리티 관련 팝업 및 Int -> Enum -> Int로 변경
				bool isAbleToSave = true;
				if (_editor._captureFrame_GIFQuality == apEditor.CAPTURE_GIF_QUALITY.Maximum)
				{
					//"GIF Quality Warning", "Saving with Maximum Quality takes a very long time to process. Are you sure you want to save with this type?", "Okay", "Cancel"
					isAbleToSave = EditorUtility.DisplayDialog(_editor.GetText(TEXT.DLG_ExportGIXMaxQualityWarining_Title),
																_editor.GetText(TEXT.DLG_ExportGIXMaxQualityWarining_Body),
																_editor.GetText(TEXT.Okay),
																_editor.GetText(TEXT.Cancel));
				}
				if (isAbleToSave)
				{
					int gifQuality_255 = 128;
					switch (_editor._captureFrame_GIFQuality)
					{
						case apEditor.CAPTURE_GIF_QUALITY.Low: gifQuality_255 = 128; break;
						case apEditor.CAPTURE_GIF_QUALITY.Medium: gifQuality_255 = 50; break;
						case apEditor.CAPTURE_GIF_QUALITY.High: gifQuality_255 = 10; break;
						case apEditor.CAPTURE_GIF_QUALITY.Maximum: gifQuality_255 = 1; break;
					}


					bool isResult = Editor.SeqExporter.StartGIFAnimation(_editor.Select.RootUnit,
						_captureSelectedAnimClip,
						_editor._captureFrame_GIFSampleLoopCount,
						//_editor._captureFrame_GIFSampleQuality,
						//(256 - gifQuality_255),
						gifQuality_255,
						saveFilePath,
						OnGIFMP4AnimationSaved);

					if (isResult)
					{
						System.IO.FileInfo fi = new System.IO.FileInfo(saveFilePath);
						_capturePrevFilePath_Directory = fi.Directory.FullName;
						_captureMode = CAPTURE_MODE.Capturing_GIF_Animation;
					}
				}
				#region [미사용 코드 : Sequence Exporter를 이용하자]
				////애니메이션 정보를 저장한다.
				//_captureGIF_IsLoopAnimation = _captureSelectedAnimClip.IsLoop;
				//_captureGIF_IsAnimFirstFrame = true;
				//_captureGIF_StartAnimFrame = _captureSelectedAnimClip.StartFrame;
				//_captureGIF_LastAnimFrame = _captureSelectedAnimClip.EndFrame;

				//_captureGIF_AnimLoopCount = _editor._captureFrame_GIFSampleLoopCount;
				//if (_captureGIF_AnimLoopCount < 1)
				//{
				//	_captureGIF_AnimLoopCount = 1;
				//}

				//_captureGIF_GifAnimQuality = _editor._captureFrame_GIFSampleQuality;

				//if (_captureGIF_IsLoopAnimation)
				//{
				//	_captureGIF_LastAnimFrame--;//루프인 경우 마지막 프레임은 제외
				//}

				//if (_captureGIF_LastAnimFrame < _captureGIF_StartAnimFrame)
				//{
				//	_captureGIF_LastAnimFrame = _captureGIF_StartAnimFrame;
				//}

				//_editor._portrait._animPlayManager.Stop_Editor();
				//_editor._portrait._animPlayManager.SetAnimClip_Editor(_captureSelectedAnimClip);

				//_captureGIF_CurAnimLoop = 0;
				//_captureGIF_CurAnimFrame = _captureGIF_StartAnimFrame;

				//_captureGIF_CurAnimProcess = 0;
				//_captureGIF_TotalAnimProcess = (Mathf.Abs(_captureGIF_LastAnimFrame - _captureGIF_StartAnimFrame) + 1) * _captureGIF_AnimLoopCount;


				////1. GIF 헤더를 만들고
				////2. 이제 프레임을 하나씩 렌더링하기 시작하자

				//_captureMode = CAPTURE_MODE.Capturing_GIF_Animation;

				////GIF 헤더
				//bool isHeaderResult = _editor.Exporter.MakeGIFHeader(saveFilePath, _captureSelectedAnimClip, _editor._captureFrame_DstWidth, _editor._captureFrame_DstHeight);

				//if(!isHeaderResult)
				//{
				//	//실패한 경우
				//	_captureMode = CAPTURE_MODE.None;
				//	return;
				//}


				////첫번째 프레임
				////Request를 만든다.
				//apScreenCaptureRequest newRequest = new apScreenCaptureRequest();
				//_captureLoadKey = newRequest.MakeAnimCapture(OnGIFFrameCaptured,
				//											_editor,
				//											_editor.Select.RootUnit._childMeshGroup,
				//											true,
				//											_captureSelectedAnimClip, _captureGIF_CurAnimFrame,
				//											(int)(_editor._captureFrame_PosX + apGL.WindowSizeHalf.x),
				//											(int)(_editor._captureFrame_PosY + apGL.WindowSizeHalf.y),
				//											_editor._captureFrame_SrcWidth, _editor._captureFrame_SrcHeight,
				//											_editor._captureFrame_DstWidth, _editor._captureFrame_DstHeight,
				//											_editor._captureFrame_Color, _captureGIF_CurAnimProcess, saveFilePath);

				////에디터에 대신 렌더링해달라고 요청을 합시다.
				//_editor.ScreenCaptureRequest(newRequest);
				//_editor.SetRepaint(); 
				#endregion
			}
		}



		private void StartMP4Animation()
		{
			if (_captureSelectedAnimClip == null || _editor.Select.RootUnit._childMeshGroup == null)
			{
				return;
			}

			string defFileName = "MP4_" + DateTime.Now.Year + "" + DateTime.Now.Month + "" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + ".mp4";
			string saveFilePath = EditorUtility.SaveFilePanel("Save MP4 Animation", _capturePrevFilePath_Directory, defFileName, "mp4");
			if (!string.IsNullOrEmpty(saveFilePath))
			{
				bool isResult = Editor.SeqExporter.StartMP4Animation(_editor.Select.RootUnit,
						_captureSelectedAnimClip,
						_editor._captureFrame_GIFSampleLoopCount,
						saveFilePath,
						OnGIFMP4AnimationSaved);

				if (isResult)
				{
					System.IO.FileInfo fi = new System.IO.FileInfo(saveFilePath);
					_capturePrevFilePath_Directory = fi.Directory.FullName;
					_captureMode = CAPTURE_MODE.Capturing_MP4_Animation;
				}

			}
		}


		//4. Sprite Sheet로 만들기
		private void StartSpriteSheet(bool isSequenceFiles)
		{
			if (RootUnitAnimClipList.Count != _captureSprite_AnimClipFlags.Count || _editor.Select.RootUnit._childMeshGroup == null)
			{
				return;
			}
			string defFileName = "";
			string saveFileDialogTitle = "";
			if (!isSequenceFiles)
			{
				defFileName = "Spritesheet_" + DateTime.Now.Year + "" + DateTime.Now.Month + "" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + ".png";
				saveFileDialogTitle = "Save Spritesheet";
			}
			else
			{
				defFileName = "Sequence_" + DateTime.Now.Year + "" + DateTime.Now.Month + "" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + ".png";
				saveFileDialogTitle = "Save Sequence Files";
			}

			string saveFilePath = EditorUtility.SaveFilePanel(saveFileDialogTitle, _capturePrevFilePath_Directory, defFileName, "png");
			if (!string.IsNullOrEmpty(saveFilePath))
			{


				bool isResult = Editor.SeqExporter.StartSpritesheet(_editor.Select.RootUnit,
													RootUnitAnimClipList,
													_captureSprite_AnimClipFlags,
													saveFilePath,
													_editor._captureSpriteTrimSize == apEditor.CAPTURE_SPRITE_TRIM_METHOD.Compressed,
													isSequenceFiles,
													Editor._captureFrame_SpriteMargin,
													Editor._captureSpriteMeta_XML,
													Editor._captureSpriteMeta_JSON,
													Editor._captureSpriteMeta_TXT,
													OnSpritesheetSaved);

				if (isResult)
				{
					System.IO.FileInfo fi = new System.IO.FileInfo(saveFilePath);
					_capturePrevFilePath_Directory = fi.Directory.FullName;
					_captureMode = CAPTURE_MODE.Capturing_Spritesheet;
				}
			}
		}


		//추가 11.15 : 
		/// <summary>
		/// 그래픽 가속을 지원하지 않는 경우, 안내 메시지를 보여주고 선택하게 해야한다.
		/// 그래픽 가속을 지원하거나 계속 처리가 가능한 경우(무시 포함) true를 리턴한다.
		/// </summary>
		/// <returns></returns>
		private bool CheckComputeShaderSupportedForScreenCapture()
		{
			//1. Compute Shader를 지원한다.
			if (SystemInfo.supportsComputeShaders)
			{
				return true;
			}

#if UNITY_EDITOR_WIN
			// 이 코드는 Window 에디터에서만 열린다.
			int iBtn = EditorUtility.DisplayDialogComplex(Editor.GetText(TEXT.DLG_NoComputeShaderOnCapture_Title),
															Editor.GetText(TEXT.DLG_NoComputeShaderOnCapture_Body),
															Editor.GetText(TEXT.DLG_NoComputeShaderOnCapture_IgnoreAndCapture),
															Editor.GetText(TEXT.DLG_NoComputeShaderOnCapture_OpenBuildSettings),
															Editor.GetText(TEXT.Cancel)
															);

			if (iBtn == 0)
			{
				//무시하고 진행하기

				return true;
			}
			else if (iBtn == 1)
			{
				//BuildSetting 창을 열자
				EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
			}

			return false;

#else
			return true;
#endif


		}



		private void OnThumbnailCaptured(bool isSuccess, Texture2D captureImage, int iProcessStep, string filePath, object loadKey)
		{
			_captureMode = CAPTURE_MODE.None;

			//우왕 왔당
			if (!isSuccess || captureImage == null)
			{
				//Debug.LogError("Failed..");
				if (captureImage != null)
				{
					UnityEngine.GameObject.DestroyImmediate(captureImage);
				}
				_captureLoadKey = null;

				return;
			}
			if (_captureLoadKey != loadKey)
			{
				//Debug.LogError("LoadKey Mismatched");

				if (captureImage != null)
				{
					UnityEngine.GameObject.DestroyImmediate(captureImage);
				}
				_captureLoadKey = null;
				return;
			}


			//이제 처리합시당 (Destroy도 포함되어있다)
			string filePathWOExtension = _editor._portrait._imageFilePath_Thumbnail.Substring(0, _editor._portrait._imageFilePath_Thumbnail.Length - 4);
			bool isSaveSuccess = _editor.Exporter.SaveTexture2DToPNG(captureImage, filePathWOExtension, true);

			if (isSaveSuccess)
			{
				AssetDatabase.Refresh();

				_editor._portrait._thumbnailImage = AssetDatabase.LoadAssetAtPath<Texture2D>(_editor._portrait._imageFilePath_Thumbnail);
			}
		}


		private void OnScreeenShotCaptured(bool isSuccess, Texture2D captureImage, int iProcessStep, string filePath, object loadKey)
		{
			_captureMode = CAPTURE_MODE.None;

			//우왕 왔당
			if (!isSuccess || captureImage == null || string.IsNullOrEmpty(filePath))
			{
				//Debug.LogError("Failed..");
				if (captureImage != null)
				{
					UnityEngine.GameObject.DestroyImmediate(captureImage);
				}
				_captureLoadKey = null;

				return;
			}
			if (_captureLoadKey != loadKey)
			{
				//Debug.LogError("LoadKey Mismatched");

				if (captureImage != null)
				{
					UnityEngine.GameObject.DestroyImmediate(captureImage);
				}
				_captureLoadKey = null;
				return;
			}

			//이제 파일로 저장하자
			try
			{
				string filePathWOExtension = filePath.Substring(0, filePath.Length - 4);

				//AutoDestroy = true
				bool isSaveSuccess = _editor.Exporter.SaveTexture2DToPNG(captureImage, filePathWOExtension, true);

				if (isSaveSuccess)
				{
					System.IO.FileInfo fi = new System.IO.FileInfo(filePath);

					Application.OpenURL("file://" + fi.Directory.FullName);
					Application.OpenURL("file://" + filePath);

					//_prevFilePath = filePath;
					_capturePrevFilePath_Directory = fi.Directory.FullName;
				}
			}
			catch (Exception)
			{

			}
		}






		private void OnGIFMP4AnimationSaved(bool isResult)
		{
			_captureMode = CAPTURE_MODE.None;
		}

		private void OnSpritesheetSaved(bool isResult)
		{
			//Debug.LogError("OnSpritesheetSaved : " + isResult);
			_captureMode = CAPTURE_MODE.None;
		}

		//------------------------------------------------------------------------------------------------------------------------


		//private string _prevParamName = "";
		private void Draw_Param(int width, int height)
		{
			EditorGUILayout.Space();

			apControlParam cParam = _param;
			if (cParam == null)
			{
				SetNone();
				return;
			}
			if (_prevParam != cParam)
			{
				_prevParam = cParam;
				//_prevParamName = cParam._keyName;
			}
			if (cParam._isReserved)
			{
				//GUIStyle guiStyle_RedTextColor = new GUIStyle(GUI.skin.label);
				//guiStyle_RedTextColor.normal.textColor = Color.red;

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ReservedParameter), apGUIStyleWrapper.I.Label_RedColor);//"Reserved Parameter"
				GUILayout.Space(10);
			}

			//bool isChanged = false;
			apControlParam.CATEGORY next_category = cParam._category;
			apControlParam.ICON_PRESET next_iconPreset = cParam._iconPreset;
			apControlParam.TYPE next_valueType = cParam._valueType;

			string next_label_Min = cParam._label_Min;
			string next_label_Max = cParam._label_Max;
			int next_snapSize = cParam._snapSize;

			int next_int_Def = cParam._int_Def;
			float next_float_Def = cParam._float_Def;
			Vector2 next_vec2_Def = cParam._vec2_Def;
			int next_int_Min = cParam._int_Min;
			int next_int_Max = cParam._int_Max;
			float next_float_Min = cParam._float_Min;
			float next_float_Max = cParam._float_Max;
			Vector2 next_vec2_Min = cParam._vec2_Min;
			Vector2 next_vec2_Max = cParam._vec2_Max;





			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.NameUnique));//"Name (Unique)"

			if (cParam._isReserved)
			{
				EditorGUILayout.LabelField(cParam._keyName, GUI.skin.textField, apGUILOFactory.I.Width(width));
			}
			else
			{
				string nextKeyName = EditorGUILayout.DelayedTextField(cParam._keyName, apGUILOFactory.I.Width(width));
				if (!string.Equals(nextKeyName, cParam._keyName))
				{
					if (string.IsNullOrEmpty(nextKeyName))
					{
						//이름이 빈칸이다
						//EditorUtility.DisplayDialog("Error", "Empty Name is not allowed", "Okay");

						EditorUtility.DisplayDialog(Editor.GetText(TEXT.ControlParamNameError_Title),
													Editor.GetText(TEXT.ControlParamNameError_Body_Wrong),
													Editor.GetText(TEXT.Close));
					}
					else if (Editor.ParamControl.FindParam(nextKeyName) != null)
					{
						//이미 사용중인 이름이다.
						//EditorUtility.DisplayDialog("Error", "It is used Name", "Okay");
						EditorUtility.DisplayDialog(Editor.GetText(TEXT.ControlParamNameError_Title),
												Editor.GetText(TEXT.ControlParamNameError_Body_Used),
												Editor.GetText(TEXT.Close));
					}
					else
					{


						Editor.Controller.ChangeParamName(cParam, nextKeyName);
						cParam._keyName = nextKeyName;
					}
				}
			}
			#region [미사용 코드] DelayedTextField를 사용하기 전
			//EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			//if (cParam._isReserved)
			//{
			//	//TextField의 Skin을 사용하지만 작동은 불가능한 Label
			//	EditorGUILayout.LabelField(cParam._keyName, GUI.skin.textField);
			//	//EditorGUILayout.TextField(cParam._keyName);
			//}
			//else
			//{
			//	_prevParamName = EditorGUILayout.TextField(_prevParamName);
			//	if (GUILayout.Button("Change", GUILayout.Width(60)))
			//	{
			//		if (!_prevParamName.Equals(cParam._keyName))
			//		{
			//			if (string.IsNullOrEmpty(_prevParamName))
			//			{
			//				EditorUtility.DisplayDialog("Error", "Empty Name is not allowed", "Okay");

			//				_prevParamName = cParam._keyName;
			//			}
			//			else
			//			{
			//				if (Editor.ParamControl.FindParam(_prevParamName) != null)
			//				{
			//					EditorUtility.DisplayDialog("Error", "It is used Name", "Okay");

			//					_prevParamName = cParam._keyName;
			//				}
			//				else
			//				{
			//					//cParam._keyName = _prevParamName;

			//					//수정
			//					//링크가 깨지지 않도록 전체적으로 검색하여 키 이름을 바꾸어주자
			//					Editor.Controller.ChangeParamName(cParam, _prevParamName);
			//					cParam._keyName = _prevParamName;
			//				}
			//			}


			//		}
			//		GUI.FocusControl("");
			//		//Editor.Hierarchy.RefreshUnits();
			//		Editor.RefreshControllerAndHierarchy();
			//	}
			//}
			//EditorGUILayout.EndHorizontal(); 

			#endregion
			EditorGUILayout.Space();

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ValueType));//"Type"
			if (cParam._isReserved)
			{
				EditorGUILayout.EnumPopup(cParam._valueType);
			}
			else
			{
				next_valueType = (apControlParam.TYPE)EditorGUILayout.EnumPopup(cParam._valueType);
			}
			EditorGUILayout.Space();

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Category));//"Category"
			if (cParam._isReserved)
			{
				EditorGUILayout.EnumPopup(cParam._category);
			}
			else
			{
				next_category = (apControlParam.CATEGORY)EditorGUILayout.EnumPopup(cParam._category);
			}
			GUILayout.Space(10);

			int iconSize = 32;
			int iconPresetHeight = 32;
			int presetCategoryWidth = width - (iconSize + 8 + 5);
			Texture2D imgIcon = Editor.ImageSet.Get(apEditorUtil.GetControlParamPresetIconType(cParam._iconPreset));

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconPresetHeight));
			GUILayout.Space(2);

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(presetCategoryWidth), apGUILOFactory.I.Height(iconPresetHeight));

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IconPreset), apGUILOFactory.I.Width(presetCategoryWidth));//"Icon Preset"
			next_iconPreset = (apControlParam.ICON_PRESET)EditorGUILayout.EnumPopup(cParam._iconPreset, apGUILOFactory.I.Width(presetCategoryWidth));

			EditorGUILayout.EndVertical();
			GUILayout.Space(2);

			//이전
			//EditorGUILayout.LabelField(new GUIContent(imgIcon), GUILayout.Width(iconSize), GUILayout.Height(iconPresetHeight));

			//변경
			if (_guiContent_Param_IconPreset == null)
			{
				_guiContent_Param_IconPreset = apGUIContentWrapper.Make(imgIcon);
			}
			else
			{
				_guiContent_Param_IconPreset.SetImage(imgIcon);
			}

			EditorGUILayout.LabelField(_guiContent_Param_IconPreset.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconPresetHeight));


			EditorGUILayout.EndHorizontal();


			EditorGUILayout.Space();


			string strRangeLabelName_Min = Editor.GetUIWord(UIWORD.Min);//"Min"
			string strRangeLabelName_Max = Editor.GetUIWord(UIWORD.Max);//"Max"
			switch (cParam._valueType)
			{
				case apControlParam.TYPE.Int:
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Param_IntegerType));//"Integer Type"
					EditorGUILayout.Space();

					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Param_DefaultValue));//"Default Value"
					next_int_Def = EditorGUILayout.DelayedIntField(cParam._int_Def);
					break;

				case apControlParam.TYPE.Float:
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Param_FloatType));//"Float Number Type"
					EditorGUILayout.Space();

					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Param_DefaultValue));//"Default Value"
					next_float_Def = EditorGUILayout.DelayedFloatField(cParam._float_Def);
					break;

				case apControlParam.TYPE.Vector2:
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Param_Vector2Type));//"Vector2 Type"
					EditorGUILayout.Space();

					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Param_DefaultValue));//"Default Value"

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					next_vec2_Def.x = EditorGUILayout.DelayedFloatField(cParam._vec2_Def.x, apGUILOFactory.I.Width((width / 2) - 2));
					next_vec2_Def.y = EditorGUILayout.DelayedFloatField(cParam._vec2_Def.y, apGUILOFactory.I.Width((width / 2) - 2));
					EditorGUILayout.EndHorizontal();

					strRangeLabelName_Min = Editor.GetUIWord(UIWORD.Param_Axis1);//"Axis 1"
					strRangeLabelName_Max = Editor.GetUIWord(UIWORD.Param_Axis2);//"Axis 2"
					break;
			}
			GUILayout.Space(25);

			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(25);


			GUILayoutOption opt_Label = apGUILOFactory.I.Width(50);
			GUILayoutOption opt_Data = apGUILOFactory.I.Width(width - (50 + 5));
			GUILayoutOption opt_SubData2 = apGUILOFactory.I.Width((width - (50 + 5)) / 2 - 2);

			//GUIStyle guiStyle_LabelRight = new GUIStyle(GUI.skin.label);
			//guiStyle_LabelRight.alignment = TextAnchor.MiddleRight;

			GUILayout.Space(25);
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.RangeValueLabel));//"Range Value Label" -> Name of value Range

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(strRangeLabelName_Min, opt_Label);
			next_label_Min = EditorGUILayout.DelayedTextField(cParam._label_Min, opt_Data);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(strRangeLabelName_Max, opt_Label);
			next_label_Max = EditorGUILayout.DelayedTextField(cParam._label_Max, opt_Data);
			EditorGUILayout.EndHorizontal();


			GUILayout.Space(25);

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Range));//"Range Value" -> "Range"


			switch (cParam._valueType)
			{
				case apControlParam.TYPE.Int:
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Min), opt_Label);//"Min"
					next_int_Min = EditorGUILayout.DelayedIntField(cParam._int_Min, opt_Data);
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Max), opt_Label);//"Max"
					next_int_Max = EditorGUILayout.DelayedIntField(cParam._int_Max, opt_Data);
					EditorGUILayout.EndHorizontal();
					break;

				case apControlParam.TYPE.Float:
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Min), opt_Label);//"Min"
					next_float_Min = EditorGUILayout.DelayedFloatField(cParam._float_Min, opt_Data);
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Max), opt_Label);//"Max"
					next_float_Max = EditorGUILayout.DelayedFloatField(cParam._float_Max, opt_Data);
					EditorGUILayout.EndHorizontal();
					break;

				case apControlParam.TYPE.Vector2:
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(apStringFactory.I.None, opt_Label);
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Min), opt_SubData2);//"Min"
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Max), apGUIStyleWrapper.I.Label_MiddleRight, opt_SubData2);//"Max"
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(apStringFactory.I.X, opt_Label);
					next_vec2_Min.x = EditorGUILayout.DelayedFloatField(cParam._vec2_Min.x, opt_SubData2);
					next_vec2_Max.x = EditorGUILayout.DelayedFloatField(cParam._vec2_Max.x, opt_SubData2);
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(apStringFactory.I.Y, opt_Label);
					next_vec2_Min.y = EditorGUILayout.DelayedFloatField(cParam._vec2_Min.y, opt_SubData2);
					next_vec2_Max.y = EditorGUILayout.DelayedFloatField(cParam._vec2_Max.y, opt_SubData2);
					EditorGUILayout.EndHorizontal();
					break;

			}


			if (cParam._valueType == apControlParam.TYPE.Float ||
				cParam._valueType == apControlParam.TYPE.Vector2)
			{
				GUILayout.Space(10);



				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SnapSize));//"Snap Size"
				next_snapSize = EditorGUILayout.DelayedIntField(cParam._snapSize, apGUILOFactory.I.Width(width));
				//if (next_snapSize != cParam._snapSize)
				//{
				//	cParam._snapSize = nextSnapSize;
				//	if (cParam._snapSize < 1)
				//	{
				//		cParam._snapSize = 1;
				//	}
				//	GUI.FocusControl(null);
				//}
			}



			if (next_category != cParam._category ||
				next_iconPreset != cParam._iconPreset ||
				next_valueType != cParam._valueType ||

				next_label_Min != cParam._label_Min ||
				next_label_Max != cParam._label_Max ||
				next_snapSize != cParam._snapSize ||

				next_int_Def != cParam._int_Def ||
				next_float_Def != cParam._float_Def ||
				next_vec2_Def.x != cParam._vec2_Def.x ||
				next_vec2_Def.y != cParam._vec2_Def.y ||

				next_int_Min != cParam._int_Min ||
				next_int_Max != cParam._int_Max ||

				next_float_Min != cParam._float_Min ||
				next_float_Max != cParam._float_Max ||

				next_vec2_Min.x != cParam._vec2_Min.x ||
				next_vec2_Min.y != cParam._vec2_Min.y ||
				next_vec2_Max.x != cParam._vec2_Max.x ||
				next_vec2_Max.y != cParam._vec2_Max.y
				)
			{
				apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.ControlParam_SettingChanged, Editor, Editor._portrait, null, false);

				if (next_snapSize < 1)
				{
					next_snapSize = 1;
				}

				if (cParam._iconPreset != next_iconPreset)
				{
					cParam._isIconChanged = true;
				}
				else if (cParam._category != next_category && !cParam._isIconChanged)
				{
					//아이콘을 한번도 바꾸지 않았더라면 자동으로 다음 아이콘을 추천해주자
					next_iconPreset = apEditorUtil.GetControlParamPresetIconTypeByCategory(next_category);
				}

				cParam._category = next_category;
				cParam._iconPreset = next_iconPreset;
				cParam._valueType = next_valueType;

				cParam._label_Min = next_label_Min;
				cParam._label_Max = next_label_Max;
				cParam._snapSize = next_snapSize;

				cParam._int_Def = next_int_Def;
				cParam._float_Def = next_float_Def;
				cParam._vec2_Def = next_vec2_Def;

				cParam._int_Min = next_int_Min;
				cParam._int_Max = next_int_Max;

				cParam._float_Min = next_float_Min;
				cParam._float_Max = next_float_Max;

				cParam._vec2_Min = next_vec2_Min;
				cParam._vec2_Max = next_vec2_Max;

				cParam.MakeInterpolationRange();
				GUI.FocusControl(null);
			}


			GUILayout.Space(30);

			//"Presets"
			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.Presets), Editor.ImageSet.Get(apImageSet.PRESET.ControlParam_Palette)), GUILayout.Height(30)))

			//변경
			if (_guiContent_Param_Presets == null)
			{
				_guiContent_Param_Presets = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Presets), Editor.ImageSet.Get(apImageSet.PRESET.ControlParam_Palette));
			}
			if (GUILayout.Button(_guiContent_Param_Presets.Content, apGUILOFactory.I.Height(30)))
			{
				_loadKey_OnSelectControlParamPreset = apDialog_ControlParamPreset.ShowDialog(Editor, cParam, OnSelectControlParamPreset);
			}

			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);





			if (!cParam._isReserved)
			{


				//"Remove Parameter"
				//이전
				//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveParameter),
				//								Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
				//								),
				//				GUILayout.Height(24)))

				//변경
				if (_guiContent_Param_RemoveParam == null)
				{
					_guiContent_Param_RemoveParam = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveParameter), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
				}

				if (GUILayout.Button(_guiContent_Param_RemoveParam.Content, apGUILOFactory.I.Height(24)))
				{
					string strRemoveParamText = Editor.Controller.GetRemoveItemMessage(_portrait,
														cParam,
														5,
														Editor.GetTextFormat(TEXT.RemoveControlParam_Body, cParam._keyName),
														Editor.GetText(TEXT.DLG_RemoveItemChangedWarning)
														);

					//bool isResult = EditorUtility.DisplayDialog("Warning", "If this param removed, some motion data may be not worked correctly", "Remove it!", "Cancel");
					bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveControlParam_Title),
																	//Editor.GetTextFormat(TEXT.RemoveControlParam_Body, cParam._keyName),
																	strRemoveParamText,
																	Editor.GetText(TEXT.Remove),
																	Editor.GetText(TEXT.Cancel));
					if (isResult)
					{
						Editor.Controller.RemoveParam(cParam);
					}
				}
			}
		}



		private void OnSelectControlParamPreset(bool isSuccess, object loadKey, apControlParamPresetUnit controlParamPresetUnit, apControlParam controlParam)
		{
			if (!isSuccess
				|| _loadKey_OnSelectControlParamPreset != loadKey
				|| controlParamPresetUnit == null
				|| controlParam != Param)
			{
				_loadKey_OnSelectControlParamPreset = null;
				return;
			}
			_loadKey_OnSelectControlParamPreset = null;

			//ControlParam에 프리셋 정보를 넣어주자
			Editor.Controller.SetControlParamPreset(controlParam, controlParamPresetUnit);
		}


		private void DrawTitle(string strTitle, int width, int height)
		{
			int titleWidth = width;

			//삭제 19.8.18 : Layout 출력 여부 버튼의 위치가 바뀌었다.
			//bool isShowHideBtn = false;
			//if (_selectionType == SELECTION_TYPE.MeshGroup || _selectionType == SELECTION_TYPE.Animation)
			//{
			//	titleWidth = width - (height + 2);
			//	isShowHideBtn = true;
			//}

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height));

			GUILayout.Space(5);

			//GUIStyle guiStyle_Box = new GUIStyle(GUI.skin.box);
			//guiStyle_Box.normal.textColor = Color.white;
			//guiStyle_Box.alignment = TextAnchor.MiddleCenter;
			//guiStyle_Box.margin = GUI.skin.label.margin;


			Color prevColor = GUI.backgroundColor;
			//GUI.backgroundColor = new Color(0.0f, 0.2f, 0.3f, 1.0f);
			GUI.backgroundColor = apEditorUtil.ToggleBoxColor_Selected;

			GUILayout.Box(strTitle, apGUIStyleWrapper.I.Box_MiddleCenter_LabelMargin_WhiteColor, apGUILOFactory.I.Width(titleWidth), apGUILOFactory.I.Height(20));

			GUI.backgroundColor = prevColor;


			EditorGUILayout.EndHorizontal();
		}

		//---------------------------------------------------------------------------


		//---------------------------------------------------------------------
		/// <summary>
		/// Mesh Property GUI에서 "조작 방법"에 대한 안내 UI를 보여준다.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="msgMouseLeft">마우스 좌클릭에 대한 설명 (없을 경우 null)</param>
		/// <param name="msgMouseMiddle">마우스 휠클릭에 대한 설명 (없을 경우 null)</param>
		/// <param name="msgMouseRight">마우스 우클릭에 대한 설명 (없을 경우 null)</param>
		/// <param name="msgKeyboardList">키보드 입력에 대한 설명. 여러개 가능</param>
		private void DrawHowToControl(int width, string msgMouseLeft, string msgMouseMiddle, string msgMouseRight, string msgKeyboardDelete = null, string msgKeyboardCtrl = null, string msgKeyboardShift = null)
		{
			bool isMouseLeft = !string.IsNullOrEmpty(msgMouseLeft);
			bool isMouseMiddle = !string.IsNullOrEmpty(msgMouseMiddle);
			bool isMouseRight = !string.IsNullOrEmpty(msgMouseRight);
			bool isKeyDelete = !string.IsNullOrEmpty(msgKeyboardDelete);
			bool isKeyCtrl = !string.IsNullOrEmpty(msgKeyboardCtrl);
			bool isKeyShift = !string.IsNullOrEmpty(msgKeyboardShift);
			//int nKeyMsg = 0;
			//if (msgKeyboardList != null)
			//{
			//	nKeyMsg = msgKeyboardList.Length;
			//}

			//GUIStyle guiStyle_Icon = new GUIStyle(GUI.skin.label);
			//guiStyle_Icon.margin = GUI.skin.box.margin;

			int labelSize = 30;
			int subTextWidth = width - (labelSize + 8);


			if (_guiContent_MeshProperty_HowTo_MouseLeft == null) { _guiContent_MeshProperty_HowTo_MouseLeft = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MouseLeft)); }
			if (_guiContent_MeshProperty_HowTo_MouseMiddle == null) { _guiContent_MeshProperty_HowTo_MouseMiddle = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MouseMiddle)); }
			if (_guiContent_MeshProperty_HowTo_MouseRight == null) { _guiContent_MeshProperty_HowTo_MouseRight = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MouseRight)); }
			if (_guiContent_MeshProperty_HowTo_KeyDelete == null) { _guiContent_MeshProperty_HowTo_KeyDelete = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Edit_KeyDelete)); }
			if (_guiContent_MeshProperty_HowTo_KeyCtrl == null) { _guiContent_MeshProperty_HowTo_KeyCtrl = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Edit_KeyCtrl)); }
			if (_guiContent_MeshProperty_HowTo_KeyShift == null) { _guiContent_MeshProperty_HowTo_KeyShift = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Edit_KeyShift)); }

			if (isMouseLeft)
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(5);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MouseLeft)), guiStyle_Icon, GUILayout.Width(labelSize), GUILayout.Height(labelSize));
				EditorGUILayout.LabelField(_guiContent_MeshProperty_HowTo_MouseLeft.Content, apGUIStyleWrapper.I.Label_BoxMargin, apGUILOFactory.I.Width(labelSize), apGUILOFactory.I.Height(labelSize));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(8);
				EditorGUILayout.LabelField(msgMouseLeft, apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(20));
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			if (isMouseMiddle)
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(5);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MouseMiddle)), guiStyle_Icon, GUILayout.Width(labelSize), GUILayout.Height(labelSize));
				EditorGUILayout.LabelField(_guiContent_MeshProperty_HowTo_MouseMiddle.Content, apGUIStyleWrapper.I.Label_BoxMargin, apGUILOFactory.I.Width(labelSize), apGUILOFactory.I.Height(labelSize));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(8);
				EditorGUILayout.LabelField(msgMouseMiddle, apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(20));
				EditorGUILayout.EndVertical();


				EditorGUILayout.EndHorizontal();
			}

			if (isMouseRight)
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(5);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MouseRight)), guiStyle_Icon, GUILayout.Width(labelSize), GUILayout.Height(labelSize));
				EditorGUILayout.LabelField(_guiContent_MeshProperty_HowTo_MouseRight.Content, apGUIStyleWrapper.I.Label_BoxMargin, apGUILOFactory.I.Width(labelSize), apGUILOFactory.I.Height(labelSize));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(8);
				EditorGUILayout.LabelField(msgMouseRight, apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(20));
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			if (isKeyDelete)
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(5);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_KeyDelete)), guiStyle_Icon, GUILayout.Width(labelSize), GUILayout.Height(labelSize));
				EditorGUILayout.LabelField(_guiContent_MeshProperty_HowTo_KeyDelete.Content, apGUIStyleWrapper.I.Label_BoxMargin, apGUILOFactory.I.Width(labelSize), apGUILOFactory.I.Height(labelSize));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(8);
				EditorGUILayout.LabelField(msgKeyboardDelete, apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(20));
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			if (isKeyCtrl)
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(5);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_KeyCtrl)), guiStyle_Icon, GUILayout.Width(labelSize), GUILayout.Height(labelSize));
				EditorGUILayout.LabelField(_guiContent_MeshProperty_HowTo_KeyCtrl.Content, apGUIStyleWrapper.I.Label_BoxMargin, apGUILOFactory.I.Width(labelSize), apGUILOFactory.I.Height(labelSize));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(8);
				EditorGUILayout.LabelField(msgKeyboardCtrl, apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(20));
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			if (isKeyShift)
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(5);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_KeyShift)), guiStyle_Icon, GUILayout.Width(labelSize), GUILayout.Height(labelSize));
				EditorGUILayout.LabelField(_guiContent_MeshProperty_HowTo_KeyShift.Content, apGUIStyleWrapper.I.Label_BoxMargin, apGUILOFactory.I.Width(labelSize), apGUILOFactory.I.Height(labelSize));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(labelSize));
				GUILayout.Space(8);
				EditorGUILayout.LabelField(msgKeyboardShift, apGUILOFactory.I.Width(subTextWidth), apGUILOFactory.I.Height(20));
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(20);
		}

		//private string _prevMesh_Name = "";

		private void MeshProperty_None(int width, int height)
		{
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Name));//"Name"

			string nextMeshName = EditorGUILayout.DelayedTextField(_mesh._name, apGUILOFactory.I.Width(width));
			if (!string.Equals(nextMeshName, _mesh._name))
			{
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, _mesh, null, false);
				_mesh._name = nextMeshName;
				Editor.RefreshControllerAndHierarchy(false);
			}



			EditorGUILayout.Space();

			//1. 어느 텍스쳐를 사용할 것인가
			//[수정]
			//다이얼로그를 보여주자

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Image));//"Image"
																	   //apTextureData textureData = _mesh._textureData;

			string strTextureName = null;
			Texture2D curTextureImage = null;
			int selectedImageHeight = 20;
			if (_mesh.LinkedTextureData != null)
			{
				strTextureName = _mesh.LinkedTextureData._name;
				curTextureImage = _mesh.LinkedTextureData._image;

				if (curTextureImage != null && _mesh.LinkedTextureData._width > 0 && _mesh.LinkedTextureData._height > 0)
				{
					selectedImageHeight = (int)((float)(width * _mesh.LinkedTextureData._height) / (float)(_mesh.LinkedTextureData._width));
				}
			}

			if (_guiContent_MeshProperty_Texture == null)
			{
				_guiContent_MeshProperty_Texture = new apGUIContentWrapper();
			}


			if (curTextureImage != null)
			{
				//EditorGUILayout.TextField(strTextureName);
				EditorGUILayout.LabelField(strTextureName != null ? strTextureName : apStringFactory.I.NoImage);
				GUILayout.Space(10);

				//EditorGUILayout.LabelField(new GUIContent(curTextureImage), GUILayout.Height(selectedImageHeight));
				_guiContent_MeshProperty_Texture.SetImage(curTextureImage);
				EditorGUILayout.LabelField(_guiContent_MeshProperty_Texture.Content, apGUILOFactory.I.Height(selectedImageHeight));
				GUILayout.Space(10);
			}
			else
			{
				EditorGUILayout.LabelField(apStringFactory.I.NoImage);
			}

			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.ChangeImage), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Image)), GUILayout.Height(30)))//"  Change Image"

			//변경
			if (_guiContent_MeshProperty_ChangeImage == null)
			{
				_guiContent_MeshProperty_ChangeImage = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.ChangeImage), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Image));
			}

			if (GUILayout.Button(_guiContent_MeshProperty_ChangeImage.Content, apGUILOFactory.I.Height(30)))//"  Change Image"
			{
				//_isShowTextureDataList = !_isShowTextureDataList;
				_loadKey_SelectTextureDataToMesh = apDialog_SelectTextureData.ShowDialog(Editor, _mesh, OnSelectTextureDataToMesh);
			}

			//EditorGUILayout.Space();


			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			if (curTextureImage != null)
			{
				//Atlas + Texutre의 Read 세팅
				DrawMeshAtlasOption(width);

				GUILayout.Space(10);
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(10);
			}

			if (_guiContent_MeshProperty_ResetVerts == null)
			{
				_guiContent_MeshProperty_ResetVerts = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.ResetVertices), false, apStringFactory.I.RemoveAllVerticesAndPolygons);//"Remove all Vertices and Polygons"
			}

			//2. 버텍스 세팅
			if (GUILayout.Button(_guiContent_MeshProperty_ResetVerts.Content))//"Reset Vertices"
			{
				if (_mesh.LinkedTextureData != null && _mesh.LinkedTextureData._image != null)
				{
					bool isConfirmReset = false;
					if (_mesh._vertexData != null && _mesh._vertexData.Count > 0 &&
						_mesh._indexBuffer != null && _mesh._indexBuffer.Count > 0)
					{
						//isConfirmReset = EditorUtility.DisplayDialog("Reset Vertex", "If you reset vertices, All data is reset.", "Reset", "Cancel");
						isConfirmReset = EditorUtility.DisplayDialog(Editor.GetText(TEXT.ResetMeshVertices_Title),
																		Editor.GetText(TEXT.ResetMeshVertices_Body),
																		Editor.GetText(TEXT.ResetMeshVertices_Okay),
																		Editor.GetText(TEXT.Cancel));


					}
					else
					{
						isConfirmReset = true;
					}

					if (isConfirmReset)
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_ResetVertices, Editor, _mesh, _mesh, false);

						_mesh._vertexData.Clear();
						_mesh._indexBuffer.Clear();
						_mesh._edges.Clear();
						_mesh._polygons.Clear();
						_mesh.MakeEdgesToPolygonAndIndexBuffer();

						_mesh.ResetVerticesByImageOutline();
						_mesh.MakeEdgesToPolygonAndIndexBuffer();

						Editor.Controller.ResetAllRenderUnitsVertexIndex();//<<추가. RenderUnit에 Mesh 변경사항 반영
					}
				}
			}

			//와! 새해 첫 코드!
			//추가 20.1.5 : 메시 복제하기
			GUILayout.Space(5);
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.Duplicate), apGUILOFactory.I.Width(width)))//"Duplicate"
			{
				Editor.Controller.DuplicateMesh(_mesh);
			}


			// Remove Mesh
			GUILayout.Space(20);

			if (_guiContent_MeshProperty_RemoveMesh == null)
			{
				_guiContent_MeshProperty_RemoveMesh = new apGUIContentWrapper();
				_guiContent_MeshProperty_RemoveMesh.ClearText(false);
				_guiContent_MeshProperty_RemoveMesh.AppendSpaceText(2, false);
				_guiContent_MeshProperty_RemoveMesh.AppendText(Editor.GetUIWord(UIWORD.RemoveMesh), true);
				_guiContent_MeshProperty_RemoveMesh.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}

			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveMesh), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform) ),
			//						GUILayout.Height(24)))//"  Remove Mesh"

			//변경
			if (GUILayout.Button(_guiContent_MeshProperty_RemoveMesh.Content, apGUILOFactory.I.Height(24)))//"  Remove Mesh"
			{
				string strRemoveDialogInfo = Editor.Controller.GetRemoveItemMessage(
																_portrait,
																_mesh,
																5,
																Editor.GetTextFormat(TEXT.RemoveMesh_Body, _mesh._name),
																Editor.GetText(TEXT.DLG_RemoveItemChangedWarning)
																);

				//bool isResult = EditorUtility.DisplayDialog("Remove Mesh", "Do you want to remove [" + _mesh._name + "]?", "Remove", "Cancel");
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveMesh_Title),
																//Editor.GetTextFormat(TEXT.RemoveMesh_Body, _mesh._name),
																strRemoveDialogInfo,
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel));

				if (isResult)
				{
					//apEditorUtil.SetRecord("Remove Mesh", _portrait);

					//MonoBehaviour.DestroyImmediate(_mesh.gameObject);
					//_portrait._meshes.Remove(_mesh);
					Editor.Controller.RemoveMesh(_mesh);

					SetNone();
				}
			}
		}



		private void OnSelectTextureDataToMesh(bool isSuccess, apMesh targetMesh, object loadKey, apTextureData resultTextureData)
		{
			if (!isSuccess || resultTextureData == null || _mesh != targetMesh || _loadKey_SelectTextureDataToMesh != loadKey)
			{
				_loadKey_SelectTextureDataToMesh = null;
				return;
			}

			_loadKey_SelectTextureDataToMesh = null;

			//Undo
			apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SetImage, Editor, targetMesh, resultTextureData, false);

			//이전 코드
			//_mesh._textureData = resultTextureData;

			//변경 코드 4.1
			_mesh.SetTextureData(resultTextureData);

			//_isShowTextureDataList = false;

		}



		private void MeshProperty_Modify(int width, int height)
		{
			GUILayout.Space(10);
			//EditorGUILayout.LabelField("Left Click : Select Vertex");
			DrawHowToControl(width, Editor.GetUIWord(UIWORD.SelectVertex), null, null);//"Select Vertex"

			EditorGUILayout.Space();

			bool isSingleVertex = Editor.VertController.Vertex != null && Editor.VertController.Vertices.Count == 1;
			bool isMultipleVertex = Editor.VertController.Vertices.Count > 1;

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Property_Modify_UI_Single, isSingleVertex);//"Mesh Property Modify UI Single"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Property_Modify_UI_Multiple, isMultipleVertex);//"Mesh Property Modify UI Multiple"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Property_Modify_UI_No_Info, (Editor.VertController.Vertex == null));//"Mesh Property Modify UI No Info"

			if (isSingleVertex)
			{
				if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Property_Modify_UI_Single))//"Mesh Property Modify UI Single"
				{
					if (_strWrapper_64 == null)
					{
						_strWrapper_64 = new apStringWrapper(64);
					}
					_strWrapper_64.Clear();
					_strWrapper_64.Append(apStringFactory.I.Index_Colon, false);
					_strWrapper_64.Append(Editor.VertController.Vertex._index, true);


					EditorGUILayout.LabelField(_strWrapper_64.ToString());//"Index : " + Editor.VertController.Vertex._index

					GUILayout.Space(5);

					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Position));//"Position"
					Vector2 prevPos2 = Editor.VertController.Vertex._pos;
					Vector2 nextPos2 = apEditorUtil.DelayedVector2Field(Editor.VertController.Vertex._pos, width);

					GUILayout.Space(5);

					EditorGUILayout.LabelField(apStringFactory.I.UV);//"UV"
					Vector2 prevUV = Editor.VertController.Vertex._uv;
					Vector2 nextUV = apEditorUtil.DelayedVector2Field(Editor.VertController.Vertex._uv, width);

					GUILayout.Space(5);

					_strWrapper_64.Clear();
					_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Z_Depth), false);
					_strWrapper_64.Append(apStringFactory.I.DepthZeroToOne, true);

					EditorGUILayout.LabelField(_strWrapper_64.ToString());//Editor.GetUIWord(UIWORD.Z_Depth) + " (0~1)"
					float prevDepth = Editor.VertController.Vertex._zDepth;
					//float nextDepth = EditorGUILayout.DelayedFloatField(Editor.VertController.Vertex._zDepth, GUILayout.Width(width));
					float nextDepth = EditorGUILayout.Slider(Editor.VertController.Vertex._zDepth, 0.0f, 1.0f, apGUILOFactory.I.Width(width));

					if (nextPos2.x != prevPos2.x ||
						nextPos2.y != prevPos2.y ||
						nextUV.x != prevUV.x ||
						nextUV.y != prevUV.y ||
						nextDepth != prevDepth)
					{
						//Vertex 정보가 바뀌었다.
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_EditVertex, Editor, Mesh, Editor.VertController.Vertex, false);

						Editor.VertController.Vertex._pos = nextPos2;
						Editor.VertController.Vertex._uv = nextUV;
						Editor.VertController.Vertex._zDepth = nextDepth;

						//Mesh.RefreshPolygonsToIndexBuffer();
						Editor.SetRepaint();

					}
				}
			}
			else if (isMultipleVertex)
			{
				if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Property_Modify_UI_Multiple))//"Mesh Property Modify UI Multiple"
				{
					if (_strWrapper_64 == null)
					{
						_strWrapper_64 = new apStringWrapper(64);
					}
					_strWrapper_64.Clear();
					_strWrapper_64.Append(Editor.VertController.Vertices.Count, false);
					_strWrapper_64.AppendSpace(1, false);
					_strWrapper_64.Append(apStringFactory.I.VerticesSelected, true);

					EditorGUILayout.LabelField(_strWrapper_64.ToString());//Editor.VertController.Vertices.Count + " Vertices Selected"

					GUILayout.Space(5);

					_strWrapper_64.Clear();
					_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Z_Depth), false);
					_strWrapper_64.Append(apStringFactory.I.DepthZeroToOne, true);

					EditorGUILayout.LabelField(_strWrapper_64.ToString());//Editor.GetUIWord(UIWORD.Z_Depth) + " (0~1)"

					float prevDepth_Avg = 0.0f;
					float prevDepth_Min = -1.0f;
					float prevDepth_Max = -1.0f;

					apVertex vert = null;
					for (int i = 0; i < Editor.VertController.Vertices.Count; i++)
					{
						vert = Editor.VertController.Vertices[i];

						prevDepth_Avg += vert._zDepth;
						if (prevDepth_Min < 0.0f || vert._zDepth < prevDepth_Min)
						{
							prevDepth_Min = vert._zDepth;
						}

						if (prevDepth_Max < 0.0f || vert._zDepth > prevDepth_Max)
						{
							prevDepth_Max = vert._zDepth;
						}
					}

					prevDepth_Avg /= Editor.VertController.Vertices.Count;

					_strWrapper_64.Clear();
					_strWrapper_64.Append(apStringFactory.I.Min_Colon, false);
					_strWrapper_64.Append(prevDepth_Min, true);

					EditorGUILayout.LabelField(_strWrapper_64.ToString());//"Min : " + prevDepth_Min

					_strWrapper_64.Clear();
					_strWrapper_64.Append(apStringFactory.I.Max_Colon, false);
					_strWrapper_64.Append(prevDepth_Max, true);

					EditorGUILayout.LabelField(_strWrapper_64.ToString());//"Max : " + prevDepth_Max

					_strWrapper_64.Clear();
					_strWrapper_64.Append(apStringFactory.I.Average_Colon, false);
					_strWrapper_64.Append(prevDepth_Avg, true);

					EditorGUILayout.LabelField(_strWrapper_64.ToString());//"Average : " + prevDepth_Avg

					GUILayout.Space(5);

					int heightSetWeight = 25;
					int widthSetBtn = 90;
					int widthIncDecBtn = 30;
					int widthValue = width - (widthSetBtn + widthIncDecBtn * 2 + 2 * 5 + 5);

					bool isDepthChanged = false;
					float nextDepth = 0.0f;
					int calculateType = 0;


					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightSetWeight));
					GUILayout.Space(5);

					EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(widthValue), apGUILOFactory.I.Height(heightSetWeight - 2));
					GUILayout.Space(8);
					_meshEdit_zDepthWeight = EditorGUILayout.DelayedFloatField(_meshEdit_zDepthWeight);

					EditorGUILayout.EndVertical();

					//"Set Weight"
					if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.SetWeight), false, true, widthSetBtn, heightSetWeight, apStringFactory.I.SetMeshZDepthWeightTooltip))//"Specify the Z value of the vertex. The larger the value, the more in front."
					{
						isDepthChanged = true;
						nextDepth = _meshEdit_zDepthWeight;
						calculateType = 1;
						GUI.FocusControl(null);
					}

					if (apEditorUtil.ToggledButton(apStringFactory.I.Plus, false, true, widthIncDecBtn, heightSetWeight))//"+"
					{
						////0.05 단위로 올라가거나 내려온다. (5%)
						isDepthChanged = true;
						nextDepth = 0.05f;
						calculateType = 2;

						GUI.FocusControl(null);
					}
					if (apEditorUtil.ToggledButton(apStringFactory.I.Minus, false, true, widthIncDecBtn, heightSetWeight))//"-"
					{
						//0.05 단위로 올라가거나 내려온다. (5%)
						isDepthChanged = true;
						nextDepth = -0.05f;
						calculateType = 2;

						GUI.FocusControl(null);
					}
					EditorGUILayout.EndHorizontal();


					if (isDepthChanged)
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_EditVertex, Editor, Mesh, Editor.VertController.Vertex, false);

						if (calculateType == 1)
						{
							//SET : 선택된 모든 Vertex의 값을 지정한다.
							for (int i = 0; i < Editor.VertController.Vertices.Count; i++)
							{
								vert = Editor.VertController.Vertices[i];
								vert._zDepth = Mathf.Clamp01(nextDepth);
							}
						}
						else if (calculateType == 2)
						{
							//ADD : 선택된 Vertex 각각의 값을 증감한다.
							for (int i = 0; i < Editor.VertController.Vertices.Count; i++)
							{
								vert = Editor.VertController.Vertices[i];
								vert._zDepth = Mathf.Clamp01(vert._zDepth + nextDepth);
							}
						}

						//Mesh.RefreshPolygonsToIndexBuffer();
						Editor.SetRepaint();
					}

				}


			}
			else
			{
				if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Property_Modify_UI_No_Info))//"Mesh Property Modify UI No Info"
				{
					EditorGUILayout.LabelField(apStringFactory.I.NoVertexSelected);//"No vertex selected"
				}
			}

			GUILayout.Space(20);

			//"Z-Depth Rendering"
			if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.Z_DepthRendering), Editor.GetUIWord(UIWORD.Z_DepthRendering), Editor._meshEditZDepthView == apEditor.MESH_EDIT_RENDER_MODE.ZDepth, true, width, 30))
			{
				if (Editor._meshEditZDepthView == apEditor.MESH_EDIT_RENDER_MODE.Normal)
				{
					Editor._meshEditZDepthView = apEditor.MESH_EDIT_RENDER_MODE.ZDepth;
				}
				else
				{
					Editor._meshEditZDepthView = apEditor.MESH_EDIT_RENDER_MODE.Normal;
				}
			}
			GUILayout.Space(5);
			//"Make Polygons"


			//이전
			//if (GUILayout.Button(new GUIContent(Editor.GetUIWord(UIWORD.MakePolygons), Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakePolygon), "Make Polygons and Refresh Mesh"), GUILayout.Width(width), GUILayout.Height(40)))

			//변경
			if (_guiContent_MeshProperty_MakePolygones == null)
			{
				_guiContent_MeshProperty_MakePolygones = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.MakePolygons), Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakePolygon), apStringFactory.I.MakePolygonsAndRefreshMesh);//"Make Polygons and Refresh Mesh"
			}
			if (GUILayout.Button(_guiContent_MeshProperty_MakePolygones.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40)))
			{
				//Undo
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_MakeEdges, Editor, Editor.Select.Mesh, Editor.Select.Mesh, false);

				//Editor.VertController.StopEdgeWire();

				Editor.Select.Mesh.MakeEdgesToPolygonAndIndexBuffer();
				Editor.Select.Mesh.RefreshPolygonsToIndexBuffer();
				Editor.Controller.ResetAllRenderUnitsVertexIndex();//<<추가. RenderUnit에 Mesh 변경사항 반영
			}
		}

		private void MeshProperty_MakeMesh(int width, int height)
		{
			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			//추가 : 8.22
			//Auto와 TRS 기능이 추가되어서 별도의 서브탭이 필요하다.
			//변수가 추가된 것은 아니며, Enum을 묶어서 SubTab으로 처리
			bool isSubTab_AutoGen = Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.AutoGenerate;
			bool isSubTab_TRS = Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.TRS;
			bool isSubTab_Add = (!isSubTab_AutoGen && !isSubTab_TRS);//나머지

			Texture2D icon_SubTab_Add = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakeTab_Add);
			Texture2D icon_SubTab_AutoGen = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakeTab_Auto);
			Texture2D icon_SubTab_TRS = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakeTab_TRS);


			int subTab_btnWidth = (width / 3) - 4;
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(28));
			GUILayout.Space(5);
			bool isSubTabBtn_Add = apEditorUtil.ToggledButton(icon_SubTab_Add, 1, Editor.GetUIWord(UIWORD.MakeMeshTab_Add), isSubTab_Add, true, subTab_btnWidth, 28, apStringFactory.I.CreateAMeshManually);//Add//"Create a mesh manually"
			bool isSubTabBtn_TRS = apEditorUtil.ToggledButton(icon_SubTab_TRS, 1, Editor.GetUIWord(UIWORD.MakeMeshTab_Edit), isSubTab_TRS, true, subTab_btnWidth, 28, apStringFactory.I.SelectAndModifyVertices);//Edit//"Select and modify vertices"
			bool isSubTabBtn_AutoGen = apEditorUtil.ToggledButton(icon_SubTab_AutoGen, 1, Editor.GetUIWord(UIWORD.MakeMeshTab_Auto), isSubTab_AutoGen, true, subTab_btnWidth, 28, apStringFactory.I.GenerateAMeshAutomatically);//Auto//"Generate a mesh automatically"
			EditorGUILayout.EndHorizontal();



			if (isSubTabBtn_Add && !isSubTab_Add)
			{
				Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge;
				isSubTab_Add = true;
				isSubTab_TRS = false;
				isSubTab_AutoGen = false;
				//미러도 초기화
				Editor._meshEditMirrorMode = apEditor.MESH_EDIT_MIRROR_MODE.None;
				Editor.MirrorSet.Clear();
				Editor.MirrorSet.ClearMovedVertex();
			}
			if (isSubTabBtn_TRS && !isSubTab_TRS)
			{
				Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.TRS;
				isSubTab_Add = false;
				isSubTab_TRS = true;
				isSubTab_AutoGen = false;

				//기즈모 이벤트 변경
				Editor.Gizmos.Unlink();
				Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshTRS());

				//미러도 초기화
				Editor._meshEditMirrorMode = apEditor.MESH_EDIT_MIRROR_MODE.None;
				Editor.MirrorSet.Clear();
				Editor.MirrorSet.ClearMovedVertex();
			}
			if (isSubTabBtn_AutoGen && !isSubTab_AutoGen)
			{
				Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.AutoGenerate;
				isSubTab_Add = false;
				isSubTab_TRS = false;
				isSubTab_AutoGen = true;

				//기즈모 이벤트 변경
				Editor.Gizmos.Unlink();
				Editor.Gizmos.LinkObject(Editor.GizmoController.GetEventSet_MeshAutoGen());

				//미러도 초기화
				Editor._meshEditMirrorMode = apEditor.MESH_EDIT_MIRROR_MODE.None;
				Editor.MirrorSet.Clear();
				Editor.MirrorSet.ClearMovedVertex();
			}
			GUILayout.Space(10);


			if (isSubTab_Add)
			{
				//서브탭 1 : Add 모드


				//EditorGUILayout.Space();

				Texture2D icon_EditVertexWithEdge = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_VertexEdge);
				Texture2D icon_EditVertexOnly = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_VertexOnly);
				Texture2D icon_EditEdgeOnly = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_EdgeOnly);
				Texture2D icon_EditPolygon = Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Polygon);

				bool isSubEditMode_VE = (Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge);
				bool isSubEditMode_Vertex = (Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.VertexOnly);
				bool isSubEditMode_Edge = (Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.EdgeOnly);
				bool isSubEditMode_Polygon = (Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.Polygon);

				//int btnWidth = (width / 3) - 4;
				int btnWidth = (width / 4) - 4;

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(45));
				GUILayout.Space(5);
				bool nextEditMode_VE = apEditorUtil.ToggledButton(icon_EditVertexWithEdge, isSubEditMode_VE, true, btnWidth, 35, apStringFactory.I.MakeMeshTooltip_AddVertexLinkEdge);//"Add Vertex / Link Edge"
				bool nextEditMode_Vertex = apEditorUtil.ToggledButton(icon_EditVertexOnly, isSubEditMode_Vertex, true, btnWidth, 35, apStringFactory.I.MakeMeshTooltip_AddVertex);//"Add Vertex"
				bool nextEditMode_Edge = apEditorUtil.ToggledButton(icon_EditEdgeOnly, isSubEditMode_Edge, true, btnWidth, 35, apStringFactory.I.MakeMeshTooltip_LinkEdge);//"Link Edge"
				bool nextEditMode_Polygon = apEditorUtil.ToggledButton(icon_EditPolygon, isSubEditMode_Polygon, true, btnWidth, 35, apStringFactory.I.MakeMeshTooltip_SelectPolygon);//"Select Polygon"

				EditorGUILayout.EndHorizontal();

				if (nextEditMode_VE && !isSubEditMode_VE)
				{
					Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge;
					Editor.VertController.UnselectVertex();
				}

				if (nextEditMode_Vertex && !isSubEditMode_Vertex)
				{
					Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.VertexOnly;
					Editor.VertController.UnselectVertex();
				}

				if (nextEditMode_Edge && !isSubEditMode_Edge)
				{
					Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.EdgeOnly;
					Editor.VertController.UnselectVertex();
				}

				if (nextEditMode_Polygon && !isSubEditMode_Polygon)
				{
					Editor._meshEditeMode_MakeMesh = apEditor.MESH_EDIT_MODE_MAKEMESH.Polygon;
					Editor.VertController.UnselectVertex();
				}

				GUILayout.Space(5);

				Color makeMeshModeColor = Color.black;
				string strMakeMeshModeInfo = null;
				switch (Editor._meshEditeMode_MakeMesh)
				{
					case apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge:
						//strMakeMeshModeInfo = "Add Vertex / Link Edge";
						strMakeMeshModeInfo = Editor.GetUIWord(UIWORD.AddVertexLinkEdge);
						makeMeshModeColor = new Color(0.87f, 0.57f, 0.92f, 1.0f);
						break;

					case apEditor.MESH_EDIT_MODE_MAKEMESH.VertexOnly:
						//strMakeMeshModeInfo = "Add Vertex";
						strMakeMeshModeInfo = Editor.GetUIWord(UIWORD.AddVertex);
						makeMeshModeColor = new Color(0.57f, 0.82f, 0.95f, 1.0f);
						break;

					case apEditor.MESH_EDIT_MODE_MAKEMESH.EdgeOnly:
						//strMakeMeshModeInfo = "Link Edge";
						strMakeMeshModeInfo = Editor.GetUIWord(UIWORD.LinkEdge);
						makeMeshModeColor = new Color(0.95f, 0.65f, 0.65f, 1.0f);
						break;

					case apEditor.MESH_EDIT_MODE_MAKEMESH.Polygon:
						//strMakeMeshModeInfo = "Polygon";
						strMakeMeshModeInfo = Editor.GetUIWord(UIWORD.Polygon);
						makeMeshModeColor = new Color(0.65f, 0.95f, 0.65f, 1.0f);
						break;
				}
				//Polygon HotKey 이벤트 추가 -> 변경
				//이건 Layout이 안나타나면 처리가 안된다. 다른 곳으로 옮기자
				//if (Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.Polygon)
				//{
				//	Editor.AddHotKeyEvent(Editor.Controller.RemoveSelectedMeshPolygon, "Remove Polygon", KeyCode.Delete, false, false, false, null);
				//}


				//GUIStyle guiStyle_Info = new GUIStyle(GUI.skin.box);
				//guiStyle_Info.alignment = TextAnchor.MiddleCenter;
				//guiStyle_Info.normal.textColor = apEditorUtil.BoxTextColor;

				Color prevColor = GUI.backgroundColor;

				GUI.backgroundColor = makeMeshModeColor;
				GUILayout.Box((strMakeMeshModeInfo != null ? strMakeMeshModeInfo : apStringFactory.I.None), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 8), apGUILOFactory.I.Height(34));

				GUI.backgroundColor = prevColor;

				GUILayout.Space(10);
				//변경 : 설명 UI가 밑으로 들어간다.
				switch (Editor._meshEditeMode_MakeMesh)
				{
					case apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge:
						//DrawHowToControl(width, "Add or Move Vertex with Edges", "Move View", "Remove Vertex or Edge", null, "Snap to Vertex", "L:Cut Edge / R:Delete Vertex");
						DrawHowToControl(width,
										Editor.GetUIWord(UIWORD.AddOrMoveVertexWithEdges),
										Editor.GetUIWord(UIWORD.MoveView),
										Editor.GetUIWord(UIWORD.RemoveVertexorEdge),
										null,
										Editor.GetUIWord(UIWORD.SnapToVertex),
										Editor.GetUIWord(UIWORD.LCutEdge_RDeleteVertex));
						break;

					case apEditor.MESH_EDIT_MODE_MAKEMESH.VertexOnly:
						//DrawHowToControl(width, "Add or Move Vertex", "Move View", "Remove Vertex");
						DrawHowToControl(width,
										Editor.GetUIWord(UIWORD.AddOrMoveVertex),
										Editor.GetUIWord(UIWORD.MoveView),
										Editor.GetUIWord(UIWORD.RemoveVertex));
						break;

					case apEditor.MESH_EDIT_MODE_MAKEMESH.EdgeOnly:
						//DrawHowToControl(width, "Link Vertices / Turn Edge", "Move View", "Remove Edge", null, "Snap to Vertex", "Cut Edge");
						DrawHowToControl(width,
										Editor.GetUIWord(UIWORD.LinkVertices_TurnEdge),
										Editor.GetUIWord(UIWORD.MoveView),
										Editor.GetUIWord(UIWORD.RemoveEdge),
										null,
										Editor.GetUIWord(UIWORD.SnapToVertex),
										Editor.GetUIWord(UIWORD.CutEdge));
						break;

					case apEditor.MESH_EDIT_MODE_MAKEMESH.Polygon:
						//DrawHowToControl(width, "Select Polygon", "Move View", null, "Remove Polygon");
						DrawHowToControl(width,
										Editor.GetUIWord(UIWORD.SelectPolygon),
										Editor.GetUIWord(UIWORD.MoveView),
										null,
										Editor.GetUIWord(UIWORD.RemovePolygon));
						break;
				}


				DrawMakePolygonsTool(width);


				if (Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.VertexAndEdge
					|| Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.EdgeOnly
					|| Editor._meshEditeMode_MakeMesh == apEditor.MESH_EDIT_MODE_MAKEMESH.VertexOnly)
				{
					//추가 9.12 : Vertex+Edge / Vertex 추가 모드에서는 미러 툴이 나타난다.
					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width);
					GUILayout.Space(10);

					DrawMakeMeshMirrorTool(width, false, true);
				}
			}
			else if (isSubTab_TRS)
			{
				//서브탭 2 : TRS 모드
				//도구들
				//- 미러 복사
				//- 합치기
				//- 정렬


				DrawMakePolygonsTool(width);

				GUILayout.Space(10);
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(10);

				DrawMakeMeshMirrorTool(width, true, false);



				GUILayout.Space(10);
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(10);

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AlignTools));//"Align Tool"
				GUILayout.Space(5);
				//- X/Y 합치기 (3개 * 2)
				//- X/Y 고르게 정렬 (2개)
				int width_AlignBtn4 = ((width - 10) / 4) - 1;
				int height_AlignBtn = 28;

				//X Align + X Distribute
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_AlignBtn));
				GUILayout.Space(5);
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Align_XLeft), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.MinX);
				}
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Align_XCenter), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.CenterX);
				}
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Align_XRight), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.MaxX);
				}
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Distribute_X), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.DistributeX);
				}
				EditorGUILayout.EndHorizontal();

				//Y Align
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_AlignBtn));
				GUILayout.Space(5);
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Align_YUp), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.MaxY);
				}
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Align_YCenter), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.CenterY);
				}
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Align_YDown), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.MinY);
				}
				if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Distribute_Y), apGUILOFactory.I.Width(width_AlignBtn4), apGUILOFactory.I.Height(height_AlignBtn)))
				{
					Editor.Controller.AlignVertices(apEditorController.VERTEX_ALIGN_REQUEST.DistributeY);
				}
				EditorGUILayout.EndHorizontal();


				//Hot Key를 등록하자
				//Delete 버튼을 누르면 버텍스 삭제
				Editor.AddHotKeyEvent(OnHotKeyEvent_RemoveVertexOnTRS, apHotKey.LabelText.RemoveVertices, KeyCode.Delete, false, false, false, null);//"Remove Vertices"
				Editor.AddHotKeyEvent(OnHotKeyEvent_RemoveVertexOnTRS, apHotKey.LabelText.RemoveVertices, KeyCode.Delete, true, false, false, null);//"Remove Vertices"

			}
			else
			{
				//서브탭 3 : 자동 생성 모드
				//3개의 그룹으로 나뉜다.
				//- Atlas 설정
				//- Scan
				//- Preview / Make
				//이전
				//GUIContent guiContent_StepCompleted = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_StepCompleted));
				//GUIContent guiContent_StepUncompleted = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_StepUncompleted));
				//GUIContent guiContent_StepUnUsed = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_StepUnused));

				//변경
				if (_guiContent_StepCompleted == null)
				{
					_guiContent_StepCompleted = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_StepCompleted));
				}
				if (_guiContent_StepUncompleted == null)
				{
					_guiContent_StepUncompleted = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_StepUncompleted));
				}
				if (_guiContent_StepUnUsed == null)
				{
					_guiContent_StepUnUsed = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_StepUnused));
				}

				//GUIStyle guiStyle_StepIcon = new GUIStyle(GUI.skin.label);
				//guiStyle_StepIcon.alignment = TextAnchor.MiddleCenter;

				int width_StepIcon = 25;
				int width_StepBtn = width - (width_StepIcon + 5);

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AreaSizeSettings));//"Area Size within the Atlas"
				GUILayout.Space(5);

				//isPSDParsed 옵션 여부
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(5);
				EditorGUILayout.LabelField(Mesh._isPSDParsed ? _guiContent_StepCompleted.Content : _guiContent_StepUncompleted.Content, apGUIStyleWrapper.I.Label_MiddleCenter, apGUILOFactory.I.Width(width_StepIcon), apGUILOFactory.I.Height(25));

				if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.AreaOptionEnabled), Editor.GetUIWord(UIWORD.AreaOptionDisabled), Mesh._isPSDParsed, true, width_StepBtn, 25))//"Area Option Enabled", "Area Option Disabled"
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._isPSDParsed = !Mesh._isPSDParsed;
				}
				EditorGUILayout.EndHorizontal();

				//int width_Label = 150;
				//int width_Value = (width - (10 + width_Label));
				int width_AreaValue = 40;
				int width_AreaChangeBtn = 24;
				int width_AreaMargin_1Btn = 5 + (width - (width_AreaValue + 4 + 10)) / 2;
				int width_AreaMargin_2Btn = width - ((width_AreaValue + width_AreaChangeBtn * 2 + 4) * 2 + 10);

				//GUIStyle guiStyle_AreaResizeBtn = new GUIStyle(GUI.skin.button);
				//guiStyle_AreaResizeBtn.margin = GUI.skin.textField.margin;

				//이전
				//GUIContent guiContent_imgValueUp = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Up));
				//GUIContent guiContent_imgValueDown = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Down));
				//GUIContent guiContent_imgValueLeft = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Left));
				//GUIContent guiContent_imgValueRight = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Right));

				//변경 19.11.20
				if (_guiContent_imgValueUp == null)
				{
					_guiContent_imgValueUp = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Up));
				}
				if (_guiContent_imgValueDown == null)
				{
					_guiContent_imgValueDown = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Down));
				}
				if (_guiContent_imgValueLeft == null)
				{
					_guiContent_imgValueLeft = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Left));
				}
				if (_guiContent_imgValueRight == null)
				{
					_guiContent_imgValueRight = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Right));
				}


				int width_Label = 100;
				int width_Value = width - (5 + width_Label);

				if (Mesh._isPSDParsed)
				{
					//이게 활성화 될 때에만 가능하다
					GUILayout.Space(10);

					//영역을 설정
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AreaSize));//"Area Size"

					//Top
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
					GUILayout.Space(width_AreaMargin_1Btn);

					float meshAtlas_Top = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_LT.y, apGUILOFactory.I.Width(width_AreaValue));
					if (GUILayout.Button(_guiContent_imgValueUp.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						//5씩 움직인다.
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_LT.y += 5;
						meshAtlas_Top = Mesh._atlasFromPSD_LT.y;
						apEditorUtil.ReleaseGUIFocus();
					}
					if (GUILayout.Button(_guiContent_imgValueDown.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_LT.y -= 5;
						meshAtlas_Top = Mesh._atlasFromPSD_LT.y;
						apEditorUtil.ReleaseGUIFocus();
					}
					EditorGUILayout.EndHorizontal();
					GUILayout.Space(5);

					//Left / Right
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
					GUILayout.Space(5);

					float meshAtlas_Left = Mesh._atlasFromPSD_LT.x;
					if (GUILayout.Button(_guiContent_imgValueLeft.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_LT.x -= 5;
						meshAtlas_Left = Mesh._atlasFromPSD_LT.x;
						apEditorUtil.ReleaseGUIFocus();
					}
					if (GUILayout.Button(_guiContent_imgValueRight.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_LT.x += 5;
						meshAtlas_Left = Mesh._atlasFromPSD_LT.x;
						apEditorUtil.ReleaseGUIFocus();
					}
					meshAtlas_Left = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_LT.x, apGUILOFactory.I.Width(width_AreaValue));

					GUILayout.Space(width_AreaMargin_2Btn);
					float meshAtlas_Right = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_RB.x, apGUILOFactory.I.Width(width_AreaValue));
					if (GUILayout.Button(_guiContent_imgValueLeft.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_RB.x -= 5;
						meshAtlas_Right = Mesh._atlasFromPSD_RB.x;
						apEditorUtil.ReleaseGUIFocus();
					}
					if (GUILayout.Button(_guiContent_imgValueRight.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_RB.x += 5;
						meshAtlas_Right = Mesh._atlasFromPSD_RB.x;
						apEditorUtil.ReleaseGUIFocus();
					}
					EditorGUILayout.EndHorizontal();
					GUILayout.Space(5);

					//Bottom
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
					GUILayout.Space(width_AreaMargin_1Btn);
					float meshAtlas_Bottom = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_RB.y, apGUILOFactory.I.Width(width_AreaValue));
					if (GUILayout.Button(_guiContent_imgValueDown.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_RB.y -= 5;
						meshAtlas_Bottom = Mesh._atlasFromPSD_RB.y;
						apEditorUtil.ReleaseGUIFocus();
					}
					if (GUILayout.Button(_guiContent_imgValueUp.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_RB.y += 5;
						meshAtlas_Bottom = Mesh._atlasFromPSD_RB.y;
						apEditorUtil.ReleaseGUIFocus();
					}
					EditorGUILayout.EndHorizontal();




					//Atlas의 범위 값이 바뀌었을 때
					if (meshAtlas_Top != Mesh._atlasFromPSD_LT.y
						|| meshAtlas_Left != Mesh._atlasFromPSD_LT.x
						|| meshAtlas_Right != Mesh._atlasFromPSD_RB.x
						|| meshAtlas_Bottom != Mesh._atlasFromPSD_RB.y)
					{
						apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
						Mesh._atlasFromPSD_LT.y = meshAtlas_Top;
						Mesh._atlasFromPSD_LT.x = meshAtlas_Left;
						Mesh._atlasFromPSD_RB.x = meshAtlas_Right;
						Mesh._atlasFromPSD_RB.y = meshAtlas_Bottom;
						apEditorUtil.ReleaseGUIFocus();
					}

					GUILayout.Space(10);

					Editor.MeshGenerator.CheckAndSetMesh(Mesh);

					bool isScannable = Editor.MeshGenerator.IsScanable();
					bool isScanned = Editor.MeshGenerator.IsScanned;
					bool isPreviewed = Editor.MeshGenerator.IsPreviewed;
					bool isCompleted = Editor.MeshGenerator.IsCompleted;

					//이미지의 Read/Write가 켜져 있는지 체크
					//Scan 전에 Image Read/Write Enabled가 켜져 있어야 한다.
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
					GUILayout.Space(5);
					bool isTextureReadWriteEnabled = Editor.MeshGenerator.IsTextureReadWriteEnabled();
					bool isValidTexture = Editor.MeshGenerator.IsValidTexture();
					//Read&Write => Completed 단계 전에는 Completed, Completed 단계에서는 UnUsed
					EditorGUILayout.LabelField(isTextureReadWriteEnabled && isValidTexture ? (Editor.MeshGenerator.IsCompleted ? _guiContent_StepUnUsed.Content : _guiContent_StepCompleted.Content) : _guiContent_StepUncompleted.Content,
												apGUIStyleWrapper.I.Label_MiddleCenter,
												apGUILOFactory.I.Width(width_StepIcon), apGUILOFactory.I.Height(25));


					if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.TextureRWEnabled), Editor.GetUIWord(UIWORD.TextureRWDisabled), isTextureReadWriteEnabled, isValidTexture, width_StepBtn, 25))//"Texture Read/Write Enabled", "Texture Read/Write Disabled"
					{
						Editor.MeshGenerator.SetTextureReadWriteEnableToggle();
					}
					EditorGUILayout.EndHorizontal();


					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width);
					GUILayout.Space(10);


					//Scan 전에 불가능한 상태라면 경고
					bool isScanWarning = false;
					string scanWarning = null;
					if (!Editor.MeshGenerator.IsTextureReadWriteEnabled())
					{
						//scanWarning = "Read/Write Property is Disabled";
						scanWarning = Editor.GetUIWord(UIWORD.WarningTextureRWDisabled);
						isScanWarning = true;
					}
					else if (!Editor.MeshGenerator.IsValidAtlasArea())
					{
						//scanWarning = "Area is too Small";
						scanWarning = Editor.GetUIWord(UIWORD.WarningAreaSmall);
						isScanWarning = true;
					}
					if (isScanWarning)
					{
						Color prevColor = GUI.backgroundColor;

						GUI.backgroundColor = new Color(1.0f, 0.6f, 0.5f, 1.0f);
						GUILayout.Box(scanWarning, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(32));

						GUI.backgroundColor = prevColor;

					}
					else
					{
						//GUILayout.Space(10);
						//Scan 옵션
						//- Alpha CutOff
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
						GUILayout.Space(5);
						EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AlphaCutout), apGUILOFactory.I.Width(width_Label));//"Alpha Cutout"
						float alphaCutOff = EditorGUILayout.DelayedFloatField(Editor._meshAutoGenOption_AlphaCutOff, apGUILOFactory.I.Width(width_Value));
						if (Mathf.Abs(alphaCutOff - Editor._meshAutoGenOption_AlphaCutOff) > 0.0001f)
						{
							Editor._meshAutoGenOption_AlphaCutOff = Mathf.Clamp(alphaCutOff, 0.0f, 1.0f);
							apEditorUtil.ReleaseGUIFocus();
							Editor.SaveEditorPref();
						}
						EditorGUILayout.EndHorizontal();

					}

					GUILayout.Space(10);


					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.WrapperShapeOptions));//"Mapping Wrapper Shape Options"
					GUILayout.Space(5);


					//- Shape 타입
					//- Reset Shape
					//- 타입별 ControlPoint
					//- Margin, Grid Size
					int width_4Btn = ((width - 5) / 4) - 2;
					int height_ShapeBtn = 34;
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_ShapeBtn));
					GUILayout.Space(5);

					if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Map_Quad), Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.Quad, isScanned && isScannable, width_4Btn, height_ShapeBtn))
					{
						//Quad 타입으로 변경 : TODO 기즈모 갱신
						Editor.MeshGenerator.Mapper._shape = apMeshGenMapper.MapperShape.Quad;
						Editor.MeshGenerator._selectedControlPoints.Clear();
					}
					if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Map_TFQuad), Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.ComplexQuad, isScanned && isScannable, width_4Btn, height_ShapeBtn))
					{
						//ComplexQuad 타입으로 변경 : TODO 기즈모 갱신/없으면 새로 생성
						Editor.MeshGenerator.Mapper._shape = apMeshGenMapper.MapperShape.ComplexQuad;
						Editor.MeshGenerator._selectedControlPoints.Clear();
					}
					if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Map_Radial), Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.Circle, isScanned && isScannable, width_4Btn, height_ShapeBtn))
					{
						//Circle 타입으로 변경 : TODO 기즈모 갱신/없으면 새로 생성
						Editor.MeshGenerator.Mapper._shape = apMeshGenMapper.MapperShape.Circle;
						Editor.MeshGenerator._selectedControlPoints.Clear();
					}
					if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_Map_Ring), Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.Ring, isScanned && isScannable, width_4Btn, height_ShapeBtn))
					{
						//Ring 타입으로 변경 : TODO 기즈모 갱신/없으면 새로 생성
						Editor.MeshGenerator.Mapper._shape = apMeshGenMapper.MapperShape.Ring;
						Editor.MeshGenerator._selectedControlPoints.Clear();
					}
					EditorGUILayout.EndHorizontal();
					if (Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.ComplexQuad)
					{
						GUILayout.Space(5);

						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
						GUILayout.Space(5);

						if (_guiContent_MakeMesh_PointCount_X == null)
						{
							_guiContent_MakeMesh_PointCount_X = new apGUIContentWrapper();
							_guiContent_MakeMesh_PointCount_X.AppendText(Editor.GetUIWord(UIWORD.PointCount), false);
							_guiContent_MakeMesh_PointCount_X.AppendSpaceText(1, false);
							_guiContent_MakeMesh_PointCount_X.AppendText(apStringFactory.I.X, true);
						}

						if (_guiContent_MakeMesh_PointCount_Y == null)
						{
							_guiContent_MakeMesh_PointCount_Y = new apGUIContentWrapper();
							_guiContent_MakeMesh_PointCount_Y.AppendText(Editor.GetUIWord(UIWORD.PointCount), false);
							_guiContent_MakeMesh_PointCount_Y.AppendSpaceText(1, false);
							_guiContent_MakeMesh_PointCount_Y.AppendText(apStringFactory.I.Y, true);
						}


						EditorGUILayout.LabelField(_guiContent_MakeMesh_PointCount_X.Content, apGUILOFactory.I.Width(width_Label));//"Point Count X"
						int numPointX = EditorGUILayout.DelayedIntField(Editor._meshAutoGenOption_numControlPoint_ComplexQuad_X, apGUILOFactory.I.Width(width_Value));
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
						GUILayout.Space(5);
						EditorGUILayout.LabelField(_guiContent_MakeMesh_PointCount_Y.Content, apGUILOFactory.I.Width(width_Label));//"Point Count Y"
						int numPointY = EditorGUILayout.DelayedIntField(Editor._meshAutoGenOption_numControlPoint_ComplexQuad_Y, apGUILOFactory.I.Width(width_Value));
						EditorGUILayout.EndHorizontal();

						if (numPointX != Editor._meshAutoGenOption_numControlPoint_ComplexQuad_X
							|| numPointY != Editor._meshAutoGenOption_numControlPoint_ComplexQuad_Y)
						{
							if (numPointX < 3)
							{
								numPointX = 3;
							}
							if (numPointY < 3)
							{
								numPointY = 3;
							}
							Editor._meshAutoGenOption_numControlPoint_ComplexQuad_X = numPointX;
							Editor._meshAutoGenOption_numControlPoint_ComplexQuad_Y = numPointY;
							apEditorUtil.ReleaseGUIFocus();
							Editor.SaveEditorPref();
						}
					}
					else if (Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.Circle ||
						Editor.MeshGenerator.Mapper._shape == apMeshGenMapper.MapperShape.Ring)
					{
						GUILayout.Space(5);

						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
						GUILayout.Space(5);
						EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.PointCount), apGUILOFactory.I.Width(width_Label));//"Point Count"
						int numPoint = EditorGUILayout.DelayedIntField(Editor._meshAutoGenOption_numControlPoint_CircleRing, apGUILOFactory.I.Width(width_Value));
						EditorGUILayout.EndHorizontal();

						if (numPoint != Editor._meshAutoGenOption_numControlPoint_CircleRing)
						{
							if (numPoint < 4)
							{
								numPoint = 4;
							}
							Editor._meshAutoGenOption_numControlPoint_CircleRing = numPoint;
							apEditorUtil.ReleaseGUIFocus();
							Editor.SaveEditorPref();
						}
					}

					GUILayout.Space(10);

					//Step - Scan
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					GUILayout.Space(5);
					EditorGUILayout.LabelField(isScanned && isScannable ? _guiContent_StepCompleted.Content : _guiContent_StepUncompleted.Content,
												apGUIStyleWrapper.I.Label_MiddleCenter, apGUILOFactory.I.Width(width_StepIcon), apGUILOFactory.I.Height(30));

					//string strScanImage = "  " + Editor.GetUIWord(UIWORD.ScanImage);

					if (_strWrapper_64 == null)
					{
						_strWrapper_64 = new apStringWrapper(64);
					}
					_strWrapper_64.Clear();
					_strWrapper_64.AppendSpace(2, false);
					_strWrapper_64.Append(Editor.GetUIWord(UIWORD.ScanImage), true);

					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_AutoGen_Scan), _strWrapper_64.ToString(), _strWrapper_64.ToString(), false, isScannable, width_StepBtn, 30))//"  Scan Image"
					{
						//Scan 버튼
						Editor.MeshGenerator.Step1_Scan();
					}
					EditorGUILayout.EndHorizontal();

					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width);
					GUILayout.Space(10);

					//Outline(Outer Point) Group을 선택하고 삭제할 수 있다.
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.OutlineGroups));//"Outline Groups"
					GUILayout.Space(5);
					int curGroupIndex = 0;
					int curGroupCount = 0;

					if (Editor.MeshGenerator._selectedOuterGroup == null && Editor.MeshGenerator._outlineGroups.Count > 0)
					{
						Editor.MeshGenerator._selectedOuterGroupIndex = 0;
						Editor.MeshGenerator._selectedOuterGroup = Editor.MeshGenerator._outlineGroups[0];
					}
					if (Editor.MeshGenerator._outlineGroups.Count > 0)
					{
						curGroupIndex = Editor.MeshGenerator._selectedOuterGroupIndex + 1;
						curGroupCount = Editor.MeshGenerator._outlineGroups.Count;
					}

					int width_GroupPrevNext = (width - (10 + 100)) / 2;
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
					GUILayout.Space(5);
					if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_MoveToPrevFrame), false, (Editor.MeshGenerator._outlineGroups.Count > 0), width_GroupPrevNext, 20))
					{
						if (Editor.MeshGenerator._outlineGroups.Count > 0)
						{
							Editor.MeshGenerator._selectedOuterGroupIndex--;
							if (Editor.MeshGenerator._selectedOuterGroupIndex < 0)
							{
								Editor.MeshGenerator._selectedOuterGroupIndex = Editor.MeshGenerator._outlineGroups.Count - 1;
							}
							Editor.MeshGenerator._selectedOuterGroup = Editor.MeshGenerator._outlineGroups[Editor.MeshGenerator._selectedOuterGroupIndex];
						}
					}

					_strWrapper_64.Clear();
					_strWrapper_64.Append(curGroupIndex, false);
					_strWrapper_64.Append(apStringFactory.I.Slash_Space, false);
					_strWrapper_64.Append(curGroupCount, true);


					GUILayout.Button(_strWrapper_64.ToString(), apGUILOFactory.I.Width(101), apGUILOFactory.I.Height(20));
					if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_MoveToNextFrame), false, (Editor.MeshGenerator._outlineGroups.Count > 0), width_GroupPrevNext, 20))
					{
						if (Editor.MeshGenerator._outlineGroups.Count > 0)
						{
							Editor.MeshGenerator._selectedOuterGroupIndex++;
							if (Editor.MeshGenerator._selectedOuterGroupIndex >= Editor.MeshGenerator._outlineGroups.Count)
							{
								Editor.MeshGenerator._selectedOuterGroupIndex = 0;
							}
							Editor.MeshGenerator._selectedOuterGroup = Editor.MeshGenerator._outlineGroups[Editor.MeshGenerator._selectedOuterGroupIndex];
						}
					}

					EditorGUILayout.EndHorizontal();
					bool isEnabledGroup = false;
					if (Editor.MeshGenerator._selectedOuterGroup != null)
					{
						isEnabledGroup = Editor.MeshGenerator._selectedOuterGroup._isEnabled;
					}
					//"Enabled" , "Disabled"
					if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.OutlineEnabled), Editor.GetUIWord(UIWORD.OutlineDisabled), isEnabledGroup, Editor.MeshGenerator._selectedOuterGroup != null, width, 25))
					{
						if (Editor.MeshGenerator._selectedOuterGroup != null)
						{
							Editor.MeshGenerator._selectedOuterGroup._isEnabled = !Editor.MeshGenerator._selectedOuterGroup._isEnabled;
						}
					}
					//"Resize Area to Selected Group"
					if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.ResizeAreaToSelectedGroup), false, Editor.MeshGenerator._selectedOuterGroup != null && isEnabledGroup, width, 20))
					{
						//TODO : Resize (물어본다)

						if (Editor.MeshGenerator._selectedOuterGroup != null)
						{
							//"Resize Area", "Do you want to resize the area to the currently selected group?", "Okay", "Cancel"
							bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.DLG_ResizeArea_Title),
																		Editor.GetText(TEXT.DLG_ResizeArea_Body),
																		Editor.GetText(TEXT.Okay),
																		Editor.GetText(TEXT.Cancel));
							if (isResult)
							{
								Vector4 ltrb = Editor.MeshGenerator._selectedOuterGroup.GetLTRB();
								Vector2 LT = Editor.MeshGenerator.ConvertWorld2Tex(new Vector2(ltrb.x, ltrb.y));
								Vector2 RB = Editor.MeshGenerator.ConvertWorld2Tex(new Vector2(ltrb.z, ltrb.w));
								//Debug.Log("Current LTRB : " + LT + " ~ " + RB);
								apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
								_mesh._atlasFromPSD_LT.x = Mathf.Min(LT.x, RB.x);
								_mesh._atlasFromPSD_RB.x = Mathf.Max(LT.x, RB.x);
								_mesh._atlasFromPSD_LT.y = Mathf.Max(LT.y, RB.y);
								_mesh._atlasFromPSD_RB.y = Mathf.Min(LT.y, RB.y);

								//다시 스캔할지 물어봄
								if (isScannable)
								{
									//"Re-Scan", "Would you like to Re-Scan for the resized area?", "Okay", "Cancel"
									bool isRescan = EditorUtility.DisplayDialog(Editor.GetText(TEXT.DLG_Rescan_Title),
																				Editor.GetText(TEXT.DLG_Rescan_Body),
																				Editor.GetText(TEXT.Okay),
																				Editor.GetText(TEXT.Cancel));
									if (isRescan)
									{
										//다시 스캔하자
										Editor.MeshGenerator.Step1_Scan();
									}
								}
							}
						}
					}


					//Preview + Generate 설정들
					GUILayout.Space(15);



					GUILayout.Space(10);
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
					GUILayout.Space(5);
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Division), apGUILOFactory.I.Width(width_Label));//"Division"
					int gridDivide = EditorGUILayout.DelayedIntField(Editor._meshAutoGenOption_GridDivide, apGUILOFactory.I.Width(width_Value));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
					GUILayout.Space(5);
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SpriteMargin), apGUILOFactory.I.Width(width_Label));//"Margin"
					int margin = EditorGUILayout.DelayedIntField(Editor._meshAutoGenOption_Margin, apGUILOFactory.I.Width(width_Value));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
					GUILayout.Space(5);
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.LockAxis), apGUILOFactory.I.Width(width_Label));//"Lock Axis"
					bool isLockAxis = EditorGUILayout.Toggle(Editor._meshAutoGenOption_IsLockAxis, apGUILOFactory.I.Width(width_Value));
					EditorGUILayout.EndHorizontal();


					if (gridDivide != Editor._meshAutoGenOption_GridDivide ||
						margin != Editor._meshAutoGenOption_Margin ||
						isLockAxis != Editor._meshAutoGenOption_IsLockAxis
						)
					{
						Editor._meshAutoGenOption_GridDivide = Mathf.Clamp(gridDivide, 1, 10);
						Editor._meshAutoGenOption_Margin = Mathf.Max(margin, 0);
						Editor._meshAutoGenOption_IsLockAxis = isLockAxis;
						apEditorUtil.ReleaseGUIFocus();
						Editor.SaveEditorPref();
					}


					GUILayout.Space(10);

					//Step - Preview
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					GUILayout.Space(5);
					EditorGUILayout.LabelField(isPreviewed && isScannable ? _guiContent_StepCompleted.Content : _guiContent_StepUncompleted.Content,
												apGUIStyleWrapper.I.Label_MiddleCenter, apGUILOFactory.I.Width(width_StepIcon), apGUILOFactory.I.Height(30));

					//"  Preview Mesh"
					if (_guiContent_MakeMesh_AutoGenPreview == null)
					{
						_guiContent_MakeMesh_AutoGenPreview = new apGUIContentWrapper();
						_guiContent_MakeMesh_AutoGenPreview.AppendSpaceText(2, false);
						_guiContent_MakeMesh_AutoGenPreview.AppendText(Editor.GetUIWord(UIWORD.PrevewMesh), true);
					}

					//string strPreviewMesh = "  " + Editor.GetUIWord(UIWORD.PrevewMesh);
					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_AutoGen_Preview), _guiContent_MakeMesh_AutoGenPreview.Content.text, _guiContent_MakeMesh_AutoGenPreview.Content.text, false, isScanned && isScannable, width_StepBtn, 30))
					{
						//Prevew 버튼
						Editor.MeshGenerator.Step2_Preview();
					}
					EditorGUILayout.EndHorizontal();



					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width);
					GUILayout.Space(10);

					//Relax 버튼
					if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Relax), false, isPreviewed && isScannable, width, 30))//"Relax"
					{
						//Generate 버튼
						Editor.MeshGenerator.RelaxInnerPoints(10);
					}

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					GUILayout.Space(5);
					EditorGUILayout.LabelField(isCompleted && isScannable ? _guiContent_StepCompleted.Content : _guiContent_StepUncompleted.Content,
												apGUIStyleWrapper.I.Label_MiddleCenter, apGUILOFactory.I.Width(width_StepIcon), apGUILOFactory.I.Height(30));

					//"  Generate Mesh"
					//string strGenMesh = "  " + Editor.GetUIWord(UIWORD.GenerateMesh);
					if (_guiContent_MakeMesh_GenerateMesh == null)
					{
						_guiContent_MakeMesh_GenerateMesh = new apGUIContentWrapper();
						_guiContent_MakeMesh_GenerateMesh.AppendSpaceText(2, false);
						_guiContent_MakeMesh_GenerateMesh.AppendText(Editor.GetUIWord(UIWORD.GenerateMesh), true);
					}


					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_AutoGen_Complete), _guiContent_MakeMesh_GenerateMesh.Content.text, _guiContent_MakeMesh_GenerateMesh.Content.text, false, isPreviewed && isScannable, width_StepBtn, 30))
					{
						//Generate 버튼
						if (Editor.Select.Mesh._vertexData.Count > 0)
						{
							//TODO : 버텍스를 모두 삭제할지 여부 결정
							//"Replace or Append vertices", "Do you want to replace existing vertices when creating the mesh?", "Replace", "Append", "Cancel"
							int iRemoveType = EditorUtility.DisplayDialogComplex(Editor.GetText(TEXT.DLG_ReplaceAppendVertices_Title),
																					Editor.GetText(TEXT.DLG_ReplaceAppendVertices_Body),
																					Editor.GetText(TEXT.DLG_Replace),
																					Editor.GetText(TEXT.DLG_Append),
																					Editor.GetText(TEXT.Cancel));
							if (iRemoveType < 2)
							{
								Editor.MeshGenerator.Step3_Generate(iRemoveType == 0);
							}
						}
						else
						{
							//버텍스가 없다. 그냥 실행
							Editor.MeshGenerator.Step3_Generate(false);
						}


					}
					EditorGUILayout.EndHorizontal();

					//EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(18));
					//GUILayout.Space(5);
					//EditorGUILayout.LabelField("Left", GUILayout.Width(width_Label));
					//float leftValue = EditorGUILayout.FloatField(Mesh._atlasFromPSD_LT.x, GUILayout.Width(width_Value));
					//EditorGUILayout.EndHorizontal();
				}
				else
				{
					GUILayout.Space(10);
					//GUIStyle guiStyle_Info = new GUIStyle(GUI.skin.box);
					//guiStyle_Info.alignment = TextAnchor.MiddleCenter;
					//guiStyle_Info.normal.textColor = apEditorUtil.BoxTextColor;

					Color prevColor = GUI.backgroundColor;

					GUI.backgroundColor = new Color(1.0f, 0.6f, 0.5f, 1.0f);
					GUILayout.Box(Editor.GetUIWord(UIWORD.WarningAreaDisabled), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(32));//"Area Option is Disabled"

					GUI.backgroundColor = prevColor;
				}
			}



			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			//"Remove All Vertices"
			//이전
			//if (GUILayout.Button(new GUIContent(Editor.GetUIWord(UIWORD.RemoveAllVertices), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform), "Remove all Vertices and Polygons"), GUILayout.Height(24)))

			//변경
			if (_guiContent_MeshProperty_RemoveAllVertices == null)
			{
				_guiContent_MeshProperty_RemoveAllVertices = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.RemoveAllVertices), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform), apStringFactory.I.RemoveAllVerticesAndPolygons);//"Remove all Vertices and Polygons"
			}

			if (GUILayout.Button(_guiContent_MeshProperty_RemoveAllVertices.Content, apGUILOFactory.I.Height(24)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove All Vertices", "Do you want to remove All vertices? (Not Undo)", "Remove All", "Cancel");

				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveMeshVertices_Title),
																Editor.GetText(TEXT.RemoveMeshVertices_Body),
																Editor.GetText(TEXT.RemoveMeshVertices_Okay),
																Editor.GetText(TEXT.Cancel));

				if (isResult)
				{
					//Undo
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_RemoveAllVertices, Editor, _mesh, null, false);

					_mesh._vertexData.Clear();
					_mesh._indexBuffer.Clear();
					_mesh._edges.Clear();
					_mesh._polygons.Clear();

					_mesh.MakeEdgesToPolygonAndIndexBuffer();

					Editor.Controller.ResetAllRenderUnitsVertexIndex();//<<추가. RenderUnit에 Mesh 변경사항 반영

					Editor.VertController.UnselectVertex();
					Editor.VertController.UnselectNextVertex();
				}
			}

		}


		private void DrawMakePolygonsTool(int width)
		{
			// Make Mesh + Auto Link

			if (_guiContent_MeshProperty_AutoLinkEdge == null)
			{
				_guiContent_MeshProperty_AutoLinkEdge = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AutoLinkEdge), Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_AutoLink));
			}
			if (_guiContent_MeshProperty_Draw_MakePolygones == null)
			{
				_guiContent_MeshProperty_Draw_MakePolygones = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.MakePolygons), Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakePolygon), apStringFactory.I.MakePolygonsAndRefreshMesh);//"Make Polygons and Refresh Mesh"
			}


			//"Auto Link Edge"
			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AutoLinkEdge), Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_AutoLink), "Automatically creates edges connecting vertices"), GUILayout.Height(30)))
			//변경
			if (GUILayout.Button(_guiContent_MeshProperty_AutoLinkEdge.Content, apGUILOFactory.I.Height(30)))
			{
				//Undo
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_MakeEdges, Editor, Editor.Select.Mesh, Editor.Select.Mesh, false);

				//Editor.VertController.StopEdgeWire();
				Editor.Select.Mesh.AutoLinkEdges();
			}
			GUILayout.Space(10);
			//"Make Polygons"
			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.MakePolygons), Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MakePolygon), "Make Polygons and Refresh Mesh"), GUILayout.Height(50)))

			//변경
			if (GUILayout.Button(_guiContent_MeshProperty_Draw_MakePolygones.Content, apGUILOFactory.I.Height(50)))
			{
				//Undo
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_MakeEdges, Editor, Editor.Select.Mesh, Editor.Select.Mesh, false);

				//Editor.VertController.StopEdgeWire();

				Editor.Select.Mesh.MakeEdgesToPolygonAndIndexBuffer();
				Editor.Select.Mesh.RefreshPolygonsToIndexBuffer();
				Editor.Controller.ResetAllRenderUnitsVertexIndex();//<<추가. RenderUnit에 Mesh 변경사항 반영
			}
		}


		private void DrawMakeMeshMirrorTool(int width, bool isUseCopyTool, bool isUseSnapTool)
		{
			//- 미러 복사

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.MirrorTool));//"Mirror Tool"
			GUILayout.Space(5);
			//X/Y 툴 켜기/끄기 (+ 위치)
			bool isMirrorEnabled = (Editor._meshEditMirrorMode == apEditor.MESH_EDIT_MIRROR_MODE.Mirror);
			bool isMirrorX = Mesh._isMirrorX;

			int nVertices = Editor.VertController.Vertices != null ? Editor.VertController.Vertices.Count : 0;


			//미러 모드 켜고 끄기
			if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.MirrorEnabled), Editor.GetUIWord(UIWORD.MirrorDisabled), isMirrorEnabled, true, width, 30))//"Mirror Enabled" / "Mirror Disabled"
			{
				isMirrorEnabled = !isMirrorEnabled;
				Editor._meshEditMirrorMode = isMirrorEnabled ? apEditor.MESH_EDIT_MIRROR_MODE.Mirror : apEditor.MESH_EDIT_MIRROR_MODE.None;
				apEditorUtil.ReleaseGUIFocus();
			}


			//int width_AxisBtn = 60;
			int width_AxisBtn = (width - (5)) / 2;
			//int width_CopyBtn = width - (10 + width_AxisBtn * 2 + 7);
			int height_Axis = 30;
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_Axis));
			GUILayout.Space(5);

			//GUIStyle guiStyle_AxisValue = new GUIStyle(GUI.skin.textField);
			//guiStyle_AxisValue.margin = GUI.skin.button.margin;

			//X/Y축 설정 + 복사하기
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MirrorAxis_X), isMirrorX, isMirrorEnabled, width_AxisBtn, height_Axis))
			{
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
				Mesh._isMirrorX = true;
				isMirrorX = Mesh._isMirrorX;
				apEditorUtil.ReleaseGUIFocus();
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_MirrorAxis_Y), !isMirrorX, isMirrorEnabled, width_AxisBtn, height_Axis))
			{
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
				Mesh._isMirrorX = false;
				isMirrorX = Mesh._isMirrorX;
				apEditorUtil.ReleaseGUIFocus();
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			//축 위치 바꾸기
			int width_Label = 100;
			int width_Value = width - (10 + width_Label);
			int width_Label_Long = 200;
			int width_Value_Long = width - (10 + width_Label_Long);
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.RulerSettings));//"Ruler Settings"
																			   //GUILayout.Space(5);


			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
			GUILayout.Space(5);
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.RulerPosition), apGUILOFactory.I.Width(width_Label));//"Position"

			float prevAxisValue = isMirrorX ? Mesh._mirrorAxis.x : Mesh._mirrorAxis.y;

			float nextAxisValue = EditorGUILayout.DelayedFloatField(prevAxisValue, apGUIStyleWrapper.I.TextField_BtnMargin, apGUILOFactory.I.Width(width_Value));
			if (Mathf.Abs(nextAxisValue - prevAxisValue) > 0.0001f)
			{
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
				if (isMirrorX)
				{
					Mesh._mirrorAxis.x = nextAxisValue;
				}
				else
				{
					Mesh._mirrorAxis.y = nextAxisValue;
				}
				apEditorUtil.ReleaseGUIFocus();
			}

			EditorGUILayout.EndHorizontal();


			//위치 : 상하좌우, + Area 중심으로 이동
			//버튼을 Left, Right, Up, Down, Move To Center로 설정

			int width_MoveAxisBtn = 26;
			int width_CenterAxisBtn = width - (10 + width_MoveAxisBtn * 4 + 8);
			int height_MoveAxisBtn = 20;
			GUILayout.Space(2);
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_MoveAxisBtn));
			GUILayout.Space(5);
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Left), false, isMirrorX && isMirrorEnabled, width_MoveAxisBtn, height_MoveAxisBtn))
			{
				//Axis Y의 X 이동 (-) (Left)
				if (isMirrorX && isMirrorEnabled)
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
					Mesh._mirrorAxis.x -= 2;
					apEditorUtil.ReleaseGUIFocus();
				}
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Right), false, isMirrorX && isMirrorEnabled, width_MoveAxisBtn, height_MoveAxisBtn))
			{
				//Axis Y의 X 이동 (+) (Right)
				if (isMirrorX && isMirrorEnabled)
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
					Mesh._mirrorAxis.x += 2;
					apEditorUtil.ReleaseGUIFocus();
				}
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Up), false, !isMirrorX && isMirrorEnabled, width_MoveAxisBtn, height_MoveAxisBtn))
			{
				//Axis X의 Y 이동 (+) (Up)
				if (!isMirrorX && isMirrorEnabled)
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
					Mesh._mirrorAxis.y += 2;
					apEditorUtil.ReleaseGUIFocus();
				}
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Down), false, !isMirrorX && isMirrorEnabled, width_MoveAxisBtn, height_MoveAxisBtn))
			{
				//Axis X의 Y 이동 (-) (Down)
				if (!isMirrorX && isMirrorEnabled)
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
					Mesh._mirrorAxis.y -= 2;
					apEditorUtil.ReleaseGUIFocus();
				}
			}
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.MoveToCenter), false, isMirrorEnabled, width_CenterAxisBtn, height_MoveAxisBtn))//"Move to Center"
			{
				if (isMirrorEnabled)
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SettingChanged, Editor, Mesh, Mesh, false);
					if (Mesh._isPSDParsed)
					{
						//Area의 중심으로 이동
						if (isMirrorX)
						{
							Mesh._mirrorAxis.x = (Mesh._atlasFromPSD_LT.x + Mesh._atlasFromPSD_RB.x) * 0.5f;
						}
						else
						{
							Mesh._mirrorAxis.y = (Mesh._atlasFromPSD_LT.y + Mesh._atlasFromPSD_RB.y) * 0.5f;
						}
					}
					else
					{
						//그냥 Zero
						if (isMirrorX)
						{
							Mesh._mirrorAxis.x = 0;
						}
						else
						{
							Mesh._mirrorAxis.y = 0;
						}
					}
					apEditorUtil.ReleaseGUIFocus();
				}
			}
			//GUILayout.Space(5 + (width - (10 + width_MoveAxisBtn)) / 2);

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
			GUILayout.Space(5);
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Offset), apGUILOFactory.I.Width(width_Label));//"Offset"

			float nextMirrorCopyOffset = EditorGUILayout.DelayedFloatField(Editor._meshTRSOption_MirrorOffset, apGUIStyleWrapper.I.TextField_BtnMargin, apGUILOFactory.I.Width(width_Value));
			if (Mathf.Abs(nextMirrorCopyOffset - Editor._meshTRSOption_MirrorOffset) > 0.0001f)
			{
				Editor._meshTRSOption_MirrorOffset = nextMirrorCopyOffset;
				Editor.SaveEditorPref();
				apEditorUtil.ReleaseGUIFocus();
			}
			EditorGUILayout.EndHorizontal();

			if (isUseCopyTool)
			{
				GUILayout.Space(10);
				//복사
				//EditorGUILayout.LabelField("Mirror Copy");
				//" Copy"
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(isMirrorX ? apImageSet.PRESET.MeshEdit_MirrorCopy_X : apImageSet.PRESET.MeshEdit_MirrorCopy_Y), 1, Editor.GetUIWord(UIWORD.CopySymmetry), false, nVertices > 0 && isMirrorEnabled, width, 25))
				{
					//미러 복사하기
					if (nVertices > 0)
					{
						Editor.Controller.DuplicateMirrorVertices();
					}
					apEditorUtil.ReleaseGUIFocus();
				}
			}

			if (isUseSnapTool)
			{
				GUILayout.Space(10);
				//EditorGUILayout.LabelField("Snap to Ruler");
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
				GUILayout.Space(5);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SnapToRuler), apGUILOFactory.I.Width(width_Label_Long));//"Snap to Ruler"
				bool isNextSnapOpt = EditorGUILayout.Toggle(Editor._meshTRSOption_MirrorSnapVertOnRuler, apGUILOFactory.I.Width(width_Value_Long));
				if (isNextSnapOpt != Editor._meshTRSOption_MirrorSnapVertOnRuler)
				{
					Editor._meshTRSOption_MirrorSnapVertOnRuler = isNextSnapOpt;
					Editor.SaveEditorPref();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
				GUILayout.Space(5);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.RemoveSymmetry), apGUILOFactory.I.Width(width_Label_Long));//"Remove Symmetry"
				bool isNextRemoveSymm = EditorGUILayout.Toggle(Editor._meshTRSOption_MirrorRemoved, apGUILOFactory.I.Width(width_Value_Long));
				if (isNextRemoveSymm != Editor._meshTRSOption_MirrorRemoved)
				{
					Editor._meshTRSOption_MirrorRemoved = isNextRemoveSymm;
					Editor.SaveEditorPref();
				}
				EditorGUILayout.EndHorizontal();


			}
		}

		//Mesh의 기본 설정에서 Area 설정과 Mesh가 연결된 Texture의 Read 설정을 표시하는 UI
		private void DrawMeshAtlasOption(int width)
		{
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AreaSizeSettings));//"Area Size within the Atlas"
			GUILayout.Space(5);

			//isPSDParsed 옵션 여부
			if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.AreaOptionEnabled), Editor.GetUIWord(UIWORD.AreaOptionDisabled), Mesh._isPSDParsed, true, width, 25))//"Area Option Enabled", "Area Option Disabled"
			{
				apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
				Mesh._isPSDParsed = !Mesh._isPSDParsed;
			}

			//int width_Label = 150;
			//int width_Value = (width - (10 + width_Label));
			int width_AreaValue = 40;
			int width_AreaChangeBtn = 24;
			int width_AreaMargin_1Btn = 5 + (width - (width_AreaValue + 4 + 10)) / 2;
			int width_AreaMargin_2Btn = width - ((width_AreaValue + width_AreaChangeBtn * 2 + 4) * 2 + 10);

			//GUIStyle guiStyle_AreaResizeBtn = new GUIStyle(GUI.skin.button);
			//guiStyle_AreaResizeBtn.margin = GUI.skin.textField.margin;

			//이전
			//GUIContent guiContent_imgValueUp = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Up));
			//GUIContent guiContent_imgValueDown = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Down));
			//GUIContent guiContent_imgValueLeft = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Left));
			//GUIContent guiContent_imgValueRight = new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Right));

			//변경 19.11.20
			if (_guiContent_imgValueUp == null)
			{
				_guiContent_imgValueUp = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Up));
			}
			if (_guiContent_imgValueDown == null)
			{
				_guiContent_imgValueDown = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Down));
			}
			if (_guiContent_imgValueLeft == null)
			{
				_guiContent_imgValueLeft = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Left));
			}
			if (_guiContent_imgValueRight == null)
			{
				_guiContent_imgValueRight = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.MeshEdit_ValueChange_Right));
			}


			//int width_Label = 100;
			//int width_Value = width - (5 + width_Label);

			if (Mesh._isPSDParsed)
			{
				//이게 활성화 될 때에만 가능하다
				GUILayout.Space(10);

				//영역을 설정
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AreaSize));//"Area Size"

				//Top
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
				GUILayout.Space(width_AreaMargin_1Btn);
				float meshAtlas_Top = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_LT.y, apGUILOFactory.I.Width(width_AreaValue));
				if (GUILayout.Button(_guiContent_imgValueUp.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					//5씩 움직인다.
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_LT.y += 5;
					meshAtlas_Top = Mesh._atlasFromPSD_LT.y;
					apEditorUtil.ReleaseGUIFocus();
				}
				if (GUILayout.Button(_guiContent_imgValueDown.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_LT.y -= 5;
					meshAtlas_Top = Mesh._atlasFromPSD_LT.y;
					apEditorUtil.ReleaseGUIFocus();
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);

				//Left / Right
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
				GUILayout.Space(5);
				float meshAtlas_Left = Mesh._atlasFromPSD_LT.x;
				if (GUILayout.Button(_guiContent_imgValueLeft.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_LT.x -= 5;
					meshAtlas_Left = Mesh._atlasFromPSD_LT.x;
					apEditorUtil.ReleaseGUIFocus();
				}
				if (GUILayout.Button(_guiContent_imgValueRight.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_LT.x += 5;
					meshAtlas_Left = Mesh._atlasFromPSD_LT.x;
					apEditorUtil.ReleaseGUIFocus();
				}
				meshAtlas_Left = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_LT.x, apGUILOFactory.I.Width(width_AreaValue));

				GUILayout.Space(width_AreaMargin_2Btn);
				float meshAtlas_Right = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_RB.x, apGUILOFactory.I.Width(width_AreaValue));
				if (GUILayout.Button(_guiContent_imgValueLeft.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_RB.x -= 5;
					meshAtlas_Right = Mesh._atlasFromPSD_RB.x;
					apEditorUtil.ReleaseGUIFocus();
				}
				if (GUILayout.Button(_guiContent_imgValueRight.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_RB.x += 5;
					meshAtlas_Right = Mesh._atlasFromPSD_RB.x;
					apEditorUtil.ReleaseGUIFocus();
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);

				//Bottom
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(18));
				GUILayout.Space(width_AreaMargin_1Btn);
				float meshAtlas_Bottom = EditorGUILayout.DelayedFloatField(Mesh._atlasFromPSD_RB.y, apGUILOFactory.I.Width(width_AreaValue));
				if (GUILayout.Button(_guiContent_imgValueDown.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_RB.y -= 5;
					meshAtlas_Bottom = Mesh._atlasFromPSD_RB.y;
					apEditorUtil.ReleaseGUIFocus();
				}
				if (GUILayout.Button(_guiContent_imgValueUp.Content, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(width_AreaChangeBtn), apGUILOFactory.I.Height(18)))
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_RB.y += 5;
					meshAtlas_Bottom = Mesh._atlasFromPSD_RB.y;
					apEditorUtil.ReleaseGUIFocus();
				}
				EditorGUILayout.EndHorizontal();




				//Atlas의 범위 값이 바뀌었을 때
				if (meshAtlas_Top != Mesh._atlasFromPSD_LT.y
					|| meshAtlas_Left != Mesh._atlasFromPSD_LT.x
					|| meshAtlas_Right != Mesh._atlasFromPSD_RB.x
					|| meshAtlas_Bottom != Mesh._atlasFromPSD_RB.y)
				{
					apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_AtlasChanged, Editor, Mesh, Mesh, false);
					Mesh._atlasFromPSD_LT.y = meshAtlas_Top;
					Mesh._atlasFromPSD_LT.x = meshAtlas_Left;
					Mesh._atlasFromPSD_RB.x = meshAtlas_Right;
					Mesh._atlasFromPSD_RB.y = meshAtlas_Bottom;
					apEditorUtil.ReleaseGUIFocus();
				}

				GUILayout.Space(10);

				bool isValidTexture = false;
				bool isTextureReadWriteEnabled = false;
				apTextureData linkedTextureData = Mesh.LinkedTextureData;
				TextureImporter textureImporter = null;
				if (linkedTextureData != null && linkedTextureData._image != null)
				{
					isValidTexture = true;
					string path = AssetDatabase.GetAssetPath(linkedTextureData._image);
					textureImporter = TextureImporter.GetAtPath(path) as TextureImporter;
					isTextureReadWriteEnabled = textureImporter.isReadable;
				}

				if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.TextureRWEnabled), Editor.GetUIWord(UIWORD.TextureRWDisabled), isTextureReadWriteEnabled, isValidTexture, width, 25))//"Texture Read/Write Enabled", "Texture Read/Write Disabled"
				{
					if (textureImporter != null)
					{
						textureImporter.isReadable = !textureImporter.isReadable;
						textureImporter.SaveAndReimport();
						AssetDatabase.Refresh();
					}
				}

				if (isTextureReadWriteEnabled && isValidTexture)
				{
					//경고 메시지
					//GUILayout.Space(10);
					//GUIStyle guiStyle_Info = new GUIStyle(GUI.skin.box);
					//guiStyle_Info.alignment = TextAnchor.MiddleCenter;
					//guiStyle_Info.normal.textColor = apEditorUtil.BoxTextColor;

					Color prevColor = GUI.backgroundColor;

					GUI.backgroundColor = new Color(1.0f, 0.6f, 0.5f, 1.0f);
					GUILayout.Box(Editor.GetUIWord(UIWORD.WarningTextureRWNeedToDisabledForOpt), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(48));

					GUI.backgroundColor = prevColor;
				}


			}
		}

		private void OnHotKeyEvent_RemoveVertexOnTRS(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.Mesh
				|| _mesh == null
				|| Editor.Gizmos.IsFFDMode
				|| Editor.VertController.Vertex == null
				|| Editor.VertController.Vertices.Count == 0)
			{
				return;
			}

			bool isShift = Event.current.shift;

			apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_RemoveVertex, Editor, _mesh, _mesh, false);
			List<apVertex> vertices = Editor.VertController.Vertices;
			for (int i = 0; i < vertices.Count; i++)
			{
				_mesh.RemoveVertex(vertices[i], isShift);
				Editor.SetRepaint();
			}
			_mesh.RefreshPolygonsToIndexBuffer();
			Editor.VertController.UnselectVertex();
		}


		private void MeshProperty_Pivot(int width, int height)
		{
			GUILayout.Space(10);
			//EditorGUILayout.LabelField("Left Drag : Change Pivot To Origin");
			//DrawHowToControl(width, "Move Pivot", null, null, null);
			DrawHowToControl(width, Editor.GetUIWord(UIWORD.MovePivot), null, null, null);

			EditorGUILayout.Space();

			//"Reset Pivot"
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.ResetPivot), apGUILOFactory.I.Height(40)))
			{
				//아예 함수로 만들것
				//이전 코드
				//>> OffsetPos만 바꾸는 코드
				//apEditorUtil.SetRecord_Mesh(apUndoGroupData.ACTION.MeshEdit_SetPivot, Editor, _mesh, _mesh._offsetPos, false);

				//Editor.Select.Mesh._offsetPos = Vector2.zero;//<TODO : 이걸 사용하는 MeshGroup들의 DefaultPos를 역연산해야한다.

				//Editor.Select.Mesh.MakeOffsetPosMatrix();//<<OffsetPos를 수정하면 이걸 바꿔주자

				Editor.Controller.SetMeshPivot(Editor.Select.Mesh, Vector2.zero);
			}
		}




		//----------------------------------------------------------------------------
		//private string _prevMeshGroup_Name = "";

		private void MeshGroupProperty_Setting(int width, int height)
		{
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Name));//"Name"
			string nextMeshGroupName = EditorGUILayout.DelayedTextField(_meshGroup._name, apGUILOFactory.I.Width(width));
			if (!string.Equals(nextMeshGroupName, _meshGroup._name))
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, null, false, false);
				_meshGroup._name = nextMeshGroupName;
				Editor.RefreshControllerAndHierarchy(false);
			}

			GUILayout.Space(20);

			//" Editing.." / " Edit Default Transform"
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Edit_MeshGroupDefaultTransform),
				1, Editor.GetUIWord(UIWORD.EditingDefaultTransform),
				Editor.GetUIWord(UIWORD.EditDefaultTransform),
				_isMeshGroupSetting_ChangePivot, true, width, 30, apStringFactory.I.EditDefaultTransformsOfSubMeshesMeshGroups))//"Edit Default Transforms of Sub Meshs/MeshGroups"
			{
				_isMeshGroupSetting_ChangePivot = !_isMeshGroupSetting_ChangePivot;
				if (_isMeshGroupSetting_ChangePivot)
				{
					//Modifier 모두 비활성화
					MeshGroup._modifierStack.SetExclusiveModifierInEditing(null, null, false);
				}
				else
				{
					//Modifier 모두 활성화
					MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
					
					//이전
					//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, true, true, true);

					//변경 20.4.13
					Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																		apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff, 
																		apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);
				}

				RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, false);//<<추가
			}

			GUILayout.Space(20);


			//MainMesh에 포함되는가
			bool isMainMeshGroup = _portrait._mainMeshGroupList.Contains(MeshGroup);
			if (isMainMeshGroup)
			{
				//GUIStyle guiStyle = new GUIStyle(GUI.skin.box);
				//guiStyle.alignment = TextAnchor.MiddleCenter;
				//guiStyle.normal.textColor = apEditorUtil.BoxTextColor;

				Color prevColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(0.5f, 0.7f, 0.9f, 1.0f);

				//"Root Unit"
				GUILayout.Box(Editor.GetUIWord(UIWORD.RootUnit), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

				GUI.backgroundColor = prevColor;
			}
			else
			{
				//" Set Root Unit"
				//이전
				//if (GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.SetRootUnit), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Root), "Make Mesh Group as Root Unit"), GUILayout.Width(width), GUILayout.Height(30)))

				//변경
				if (_guiContent_MeshGroupProperty_SetRootUnit == null)
				{
					_guiContent_MeshGroupProperty_SetRootUnit = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.SetRootUnit), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Root), apStringFactory.I.MakeMeshGroupAsRootUnit);//"Make Mesh Group as Root Unit"
				}

				if (GUILayout.Button(_guiContent_MeshGroupProperty_SetRootUnit.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
				{
					apEditorUtil.SetRecord_PortraitMeshGroup(apUndoGroupData.ACTION.Portrait_SetMeshGroup, Editor, _portrait, MeshGroup, null, false, true);

					_portrait._mainMeshGroupIDList.Add(MeshGroup._uniqueID);
					_portrait._mainMeshGroupList.Add(MeshGroup);

					apRootUnit newRootUnit = new apRootUnit();
					newRootUnit.SetPortrait(_portrait);
					newRootUnit.SetMeshGroup(MeshGroup);

					_portrait._rootUnits.Add(newRootUnit);

					Editor.RefreshControllerAndHierarchy(false);

					//Root Hierarchy Filter를 활성화한다.
					Editor.SetHierarchyFilter(apEditor.HIERARCHY_FILTER.RootUnit, true);
				}
			}


			GUILayout.Space(20);

			apEditorUtil.GUI_DelimeterBoxH(width - 10);

			//와! 새해 첫 코드!
			//추가 20.1.5 : 메시 복제하기
			GUILayout.Space(5);
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.Duplicate), apGUILOFactory.I.Width(width)))//"Duplicate"
			{
				//TODO : 애니메이션도 복사할지 물어봐야함
				Editor.Controller.DuplicateMeshGroup(
					MeshGroup, null, true, true);
			}

			//삭제하기
			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width - 10);
			GUILayout.Space(5);
			//"  Remove Mesh Group"
			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveMeshGroup),
			//										Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
			//										),
			//						GUILayout.Height(24)))

			//변경
			if (_guiContent_MeshGroupProperty_RemoveMeshGroup == null)
			{
				_guiContent_MeshGroupProperty_RemoveMeshGroup = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveMeshGroup), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}


			if (GUILayout.Button(_guiContent_MeshGroupProperty_RemoveMeshGroup.Content, apGUILOFactory.I.Height(24)))
			{

				string strRemoveDialogInfo = Editor.Controller.GetRemoveItemMessage(
																_portrait,
																_meshGroup,
																5,
																Editor.GetTextFormat(TEXT.RemoveMeshGroup_Body, _meshGroup._name),
																Editor.GetText(TEXT.DLG_RemoveItemChangedWarning)
																);

				//bool isResult = EditorUtility.DisplayDialog("Remove Mesh Group", "Do you want to remove [" + _meshGroup._name + "]?", "Remove", "Cancel");
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveMeshGroup_Title),
																//Editor.GetTextFormat(TEXT.RemoveMeshGroup_Body, _meshGroup._name),
																strRemoveDialogInfo,
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);
				if (isResult)
				{
					Editor.Controller.RemoveMeshGroup(_meshGroup);

					SetNone();
				}
			}


		}



		private void MeshGroupProperty_Bone(int width, int height)
		{
			GUILayout.Space(10);

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Editable, _isBoneDefaultEditing);//"BoneEditMode - Editable"

			//" Editing Bones", " Start Editing Bones"
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_EditMode),
												1,
												Editor.GetUIWord(UIWORD.EditingBones), Editor.GetUIWord(UIWORD.StartEditingBones),
												IsBoneDefaultEditing, true, width, 30, apStringFactory.I.EditBones))//"Edit Bones"
			{
				//Bone을 수정할 수 있다.
				SetBoneEditing(!_isBoneDefaultEditing, true);
			}

			GUILayout.Space(5);

			//Add 툴과 Select 툴 On/Off

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Select, _boneEditMode == BONE_EDIT_MODE.SelectAndTRS);  //"BoneEditMode - Select"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Add, _boneEditMode == BONE_EDIT_MODE.Add);          //"BoneEditMode - Add"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Link, _boneEditMode == BONE_EDIT_MODE.Link);            //"BoneEditMode - Link"

			bool isBoneEditable = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Editable);//"BoneEditMode - Editable"
			bool isBoneEditMode_Select = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Select);//"BoneEditMode - Select"
			bool isBoneEditMode_Add = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Add);//"BoneEditMode - Add"
			bool isBoneEditMode_Link = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.BoneEditMode__Link);//"BoneEditMode - Link"

			int subTabWidth = (width / 3) - 4;
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));



			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Select),
											isBoneEditMode_Select, _isBoneDefaultEditing,
											subTabWidth, 40, Editor.GetUIWord(UIWORD.SelectBones)))//"Select Bones"
			{
				SetBoneEditMode(BONE_EDIT_MODE.SelectAndTRS, true);
			}

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Add),
											isBoneEditMode_Add, _isBoneDefaultEditing,
											subTabWidth, 40, Editor.GetUIWord(UIWORD.AddBones)))//"Add Bones"
			{
				SetBoneEditMode(BONE_EDIT_MODE.Add, true);
			}

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Link),
											isBoneEditMode_Link, _isBoneDefaultEditing,
											subTabWidth, 40, Editor.GetUIWord(UIWORD.LinkBones)))//"Link Bones"
			{
				SetBoneEditMode(BONE_EDIT_MODE.Link, true);
			}

			EditorGUILayout.EndHorizontal();


			GUILayout.Space(5);

			if (isBoneEditable)
			{
				string strBoneEditInfo = null;
				Color prevColor = GUI.backgroundColor;
				Color colorBoneEdit = Color.black;
				switch (_boneEditMode)
				{
					case BONE_EDIT_MODE.None:
						strBoneEditInfo = apStringFactory.I.NotEditable;
						colorBoneEdit = new Color(0.6f, 0.6f, 0.6f, 1.0f);
						break;

					case BONE_EDIT_MODE.SelectOnly:
						strBoneEditInfo = Editor.GetUIWord(UIWORD.SelectBones);//"Select Bones"
						colorBoneEdit = new Color(0.6f, 0.9f, 0.9f, 1.0f);
						break;

					case BONE_EDIT_MODE.SelectAndTRS:
						strBoneEditInfo = Editor.GetUIWord(UIWORD.SelectBones);//"Select Bones"
						colorBoneEdit = new Color(0.5f, 0.9f, 0.6f, 1.0f);
						break;

					case BONE_EDIT_MODE.Add:
						strBoneEditInfo = Editor.GetUIWord(UIWORD.AddBones);//"Add Bones"
						colorBoneEdit = new Color(0.95f, 0.65f, 0.65f, 1.0f);
						break;

					case BONE_EDIT_MODE.Link:
						strBoneEditInfo = Editor.GetUIWord(UIWORD.LinkBones);//"Link Bones"
						colorBoneEdit = new Color(0.57f, 0.82f, 0.95f, 1.0f);
						break;
				}

				//GUIStyle guiStyle_Info = new GUIStyle(GUI.skin.box);
				//guiStyle_Info.alignment = TextAnchor.MiddleCenter;
				//guiStyle_Info.normal.textColor = apEditorUtil.BoxTextColor;

				GUI.backgroundColor = colorBoneEdit;
				GUILayout.Box((strBoneEditInfo != null ? strBoneEditInfo : apStringFactory.I.None), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 8), apGUILOFactory.I.Height(34));

				GUI.backgroundColor = prevColor;

				GUILayout.Space(5);
				switch (_boneEditMode)
				{
					case BONE_EDIT_MODE.None:
						DrawHowToControl(width, apStringFactory.I.HowToControl_None, Editor.GetUIWord(UIWORD.MoveView), apStringFactory.I.HowToControl_None, null);//"None" / "Move View" / "None"
						break;

					//"Select Bones", "Move View", "Deselect"
					case BONE_EDIT_MODE.SelectOnly:
						DrawHowToControl(width, Editor.GetUIWord(UIWORD.SelectBones), Editor.GetUIWord(UIWORD.MoveView), Editor.GetUIWord(UIWORD.Deselect), null);//<<삭제 포함해야할 듯?
						break;

					//"Select Bones", "Move View", "Deselect"
					case BONE_EDIT_MODE.SelectAndTRS:
						DrawHowToControl(width, Editor.GetUIWord(UIWORD.SelectBones), Editor.GetUIWord(UIWORD.MoveView), Editor.GetUIWord(UIWORD.Deselect), null);
						break;

					//"Add Bones", "Move View", "Deselect"
					case BONE_EDIT_MODE.Add:
						DrawHowToControl(width, Editor.GetUIWord(UIWORD.AddBones), Editor.GetUIWord(UIWORD.MoveView), Editor.GetUIWord(UIWORD.Deselect), null);
						break;

					//"Select and Link Bones", "Move View", "Deselect"
					case BONE_EDIT_MODE.Link:
						DrawHowToControl(width, Editor.GetUIWord(UIWORD.SelectAndLinkBones), Editor.GetUIWord(UIWORD.MoveView), Editor.GetUIWord(UIWORD.Deselect), null);
						break;
				}

			}

			GUILayout.Space(10);
			//" Export/Import Bones"
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_SaveLoad),
												1, Editor.GetUIWord(UIWORD.ExportImportBones),
												Editor.GetUIWord(UIWORD.ExportImportBones),
												false, true, width, 26))
			{
				//Bone을 파일로 저장하거나 열수 있는 다이얼로그를 호출한다.
				_loadKey_OnBoneStructureLoaded = apDialog_RetargetBase.ShowDialog(Editor, _meshGroup, OnBoneStruceLoaded);
			}

			GUILayout.Space(20);

			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(20);

			//"Remove All Bones"
			//이전
			//if (GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.RemoveAllBones), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)), GUILayout.Width(width), GUILayout.Height(24)))

			//변경
			if (_guiContent_MeshGroupProperty_RemoveAllBones == null)
			{
				_guiContent_MeshGroupProperty_RemoveAllBones = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.RemoveAllBones), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}

			if (GUILayout.Button(_guiContent_MeshGroupProperty_RemoveAllBones.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(24)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove Bones", "Remove All Bones?", "Remove", "Cancel");
				//이건 관련 메시지가 없다.
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveBonesAll_Title),
																Editor.GetText(TEXT.RemoveBonesAll_Body),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);
				if (isResult)
				{
					Editor.Controller.RemoveAllBones(MeshGroup);
				}
			}
		}




		private void OnBoneStruceLoaded(bool isSuccess, object loadKey, apRetarget retargetData, apMeshGroup targetMeshGroup)
		{
			if (!isSuccess || _loadKey_OnBoneStructureLoaded != loadKey || _meshGroup != targetMeshGroup || targetMeshGroup == null)
			{
				_loadKey_OnBoneStructureLoaded = null;
				return;
			}
			_loadKey_OnBoneStructureLoaded = null;

			if (retargetData.IsBaseFileLoaded)
			{
				Editor.Controller.ImportBonesFromRetargetBaseFile(targetMeshGroup, retargetData);
			}
		}







		private void MeshGroupProperty_Modify(int width, int height)
		{
			//EditorGUILayout.LabelField("Presets");
			GUILayout.Space(10);

			//"  Add Modifier"
			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddModifier), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddNewMod), "Add a New Modifier"), GUILayout.Height(30)))

			//변경
			if (_guiContent_MeshGroupProperty_AddModifier == null)
			{
				_guiContent_MeshGroupProperty_AddModifier = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AddModifier), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddNewMod), apStringFactory.I.AddANewModifier);//"Add a New Modifier"
			}

			if (GUILayout.Button(_guiContent_MeshGroupProperty_AddModifier.Content, apGUILOFactory.I.Height(30)))
			{
				_loadKey_AddModifier = apDialog_AddModifier.ShowDialog(Editor, MeshGroup, OnAddModifier);
			}

			GUILayout.Space(20);
			//EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
			GUILayout.Space(2);

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ModifierStack), apGUILOFactory.I.Height(25));//"Modifier Stack"

			//GUIStyle guiStyle_None = new GUIStyle(GUIStyle.none);
			GUILayout.Button("", apGUIStyleWrapper.I.None, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20));//<레이아웃 정렬을 위한의미없는 숨은 버튼
			EditorGUILayout.EndHorizontal();
			apModifierStack modStack = MeshGroup._modifierStack;

			if (_guiContent_MeshGroupProperty_ModifierLayerUnit == null)
			{
				_guiContent_MeshGroupProperty_ModifierLayerUnit = new apGUIContentWrapper();
			}


			//등록된 Modifier 리스트를 출력하자
			if (modStack._modifiers.Count > 0)
			{
				//int iLayerSortChange = -1;
				//bool isLayerUp = false;//<<Up : 레이어값을 올린다.

				//역순으로 출력한다.
				for (int i = modStack._modifiers.Count - 1; i >= 0; i--)
				{
					DrawModifierLayerUnit(modStack._modifiers[i], width, 25);
				}

				//레이어 바꾸는 기능은 다른 곳에서..
				//if(iLayerSortChange >= 0)
				//{
				//	Editor.Controller.LayerChange(modStack._modifiers[iLayerSortChange], isLayerUp);
				//}
			}


		}

		private void OnAddModifier(bool isSuccess, object loadKey, apModifierBase.MODIFIER_TYPE modifierType, apMeshGroup targetMeshGroup, int validationKey)
		{
			if (!isSuccess || _loadKey_AddModifier != loadKey || MeshGroup != targetMeshGroup)
			{
				_loadKey_AddModifier = null;
				return;
			}

			if (modifierType != apModifierBase.MODIFIER_TYPE.Base)
			{
				Editor.Controller.AddModifier(modifierType, validationKey);
			}
			_loadKey_AddModifier = null;
		}

		//private int DrawModifierLayerUnit(apModifierBase modifier, int width, int height, bool isLayerUp, bool isLayerDown)
		private int DrawModifierLayerUnit(apModifierBase modifier, int width, int height)
		{
			Rect lastRect = GUILayoutUtility.GetLastRect();

			GUIStyle curGUIStyle = null;//<<최적화된 코드
										//Color texColor = GUI.skin.label.normal.textColor;

			if (Modifier == modifier)
			{
				Color prevColor = GUI.backgroundColor;

				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
					//texColor = Color.cyan;
				}
				else
				{
					GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
					//texColor = Color.white;
				}

				GUI.Box(new Rect(lastRect.x, lastRect.y + height, width + 15, height), "");
				GUI.backgroundColor = prevColor;

				curGUIStyle = apGUIStyleWrapper.I.None_White2Cyan;
			}
			else
			{
				curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;
			}

			//GUIStyle guiStyle_None = new GUIStyle(GUIStyle.none);
			//guiStyle_None.normal.textColor = texColor;

			apImageSet.PRESET iconType = apEditorUtil.GetModifierIconType(modifier.ModifierType);

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height));
			GUILayout.Space(10);

			//이전
			//if (GUILayout.Button(new GUIContent(" " + modifier.DisplayName, Editor.ImageSet.Get(iconType)), guiStyle_None, GUILayout.Width(width - 40), GUILayout.Height(height)))

			//변경
			_guiContent_MeshGroupProperty_ModifierLayerUnit.SetText(1, modifier.DisplayName);
			_guiContent_MeshGroupProperty_ModifierLayerUnit.SetImage(Editor.ImageSet.Get(iconType));

			if (GUILayout.Button(_guiContent_MeshGroupProperty_ModifierLayerUnit.Content, curGUIStyle, apGUILOFactory.I.Width(width - 40), apGUILOFactory.I.Height(height)))
			{
				SetModifier(modifier);
			}

			int iResult = 0;

			Texture2D activeBtn = null;
			bool isActiveMod = false;
			if (modifier._isActive && modifier._editorExclusiveActiveMod != apModifierBase.MOD_EDITOR_ACTIVE.Disabled)
			{
				activeBtn = Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Active);
				isActiveMod = true;
			}
			else
			{
				activeBtn = Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Deactive);
				isActiveMod = false;
			}
			if (GUILayout.Button(activeBtn, curGUIStyle, apGUILOFactory.I.Width(height), apGUILOFactory.I.Height(height)))
			{
				//일단 토글한다.
				modifier._isActive = !isActiveMod;

				if (ExEditingMode != EX_EDIT.None)
				{
					if (modifier._editorExclusiveActiveMod == apModifierBase.MOD_EDITOR_ACTIVE.Disabled)
					{
						//작업이 허용된 Modifier가 아닌데 Active를 제어했다면
						//ExEdit를 해제해야한다.
						SetModifierExclusiveEditing(EX_EDIT.None);
					}
				}


				//if (!ExEditingMode)
				//{
				//	//Debug.LogError("TODO : Active를 바꾸면, 녹화 기능이 무조건 비활성화되어야 한다.");
				//	SetModifierExclusiveEditing(false);
				//}
			}
			EditorGUILayout.EndHorizontal();

			return iResult;
		}

		//------------------------------------------------------------------------------------
		public void DrawEditor_Right2(int width, int height)
		{
			if (Editor == null || Editor.Select.Portrait == null)
			{
				return;
			}

			EditorGUILayout.Space();

			switch (_selectionType)
			{
				case SELECTION_TYPE.MeshGroup:
					{
						switch (Editor._meshGroupEditMode)
						{
							case apEditor.MESHGROUP_EDIT_MODE.Setting:
								DrawEditor_Right2_MeshGroup_Setting(width, height);
								break;

							case apEditor.MESHGROUP_EDIT_MODE.Bone:
								DrawEditor_Right2_MeshGroup_Bone(width, height);
								break;

							case apEditor.MESHGROUP_EDIT_MODE.Modifier:
								DrawEditor_Right2_MeshGroup_Modifier(width, height);
								break;
						}
					}
					break;

				case SELECTION_TYPE.Animation:
					{
						DrawEditor_Right2_Animation(width, height);
					}
					break;
			}
		}


		//--------------------------------------------------------------------------------------
		public void DrawEditor_Bottom2Edit(int width, int height)
		{
			if (Editor == null || Editor.Select.Portrait == null || Modifier == null)
			{
				return;
			}

			//GUIStyle btnGUIStyle = new GUIStyle(GUI.skin.button);
			//btnGUIStyle.alignment = TextAnchor.MiddleLeft;

			bool isRiggingModifier = (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging);
			bool isWeightedVertModifier = (int)(Modifier.ModifiedValueType & apModifiedMesh.MOD_VALUE_TYPE.VertexWeightList_Physics) != 0
										|| (int)(Modifier.ModifiedValueType & apModifiedMesh.MOD_VALUE_TYPE.VertexWeightList_Volume) != 0;
			//기본 Modifier가 있고
			//Rigging용 Modifier UI가 따로 있다.
			//추가 : Weight값을 사용하는 Physic/Volume도 따로 설정
			int editToggleWidth = 160;//140 > 180
			if (isRiggingModifier)
			{
				//리깅 타입인 경우
				//리깅 편집 툴 / 보기 버튼들이 나온다.
				//1. Rigging On/Off
				//+ 선택된 Mesh Transform
				//2. View 모드
				//3. Test Posing On/Off
				//"  Binding..", "  Start Binding"
				if (apEditorUtil.ToggledButton_2Side_LeftAlign(Editor.ImageSet.Get(apImageSet.PRESET.Rig_EditBinding),
														2, Editor.GetUIWord(UIWORD.ModBinding),
														Editor.GetUIWord(UIWORD.ModStartBinding),
														_rigEdit_isBindingEdit, true, editToggleWidth, height,
														apStringFactory.I.BindingModeToggleTooltip))//"Enable/Disable Bind Mode (A)"
				{
					_rigEdit_isBindingEdit = !_rigEdit_isBindingEdit;
					_rigEdit_isTestPosing = false;

					//작업중인 Modifier 외에는 일부 제외를 하자
					if (_rigEdit_isBindingEdit)
					{
						MeshGroup._modifierStack.SetExclusiveModifierInEditing(_modifier, SubEditedParamSetGroup, false);
						RefreshMeshGroupExEditingFlags(MeshGroup, _modifier, null, null, false);//<<추가

						//변경 3.23 : 선택 잠금을 무조건 켜는게 아니라, 옵션에 의해서 켤지 말지를 결정한다.
						if (Editor._isSelectionLockOption_RiggingPhysics)
						{
							_isSelectionLock = true;
						}
					}
					else
					{
						if (MeshGroup != null)
						{
							//Exclusive 모두 해제
							MeshGroup._modifierStack.ActiveAllModifierFromExclusiveEditing();
							
							//이전
							//Editor.Controller.SetMeshGroupTmpWorkVisibleReset(MeshGroup, false, true, true);

							//변경 20.4.13
							Editor.Controller.SetMeshGroupTmpWorkVisibleReset(	MeshGroup, 
																				apEditorController.RESET_VISIBLE_ACTION.OnlyRefreshIfOptionIsOff,
																				apEditorController.RESET_VISIBLE_TARGET.RenderUnitsAndBones);

							RefreshMeshGroupExEditingFlags(MeshGroup, null, null, null, false);//<<추가
						}
						_isSelectionLock = false;
					}

					//추가 19.7.27 : 본의 RigLock을 해제
					Editor.Controller.ResetBoneRigLock(MeshGroup);

					//Rigging의 Brush 모드 초기화
					_rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
					Editor.Gizmos.EndBrush();

					Editor.RefreshControllerAndHierarchy(false);
				}
				GUILayout.Space(10);

			}
			else
			{
				//그외의 Modifier
				//편집 On/Off와 현재 선택된 Key/Value가 나온다.
				//"  Editing..", "  Start Editing", "  Not Editiable"
				if (apEditorUtil.ToggledButton_2Side_LeftAlign(Editor.ImageSet.Get(apImageSet.PRESET.Edit_Recording),
													Editor.ImageSet.Get(apImageSet.PRESET.Edit_Record),
													Editor.ImageSet.Get(apImageSet.PRESET.Edit_NoRecord),
													2, Editor.GetUIWord(UIWORD.ModEditing),
													Editor.GetUIWord(UIWORD.ModStartEditing),
													Editor.GetUIWord(UIWORD.ModNotEditable),
													_exclusiveEditing != EX_EDIT.None, IsExEditable, editToggleWidth, height,
													apStringFactory.I.EditModeToggleTooltip))//"Enable/Disable Edit Mode (A)"
				{
					EX_EDIT nextResult = EX_EDIT.None;
					if (_exclusiveEditing == EX_EDIT.None && IsExEditable)
					{
						//None -> ExOnly로 바꾼다.
						//General은 특별한 경우
						nextResult = EX_EDIT.ExOnly_Edit;
					}
					//if (IsExEditable || !isNextResult)
					//{
					//	//SetModifierExclusiveEditing(isNextResult);
					//}
					SetModifierExclusiveEditing(nextResult);
					if (nextResult == EX_EDIT.ExOnly_Edit)
					{
						//변경 3.23 : 선택 잠금을 무조건 켜는게 아니라, 에디터 설정에 따라 켤지 말지 결정한다.
						//true 또는 변경 없음 (false가 아님)
						//모디파이어의 종류에 따라서 다른 옵션을 적용
						if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Physic)
						{
							if (Editor._isSelectionLockOption_RiggingPhysics)
							{
								_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
							}
						}
						else if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Morph ||
							Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.AnimatedMorph)
						{
							if (Editor._isSelectionLockOption_Morph)
							{
								_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
							}
						}
						else
						{
							if (Editor._isSelectionLockOption_Transform)
							{
								_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
							}
						}
					}
					else
					{
						_isSelectionLock = false;//Editing 해제시 Lock 해제
					}
				}




				GUILayout.Space(10);
				//Lock 걸린 키 / 수정중인 객체 / 그 값을 각각 표시하자

			}



			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Edit_SelectionLock),
												Editor.ImageSet.Get(apImageSet.PRESET.Edit_SelectionUnlock),
												IsSelectionLock, true, height, height,
												apStringFactory.I.SelectionLockToggleTooltip
												))//"Selection Lock/Unlock (S)"
			{
				SetModifierExclusiveEditKeyLock(!IsSelectionLock);
			}



			GUILayout.Space(10);

			//#if UNITY_EDITOR_OSX
			//			string strCtrlKey = "Command";
			//#else
			//			string strCtrlKey = "Ctrl";
			//#endif




			if (apEditorUtil.ToggledButton_2Side_Ctrl(Editor.ImageSet.Get(apImageSet.PRESET.Edit_ModLock),
												Editor.ImageSet.Get(apImageSet.PRESET.Edit_ModUnlock),
												_exclusiveEditing == EX_EDIT.ExOnly_Edit,
												IsExEditable && _exclusiveEditing != EX_EDIT.None,
												height, height,
												apStringFactory.I.ModifierLockToggleTooltip,
												Event.current.control,
												Event.current.command))////"Modifier Lock/Unlock (D) / If you press the button while holding down [" + strCtrlKey + "], the Setting dialog opens",
			{
				//여기서 ExOnly <-> General 사이를 바꾼다.

				//변경 3.22 : Ctrl 키를 누르고 클릭하면 설정 Dialog가 뜬다.
#if UNITY_EDITOR_OSX
				bool isCtrl = Event.current.command;
#else
				bool isCtrl = Event.current.control;
#endif
				if (isCtrl)
				{
					apDialog_ModifierLockSetting.ShowDialog(Editor, _portrait);
				}
				else
				{
					if (IsExEditable && _exclusiveEditing != EX_EDIT.None)
					{
						EX_EDIT nextEditMode = EX_EDIT.ExOnly_Edit;
						if (_exclusiveEditing == EX_EDIT.ExOnly_Edit)
						{
							nextEditMode = EX_EDIT.General_Edit;
						}
						SetModifierExclusiveEditing(nextEditMode);
					}
				}
			}


			//토글 단축키를 입력하자
			//[A : Editor Toggle]
			//[S (Space에서 S로 변경) : Selection Lock]
			//[D : Modifier Lock)
			Editor.AddHotKeyEvent(OnHotKeyEvent_ToggleModifierEditing, apHotKey.LabelText.ToggleEditingMode, KeyCode.A, false, false, false, null);//"Toggle Editing Mode"
			Editor.AddHotKeyEvent(OnHotKeyEvent_ToggleExclusiveEditKeyLock, apHotKey.LabelText.ToggleSelectionLock, KeyCode.S, false, false, false, null);//"Toggle Selection Lock"
			Editor.AddHotKeyEvent(OnHotKeyEvent_ToggleExclusiveModifierLock, apHotKey.LabelText.ToggleModifierLock, KeyCode.D, false, false, false, null);//"Toggle Modifier Lock"



			GUILayout.Space(10);

			apImageSet.PRESET modImagePreset = apEditorUtil.GetModifierIconType(Modifier.ModifierType);

			if (_guiContent_Bottom_EditMode_CommonIcon == null)
			{
				_guiContent_Bottom_EditMode_CommonIcon = new apGUIContentWrapper();
			}

			//이전
			//GUIStyle guiStyle_Key = new GUIStyle(GUI.skin.label);
			//if (IsSelectionLock)
			//{
			//	guiStyle_Key.normal.textColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
			//}

			//변경
			GUIStyle guiStyle_Key = IsSelectionLock ? apGUIStyleWrapper.I.Label_RedColor : apGUIStyleWrapper.I.Label;//최적화된 코드

			//이전
			//GUIStyle guiStyle_NotSelected = new GUIStyle(GUI.skin.label);
			//guiStyle_NotSelected.normal.textColor = new Color(0.0f, 0.5f, 1.0f, 1.0f);

			//변경
			GUIStyle guiStyle_NotSelected = apGUIStyleWrapper.I.Label_LightBlueColor;//최적화된 코드


			int paramSetWidth = 140;//100 -> 140
			int modValueWidth = 200;//170 -> 200

			switch (_exEditKeyValue)
			{
				case EX_EDIT_KEY_VALUE.None:
					break;

				case EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert:
				case EX_EDIT_KEY_VALUE.ParamKey_ModMesh://ModVert와 ModMesh는 비슷하다
					{
						//Key
						//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(modImagePreset)), GUILayout.Width(height), GUILayout.Height(height));

						_guiContent_Bottom_EditMode_CommonIcon.SetImage(Editor.ImageSet.Get(modImagePreset));
						EditorGUILayout.LabelField(_guiContent_Bottom_EditMode_CommonIcon.Content, apGUILOFactory.I.Width(height), apGUILOFactory.I.Height(height));


						Texture2D selectedImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Mesh);

						string strKey_ParamSetGroup = Editor.GetUIWord(UIWORD.ModNoParam);//"<No Parameter>"
						string strKey_ParamSet = Editor.GetUIWord(UIWORD.ModNoKey);//"<No Key>"
						string strKey_ModMesh = Editor.GetUIWord(UIWORD.ModNoSelected);//"<Not Selected>"
						string strKey_ModMeshLabel = Editor.GetUIWord(UIWORD.ModSubObject);//"Sub Object"

						GUIStyle guiStyle_ParamSetGroup = guiStyle_NotSelected;//<<최적화된 코드
						GUIStyle guiStyle_ParamSet = guiStyle_NotSelected;//<<최적화된 코드
						GUIStyle guiStyle_Transform = guiStyle_NotSelected;//<<최적화된 코드

						if (ExKey_ModParamSetGroup != null)
						{
							if (ExKey_ModParamSetGroup._keyControlParam != null)
							{
								strKey_ParamSetGroup = ExKey_ModParamSetGroup._keyControlParam._keyName;
								guiStyle_ParamSetGroup = guiStyle_Key;
							}
						}

						if (ExKey_ModParamSet != null)
						{
							//TODO : 컨트롤 타입이 아니면 다른 이름을 쓰자
							strKey_ParamSet = ExKey_ModParamSet.ControlParamValue;
							guiStyle_ParamSet = guiStyle_Key;
						}

						apModifiedMesh modMesh = null;
						if (_exEditKeyValue == EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert)
						{
							modMesh = ExKey_ModMesh;
						}
						else
						{
							modMesh = ExValue_ModMesh;
						}

						if (modMesh != null)
						{
							if (modMesh._transform_Mesh != null)
							{
								strKey_ModMeshLabel = Editor.GetUIWord(UIWORD.Mesh);//>그냥 Mesh로 표현
								strKey_ModMesh = modMesh._transform_Mesh._nickName;
								selectedImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Mesh);
								guiStyle_Transform = guiStyle_Key;
							}
							else if (modMesh._transform_MeshGroup != null)
							{
								strKey_ModMeshLabel = Editor.GetUIWord(UIWORD.MeshGroup);//>그냥 MeshGroup으로 표현
								strKey_ModMesh = modMesh._transform_MeshGroup._nickName;
								selectedImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_MeshGroup);
								guiStyle_Transform = guiStyle_Key;
							}
						}
						else
						{
							if (ExKey_ModParamSet == null)
							{
								//Key를 먼저 선택할 것을 알려야한다.
								strKey_ModMesh = Editor.GetUIWord(UIWORD.ModSelectKeyFirst);//"<Select Key First>"
							}
						}

						if (Modifier.SyncTarget != apModifierParamSetGroup.SYNC_TARGET.Static)
						{
							EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(paramSetWidth), apGUILOFactory.I.Height(height));
							EditorGUILayout.LabelField(strKey_ParamSetGroup, guiStyle_ParamSetGroup, apGUILOFactory.I.Width(paramSetWidth));
							EditorGUILayout.LabelField(strKey_ParamSet, guiStyle_ParamSet, apGUILOFactory.I.Width(paramSetWidth));
							EditorGUILayout.EndVertical();
						}
						else
						{
							EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(paramSetWidth), apGUILOFactory.I.Height(height));
							EditorGUILayout.LabelField(Modifier.DisplayName, guiStyle_Key, apGUILOFactory.I.Width(paramSetWidth));
							//EditorGUILayout.LabelField(strKey_ParamSet, guiStyle_ParamSet, GUILayout.Width(100));
							EditorGUILayout.EndVertical();
						}

						//EditorGUILayout.LabelField(new GUIContent(selectedImage), GUILayout.Width(height), GUILayout.Height(height));

						_guiContent_Bottom_EditMode_CommonIcon.SetImage(selectedImage);
						EditorGUILayout.LabelField(_guiContent_Bottom_EditMode_CommonIcon.Content, apGUILOFactory.I.Width(height), apGUILOFactory.I.Height(height));

						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(modValueWidth), apGUILOFactory.I.Height(height));
						EditorGUILayout.LabelField(strKey_ModMeshLabel, apGUILOFactory.I.Width(modValueWidth));
						EditorGUILayout.LabelField(strKey_ModMesh, guiStyle_Transform, apGUILOFactory.I.Width(modValueWidth));
						EditorGUILayout.EndVertical();


						GUILayout.Space(10);
						apEditorUtil.GUI_DelimeterBoxV(height - 6);
						GUILayout.Space(10);

						//Value
						//(선택한 Vert의 값을 출력하자. 단, Rigging Modifier가 아닐때)
						if (_exEditKeyValue == EX_EDIT_KEY_VALUE.ModMeshAndParamKey_ModVert && !isRiggingModifier && !isWeightedVertModifier)
						{

							bool isModVertSelected = (ExValue_ModVert != null);
							Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Bottom2_Transform_Mod_Vert, isModVertSelected);//"Bottom2 Transform Mod Vert"

							if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Bottom2_Transform_Mod_Vert))//"Bottom2 Transform Mod Vert"
							{
								//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Edit_Vertex)), GUILayout.Width(height), GUILayout.Height(height));
								_guiContent_Bottom_EditMode_CommonIcon.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Edit_Vertex));
								EditorGUILayout.LabelField(_guiContent_Bottom_EditMode_CommonIcon.Content, apGUILOFactory.I.Width(height), apGUILOFactory.I.Height(height));

								EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(150), apGUILOFactory.I.Height(height));

								//"Vertex : " + ExValue_ModVert._modVert._vertexUniqueID

								if (_strWrapper_64 == null)
								{
									_strWrapper_64 = new apStringWrapper(64);
								}
								_strWrapper_64.Clear();
								_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Vertex), false);
								_strWrapper_64.Append(apStringFactory.I.Colon_Space, false);
								_strWrapper_64.Append(ExValue_ModVert._modVert._vertexUniqueID, true);

								//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Vertex) + " : " + ExValue_ModVert._modVert._vertexUniqueID, apGUILOFactory.I.Width(150));
								EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(150));

								//Vector2 newDeltaPos = EditorGUILayout.Vector2Field("", ExValue_ModVert._modVert._deltaPos, GUILayout.Width(150));
								Vector2 newDeltaPos = apEditorUtil.DelayedVector2Field(ExValue_ModVert._modVert._deltaPos, 150);
								if (ExEditingMode != EX_EDIT.None)
								{
									ExValue_ModVert._modVert._deltaPos = newDeltaPos;
								}
								EditorGUILayout.EndVertical();
							}
						}


					}
					break;

				case EX_EDIT_KEY_VALUE.ParamKey_Bone:
					{
						//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(modImagePreset)), GUILayout.Width(height), GUILayout.Height(height));

						_guiContent_Bottom_EditMode_CommonIcon.SetImage(Editor.ImageSet.Get(modImagePreset));
						EditorGUILayout.LabelField(_guiContent_Bottom_EditMode_CommonIcon.Content, apGUILOFactory.I.Width(height), apGUILOFactory.I.Height(height));
					}
					break;

			}

			if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
			{
				//리깅 타입이면 몇가지 제어 버튼이 추가된다.
				//2. View 모드
				//3. Test Posing On/Off

				//View 모드는 Weight+Texture 여부 / Bone Color / Circle<->Square Vertex의 세가지로 구성된다.
				if (apEditorUtil.ToggledButton_2Side(
					Editor.ImageSet.Get(apImageSet.PRESET.Rig_WeightColorOnly),
					Editor.ImageSet.Get(apImageSet.PRESET.Rig_WeightColorWithTexture),
					Editor._rigViewOption_WeightOnly, true, height + 5, height, apStringFactory.I.RiggingViewModeTooltip_ColorWithTexture))//"Whether to render the Rigging weight with the texture of the image"
				{
					Editor._rigViewOption_WeightOnly = !Editor._rigViewOption_WeightOnly;
					Editor.SaveEditorPref();
				}

				GUILayout.Space(2);

				//"Bone Color", "Bone Color"
				if (apEditorUtil.ToggledButton_2Side(
					Editor.ImageSet.Get(apImageSet.PRESET.Rig_BoneColor),
					Editor.ImageSet.Get(apImageSet.PRESET.Rig_NoBoneColor),
					Editor._rigViewOption_BoneColor,
					true,
					height + 5, height, apStringFactory.I.RiggingViewModeTooltip_BoneColor))//"Whether to render the Rigging weight by the color of the Bone"
				{
					Editor._rigViewOption_BoneColor = !Editor._rigViewOption_BoneColor;
					Editor.SaveEditorPref();//<<이것도 Save 요건
				}

				GUILayout.Space(2);

				if (apEditorUtil.ToggledButton_2Side(
					Editor.ImageSet.Get(apImageSet.PRESET.Rig_CircleVert),
					Editor.ImageSet.Get(apImageSet.PRESET.Rig_SquareColorVert),
					Editor._rigViewOption_CircleVert,
					true,
					height + 5, height, apStringFactory.I.RiggingViewModeTooltip_CircleVert))//"Whether to render vertices into circular shapes"
				{
					Editor._rigViewOption_CircleVert = !Editor._rigViewOption_CircleVert;
					Editor.SaveEditorPref();//<<이것도 Save 요건
				}

				Texture2D iconNoLinkedBone = null;
				switch (Editor._rigGUIOption_NoLinkedBoneVisibility)
				{
					case apEditor.NOLINKED_BONE_VISIBILITY.Opaque: iconNoLinkedBone = Editor.ImageSet.Get(apImageSet.PRESET.Rig_ShowAllBones); break;
					case apEditor.NOLINKED_BONE_VISIBILITY.Translucent: iconNoLinkedBone = Editor.ImageSet.Get(apImageSet.PRESET.Rig_TransculentBones); break;
					case apEditor.NOLINKED_BONE_VISIBILITY.Hidden: iconNoLinkedBone = Editor.ImageSet.Get(apImageSet.PRESET.Rig_HideBones); break;
				}

				if (apEditorUtil.ToggledButton_2Side_Ctrl(
					iconNoLinkedBone, iconNoLinkedBone,
					Editor._rigGUIOption_NoLinkedBoneVisibility != apEditor.NOLINKED_BONE_VISIBILITY.Opaque,
					true,
					height + 5, height, apStringFactory.I.RiggingViewModeTooltip_NoLinkedBoneVisibility,
					Event.current.control, Event.current.command))//"Whether to render vertices into circular shapes"
				{
					//클릭할 때마다 하나씩 다음 단계로 이동. Ctrl를 누르면 반대로 이동
#if UNITY_EDITOR_OSX
					if(Event.current.command)
#else
					if (Event.current.control)
#endif
					{
						switch (Editor._rigGUIOption_NoLinkedBoneVisibility)
						{
							case apEditor.NOLINKED_BONE_VISIBILITY.Opaque: Editor._rigGUIOption_NoLinkedBoneVisibility = apEditor.NOLINKED_BONE_VISIBILITY.Hidden; break;
							case apEditor.NOLINKED_BONE_VISIBILITY.Translucent: Editor._rigGUIOption_NoLinkedBoneVisibility = apEditor.NOLINKED_BONE_VISIBILITY.Opaque; break;
							case apEditor.NOLINKED_BONE_VISIBILITY.Hidden: Editor._rigGUIOption_NoLinkedBoneVisibility = apEditor.NOLINKED_BONE_VISIBILITY.Translucent; break;
						}
					}
					else
					{
						switch (Editor._rigGUIOption_NoLinkedBoneVisibility)
						{
							case apEditor.NOLINKED_BONE_VISIBILITY.Opaque: Editor._rigGUIOption_NoLinkedBoneVisibility = apEditor.NOLINKED_BONE_VISIBILITY.Translucent; break;
							case apEditor.NOLINKED_BONE_VISIBILITY.Translucent: Editor._rigGUIOption_NoLinkedBoneVisibility = apEditor.NOLINKED_BONE_VISIBILITY.Hidden; break;
							case apEditor.NOLINKED_BONE_VISIBILITY.Hidden: Editor._rigGUIOption_NoLinkedBoneVisibility = apEditor.NOLINKED_BONE_VISIBILITY.Opaque; break;
						}
					}
					Editor.SaveEditorPref();//<<이것도 Save 요건
				}



				GUILayout.Space(10);
				apEditorUtil.GUI_DelimeterBoxV(height - 6);
				GUILayout.Space(10);

				//"  Pose Test", "  Pose Test"
				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_TestPosing),
													2, Editor.GetUIWord(UIWORD.RigPoseTest), Editor.GetUIWord(UIWORD.RigPoseTest),
													_rigEdit_isTestPosing, _rigEdit_isBindingEdit, 130, height,
													apStringFactory.I.RiggingViewModeTooltip_TestPose))//"Enable/Disable Pose Test Mode"
				{
					_rigEdit_isTestPosing = !_rigEdit_isTestPosing;

					SetBoneRiggingTest();

				}

				if (GUILayout.Button(Editor.GetUIWord(UIWORD.RigResetPose), apGUILOFactory.I.Width(120), apGUILOFactory.I.Height(height)))//"Reset Pose"
				{
					ResetRiggingTestPose();
				}
			}
			else if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Physic)
			{
				//테스트로 시뮬레이션을 할 수 있다.
				//바람을 켜고 끌 수 있다.
				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(100), apGUILOFactory.I.Height(height));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.PxDirection), apGUILOFactory.I.Width(100));//"Direction"
				_physics_windSimulationDir = apEditorUtil.DelayedVector2Field(_physics_windSimulationDir, 100 - 4);
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(100), apGUILOFactory.I.Height(height));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.PxPower), apGUILOFactory.I.Width(100));//"Power"
				_physics_windSimulationScale = EditorGUILayout.DelayedFloatField(_physics_windSimulationScale, apGUILOFactory.I.Width(100));
				EditorGUILayout.EndVertical();

				//"Wind On"

				if (_guiContent_Bottom2_Physic_WindON == null)
				{
					_guiContent_Bottom2_Physic_WindON = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.PxWindOn), false, apStringFactory.I.SimulateWindForce);//"Simulate wind force"
				}

				if (_guiContent_Bottom2_Physic_WindOFF == null)
				{
					_guiContent_Bottom2_Physic_WindOFF = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.PxWindOff), false, apStringFactory.I.ClearWindForce);//"Clear wind force"
				}

				if (GUILayout.Button(_guiContent_Bottom2_Physic_WindON.Content, apGUILOFactory.I.Width(110), apGUILOFactory.I.Height(height)))
				{
					GUI.FocusControl(null);

					if (_portrait != null)
					{
						_portrait.ClearForce();
						_portrait.AddForce_Direction(_physics_windSimulationDir,
							0.3f,
							0.3f,
							3, 5)
							.SetPower(_physics_windSimulationScale, _physics_windSimulationScale * 0.3f, 4.0f)
							.EmitLoop();
					}
				}
				//"Wind Off"
				if (GUILayout.Button(_guiContent_Bottom2_Physic_WindOFF.Content, apGUILOFactory.I.Width(110), apGUILOFactory.I.Height(height)))
				{
					GUI.FocusControl(null);
					if (_portrait != null)
					{
						_portrait.ClearForce();
					}
				}


			}

			return;
		}
		//------------------------------------------------------------------------------------


		public void DrawEditor_Bottom(int width, int height, int layoutX, int layoutY, int windowWidth, int windowHeight)
		{
			if (Editor == null || Editor.Select.Portrait == null)
			{
				return;
			}

			switch (_selectionType)
			{
				case SELECTION_TYPE.Animation:
					{
						DrawEditor_Bottom_Animation(width, height, layoutX, layoutY, windowWidth, windowHeight);
					}
					break;


				case SELECTION_TYPE.MeshGroup:
					{

					}
					break;
			}

			return;
		}




		private void DrawEditor_Bottom_Animation(int width, int height, int layoutX, int layoutY, int windowWidth, int windowHeight)
		{
			//좌우 두개의 탭으로 나뉜다. [타임라인 - 선택된 객체 정보]
			int rightTabWidth = 300;
			int margin = 5;
			int mainTabWidth = width - (rightTabWidth + margin);
			Rect lastRect = GUILayoutUtility.GetLastRect();

			List<apTimelineLayerInfo> timelineInfoList = Editor.TimelineInfoList;
			apTimelineLayerInfo nextSelectLayerInfo = null;

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height));
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(mainTabWidth), apGUILOFactory.I.Height(height));
			//1. [좌측] 타임라인 레이아웃

			//1-1 요약부 : [레코드] + [타임과 통합 키프레임]
			//1-2 메인 타임라인 : [레이어] + [타임라인 메인]
			//1-3 하단 컨트롤과 스크롤 : [컨트롤러] + [스크롤 + 애니메이션 설정]
			int leftTabWidth = 280;
			int timelineWidth = mainTabWidth - (leftTabWidth + 4);

			if (Event.current.type == EventType.Repaint)
			{
				_timlineGUIWidth = timelineWidth;
			}

			//int recordAndSummaryHeight = 45;
			int recordAndSummaryHeight = 70;
			int bottomControlHeight = 54;
			int timelineHeight = height - (recordAndSummaryHeight + bottomControlHeight + 4);
			int guiHeight = height - bottomControlHeight;


			//레이어의 높이
			int heightPerTimeline = 24;
			int heightPerLayer = 28;//조금 작게 만들자

			//>>원래 자동 스크롤 코드가 있던 위치




			//if(Editor._timelineLayoutSize == apEditor.TIMELINE_LAYOUTSIZE.Size1)
			//{
			//	guiHeight = viewAndSummaryHeight;
			//}

			bool isDrawMainTimeline = (Editor._timelineLayoutSize != apEditor.TIMELINE_LAYOUTSIZE.Size1);

			//스크롤 값을 넣어주자
			int startFrame = AnimClip.StartFrame;
			int endFrame = AnimClip.EndFrame;
			int widthPerFrame = Editor.WidthPerFrameInTimeline;
			int nFrames = Mathf.Max((endFrame - startFrame) + 1, 1);
			int widthForTotalFrame = nFrames * widthPerFrame;
			int widthForScrollFrame = widthForTotalFrame;

			int timelineLayoutSize_Min = 0;
			int timelineLayoutSize_Max = Editor._timelineZoomWPFPreset.Length - 1;



			//세로 스크롤 영역에 대한 처리 (20.4.14)
			//스크롤값에 따라서 일부 타임라인 레이어의 GUI가 렌더링되지 않는데,
			//이때문에 GUI 출력 순서가 바뀌어서 세로 스크롤의 GUI의 포커싱이 풀리는 문제가 있다. (스크롤 도중에 스크롤바 인식이 풀리는 문제)
			//그래서 스크롤하는 도중에는 미리 Down이벤트를 감지하여 높이에 따른 렌더링 생략 로직을 피하고 모두 렌더링해야한다.
			Rect timelineVerticalScrollRect = new Rect(	lastRect.x + leftTabWidth + 4 + timelineWidth - 15,
														lastRect.y,
														15,
														timelineHeight + recordAndSummaryHeight + 4);
			if(Event.current.type == EventType.MouseDown)
			{
				Vector2 mousePos = Event.current.mousePosition;
				Vector2 rectPosRange_Min = new Vector2(timelineVerticalScrollRect.x, timelineVerticalScrollRect.y);
				Vector2 rectPosRange_Max = new Vector2(timelineVerticalScrollRect.x + timelineVerticalScrollRect.width, timelineVerticalScrollRect.y + timelineVerticalScrollRect.height);
				if(mousePos.x >= rectPosRange_Min.x && mousePos.x <= rectPosRange_Max.x
					&& mousePos.y >= rectPosRange_Min.y && mousePos.y <= rectPosRange_Max.y)
				{
					//스크롤바를 움직일 것입니더
					//스크롤바를 움직이는 동안에는 모든 레이어가 출력되어야 한다.
					//Debug.LogError("Start Scrolling");
					_isScrollingTimelineY = true;
				}
			}
			if(_isScrollingTimelineY)
			{
				//스크롤을 중단시키자
				//다른 요소를 이용했거나 Up 이벤트 발생시
				if(Event.current.type == EventType.Used
					|| Event.current.rawType == EventType.Used
					|| Event.current.type == EventType.MouseUp
					|| Event.current.rawType == EventType.MouseUp)
				{
					//Debug.LogWarning("End Scrolling");
					_isScrollingTimelineY = false;
				}
			}

			//출력할 레이어 개수

			//삭제 19.11.22
			//int timelineLayers = Mathf.Max(10, Editor.TimelineInfoList.Count);
			//int heightForScrollLayer = (timelineLayers * heightPerLayer);

			//이벤트가 발생했다면 Repaint하자
			bool isEventOccurred = false;


			//GL에 크기값을 넣어주자
			apTimelineGL.SetLayoutSize(timelineWidth, recordAndSummaryHeight, timelineHeight,
											layoutX + leftTabWidth,
											layoutY, layoutY + recordAndSummaryHeight,
											windowWidth, windowHeight,
											isDrawMainTimeline, _scroll_Timeline);

			//GL에 마우스 값을 넣고 업데이트를 하자

			bool isLeftBtnPressed = false;
			bool isRightBtnPressed = false;

			if (Event.current.rawType == EventType.MouseDown ||
				Event.current.rawType == EventType.MouseDrag)
			{
				if (Event.current.button == 0) { isLeftBtnPressed = true; }
				else if (Event.current.button == 1) { isRightBtnPressed = true; }
			}

#if UNITY_EDITOR_OSX
			bool isCtrl = Event.current.command;
#else
			bool isCtrl = Event.current.control;
#endif

			apTimelineGL.SetMouseValue(isLeftBtnPressed,
										isRightBtnPressed,
										//apMouse.PosNotBound,//이전
										Editor.Mouse.PosNotBound,//이후
										Event.current.shift, isCtrl, Event.current.alt,
										Event.current.rawType,
										this);


			//TODO

			//GUI의 배경 색상
			Color prevColor = GUI.backgroundColor;
			if (EditorGUIUtility.isProSkin)
			{
				GUI.backgroundColor = new Color(Editor._guiMainEditorColor.r * 0.8f,
										Editor._guiMainEditorColor.g * 0.8f,
										Editor._guiMainEditorColor.b * 0.8f,
										1.0f);
			}
			else
			{
				GUI.backgroundColor = Editor._guiMainEditorColor;
			}

			Rect timelineRect = new Rect(lastRect.x + leftTabWidth + 4, lastRect.y, timelineWidth, guiHeight + 15);
			GUI.Box(timelineRect, apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box);

			if (EditorGUIUtility.isProSkin)
			{
				GUI.backgroundColor = new Color(Editor._guiSubEditorColor.r * 0.8f,
										Editor._guiSubEditorColor.g * 0.8f,
										Editor._guiSubEditorColor.b * 0.8f,
										1.0f);
			}
			else
			{
				GUI.backgroundColor = Editor._guiSubEditorColor;
			}

			Rect timelineBottomRect = new Rect(lastRect.x + leftTabWidth + 4, lastRect.y + guiHeight + 15, timelineWidth, height - (guiHeight));
			GUI.Box(timelineBottomRect, apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box);

			GUI.backgroundColor = prevColor;

			//추가 : 하단 GUI도 넣어주자

			bool isWheelDrag = false;
			//마우스 휠 이벤트를 직접 주자
			if (Event.current.rawType == EventType.ScrollWheel)
			{
				//휠 드르륵..
				Vector2 mousePos = Event.current.mousePosition;

				if (mousePos.x > 0 && mousePos.x < lastRect.x + leftTabWidth + timelineWidth &&
					mousePos.y > lastRect.y + recordAndSummaryHeight && mousePos.y < lastRect.y + guiHeight)
				{
					if (isCtrl)
					{
						//추가 20.4.14 : Ctrl을 누른 상태로 휠을 돌리면 확대/축소가 된다.
						if(Event.current.delta.y > 0)
						{
							Editor._timelineZoom_Index = Mathf.Clamp(Editor._timelineZoom_Index - 1, timelineLayoutSize_Min, timelineLayoutSize_Max);
						}
						else if(Event.current.delta.y < 0)
						{
							Editor._timelineZoom_Index = Mathf.Clamp(Editor._timelineZoom_Index + 1, timelineLayoutSize_Min, timelineLayoutSize_Max);
						}
					}
					else
					{
						//Ctrl를 누르지 않고 휠을 돌리면 상하좌우로 스크롤된다.
						_scroll_Timeline += Event.current.delta * 7;

					}

					Event.current.Use();
					apTimelineGL.SetMouseUse();

					isEventOccurred = true;

					//클릭시 GUI 포커스 날림
					apEditorUtil.ReleaseGUIFocus();//추가 : 19.11.23
				}
			}

			if (Event.current.isMouse && Event.current.type != EventType.Used)
			{
				//휠 클릭 후 드래그
				if (Event.current.button == 2)
				{
					
					if (Event.current.type == EventType.MouseDown)
					{
						Vector2 mousePos = Event.current.mousePosition;

						if (mousePos.x > leftTabWidth && mousePos.x < lastRect.x + leftTabWidth + timelineWidth &&
							mousePos.y > lastRect.y + recordAndSummaryHeight && mousePos.y < lastRect.y + guiHeight)
						{
							//휠클릭 드래그 시작
							_isTimelineWheelDrag = true;
							_prevTimelineWheelDragPos = mousePos;

							isWheelDrag = true;
							Event.current.Use();
							apTimelineGL.SetMouseUse();

							isEventOccurred = true;

							//클릭시 GUI 포커스 날림
							apEditorUtil.ReleaseGUIFocus();//추가 : 19.11.23

							//Debug.LogError("Mouse Input > Use");
						}
					}
					else if (Event.current.type == EventType.MouseDrag && _isTimelineWheelDrag)
					{
						Vector2 mousePos = Event.current.mousePosition;
						Vector2 deltaPos = mousePos - _prevTimelineWheelDragPos;

						//_scroll_Timeline -= deltaPos * 1.0f;
						_scroll_Timeline.x -= deltaPos.x * 1.0f;//X만 움직이자

						_prevTimelineWheelDragPos = mousePos;
						isWheelDrag = true;
						Event.current.Use();
						apTimelineGL.SetMouseUse();

						isEventOccurred = true;

						//Debug.LogError("Mouse Input > Use");
					}
				}
			}

			if (!isWheelDrag && Event.current.isMouse)
			{
				_isTimelineWheelDrag = false;
			}

			// ┌──┬─────┬──┐
			// │ㅁㅁ│	  v      │ inf│
			// ├──┼─────┤    │
			// │~~~~│  ㅁ  ㅁ  │    │
			// │~~~~│    ㅁ    │    │
			// ├──┼─────┤    │
			// │ >  │Zoom      │    │
			// └──┴─────┴──┘

			//1-1 요약부 : [레코드] + [타임과 통합 키프레임]
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(mainTabWidth), apGUILOFactory.I.Height(recordAndSummaryHeight));

			int animEditBtnGroupHeight = 30;

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(leftTabWidth), apGUILOFactory.I.Height(recordAndSummaryHeight));
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(leftTabWidth), apGUILOFactory.I.Height(animEditBtnGroupHeight));
			GUILayout.Space(5);

			//Texture2D imgAutoKey = null;
			//if (IsAnimAutoKey)	{ imgAutoKey = Editor.ImageSet.Get(apImageSet.PRESET.Anim_KeyOn); }
			//else					{ imgAutoKey = Editor.ImageSet.Get(apImageSet.PRESET.Anim_KeyOff); }

			Texture2D imgKeyLock = null;
			if (IsAnimSelectionLock) { imgKeyLock = Editor.ImageSet.Get(apImageSet.PRESET.Edit_SelectionLock); }
			else { imgKeyLock = Editor.ImageSet.Get(apImageSet.PRESET.Edit_SelectionUnlock); }

			Texture2D imgLayerLock = null;
			if (ExAnimEditingMode == EX_EDIT.General_Edit) { imgLayerLock = Editor.ImageSet.Get(apImageSet.PRESET.Edit_ModUnlock); }
			else { imgLayerLock = Editor.ImageSet.Get(apImageSet.PRESET.Edit_ModLock); }

			Texture2D imgAddKeyframe = Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddKeyframe);

			// 요약부 + 왼쪽의 [레코드] 부분
			//1. Start / Stop Editing (Toggle)
			//2. Auto Key (Toggle)
			//3. Set Key
			//4. Lock (Toggle)로 이루어져 있다.


			Texture2D editIcon = null;
			string strButtonName = null;
			bool isEditable = false;


			if (ExAnimEditingMode != EX_EDIT.None)
			{
				//현재 애니메이션 수정 작업중이라면..
				editIcon = Editor.ImageSet.Get(apImageSet.PRESET.Edit_Recording);
				//strButtonName = " Editing";
				strButtonName = Editor.GetUIWord(UIWORD.EditingAnim);
				isEditable = true;
			}
			else
			{
				//현재 애니메이션 수정 작업을 하고 있지 않다면..
				if (IsAnimEditable)
				{
					editIcon = Editor.ImageSet.Get(apImageSet.PRESET.Edit_Record);
					//strButtonName = " Start Edit";
					strButtonName = Editor.GetUIWord(UIWORD.StartEdit);
					isEditable = true;
				}
				else
				{
					editIcon = Editor.ImageSet.Get(apImageSet.PRESET.Edit_NoRecord);
					//strButtonName = " No-Editable";
					strButtonName = Editor.GetUIWord(UIWORD.NoEditable);
				}
			}



			// Anim 편집 On/Off
			//Animation Editing On / Off
			if (apEditorUtil.ToggledButton_2Side_LeftAlign(editIcon, 1, strButtonName, strButtonName, ExAnimEditingMode != EX_EDIT.None, isEditable, 105, animEditBtnGroupHeight, apStringFactory.I.AnimationEditModeToggleTooltip))//"Animation Edit Mode (A)"
			{
				//AnimEditing을 On<->Off를 전환하고 기즈모 이벤트를 설정한다.
				SetAnimEditingToggle();

				//추가 : 19.11.23
				apEditorUtil.ReleaseGUIFocus();
			}

			//2개의 Lock 버튼
			if (apEditorUtil.ToggledButton_2Side(imgKeyLock, IsAnimSelectionLock, ExAnimEditingMode != EX_EDIT.None, 35, animEditBtnGroupHeight, apStringFactory.I.SelectionLockToggleTooltip))//"Selection Lock/Unlock (S)"
			{
				_isAnimSelectionLock = !_isAnimSelectionLock;

				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.None, null);
			}



			//#if UNITY_EDITOR_OSX
			//			string strCtrlKey = "Command";
			//#else
			//			string strCtrlKey = "Ctrl";
			//#endif

			if (apEditorUtil.ToggledButton_2Side_Ctrl(	imgLayerLock,
														ExAnimEditingMode == EX_EDIT.ExOnly_Edit,
														ExAnimEditingMode != EX_EDIT.None,
														35,
														animEditBtnGroupHeight,
														apStringFactory.I.TimelineLayerLockToggleTooltip,
														//"Timeline Layer Lock/Unlock (D) / If you press the button while holding down [" + strCtrlKey + "], the Setting dialog opens",
														Event.current.control,
														Event.current.command
													))
			{
				//변경 3.22 : Ctrl 키를 누르고 클릭하면 설정 Dialog가 뜬다.

				if (isCtrl)
				{
					apDialog_ModifierLockSetting.ShowDialog(Editor, _portrait);
				}
				else
				{
					SetAnimEditingLayerLockToggle();//Mod Layer Lock을 토글
				}
			}

			//"Add Key"
			if (apEditorUtil.ToggledButton_2Side(imgAddKeyframe, Editor.GetUIWord(UIWORD.AddKey), Editor.GetUIWord(UIWORD.AddKey), false, ExAnimEditingMode != EX_EDIT.None, 85, animEditBtnGroupHeight, apStringFactory.I.AddKeyframe))//"Add Keyframe"
			{
				//Debug.LogError("TODO : Set Key");
				if (AnimTimelineLayer != null)
				{
					apAnimKeyframe addedKeyframe = Editor.Controller.AddAnimKeyframe(AnimClip.CurFrame, AnimTimelineLayer, true);
					if (addedKeyframe != null)
					{
						//프레임을 이동하자
						_animClip.SetFrame_Editor(addedKeyframe._frameIndex);
						SetAnimKeyframe(addedKeyframe, true, apGizmos.SELECT_TYPE.New);

						//추가 : 자동 스크롤
						AutoSelectAnimTimelineLayer(true, false);

						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
				}
			}



			//단축키 [A]로 Editing 상태 토글
			//단축키 [S]에 의해서 Seletion Lock을 켜고 끌 수 있다.
			//단축키 [D]로 Layer Lock을 토글
			Editor.AddHotKeyEvent(OnHotKey_AnimEditingToggle, apHotKey.LabelText.ToggleEditingMode, KeyCode.A, false, false, false, null);//"Toggle Editing Mode"
			Editor.AddHotKeyEvent(OnHotKey_AnimSelectionLockToggle, apHotKey.LabelText.ToggleSelectionLock, KeyCode.S, false, false, false, null);//"Toggle Selection Lock"
			Editor.AddHotKeyEvent(OnHotKey_AnimLayerLockToggle, apHotKey.LabelText.ToggleLayerLock, KeyCode.D, false, false, false, null);//"Toggle Layer Lock"
			Editor.AddHotKeyEvent(OnHotKey_AnimAddKeyframe, apHotKey.LabelText.AddNewKeyframe, KeyCode.F, false, false, false, null);//"Add New Keyframe"


			//if(GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_AutoZoom), GUILayout.Width(30), GUILayout.Height(30)))

			EditorGUILayout.EndHorizontal();

			//"Add Keyframes to All Layers"
			if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.AddKeyframesToAllLayers), Editor.GetUIWord(UIWORD.AddKeyframesToAllLayers), false, ExAnimEditingMode != EX_EDIT.None, leftTabWidth - (10), 20))
			{
				//현재 프레임의 모든 레이어에 Keyframe을 추가한다.
				//이건 다이얼로그로 꼭 물어보자
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.AddKeyframeToAllLayer_Title),
														Editor.GetText(TEXT.AddKeyframeToAllLayer_Body),
														Editor.GetText(TEXT.Okay),
														Editor.GetText(TEXT.Cancel));

				if (isResult)
				{
					Editor.Controller.AddAnimKeyframeToAllLayer(AnimClip.CurFrame, AnimClip, true);
				}

			}

			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(timelineWidth), apGUILOFactory.I.Height(recordAndSummaryHeight));

			// 요약부 + 오른쪽의 [시간 / 통합 키 프레임]
			// 이건 GUI로 해야한다.
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();



			//1-2 메인 타임라인 : [레이어] + [타임라인 메인]
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(mainTabWidth), apGUILOFactory.I.Height(timelineHeight));

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(leftTabWidth), apGUILOFactory.I.Height(timelineHeight));
			GUILayout.BeginArea(new Rect(lastRect.x, lastRect.y + recordAndSummaryHeight, leftTabWidth, timelineHeight));
			// 메인 + 왼쪽의 [레이어] 부분

			// 레이어에 대한 렌더링 (정보 부분)
			//--------------------------------------------------------------
			int nTimelines = AnimClip._timelines.Count;
			//apAnimTimeline curTimeline = null;


			//삭제 19.11.22
			//GUIStyle guiStyle_layerInfoBox = new GUIStyle(GUI.skin.label);
			//guiStyle_layerInfoBox.alignment = TextAnchor.MiddleLeft;
			//guiStyle_layerInfoBox.padding = GUI.skin.button.padding;

			//int baseLeftPadding = GUI.skin.button.padding.left;

			//변경 19.11.22
			GUIStyle curGuiStyle_layerInfoBox = null;//최적화 코드
			bool isLeftPaddingAdded = false;
			int textColorType = 0;//Black / White / Gray

			int btnWidth_Layer = leftTabWidth + 4;
			//Texture2D img_HideLayer = Editor.ImageSet.Get(apImageSet.PRESET.Anim_HideLayer);
			Texture2D img_TimelineFolded = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_FoldRight);
			Texture2D img_TimelineNotFolded = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_FoldDown);

			Texture2D img_CurFold = null;

			//삭제 19.11.22
			//GUIStyle guiStyle_LeftBtn = new GUIStyle(GUI.skin.button);
			//guiStyle_LeftBtn.padding = new RectOffset(0, 0, 0, 0);


			//추가
			if (_guiContent_Bottom_Animation_TimelineLayerInfo == null)
			{
				_guiContent_Bottom_Animation_TimelineLayerInfo = new apGUIContentWrapper();
			}


			int curLayerY = 0;
			int totalTimelineInfoHeight = 0;
			//int nRealDrawInfo = 0;


			//1. 타임라인/타임라인 레이어의 왼쪽의 이름 리스트를 출력하자.
			//추가로, 현재 렌더링되는 LayerInfo의 전체 Height를 계산하자
			for (int iLayer = 0; iLayer < timelineInfoList.Count; iLayer++)
			{
				apTimelineLayerInfo info = timelineInfoList[iLayer];

				//일단 렌더링 여부를 초기화한다.
				//Layer Info는 GUI에서 그리도록 하고, 나중에 TimelineGL에서 렌더링을 할지 결정한다.
				info._isRenderable = false;

				if (!info._isTimeline && !info.IsVisibleLayer)
				{
					//숨겨진 레이어이다.
					info._guiLayerPosY = 0.0f;
					continue;
				}
				int layerHeight = heightPerLayer;
				//int leftPadding = baseLeftPadding + 20;
				isLeftPaddingAdded = true;
				if (info._isTimeline)
				{
					layerHeight = heightPerTimeline;
					//leftPadding = baseLeftPadding;
					isLeftPaddingAdded = false;
				}

				//배경 / 텍스트 색상을 정하자
				Color layerBGColor = info.GUIColor;
				//Color textColor = Color.black;
				textColorType = 0;//<<0 : Black, 1 : White, 2 : Gray

				info._guiLayerPosY = curLayerY;

				//float relativeY = info._guiLayerPosY - _scroll_Timeline.y;
				//if (relativeY + layerHeight < 0 || relativeY > timelineHeight)
				if(_isScrollingTimelineY)
				{
					//우측 세로 스크롤중에는 다 보여야 한다. (유니티 이벤트때문에)
					//(20.4.14)
					info._isRenderable = true;
				}
				else
				{
					if (info._guiLayerPosY < _scroll_Timeline.y - layerHeight
						|| info._guiLayerPosY > _scroll_Timeline.y + timelineHeight)
					{
						//렌더링 영역 바깥에 있다.
						//TimelineGL에서는 출력하지 않도록 한다. 에디터가 빨라지겠져
						info._isRenderable = false;
					}
					else
					{
						info._isRenderable = true;
					}
				}


				//변경 19.11.22 : 여기서부터는 "렌더링되는 Info"만 렌더링하자
				if (info._isRenderable)
				{
					//nRealDrawInfo++;

					if (!info._isAvailable)
					{
						//textColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
						textColorType = 2;
					}
					else
					{
						float grayScale = (layerBGColor.r + layerBGColor.g + layerBGColor.b) / 3.0f;
						if (grayScale < 0.3f)
						{
							//textColor = Color.white;
							textColorType = 1;
						}
					}

					//아이콘을 결정하자

					//guiStyle_layerInfoBox.normal.textColor = textColor;
					//guiStyle_layerInfoBox.padding.left = leftPadding;
					switch (textColorType)
					{
						case 0://Black
							curGuiStyle_layerInfoBox = (isLeftPaddingAdded ? apGUIStyleWrapper.I.Label_MiddleLeft_BtnPadding_Left20_BlackColor : apGUIStyleWrapper.I.Label_MiddleLeft_BtnPadding_BlackColor);
							break;

						case 1://White
							curGuiStyle_layerInfoBox = (isLeftPaddingAdded ? apGUIStyleWrapper.I.Label_MiddleLeft_BtnPadding_Left20_WhiteColor : apGUIStyleWrapper.I.Label_MiddleLeft_BtnPadding_WhiteColor);
							break;

						case 2://Gray
						default:
							curGuiStyle_layerInfoBox = (isLeftPaddingAdded ? apGUIStyleWrapper.I.Label_MiddleLeft_BtnPadding_Left20_GrayColor : apGUIStyleWrapper.I.Label_MiddleLeft_BtnPadding_GrayColor);
							break;
					}


					//이전
					//Texture2D layerIcon = Editor.ImageSet.Get(info.IconImgType);

					//변경
					_guiContent_Bottom_Animation_TimelineLayerInfo.SetText(2, info.DisplayName);
					_guiContent_Bottom_Animation_TimelineLayerInfo.SetImage(Editor.ImageSet.Get(info.IconImgType));



					//[ 레이어 선택 ]

					if (info._isTimeline)
					{
						GUI.backgroundColor = layerBGColor;
						GUI.Box(new Rect(0, curLayerY - _scroll_Timeline.y, btnWidth_Layer, layerHeight), apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box);

						int yOffset = (layerHeight - 18) / 2;

						if (info.IsTimelineFolded)
						{
							img_CurFold = img_TimelineFolded;
						}
						else
						{
							img_CurFold = img_TimelineNotFolded;
						}
						if (GUI.Button(new Rect(2, (curLayerY + yOffset) - _scroll_Timeline.y, 18, 18), img_CurFold, apGUIStyleWrapper.I.Button_Margin0))
						{
							if (info._timeline != null)
							{
								info._timeline._guiTimelineFolded = !info._timeline._guiTimelineFolded;
							}
						}

						GUI.backgroundColor = prevColor;

						if (GUI.Button(new Rect(19, curLayerY - _scroll_Timeline.y, btnWidth_Layer, layerHeight),
										//new GUIContent("  " + info.DisplayName, layerIcon), //이전
										_guiContent_Bottom_Animation_TimelineLayerInfo.Content,//변경
										curGuiStyle_layerInfoBox))
						{
							nextSelectLayerInfo = info;//<<선택!
						}
					}
					else
					{
						//[ Hide 버튼]
						//int xOffset = (btnWidth_Layer - (layerHeight + 4)) + 2;
						//int xOffset = 18;
						int yOffset = (layerHeight - 18) / 2;

						GUI.backgroundColor = layerBGColor;
						GUI.Box(new Rect(0, curLayerY - _scroll_Timeline.y, btnWidth_Layer, layerHeight), apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box);

						if (GUI.Button(new Rect(2, (curLayerY + yOffset) - _scroll_Timeline.y, 18, 18), apStringFactory.I.Minus, apGUIStyleWrapper.I.Button_Margin0))
						{
							//Hide
							info._layer._guiLayerVisible = false;//<<숨기자!
						}

						GUI.backgroundColor = prevColor;


						//2 + 18 + 2 = 22
						if (GUI.Button(new Rect(19, curLayerY - _scroll_Timeline.y, btnWidth_Layer - 22, layerHeight),
										//new GUIContent("  " + info.DisplayName, layerIcon), //이전
										_guiContent_Bottom_Animation_TimelineLayerInfo.Content,//변경
										curGuiStyle_layerInfoBox))
						{
							nextSelectLayerInfo = info;//<<선택!
						}
					}
				}

				curLayerY += layerHeight;
				totalTimelineInfoHeight += layerHeight;//전체 높이도 계산하자
			}

			//너무 작다면 크게 바꾸어야 한다
			totalTimelineInfoHeight = Mathf.Max(totalTimelineInfoHeight, heightPerLayer * 10);//레이어 10개분 정도는 기본적으로 나와야 한다.
			totalTimelineInfoHeight += 50;
			//Debug.Log("Current Draw : " + nRealDrawInfo);


			//--------------------------------------------------------------

			GUILayout.EndArea();
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(timelineWidth), apGUILOFactory.I.Height(timelineHeight));
			GUILayout.BeginArea(timelineRect);
			// 메인 + 오른쪽의 [메인 타임라인]
			// 이건 GUI로 해야한다.

			//기본 타임라인 GL 세팅
			apTimelineGL.SetTimelineSetting(0, AnimClip.StartFrame, AnimClip.EndFrame, Editor.WidthPerFrameInTimeline, AnimClip.IsLoop);
			//apTimelineGL.DrawTimeBars_Header(new Color(0.4f, 0.4f, 0.4f, 1.0f));

			// 레이어에 대한 렌더링 (타임라인 부분 - BG)
			//--------------------------------------------------------------
			curLayerY = 0;
			for (int iLayer = 0; iLayer < timelineInfoList.Count; iLayer++)
			{
				apTimelineLayerInfo info = timelineInfoList[iLayer];
				int layerHeight = heightPerLayer;

				if (!info._isTimeline && !info.IsVisibleLayer)
				{
					continue;
				}
				if (info._isTimeline)
				{
					layerHeight = heightPerTimeline;
				}
				if (info._isSelected)
				{
					apTimelineGL.DrawTimeBars_MainBG(info.TimelineColor, curLayerY + layerHeight - (int)_scroll_Timeline.y, layerHeight);
				}
				curLayerY += layerHeight;
			}

			//Grid를 그린다.
			apTimelineGL.DrawTimelineAreaBG(ExAnimEditingMode != EX_EDIT.None);
			apTimelineGL.DrawTimeGrid(new Color(0.4f, 0.4f, 0.4f, 1.0f), new Color(0.3f, 0.3f, 0.3f, 1.0f), new Color(0.7f, 0.7f, 0.7f, 1.0f));
			apTimelineGL.DrawTimeBars_Header(new Color(0.4f, 0.4f, 0.4f, 1.0f));



			// 레이어에 대한 렌더링 (타임라인 부분 - Line + Frames)
			//--------------------------------------------------------------

			//추가 : 커브 편집에 관한 데이터
			bool isSelectedAnimTimeline = false;
			bool isSingleCurveEdit = false;
			bool isCurveEdit = false;
			int iMultiCurveType = 0;
			if (IsAnimKeyframeMultipleSelected)
			{
				//키프레임 여러개 편집중인 경우
				isSingleCurveEdit = false;
				isCurveEdit = true;
				switch (_animPropertyCurveUI_Multi)
				{
					case ANIM_MULTI_PROPERTY_CURVE_UI.Prev: iMultiCurveType = 0; break;
					case ANIM_MULTI_PROPERTY_CURVE_UI.Middle: iMultiCurveType = 1; break;
					case ANIM_MULTI_PROPERTY_CURVE_UI.Next: iMultiCurveType = 2; break;
				}

				//isSelectedAnimTimeline = _animTimelineCommonCurve.IsSelectedTimelineLayer(info._layer);
			}
			else
			{
				//1개의 커브를 편집 중(이거나 없거나)
				isSingleCurveEdit = true;
				isCurveEdit = (_animPropertyUI == ANIM_SINGLE_PROPERTY_UI.Curve);
			}

			curLayerY = 0;
			bool isAnyHidedLayer = false;
			apTimelineGL.BeginKeyframeControl();

			for (int iLayer = 0; iLayer < timelineInfoList.Count; iLayer++)
			{
				apTimelineLayerInfo info = timelineInfoList[iLayer];
				if (!info._isTimeline && !info.IsVisibleLayer)
				{
					//숨겨진 레이어
					isAnyHidedLayer = true;
					continue;
				}
				int layerHeight = heightPerLayer;
				if (info._isTimeline)
				{
					layerHeight = heightPerTimeline;
				}

				if (info._isRenderable)
				{
					apTimelineGL.DrawTimeBars_MainLine(new Color(0.3f, 0.3f, 0.3f, 1.0f), curLayerY + layerHeight - (int)_scroll_Timeline.y);

					if (!info._isTimeline)
					{
						Color curveEditColor = Color.black;

						//커브를 여러개 편집 중인지 아닌지 확인
						if (isSingleCurveEdit)
						{
							//단일 키프레임 편집 중일때
							isSelectedAnimTimeline = (AnimTimelineLayer == info._layer);

						}
						else
						{
							//여러개의 키프레임을 편집 중일때
							isSelectedAnimTimeline = _animTimelineCommonCurve.IsSelectedTimelineLayer(info._layer);
						}

						apTimelineGL.DrawKeyframes(info._layer,
													curLayerY + layerHeight / 2,
													info.GUIColor,
													info._isAvailable,
													layerHeight,
													AnimClip.CurFrame,
													isSelectedAnimTimeline,
													isCurveEdit,
													isSingleCurveEdit,
													_animPropertyCurveUI,
													iMultiCurveType,
													_animTimelineCommonCurve
													//curveEditColor
													);
					}
				}
				curLayerY += layerHeight;


			}



			// Play Bar를 그린다.
			//int prevClipFrame = AnimClip.CurFrame;
			//bool isAutoRefresh = false;
			apTimelineGL.DrawKeySummry(_subAnimCommonKeyframeList, 58);
			apTimelineGL.DrawEventMarkers(_animClip._animEvents, 30);

			if (Editor.Onion.IsVisible)
			{
				if (Editor._onionOption_IsRenderAnimFrames)
				{
					//영역 Onion인 경우
					int animLength = (AnimClip.EndFrame - AnimClip.StartFrame) + 1;
					int renderPerFrame = Mathf.Max(Editor._onionOption_RenderPerFrame, 0);
					if (animLength >= 1 && renderPerFrame > 0)
					{
						int prevRange = Mathf.Clamp(Editor._onionOption_PrevRange, 0, animLength / 2);
						int nextRange = Mathf.Clamp(Editor._onionOption_NextRange, 0, animLength / 2);

						prevRange = (prevRange / renderPerFrame) * renderPerFrame;
						nextRange = (nextRange / renderPerFrame) * renderPerFrame;

						int minFrame = AnimClip.CurFrame - prevRange;
						int maxFrame = AnimClip.CurFrame + nextRange;

						if (AnimClip.IsLoop)
						{
							if (minFrame < AnimClip.StartFrame) { minFrame = (minFrame + animLength) - 1; }
							if (maxFrame > AnimClip.EndFrame) { maxFrame = (maxFrame - animLength) + 1; }
						}
						minFrame = Mathf.Clamp(minFrame, AnimClip.StartFrame, AnimClip.EndFrame);
						maxFrame = Mathf.Clamp(maxFrame, AnimClip.StartFrame, AnimClip.EndFrame);

						if (prevRange > 0)
						{
							apTimelineGL.DrawOnionMarkers(minFrame,
													new Color(Mathf.Clamp01(Editor._colorOption_OnionAnimPrevColor.r * 2),
																Mathf.Clamp01(Editor._colorOption_OnionAnimPrevColor.g * 2),
																Mathf.Clamp01(Editor._colorOption_OnionAnimPrevColor.b * 2),
																1.0f),
													44, 1);
						}
						if (nextRange > 0)
						{
							apTimelineGL.DrawOnionMarkers(maxFrame,
													new Color(Mathf.Clamp01(Editor._colorOption_OnionAnimNextColor.r * 2),
																Mathf.Clamp01(Editor._colorOption_OnionAnimNextColor.g * 2),
																Mathf.Clamp01(Editor._colorOption_OnionAnimNextColor.b * 2),
																1.0f),
													44, 2);
						}
					}
				}
				else if (Editor.Onion.IsRecorded)
				{
					apTimelineGL.DrawOnionMarkers(Editor.Onion.RecordAnimFrame,
													new Color(Mathf.Clamp01(Editor._colorOption_OnionToneColor.r * 2),
																Mathf.Clamp01(Editor._colorOption_OnionToneColor.g * 2),
																Mathf.Clamp01(Editor._colorOption_OnionToneColor.b * 2),
																1.0f),
													44, 0);
				}


			}


			bool isChangeFrame = apTimelineGL.DrawPlayBar(AnimClip.CurFrame);
			if (isChangeFrame)
			{
				AutoSelectAnimWorkKeyframe();
				//isAutoRefresh = true;

				apEditorUtil.ReleaseGUIFocus();//추가 : 19.11.23

				
			}

			bool isKeyframeEvent = apTimelineGL.EndKeyframeControl();//<<제어용 함수
			if (isKeyframeEvent) { isEventOccurred = true; }

			//if(prevClipFrame != AnimClip.CurFrame)
			//{
			//	Debug.Log("Frame Changed [" + isAutoRefresh + "] : " + (AnimWorkKeyframe != null));
			//}

			apTimelineGL.DrawAndUpdateSelectArea();

			//키프레임+타임 슬라이더를 화면 끝으로 이동한 경우 자동 스크롤
			if (apTimelineGL.IsKeyframeDragging)
			{
				float rightBound = timelineRect.xMin + (timelineWidth - rightTabWidth) + 30;
				float leftBound = 30;
				//if(apMouse.Pos.x > rightBound || apMouse.Pos.x < leftBound)//이전
				if (Editor.Mouse.Pos.x > rightBound || Editor.Mouse.Pos.x < leftBound)//이후
				{
					_animKeyframeAutoScrollTimer += apTimer.I.DeltaTime_Repaint;
					if (_animKeyframeAutoScrollTimer > 0.1f)
					{
						_animKeyframeAutoScrollTimer = 0.0f;
						AutoAnimScrollWithoutFrameMoving(apTimelineGL.FrameOnMouseX, 1);
						apTimelineGL.RefreshScrollDown();
					}
				}

			}


			//--------------------------------------------------------------

			GUILayout.EndArea();
			EditorGUILayout.EndVertical();


			EditorGUILayout.EndHorizontal();


			//스크롤은 현재 키프레임의 범위, 레이어의 개수에 따라 바뀐다.
			

			//float prevScrollTimelineY = _scroll_Timeline.y;
			
			//if(Event.current.type == EventType.Layout)
			//{
			//	//_scroll_Timeline_DummyY = _scroll_Timeline.y;
			//}

			//추가 20.4.14 : 세로 스크롤을 하면 타임라인에서 보여지는 레이어가 바뀌는데(최적화때문에)
			//이때 GUI 호출 순서가 바뀌면서 이 스크롤바가 연속으로 인식되지 않아서 스크롤 도중에 입력이 풀리는 문제가 있다.
			//세로 스크롤바의 영역을 클릭했다면, 마우스가 Up되기 전에는 모든 리스트가 보여져야 한다.
			//단, 이 체크는 위에서 해야한다.

			
			_scroll_Timeline.y = GUI.VerticalScrollbar(
													//new Rect(	lastRect.x + leftTabWidth + 4 + timelineWidth - 15,
													//				lastRect.y,
													//				15,
													//				timelineHeight + recordAndSummaryHeight + 4),
													timelineVerticalScrollRect,
														_scroll_Timeline.y,
														50.0f,
														0.0f,
														//heightForScrollLayer//이전
														totalTimelineInfoHeight//변경 19.11.22
														);

			//_scroll_Timeline.y = _scroll_Timeline_DummyY;

			//if(string.Equals(GUI.GetNameOfFocusedControl(), GUI_NAME_TIMELINE_SCROLL_Y))
			//{
			//	Debug.LogWarning("Timeline Scrolling [" + _scroll_Timeline.y + "] (" + Event.current.type + ")");
			//}

			//??? 이게 왜 있는거지?
			//if (Mathf.Abs(prevScrollTimelineY - _scroll_Timeline.y) > 0.5f)
			//{
			//	//Debug.Log("Scroll Y");
			//	//Event.current.Use();
			//	//apEditorUtil.ReleaseGUIFocus();
			//	apTimelineGL.SetMouseUse();
			//}

			//Anim 레이어를 선택하자
			if (nextSelectLayerInfo != null)
			{
				_isIgnoreAnimTimelineGUI = true;//<깜빡이지 않게..
				if (nextSelectLayerInfo._isTimeline)
				{
					//Timeline을 선택하기 전에
					//Anim객체를 초기화한다. (안그러면 자동으로 선택된 오브젝트에 의해서 TimelineLayer를 선택하게 된다.)
					SetBoneForAnimClip(null, false, false);
					SetSubControlParamForAnimClipEdit(null, false, false);
					SetSubMeshTransformForAnimClipEdit(null, false, false);
					SetSubMeshGroupTransformForAnimClipEdit(null, false, false);

					SetAnimTimeline(nextSelectLayerInfo._timeline, true, true);
					SetAnimTimelineLayer(null, true, true, true);
					SetAnimKeyframe(null, false, apGizmos.SELECT_TYPE.New);

					AutoSelectAnimTimelineLayer(false);
				}
				else
				{
					SetAnimTimeline(nextSelectLayerInfo._parentTimeline, true, true);
					SetAnimTimelineLayer(nextSelectLayerInfo._layer, true, true, true);
					SetAnimKeyframe(null, false, apGizmos.SELECT_TYPE.New);
				}
				AutoSelectAnimWorkKeyframe();

				Editor.RefreshControllerAndHierarchy(false);
			}

			//float prevScrollTimelineX = _scroll_Timeline.x;
			
			_scroll_Timeline.x = GUI.HorizontalScrollbar(new Rect(lastRect.x + leftTabWidth + 4, lastRect.y + recordAndSummaryHeight + timelineHeight + 4, timelineWidth - 15, 15),
															_scroll_Timeline.x,
															20.0f, 0.0f,
															widthForScrollFrame);

			//이것도 왜 있는거지??
			//if (Mathf.Abs(prevScrollTimelineX - _scroll_Timeline.x) > 0.5f)
			//{
			//	//Debug.Log("Scroll X");
			//	Event.current.Use();
			//	apTimelineGL.SetMouseUse();
			//}

			if (GUI.Button(new Rect(lastRect.x + leftTabWidth + 4 + timelineWidth - 15, lastRect.y + recordAndSummaryHeight + timelineHeight + 4, 15, 15), apStringFactory.I.None))
			{
				_scroll_Timeline.x = 0;
				_scroll_Timeline.y = 0;
			}

			//1-3 하단 컨트롤과 스크롤 : [컨트롤러] + [스크롤 + 애니메이션 설정]
			int ctrlBtnSize_Small = 30;
			int ctrlBtnSize_Large = 30;
			int ctrlBtnSize_LargeUnder = bottomControlHeight - (ctrlBtnSize_Large + 6);

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(mainTabWidth), apGUILOFactory.I.Height(bottomControlHeight));
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(leftTabWidth), apGUILOFactory.I.Height(bottomControlHeight));
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(leftTabWidth), apGUILOFactory.I.Height(ctrlBtnSize_Large + 2));
			GUILayout.Space(5);


			//플레이 제어 단축키
			//단축키 [<, >]로 키프레임을 이동할 수 있다.
			// Space : 재생/정지
			// <, > : 1프레임 이동
			// Shift + <, > : 첫프레임, 끝프레임으로 이동
			Editor.AddHotKeyEvent(OnHotKey_AnimMoveFrame, apHotKey.LabelText.PlayPause, KeyCode.Space, false, false, false, 0);//"Play/Pause"
			Editor.AddHotKeyEvent(OnHotKey_AnimMoveFrame, apHotKey.LabelText.PreviousFrame, KeyCode.Comma, false, false, false, 1);//"Previous Frame"
			Editor.AddHotKeyEvent(OnHotKey_AnimMoveFrame, apHotKey.LabelText.NextFrame, KeyCode.Period, false, false, false, 2);//"Next Frame"
			Editor.AddHotKeyEvent(OnHotKey_AnimMoveFrame, apHotKey.LabelText.FirstFrame, KeyCode.Comma, true, false, false, 3);//"First Frame"
			Editor.AddHotKeyEvent(OnHotKey_AnimMoveFrame, apHotKey.LabelText.LastFrame, KeyCode.Period, true, false, false, 4);//"Last Frame"

			//추가 3.29 : 키프레임 선택해서 복사, 붙여넣기
			if (_subAnimKeyframeList != null && _subAnimKeyframeList.Count > 0)
			{
				//타임라인에서 선택된 키프레임들이 있을 때 복사하기
				Editor.AddHotKeyEvent(OnHotKey_AnimCopyKeyframes, apHotKey.LabelText.CopyKeyframes, KeyCode.C, false, false, true, null);//"Copy Keyframes"
			}
			//if (apSnapShotManager.I.IsKeyframesPastableOnTimelineUI(AnimClip))
			{
				//현재 AnimClip에 키프레임들을 Ctrl+V로 복사할 수 있다면..
				Editor.AddHotKeyEvent(OnHotKey_AnimPasteKeyframes, apHotKey.LabelText.PasteKeyframes, KeyCode.V, false, false, true, null);//"Paste Keyframes"
			}

			if (_guiContent_Bottom_Animation_FirstFrame == null) { _guiContent_Bottom_Animation_FirstFrame = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_FirstFrame), "Move to First Frame (Shift + <)"); }
			if (_guiContent_Bottom_Animation_PrevFrame == null) { _guiContent_Bottom_Animation_PrevFrame = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_PrevFrame), "Move to Previous Frame (<)"); }
			if (_guiContent_Bottom_Animation_Play == null) { _guiContent_Bottom_Animation_Play = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_Play), "Play (Space Bar)"); }
			if (_guiContent_Bottom_Animation_Pause == null) { _guiContent_Bottom_Animation_Pause = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_Pause), "Pause (Space Bar)"); }
			if (_guiContent_Bottom_Animation_NextFrame == null) { _guiContent_Bottom_Animation_NextFrame = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_NextFrame), "Move to Next Frame (>)"); }
			if (_guiContent_Bottom_Animation_LastFrame == null) { _guiContent_Bottom_Animation_LastFrame = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_LastFrame), "Move to Last Frame (Shift + >)"); }


			//플레이 제어
			//if (GUILayout.Button(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Anim_FirstFrame), "Move to First Frame (Shift + <)"), GUILayout.Width(ctrlBtnSize_Large), GUILayout.Height(ctrlBtnSize_Large)))
			if (GUILayout.Button(_guiContent_Bottom_Animation_FirstFrame.Content, apGUILOFactory.I.Width(ctrlBtnSize_Large), apGUILOFactory.I.Height(ctrlBtnSize_Large)))
			{
				//제어 : 첫 프레임으로 이동
				AnimClip.SetFrame_Editor(AnimClip.StartFrame);
				AutoSelectAnimWorkKeyframe();

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
			}

			//if (GUILayout.Button(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Anim_PrevFrame), "Move to Previous Frame (<)"), GUILayout.Width(ctrlBtnSize_Large + 10), GUILayout.Height(ctrlBtnSize_Large)))
			if (GUILayout.Button(_guiContent_Bottom_Animation_PrevFrame.Content, apGUILOFactory.I.Width(ctrlBtnSize_Large + 10), apGUILOFactory.I.Height(ctrlBtnSize_Large)))
			{
				//제어 : 이전 프레임으로 이동
				int prevFrame = AnimClip.CurFrame - 1;
				if (prevFrame < AnimClip.StartFrame)
				{
					if (AnimClip.IsLoop)
					{
						prevFrame = AnimClip.EndFrame;
					}
				}
				AnimClip.SetFrame_Editor(prevFrame);
				AutoSelectAnimWorkKeyframe();

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
			}

			//Texture2D playIcon = null;

			//if (AnimClip.IsPlaying_Editor)
			//{
			//	//플레이중 -> Pause 버튼
			//	//playIcon = Editor.ImageSet.Get(apImageSet.PRESET.Anim_Pause);
			//}
			//else
			//{
			//	//일시 정지 -> 플레이 버튼
			//	//playIcon = Editor.ImageSet.Get(apImageSet.PRESET.Anim_Play);
			//}

			apGUIContentWrapper curPlayPauseGUIContent = AnimClip.IsPlaying_Editor ? _guiContent_Bottom_Animation_Pause : _guiContent_Bottom_Animation_Play;

			//if (GUILayout.Button(new GUIContent(playIcon, "Play/Pause (Space Bar)"), GUILayout.Width(ctrlBtnSize_Large + 30), GUILayout.Height(ctrlBtnSize_Large)))
			if (GUILayout.Button(curPlayPauseGUIContent.Content, apGUILOFactory.I.Width(ctrlBtnSize_Large + 30), apGUILOFactory.I.Height(ctrlBtnSize_Large)))
			{
				//제어 : 플레이 / 일시정지
				if (AnimClip.IsPlaying_Editor)
				{
					// 플레이 -> 일시 정지
					AnimClip.Pause_Editor();
				}
				else
				{
					//마지막 프레임이라면 첫 프레임으로 이동하여 재생한다.
					if (AnimClip.CurFrame == AnimClip.EndFrame)
					{
						AnimClip.SetFrame_Editor(AnimClip.StartFrame);
						Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
					}
					// 일시 정지 -> 플레이
					AnimClip.Play_Editor();
				}

				//Play 전환 여부에 따라서도 WorkKeyframe을 전환한다.
				AutoSelectAnimWorkKeyframe();
				Editor.SetRepaint();
				Editor.Gizmos.SetUpdate();

			}

			//if (GUILayout.Button(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Anim_NextFrame), "Move to Next Frame (>)"), GUILayout.Width(ctrlBtnSize_Large + 10), GUILayout.Height(ctrlBtnSize_Large)))
			if (GUILayout.Button(_guiContent_Bottom_Animation_NextFrame.Content, apGUILOFactory.I.Width(ctrlBtnSize_Large + 10), apGUILOFactory.I.Height(ctrlBtnSize_Large)))
			{
				//제어 : 다음 프레임으로 이동
				int nextFrame = AnimClip.CurFrame + 1;
				if (nextFrame > AnimClip.EndFrame)
				{
					if (AnimClip.IsLoop)
					{
						nextFrame = AnimClip.StartFrame;
					}
				}
				AnimClip.SetFrame_Editor(nextFrame);
				AutoSelectAnimWorkKeyframe();

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
			}

			//if (GUILayout.Button(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Anim_LastFrame), "Move to Last Frame (Shift + >)"), GUILayout.Width(ctrlBtnSize_Large), GUILayout.Height(ctrlBtnSize_Large)))
			if (GUILayout.Button(_guiContent_Bottom_Animation_LastFrame.Content, apGUILOFactory.I.Width(ctrlBtnSize_Large), apGUILOFactory.I.Height(ctrlBtnSize_Large)))
			{
				//제어 : 마지막 프레임으로 이동
				AnimClip.SetFrame_Editor(AnimClip.EndFrame);
				AutoSelectAnimWorkKeyframe();

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
			}

			GUILayout.Space(10);
			bool isLoopPlay = AnimClip.IsLoop;
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Anim_Loop), isLoopPlay, true, ctrlBtnSize_Large, ctrlBtnSize_Large, apStringFactory.I.ToggleAnimLoop))//"Enable/Disable Loop"
			{
				if(AnimClip._targetMeshGroup != null)
				{
					apEditorUtil.SetRecord_PortraitMeshGroup(	apUndoGroupData.ACTION.Anim_SettingChanged,
																Editor, 
																Editor._portrait,
																AnimClip._targetMeshGroup,
																AnimClip,
																false,
																false);
				}
				else
				{
					apEditorUtil.SetRecord_Portrait(	apUndoGroupData.ACTION.Anim_SettingChanged,
														Editor, 
														Editor._portrait,
														AnimClip,
														false);
				}
				
				//AnimClip._isLoop = !AnimClip._isLoop;
				AnimClip.SetOption_IsLoop(!AnimClip.IsLoop);
				AnimClip.SetFrame_Editor(AnimClip.CurFrame);

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.

				//Loop를 바꿨다면 전체 Sort를 해야겠다.
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Timelines | apEditor.REFRESH_TIMELINE_REQUEST.LinkKeyframeAndModifier, null);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(leftTabWidth), apGUILOFactory.I.Height(ctrlBtnSize_LargeUnder + 2));

			GUILayout.Space(5);

			//현재 프레임 + 세밀 조정
			//"Frame"
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Frame), apGUILOFactory.I.Width(80), apGUILOFactory.I.Height(ctrlBtnSize_LargeUnder));
			int curFrame = AnimClip.CurFrame;
			int nextCurFrame = EditorGUILayout.IntSlider(curFrame, AnimClip.StartFrame, AnimClip.EndFrame, apGUILOFactory.I.Width(leftTabWidth - 95), apGUILOFactory.I.Height(ctrlBtnSize_LargeUnder));
			if (nextCurFrame != curFrame)
			{

				AnimClip.SetFrame_Editor(nextCurFrame);
				AutoSelectAnimWorkKeyframe();

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(mainTabWidth - leftTabWidth), apGUILOFactory.I.Height(bottomControlHeight));
			GUILayout.Space(18);
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(mainTabWidth - leftTabWidth), apGUILOFactory.I.Height(bottomControlHeight - 18));

			//>>여기서부터

			//맨 하단은 키 복붙이나 View, 영역 등에 관련된 정보를 출력한다.
			GUILayout.Space(10);

			//Timeline 정렬
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_SortRegOrder),
											Editor._timelineInfoSortType == apEditor.TIMELINE_INFO_SORT.Registered,
											true, ctrlBtnSize_Small, ctrlBtnSize_Small,
											apStringFactory.I.AnimTimelineSort_RegOrder))//"Sort by registeration order"
			{
				Editor._timelineInfoSortType = apEditor.TIMELINE_INFO_SORT.Registered;
				Editor.SaveEditorPref();
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Info | apEditor.REFRESH_TIMELINE_REQUEST.Timelines, null);
			}

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_SortABC),
											Editor._timelineInfoSortType == apEditor.TIMELINE_INFO_SORT.ABC,
											true, ctrlBtnSize_Small, ctrlBtnSize_Small,
											apStringFactory.I.AnimTImelineSort_Name))//"Sort by name"
			{
				Editor._timelineInfoSortType = apEditor.TIMELINE_INFO_SORT.ABC;
				Editor.SaveEditorPref();
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Info | apEditor.REFRESH_TIMELINE_REQUEST.Timelines, null);
			}

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_SortDepth),
											Editor._timelineInfoSortType == apEditor.TIMELINE_INFO_SORT.Depth,
											true, ctrlBtnSize_Small, ctrlBtnSize_Small,
											apStringFactory.I.AnimTimelineSort_Depth))//"Sort by Depth"
			{
				Editor._timelineInfoSortType = apEditor.TIMELINE_INFO_SORT.Depth;
				Editor.SaveEditorPref();
				Editor.RefreshTimelineLayers(apEditor.REFRESH_TIMELINE_REQUEST.Info | apEditor.REFRESH_TIMELINE_REQUEST.Timelines, null);

			}

			GUILayout.Space(20);

			//"Unhide Layers"
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.UnhideLayers), !isAnyHidedLayer, 120, ctrlBtnSize_Small))
			{
				Editor.ShowAllTimelineLayers();
			}

			GUILayout.Space(20);



			// 타임라인 사이즈 (1, 2, 3)
			apEditor.TIMELINE_LAYOUTSIZE nextLayoutSize = Editor._timelineLayoutSize;

			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_TimelineSize1),
											Editor._timelineLayoutSize == apEditor.TIMELINE_LAYOUTSIZE.Size1,
											true, ctrlBtnSize_Small, ctrlBtnSize_Small,
											apStringFactory.I.AnimTimelineSize_Small))//"Timeline UI Size [Small]"
			{
				nextLayoutSize = apEditor.TIMELINE_LAYOUTSIZE.Size1;
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_TimelineSize2),
											Editor._timelineLayoutSize == apEditor.TIMELINE_LAYOUTSIZE.Size2,
											true, ctrlBtnSize_Small, ctrlBtnSize_Small,
											apStringFactory.I.AnimTimelineSize_Medium))//"Timeline UI Size [Medium]"
			{
				nextLayoutSize = apEditor.TIMELINE_LAYOUTSIZE.Size2;
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Anim_TimelineSize3),
											Editor._timelineLayoutSize == apEditor.TIMELINE_LAYOUTSIZE.Size3,
											true, ctrlBtnSize_Small, ctrlBtnSize_Small,
											apStringFactory.I.AnimTimelineSize_Large))//"Timeline UI Size [Large]"
			{
				nextLayoutSize = apEditor.TIMELINE_LAYOUTSIZE.Size3;
			}


			//Zoom
			GUILayout.Space(4);

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(90));
			//EditorGUILayout.LabelField("Zoom", GUILayout.Width(100), GUILayout.Height(15));
			GUILayout.Space(7);
			

			int nextTimelineIndex = (int)(GUILayout.HorizontalSlider(Editor._timelineZoom_Index, timelineLayoutSize_Min, timelineLayoutSize_Max, apGUILOFactory.I.Width(90), apGUILOFactory.I.Height(20)) + 0.5f);
			if (nextTimelineIndex != Editor._timelineZoom_Index)
			{
				if (nextTimelineIndex < timelineLayoutSize_Min) { nextTimelineIndex = timelineLayoutSize_Min; }
				else if (nextTimelineIndex > timelineLayoutSize_Max) { nextTimelineIndex = timelineLayoutSize_Max; }

				Editor._timelineZoom_Index = nextTimelineIndex;
			}
			EditorGUILayout.EndVertical();

			//Fit은 유지

			//if (GUILayout.Button(new GUIContent(" Fit", Editor.ImageSet.Get(apImageSet.PRESET.Anim_AutoZoom), "Zoom to fit the animation length"),
			//						GUILayout.Width(80), GUILayout.Height(ctrlBtnSize_Small)))

			if (_guiContent_Bottom_Animation_Fit == null)
			{
				//" Fit" / "Zoom to fit the animation length"
				_guiContent_Bottom_Animation_Fit = apGUIContentWrapper.Make(apStringFactory.I.AnimTimelineFit, Editor.ImageSet.Get(apImageSet.PRESET.Anim_AutoZoom), apStringFactory.I.AnimTimelineFitTooltip);
			}

			if (GUILayout.Button(_guiContent_Bottom_Animation_Fit.Content, apGUILOFactory.I.Width(80), apGUILOFactory.I.Height(ctrlBtnSize_Small)))
			{
				//Debug.LogError("TODO : Timeline AutoZoom");
				//Width / 전체 Frame수 = 목표 WidthPerFrame
				int numFrames = Mathf.Max(AnimClip.EndFrame - AnimClip.StartFrame, 1);
				int targetWidthPerFrame = (int)((float)timelineWidth / (float)numFrames + 0.5f);
				_scroll_Timeline.x = 0;
				//적절한 값을 찾자
				int optWPFIndex = -1;
				for (int i = 0; i < Editor._timelineZoomWPFPreset.Length; i++)
				{
					int curWPF = Editor._timelineZoomWPFPreset[i];
					if (curWPF < targetWidthPerFrame)
					{
						optWPFIndex = i;
						break;
					}
				}
				if (optWPFIndex < 0)
				{
					Editor._timelineZoom_Index = Editor._timelineZoomWPFPreset.Length - 1;
				}
				else
				{
					Editor._timelineZoom_Index = optWPFIndex;
				}
			}

			GUILayout.Space(4);

			//Auto Scroll
			//" Auto Scroll"
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Anim_AutoScroll),
													1, Editor.GetUIWord(UIWORD.AutoScroll), Editor.GetUIWord(UIWORD.AutoScroll),
													Editor._isAnimAutoScroll, true,
													140, ctrlBtnSize_Small,
													apStringFactory.I.AnimTimelineAutoScrollTooltip))//"Scrolls automatically according to the frame of the animation"
			{
				Editor._isAnimAutoScroll = !Editor._isAnimAutoScroll;
			}


			//AutoKey
			GUILayout.Space(6);
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Anim_AutoKey),
													1, Editor.GetUIWord(UIWORD.AutoKey), Editor.GetUIWord(UIWORD.AutoKey),
													Editor._isAnimAutoKey, true,
													140, ctrlBtnSize_Small,
													apStringFactory.I.AnimTimelineAutoKeyTooltip))//"When you move the object, keyframes are automatically created"
			{
				Editor._isAnimAutoKey = !Editor._isAnimAutoKey;
			}


			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();


			EditorGUILayout.EndHorizontal();


			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(rightTabWidth), apGUILOFactory.I.Height(height));
			//2. [우측] 선택된 레이어/키 정보

			_scrollPos_BottomAnimationRightProperty = EditorGUILayout.BeginScrollView(_scrollPos_BottomAnimationRightProperty, false, true, apGUILOFactory.I.Width(rightTabWidth), apGUILOFactory.I.Height(height));

			//int rightPropertyWidth = rightTabWidth - 24;
			int rightPropertyWidth = rightTabWidth - 28;

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(rightPropertyWidth));


			//프로퍼티 타이틀
			//프로퍼티는 (KeyFrame -> Layer -> Timeline -> None) 순으로 정보를 보여준다.
			//string propertyTitle = "";

			if (_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}
			_strWrapper_64.Clear();

			int propertyType = 0;
			if (AnimKeyframe != null)
			{
				if (IsAnimKeyframeMultipleSelected)
				{
					//propertyTitle = "Keyframes [ " + AnimKeyframes.Count + " Selected ]";
					//propertyTitle = string.Format("{0} [ {1} {2} ]", Editor.GetUIWord(UIWORD.Keyframes), AnimKeyframes.Count, Editor.GetUIWord(UIWORD.Selected));

					_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Keyframes), false);
					_strWrapper_64.AppendSpace(1, false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
					_strWrapper_64.Append(AnimKeyframes.Count, false);
					_strWrapper_64.AppendSpace(1, false);
					_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Selected), false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

					propertyType = 1;
				}
				else
				{
					//propertyTitle = "Keyframe [ " + AnimKeyframe._frameIndex + " ]";
					//propertyTitle = string.Format("{0} [ {1} ]", Editor.GetUIWord(UIWORD.Keyframe), AnimKeyframe._frameIndex);

					_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Keyframe), false);
					_strWrapper_64.AppendSpace(1, false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
					_strWrapper_64.Append(AnimKeyframe._frameIndex, false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

					propertyType = 2;
				}

			}
			else if (AnimTimelineLayer != null)
			{
				//propertyTitle = "Layer [" + AnimTimelineLayer.DisplayName + " ]";
				//propertyTitle = string.Format("{0} [ {1} ]", Editor.GetUIWord(UIWORD.Layer), AnimTimelineLayer.DisplayName);

				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Layer), false);
				_strWrapper_64.AppendSpace(1, false);
				_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
				_strWrapper_64.Append(AnimTimelineLayer.DisplayName, false);
				_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

				propertyType = 3;
			}
			else if (AnimTimeline != null)
			{
				//propertyTitle = "Timeline [ " + AnimTimeline.DisplayName + " ]";
				//propertyTitle = string.Format("{0} [ {1} ]", Editor.GetUIWord(UIWORD.Timeline), AnimTimeline.DisplayName);

				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Timeline), false);
				_strWrapper_64.AppendSpace(1, false);
				_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
				_strWrapper_64.Append(AnimTimeline.DisplayName, false);
				_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

				propertyType = 4;
			}
			else
			{
				//propertyTitle = "Not Selected";
				//propertyTitle = Editor.GetUIWord(UIWORD.NotSelected);
				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.NotSelected), true);
			}

			//GUIStyle guiStyleProperty = new GUIStyle(GUI.skin.box);
			//guiStyleProperty.normal.textColor = Color.white;
			//guiStyleProperty.alignment = TextAnchor.MiddleCenter;

			//GUI.backgroundColor = new Color(0.0f, 0.2f, 0.3f, 1.0f);
			GUI.backgroundColor = apEditorUtil.ToggleBoxColor_Selected;

			GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_WhiteColor, apGUILOFactory.I.Width(rightPropertyWidth), apGUILOFactory.I.Height(20));
			GUI.backgroundColor = prevColor;

			GUILayout.Space(5);


			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__MK, propertyType == 1);//"Animation Bottom Property - MK"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__SK, propertyType == 2);//"Animation Bottom Property - SK"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__L, propertyType == 3);//"Animation Bottom Property - L"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__T, propertyType == 4);//"Animation Bottom Property - T"

			switch (propertyType)
			{
				case 1:

					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__MK))//"Animation Bottom Property - MK"
					{
						DrawEditor_Bottom_AnimationProperty_MultipleKeyframes(AnimKeyframes, rightPropertyWidth,
							windowWidth,
							windowHeight,
							(layoutX + leftTabWidth + margin + mainTabWidth + margin),
							(int)(layoutY),
							(int)(_scrollPos_BottomAnimationRightProperty.y));
					}
					break;

				case 2:
					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__SK))//"Animation Bottom Property - SK"
					{
						DrawEditor_Bottom_AnimationProperty_SingleKeyframe(
							AnimKeyframe,
							rightPropertyWidth,
							windowWidth,
							windowHeight,
							(layoutX + leftTabWidth + margin + mainTabWidth + margin),
							//layoutX + margin + mainTabWidth + margin, 
							//leftTabWidth + margin + mainTabWidth + margin, 
							(int)(layoutY),
							(int)(_scrollPos_BottomAnimationRightProperty.y)
							//(int)(layoutY)
							);
					}
					break;

				case 3:
					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__L))//"Animation Bottom Property - L"
					{
						DrawEditor_Bottom_AnimationProperty_TimelineLayer(AnimTimelineLayer, rightPropertyWidth);
					}
					break;

				case 4:
					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Animation_Bottom_Property__T))//"Animation Bottom Property - T"
					{
						DrawEditor_Bottom_AnimationProperty_Timeline(AnimTimeline, rightPropertyWidth);
					}
					break;
			}



			GUILayout.Space(height);

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();


			EditorGUILayout.EndHorizontal();



			//자동 스크롤 이벤트 요청이 들어왔다.
			//처리를 해주자
			if (_isAnimTimelineLayerGUIScrollRequest)
			{
				//_scroll_Timeline.y
				//일단 어느 TimelineInfo인지 찾고,
				//그 값으로 이동
				apTimelineLayerInfo targetInfo = null;
				if (_subAnimTimelineLayer != null)
				{
					targetInfo = timelineInfoList.Find(delegate (apTimelineLayerInfo a)
					{
						return a._layer == _subAnimTimelineLayer && a.IsVisibleLayer;
					});
				}
				//삭제 : 19.11.22 : 타임라인으로는 자동으로 스크롤 되지 않는다.
				//else if (_subAnimTimeline != null)
				//{
				//	targetInfo = timelineInfoList.Find(delegate (apTimelineLayerInfo a)
				//	{
				//		return a._timeline == _subAnimTimeline && a._isTimeline;
				//	});
				//}


				if (targetInfo != null)
				{
					//화면 밖에 있는 경우에 한해서
					//위쪽에 있으면 위쪽으로, 아니면 아래쪽으로 설정하자
					if (Editor._timelineLayoutSize != apEditor.TIMELINE_LAYOUTSIZE.Size1)
					{
						//타임라인의 크기가 1이 아니라면 항목 위치에 따라서 스크롤을 조절
						if (targetInfo._guiLayerPosY < _scroll_Timeline.y - (heightPerLayer + 3))
						{

							//스크롤보다 위쪽 (작은 인덱스)인 경우
							_scroll_Timeline.y = targetInfo._guiLayerPosY - heightPerLayer;
							if (_scroll_Timeline.y < 0.0f)
							{
								_scroll_Timeline.y = 0.0f;
							}
						}
						else if (targetInfo._guiLayerPosY > _scroll_Timeline.y + (timelineHeight + 3))
						{
							//스크롤보다 아래쪽 (큰 인덱스)인 경우
							//중간으로 올린다.
							_scroll_Timeline.y = targetInfo._guiLayerPosY - ((timelineHeight * 0.5f) + heightPerLayer);
							if (_scroll_Timeline.y < 0.0f)
							{
								_scroll_Timeline.y = 0.0f;
							}
						}
					}
					else
					{
						//타임라인의 크기가 1이라면 그냥 매번 고정
						_scroll_Timeline.y = targetInfo._guiLayerPosY - heightPerLayer;
						if (_scroll_Timeline.y < 0.0f)
						{
							_scroll_Timeline.y = 0.0f;
						}
					}

					//Debug.LogError("Timeline Y Changed : " + GUI.GetNameOfFocusedControl());
				}

				_isAnimTimelineLayerGUIScrollRequest = false;
				Editor.SetRepaint();//<<화면 갱신 요청
			}




			if (Editor._timelineLayoutSize != nextLayoutSize)
			{
				Editor._timelineLayoutSize = nextLayoutSize;
			}

			if (isEventOccurred)
			{
				Editor.SetRepaint();
			}
		}



		//화면 우측의 UI 중 : 키프레임을 "1개 선택할 때" 출력되는 UI
		private void DrawEditor_Bottom_AnimationProperty_SingleKeyframe(apAnimKeyframe keyframe, int width, int windowWidth, int windowHeight, int layoutX, int layoutY, int scrollValue)
		{
			//TODO : 커브 조절


			//프레임 이동
			//EditorGUILayout.LabelField("Frame [" + keyframe._frameIndex + "]", GUILayout.Width(width));
			//GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));

			GUILayout.Space(5);

			Texture2D imgPrev = Editor.ImageSet.Get(apImageSet.PRESET.Anim_MoveToPrevFrame);
			Texture2D imgNext = Editor.ImageSet.Get(apImageSet.PRESET.Anim_MoveToNextFrame);
			Texture2D imgCurKey = Editor.ImageSet.Get(apImageSet.PRESET.Anim_MoveToCurrentFrame);

			int btnWidthSide = ((width - (10 + 80)) / 2) - 4;
			int btnWidthCenter = 90;
			bool isPrevKey = false;
			bool isNextKey = false;
			bool isCurKey = (AnimClip.CurFrame == keyframe._frameIndex);
			if (keyframe._prevLinkedKeyframe != null)
			{
				isPrevKey = true;
			}
			if (keyframe._nextLinkedKeyframe != null)
			{
				isNextKey = true;
			}

			if (apEditorUtil.ToggledButton_2Side(imgPrev, false, isPrevKey, btnWidthSide, 20))
			{
				//연결된 이전 프레임으로 이동한다.
				if (isPrevKey)
				{
					AnimClip.SetFrame_Editor(keyframe._prevLinkedKeyframe._frameIndex);
					SetAnimKeyframe(keyframe._prevLinkedKeyframe, true, apGizmos.SELECT_TYPE.New);

					Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
				}
			}
			if (apEditorUtil.ToggledButton_2Side(imgCurKey, isCurKey, true, btnWidthCenter, 20))
			{
				//현재 프레임으로 이동한다.
				AnimClip.SetFrame_Editor(keyframe._frameIndex);
				AutoSelectAnimWorkKeyframe();
				SetAutoAnimScroll();

				Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
			}
			if (apEditorUtil.ToggledButton_2Side(imgNext, false, isNextKey, btnWidthSide, 20))
			{
				//연결된 다음 프레임으로 이동한다.
				if (isNextKey)
				{
					AnimClip.SetFrame_Editor(keyframe._nextLinkedKeyframe._frameIndex);
					SetAnimKeyframe(keyframe._nextLinkedKeyframe, true, apGizmos.SELECT_TYPE.New);

					Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.

					Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
				}
			}


			EditorGUILayout.EndHorizontal();


			//Value / Curve에 따라서 다른 UI가 나온다.
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(22));
			GUILayout.Space(5);
			//"Transform"
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Transform),
											(_animPropertyUI == ANIM_SINGLE_PROPERTY_UI.Value),
											(width / 2) - 2
										))
			{
				_animPropertyUI = ANIM_SINGLE_PROPERTY_UI.Value;
			}
			//"Curve"
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Curve),
											(_animPropertyUI == ANIM_SINGLE_PROPERTY_UI.Curve),
											(width / 2) - 2
										))
			{
				_animPropertyUI = ANIM_SINGLE_PROPERTY_UI.Curve;
			}

			EditorGUILayout.EndHorizontal();


			//키프레임 타입인 경우
			bool isControlParamUI = (AnimTimeline._linkType == apAnimClip.LINK_TYPE.ControlParam &&
									AnimTimelineLayer._linkedControlParam != null);
			bool isModifierUI = (AnimTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier &&
									AnimTimeline._linkedModifier != null);

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Bottom_Right_Anim_Property__ControlParamUI, isControlParamUI);//"Bottom Right Anim Property - ControlParamUI"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Bottom_Right_Anim_Property__ModifierUI, isModifierUI);//"Bottom Right Anim Property - ModifierUI"

			bool isDrawControlParamUI = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Bottom_Right_Anim_Property__ControlParamUI);//"Bottom Right Anim Property - ControlParamUI"
			bool isDrawModifierUI = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Bottom_Right_Anim_Property__ModifierUI);//"Bottom Right Anim Property - ModifierUI"


			apControlParam controlParam = AnimTimelineLayer._linkedControlParam;

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Anim_Property__SameKeyframe, _tmpPrevSelectedAnimKeyframe == keyframe);//"Anim Property - SameKeyframe"
			bool isSameKP = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Anim_Property__SameKeyframe);//"Anim Property - SameKeyframe"

			//if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
			if (Event.current.type != EventType.Layout)
			{
				_tmpPrevSelectedAnimKeyframe = keyframe;
			}

			Color prevColor = GUI.backgroundColor;


			if (_animPropertyUI == ANIM_SINGLE_PROPERTY_UI.Value)
			{
				//1. Value Mode
				if (isDrawControlParamUI && isSameKP)
				{
					#region Control Param UI 그리는 코드
					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width);
					GUILayout.Space(10);

					GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);

					//GUIStyle guiStyleBox = new GUIStyle(GUI.skin.box);
					//guiStyleBox.alignment = TextAnchor.MiddleCenter;
					//guiStyleBox.normal.textColor = apEditorUtil.BoxTextColor;

					//"Control Parameter Value"
					GUILayout.Box(Editor.GetUIWord(UIWORD.ControlParameterValue),
									apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor,
									apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

					GUI.backgroundColor = prevColor;


					//GUIStyle guiStyle_LableMin = new GUIStyle(GUI.skin.label);
					//guiStyle_LableMin.alignment = TextAnchor.MiddleLeft;

					//GUIStyle guiStyle_LableMax = new GUIStyle(GUI.skin.label);
					//guiStyle_LableMax.alignment = TextAnchor.MiddleRight;

					int widthLabelRange = (width / 2) - 2;

					GUILayout.Space(5);

					bool isChanged = false;

					switch (controlParam._valueType)
					{
						case apControlParam.TYPE.Int:
							{
								int iNext = keyframe._conSyncValue_Int;

								EditorGUILayout.LabelField(controlParam._keyName, apGUILOFactory.I.Width(width));
								EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
								EditorGUILayout.LabelField(controlParam._label_Min, apGUIStyleWrapper.I.Label_MiddleLeft, apGUILOFactory.I.Width(widthLabelRange));
								EditorGUILayout.LabelField(controlParam._label_Max, apGUIStyleWrapper.I.Label_MiddleRight, apGUILOFactory.I.Width(widthLabelRange));
								EditorGUILayout.EndHorizontal();
								iNext = EditorGUILayout.IntSlider(keyframe._conSyncValue_Int, controlParam._int_Min, controlParam._int_Max, apGUILOFactory.I.Width(width));


								if (iNext != keyframe._conSyncValue_Int)
								{
									apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, Editor._portrait, keyframe, true);

									keyframe._conSyncValue_Int = iNext;
									isChanged = true;
								}
							}
							break;

						case apControlParam.TYPE.Float:
							{
								float fNext = keyframe._conSyncValue_Float;

								EditorGUILayout.LabelField(controlParam._keyName, apGUILOFactory.I.Width(width));
								EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
								EditorGUILayout.LabelField(controlParam._label_Min, apGUIStyleWrapper.I.Label_MiddleLeft, apGUILOFactory.I.Width(widthLabelRange));
								EditorGUILayout.LabelField(controlParam._label_Max, apGUIStyleWrapper.I.Label_MiddleRight, apGUILOFactory.I.Width(widthLabelRange));
								EditorGUILayout.EndHorizontal();
								fNext = EditorGUILayout.Slider(keyframe._conSyncValue_Float, controlParam._float_Min, controlParam._float_Max, apGUILOFactory.I.Width(width));

								if (fNext != keyframe._conSyncValue_Float)
								{
									apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, Editor._portrait, keyframe, true);

									keyframe._conSyncValue_Float = fNext;
									isChanged = true;
								}
							}
							break;

						case apControlParam.TYPE.Vector2:
							{
								Vector2 v2Next = keyframe._conSyncValue_Vector2;
								EditorGUILayout.LabelField(controlParam._keyName, apGUILOFactory.I.Width(width));

								EditorGUILayout.LabelField(controlParam._label_Min, apGUILOFactory.I.Width(width));
								v2Next.x = EditorGUILayout.Slider(keyframe._conSyncValue_Vector2.x, controlParam._vec2_Min.x, controlParam._vec2_Max.x, apGUILOFactory.I.Width(width));

								EditorGUILayout.LabelField(controlParam._label_Max, apGUILOFactory.I.Width(width));
								v2Next.y = EditorGUILayout.Slider(keyframe._conSyncValue_Vector2.y, controlParam._vec2_Min.y, controlParam._vec2_Max.y, apGUILOFactory.I.Width(width));

								if (v2Next.x != keyframe._conSyncValue_Vector2.x ||
									v2Next.y != keyframe._conSyncValue_Vector2.y)
								{
									apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, Editor._portrait, keyframe, true);

									keyframe._conSyncValue_Vector2 = v2Next;
									isChanged = true;
								}
							}
							break;

					}

					if (isChanged)
					{
						AnimClip.UpdateControlParam(true);
					}
					#endregion
				}

				if (isDrawModifierUI && isSameKP)
				{
					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width);
					GUILayout.Space(10);

					GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);

					//GUIStyle guiStyleBox = new GUIStyle(GUI.skin.box);
					//guiStyleBox.alignment = TextAnchor.MiddleCenter;
					//guiStyleBox.normal.textColor = apEditorUtil.BoxTextColor;

					apModifierBase linkedModifier = AnimTimeline._linkedModifier;


					//string boxText = null;
					bool isMod_Morph = ((int)(linkedModifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.VertexPos) != 0);
					bool isMod_TF = ((int)(linkedModifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.TransformMatrix) != 0);
					bool isMod_Color = ((int)(linkedModifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.Color) != 0);

					//if (isMod_Morph)
					//{
					//	//boxText = "Morph Modifier Value";
					//	boxText = Editor.GetUIWord(UIWORD.MorphModifierValue);
					//}
					//else
					//{
					//	//boxText = "Transform Modifier Value";
					//	boxText = Editor.GetUIWord(UIWORD.TransformModifierValue);
					//}

					GUILayout.Box(isMod_Morph ? Editor.GetUIWord(UIWORD.MorphModifierValue) : Editor.GetUIWord(UIWORD.TransformModifierValue),
									apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor,
									apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

					GUI.backgroundColor = prevColor;

					//apModifierParamSet paramSet = keyframe._linkedParamSet_Editor;
					apModifiedMesh modMesh = keyframe._linkedModMesh_Editor;
					apModifiedBone modBone = keyframe._linkedModBone_Editor;
					if (modMesh == null)
					{
						isMod_Morph = false;
						isMod_Color = false;
					}
					if (modBone == null && modMesh == null)
					{
						//TF 타입은 Bone 타입이 적용될 수 있다.
						isMod_TF = false;
					}
					//TODO : 여기서부터 작성하자

					bool isChanged = false;

					if (_guiContent_Icon_ModTF_Pos == null) { _guiContent_Icon_ModTF_Pos = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Move)); }
					if (_guiContent_Icon_ModTF_Rot == null) { _guiContent_Icon_ModTF_Rot = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Rotate)); }
					if (_guiContent_Icon_ModTF_Scale == null) { _guiContent_Icon_ModTF_Scale = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Scale)); }
					if (_guiContent_Icon_Mod_Color == null) { _guiContent_Icon_Mod_Color = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Color)); }

					if (isMod_Morph)
					{
						GUILayout.Space(5);
					}

					if (isMod_TF)
					{
						GUILayout.Space(5);

						//Texture2D img_Pos = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Move);
						//Texture2D img_Rot = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Rotate);
						//Texture2D img_Scale = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Scale);


						//Texture2D img_BoneIK = Editor.ImageSet.Get(apImageSet.PRESET.Transform_IKController);

						Vector2 nextPos = Vector2.zero;
						float nextAngle = 0.0f;
						Vector2 nextScale = Vector2.one;
						//float nextBoneIKWeight = 0.0f;

						if (modMesh != null)
						{
							nextPos = modMesh._transformMatrix._pos;
							nextAngle = modMesh._transformMatrix._angleDeg;
							nextScale = modMesh._transformMatrix._scale;
						}
						else if (modBone != null)
						{
							nextPos = modBone._transformMatrix._pos;
							nextAngle = modBone._transformMatrix._angleDeg;
							nextScale = modBone._transformMatrix._scale;
							//nextBoneIKWeight = modBone._boneIKController_MixWeight;
						}

						bool isAngleIsOutRange = nextAngle <= -180.0f || nextAngle >= 180.0f;

						int iconSize = 30;
						int propertyWidth = width - (iconSize + 8);
						int rotationLockBtnSize = 26;
						int propertyRotationWidth = width - (iconSize + rotationLockBtnSize + 16);



						//Position
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

						//EditorGUILayout.LabelField(new GUIContent(img_Pos), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
						EditorGUILayout.LabelField(_guiContent_Icon_ModTF_Pos.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

						EditorGUILayout.EndVertical();

						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyWidth), apGUILOFactory.I.Height(iconSize));
						EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Position));//"Position"
																					  //nextPos = EditorGUILayout.Vector2Field("", nextPos, GUILayout.Width(propertyWidth));
						nextPos = apEditorUtil.DelayedVector2Field(nextPos, propertyWidth);
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();

						//Rotation
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

						//EditorGUILayout.LabelField(new GUIContent(img_Rot), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
						EditorGUILayout.LabelField(_guiContent_Icon_ModTF_Rot.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

						EditorGUILayout.EndVertical();

						//변경 20.1.21 : 180도 제한을 풀 수 있다.
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyRotationWidth), apGUILOFactory.I.Height(iconSize));
						EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Rotation), apGUILOFactory.I.Width(propertyRotationWidth));//"Rotation"

						if (isAngleIsOutRange)
						{
							//만약 각도 범위가 180 제한을 벗어났다면 색상을 구분해서 주의를 주자
							GUI.backgroundColor = new Color(1.0f, 0.8f, 0.8f, 1.0f);
						}
						nextAngle = EditorGUILayout.DelayedFloatField(nextAngle, apGUILOFactory.I.Width(propertyRotationWidth));

						if (isAngleIsOutRange)
						{
							GUI.backgroundColor = prevColor;
						}

						EditorGUILayout.EndVertical();


						//추가 20.1.21 : 180도 제한 설정/해제 버튼
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(rotationLockBtnSize), apGUILOFactory.I.Height(iconSize));
						GUILayout.Space(6);
						if (apEditorUtil.ToggledButton_2Side(
							Editor.ImageSet.Get(apImageSet.PRESET.Anim_180Lock),
							Editor.ImageSet.Get(apImageSet.PRESET.Anim_180Unlock), Editor._isAnimRotation180Lock, true, rotationLockBtnSize, rotationLockBtnSize))
						{
							Editor._isAnimRotation180Lock = !Editor._isAnimRotation180Lock;
						}
						EditorGUILayout.EndVertical();


						EditorGUILayout.EndHorizontal();

						//추가 : CW, CCW 옵션을 표시한다.
						int rotationEnumWidth = 80;
						int rotationValueWidth = (((width - 10) / 2) - rotationEnumWidth) - 4;
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
						GUILayout.Space(5);
						apAnimKeyframe.ROTATION_BIAS prevRotationBias = (apAnimKeyframe.ROTATION_BIAS)EditorGUILayout.EnumPopup(keyframe._prevRotationBiasMode, apGUILOFactory.I.Width(rotationEnumWidth));
						int prevRotationBiasCount = EditorGUILayout.IntField(keyframe._prevRotationBiasCount, apGUILOFactory.I.Width(rotationValueWidth));
						apAnimKeyframe.ROTATION_BIAS nextRotationBias = (apAnimKeyframe.ROTATION_BIAS)EditorGUILayout.EnumPopup(keyframe._nextRotationBiasMode, apGUILOFactory.I.Width(rotationEnumWidth));
						int nextRotationBiasCount = EditorGUILayout.IntField(keyframe._nextRotationBiasCount, apGUILOFactory.I.Width(rotationValueWidth));
						EditorGUILayout.EndHorizontal();



						//Scaling
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

						//EditorGUILayout.LabelField(new GUIContent(img_Scale), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
						EditorGUILayout.LabelField(_guiContent_Icon_ModTF_Scale.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

						EditorGUILayout.EndVertical();

						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyWidth), apGUILOFactory.I.Height(iconSize));
						EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Scaling));//"Scaling"

						//nextScale = EditorGUILayout.Vector2Field("", nextScale, GUILayout.Width(propertyWidth));
						nextScale = apEditorUtil.DelayedVector2Field(nextScale, propertyWidth);
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();




						if (modMesh != null)
						{
							if (nextPos.x != modMesh._transformMatrix._pos.x ||
								nextPos.y != modMesh._transformMatrix._pos.y ||
								nextAngle != modMesh._transformMatrix._angleDeg ||
								nextScale.x != modMesh._transformMatrix._scale.x ||
								nextScale.y != modMesh._transformMatrix._scale.y
								)
							{
								isChanged = true;

								apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, linkedModifier, null, false);

								//추가 20.1.21 : 180 제한 옵션에 따라 이 각도를 180 이내로 제한한다
								if (Editor._isAnimRotation180Lock)
								{
									nextAngle = apUtil.AngleTo180(nextAngle);
								}

								modMesh._transformMatrix.SetPos(nextPos);
								modMesh._transformMatrix.SetRotate(nextAngle);
								modMesh._transformMatrix.SetScale(nextScale);
								modMesh._transformMatrix.MakeMatrix();

								apEditorUtil.ReleaseGUIFocus();
							}
						}
						else if (modBone != null)
						{
							if (nextPos.x != modBone._transformMatrix._pos.x ||
								nextPos.y != modBone._transformMatrix._pos.y ||
								nextAngle != modBone._transformMatrix._angleDeg ||
								nextScale.x != modBone._transformMatrix._scale.x ||
								nextScale.y != modBone._transformMatrix._scale.y
								//nextBoneIKWeight != modBone._boneIKController_MixWeight
								)
							{
								isChanged = true;

								apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, linkedModifier, null, false);

								//추가 20.1.21 : 180 제한 옵션에 따라 이 각도를 180 이내로 제한한다
								if (Editor._isAnimRotation180Lock)
								{
									nextAngle = apUtil.AngleTo180(nextAngle);
								}

								modBone._transformMatrix.SetPos(nextPos);
								modBone._transformMatrix.SetRotate(nextAngle);
								modBone._transformMatrix.SetScale(nextScale);
								modBone._transformMatrix.MakeMatrix();

								//modBone._boneIKController_MixWeight = Mathf.Clamp01(nextBoneIKWeight);

								apEditorUtil.ReleaseGUIFocus();
							}


						}

						if (prevRotationBias != keyframe._prevRotationBiasMode ||
							prevRotationBiasCount != keyframe._prevRotationBiasCount ||
							nextRotationBias != keyframe._nextRotationBiasMode ||
							nextRotationBiasCount != keyframe._nextRotationBiasCount)
						{
							isChanged = true;

							apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, Editor._portrait, null, false);

							if (prevRotationBiasCount < 0) { prevRotationBiasCount = 0; }
							if (nextRotationBiasCount < 0) { nextRotationBiasCount = 0; }

							keyframe._prevRotationBiasMode = prevRotationBias;
							keyframe._prevRotationBiasCount = prevRotationBiasCount;
							keyframe._nextRotationBiasMode = nextRotationBias;
							keyframe._nextRotationBiasCount = nextRotationBiasCount;


						}

					}

					if (isMod_Color)
					{
						GUILayout.Space(5);

						if (linkedModifier._isColorPropertyEnabled)
						{
							//Texture2D img_Color = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Color);

							Color nextColor = modMesh._meshColor;
							bool isMeshVisible = modMesh._isVisible;

							int iconSize = 30;
							int propertyWidth = width - (iconSize + 8);

							//Color
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
							EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

							//EditorGUILayout.LabelField(new GUIContent(img_Color), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
							EditorGUILayout.LabelField(_guiContent_Icon_Mod_Color.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

							EditorGUILayout.EndVertical();

							EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyWidth), apGUILOFactory.I.Height(iconSize));
							EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Color2X));//"Color (2X)"
							try
							{
								nextColor = EditorGUILayout.ColorField(apStringFactory.I.None, modMesh._meshColor, apGUILOFactory.I.Width(propertyWidth));
							}
							catch (Exception)
							{

							}

							EditorGUILayout.EndVertical();
							EditorGUILayout.EndHorizontal();


							//Visible
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
							GUILayout.Space(5);
							EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IsVisible), apGUILOFactory.I.Width(propertyWidth));//"Is Visible..
							isMeshVisible = EditorGUILayout.Toggle(isMeshVisible, apGUILOFactory.I.Width(iconSize));
							EditorGUILayout.EndHorizontal();



							if (nextColor.r != modMesh._meshColor.r ||
								nextColor.g != modMesh._meshColor.g ||
								nextColor.b != modMesh._meshColor.b ||
								nextColor.a != modMesh._meshColor.a ||
								isMeshVisible != modMesh._isVisible)
							{
								isChanged = true;

								apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, linkedModifier, null, false);

								modMesh._meshColor = nextColor;
								modMesh._isVisible = isMeshVisible;

								//apEditorUtil.ReleaseGUIFocus();
							}
						}
						else
						{
							GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);

							//"Color Property is disabled"
							GUILayout.Box(Editor.GetUIWord(UIWORD.ColorPropertyIsDisabled),
											apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor,
											apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));

							GUI.backgroundColor = prevColor;
						}
					}

					GUILayout.Space(10);

					if (isChanged)
					{
						AnimClip.UpdateControlParam(true);
					}
				}
			}
			else
			{
				//2. Curve Mode
				//1) Prev 커브를 선택할 것인지, Next 커브를 선택할 것인지 결정해야한다.
				//2) 양쪽의 컨트롤 포인트의 설정을 결정한다. (Linear / Smooth / Constant(Stepped))
				//3) 커브 GUI

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

				GUILayout.Space(5);

				int curveTypeBtnSize = 30;
				int curveBtnSize = (width - (curveTypeBtnSize * 3 + 2 * 5)) / 2 - 6;

				apAnimCurve curveA = null;
				apAnimCurve curveB = null;
				apAnimCurveResult curveResult = null;

				//string strPrevKey = "";
				//string strNextKey = "";

				if (_guiContent_AnimKeyframeProp_PrevKeyLabel == null)
				{
					_guiContent_AnimKeyframeProp_PrevKeyLabel = new apGUIContentWrapper();
				}
				if (_guiContent_AnimKeyframeProp_NextKeyLabel == null)
				{
					_guiContent_AnimKeyframeProp_NextKeyLabel = new apGUIContentWrapper();
				}



				//변경
				bool isColorLabelRed_Prev = false;
				bool isColorLabelRed_Next = false;

				if (_animPropertyCurveUI == ANIM_SINGLE_PROPERTY_CURVE_UI.Prev)
				{
					curveA = keyframe._curveKey._prevLinkedCurveKey;
					curveB = keyframe._curveKey;
					curveResult = keyframe._curveKey._prevCurveResult;

					if (keyframe._prevLinkedKeyframe != null)
					{
						//strPrevKey = "Prev [" + keyframe._prevLinkedKeyframe._frameIndex + "]";
						//strPrevKey = string.Format("{0} [{1}]", Editor.GetUIWord(UIWORD.Prev), keyframe._prevLinkedKeyframe._frameIndex);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.ClearText(false);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.AppendText(Editor.GetUIWord(UIWORD.Prev), false);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.AppendSpaceText(1, false);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.AppendText(keyframe._prevLinkedKeyframe._frameIndex, true);
					}
					else
					{
						_guiContent_AnimKeyframeProp_PrevKeyLabel.ClearText(true);
					}
					//strNextKey = "Current [" + keyframe._frameIndex + "]";
					//strNextKey = string.Format("{0} [{1}]", Editor.GetUIWord(UIWORD.Current), keyframe._frameIndex);

					_guiContent_AnimKeyframeProp_NextKeyLabel.ClearText(false);
					_guiContent_AnimKeyframeProp_NextKeyLabel.AppendText(Editor.GetUIWord(UIWORD.Current), false);
					_guiContent_AnimKeyframeProp_NextKeyLabel.AppendSpaceText(1, false);
					_guiContent_AnimKeyframeProp_NextKeyLabel.AppendText(keyframe._frameIndex, true);


					//colorLabel_Next = Color.red;
					isColorLabelRed_Next = true;
				}
				else
				{
					curveA = keyframe._curveKey;
					curveB = keyframe._curveKey._nextLinkedCurveKey;
					curveResult = keyframe._curveKey._nextCurveResult;


					//strPrevKey = "Current [" + keyframe._frameIndex + "]";
					//strNextKey = string.Format("{0} [{1}]", Editor.GetUIWord(UIWORD.Current), keyframe._frameIndex);
					_guiContent_AnimKeyframeProp_NextKeyLabel.ClearText(false);
					_guiContent_AnimKeyframeProp_NextKeyLabel.AppendText(Editor.GetUIWord(UIWORD.Current), false);
					_guiContent_AnimKeyframeProp_NextKeyLabel.AppendSpaceText(1, false);
					_guiContent_AnimKeyframeProp_NextKeyLabel.AppendText(keyframe._frameIndex, true);

					if (keyframe._nextLinkedKeyframe != null)
					{
						//strNextKey = "Next [" + keyframe._nextLinkedKeyframe._frameIndex + "]";
						//strPrevKey = string.Format("{0} [{1}]", Editor.GetUIWord(UIWORD.Next), keyframe._nextLinkedKeyframe._frameIndex);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.ClearText(false);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.AppendText(Editor.GetUIWord(UIWORD.Next), false);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.AppendSpaceText(1, false);
						_guiContent_AnimKeyframeProp_PrevKeyLabel.AppendText(keyframe._nextLinkedKeyframe._frameIndex, true);
					}
					else
					{
						_guiContent_AnimKeyframeProp_PrevKeyLabel.ClearText(true);
					}

					//colorLabel_Prev = Color.red;
					isColorLabelRed_Prev = true;
				}



				if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Prev), _animPropertyCurveUI == ANIM_SINGLE_PROPERTY_CURVE_UI.Prev, curveBtnSize, 30))//"Prev"
				{
					_animPropertyCurveUI = ANIM_SINGLE_PROPERTY_CURVE_UI.Prev;
				}
				if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Next), _animPropertyCurveUI == ANIM_SINGLE_PROPERTY_CURVE_UI.Next, curveBtnSize, 30))//"Next"
				{
					_animPropertyCurveUI = ANIM_SINGLE_PROPERTY_CURVE_UI.Next;
				}
				GUILayout.Space(5);
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Curve_Linear), curveResult.CurveTangentType == apAnimCurve.TANGENT_TYPE.Linear, true, curveTypeBtnSize, 30,
												apStringFactory.I.AnimCurveTooltip_Linear))//"Linear Curve"
				{
					apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, curveResult, false);
					curveResult.SetTangent(apAnimCurve.TANGENT_TYPE.Linear);
				}
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Curve_Smooth), curveResult.CurveTangentType == apAnimCurve.TANGENT_TYPE.Smooth, true, curveTypeBtnSize, 30,
												apStringFactory.I.AnimCurveTooltip_Smooth))//"Smooth Curve"
				{
					apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, curveResult, false);
					curveResult.SetTangent(apAnimCurve.TANGENT_TYPE.Smooth);
				}
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Curve_Stepped), curveResult.CurveTangentType == apAnimCurve.TANGENT_TYPE.Constant, true, curveTypeBtnSize, 30,
												apStringFactory.I.AnimCurveTooltip_Constant))//"Constant Curve"
				{
					apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, curveResult, false);
					curveResult.SetTangent(apAnimCurve.TANGENT_TYPE.Constant);
				}



				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);

				if (isSameKP)
				{

					if (curveA == null || curveB == null)
					{
						EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.KeyframeIsNotLinked));//"Keyframe is not linked"
					}
					else
					{

						int curveUI_Width = width - 1;
						int curveUI_Height = 200;
						prevColor = GUI.backgroundColor;

						Rect lastRect = GUILayoutUtility.GetLastRect();

						if (EditorGUIUtility.isProSkin)
						{
							GUI.backgroundColor = new Color(Editor._guiMainEditorColor.r * 0.8f,
													Editor._guiMainEditorColor.g * 0.8f,
													Editor._guiMainEditorColor.b * 0.8f,
													1.0f);
						}
						else
						{
							GUI.backgroundColor = Editor._guiMainEditorColor;
						}

						Rect curveRect = new Rect(lastRect.x + 5, lastRect.y, curveUI_Width, curveUI_Height);

						curveUI_Width -= 2;
						curveUI_Height -= 4;

						//int layoutY_Clip = layoutY - Mathf.Min(scrollValue, 115);
						//int layoutY_Clip = layoutY - Mathf.Clamp(scrollValue, 0, 115);
						//int layoutY_Clip = (layoutY - (scrollValue + (115 - scrollValue));//scrollValue > 115
						//int clipPosY = 115 - scrollValue;


						//Debug.Log("Lyout Y / layoutY : " + layoutY + " / scrollValue : " + scrollValue + " => " + layoutY_Clip);
						apAnimCurveGL.SetLayoutSize(
							curveUI_Width,
							curveUI_Height,
							(int)(lastRect.x) + layoutX - (curveUI_Width + 10),
							//(int)(lastRect.y) + layoutY_Clip,
							(int)(lastRect.y) + layoutY,
							scrollValue,
							Mathf.Min(scrollValue, 115),
							windowWidth, windowHeight);

						bool isLeftBtnPressed = false;
						if (Event.current.rawType == EventType.MouseDown ||
							Event.current.rawType == EventType.MouseDrag)
						{
							if (Event.current.button == 0)
							{ isLeftBtnPressed = true; }
						}

						//apAnimCurveGL.SetMouseValue(isLeftBtnPressed, apMouse.PosNotBound, Event.current.rawType, this);//이전
						apAnimCurveGL.SetMouseValue(isLeftBtnPressed, Editor.Mouse.PosNotBound, Event.current.rawType, this);

						GUI.Box(curveRect, apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box);
						//EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(curveUI_Height));
						GUILayout.BeginArea(new Rect(lastRect.x + 8, lastRect.y + 124, curveUI_Width - 2, curveUI_Height - 2));

						Color curveGraphColorA = Color.black;
						Color curveGraphColorB = Color.black;

						if (curveResult.CurveTangentType == apAnimCurve.TANGENT_TYPE.Linear)
						{
							curveGraphColorA = new Color(1.0f, 0.1f, 0.1f, 1.0f);
							curveGraphColorB = new Color(1.0f, 1.0f, 0.1f, 1.0f);
						}
						else if (curveResult.CurveTangentType == apAnimCurve.TANGENT_TYPE.Smooth)
						{
							curveGraphColorA = new Color(0.2f, 0.2f, 1.0f, 1.0f);
							curveGraphColorB = new Color(0.2f, 1.0f, 1.0f, 1.0f);
						}
						else
						{
							curveGraphColorA = new Color(0.2f, 1.0f, 0.1f, 1.0f);
							curveGraphColorB = new Color(0.1f, 1.0f, 0.6f, 1.0f);
						}


						apAnimCurveGL.DrawCurve(curveA, curveB, curveResult, curveGraphColorA, curveGraphColorB);


						GUILayout.EndArea();
						//EditorGUILayout.EndVertical();



						//GUILayout.Space(10);

						GUI.backgroundColor = prevColor;


						GUILayout.Space(curveUI_Height - 2);


						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

						//삭제 19.11.22
						//GUIStyle guiStyle_FrameLabel_Prev = new GUIStyle(GUI.skin.label);
						//GUIStyle guiStyle_FrameLabel_Next = new GUIStyle(GUI.skin.label);
						//guiStyle_FrameLabel_Next.alignment = TextAnchor.MiddleRight;

						//guiStyle_FrameLabel_Prev.normal.textColor = colorLabel_Prev;
						//guiStyle_FrameLabel_Next.normal.textColor = colorLabel_Next;




						GUILayout.Space(5);
						//EditorGUILayout.LabelField(strPrevKey, guiStyle_FrameLabel_Prev, GUILayout.Width(width / 2 - 4));
						EditorGUILayout.LabelField(_guiContent_AnimKeyframeProp_PrevKeyLabel.Content,
							(isColorLabelRed_Prev ? apGUIStyleWrapper.I.Label_MiddleLeft_RedColor : apGUIStyleWrapper.I.Label_MiddleLeft_BlackColor),
							apGUILOFactory.I.Width(width / 2 - 4));

						//EditorGUILayout.LabelField(strNextKey, guiStyle_FrameLabel_Next, GUILayout.Width(width / 2 - 4));
						EditorGUILayout.LabelField(_guiContent_AnimKeyframeProp_NextKeyLabel.Content,
							(isColorLabelRed_Next ? apGUIStyleWrapper.I.Label_MiddleRight_RedColor : apGUIStyleWrapper.I.Label_MiddleRight_BlackColor),
							apGUILOFactory.I.Width(width / 2 - 4));

						EditorGUILayout.EndHorizontal();

						if (curveResult.CurveTangentType == apAnimCurve.TANGENT_TYPE.Smooth)
						{
							GUILayout.Space(5);

							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
							GUILayout.Space(5);

							int smoothPresetBtnWidth = ((width - 10) / 4) - 1;
							if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Default), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
							{
								//커브 프리셋 : 기본
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
								curveResult.SetCurvePreset_Default();
							}
							if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Hard), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
							{
								//커브 프리셋 : 하드
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
								curveResult.SetCurvePreset_Hard();
							}
							if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Acc), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
							{
								//커브 프리셋 : 가속 (느리다가 빠르게)
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
								curveResult.SetCurvePreset_Acc();
							}
							if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Dec), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
							{
								//커브 프리셋 : 감속 (빠르다가 느리게)
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
								curveResult.SetCurvePreset_Dec();
							}

							EditorGUILayout.EndHorizontal();

							if (GUILayout.Button(Editor.GetUIWord(UIWORD.ResetSmoothSetting), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))//"Reset Smooth Setting"
							{
								//Curve는 Anim 고유의 값이다. -> Portrait
								apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
								curveResult.ResetSmoothSetting();

								Editor.SetRepaint();
								//Editor.Repaint();
							}
						}
						GUILayout.Space(5);
						if (GUILayout.Button(Editor.GetUIWord(UIWORD.CopyCurveToAllKeyframes), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))//"Copy Curve to All Keyframes"
						{

							Editor.Controller.CopyAnimCurveToAllKeyframes(curveResult, keyframe._parentTimelineLayer, keyframe._parentTimelineLayer._parentAnimClip);
							Editor.SetRepaint();
						}




					}
				}
			}



			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			if (isSameKP)
			{
				//복사 / 붙여넣기 / 삭제 // (복붙은 모든 타입에서 등장한다)
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
				GUILayout.Space(5);
				//int editBtnWidth = ((width) / 2) - 3;
				int editBtnWidth_Copy = 80;
				int editBtnWidth_Paste = width - (80 + 4);
				//if (GUILayout.Button(new GUIContent(" Copy", Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy)), GUILayout.Width(editBtnWidth), GUILayout.Height(25)))

				//string strCopy = " " + Editor.GetUIWord(UIWORD.Copy);

				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy), 1, Editor.GetUIWord(UIWORD.Copy), Editor.GetUIWord(UIWORD.Copy), false, true, editBtnWidth_Copy, 25))//" Copy"
				{
					//Debug.LogError("TODO : Copy Keyframe");
					if (keyframe != null)
					{
						string copyName = "";
						if (keyframe._parentTimelineLayer != null)
						{
							copyName += keyframe._parentTimelineLayer.DisplayName + " ";
						}
						copyName += "[ " + keyframe._frameIndex + " ]";
						apSnapShotManager.I.Copy_Keyframe(keyframe, copyName);
					}
				}

				string pasteKeyName = apSnapShotManager.I.GetClipboardName_Keyframe();
				bool isPastable = apSnapShotManager.I.IsPastable(keyframe);
				if (string.IsNullOrEmpty(pasteKeyName) || !isPastable)
				{
					//pasteKeyName = "Paste";
					pasteKeyName = Editor.GetUIWord(UIWORD.Paste);
				}
				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Edit_Paste), 1, pasteKeyName, pasteKeyName, false, isPastable, editBtnWidth_Paste, 25))
				{
					if (keyframe != null)
					{
						//붙여넣기
						//Anim (portrait) + Keyframe+LinkedMod (Modifier = nullable)
						apEditorUtil.SetRecord_PortraitModifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, keyframe._parentTimelineLayer._parentTimeline._linkedModifier, null, false);
						apSnapShotManager.I.Paste_Keyframe(keyframe);
						RefreshAnimEditing(true);
					}
				}
				EditorGUILayout.EndHorizontal();


				//Pose Export / Import
				if (keyframe._parentTimelineLayer._linkedBone != null
					&& keyframe._parentTimelineLayer._parentTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier
					)
				{
					//Bone 타입인 경우
					//Pose 복사 / 붙여넣기를 할 수 있다.

					GUILayout.Space(5);
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.PoseExportImportLabel), apGUILOFactory.I.Width(width));//"Pose Export / Import"

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					GUILayout.Space(5);

					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_SaveLoad), 1, Editor.GetUIWord(UIWORD.Export), Editor.GetUIWord(UIWORD.Export), false, true, ((width) / 2) - 2, 25))
					{
						if (keyframe._parentTimelineLayer._parentAnimClip._targetMeshGroup != null)
						{
							apDialog_RetargetSinglePoseExport.ShowDialog(Editor, keyframe._parentTimelineLayer._parentAnimClip._targetMeshGroup, keyframe._parentTimelineLayer._linkedBone);
						}
					}
					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_LoadBones), 1, Editor.GetUIWord(UIWORD.Import), Editor.GetUIWord(UIWORD.Import), false, true, ((width) / 2) - 2, 25))
					{
						if (keyframe._parentTimelineLayer._parentAnimClip._targetMeshGroup != null)
						{
							_loadKey_SinglePoseImport_Anim = apDialog_RetargetSinglePoseImport.ShowDialog(
								OnRetargetSinglePoseImportAnim, Editor,
								keyframe._parentTimelineLayer._parentAnimClip._targetMeshGroup,
								keyframe._parentTimelineLayer._parentAnimClip,
								keyframe._parentTimelineLayer._parentTimeline,
								keyframe._parentTimelineLayer._parentAnimClip.CurFrame
								);
						}
					}
					EditorGUILayout.EndHorizontal();
				}


				GUILayout.Space(10);
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(10);


				//삭제 단축키 이벤트를 넣자
				Editor.AddHotKeyEvent(OnHotKeyRemoveKeyframes, apHotKey.LabelText.RemoveKeyframe, KeyCode.Delete, false, false, false, keyframe);//"Remove Keyframe"



				//키 삭제
				//"Remove Keyframe"
				//이전
				//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveKeyframe),
				//									Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
				//									),
				//					GUILayout.Width(width), GUILayout.Height(24)))

				//변경
				if (_guiContent_Bottom_Animation_RemoveKeyframes == null)
				{
					_guiContent_Bottom_Animation_RemoveKeyframes = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveKeyframe), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
				}

				if (GUILayout.Button(_guiContent_Bottom_Animation_RemoveKeyframes.Content,
									apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(24)))
				{

					bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveKeyframe1_Title),
																Editor.GetText(TEXT.RemoveKeyframe1_Body),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);
					if (isResult)
					{
						Editor.Controller.RemoveKeyframe(keyframe);
					}
				}


			}


		}





		private void OnRetargetSinglePoseImportAnim(object loadKey, bool isSuccess, apRetarget resultRetarget,
																apMeshGroup targetMeshGroup,
																apAnimClip targetAnimClip,
																apAnimTimeline targetTimeline, int targetFrame,
																apDialog_RetargetSinglePoseImport.IMPORT_METHOD importMethod)
		{
			if (loadKey != _loadKey_SinglePoseImport_Anim || !isSuccess)
			{
				_loadKey_SinglePoseImport_Anim = null;
				return;
			}

			_loadKey_SinglePoseImport_Anim = null;

			//Pose Import 처리를 하자
			Editor.Controller.ImportBonePoseFromRetargetSinglePoseFileToAnimClip(targetMeshGroup, resultRetarget, targetAnimClip, targetTimeline, targetFrame, importMethod);

		}




		private void DrawEditor_Bottom_AnimationProperty_MultipleKeyframes(List<apAnimKeyframe> keyframes, int width, int windowWidth, int windowHeight, int layoutX, int layoutY, int scrollValue)
		{
			//추가 3.30 : 공통 커브 설정 기능

			//keyframes.Count + " Keyframes Selected"
			Color prevColor = GUI.backgroundColor;

			//GUILayout.Space(10);
			//apEditorUtil.GUI_DelimeterBoxH(width);
			//GUILayout.Space(10);
			//GUILayout.Space(5);

			//추가 19.12.31 : 어느 커브를 설정할지 선택하여 편집할 수 있음
			int curvePosBtnSize = ((width - 10) / 3);
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
			GUILayout.Space(5);
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Prev), _animPropertyCurveUI_Multi == ANIM_MULTI_PROPERTY_CURVE_UI.Prev, curvePosBtnSize, 20))//"Prev"
			{
				_animPropertyCurveUI_Multi = ANIM_MULTI_PROPERTY_CURVE_UI.Prev;
			}
			//"Between"
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Between), _animPropertyCurveUI_Multi == ANIM_MULTI_PROPERTY_CURVE_UI.Middle, curvePosBtnSize, 20))//"Mid"
			{
				_animPropertyCurveUI_Multi = ANIM_MULTI_PROPERTY_CURVE_UI.Middle;
			}
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.Next), _animPropertyCurveUI_Multi == ANIM_MULTI_PROPERTY_CURVE_UI.Next, curvePosBtnSize, 20))//"Prev"
			{
				_animPropertyCurveUI_Multi = ANIM_MULTI_PROPERTY_CURVE_UI.Next;
			}
			EditorGUILayout.EndHorizontal();


			int iCurveType = 0;
			switch (_animPropertyCurveUI_Multi)
			{
				case ANIM_MULTI_PROPERTY_CURVE_UI.Prev: iCurveType = 0; break;
				case ANIM_MULTI_PROPERTY_CURVE_UI.Middle: iCurveType = 1; break;
				case ANIM_MULTI_PROPERTY_CURVE_UI.Next: iCurveType = 2; break;
			}
			apTimelineCommonCurve.SYNC_STATUS curveSync = _animTimelineCommonCurve.GetSyncStatus(iCurveType);

			bool isCurves_NoKey = (curveSync == apTimelineCommonCurve.SYNC_STATUS.NoKeyframes);
			bool isCurves_Sync = (curveSync == apTimelineCommonCurve.SYNC_STATUS.Sync);
			bool isCurves_NotSync = (curveSync == apTimelineCommonCurve.SYNC_STATUS.NotSync);

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimProperty_MultipleCurve__NoKey, isCurves_NoKey);
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimProperty_MultipleCurve__Sync, isCurves_Sync);//AnimProperty_MultipleCurve : Sync
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimProperty_MultipleCurve__NotSync, isCurves_NotSync);//"AnimProperty_MultipleCurve : NotSync"

			//3가지 경우가 있다.
			//1) 편집할 수 있는 커브가 없다.
			//2) 동기화된 상태여서 편집이 가능하다.
			//3) 동기화되지 않았다.

			if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimProperty_MultipleCurve__NoKey))
			{
				//1) 편집할 수 있는 커브가 없다.
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.NoSelectedCurveEdit));//"There is no selected curve to edit."
				GUILayout.Space(5);
			}
			else if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimProperty_MultipleCurve__Sync))//"AnimProperty_MultipleCurve : Sync"
			{
				//2) 동기화된 상태여서 편집이 가능하다.
				//동시에 여러개의 "동기화된" 커브를 조작할 수 있다.

				apAnimCurve.TANGENT_TYPE curveTangentType = apAnimCurve.TANGENT_TYPE.Smooth;
				if (isCurves_Sync)
				{
					curveTangentType = _animTimelineCommonCurve.GetSyncCurveResult(iCurveType).CurveTangentType;
				}

				//추가 19.12.31 : 1. 커브 위치
				int curveTypeBtnSize = ((width - 10) / 3);

				//1. 커브 종류
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(5);
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Curve_Linear), curveTangentType == apAnimCurve.TANGENT_TYPE.Linear, true, curveTypeBtnSize, 25,
												apStringFactory.I.AnimCurveTooltip_Linear))//"Linear Curve"
				{
					if (isCurves_Sync)
					{
						apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animTimelineCommonCurve, false);
						_animTimelineCommonCurve.SetTangentType(apAnimCurve.TANGENT_TYPE.Linear, iCurveType);
					}
				}
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Curve_Smooth), curveTangentType == apAnimCurve.TANGENT_TYPE.Smooth, true, curveTypeBtnSize, 25,
												apStringFactory.I.AnimCurveTooltip_Smooth))//"Smooth Curve"
				{
					if (isCurves_Sync)
					{
						apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animTimelineCommonCurve, false);
						_animTimelineCommonCurve.SetTangentType(apAnimCurve.TANGENT_TYPE.Smooth, iCurveType);
					}
				}
				if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Curve_Stepped), curveTangentType == apAnimCurve.TANGENT_TYPE.Constant, true, curveTypeBtnSize, 26,
												apStringFactory.I.AnimCurveTooltip_Constant))//"Constant Curve"
				{
					if (isCurves_Sync)
					{
						apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animTimelineCommonCurve, false);
						_animTimelineCommonCurve.SetTangentType(apAnimCurve.TANGENT_TYPE.Constant, iCurveType);
					}
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);

				//2. 커브를 그리자

				int curveUI_Width = width - 1;
				int curveUI_Height = 200;
				prevColor = GUI.backgroundColor;

				Rect lastRect = GUILayoutUtility.GetLastRect();

				if (EditorGUIUtility.isProSkin)
				{
					GUI.backgroundColor = new Color(Editor._guiMainEditorColor.r * 0.8f,
														Editor._guiMainEditorColor.g * 0.8f,
														Editor._guiMainEditorColor.b * 0.8f,
														1.0f);
				}
				else
				{
					GUI.backgroundColor = Editor._guiMainEditorColor;
				}

				Rect curveRect = new Rect(lastRect.x + 5, lastRect.y, curveUI_Width, curveUI_Height);

				curveUI_Width -= 2;
				curveUI_Height -= 4;


				apAnimCurveGL.SetLayoutSize(curveUI_Width, curveUI_Height,
												(int)(lastRect.x) + layoutX - (curveUI_Width + 10),
												(int)(lastRect.y) + layoutY,
												//(int)(lastRect.y) + layoutY - 25,
												scrollValue,
												//Mathf.Min(scrollValue, 115),
												//Mathf.Min(scrollValue, 115 - 57),
												//Mathf.Min(scrollValue, 115 - 80),
												Mathf.Min(scrollValue, (115 - (57 - 27))),
												windowWidth, windowHeight);

				bool isLeftBtnPressed = false;
				if (Event.current.rawType == EventType.MouseDown || Event.current.rawType == EventType.MouseDrag)
				{
					if (Event.current.button == 0)
					{
						isLeftBtnPressed = true;
					}
				}

				apAnimCurveGL.SetMouseValue(isLeftBtnPressed, Editor.Mouse.PosNotBound, Event.current.rawType, this);
				GUI.Box(curveRect, apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box);

				//GUILayout.BeginArea(new Rect(lastRect.x + 8, lastRect.y + 124, curveUI_Width - 2, curveUI_Height - 2));
				//GUILayout.BeginArea(new Rect(lastRect.x + 8, lastRect.y + 68, curveUI_Width - 2, curveUI_Height - 2));
				GUILayout.BeginArea(new Rect(lastRect.x + 8, lastRect.y + 68 + 26, curveUI_Width - 2, curveUI_Height - 2));

				Color curveGraphColorA = Color.black;
				Color curveGraphColorB = Color.black;

				if (curveTangentType == apAnimCurve.TANGENT_TYPE.Linear)
				{
					curveGraphColorA = new Color(1.0f, 0.1f, 0.1f, 1.0f);
					curveGraphColorB = new Color(1.0f, 1.0f, 0.1f, 1.0f);
				}
				else if (curveTangentType == apAnimCurve.TANGENT_TYPE.Smooth)
				{
					curveGraphColorA = new Color(0.2f, 0.2f, 1.0f, 1.0f);
					curveGraphColorB = new Color(0.2f, 1.0f, 1.0f, 1.0f);
				}
				else
				{
					curveGraphColorA = new Color(0.2f, 1.0f, 0.1f, 1.0f);
					curveGraphColorB = new Color(0.1f, 1.0f, 0.6f, 1.0f);
				}

				//Curve 값 변경시..
				//이전
				//bool isCurveChanged = apAnimCurveGL.DrawCurve(_animTimelineCommonCurve._syncCurve_Prev,
				//						_animTimelineCommonCurve._syncCurve_Next,
				//						_animTimelineCommonCurve.SyncCurveResult,
				//						curveGraphColorA, curveGraphColorB);

				//변경 19.12.31
				bool isCurveChanged = apAnimCurveGL.DrawCurve(_animTimelineCommonCurve.GetSyncCurve_Prev(iCurveType),
										_animTimelineCommonCurve.GetSyncCurve_Next(iCurveType),
										_animTimelineCommonCurve.GetSyncCurveResult(iCurveType),
										curveGraphColorA, curveGraphColorB);

				if (isCurves_Sync)
				{
					if (isCurveChanged)
					{
						//커브가 바뀌었음을 알리자
						_animTimelineCommonCurve.SetChanged();
					}

					//마우스 입력에 따른 Sync
					//_animTimelineCommonCurve.ApplySync(false, isLeftBtnPressed);
					_animTimelineCommonCurve.ApplySync(iCurveType, false, isLeftBtnPressed);
				}

				GUILayout.EndArea();

				GUI.backgroundColor = prevColor;

				GUILayout.Space(curveUI_Height - 2);

				if (curveTangentType == apAnimCurve.TANGENT_TYPE.Smooth)
				{
					GUILayout.Space(5);

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					GUILayout.Space(5);

					int smoothPresetBtnWidth = ((width - 10) / 4) - 1;
					if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Default), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
					{
						//커브 프리셋 : 기본
						if (isCurves_Sync)
						{
							apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
							_animTimelineCommonCurve.SetCurvePreset_Default(iCurveType);
						}
					}
					if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Hard), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
					{
						//커브 프리셋 : 하드
						if (isCurves_Sync)
						{
							apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
							_animTimelineCommonCurve.SetCurvePreset_Hard(iCurveType);
						}
					}
					if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Acc), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
					{
						//커브 프리셋 : 가속 (느리다가 빠르게)
						if (isCurves_Sync)
						{
							apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
							_animTimelineCommonCurve.SetCurvePreset_Acc(iCurveType);
						}
					}
					if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Anim_CurvePreset_Dec), apGUILOFactory.I.Width(smoothPresetBtnWidth), apGUILOFactory.I.Height(28)))
					{
						//커브 프리셋 : 감속 (빠르다가 느리게)
						if (isCurves_Sync)
						{
							apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
							_animTimelineCommonCurve.SetCurvePreset_Dec(iCurveType);
						}
					}

					EditorGUILayout.EndHorizontal();

					if (GUILayout.Button(Editor.GetUIWord(UIWORD.ResetSmoothSetting), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))//"Reset Smooth Setting"
					{
						if (isCurves_Sync)
						{
							apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animClip._targetMeshGroup, false);
							_animTimelineCommonCurve.ResetSmoothSetting(iCurveType);
						}
						Editor.SetRepaint();
					}
				}



			}
			else if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimProperty_MultipleCurve__NotSync))//"AnimProperty_MultipleCurve : NotSync"
			{
				//"Curves of keyframes are different"
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.CurvesAreDifferent));
				GUILayout.Space(5);
				//아직 커브들이 "동기화"되지 않았다.
				//"Reset curves of all selected keyframes"
				if (GUILayout.Button(Editor.GetUIWord(UIWORD.ResetMultipleCurves), apGUILOFactory.I.Height(25)))
				{
					if (isCurves_NotSync)
					{
						apEditorUtil.SetRecord_Portrait(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, _portrait, _animTimelineCommonCurve, false);
						_animTimelineCommonCurve.NotSync2SyncStatus(iCurveType);
					}
				}
			}
			else
			{
				//현재는 커브 조작을 할 수 없다.
			}





			// 키프레임들 삭제하기
			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			//삭제 단축키 이벤트를 넣자
			Editor.AddHotKeyEvent(OnHotKeyRemoveKeyframes, apHotKey.LabelText.RemoveKeyframes, KeyCode.Delete, false, false, false, keyframes);//"Remove Keyframes"


			//키 삭제
			//"  Remove " + keyframes.Count +" Keyframes"

			//이전
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWordFormat(UIWORD.RemoveNumKeyframes, keyframes.Count),
			//										Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
			//										),
			//						GUILayout.Width(width), GUILayout.Height(24)))
			if (_guiContent_Bottom_Animation_RemoveNumKeyframes == null)
			{
				_guiContent_Bottom_Animation_RemoveNumKeyframes = new apGUIContentWrapper();
				_guiContent_Bottom_Animation_RemoveNumKeyframes.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}

			_guiContent_Bottom_Animation_RemoveNumKeyframes.SetText(2, Editor.GetUIWordFormat(UIWORD.RemoveNumKeyframes, keyframes.Count));

			if (GUILayout.Button(_guiContent_Bottom_Animation_RemoveNumKeyframes.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(24)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove Keyframes", "Remove " + keyframes.Count + "s Keyframes?", "Remove", "Cancel");

				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveKeyframes_Title),
																Editor.GetTextFormat(TEXT.RemoveKeyframes_Body, keyframes.Count),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);

				if (isResult)
				{
					Editor.Controller.RemoveKeyframes(keyframes);
				}
			}
		}

		private void DrawEditor_Bottom_AnimationProperty_TimelineLayer(apAnimTimelineLayer timelineLayer, int width)
		{
			//EditorGUILayout.LabelField("Timeline Layer");
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.TimelineLayer));

			GUILayout.Space(10);
			if (timelineLayer._targetParamSetGroup != null &&
				timelineLayer._parentTimeline != null &&
				timelineLayer._parentTimeline._linkedModifier != null
				)
			{
				apModifierParamSetGroup keyParamSetGroup = timelineLayer._targetParamSetGroup;
				apModifierBase modifier = timelineLayer._parentTimeline._linkedModifier;
				//apAnimTimeline timeline = timelineLayer._parentTimeline;

				//이름
				//설정
				Color prevColor = GUI.backgroundColor;

				GUI.backgroundColor = timelineLayer._guiColor;
				//GUIStyle guiStyle_Box = new GUIStyle(GUI.skin.box);
				//guiStyle_Box.alignment = TextAnchor.MiddleCenter;
				//guiStyle_Box.normal.textColor = apEditorUtil.BoxTextColor;

				GUILayout.Box(timelineLayer.DisplayName, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

				GUI.backgroundColor = prevColor;

				GUILayout.Space(10);

				//1. 색상 Modifier라면 색상 옵션을 설정한다.
				if ((int)(modifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.Color) != 0)
				{
					//" Color Option On", " Color Option Off",
					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ColorVisibleOption),
															1, Editor.GetUIWord(UIWORD.ColorOptionOn), Editor.GetUIWord(UIWORD.ColorOptionOff),
															keyParamSetGroup._isColorPropertyEnabled, true,
															width, 24))
					{
						apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, modifier, _animClip._targetMeshGroup, false);
						keyParamSetGroup._isColorPropertyEnabled = !keyParamSetGroup._isColorPropertyEnabled;

						_animClip._targetMeshGroup.RefreshForce();
						Editor.RefreshControllerAndHierarchy(false);
					}

					//추가 : Color Option이 가능하면 Extra 옵션도 가능하다.
					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ExtraOption),
													1, Editor.GetUIWord(UIWORD.ExtraOptionON), Editor.GetUIWord(UIWORD.ExtraOptionOFF),
													modifier._isExtraPropertyEnabled, true, width, 20))
					{
						apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, modifier, _animClip._targetMeshGroup, false);

						modifier._isExtraPropertyEnabled = !modifier._isExtraPropertyEnabled;
						_animClip._targetMeshGroup.RefreshForce();
						Editor.RefreshControllerAndHierarchy(false);
					}
					GUILayout.Space(10);
				}
			}

			//2. GUI Color를 설정
			try
			{
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.LayerGUIColor));//"Layer GUI Color"
				Color nextGUIColor = EditorGUILayout.ColorField(timelineLayer._guiColor, apGUILOFactory.I.Width(width));
				if (nextGUIColor != timelineLayer._guiColor)
				{
					apEditorUtil.SetEditorDirty();
					timelineLayer._guiColor = nextGUIColor;
				}
			}
			catch (Exception) { }

			GUILayout.Space(10);

			//Pose Export / Import
			if (timelineLayer._linkedBone != null
				&& timelineLayer._parentTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier
				)
			{
				//Bone 타입인 경우
				//Pose 복사 / 붙여넣기를 할 수 있다.

				GUILayout.Space(5);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.PoseExportImportLabel), apGUILOFactory.I.Width(width));//"Pose Export / Import"

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
				GUILayout.Space(5);

				//string strExport = " " + Editor.GetUIWord(UIWORD.Export);
				//string strImport = " " + Editor.GetUIWord(UIWORD.Import);

				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_SaveLoad), 1, Editor.GetUIWord(UIWORD.Export), Editor.GetUIWord(UIWORD.Export), false, true, ((width) / 2) - 2, 25))
				{
					if (timelineLayer._parentAnimClip._targetMeshGroup != null)
					{
						apDialog_RetargetSinglePoseExport.ShowDialog(Editor, timelineLayer._parentAnimClip._targetMeshGroup, timelineLayer._linkedBone);
					}
				}
				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_LoadBones), 1, Editor.GetUIWord(UIWORD.Import), Editor.GetUIWord(UIWORD.Import), false, true, ((width) / 2) - 2, 25))
				{
					if (timelineLayer._parentAnimClip._targetMeshGroup != null)
					{
						_loadKey_SinglePoseImport_Anim = apDialog_RetargetSinglePoseImport.ShowDialog(
							OnRetargetSinglePoseImportAnim, Editor,
							timelineLayer._parentAnimClip._targetMeshGroup,
							timelineLayer._parentAnimClip,
							timelineLayer._parentTimeline,
							timelineLayer._parentAnimClip.CurFrame
							);
					}
				}
				EditorGUILayout.EndHorizontal();
			}


			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);
		}

		private void DrawEditor_Bottom_AnimationProperty_Timeline(apAnimTimeline timeline, int width)
		{
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Timeline));//"Timeline"

			GUILayout.Space(10);

			if (timeline._linkedModifier != null
				)
			{
				apModifierBase modifier = timeline._linkedModifier;


				//이름
				//설정
				Color prevColor = GUI.backgroundColor;

				GUI.backgroundColor = timeline._guiColor;
				//GUIStyle guiStyle_Box = new GUIStyle(GUI.skin.box);
				//guiStyle_Box.alignment = TextAnchor.MiddleCenter;
				//guiStyle_Box.normal.textColor = apEditorUtil.BoxTextColor;

				GUILayout.Box(timeline.DisplayName, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

				GUI.backgroundColor = prevColor;

				GUILayout.Space(10);

				//" Color Option On", " Color Option Off",
				//1. 색상 Modifier라면 색상 옵션을 설정한다.
				if ((int)(modifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.Color) != 0)
				{
					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ColorVisibleOption),
															1, Editor.GetUIWord(UIWORD.ColorOptionOn), Editor.GetUIWord(UIWORD.ColorOptionOff),
															modifier._isColorPropertyEnabled, true,
															width, 24))
					{
						apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, modifier, _animClip._targetMeshGroup, false);

						modifier._isColorPropertyEnabled = !modifier._isColorPropertyEnabled;
						_animClip._targetMeshGroup.RefreshForce();
						Editor.RefreshControllerAndHierarchy(false);
					}

					//추가 : Color Option이 가능하면 Extra 옵션도 가능하다.
					//"Extra Option On" / "Extra Option Off"
					if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ExtraOption),
													1, Editor.GetUIWord(UIWORD.ExtraOptionON), Editor.GetUIWord(UIWORD.ExtraOptionOFF),
													modifier._isExtraPropertyEnabled, true, width, 20))
					{
						apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Anim_KeyframeValueChanged, Editor, modifier, _animClip._targetMeshGroup, false);

						modifier._isExtraPropertyEnabled = !modifier._isExtraPropertyEnabled;
						_animClip._targetMeshGroup.RefreshForce();
						Editor.RefreshControllerAndHierarchy(false);
					}
				}
				GUILayout.Space(10);

			}


			//Pose Export / Import
			if (timeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier
				&& timeline._linkedModifier != null
				&& timeline._linkedModifier.IsTarget_Bone)
			{
				//Bone 타입인 경우
				//Pose 복사 / 붙여넣기를 할 수 있다.

				GUILayout.Space(5);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.PoseExportImportLabel), apGUILOFactory.I.Width(width));//"Pose Export / Import"

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
				GUILayout.Space(5);

				//string strExport = " " + Editor.GetUIWord(UIWORD.Export);
				//string strImport = " " + Editor.GetUIWord(UIWORD.Import);

				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_SaveLoad), 1, Editor.GetUIWord(UIWORD.Export), Editor.GetUIWord(UIWORD.Export), false, true, ((width) / 2) - 2, 25))// " Export"
				{
					if (timeline._parentAnimClip._targetMeshGroup != null)
					{
						apDialog_RetargetSinglePoseExport.ShowDialog(Editor, timeline._parentAnimClip._targetMeshGroup, null);
					}
				}
				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_LoadBones), 1, Editor.GetUIWord(UIWORD.Import), Editor.GetUIWord(UIWORD.Import), false, true, ((width) / 2) - 2, 25))//" Import"
				{
					if (timeline._parentAnimClip._targetMeshGroup != null)
					{
						_loadKey_SinglePoseImport_Anim = apDialog_RetargetSinglePoseImport.ShowDialog(
							OnRetargetSinglePoseImportAnim, Editor,
							timeline._parentAnimClip._targetMeshGroup,
							timeline._parentAnimClip,
							timeline,
							timeline._parentAnimClip.CurFrame
							);
					}
				}
				EditorGUILayout.EndHorizontal();
			}


			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);
		}



		private void OnHotKeyRemoveKeyframes(object paramObject)
		{
			if (SelectionType != SELECTION_TYPE.Animation ||
				AnimClip == null)
			{
				return;
			}

			if (paramObject is apAnimKeyframe)
			{
				apAnimKeyframe keyframe = paramObject as apAnimKeyframe;

				if (keyframe != null)
				{
					Editor.Controller.RemoveKeyframe(keyframe);
				}
			}
			else if (paramObject is List<apAnimKeyframe>)
			{
				List<apAnimKeyframe> keyframes = paramObject as List<apAnimKeyframe>;
				if (keyframes != null && keyframes.Count > 0)
				{
					Editor.Controller.RemoveKeyframes(keyframes);
				}
			}
		}


		//------------------------------------------------------------------------------------



		private void DrawEditor_Right2_MeshGroup_Setting(int width, int height)
		{
			bool isMeshTransform = false;
			bool isValidSelect = false;

			if (SubMeshInGroup != null)
			{
				if (SubMeshInGroup._mesh != null)
				{
					isMeshTransform = true;
					isValidSelect = true;
				}
			}
			else if (SubMeshGroupInGroup != null)
			{
				if (SubMeshGroupInGroup._meshGroup != null)
				{
					isMeshTransform = false;
					isValidSelect = true;
				}
			}

			//if (isValidSelect)
			//{
			//	//1-1. 선택된 객체가 존재하여 [객체 정보]를 출력할 수 있다.
			//	Editor.SetGUIVisible("MeshGroupBottom_Setting", true);
			//}
			//else
			//{
			//	//1-2. 선택된 객체가 없어서 우측 상세 정보 UI를 출력하지 않는다.
			//	//수정 -> 기본 루트 MeshGroupTransform을 출력한다.
			//	Editor.SetGUIVisible("MeshGroupBottom_Setting", false);

			//	return; //바로 리턴
			//}

			////2. 출력할 정보가 있다 하더라도
			////=> 바로 출력 가능한게 아니라 경우에 따라 Hide 상태를 조금 더 유지할 필요가 있다.
			//if (!Editor.IsDelayedGUIVisible("MeshGroupBottom_Setting"))
			//{
			//	//아직 출력하면 안된다.
			//	return;
			//}

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupRight_Setting_ObjectSelected, isValidSelect);//"MeshGroupRight_Setting_ObjectSelected"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupRight_Setting_ObjectNotSelected, !isValidSelect);//"MeshGroupRight_Setting_ObjectNotSelected"

			bool isSelectedObjectRender = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupRight_Setting_ObjectSelected);//"MeshGroupRight_Setting_ObjectSelected"
			bool isNotSelectedObjectRender = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupRight_Setting_ObjectNotSelected);//"MeshGroupRight_Setting_ObjectNotSelected"

			if (!isSelectedObjectRender && !isNotSelectedObjectRender)
			{
				return;
			}

			if (_guiContent_Right_MeshGroup_MeshIcon == null) { _guiContent_Right_MeshGroup_MeshIcon = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Mesh)); }
			if (_guiContent_Right_MeshGroup_MeshGroupIcon == null) { _guiContent_Right_MeshGroup_MeshGroupIcon = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_MeshGroup)); }

			if (_guiContent_Right2MeshGroup_ObjectProp_Name == null) { _guiContent_Right2MeshGroup_ObjectProp_Name = new apGUIContentWrapper(); }
			if (_guiContent_Right2MeshGroup_ObjectProp_Type == null) { _guiContent_Right2MeshGroup_ObjectProp_Type = new apGUIContentWrapper(); }
			if (_guiContent_Right2MeshGroup_ObjectProp_NickName == null) { _guiContent_Right2MeshGroup_ObjectProp_NickName = new apGUIContentWrapper(); }

			//1. 오브젝트가 선택이 되었다.
			if (isSelectedObjectRender)
			{
				//string objectName = null;
				//string strType = null;
				//string prevNickName = null;

				_guiContent_Right2MeshGroup_ObjectProp_Name.ClearText(false);
				_guiContent_Right2MeshGroup_ObjectProp_Type.ClearText(false);
				_guiContent_Right2MeshGroup_ObjectProp_NickName.ClearText(false);

				bool isSocket = false;
				if (isMeshTransform)
				{
					//strType = Editor.GetUIWord(UIWORD.Mesh);//"Sub Mesh" -> "Mesh"
					//objectName = SubMeshInGroup._mesh._name;
					//prevNickName = SubMeshInGroup._nickName;

					_guiContent_Right2MeshGroup_ObjectProp_Type.AppendText(Editor.GetUIWord(UIWORD.Mesh), true);
					_guiContent_Right2MeshGroup_ObjectProp_Name.AppendText(SubMeshInGroup._mesh._name, true);
					_guiContent_Right2MeshGroup_ObjectProp_NickName.AppendText(SubMeshInGroup._nickName, true);

					isSocket = SubMeshInGroup._isSocket;
				}
				else
				{
					//strType = Editor.GetUIWord(UIWORD.MeshGroup);//"Sub Mesh Group" -> "Mesh Group"
					//objectName = SubMeshGroupInGroup._meshGroup._name;
					//prevNickName = SubMeshGroupInGroup._nickName;

					_guiContent_Right2MeshGroup_ObjectProp_Type.AppendText(Editor.GetUIWord(UIWORD.MeshGroup), true);
					_guiContent_Right2MeshGroup_ObjectProp_Name.AppendText(SubMeshGroupInGroup._meshGroup._name, true);
					_guiContent_Right2MeshGroup_ObjectProp_NickName.AppendText(SubMeshGroupInGroup._nickName, true);

					isSocket = SubMeshGroupInGroup._isSocket;
				}
				//EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(height));

				//추가
				//1. 아이콘 / 타입
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(50));
				GUILayout.Space(10);
				if (isMeshTransform)
				{
					//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Mesh)), GUILayout.Width(50), GUILayout.Height(50));
					EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_MeshIcon.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(50));
				}
				else
				{
					//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_MeshGroup)), GUILayout.Width(50), GUILayout.Height(50));
					EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_MeshGroupIcon.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(50));
				}

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width - (50 + 10)));
				GUILayout.Space(5);
				EditorGUILayout.LabelField(_guiContent_Right2MeshGroup_ObjectProp_Type.Content, apGUILOFactory.I.Width(width - (50 + 10)));
				EditorGUILayout.LabelField(_guiContent_Right2MeshGroup_ObjectProp_Name.Content, apGUILOFactory.I.Width(width - (50 + 10)));


				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(10);

				//2. 닉네임
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Name), apGUILOFactory.I.Width(80));//"Name"
				string nextNickName = EditorGUILayout.DelayedTextField(_guiContent_Right2MeshGroup_ObjectProp_NickName.Content.text, apGUILOFactory.I.Width(width));
				if (!string.Equals(nextNickName, _guiContent_Right2MeshGroup_ObjectProp_NickName.Content.text))
				{

					if (isMeshTransform)
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						SubMeshInGroup._nickName = nextNickName;
					}
					else
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshGroupInGroup, false, true);
						SubMeshGroupInGroup._nickName = nextNickName;
					}

					Editor.RefreshControllerAndHierarchy(false);
				}


				GUILayout.Space(10);

				//"Socket Enabled", "Socket Disabled"
				if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.SocketEnabled), Editor.GetUIWord(UIWORD.SocketDisabled), isSocket, true, width, 25))
				{
					if (isMeshTransform)
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						SubMeshInGroup._isSocket = !SubMeshInGroup._isSocket;
					}
					else
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshGroupInGroup, false, true);
						SubMeshGroupInGroup._isSocket = !SubMeshGroupInGroup._isSocket;
					}
				}

				GUILayout.Space(10);

				apEditorUtil.GUI_DelimeterBoxH(width - 10);
				GUILayout.Space(10);

				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Render_Unit_Detail_Status__MeshTransform, isMeshTransform);//"Render Unit Detail Status - MeshTransform"
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Render_Unit_Detail_Status__MeshGroupTransform, !isMeshTransform);//"Render Unit Detail Status - MeshGroupTransform"

				bool isMeshTransformDetailRendererable = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Render_Unit_Detail_Status__MeshTransform);//"Render Unit Detail Status - MeshTransform"
				bool isMeshGroupTransformDetailRendererable = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Render_Unit_Detail_Status__MeshGroupTransform);//"Render Unit Detail Status - MeshGroupTransform"





				//3. Mesh Transform Setting
				if (isMeshTransform && isMeshTransformDetailRendererable)
				{
					//"Shader Setting"
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ShaderSetting));

					GUILayout.Space(5);

					//Blend 방식 (Enum)
					apPortrait.SHADER_TYPE nextShaderType = (apPortrait.SHADER_TYPE)EditorGUILayout.EnumPopup(SubMeshInGroup._shaderType);
					if (nextShaderType != SubMeshInGroup._shaderType)
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						SubMeshInGroup._shaderType = nextShaderType;
					}

					GUILayout.Space(5);

					//Shader Mode (Material Library <-> Custom Shader)
					int shaderMode = SubMeshInGroup._isCustomShader ? MESH_SHADER_MODE__CUSTOM_SHADER : MESH_SHADER_MODE__MATERIAL_SET;
					int nextShaderMode = EditorGUILayout.Popup(shaderMode, _shaderMode_Names);

					if (nextShaderMode != shaderMode)
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						SubMeshInGroup._isCustomShader = (nextShaderMode == MESH_SHADER_MODE__CUSTOM_SHADER);
						apEditorUtil.ReleaseGUIFocus();
					}

					bool isCustomShader = SubMeshInGroup._isCustomShader;

					GUILayout.Space(5);
					//현재 모드를 Box로 설명
					//GUIStyle guiStyle_BoxCenter = new GUIStyle(GUI.skin.box);
					//guiStyle_BoxCenter.alignment = TextAnchor.MiddleCenter;

					//Box의 색상을 지정해주자.
					Color prevColor = GUI.backgroundColor;

					if (_guiContent_Right_MeshGroup_MaterialSet == null)
					{
						_guiContent_Right_MeshGroup_MaterialSet = apGUIContentWrapper.Make(1, _editor.GetUIWord(UIWORD.MaterialSet), _editor.ImageSet.Get(apImageSet.PRESET.MaterialSet));
					}
					if (_guiContent_Right_MeshGroup_CustomShader == null)
					{
						_guiContent_Right_MeshGroup_CustomShader = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.CustomShader), _editor.ImageSet.Get(apImageSet.PRESET.MaterialSet_CustomShader));
					}


					if (!isCustomShader)
					{
						//Material Library
						//초록색
						GUI.backgroundColor = new Color(prevColor.r * 0.7f, prevColor.g * 1.0f, prevColor.b * 0.7f, 1.0f);
						//" Material Set"
						//이전
						//GUILayout.Box(new GUIContent(" " + _editor.GetUIWord(UIWORD.MaterialSet), _editor.ImageSet.Get(apImageSet.PRESET.MaterialSet)), guiStyle_BoxCenter, GUILayout.Width(width), GUILayout.Height(30));

						//변경
						GUILayout.Box(_guiContent_Right_MeshGroup_MaterialSet.Content, apGUIStyleWrapper.I.Box_MiddleCenter, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					}
					else
					{
						//Custom Shader > Material Set을 사용하지 않는다.
						//파란색
						GUI.backgroundColor = new Color(prevColor.r * 0.7f, prevColor.g * 0.9f, prevColor.b * 1.0f, 1.0f);

						//이전
						//GUILayout.Box(new GUIContent(" " + Editor.GetUIWord(UIWORD.CustomShader),
						//								_editor.ImageSet.Get(apImageSet.PRESET.MaterialSet_CustomShader)),
						//					guiStyle_BoxCenter,
						//					GUILayout.Width(width), GUILayout.Height(30));

						GUILayout.Box(_guiContent_Right_MeshGroup_CustomShader.Content,
											apGUIStyleWrapper.I.Box_MiddleCenter,
											apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
					}

					GUI.backgroundColor = prevColor;

					GUILayout.Space(5);

					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__CustomShader, isCustomShader);//"MeshGroup Mesh Setting - CustomShader"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__MaterialLibrary, !isCustomShader);//"MeshGroup Mesh Setting - MaterialLibrary"
					bool isDraw_CustomShader = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__CustomShader);//"MeshGroup Mesh Setting - CustomShader"
					bool isDraw_MaterialLibrary = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__MaterialLibrary);//"MeshGroup Mesh Setting - MaterialLibrary"

					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__MatLib_NotUseDefault, !isCustomShader && !SubMeshInGroup._isUseDefaultMaterialSet);//"MeshGroup Mesh Setting - MatLib/NotUseDefault"
					bool isDraw_MatLib_NotUseDefault = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__MatLib_NotUseDefault);//"MeshGroup Mesh Setting - MatLib/NotUseDefault"

					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__Same_Mesh, _tmp_PrevMeshTransform_MeshGroupSettingUI == SubMeshInGroup);//"MeshGroup Mesh Setting - Same Mesh"
					bool isSameMeshTF = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Mesh_Setting__Same_Mesh);//"MeshGroup Mesh Setting - Same Mesh"
					if (Event.current.type != EventType.Layout)
					{
						_tmp_PrevMeshTransform_MeshGroupSettingUI = SubMeshInGroup;
					}

					//if (!isCustomShader)
					if (isDraw_MaterialLibrary)
					{
						//1. Material Library 모드
						//"Material Set"
						EditorGUILayout.LabelField(_editor.GetUIWord(UIWORD.MaterialSet));

						//"Use Default Material Set"
						//bool nextUseDefaultMaterialSet = EditorGUILayout.Toggle(, SubMeshInGroup._isUseDefaultMaterialSet);

						if (_guiContent_MaterialSet_ON == null)
						{
							_guiContent_MaterialSet_ON = new apGUIContentWrapper();
							_guiContent_MaterialSet_ON.ClearText(false);
							_guiContent_MaterialSet_ON.AppendText(Editor.GetUIWord(UIWORD.UseDefaultMaterialSet), false);
							_guiContent_MaterialSet_ON.AppendSpaceText(1, false);
							_guiContent_MaterialSet_ON.AppendText(apStringFactory.I.ON, true);
						}
						if (_guiContent_MaterialSet_OFF == null)
						{
							_guiContent_MaterialSet_OFF = new apGUIContentWrapper();
							_guiContent_MaterialSet_OFF.ClearText(false);
							_guiContent_MaterialSet_OFF.AppendText(Editor.GetUIWord(UIWORD.UseDefaultMaterialSet), false);
							_guiContent_MaterialSet_OFF.AppendSpaceText(1, false);
							_guiContent_MaterialSet_OFF.AppendText(apStringFactory.I.OFF, true);
						}



						//if (nextUseDefaultMaterialSet != SubMeshInGroup._isUseDefaultMaterialSet)
						if (apEditorUtil.ToggledButton_2Side(_guiContent_MaterialSet_ON.Content.text,
																_guiContent_MaterialSet_OFF.Content.text,
																SubMeshInGroup._isUseDefaultMaterialSet, true, width, 22))
						{
							apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
							bool nextUseDefaultMaterialSet = !SubMeshInGroup._isUseDefaultMaterialSet;

							SubMeshInGroup._isUseDefaultMaterialSet = nextUseDefaultMaterialSet;

							if (nextUseDefaultMaterialSet)
							{
								//Default MaterialSet이 False > True로 바뀐 경우
								SubMeshInGroup._linkedMaterialSet = Portrait.GetDefaultMaterialSet();
								if (SubMeshInGroup._linkedMaterialSet != null)
								{
									SubMeshInGroup._materialSetID = SubMeshInGroup._linkedMaterialSet._uniqueID;
								}
							}

							apEditorUtil.ReleaseGUIFocus();
						}



						if (!SubMeshInGroup._isUseDefaultMaterialSet && isDraw_MatLib_NotUseDefault)
						{
							//Default Material Set을 사용하지 않는 경우
							//EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(26));
							//GUILayout.Space(5);
							if (SubMeshInGroup._linkedMaterialSet != null)
							{
								//string matSetName = " " + SubMeshInGroup._linkedMaterialSet._name;
								Texture2D matSetImg = null;
								switch (SubMeshInGroup._linkedMaterialSet._icon)
								{
									case apMaterialSet.ICON.Unlit: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_Unlit); break;
									case apMaterialSet.ICON.Lit: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_Lit); break;
									case apMaterialSet.ICON.LitSpecular: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_LitSpecular); break;
									case apMaterialSet.ICON.LitSpecularEmission: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_LitSpecularEmission); break;
									case apMaterialSet.ICON.LitRimlight: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_LitRim); break;
									case apMaterialSet.ICON.LitRamp: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_LitRamp); break;
									case apMaterialSet.ICON.Effect: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_FX); break;
									case apMaterialSet.ICON.Cartoon: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_Cartoon); break;
									case apMaterialSet.ICON.Custom1: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_Custom1); break;
									case apMaterialSet.ICON.Custom2: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_Custom2); break;
									case apMaterialSet.ICON.Custom3: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_Custom3); break;
									case apMaterialSet.ICON.UnlitVR: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_UnlitVR); break;
									case apMaterialSet.ICON.LitVR: matSetImg = Editor.ImageSet.Get(apImageSet.PRESET.MaterialSetIcon_LitVR); break;
								}

								//이전
								//GUILayout.Box(new GUIContent(matSetName, matSetImg), guiStyle_BoxCenter, GUILayout.Width(width), GUILayout.Height(30));

								//변경
								if (_guiContent_Right_MeshGroup_MatSetName == null)
								{
									_guiContent_Right_MeshGroup_MatSetName = new apGUIContentWrapper();
								}

								_guiContent_Right_MeshGroup_MatSetName.SetText(1, SubMeshInGroup._linkedMaterialSet._name);
								_guiContent_Right_MeshGroup_MatSetName.SetImage(matSetImg);

								GUILayout.Box(_guiContent_Right_MeshGroup_MatSetName.Content, apGUIStyleWrapper.I.Box_MiddleCenter, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
							}
							else
							{
								//붉은색으로 표시
								GUI.backgroundColor = new Color(prevColor.r * 1.0f, prevColor.g * 0.7f, prevColor.b * 0.7f, 1.0f);
								//GUILayout.Box(Editor.GetUIWord(UIWORD.None), guiStyle_BoxCenter, GUILayout.Width(width - (5 + 80)), GUILayout.Height(26));
								GUILayout.Box(Editor.GetUIWord(UIWORD.None), apGUIStyleWrapper.I.Box_MiddleCenter, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
								GUI.backgroundColor = prevColor;
							}
							//if (GUILayout.Button(_editor.GetUIWord(UIWORD.Change), GUILayout.Width(80), GUILayout.Height(26)))
							if (GUILayout.Button(_editor.GetUIWord(UIWORD.Change), apGUILOFactory.I.Height(22)))
							{
								//Material Set을 선택하자.
								_loadKey_SelectMaterialSetOfMeshTransform = apDialog_SelectMaterialSet.ShowDialog(
																								Editor,
																								false,
																								Editor.GetUIWord(UIWORD.SelectMaterialSet),//"Select Material Set",
																								false, OnMaterialSetOfMeshTFSelected,
																								SubMeshInGroup);
							}

							//EditorGUILayout.EndHorizontal();
						}
						//Material Library 열기 버튼
						//"Open Material Library"
						if (GUILayout.Button(Editor.GetUIWord(UIWORD.OpenMaterialLibrary), apGUILOFactory.I.Height(24)))
						{
							apDialog_MaterialLibrary.ShowDialog(Editor, Portrait);
						}
					}

					if (isDraw_CustomShader)
					{
						//2. Custom Shader 모드
						try
						{
							Shader nextCustomShader = (Shader)EditorGUILayout.ObjectField(SubMeshInGroup._customShader, typeof(Shader), false);
							if (SubMeshInGroup._customShader != nextCustomShader)
							{
								//Object Field 열때 Record하면 안된다.
								//apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);

								SubMeshInGroup._customShader = nextCustomShader;

								//apEditorUtil.ReleaseGUIFocus();
								apEditorUtil.SetEditorDirty();
							}
						}
						catch (Exception) { }
					}
					GUILayout.Space(20);

					//추가 19.6.10 : Shader Parameter의 기본값을 정하자
					//"Custom Shader Properties"
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.CustomShaderProperties));
					GUILayout.Space(5);

					//GUIStyle guiStyle_PropRemoveBtn = new GUIStyle(GUI.skin.button);
					//guiStyle_PropRemoveBtn.margin = GUI.skin.textField.margin;

					List<apTransform_Mesh.CustomMaterialProperty> cutomMatProps = SubMeshInGroup._customMaterialProperties;
					apTransform_Mesh.CustomMaterialProperty removeMatProp = null;


					if (isSameMeshTF)//출력 가능한 경우
					{
						for (int iMatProp = 0; iMatProp < cutomMatProps.Count; iMatProp++)
						{
							apTransform_Mesh.CustomMaterialProperty matProp = cutomMatProps[iMatProp];

							//Line1 : 이름, 타입, 삭제
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));

							GUILayout.Space(5);
							string nextPropName = EditorGUILayout.DelayedTextField(matProp._name, apGUILOFactory.I.Width(width - (5 + 70 + 20 + 10)));
							if (!string.Equals(nextPropName, matProp._name))
							{
								//이름 변경
								apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
								matProp._name = nextPropName;
							}
							apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE nextPropType =
								(apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE)EditorGUILayout.EnumPopup(matProp._propType, apGUILOFactory.I.Width(70));

							if (nextPropType != matProp._propType)
							{
								//타입 변경
								apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
								matProp._propType = nextPropType;
							}

							GUILayout.Space(5);
							if (GUILayout.Button(apStringFactory.I.X, apGUIStyleWrapper.I.Button_TextFieldMargin, apGUILOFactory.I.Width(18), apGUILOFactory.I.Height(18)))//"X"
							{
								//삭제
								removeMatProp = matProp;
							}
							EditorGUILayout.EndHorizontal();

							//Line2 : 속성에 따른 값
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(24));
							GUILayout.Space(10);
							int width_PropLabel = 80;
							int width_PropValue = width - (width_PropLabel + 10 + 5);
							switch (matProp._propType)
							{
								case apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE.Float:
									{
										EditorGUILayout.LabelField(apStringFactory.I.Float, apGUILOFactory.I.Width(width_PropLabel));//"Float"
										float nextFloatValue = EditorGUILayout.DelayedFloatField(matProp._value_Float, apGUILOFactory.I.Width(width_PropValue));
										if (Mathf.Abs(nextFloatValue - matProp._value_Float) > 0.0001f)
										{
											apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
											matProp._value_Float = nextFloatValue;
											apEditorUtil.ReleaseGUIFocus();
										}
									}
									break;
								case apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE.Int:
									{
										EditorGUILayout.LabelField(apStringFactory.I.Integer, apGUILOFactory.I.Width(width_PropLabel));//"Integer"
										int nextIntValue = EditorGUILayout.DelayedIntField(matProp._value_Int, apGUILOFactory.I.Width(width_PropValue));
										if (nextIntValue != matProp._value_Int)
										{
											apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
											matProp._value_Int = nextIntValue;
											apEditorUtil.ReleaseGUIFocus();
										}
									}
									break;
								case apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE.Vector:
									{
										int width_PropVecValue = (width_PropValue / 4) - 3;
										EditorGUILayout.LabelField(apStringFactory.I.Vector, apGUILOFactory.I.Width(width_PropLabel));//"Vector"
										float nextV_X = EditorGUILayout.DelayedFloatField(matProp._value_Vector.x, apGUILOFactory.I.Width(width_PropVecValue));
										float nextV_Y = EditorGUILayout.DelayedFloatField(matProp._value_Vector.y, apGUILOFactory.I.Width(width_PropVecValue));
										float nextV_Z = EditorGUILayout.DelayedFloatField(matProp._value_Vector.z, apGUILOFactory.I.Width(width_PropVecValue));
										float nextV_W = EditorGUILayout.DelayedFloatField(matProp._value_Vector.w, apGUILOFactory.I.Width(width_PropVecValue + 1));

										if (Mathf.Abs(nextV_X - matProp._value_Vector.x) > 0.0001f ||
											Mathf.Abs(nextV_Y - matProp._value_Vector.y) > 0.0001f ||
											Mathf.Abs(nextV_Z - matProp._value_Vector.z) > 0.0001f ||
											Mathf.Abs(nextV_W - matProp._value_Vector.w) > 0.0001f)
										{
											apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
											matProp._value_Vector.x = nextV_X;
											matProp._value_Vector.y = nextV_Y;
											matProp._value_Vector.z = nextV_Z;
											matProp._value_Vector.w = nextV_W;
											apEditorUtil.ReleaseGUIFocus();
										}
									}
									break;
								case apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE.Texture:
									{
										EditorGUILayout.LabelField(apStringFactory.I.Texture, apGUILOFactory.I.Width(width_PropLabel));//"Texture"
										try
										{
											Texture nextTex = (Texture)EditorGUILayout.ObjectField(matProp._value_Texture, typeof(Texture), false, apGUILOFactory.I.Width(width_PropValue));
											if (nextTex != matProp._value_Texture)
											{
												//Object Field 사용시 Record 불가
												//apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
												matProp._value_Texture = nextTex;
												//apEditorUtil.ReleaseGUIFocus();
											}
										}
										catch (Exception) { }

									}
									break;
								case apTransform_Mesh.CustomMaterialProperty.SHADER_PROP_TYPE.Color:
									{
										EditorGUILayout.LabelField(apStringFactory.I.Color, apGUILOFactory.I.Width(width_PropLabel));//"Color"
										try
										{
											Color nextColor = EditorGUILayout.ColorField(matProp._value_Color, apGUILOFactory.I.Width(width_PropValue));
											if (Mathf.Abs(nextColor.r - matProp._value_Color.r) > 0.0001f ||
												Mathf.Abs(nextColor.g - matProp._value_Color.g) > 0.0001f ||
												Mathf.Abs(nextColor.b - matProp._value_Color.b) > 0.0001f ||
												Mathf.Abs(nextColor.a - matProp._value_Color.a) > 0.0001f)
											{
												//색상은 Record 불가
												//apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
												matProp._value_Color = nextColor;
												//apEditorUtil.ReleaseGUIFocus();
											}
										}
										catch (Exception) { }
									}
									break;
							}

							EditorGUILayout.EndHorizontal();

							GUILayout.Space(5);
						}
					}

					if (removeMatProp != null)
					{
						//"Remove Shader Property", "Do you want to remove the Custom Property [ " + removeMatProp._name + " ]?"
						bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.DLG_RemoveCustomShaderProp_Title),
																		Editor.GetTextFormat(TEXT.DLG_RemoveCustomShaderProp_Body, removeMatProp._name),
																		Editor.GetText(TEXT.Remove),
																		Editor.GetText(TEXT.Cancel));

						if (isResult)
						{
							apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
							cutomMatProps.Remove(removeMatProp);
						}
						removeMatProp = null;
					}

					//"Add Custom Proprty"
					if (GUILayout.Button(Editor.GetUIWord(UIWORD.AddCustomProperty)))
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						apTransform_Mesh.CustomMaterialProperty newProp = new apTransform_Mesh.CustomMaterialProperty();
						newProp.MakeEmpty();
						cutomMatProps.Add(newProp);
					}


					GUILayout.Space(20);

					//양면 렌더링

					//이전 : 토글 UI
					bool isNext2Side = EditorGUILayout.Toggle(Editor.GetUIWord(UIWORD.TwoSidesRendering), SubMeshInGroup._isAlways2Side);//"2-Sides"
					if (SubMeshInGroup._isAlways2Side != isNext2Side)
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						SubMeshInGroup._isAlways2Side = isNext2Side;
					}

					//변경 19.6.10 : 토글 버튼
					//string str_twoSidesRendering = Editor.GetUIWord(UIWORD.TwoSidesRendering);
					//if(apEditorUtil.ToggledButton_2Side(str_twoSidesRendering, str_twoSidesRendering, SubMeshInGroup._isAlways2Side, true, width, 20))
					//{
					//	apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
					//	SubMeshInGroup._isAlways2Side = !SubMeshInGroup._isAlways2Side;
					//}

					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width - 10);
					GUILayout.Space(10);

					//추가 9.25 : 그림자 설정 : TODO 번역
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ShadowSetting));//"Shadow Setting"

					//"Override Shadow Setting", "Use Common Shadow Setting"
					if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.OverrideShadow), Editor.GetUIWord(UIWORD.UseCommonShadowSetting), !SubMeshInGroup._isUsePortraitShadowOption, true, width, 20))
					{
						apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
						SubMeshInGroup._isUsePortraitShadowOption = !SubMeshInGroup._isUsePortraitShadowOption;
					}
					GUILayout.Space(5);
					apPortrait.SHADOW_CASTING_MODE prevShadowCastMode = SubMeshInGroup._isUsePortraitShadowOption ? Portrait._meshShadowCastingMode : SubMeshInGroup._shadowCastingMode;
					bool prevReceiveShadow = SubMeshInGroup._isUsePortraitShadowOption ? Portrait._meshReceiveShadow : SubMeshInGroup._receiveShadow;


					//"Cast Shadows"
					apPortrait.SHADOW_CASTING_MODE nextChastShadows = (apPortrait.SHADOW_CASTING_MODE)EditorGUILayout.EnumPopup(Editor.GetUIWord(UIWORD.CastShadows), prevShadowCastMode);
					//"Receive Shadows"
					bool nextReceiveShaodw = EditorGUILayout.Toggle(Editor.GetUIWord(UIWORD.ReceiveShadows), prevReceiveShadow);

					if (!SubMeshInGroup._isUsePortraitShadowOption)
					{
						if (nextChastShadows != SubMeshInGroup._shadowCastingMode
							|| nextReceiveShaodw != SubMeshInGroup._receiveShadow)
						{
							apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, _meshGroup, SubMeshInGroup, false, true);
							SubMeshInGroup._shadowCastingMode = nextChastShadows;
							SubMeshInGroup._receiveShadow = nextReceiveShaodw;
						}
					}

					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width - 10);
					GUILayout.Space(10);

					//다른 메시로 설정 복사하기
					//" Copy Settings to Other Meshes"

					//이전
					//if (GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.CopySettingsToOtherMeshes), Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy)), GUILayout.Height(24)))
					if (_guiContent_Right_MeshGroup_CopySettingToOtherMeshes == null)
					{
						_guiContent_Right_MeshGroup_CopySettingToOtherMeshes = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.CopySettingsToOtherMeshes), Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy));
					}

					if (GUILayout.Button(_guiContent_Right_MeshGroup_CopySettingToOtherMeshes.Content, apGUILOFactory.I.Height(24)))
					{
						_loadKey_SelectOtherMeshTransformForCopyingSettings = apDialog_SelectMeshTransformsToCopy.ShowDialog(
																	Editor,
																	MeshGroup,
																	SubMeshInGroup,
																	OnSelectOtherMeshTransformsForCopyingSettings);
					}

					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width - 10);
					GUILayout.Space(10);

					//GUIStyle guiStyle_ClipStatus = new GUIStyle(GUI.skin.box);
					//guiStyle_ClipStatus.alignment = TextAnchor.MiddleCenter;
					//guiStyle_ClipStatus.normal.textColor = apEditorUtil.BoxTextColor;

					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Transform_Detail_Status__Clipping_Child, SubMeshInGroup._isClipping_Child);//"Mesh Transform Detail Status - Clipping Child"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Transform_Detail_Status__Clipping_Parent, SubMeshInGroup._isClipping_Parent);//"Mesh Transform Detail Status - Clipping Parent"
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Transform_Detail_Status__Clipping_None, (!SubMeshInGroup._isClipping_Parent && !SubMeshInGroup._isClipping_Child));//"Mesh Transform Detail Status - Clipping None"

					if (SubMeshInGroup._isClipping_Parent)
					{
						if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Transform_Detail_Status__Clipping_Parent))//"Mesh Transform Detail Status - Clipping Parent"
						{
							//1. 자식 메시를 가지는 Clipping의 Base Parent이다.
							//- Mask 사이즈를 보여준다.
							//- 자식 메시 리스트들을 보여준다.
							//-> 레이어 순서를 바꾼다. / Clip을 해제한다..

							//"Parent Mask Mesh"
							GUILayout.Box(Editor.GetUIWord(UIWORD.ParentMaskMesh), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
							GUILayout.Space(5);

							EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.MaskTextureSize), apGUILOFactory.I.Width(width));//"Mask Texture Size"
							int prevRTTIndex = (int)SubMeshInGroup._renderTexSize;
							if (prevRTTIndex < 0 || prevRTTIndex >= apEditorUtil.GetRenderTextureSizeNames().Length)
							{
								prevRTTIndex = (int)(apTransform_Mesh.RENDER_TEXTURE_SIZE.s_256);
							}
							int nextRTTIndex = EditorGUILayout.Popup(prevRTTIndex, apEditorUtil.GetRenderTextureSizeNames(), apGUILOFactory.I.Width(width));
							if (nextRTTIndex != prevRTTIndex)
							{
								apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_ClippingChanged, Editor, MeshGroup, null, false, true);
								SubMeshInGroup._renderTexSize = (apTransform_Mesh.RENDER_TEXTURE_SIZE)nextRTTIndex;
							}


							GUILayout.Space(5);


							//Texture2D btnImg_Down = Editor.ImageSet.Get(apImageSet.PRESET.Modifier_LayerDown);
							//Texture2D btnImg_Up = Editor.ImageSet.Get(apImageSet.PRESET.Modifier_LayerUp);
							Texture2D btnImg_Delete = Editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey);

							int iBtn = -1;
							//int btnRequestType = -1;


							for (int iChild = 0; iChild < SubMeshInGroup._clipChildMeshes.Count; iChild++)
							{
								apTransform_Mesh childMesh = SubMeshInGroup._clipChildMeshes[iChild]._meshTransform;
								EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
								if (childMesh != null)
								{
									EditorGUILayout.LabelField(childMesh._nickName, apGUILOFactory.I.Width(width - (20 + 5)), apGUILOFactory.I.Height(20));
									if (GUILayout.Button(btnImg_Delete, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
									{
										iBtn = iChild;
										//btnRequestType = 2;//2 : Delete

									}
								}
								else
								{
									EditorGUILayout.LabelField(apStringFactory.I.Dot3, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
								}
								EditorGUILayout.EndHorizontal();
							}


							if (iBtn >= 0)
							{
								//Debug.LogError("TODO : Mesh 삭제");
								apTransform_Mesh targetChildTransform = SubMeshInGroup._clipChildMeshes[iBtn]._meshTransform;
								if (targetChildTransform != null)
								{
									//해당 ChildMesh를 Release하자
									Editor.Controller.ReleaseClippingMeshTransform(MeshGroup, targetChildTransform);
								}
							}
						}
					}
					else if (SubMeshInGroup._isClipping_Child)
					{
						if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Mesh_Transform_Detail_Status__Clipping_Child))//"Mesh Transform Detail Status - Clipping Child"
						{
							//2. Parent를 Mask로 삼는 자식 Mesh이다.
							//- 부모 메시를 보여준다.
							//-> 순서 바꾸기를 요청한다
							//-> Clip을 해제한다.
							//"Child Clipped Mesh" ->"Clipped Child Mesh"
							GUILayout.Box(Editor.GetUIWord(UIWORD.ClippedChildMesh), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
							GUILayout.Space(5);

							if (_guiContent_Right2MeshGroup_MaskParentName == null)
							{
								_guiContent_Right2MeshGroup_MaskParentName = new apGUIContentWrapper();
							}

							//string strParentName = "<No Mask Parent>";
							if (SubMeshInGroup._clipParentMeshTransform != null)
							{
								//strParentName = SubMeshInGroup._clipParentMeshTransform._nickName;
								_guiContent_Right2MeshGroup_MaskParentName.ClearText(false);
								_guiContent_Right2MeshGroup_MaskParentName.AppendText(Editor.GetUIWord(UIWORD.MaskMesh), false);
								_guiContent_Right2MeshGroup_MaskParentName.AppendText(apStringFactory.I.Colon_Space, false);
								_guiContent_Right2MeshGroup_MaskParentName.AppendText(SubMeshInGroup._clipParentMeshTransform._nickName, true);
							}
							else
							{
								_guiContent_Right2MeshGroup_MaskParentName.ClearText(false);
								_guiContent_Right2MeshGroup_MaskParentName.AppendText(Editor.GetUIWord(UIWORD.MaskMesh), false);
								_guiContent_Right2MeshGroup_MaskParentName.AppendText(apStringFactory.I.Colon_Space, false);
								_guiContent_Right2MeshGroup_MaskParentName.AppendText(apStringFactory.I.NoMaskParent, true);
							}

							//"Mask Parent" -> "Mask Mesh"
							EditorGUILayout.LabelField(_guiContent_Right2MeshGroup_MaskParentName.Content, apGUILOFactory.I.Width(width));
							//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ClippedIndex) + " : " + SubMeshInGroup._clipIndexFromParent, GUILayout.Width(width));//"Clipped Index : "//<<필요없으니 삭제 19.12.6
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

							//int btnRequestType = -1;
							//"Release"
							if (GUILayout.Button(Editor.GetUIWord(UIWORD.Release), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))
							{
								//btnRequestType = 2;//2 : Delete
								Editor.Controller.ReleaseClippingMeshTransform(MeshGroup, SubMeshInGroup);
							}
							EditorGUILayout.EndHorizontal();


						}
					}
					else
					{
						//3. 기본 상태의 Mesh이다.
						//Clip을 요청한다.
						//"Clipping To Below Mesh" -> "Clip to Below Mesh"
						if (GUILayout.Button(Editor.GetUIWord(UIWORD.ClipToBelowMesh), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))
						{
							Editor.Controller.AddClippingMeshTransform(MeshGroup, SubMeshInGroup, true);
						}
					}
				}
				//else if (!isMeshTransform && isMeshGroupTransformDetailRendererable)
				//{

				//}

				if (isMeshTransformDetailRendererable || isMeshGroupTransformDetailRendererable)
				{
					//추가 20.1.16
					//Detach 외에도 Duplicate기능을 추가하자

					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width - 10);
					GUILayout.Space(10);

					//Duplicate 버튼
					if (_guiContent_Right2MeshGroup_DuplicateTransform == null)
					{
						_guiContent_Right2MeshGroup_DuplicateTransform = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.Duplicate), false);
					}
					if (GUILayout.Button(_guiContent_Right2MeshGroup_DuplicateTransform.Content))
					{
						//복사하기
						if (isMeshTransform)
						{
							Editor.Controller.DuplicateMeshTransformInSameMeshGroup(SubMeshInGroup);
						}
						else
						{
							Editor.Controller.DuplicateMeshGroupTransformInSameMeshGroup(SubMeshGroupInGroup);
						}
					}

					//20.1.20 : Migrate 버튼 추가
					if (isMeshTransform && isMeshTransformDetailRendererable)
					{

						if (_guiContent_Right2MeshGroup_MigrateTransform == null)
						{
							//TODO : 언어
							//"Migrate"
							_guiContent_Right2MeshGroup_MigrateTransform = apGUIContentWrapper.Make(Editor.GetUIWord(UIWORD.Migrate), false);
						}

						if (GUILayout.Button(_guiContent_Right2MeshGroup_MigrateTransform.Content))
						{
							//추가 20.1.18
							//다른 메시 그룹으로 메시를 이전하자
							_loadKey_MigrateMeshTransform = apDialog_SelectMigrateMeshGroup.ShowDialog(Editor, SubMeshInGroup, OnSelectMeshGroupToMigrate);
						}
					}

					//4. Detach
					GUILayout.Space(10);
					apEditorUtil.GUI_DelimeterBoxH(width - 10);
					GUILayout.Space(10);
					if (_guiContent_Right2MeshGroup_DetachObject == null)
					{
						_guiContent_Right2MeshGroup_DetachObject = new apGUIContentWrapper();
					}

					_guiContent_Right2MeshGroup_DetachObject.ClearText(false);
					_guiContent_Right2MeshGroup_DetachObject.AppendSpaceText(2, false);
					_guiContent_Right2MeshGroup_DetachObject.AppendText(Editor.GetUIWord(UIWORD.Detach), false);
					_guiContent_Right2MeshGroup_DetachObject.AppendSpaceText(1, false);
					_guiContent_Right2MeshGroup_DetachObject.AppendText(apStringFactory.I.Bracket_2_L, false);
					//_guiContent_Right2MeshGroup_DetachObject.AppendText(_guiContent_Right2MeshGroup_ObjectProp_Type.Content.text, true);
					if(_guiContent_Right2MeshGroup_ObjectProp_NickName.Content.text.Length > 10)
					{
						_guiContent_Right2MeshGroup_DetachObject.AppendText(_guiContent_Right2MeshGroup_ObjectProp_NickName.Content.text.Substring(0, 10), false);
						_guiContent_Right2MeshGroup_DetachObject.AppendText(apStringFactory.I.Dot2, false);
					}
					else
					{
						_guiContent_Right2MeshGroup_DetachObject.AppendText(_guiContent_Right2MeshGroup_ObjectProp_NickName.Content.text, false);
					}
					
					_guiContent_Right2MeshGroup_DetachObject.AppendText(apStringFactory.I.Bracket_2_R, true);
					_guiContent_Right2MeshGroup_DetachObject.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));


					//Detach 버튼
					if (GUILayout.Button(_guiContent_Right2MeshGroup_DetachObject.Content, apGUILOFactory.I.Height(25)))//"Detach [" + strType + "]"
					{
						string strDialogInfo = Editor.GetText(TEXT.Detach_Body);
						if (isMeshTransform)
						{
							strDialogInfo = Editor.Controller.GetRemoveItemMessage(
																_portrait,
																SubMeshInGroup,
																5,
																Editor.GetText(TEXT.Detach_Body),
																Editor.GetText(TEXT.DLG_RemoveItemChangedWarning));
						}
						else
						{
							strDialogInfo = Editor.Controller.GetRemoveItemMessage(
																_portrait,
																SubMeshGroupInGroup,
																5,
																Editor.GetText(TEXT.Detach_Body),
																Editor.GetText(TEXT.DLG_RemoveItemChangedWarning));
						}

						//bool isResult = EditorUtility.DisplayDialog("Detach", "Detach it?", "Detach", "Cancel");
						bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.Detach_Title),
																		//Editor.GetText(TEXT.Detach_Body),
																		strDialogInfo,
																		Editor.GetText(TEXT.Detach_Ok),
																		Editor.GetText(TEXT.Cancel)
																		);
						if (isResult)
						{
							if (isMeshTransform)
							{
								Editor.Controller.DetachMeshInMeshGroup(SubMeshInGroup, MeshGroup);
								Editor.Select.SetSubMeshInGroup(null);
							}
							else
							{
								Editor.Controller.DetachMeshGroupInMeshGroup(SubMeshGroupInGroup, MeshGroup);
								Editor.Select.SetSubMeshGroupInGroup(null);
							}
						}
						MeshGroup.SetDirtyToSort();//TODO : Sort에서 자식 객체 변한것 체크 : Clip 그룹 체크
						MeshGroup.RefreshForce();
						Editor.SetRepaint();
					}
					//EditorGUILayout.EndVertical();
				}
			}
			else if (isNotSelectedObjectRender)
			{
				//2. 오브젝트가 선택이 안되었다.
				//기본 정보를 출력하고, 루트 MeshGroupTransform의 Transform 값을 설정한다.
				apTransform_MeshGroup rootMeshGroupTransform = MeshGroup._rootMeshGroupTransform;

				if (_guiContent_Right_MeshGroup_MeshGroupIcon == null) { _guiContent_Right_MeshGroup_MeshGroupIcon = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_MeshGroup)); }

				//1. 아이콘 / 타입
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(50));
				GUILayout.Space(10);

				//EditorGUILayout.LabelField(new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_MeshGroup)), GUILayout.Width(50), GUILayout.Height(50));
				EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_MeshGroupIcon.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(50));

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width - (50 + 10)));
				GUILayout.Space(5);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.MeshGroup), apGUILOFactory.I.Width(width - (50 + 12)));//"Mesh Group"
				EditorGUILayout.LabelField(MeshGroup._name, apGUILOFactory.I.Width(width - (50 + 12)));

				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(20);
				apEditorUtil.GUI_DelimeterBoxH(width - 10);
				GUILayout.Space(10);


				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width));


				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.RootTransform));//"Root Transform"

				//Texture2D img_Pos = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Move);
				//Texture2D img_Rot = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Rotate);
				//Texture2D img_Scale = Editor.ImageSet.Get(apImageSet.PRESET.Transform_Scale);

				if (_guiContent_Icon_ModTF_Pos == null) { _guiContent_Icon_ModTF_Pos = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Move)); }
				if (_guiContent_Icon_ModTF_Rot == null) { _guiContent_Icon_ModTF_Rot = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Rotate)); }
				if (_guiContent_Icon_ModTF_Scale == null) { _guiContent_Icon_ModTF_Scale = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Transform_Scale)); }

				int iconSize = 30;
				int propertyWidth = width - (iconSize + 12);

				//Position
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

				//EditorGUILayout.LabelField(new GUIContent(img_Pos), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
				EditorGUILayout.LabelField(_guiContent_Icon_ModTF_Pos.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyWidth), apGUILOFactory.I.Height(iconSize));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Position), apGUILOFactory.I.Width(propertyWidth));//"Position"
																													 //nextPos = EditorGUILayout.Vector2Field("", nextPos, GUILayout.Width(propertyWidth));
				Vector2 rootPos = apEditorUtil.DelayedVector2Field(rootMeshGroupTransform._matrix._pos, propertyWidth);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				//Rotation
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

				//EditorGUILayout.LabelField(new GUIContent(img_Rot), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
				EditorGUILayout.LabelField(_guiContent_Icon_ModTF_Rot.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyWidth), apGUILOFactory.I.Height(iconSize));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Rotation), apGUILOFactory.I.Width(propertyWidth));//"Rotation"

				float rootAngle = EditorGUILayout.DelayedFloatField(rootMeshGroupTransform._matrix._angleDeg, apGUILOFactory.I.Width(propertyWidth));
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				//Scaling
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(iconSize));
				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(iconSize));

				//EditorGUILayout.LabelField(new GUIContent(img_Scale), GUILayout.Width(iconSize), GUILayout.Height(iconSize));
				EditorGUILayout.LabelField(_guiContent_Icon_ModTF_Scale.Content, apGUILOFactory.I.Width(iconSize), apGUILOFactory.I.Height(iconSize));

				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(propertyWidth), apGUILOFactory.I.Height(iconSize));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Scaling), apGUILOFactory.I.Width(propertyWidth));//"Scaling"

				//nextScale = EditorGUILayout.Vector2Field("", nextScale, GUILayout.Width(propertyWidth));
				Vector2 rootScale = apEditorUtil.DelayedVector2Field(rootMeshGroupTransform._matrix._scale, propertyWidth);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.EndVertical();

				//테스트용
				//rootMeshGroupTransform._isVisible_Default = EditorGUILayout.Toggle("Is Visible", rootMeshGroupTransform._isVisible_Default, GUILayout.Width(width));
				//EditorGUILayout.ColorField("Color2x", rootMeshGroupTransform._meshColor2X_Default);


				if (rootPos != rootMeshGroupTransform._matrix._pos
					|| rootAngle != rootMeshGroupTransform._matrix._angleDeg
					|| rootScale != rootMeshGroupTransform._matrix._scale)
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, MeshGroup, MeshGroup, false, true);

					rootMeshGroupTransform._matrix.SetTRS(rootPos.x, rootPos.y, rootAngle, rootScale.x, rootScale.y);
					MeshGroup.RefreshForce();
					apEditorUtil.ReleaseGUIFocus();
				}
			}
		}

		private void OnMaterialSetOfMeshTFSelected(bool isSuccess, object loadKey, apMaterialSet resultMaterialSet, bool isNoneSelected, object savedObject)
		{
			if (!isSuccess || loadKey != _loadKey_SelectMaterialSetOfMeshTransform || resultMaterialSet == null || savedObject != SubMeshInGroup)
			{
				_loadKey_SelectMaterialSetOfMeshTransform = null;
				return;
			}
			_loadKey_SelectMaterialSetOfMeshTransform = null;

			apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, MeshGroup, MeshGroup, false, true);
			SubMeshInGroup._linkedMaterialSet = resultMaterialSet;
			SubMeshInGroup._materialSetID = resultMaterialSet._uniqueID;

			Editor.SetRepaint();
			apEditorUtil.ReleaseGUIFocus();
		}




		private void OnSelectOtherMeshTransformsForCopyingSettings(bool isSuccess,
																	object loadKey,
																	apTransform_Mesh srcMeshTransform,
																	List<apTransform_Mesh> selectedObjects,
																	List<apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES> copiedProperties)
		{
			//다른 메시에 속성을 복사하는 다이얼로그의 이벤트
			if (!isSuccess || _loadKey_SelectOtherMeshTransformForCopyingSettings != loadKey || selectedObjects == null || srcMeshTransform == null || MeshGroup == null)
			{
				_loadKey_SelectOtherMeshTransformForCopyingSettings = null;
				return;
			}


			_loadKey_SelectOtherMeshTransformForCopyingSettings = null;


			if (SubMeshInGroup == null || srcMeshTransform == null || SubMeshInGroup != srcMeshTransform)
			{
				//요청한 MeshTransform이 현재 MeshTransform과 다르다.
				return;
			}

			if (selectedObjects.Contains(SubMeshInGroup))
			{
				selectedObjects.Remove(SubMeshInGroup);
			}

			if (selectedObjects.Count == 0 || copiedProperties.Count == 0)
			{
				//복사할 대상이 없다.
				return;
			}



			//속성들을 복사하자.
			apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_DefaultSettingChanged, Editor, MeshGroup, SubMeshInGroup, false, true);

			apTransform_Mesh meshTF = null;
			for (int iMesh = 0; iMesh < selectedObjects.Count; iMesh++)
			{
				meshTF = selectedObjects[iMesh];

				//속성들을 하나씩 복사한다.
				for (int iProp = 0; iProp < copiedProperties.Count; iProp++)
				{
					switch (copiedProperties[iProp])
					{
						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.DefaultColor:
							//_meshColor2X_Default
							meshTF._meshColor2X_Default = srcMeshTransform._meshColor2X_Default;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.ShaderType:
							//_shaderType
							meshTF._shaderType = srcMeshTransform._shaderType;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.CustomShader:
							//_isCustomShader, _customShader
							meshTF._isCustomShader = srcMeshTransform._isCustomShader;
							meshTF._customShader = srcMeshTransform._customShader;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.RenderTextureSize:
							//_renderTexSize
							meshTF._renderTexSize = srcMeshTransform._renderTexSize;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.TwoSides:
							//_isAlways2Side
							meshTF._isAlways2Side = srcMeshTransform._isAlways2Side;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.ShadowSettings:
							//_isUsePortraitShadowOption, _shadowCastingMode, _receiveShadow
							meshTF._isUsePortraitShadowOption = srcMeshTransform._isUsePortraitShadowOption;
							meshTF._shadowCastingMode = srcMeshTransform._shadowCastingMode;
							meshTF._receiveShadow = srcMeshTransform._receiveShadow;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.MaterialSet:
							//_materialSetID
							meshTF._isUseDefaultMaterialSet = srcMeshTransform._isUseDefaultMaterialSet;
							meshTF._materialSetID = srcMeshTransform._materialSetID;
							meshTF._linkedMaterialSet = srcMeshTransform._linkedMaterialSet;
							break;

						case apDialog_SelectMeshTransformsToCopy.COPIED_PROPERTIES.MaterialProperties:
							//_customMaterialProperties
							{
								if (meshTF._customMaterialProperties == null)
								{
									meshTF._customMaterialProperties = new List<apTransform_Mesh.CustomMaterialProperty>();
								}
								meshTF._customMaterialProperties.Clear();

								if (srcMeshTransform._customMaterialProperties != null)
								{
									for (int iCustomProp = 0; iCustomProp < srcMeshTransform._customMaterialProperties.Count; iCustomProp++)
									{
										apTransform_Mesh.CustomMaterialProperty newCustomProp = new apTransform_Mesh.CustomMaterialProperty();
										newCustomProp.CopyFromSrc(srcMeshTransform._customMaterialProperties[iCustomProp]);

										meshTF._customMaterialProperties.Add(newCustomProp);
									}
								}
							}
							break;
					}
				}
			}

			MeshGroup.RefreshForce();

			//_loadKey_SelectOtherMeshTransformForCopyingSettings = apDialog_SelectMultipleObjects.ShowDialog(
			//														Editor, 
			//														MeshGroup, 
			//														apDialog_SelectMultipleObjects.REQUEST_TARGET.MeshAndMeshGroups, 
			//														OnSelectOtherMeshTransformsForCopyingSettings, 
			//														_editor.GetText(TEXT.DLG_Apply),
			//														SubMeshInGroup, SubMeshInGroup);
		}


		private void OnSelectMeshGroupToMigrate(bool isSuccess, object loadKey, apMeshGroup dstMeshGroup, apTransform_Mesh targetMeshTransform, apMeshGroup srcMeshGroup, bool isSelectParent)
		{
			if (!isSuccess
				|| dstMeshGroup == null
				|| loadKey == null
				|| _loadKey_MigrateMeshTransform == null
				|| loadKey != _loadKey_MigrateMeshTransform
				|| targetMeshTransform == null
				|| srcMeshGroup == null)
			{
				//실패
				_loadKey_MigrateMeshTransform = null;

				Debug.LogError("AnyPortrait : Migrating is failed. > Dialog Canceled.");
				return;
			}
			_loadKey_MigrateMeshTransform = null;

			//Debug.Log("AnyPortrait : Migrating Start! [" + srcMeshGroup._name + "] > [" + dstMeshGroup._name + "] (" + targetMeshTransform._nickName + ")");
			//Transform을 복제하자
			bool result = Editor.Controller.MigrateMeshTransformToOtherMeshGroup(targetMeshTransform, srcMeshGroup, dstMeshGroup);

			if (!result)
			{
				Debug.LogError("AnyPortrait : Migrating is failed or canceled.");
			}

		}


		private void DrawEditor_Right2_MeshGroup_Bone(int width, int height)
		{
			//int subWidth = 250;
			apBone curBone = Bone;

			bool isRefresh = false;
			bool isAnyGUIAction = false;

			//bool isChildBoneChanged = false;

			bool isBoneChanged = (_prevBone_BoneProperty != curBone);
			//if (curBone != null)
			//{
			//	isChildBoneChanged = (_prevChildBoneCount != curBone._childBones.Count);
			//}


			if (_prevBone_BoneProperty != curBone)
			{
				_prevBone_BoneProperty = curBone;
				if (curBone != null)
				{
					//_prevName_BoneProperty = curBone._name;
					_prevChildBoneCount = curBone._childBones.Count;
				}
				else
				{
					//_prevName_BoneProperty = "";
					_prevChildBoneCount = 0;
				}

				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Update_Child_Bones, false);//"Update Child Bones"
			}

			if (curBone != null)
			{
				if (_prevChildBoneCount != curBone._childBones.Count)
				{
					Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Update_Child_Bones, true);//"Update Child Bones"
					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Update_Child_Bones))//"Update Child Bones"
					{
						//Debug.Log("Child Bone Count Changed : " + _prevChildBoneCount + " -> " + curBone._childBones.Count);
						_prevChildBoneCount = curBone._childBones.Count;
					}
				}
			}

			//"MeshGroupRight2 Bone"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupRight2_Bone,
				curBone != null && !isBoneChanged
				//&& !isChildBoneChanged
				);

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Bone__Child_Bone_Drawable, true);//"MeshGroup Bone - Child Bone Drawable"

			if (!Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupRight2_Bone)//"MeshGroupRight2 Bone"
																						  //|| !Editor.IsDelayedGUIVisible("MeshGroup Bone - Child Bone Drawable")
				)
			{
				return;
			}



			//1. 아이콘 / 타입
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(50));
			GUILayout.Space(10);

			//모디파이어 아이콘
			//이전
			//EditorGUILayout.LabelField(
			//	new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)),
			//	GUILayout.Width(50), GUILayout.Height(50));

			//변경
			if (_guiContent_Right_MeshGroup_ModIcon == null)
			{
				_guiContent_Right_MeshGroup_ModIcon = new apGUIContentWrapper();
			}
			_guiContent_Right_MeshGroup_ModIcon.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging));

			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_ModIcon.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(50));

			int nameWidth = width - (50 + 10);
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(nameWidth));
			GUILayout.Space(5);

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Bone), apGUILOFactory.I.Width(nameWidth));//"Bone"

			string nextBoneName = EditorGUILayout.DelayedTextField(curBone._name, apGUILOFactory.I.Width(nameWidth));
			if (!string.Equals(nextBoneName, curBone._name))
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);

				curBone._name = nextBoneName;
				isRefresh = true;
				isAnyGUIAction = true;
				apEditorUtil.ReleaseGUIFocus();
			}


			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(20);

			//Default Matrix 설정
			Vector2 defPos = curBone._defaultMatrix._pos;
			float defAngle = curBone._defaultMatrix._angleDeg;
			Vector2 defScale = curBone._defaultMatrix._scale;

			//"Base Pose Transformation"
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.BasePoseTransformation), apGUILOFactory.I.Width(width));
			int widthValue = width - 80;

			if (_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}

			if (!IsBoneDefaultEditing)
			{
				//여기서는 보여주기만

				_strWrapper_64.Clear();
				_strWrapper_64.Append(defPos.x, false);
				_strWrapper_64.Append(apStringFactory.I.Comma_Space, false);
				_strWrapper_64.Append(defPos.y, true);

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Position), apGUILOFactory.I.Width(70));//"Position"
																										  //EditorGUILayout.LabelField(defPos.x + ", " + defPos.y, apGUILOFactory.I.Width(widthValue));
				EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(widthValue));
				EditorGUILayout.EndHorizontal();


				_strWrapper_64.Clear();
				_strWrapper_64.Append(defAngle, true);

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Rotation), apGUILOFactory.I.Width(70));//"Rotation"
				EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(widthValue));
				EditorGUILayout.EndHorizontal();

				_strWrapper_64.Clear();
				_strWrapper_64.Append(defScale.x, false);
				_strWrapper_64.Append(apStringFactory.I.Comma_Space, false);
				_strWrapper_64.Append(defScale.y, true);

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Scaling), apGUILOFactory.I.Width(70));//"Scaling"
				EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(widthValue));
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				//여기서는 설정이 가능하다

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Position), apGUILOFactory.I.Width(70));//"Position"
				defPos = apEditorUtil.DelayedVector2Field(defPos, widthValue);

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Rotation), apGUILOFactory.I.Width(70));//"Rotation"
				defAngle = EditorGUILayout.DelayedFloatField(defAngle, apGUILOFactory.I.Width(widthValue + 4));
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Scaling), apGUILOFactory.I.Width(70));//"Scaling"
				defScale = apEditorUtil.DelayedVector2Field(defScale, widthValue);

				EditorGUILayout.EndHorizontal();

				if (defPos != curBone._defaultMatrix._pos ||
					defAngle != curBone._defaultMatrix._angleDeg ||
					defScale != curBone._defaultMatrix._scale)
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);

					defAngle = apUtil.AngleTo180(defAngle);

					curBone._defaultMatrix.SetPos(defPos);
					curBone._defaultMatrix.SetRotate(defAngle);
					curBone._defaultMatrix.SetScale(defScale);

					curBone.MakeWorldMatrix(true);//<<이때는 IK가 꺼져서 이것만 수정해도 된다.

					//isRefresh = true;
					isAnyGUIAction = true;
					apEditorUtil.ReleaseGUIFocus();
				}
			}
			GUILayout.Space(10);
			//"Socket Enabled", "Socket Disabled"
			if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.SocketEnabled), Editor.GetUIWord(UIWORD.SocketDisabled), curBone._isSocketEnabled, true, width, 25))
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
				curBone._isSocketEnabled = !curBone._isSocketEnabled;
			}

			//추가 8.13 : 복제 기능 (오프셋을 입력해야한다.)
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.Duplicate), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20)))
			{
				//본 복사하는 다이얼로그를 열자
				_loadKey_DuplicateBone = apDialog_DuplicateBone.ShowDialog(Editor, Bone, OnDuplicateBoneResult);
			}


			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			//IK 설정
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKSetting), apGUILOFactory.I.Width(width));//"IK Setting"

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40));
			int IKModeBtnSize = (width / 4) - 4;
			//EditorGUILayout.LabelField("IK Option", GUILayout.Width(70));
			GUILayout.Space(5);
			apBone.OPTION_IK nextOptionIK = curBone._optionIK;

			//apBone.OPTION_IK nextOptionIK = (apBone.OPTION_IK)EditorGUILayout.EnumPopup(curBone._optionIK, GUILayout.Width(widthValue));

			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_IKSingle), curBone._optionIK == apBone.OPTION_IK.IKSingle, true, IKModeBtnSize, 40, apStringFactory.I.IKSingle))//"IK Single"
			{
				nextOptionIK = apBone.OPTION_IK.IKSingle;
				isAnyGUIAction = true;
			}
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_IKHead), curBone._optionIK == apBone.OPTION_IK.IKHead, true, IKModeBtnSize, 40, apStringFactory.I.IKHead))//"IK Head"
			{
				nextOptionIK = apBone.OPTION_IK.IKHead;
				isAnyGUIAction = true;
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_IKChained), curBone._optionIK == apBone.OPTION_IK.IKChained, curBone._optionIK == apBone.OPTION_IK.IKChained, IKModeBtnSize, 40, apStringFactory.I.IKChain))//"IK Chain"
			{
				//nextOptionIK = apBone.OPTION_IK.IKSingle;//Chained는 직접 설정할 수 있는게 아니다.
				isAnyGUIAction = true;
			}
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_IKDisabled), curBone._optionIK == apBone.OPTION_IK.Disabled, true, IKModeBtnSize, 40, apStringFactory.I.Disabled))//"Disabled"
			{
				nextOptionIK = apBone.OPTION_IK.Disabled;
				isAnyGUIAction = true;
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			Color prevColor = GUI.backgroundColor;

			Color boxColor = Color.black;
			switch (curBone._optionIK)
			{
				case apBone.OPTION_IK.IKSingle: boxColor = new Color(1.0f, 0.6f, 0.5f, 1.0f); break;
				case apBone.OPTION_IK.IKHead: boxColor = new Color(1.0f, 0.5f, 0.6f, 1.0f); break;
				case apBone.OPTION_IK.IKChained: boxColor = new Color(0.7f, 0.5f, 1.0f, 1.0f); break;
				case apBone.OPTION_IK.Disabled: boxColor = new Color(0.6f, 0.8f, 1.0f, 1.0f); break;
			}
			GUI.backgroundColor = boxColor;

			switch (curBone._optionIK)
			{
				case apBone.OPTION_IK.IKSingle: GUILayout.Box(Editor.GetUIWord(UIWORD.IKInfo_Single), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40)); break;
				case apBone.OPTION_IK.IKHead: GUILayout.Box(Editor.GetUIWord(UIWORD.IKInfo_Head), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40)); break;
				case apBone.OPTION_IK.IKChained: GUILayout.Box(Editor.GetUIWord(UIWORD.IKInfo_Chain), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40)); break;
				case apBone.OPTION_IK.Disabled: GUILayout.Box(Editor.GetUIWord(UIWORD.IKInfo_Disabled), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(40)); break;
			}


			GUI.backgroundColor = prevColor;

			GUILayout.Space(10);


			if (nextOptionIK != curBone._optionIK)
			{
				//Debug.Log("IK Change : " + curBone._optionIK + " > " + nextOptionIK);

				bool isIKOptionChangeValid = false;


				//이제 IK 옵션에 맞는지 체크해주자
				if (curBone._optionIK == apBone.OPTION_IK.IKChained)
				{
					//Chained 상태에서는 아예 바꿀 수 없다.
					//EditorUtility.DisplayDialog("IK Option Information",
					//	"<IK Chained> setting has been forced.\nTo Change, change the IK setting in the <IK Header>.",
					//	"Close");

					EditorUtility.DisplayDialog(Editor.GetText(TEXT.IKOption_Title),
													Editor.GetText(TEXT.IKOption_Body_Chained),
													Editor.GetText(TEXT.Close));
				}
				else
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);

					//그외에는 변경이 가능하다
					switch (nextOptionIK)
					{
						case apBone.OPTION_IK.Disabled:
							//끄는 건 쉽다.
							isIKOptionChangeValid = true;
							break;

						case apBone.OPTION_IK.IKChained:
							//IK Chained는 직접 할 수 있는게 아니다.
							//EditorUtility.DisplayDialog("IK Option Information",
							//"<IK Chained> setting is set automatically.\nTo change, change the setting in the <IK Header>.",
							//"Close");

							EditorUtility.DisplayDialog(Editor.GetText(TEXT.IKOption_Title),
												Editor.GetText(TEXT.IKOption_Body_Chained),
												Editor.GetText(TEXT.Close));
							break;

						case apBone.OPTION_IK.IKHead:
							{
								//자식으로 연결된게 없으면 일단 바로 아래 자식을 연결하자.
								//자식이 없으면 실패

								apBone nextChainedBone = curBone._IKNextChainedBone;
								apBone targetBone = curBone._IKTargetBone;

								bool isRefreshNeed = true;
								if (nextChainedBone != null && targetBone != null)
								{
									//이전에 연결된 값이 존재하고, 재귀적인 연결도 유효한 경우는 패스
									if (curBone.GetChildBone(nextChainedBone._uniqueID) != null
										&& curBone.GetChildBoneRecursive(targetBone._uniqueID) != null)
									{
										//유효한 설정이다.
										isRefreshNeed = false;
									}
								}

								if (isRefreshNeed)
								{
									//자식 Bone의 하나를 연결하자
									if (curBone._childBones.Count > 0)
									{
										curBone._IKNextChainedBone = curBone._childBones[0];
										curBone._IKTargetBone = curBone._childBones[0];

										curBone._IKNextChainedBoneID = curBone._IKNextChainedBone._uniqueID;
										curBone._IKTargetBoneID = curBone._IKTargetBone._uniqueID;

										isIKOptionChangeValid = true;//기본값을 넣어서 변경 가능
									}
									else
									{
										//EditorUtility.DisplayDialog("IK Option Information",
										//"<IK Head> setting requires one or more child Bones.",
										//"Close");

										EditorUtility.DisplayDialog(Editor.GetText(TEXT.IKOption_Title),
													Editor.GetText(TEXT.IKOption_Body_Head),
													Editor.GetText(TEXT.Close));
									}
								}
								else
								{
									isIKOptionChangeValid = true;
								}
							}
							break;

						case apBone.OPTION_IK.IKSingle:
							{
								//IK Target과 NextChained가 다르면 일단 그것부터 같게 하자.
								//나머지는 Head와 동일
								curBone._IKTargetBone = curBone._IKNextChainedBone;
								curBone._IKTargetBoneID = curBone._IKNextChainedBoneID;

								apBone nextChainedBone = curBone._IKNextChainedBone;

								bool isRefreshNeed = true;
								if (nextChainedBone != null)
								{
									//이전에 연결된 값이 존재하고, 재귀적인 연결도 유효한 경우는 패스
									if (curBone.GetChildBone(nextChainedBone._uniqueID) != null)
									{
										//유효한 설정이다.
										isRefreshNeed = false;
									}
								}

								if (isRefreshNeed)
								{
									//자식 Bone의 하나를 연결하자
									if (curBone._childBones.Count > 0)
									{
										curBone._IKNextChainedBone = curBone._childBones[0];
										curBone._IKTargetBone = curBone._childBones[0];

										curBone._IKNextChainedBoneID = curBone._IKNextChainedBone._uniqueID;
										curBone._IKTargetBoneID = curBone._IKTargetBone._uniqueID;

										isIKOptionChangeValid = true;//기본값을 넣어서 변경 가능
									}
									else
									{
										//EditorUtility.DisplayDialog("IK Option Information",
										//"<IK Single> setting requires a child Bone.",
										//"Close");

										EditorUtility.DisplayDialog(Editor.GetText(TEXT.IKOption_Title),
													Editor.GetText(TEXT.IKOption_Body_Single),
													Editor.GetText(TEXT.Close));
									}
								}
								else
								{
									isIKOptionChangeValid = true;
								}
							}
							break;
					}
				}



				if (isIKOptionChangeValid)
				{
					curBone._optionIK = nextOptionIK;

					isRefresh = true;
				}
				//TODO : 너무 자동으로 Bone Chain을 하는것 같다;
				//옵션이 적용이 안된다;
			}

			//추가
			if (_guiContent_Right_MeshGroup_RiggingIconAndText == null)
			{
				_guiContent_Right_MeshGroup_RiggingIconAndText = new apGUIContentWrapper();
				_guiContent_Right_MeshGroup_RiggingIconAndText.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging));
			}



			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKHeader), apGUILOFactory.I.Width(width));//"IK Header"

			//string headerBoneName = "<None>";//이전
			string headerBoneName = apEditorUtil.Text_NoneName;//변경

			if (curBone._IKHeaderBone != null)
			{
				headerBoneName = curBone._IKHeaderBone._name;
			}

			//이전
			//EditorGUILayout.LabelField(new GUIContent(" " + headerBoneName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width));

			//변경
			_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, headerBoneName);
			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width));

			GUILayout.Space(5);
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKNextChainToTarget), apGUILOFactory.I.Width(width));//"IK Next Chain To Target"

			//string nextChainedBoneName = "<None>";//이전
			string nextChainedBoneName = apEditorUtil.Text_NoneName;//변경

			if (curBone._IKNextChainedBone != null)
			{
				nextChainedBoneName = curBone._IKNextChainedBone._name;
			}

			//이전
			//EditorGUILayout.LabelField(new GUIContent(" " + nextChainedBoneName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width));

			//변경
			_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, nextChainedBoneName);
			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width));
			GUILayout.Space(5);


			if (curBone._optionIK != apBone.OPTION_IK.Disabled)
			{
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKTarget), apGUILOFactory.I.Width(width));//"IK Target"

				apBone targetBone = curBone._IKTargetBone;

				//string targetBoneName = "<None>";//이전
				string targetBoneName = apEditorUtil.Text_NoneName;//변경

				if (targetBone != null)
				{
					targetBoneName = targetBone._name;
				}

				//이전
				//EditorGUILayout.LabelField(new GUIContent(" " + targetBoneName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width));

				//변경
				_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, targetBoneName);
				EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width));

				//Target을 설정하자.
				if (curBone._optionIK == apBone.OPTION_IK.IKHead)
				{
					//"Change IK Target"
					if (GUILayout.Button(Editor.GetUIWord(UIWORD.ChangeIKTarget), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20)))
					{
						//Debug.LogError("TODO : IK Target을 Dialog를 열어서 설정하자.");
						_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(Editor, curBone, curBone._meshGroup, apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKTarget, OnDialogSelectBone);
						isAnyGUIAction = true;
					}
				}



				GUILayout.Space(15);
				//"IK Angle Constraint"
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKAngleConstraint), apGUILOFactory.I.Width(width));

				//"Constraint On", "Constraint Off"
				if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.ConstraintOn), Editor.GetUIWord(UIWORD.ConstraintOff), curBone._isIKAngleRange, true, width, 25))
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);

					curBone._isIKAngleRange = !curBone._isIKAngleRange;
					isAnyGUIAction = true;
				}

				if (curBone._isIKAngleRange)
				{
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Range), apGUILOFactory.I.Width(70));//"Range"

					//변경전 Lower : -180 ~ 0, Uppder : 0 ~ 180
					//변경후 Lower : -360 ~ 360, Upper : -360 ~ 360 (크기만 맞춘다.)
					float nextLowerAngle = curBone._IKAngleRange_Lower;
					float nextUpperAngle = curBone._IKAngleRange_Upper;
					//EditorGUILayout.MinMaxSlider(ref nextLowerAngle, ref nextUpperAngle, -360, 360, GUILayout.Width(widthValue));
					EditorGUILayout.MinMaxSlider(ref nextLowerAngle, ref nextUpperAngle, -360, 360, apGUILOFactory.I.Width(width));

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Min), apGUILOFactory.I.Width(70));//"Min"
					nextLowerAngle = EditorGUILayout.DelayedFloatField(nextLowerAngle, apGUILOFactory.I.Width(widthValue));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Max), apGUILOFactory.I.Width(70));//"Max"
					nextUpperAngle = EditorGUILayout.DelayedFloatField(nextUpperAngle, apGUILOFactory.I.Width(widthValue));
					EditorGUILayout.EndHorizontal();

					//EditorGUILayout.EndHorizontal();


					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Preferred), apGUILOFactory.I.Width(70));//"Preferred"
					float nextPreferredAngle = EditorGUILayout.DelayedFloatField(curBone._IKAnglePreferred, apGUILOFactory.I.Width(widthValue));//<<정밀한 작업을 위해서 변경
					EditorGUILayout.EndHorizontal();

					if (nextLowerAngle != curBone._IKAngleRange_Lower ||
						nextUpperAngle != curBone._IKAngleRange_Upper ||
						nextPreferredAngle != curBone._IKAnglePreferred)
					{

						//apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
						apEditorUtil.SetEditorDirty();

						nextLowerAngle = Mathf.Clamp(nextLowerAngle, -360, 360);
						nextUpperAngle = Mathf.Clamp(nextUpperAngle, -360, 360);
						nextPreferredAngle = Mathf.Clamp(nextPreferredAngle, -360, 360);

						if (nextLowerAngle > nextUpperAngle)
						{
							float tmp = nextLowerAngle;
							nextLowerAngle = nextUpperAngle;
							nextUpperAngle = tmp;
						}

						curBone._IKAngleRange_Lower = nextLowerAngle;
						curBone._IKAngleRange_Upper = nextUpperAngle;
						curBone._IKAnglePreferred = nextPreferredAngle;
						//isRefresh = true;
						isAnyGUIAction = true;

						apEditorUtil.ReleaseGUIFocus();
					}
				}
			}


			//추가 5.8 IK Controller (Position / LookAt)
			GUILayout.Space(20);

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKConSettings), apGUILOFactory.I.Width(width));//"IK Controller Settings"
																											  //GUILayout.Space(10);
			apBoneIKController.CONTROLLER_TYPE nextIKControllerType = (apBoneIKController.CONTROLLER_TYPE)EditorGUILayout.EnumPopup(curBone._IKController._controllerType, apGUILOFactory.I.Width(width));
			if (nextIKControllerType != curBone._IKController._controllerType)
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneIKControllerChanged, Editor, curBone._meshGroup, curBone, false, false);
				curBone._IKController._controllerType = nextIKControllerType;
				apEditorUtil.ReleaseGUIFocus();
			}

			#region [미사용 코드]


			//if (curBone._positionController._isEnabled)
			//{
			//	//Position Controller가 켜져 있다면
			//	//- Effector
			//	//- Default Mix Weight
			//	GUILayout.Space(5);
			//	EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			//	EditorGUILayout.LabelField("Default FK/IK Weight", GUILayout.Width(width - 60));//"Default FK/IK Weight"
			//	float nextPosMixWeight = EditorGUILayout.DelayedFloatField(curBone._positionController._defaultMixWeight, GUILayout.Width(58));
			//	if(nextPosMixWeight != curBone._positionController._defaultMixWeight)
			//	{
			//		apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneIKControllerChanged, Editor, curBone._meshGroup, curBone, false, false);
			//		curBone._positionController._defaultMixWeight = Mathf.Clamp01(nextPosMixWeight);
			//		apEditorUtil.ReleaseGUIFocus();
			//	}

			//	EditorGUILayout.EndHorizontal();


			//	GUILayout.Space(5);

			//	EditorGUILayout.LabelField("Effector Bone", GUILayout.Width(width));//"Effector Bone"
			//	string posEffectorBoneName = "<None>";
			//	if (curBone._positionController._effectorBone != null)
			//	{
			//		posEffectorBoneName = curBone._positionController._effectorBone._name;
			//	}
			//	EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			//	EditorGUILayout.LabelField(new GUIContent(" " + posEffectorBoneName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width - 60));
			//	if (GUILayout.Button(Editor.GetUIWord(UIWORD.Change), GUILayout.Width(58)))//"Change"
			//	{
			//		isAnyGUIAction = true;
			//		_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(	Editor, curBone, curBone._meshGroup, 
			//																	apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKPositionControllerEffector, 
			//																	OnDialogSelectBone);
			//	}
			//	EditorGUILayout.EndHorizontal();

			//	GUILayout.Space(5);


			//	//TODO : Undo
			//} 
			#endregion

			if (curBone._IKController._controllerType != apBoneIKController.CONTROLLER_TYPE.None)
			{
				//Position / LookAt 공통 설정
				//- Default Mix Weight
				//- Effector
				GUILayout.Space(5);

				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKConEffectorBone), apGUILOFactory.I.Width(width));//"Effector Bone"

				//string lookAtEffectorBoneName = "<None>";//이전
				string lookAtEffectorBoneName = apEditorUtil.Text_NoneName;//변경

				if (curBone._IKController._effectorBone != null)
				{
					lookAtEffectorBoneName = curBone._IKController._effectorBone._name;
				}
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

				//이전
				//EditorGUILayout.LabelField(new GUIContent(" " + lookAtEffectorBoneName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width - 60));

				//변경
				_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, lookAtEffectorBoneName);
				EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width - 60));

				if (GUILayout.Button(Editor.GetUIWord(UIWORD.Change), apGUILOFactory.I.Width(58)))//"Change"
				{
					isAnyGUIAction = true;

					_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(Editor, curBone, curBone._meshGroup,
																				(curBone._IKController._controllerType == apBoneIKController.CONTROLLER_TYPE.Position ?
																					apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKPositionControllerEffector :
																					apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKLookAtControllerEffector),
																				OnDialogSelectBone);
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(5);

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.IKConDefaultWeight), apGUILOFactory.I.Width(width - 60));//"Default FK/IK Weight"
				float nextLookAtMixWeight = EditorGUILayout.DelayedFloatField(curBone._IKController._defaultMixWeight, apGUILOFactory.I.Width(58));
				if (nextLookAtMixWeight != curBone._IKController._defaultMixWeight)
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneIKControllerChanged, Editor, curBone._meshGroup, curBone, false, false);
					curBone._IKController._defaultMixWeight = Mathf.Clamp01(nextLookAtMixWeight);
					apEditorUtil.ReleaseGUIFocus();
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(5);

				//Control Parameter에 의해서 제어
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ControlParameter), apGUILOFactory.I.Width(width - 60));//"Default FK/IK Weight"
				bool nextUseControlParam = EditorGUILayout.Toggle(curBone._IKController._isWeightByControlParam, apGUILOFactory.I.Width(58));
				if (nextUseControlParam != curBone._IKController._isWeightByControlParam)
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneIKControllerChanged, Editor, curBone._meshGroup, curBone, false, false);
					curBone._IKController._isWeightByControlParam = nextUseControlParam;
					apEditorUtil.ReleaseGUIFocus();
				}
				EditorGUILayout.EndHorizontal();

				//추가
				if (_guiContent_Right_MeshGroup_ParamIconAndText == null)
				{
					_guiContent_Right_MeshGroup_ParamIconAndText = new apGUIContentWrapper();
					_guiContent_Right_MeshGroup_ParamIconAndText.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Param));
				}



				if (curBone._IKController._isWeightByControlParam)
				{
					//Control Param 선택하기
					//string controlParamName = "<None>";//이전
					string controlParamName = apEditorUtil.Text_NoneName;//변경

					if (curBone._IKController._weightControlParam != null)
					{
						controlParamName = curBone._IKController._weightControlParam._keyName;
					}
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

					//이전
					//EditorGUILayout.LabelField(new GUIContent(" " + controlParamName, Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Param)), GUILayout.Width(width - 60));

					//변경
					_guiContent_Right_MeshGroup_ParamIconAndText.SetText(1, controlParamName);
					EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_ParamIconAndText.Content, apGUILOFactory.I.Width(width - 60));

					if (GUILayout.Button(Editor.GetUIWord(UIWORD.Change), apGUILOFactory.I.Width(58)))//"Change"
					{
						isAnyGUIAction = true;
						//Control Param 선택 다이얼로그
						_loadKey_SelectControlParamForIKController = apDialog_SelectControlParam.ShowDialog(
																				Editor,
																				apDialog_SelectControlParam.PARAM_TYPE.Float,
																				OnSelectControlParamForIKController,
																				curBone);
					}
					EditorGUILayout.EndHorizontal();
				}


				GUILayout.Space(5);
			}

			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			//Hierarchy 설정
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Hierarchy), apGUILOFactory.I.Width(width));//"Hierarchy"
																										  //Parent와 Child List를 보여주자.
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ParentBone), apGUILOFactory.I.Width(width));//"Parent Bone"

			//string parentName = "<None>";//이전
			string parentName = apEditorUtil.Text_NoneName;//변경

			if (curBone._parentBone != null)
			{
				parentName = curBone._parentBone._name;
			}
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

			//이전
			//EditorGUILayout.LabelField(new GUIContent(" " + parentName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width - 60));

			//변경
			_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, parentName);
			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width - 60));

			if (GUILayout.Button(Editor.GetUIWord(UIWORD.Change), apGUILOFactory.I.Width(58)))//"Change"
			{
				//Debug.LogError("TODO : Change Parent Dialog 구현할 것");
				isAnyGUIAction = true;
				_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(Editor, curBone, curBone._meshGroup, apDialog_SelectLinkedBone.REQUEST_TYPE.ChangeParent, OnDialogSelectBone);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			int nChildList = curBone._childBones.Count;
			if (_prevChildBoneCount != nChildList)
			{
				Debug.Log("AnyPortrait : Count is not matched : " + _prevChildBoneCount + " > " + nChildList);
			}

			//"Children Bones"
			if (_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}
			_strWrapper_64.Clear();
			_strWrapper_64.Append(Editor.GetUIWord(UIWORD.ChildrenBones), false);
			_strWrapper_64.AppendSpace(1, false);
			_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
			_strWrapper_64.Append(nChildList, false);
			_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

			//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ChildrenBones) + " [" + nChildList + "]", apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(width));

			//Detach가 
			apBone detachedBone = null;

			for (int iChild = 0; iChild < _prevChildBoneCount; iChild++)
			{
				if (iChild >= nChildList)
				{
					//리스트를 벗어났다.
					//더미 Layout을 그리자
					//유니티 레이아웃 처리방식때문..
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
					EditorGUILayout.LabelField(apStringFactory.I.None, apGUILOFactory.I.Width(width - 60));
					if (GUILayout.Button(Editor.GetUIWord(UIWORD.Detach), apGUILOFactory.I.Width(58)))//"Detach"
					{

					}
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					apBone childBone = curBone._childBones[iChild];
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

					//이전
					//EditorGUILayout.LabelField(new GUIContent(" " + childBone._name, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width - 60));

					//변경
					_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, childBone._name);
					EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width - 60));

					if (GUILayout.Button(Editor.GetUIWord(UIWORD.Detach), apGUILOFactory.I.Width(58)))//"Detach"
					{
						//bool isResult = EditorUtility.DisplayDialog("Detach Child Bone", "Detach Bone? [" + childBone._name + "]", "Detach", "Cancel")
						bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.DetachChildBone_Title),
																		Editor.GetTextFormat(TEXT.DetachChildBone_Body, childBone._name),
																		Editor.GetText(TEXT.Detach_Ok),
																		Editor.GetText(TEXT.Cancel)
																		);

						if (isResult)
						{
							//Debug.LogError("TODO : Detach Child Bone 구현할 것");
							//Detach Child Bone 선택
							detachedBone = childBone;
							isAnyGUIAction = true;
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.AttachChildBone), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20)))//"Attach Child Bone"
			{
				isAnyGUIAction = true;
				_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(Editor, curBone, curBone._meshGroup, apDialog_SelectLinkedBone.REQUEST_TYPE.AttachChild, OnDialogSelectBone);
			}

			GUILayout.Space(2);

			//추가 8.13 : 자식 본을 향하도록 만들기
			bool isSnapAvailable = (Bone._childBones != null) && (Bone._childBones.Count > 0);

			//"Snap to Child Bone
			if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.SnapToChildBone), Editor.GetUIWord(UIWORD.SnapToChildBone), false, isSnapAvailable, width, 20))
			{
				if (Bone._childBones != null)
				{
					if (Bone._childBones.Count == 1)
					{
						//자식이 1개라면
						//바로 함수 호출
						Editor.Controller.SnapBoneEndToChildBone(Bone, Bone._childBones[0], MeshGroup);
					}
					else
					{
						//자식이 여러개라면 
						//선택 다이얼로그 호출
						_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(Editor, Bone, MeshGroup, apDialog_SelectLinkedBone.REQUEST_TYPE.Select1LevelChildToSnap, OnDialogSelectBone);
					}
				}

			}

			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);


			//Shape 설정
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Shape), apGUILOFactory.I.Width(width));//"Shape"

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Color), apGUILOFactory.I.Width(70));//"Color"
			try
			{
				Color nextColor = EditorGUILayout.ColorField(curBone._color, apGUILOFactory.I.Width(widthValue));
				if (nextColor != curBone._color)
				{
					apEditorUtil.SetEditorDirty();
					curBone._color = nextColor;
				}
			}
			catch (Exception) { }
			EditorGUILayout.EndHorizontal();

			//추가 20.3.24 : 색상 프리셋을 두어서 빠르게 색상을 지정할 수 있다.
			//6개의 색상 프리셋을 만들자.
			int nPreset = apEditorUtil.BoneColorPresetsCount;
			int width_ColorPresetBtn = ((width - 84) / nPreset) - 2;
			int height_ColorPresetBtn = 12;

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_ColorPresetBtn));
			GUILayout.Space(78);
			Color presetBoneColor = Color.black;
			for (int iPreset = 0; iPreset < nPreset; iPreset++)
			{
				presetBoneColor = apEditorUtil.GetBoneColorPreset(iPreset);
				GUI.backgroundColor = presetBoneColor;
				if (GUILayout.Button(apGUIContentWrapper.Empty.Content, apEditorUtil.WhiteGUIStyle, apGUILOFactory.I.Width(width_ColorPresetBtn), apGUILOFactory.I.Height(height_ColorPresetBtn)))
				{
					//색상을 프리셋에 맞게 바꾸자
					Editor.Controller.ChangeBoneColorWithPreset(MeshGroup, curBone, presetBoneColor);

				}
				GUI.backgroundColor = prevColor;
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			//Width
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Width), apGUILOFactory.I.Width(70));//"Width"
			int nextShapeWidth = EditorGUILayout.DelayedIntField(curBone._shapeWidth, apGUILOFactory.I.Width(widthValue));
			if (nextShapeWidth != curBone._shapeWidth)
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
				curBone._shapeWidth = nextShapeWidth;

				//추가 : 다음 본을 생성시에 지금 값을 이용하도록 값을 저장하자
				_lastBoneShapeWidth = nextShapeWidth;
				_isLastBoneShapeWidthChanged = true;

				apEditorUtil.ReleaseGUIFocus();
			}
			EditorGUILayout.EndHorizontal();



			//추가 3.31 : Length를 수정할 수 있다.
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Length), apGUILOFactory.I.Width(70));
			int nextShapeLength = EditorGUILayout.DelayedIntField(curBone._shapeLength, apGUILOFactory.I.Width(widthValue));
			if (nextShapeLength != curBone._shapeLength)
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
				curBone._shapeLength = nextShapeLength;
				apEditorUtil.ReleaseGUIFocus();
			}
			EditorGUILayout.EndHorizontal();

			//Taper
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Taper), apGUILOFactory.I.Width(70));//"Taper"
			int nextShapeTaper = EditorGUILayout.DelayedIntField(curBone._shapeTaper, apGUILOFactory.I.Width(widthValue));
			if (nextShapeTaper != curBone._shapeTaper)
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
				curBone._shapeTaper = nextShapeTaper;

				apEditorUtil.ReleaseGUIFocus();
			}
			EditorGUILayout.EndHorizontal();

			//Helper
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Helper), apGUILOFactory.I.Width(70));//"Helper"
			bool nextHelper = EditorGUILayout.Toggle(curBone._shapeHelper, apGUILOFactory.I.Width(widthValue));
			if (nextHelper != curBone._shapeHelper)
			{
				apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
				curBone._shapeHelper = nextHelper;
			}
			EditorGUILayout.EndHorizontal();




			//Detach 요청이 있으면 수행 후 Refresh를 하자
			if (detachedBone != null)
			{
				isAnyGUIAction = true;
				Editor.Controller.DetachBoneFromChild(curBone, detachedBone);
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroup_Bone__Child_Bone_Drawable, false);//"MeshGroup Bone - Child Bone Drawable"
				isRefresh = true;
			}


			//추가 : Mirror Bone
			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.MirrorBone), apGUILOFactory.I.Width(width));//"Mirror Bone"

			//string mirrorName = "<None>";//이전
			string mirrorName = apEditorUtil.Text_NoneName;//변경

			if (curBone._mirrorBone != null)
			{
				mirrorName = curBone._mirrorBone._name;
			}
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

			//이전
			//EditorGUILayout.LabelField(new GUIContent(" " + mirrorName, Editor.ImageSet.Get(apImageSet.PRESET.Modifier_Rigging)), GUILayout.Width(width - 60));

			//변경
			_guiContent_Right_MeshGroup_RiggingIconAndText.SetText(1, mirrorName);
			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_RiggingIconAndText.Content, apGUILOFactory.I.Width(width - 60));

			if (GUILayout.Button(Editor.GetUIWord(UIWORD.Change), apGUILOFactory.I.Width(58)))//"Change"
			{
				isAnyGUIAction = true;
				_loadKey_SelectBone = apDialog_SelectLinkedBone.ShowDialog(Editor, curBone, curBone._meshGroup, apDialog_SelectLinkedBone.REQUEST_TYPE.Mirror, OnDialogSelectBone);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			//Mirror Option 중 Offset과 Axis는 루트 본만 적용된다.
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Bone_Mirror_Axis_Option_Visible, curBone._parentBone == null);//"Bone Mirror Axis Option Visible"
			if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Bone_Mirror_Axis_Option_Visible))//"Bone Mirror Axis Option Visible"
			{
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Axis), apGUILOFactory.I.Width(70));//"Axis"
				apBone.MIRROR_OPTION nextMirrorOption = (apBone.MIRROR_OPTION)EditorGUILayout.EnumPopup(curBone._mirrorOption, apGUILOFactory.I.Width(widthValue));
				if (nextMirrorOption != curBone._mirrorOption)
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
					curBone._mirrorOption = nextMirrorOption;
				}
				EditorGUILayout.EndHorizontal();


				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Offset), apGUILOFactory.I.Width(70));//"Offset"
				float nextMirrorCenterOffset = EditorGUILayout.DelayedFloatField(curBone._mirrorCenterOffset, apGUILOFactory.I.Width(widthValue));
				if (nextMirrorCenterOffset != curBone._mirrorCenterOffset)
				{
					apEditorUtil.SetRecord_MeshGroup(apUndoGroupData.ACTION.MeshGroup_BoneSettingChanged, Editor, curBone._meshGroup, curBone, false, false);
					curBone._mirrorCenterOffset = nextMirrorCenterOffset;
					apEditorUtil.ReleaseGUIFocus();
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);
			}

			if (GUILayout.Button(Editor.GetUIWord(UIWORD.MakeNewMirrorBone), apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))//"Make a New Mirror Bone"
			{
				//-Mirror 본 생성
				//-이름에 " L " <-> " R " 전환
				//-팝업으로 Children 포함할지 물어보기
				Editor.Controller.MakeNewMirrorBone(MeshGroup, curBone);
			}


			GUILayout.Space(5);



			GUILayout.Space(20);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(20);

			//"Remove Bone"
			//이전
			//if (GUILayout.Button(	new GUIContent(	"  " + Editor.GetUIWord(UIWORD.RemoveBone),
			//										Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
			//										),
			//						GUILayout.Width(width), GUILayout.Height(24)))

			//변경
			if (_guiContent_Right_MeshGroup_RemoveBone == null)
			{
				_guiContent_Right_MeshGroup_RemoveBone = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveBone), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}


			//본 삭제 단축키 (Delete)
			Editor.AddHotKeyEvent(OnHotKeyEvent_RemoveBone, apHotKey.LabelText.RemoveBone, KeyCode.Delete, false, false, false, Bone);


			if (GUILayout.Button(_guiContent_Right_MeshGroup_RemoveBone.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(24)))
			{
				isAnyGUIAction = true;
				SetBoneEditMode(BONE_EDIT_MODE.SelectAndTRS, true);


				string strRemoveBoneText = Editor.Controller.GetRemoveItemMessage(
																	_portrait,
																	curBone,
																	5,
																	Editor.GetTextFormat(TEXT.RemoveBone_Body, curBone._name),
																	Editor.GetText(TEXT.DLG_RemoveItemChangedWarning)
																	);
				//int btnIndex = EditorUtility.DisplayDialogComplex("Remove Bone", "Remove Bone [" + curBone._name + "] ?", "Remove", "Remove All Child Bones", "Cancel");


				int btnIndex = EditorUtility.DisplayDialogComplex(
																	Editor.GetText(TEXT.RemoveBone_Title),
																	strRemoveBoneText,
																	Editor.GetText(TEXT.Remove),
																	Editor.GetText(TEXT.RemoveBone_RemoveAllChildren),
																	Editor.GetText(TEXT.Cancel));
				if (btnIndex == 0)
				{
					//Bone을 삭제한다.
					Editor.Controller.RemoveBone(curBone, false);
				}
				else if (btnIndex == 1)
				{
					//Bone과 자식을 모두 삭제한다.
					Editor.Controller.RemoveBone(curBone, true);
				}
			}

			if (isAnyGUIAction)
			{
				//여기서 뭔가 처리를 했으면 Select 모드로 강제된다.
				if (_boneEditMode != BONE_EDIT_MODE.SelectAndTRS)
				{
					SetBoneEditMode(BONE_EDIT_MODE.SelectAndTRS, true);
				}
			}

			if (isRefresh)
			{
				Editor.RefreshControllerAndHierarchy(false);
				Editor._portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_AllModifiers(MeshGroup));
			}
		}



		private void OnDialogSelectBone(bool isSuccess, object loadKey, bool isNullBone, apBone selectedBone, apBone targetBone, apDialog_SelectLinkedBone.REQUEST_TYPE requestType)
		{
			if (_loadKey_SelectBone != loadKey)
			{
				_loadKey_SelectBone = null;
				return;
			}
			if (!isSuccess)
			{
				_loadKey_SelectBone = null;
				return;
			}


			_loadKey_SelectBone = null;
			switch (requestType)
			{
				case apDialog_SelectLinkedBone.REQUEST_TYPE.AttachChild:
					{
						Editor.Controller.AttachBoneToChild(targetBone, selectedBone);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.ChangeParent:
					{
						Editor.Controller.SetBoneAsParent(targetBone, selectedBone);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKTarget:
					{
						Editor.Controller.SetBoneAsIKTarget(targetBone, selectedBone);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKPositionControllerEffector:
					{
						Editor.Controller.SetBoneAsIKPositionControllerEffector(targetBone, selectedBone);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKLookAtControllerEffector:
					{
						Editor.Controller.SetBoneAsIKLookAtControllerEffectorOrStartBone(targetBone, selectedBone, true);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.SelectIKLookAtControllerStartBone:
					{
						Editor.Controller.SetBoneAsIKLookAtControllerEffectorOrStartBone(targetBone, selectedBone, false);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.Mirror:
					{
						Editor.Controller.SetBoneAsMirror(targetBone, selectedBone);
					}
					break;

				case apDialog_SelectLinkedBone.REQUEST_TYPE.Select1LevelChildToSnap:
					{
						Editor.Controller.SnapBoneEndToChildBone(targetBone, selectedBone, MeshGroup);
					}
					break;
			}
		}



		private void OnSelectControlParamForIKController(bool isSuccess, object loadKey, apControlParam resultControlParam, object savedObject)
		{
			if (!isSuccess || savedObject == null)
			{
				_loadKey_SelectControlParamForIKController = null;
				return;
			}
			if (_loadKey_SelectControlParamForIKController != loadKey)
			{
				return;
			}
			if (savedObject is apBone)
			{
				apBone targetBone = savedObject as apBone;
				if (targetBone == null)
				{
					return;
				}
				if (resultControlParam != null)
				{
					targetBone._IKController._weightControlParam = resultControlParam;
					targetBone._IKController._weightControlParamID = resultControlParam._uniqueID;
				}
				else
				{
					targetBone._IKController._weightControlParam = null;
					targetBone._IKController._weightControlParamID = -1;
				}

			}
		}



		private void OnDuplicateBoneResult(bool isSuccess, apBone targetBone, object loadKey, float offsetX, float offsetY, bool isDuplicateChildren)
		{
			if (!isSuccess
				|| _loadKey_DuplicateBone != loadKey
				|| targetBone != Bone
				|| targetBone == null
				|| SelectionType != SELECTION_TYPE.MeshGroup
				|| Bone == null)
			{
				_loadKey_DuplicateBone = null;
				return;
			}
			_loadKey_DuplicateBone = null;

			//복제 함수를 호출하자.
			Editor.Controller.DuplicateBone(MeshGroup, targetBone, offsetX, offsetY, isDuplicateChildren);
		}


		//단축키를 이용하여 본을 삭제하자.
		private void OnHotKeyEvent_RemoveBone(object paramObject)
		{
			if (paramObject != Bone)
			{
				return;
			}

			if (SelectionType != SELECTION_TYPE.MeshGroup ||
				Editor._meshGroupEditMode != apEditor.MESHGROUP_EDIT_MODE.Bone ||
				Bone == null)
			{
				return;
			}

			apBone curBone = Bone;


			SetBoneEditMode(BONE_EDIT_MODE.SelectAndTRS, true);


			string strRemoveBoneText = Editor.Controller.GetRemoveItemMessage(
																_portrait,
																curBone,
																5,
																Editor.GetTextFormat(TEXT.RemoveBone_Body, curBone._name),
																Editor.GetText(TEXT.DLG_RemoveItemChangedWarning)
																);
			
			int btnIndex = EditorUtility.DisplayDialogComplex(
																Editor.GetText(TEXT.RemoveBone_Title),
																strRemoveBoneText,
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.RemoveBone_RemoveAllChildren),
																Editor.GetText(TEXT.Cancel));
			if (btnIndex == 0)
			{
				//Bone을 삭제한다.
				Editor.Controller.RemoveBone(curBone, false);
			}
			else if (btnIndex == 1)
			{
				//Bone과 자식을 모두 삭제한다.
				Editor.Controller.RemoveBone(curBone, true);
			}
			SetBone(null);
			Editor.RefreshControllerAndHierarchy(false);
			Editor._portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_AllModifiers(MeshGroup));
		}




		private void DrawEditor_Right2_MeshGroup_Modifier(int width, int height)
		{
			if (Modifier != null)
			{
				//1-1. 선택된 객체가 존재하여 [객체 정보]를 출력할 수 있다.
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupBottom_Modifier, true);//"MeshGroupBottom_Modifier"
			}
			else
			{
				//1-2. 선택된 객체가 없어서 하단 UI를 출력하지 않는다.
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupBottom_Modifier, false);//"MeshGroupBottom_Modifier"

				return; //바로 리턴
			}

			//2. 출력할 정보가 있다 하더라도
			//=> 바로 출력 가능한게 아니라 경우에 따라 Hide 상태를 조금 더 유지할 필요가 있다.
			if (!Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.MeshGroupBottom_Modifier))//"MeshGroupBottom_Modifier"
			{
				//아직 출력하면 안된다.
				return;
			}
			//1. 아이콘 / 타입
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(50));
			GUILayout.Space(10);

			//모디파이어 아이콘
			//이전
			//EditorGUILayout.LabelField(
			//	new GUIContent(Editor.ImageSet.Get(apEditorUtil.GetModifierIconType(Modifier.ModifierType))),
			//	GUILayout.Width(50), GUILayout.Height(50));

			//변경
			if(_guiContent_Right_MeshGroup_ModIcon == null)
			{
				_guiContent_Right_MeshGroup_ModIcon = new apGUIContentWrapper();
			}
			_guiContent_Right_MeshGroup_ModIcon.SetImage(Editor.ImageSet.Get(apEditorUtil.GetModifierIconType(Modifier.ModifierType)));
			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_ModIcon.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(50));


			//아이콘 옆의 모디파이어 이름과 레이어 (Label)
			//> 변경 20.3.29 : 원래 하단에 나왔어야 할 Blend 방식과 Weight를 "레이어(Label)" 항목 대신 넣는다.
			int headerRightWidth = width - (50 + 10);
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(headerRightWidth));
			GUILayout.Space(5);

			//모디파이어 이름
			EditorGUILayout.LabelField(Modifier.DisplayName, apGUILOFactory.I.Width(headerRightWidth));

			#region [미사용 코드]
			//"Layer" > 삭제
			//if(_strWrapper_64 == null)
			//{
			//	_strWrapper_64 = new apStringWrapper(64);
			//}
			//_strWrapper_64.Clear();
			//_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Layer), false);
			//_strWrapper_64.Append(apStringFactory.I.Colon_Space, false);
			//_strWrapper_64.Append(Modifier._layer, true);

			////EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Layer) + " : " + Modifier._layer, GUILayout.Width(width - (50 + 10)));
			//EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(width - (50 + 10)));


			//2. 기본 블렌딩 설정
			//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Blend), apGUILOFactory.I.Width(width));//"Blend"

			//GUILayout.Space(5);

			//EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

			//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Method), apGUILOFactory.I.Width(70));//"Method"
			//apModifierBase.BLEND_METHOD blendMethod = (apModifierBase.BLEND_METHOD)EditorGUILayout.EnumPopup(Modifier._blendMethod, apGUILOFactory.I.Width(width - (70 + 5)));
			//if (blendMethod != Modifier._blendMethod)
			//{
			//	apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, Modifier, true);
			//	Modifier._blendMethod = blendMethod;
			//}
			//EditorGUILayout.EndHorizontal();

			//GUILayout.Space(5);

			//EditorGUILayout.BeginHorizontal();
			//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Weight), apGUILOFactory.I.Width(70));//"Weight"
			//float layerWeight = EditorGUILayout.DelayedFloatField(Modifier._layerWeight, apGUILOFactory.I.Width(width - (70 + 5)));

			//layerWeight = Mathf.Clamp01(layerWeight);
			//if (layerWeight != Modifier._layerWeight)
			//{
			//	apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, Modifier, true);
			//	Modifier._layerWeight = layerWeight;
			//}
			//EditorGUILayout.EndHorizontal(); 
			#endregion

			//블렌드 방식과 Weight (별도의 설명 없이)
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(headerRightWidth));

			//블렌드 방식
			apModifierBase.BLEND_METHOD blendMethod = (apModifierBase.BLEND_METHOD)EditorGUILayout.EnumPopup(Modifier._blendMethod, apGUILOFactory.I.Width(headerRightWidth - (50)));
			if (blendMethod != Modifier._blendMethod)
			{
				apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, Modifier, true);
				Modifier._blendMethod = blendMethod;
			}

			//블렌드 가중치
			float layerWeight = EditorGUILayout.DelayedFloatField(Modifier._layerWeight, apGUILOFactory.I.Width(44));

			layerWeight = Mathf.Clamp01(layerWeight);
			if (layerWeight != Modifier._layerWeight)
			{
				apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, Modifier, true);
				Modifier._layerWeight = layerWeight;
			}
			EditorGUILayout.EndHorizontal();


			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			if(_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}

			//레이어 이동 (Up/Down)
			//GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.LayerUp), apGUILOFactory.I.Width(width / 2 - 5), apGUILOFactory.I.Height(16)))//"Layer Up"
			{
				Editor.Controller.LayerChange(Modifier, true);
			}
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.LayerDown), apGUILOFactory.I.Width(width / 2 - 5), apGUILOFactory.I.Height(16)))//"Layer Down"
			{
				Editor.Controller.LayerChange(Modifier, false);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);
			
			//추가
			//만약 색상 옵션이 있는 경우 설정을 하자
			if ((int)(Modifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.Color) != 0)
			{
				//" Color Option On", " Color Option Off"
				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ColorVisibleOption),
														1, Editor.GetUIWord(UIWORD.ColorOptionOn),
														Editor.GetUIWord(UIWORD.ColorOptionOff),
														Modifier._isColorPropertyEnabled, true,
														width, 24
													))
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, Modifier, true);

					Modifier._isColorPropertyEnabled = !Modifier._isColorPropertyEnabled;
					Editor.RefreshControllerAndHierarchy(false);
				}

				//추가 : Color Option이 있는 경우 Extra 설정도 가능하다.
				//Extra Option On / Off
				if(apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ExtraOption),
													1, Editor.GetUIWord(UIWORD.ExtraOptionON), 
													Editor.GetUIWord(UIWORD.ExtraOptionOFF), 
													Modifier._isExtraPropertyEnabled, true, width, 20))
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, Modifier, true);

					Modifier._isExtraPropertyEnabled = !Modifier._isExtraPropertyEnabled;

					_meshGroup.RefreshModifierLink(apUtil.LinkRefresh.Set_MeshGroup_Modifier(MeshGroup, Modifier));//<<이거 다시 연결해줘야 한다.

					Editor.RefreshControllerAndHierarchy(false);
				}

				//구분선
				GUILayout.Space(5);
				apEditorUtil.GUI_DelimeterBoxH(width);
				GUILayout.Space(5);
			}

			//3. 각 프로퍼티 렌더링
			// 수정
			//일괄적으로 호출하자
			DrawModifierPropertyGUI(width, height);
			
			GUILayout.Space(20);


			//4. Modifier 삭제
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(20);

			//"  Remove Modifier"

			//이전
			//if (GUILayout.Button(	new GUIContent(	"  " + Editor.GetUIWord(UIWORD.RemoveModifier),
			//										Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
			//										),
			//						GUILayout.Height(24)))

			
			//변경
			if (_guiContent_Right_MeshGroup_RemoveModifier == null)
			{
				_guiContent_Right_MeshGroup_RemoveModifier = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveModifier), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}

			if (GUILayout.Button(	_guiContent_Right_MeshGroup_RemoveModifier.Content, apGUILOFactory.I.Height(24)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove", "Remove Modifier [" + Modifier.DisplayName + "]?", "Remove", "Cancel");


				string strRemoveModifierText = Editor.Controller.GetRemoveItemMessage(
																_portrait,
																Modifier,
																5,
																Editor.GetTextFormat(TEXT.RemoveModifier_Body, Modifier.DisplayName),
																Editor.GetText(TEXT.DLG_RemoveItemChangedWarning)
																);

				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveModifier_Title),
																//Editor.GetTextFormat(TEXT.RemoveModifier_Body, Modifier.DisplayName),
																strRemoveModifierText,
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);

				if (isResult)
				{
					Editor.Controller.RemoveModifier(Modifier);
				}
			}


			//삭제 직후라면 출력 에러가 발생한다.
			if (Modifier == null)
			{
				return;
			}

		}

		

		//private object _controlPramDialog_LoadKey = null;

		private void DrawModifierPropertyGUI(int width, int height)
		{
			if (Modifier != null)
			{
				string strRecordName = Modifier.DisplayName;


				if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
				{
					//Rigging UI를 작성
					DrawModifierPropertyGUI_Rigging(width, height, strRecordName);
				}
				else if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Physic)
				{
					//Physic UI를 작성
					DrawModifierPropertyGUI_Physics(width, height);
				}
				else
				{
					//그 외에는 ParamSetGroup에 따라서 UI를 구성하면 된다.
					switch (Modifier.SyncTarget)
					{
						case apModifierParamSetGroup.SYNC_TARGET.Bones:
							break;

						case apModifierParamSetGroup.SYNC_TARGET.Controller:
							{
								//Control Param 리스트
								apDialog_SelectControlParam.PARAM_TYPE paramFilter = apDialog_SelectControlParam.PARAM_TYPE.All;
								DrawModifierPropertyGUI_ControllerParamSet(width, height, paramFilter, strRecordName);
							}
							break;

						case apModifierParamSetGroup.SYNC_TARGET.ControllerWithoutKey:
							break;

						case apModifierParamSetGroup.SYNC_TARGET.KeyFrame:
							{
								//Keyframe 리스트
								DrawModifierPropertyGUI_KeyframeParamSet(width, height, strRecordName);
							}
							break;

						case apModifierParamSetGroup.SYNC_TARGET.Static:
							break;
					}
				}

			}


		}


#region [미사용 코드]
		//private void MeshGroupBottomStatus_Modifier_Volume(int width, int height)
		//{
		//	apDialog_SelectControlParam.PARAM_TYPE paramFilter =
		//	apDialog_SelectControlParam.PARAM_TYPE.Float |
		//			apDialog_SelectControlParam.PARAM_TYPE.Int |
		//			apDialog_SelectControlParam.PARAM_TYPE.Vector2 |
		//			apDialog_SelectControlParam.PARAM_TYPE.Vector3;

		//	DrawModifierPropertyGUI_ControllerParamSet(width, height, paramFilter, "Volume");

		//	GUILayout.Space(20);
		//} 
#endregion

		

		// Modifier 보조 함수들
		//------------------------------------------------------------------------------------
		private void DrawModifierPropertyGUI_ControllerParamSet(int width, int height, apDialog_SelectControlParam.PARAM_TYPE paramFilter, string recordName)
		{
			
			// SyncTarget으로 Control Param을 받아서 Modifier를 제어하는 경우
			//GUIStyle guiNone = new GUIStyle(GUIStyle.none);
			//guiNone.normal.textColor = GUI.skin.label.normal.textColor;

			//GUIStyle guiSelected = new GUIStyle(GUIStyle.none);
			//if(EditorGUIUtility.isProSkin)
			//{
			//	guiSelected.normal.textColor = Color.cyan;
			//}
			//else
			//{
			//	guiSelected.normal.textColor = Color.white;
			//}
			


			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ControlParameters), apGUILOFactory.I.Width(width));//"Control Parameters"

			GUILayout.Space(5);


			// 생성된 Morph Key (Parameter Group)를 선택하자
			//------------------------------------------------------------------
			// Control Param에 따른 Param Set Group 리스트
			//------------------------------------------------------------------
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(120));
			GUILayout.Space(5);

			Rect lastRect = GUILayoutUtility.GetLastRect();

			Color prevColor = GUI.backgroundColor;

			GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);

			GUI.Box(new Rect(lastRect.x + 5, lastRect.y, width, 120), apStringFactory.I.None);
			GUI.backgroundColor = prevColor;

			//처리 역순으로 보여준다.
			List<apModifierParamSetGroup> paramSetGroups = new List<apModifierParamSetGroup>();
			if (Modifier._paramSetGroup_controller.Count > 0)
			{
				for (int i = Modifier._paramSetGroup_controller.Count - 1; i >= 0; i--)
				{
					paramSetGroups.Add(Modifier._paramSetGroup_controller[i]);
				}
			}

			//등록된 Control Param Group 리스트를 출력하자
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(120));
			_scrollBottom_Status = EditorGUILayout.BeginScrollView(_scrollBottom_Status, false, true);
			GUILayout.Space(2);
			int scrollWidth = width - (30);
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(scrollWidth), apGUILOFactory.I.Height(120));
			GUILayout.Space(3);

			//Texture2D paramIconImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Param);
			Texture2D visibleIconImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Visible_Current);
			Texture2D nonvisibleIconImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_NonVisible_Current);

			//현재 선택중인 파라미터 그룹
			apModifierParamSetGroup curParamSetGroup = SubEditedParamSetGroup;


			//추가
			if (_guiContent_Modifier_ParamSetItem == null)
			{
				_guiContent_Modifier_ParamSetItem = new apGUIContentWrapper();
				_guiContent_Modifier_ParamSetItem.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Param));
			}

			GUIStyle curGUIStyle = null;//최적화된 코드
			for (int i = 0; i < paramSetGroups.Count; i++)
			{
				
				if (curParamSetGroup == paramSetGroups[i])
				{
					lastRect = GUILayoutUtility.GetLastRect();

					if (EditorGUIUtility.isProSkin)
					{
						GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
					}
					else
					{
						GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
					}


					int offsetHeight = 18 + 3;
					if (i == 0)
					{
						offsetHeight = 1 + 3;
					}

					GUI.Box(new Rect(lastRect.x, lastRect.y + offsetHeight, scrollWidth + 35, 20), apStringFactory.I.None);
					GUI.backgroundColor = prevColor;

					curGUIStyle = apGUIStyleWrapper.I.None_White2Cyan;
				}
				else
				{
					curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;
				}

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
				GUILayout.Space(5);
				
				//이전
				//if (GUILayout.Button(new GUIContent(" " + paramSetGroups[i]._keyControlParam._keyName, paramIconImage),
				//					curGUIStyle,
				//					GUILayout.Width(scrollWidth - (5 + 25)), GUILayout.Height(20)))

				//변경
				_guiContent_Modifier_ParamSetItem.SetText(1, paramSetGroups[i]._keyControlParam._keyName);
				if (GUILayout.Button(_guiContent_Modifier_ParamSetItem.Content, curGUIStyle, apGUILOFactory.I.Width(scrollWidth - (5 + 25)), apGUILOFactory.I.Height(20)))
				{
					//ParamSetGroup을 선택했다.
					SetParamSetGroupOfModifier(paramSetGroups[i]);
					AutoSelectParamSetOfModifier();//<자동 선택까지

					Editor.RefreshControllerAndHierarchy(false);
				}

				Texture2D imageVisible = visibleIconImage;

				if (!paramSetGroups[i]._isEnabled)
				{
					imageVisible = nonvisibleIconImage;
				}
				if (GUILayout.Button(imageVisible, apGUIStyleWrapper.I.None_LabelColor, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
				{
					paramSetGroups[i]._isEnabled = !paramSetGroups[i]._isEnabled;
				}
				EditorGUILayout.EndHorizontal();
			}


			EditorGUILayout.EndVertical();

			GUILayout.Space(120);
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			//------------------------------------------------------------------ < Param Set Group 리스트
			//추가 3.22
			//이전
			//if(GUILayout.Button(new GUIContent("  Add Control Parameter", Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_AddTransform)), GUILayout.Height(25)))

			//변경
			if (_guiContent_Modifier_AddControlParameter == null)
			{
				_guiContent_Modifier_AddControlParameter = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AddControlParameter), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_AddTransform));
			}
			
			
			if(GUILayout.Button(_guiContent_Modifier_AddControlParameter.Content, apGUILOFactory.I.Height(25)))
			{
				//ParamSetGroup에 추가되지 않은 컨트롤 파라미터를 추가하자
				List<apControlParam> addableControlParams = new List<apControlParam>();
				List<apControlParam> totalControlParams = _portrait._controller._controlParams;
				for (int i = 0; i < totalControlParams.Count; i++)
				{
					//paramSetGroup에 등록 안된 컨트롤 파라미터를 추가
					apControlParam curParam = totalControlParams[i];
					bool isAlreadyRegistered = paramSetGroups.Exists(delegate(apModifierParamSetGroup a)
					{
						return curParam == a._keyControlParam;
					});
					if(!isAlreadyRegistered)
					{
						addableControlParams.Add(curParam);
					}
				}
				_loadKey_AddControlParam = apDialog_SelectControlParam.ShowDialogWithList(
																	Editor, 
																	addableControlParams, 
																	OnAddControlParameterToModifierAsParamSetGroup, 
																	Modifier);//<<SaveObject로는 모디파이어를 입력
			}


			//-----------------------------------------------------------------------------------
			// Param Set Group 선택시 / 선택된 Param Set Group 정보와 포함된 Param Set 리스트
			//-----------------------------------------------------------------------------------



			GUILayout.Space(10);

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.CP_Selected_ParamSetGroup, (SubEditedParamSetGroup != null));//"CP Selected ParamSetGroup"

			if (!Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.CP_Selected_ParamSetGroup))//"CP Selected ParamSetGroup"
			{
				return;
			}
			//ParamSetGroup에 레이어 옵션이 추가되었다.
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.SetOfKeys));//"Parameters Setting" -> "Set of Keys"
			GUILayout.Space(2);
			//"Blend Method" -> "Blend"
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Blend));
			apModifierParamSetGroup.BLEND_METHOD psgBlendMethod = (apModifierParamSetGroup.BLEND_METHOD)EditorGUILayout.EnumPopup(SubEditedParamSetGroup._blendMethod, apGUILOFactory.I.Width(width));
			if (psgBlendMethod != SubEditedParamSetGroup._blendMethod)
			{
				apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
				SubEditedParamSetGroup._blendMethod = psgBlendMethod;
			}

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Weight), apGUILOFactory.I.Width(80));//"Weight"
			float psgLayerWeight = EditorGUILayout.Slider(SubEditedParamSetGroup._layerWeight, 0.0f, 1.0f, apGUILOFactory.I.Width(width - 85));
			if (psgLayerWeight != SubEditedParamSetGroup._layerWeight)
			{
				apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
				SubEditedParamSetGroup._layerWeight = psgLayerWeight;
			}

			EditorGUILayout.EndHorizontal();

			if ((int)(Modifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.Color) != 0)
			{
				//색상 옵션을 넣어주자
				//" Color Option On", " Color Option Off"
				if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Modifier_ColorVisibleOption),
													1, Editor.GetUIWord(UIWORD.ColorOptionOn), Editor.GetUIWord(UIWORD.ColorOptionOff),
													SubEditedParamSetGroup._isColorPropertyEnabled, true,
													width, 24))
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
					SubEditedParamSetGroup._isColorPropertyEnabled = !SubEditedParamSetGroup._isColorPropertyEnabled;
					Editor.RefreshControllerAndHierarchy(false);
				}

				//추가 20.02.22 : Show Hide 토글 기능이 추가되었다.
				//TODO : 텍스트 번역 필요
				//"Toggle Visibility without blending"
				if (apEditorUtil.ToggledButton_2Side(Editor.GetUIWord(UIWORD.ToggleVisibilityWOBlending),
													SubEditedParamSetGroup._isToggleShowHideWithoutBlend, SubEditedParamSetGroup._isColorPropertyEnabled,
													width, 22))
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
					SubEditedParamSetGroup._isToggleShowHideWithoutBlend = !SubEditedParamSetGroup._isToggleShowHideWithoutBlend;
					Editor.RefreshControllerAndHierarchy(false);
				}
			}

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.LayerUp), apGUILOFactory.I.Width(width / 2 - 2)))//"Layer Up"
			{
				Modifier.ChangeParamSetGroupLayerIndex(SubEditedParamSetGroup, SubEditedParamSetGroup._layerIndex + 1);
			}
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.LayerDown), apGUILOFactory.I.Width(width / 2 - 2)))//"Layer Down"
			{
				Modifier.ChangeParamSetGroupLayerIndex(SubEditedParamSetGroup, SubEditedParamSetGroup._layerIndex - 1);
			}
			EditorGUILayout.EndHorizontal();

			//TODO : ModMeshOfMod만 작성되어있다.
			//ModBoneOfMod도 작성되어야 한다.

			GUILayout.Space(5);
			//변경 : Copy&Paste는 ModMesh가 선택되어있느냐, ModBone이 선택되어있느냐에 따라 다르다

			bool isModMeshSelected = ModMeshOfMod != null;
			bool isModBoneSelected = ModBoneOfMod != null && Modifier.IsTarget_Bone;

			//복사 가능한가
			bool isModPastable = false;

			if (isModMeshSelected)		{ isModPastable = apSnapShotManager.I.IsPastable(ModMeshOfMod); }
			else if (isModBoneSelected) { isModPastable = apSnapShotManager.I.IsPastable(ModBoneOfMod); }

			//Color prevColor = GUI.backgroundColor;

			//GUIStyle guiStyle_Center = new GUIStyle(GUI.skin.box);
			//guiStyle_Center.alignment = TextAnchor.MiddleCenter;
			//guiStyle_Center.normal.textColor = apEditorUtil.BoxTextColor;

			if (isModPastable)
			{
				GUI.backgroundColor = new Color(0.2f, 0.5f, 0.7f, 1.0f);
				//guiStyle_Center.normal.textColor = Color.white;
			}

			//Clipboard 이름 설정
			//string strClipboardKeyName = "";

			//if (isModMeshSelected)		{ strClipboardKeyName = apSnapShotManager.I.GetClipboardName_ModMesh(); }
			//else if (isModBoneSelected)	{ strClipboardKeyName = apSnapShotManager.I.GetClipboardName_ModBone(); }

			//if (string.IsNullOrEmpty(strClipboardKeyName))
			//{
			//	strClipboardKeyName = "<Empty Clipboard>";
			//}

			if(_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}
			_strWrapper_64.Clear();

			if (isModMeshSelected)		{ _strWrapper_64.Append(apSnapShotManager.I.GetClipboardName_ModMesh(), true); }
			else if (isModBoneSelected)	{ _strWrapper_64.Append(apSnapShotManager.I.GetClipboardName_ModBone(), true); }

			if(string.IsNullOrEmpty(_strWrapper_64.ToString()))
			{
				_strWrapper_64.Clear();
				_strWrapper_64.Append(apStringFactory.I.EmptyClipboard, true);
			}


			GUILayout.Box(	_strWrapper_64.ToString(), 
							//guiStyle_Center, 
							(isModPastable ? apGUIStyleWrapper.I.Box_MiddleCenter_WhiteColor : apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor),
							apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(32));
			GUI.backgroundColor = prevColor;

			//추가
			//선택된 키가 있다면 => Copy / Paste / Reset 버튼을 만든다.
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));

			//" Copy"
			if(_guiContent_CopyTextIcon == null)
			{
				_guiContent_CopyTextIcon = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.Copy), Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy));
			}
			if(_guiContent_PasteTextIcon == null)
			{
				_guiContent_PasteTextIcon = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.Paste), Editor.ImageSet.Get(apImageSet.PRESET.Edit_Paste));
			}
			

			//이전
			//if (GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.Copy), Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy)), GUILayout.Width(width / 2 - 4), GUILayout.Height(24)))

			//변경
			if (GUILayout.Button(_guiContent_CopyTextIcon.Content, apGUILOFactory.I.Width(width / 2 - 4), apGUILOFactory.I.Height(24)))
			{
				//Debug.LogError("TODO : Copy Morph Key");
				//ModMesh를 복사할 것인지, ModBone을 복사할 것인지 결정
				if (SubEditedParamSetGroup != null && ParamSetOfMod != null)
				{
					//복사하기 > 여기선 string 사용 가능
					if (isModMeshSelected && ParamSetOfMod._meshData.Contains(ModMeshOfMod))
					{
						//ModMesh 복사
						string clipboardName = "";
						if (ModMeshOfMod._transform_Mesh != null)			{ clipboardName = ModMeshOfMod._transform_Mesh._nickName; }
						else if (ModMeshOfMod._transform_MeshGroup != null) { clipboardName = ModMeshOfMod._transform_MeshGroup._nickName; }

						//clipboardName += "\n" + ParamSetOfMod._controlKeyName + "( " + ParamSetOfMod.ControlParamValue + " )";
						string controlParamName = "[Unknown Param]";
						if (SubEditedParamSetGroup._keyControlParam != null)
						{
							controlParamName = SubEditedParamSetGroup._keyControlParam._keyName;
						}
						clipboardName += "\n" + controlParamName + "( " + ParamSetOfMod.ControlParamValue + " )";

						apSnapShotManager.I.Copy_ModMesh(ModMeshOfMod, clipboardName);
					}
					else if (isModBoneSelected && ParamSetOfMod._boneData.Contains(ModBoneOfMod))
					{
						//ModBone 복사
						string clipboardName = "";
						if (ModBoneOfMod._bone != null)
						{ clipboardName = ModBoneOfMod._bone._name; }

						//clipboardName += "\n" + ParamSetOfMod._controlKeyName + "( " + ParamSetOfMod.ControlParamValue + " )";
						string controlParamName = "[Unknown Param]";
						if (SubEditedParamSetGroup._keyControlParam != null)
						{
							controlParamName = SubEditedParamSetGroup._keyControlParam._keyName;
						}
						clipboardName += "\n" + controlParamName + "( " + ParamSetOfMod.ControlParamValue + " )";

						apSnapShotManager.I.Copy_ModBone(ModBoneOfMod, clipboardName);
					}
				}
			}

			//" Paste"
			//이전
			//if (GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.Paste), Editor.ImageSet.Get(apImageSet.PRESET.Edit_Paste)), GUILayout.Width(width / 2 - 4), GUILayout.Height(24)))

			//변경
			if (GUILayout.Button(_guiContent_PasteTextIcon.Content, apGUILOFactory.I.Width(width / 2 - 4), apGUILOFactory.I.Height(24)))
			{
				//ModMesh를 복사할 것인지, ModBone을 복사할 것인지 결정
				if (SubEditedParamSetGroup != null && ParamSetOfMod != null)
				{
					object targetObj = ModMeshOfMod;
					if (isModBoneSelected)
					{
						targetObj = ModBoneOfMod;
					}
					apEditorUtil.SetRecord_MeshGroupAndModifier(apUndoGroupData.ACTION.Modifier_ModMeshValuePaste, Editor, MeshGroup, Modifier, targetObj, false);

					if (isModMeshSelected && ParamSetOfMod._meshData.Contains(ModMeshOfMod))
					{
						//ModMesh 붙여넣기를 하자
						bool isResult = apSnapShotManager.I.Paste_ModMesh(ModMeshOfMod);
						if (!isResult)
						{
							//EditorUtility.DisplayDialog("Paste Failed", "Paste Failed", "Okay");
							Editor.Notification("Paste Failed", true, false);
						}
						//MeshGroup.AddForceUpdateTarget(ModMeshOfMod._renderUnit);
						MeshGroup.RefreshForce();
					}
					else if (isModBoneSelected && ParamSetOfMod._boneData.Contains(ModBoneOfMod))
					{
						//ModBone 붙여넣기를 하자
						bool isResult = apSnapShotManager.I.Paste_ModBone(ModBoneOfMod);
						if (!isResult)
						{
							//EditorUtility.DisplayDialog("Paste Failed", "Paste Failed", "Okay");
							Editor.Notification("Paste Failed", true, false);
						}
						//if(ModBoneOfMod._renderUnit != null)
						//{
						//	MeshGroup.AddForceUpdateTarget(ModBoneOfMod._renderUnit);
						//}
						MeshGroup.RefreshForce();
					}

				}
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button(Editor.GetUIWord(UIWORD.ResetValue), apGUILOFactory.I.Width(width - 4), apGUILOFactory.I.Height(20)))//"Reset Value"
			{
				if (ParamSetOfMod != null)
				{
					object targetObj = ModMeshOfMod;
					if (ModBoneOfMod != null)
					{
						targetObj = ModBoneOfMod;
					}

					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_ModMeshValueReset, Editor, Modifier, targetObj, false);

					if (ModMeshOfMod != null)
					{
						//ModMesh를 리셋한다.

						ModMeshOfMod.ResetValues();

						//MeshGroup.AddForceUpdateTarget(ModMeshOfMod._renderUnit);
						MeshGroup.RefreshForce();
					}
					else if (ModBoneOfMod != null)
					{
						//ModBone을 리셋한다.
						ModBoneOfMod._transformMatrix.SetIdentity();
						//if(ModBoneOfMod._renderUnit != null)
						//{
						//	MeshGroup.AddForceUpdateTarget(ModBoneOfMod._renderUnit);
						//}
						MeshGroup.RefreshForce();
					}
				}
			}

			//추가 : Transform(Controller)에 한해서 Pose를 저장할 수 있다.
			if(Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.TF)
			{
				GUILayout.Space(10);
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ExportImportPose));//"Export/Import Pose"
				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(5);

				if(_guiContent_Modifier_RigExport == null)
				{
					_guiContent_Modifier_RigExport = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.Export), Editor.ImageSet.Get(apImageSet.PRESET.Rig_SaveLoad));
				}
				
				if(_guiContent_Modifier_RigImport == null)
				{
					_guiContent_Modifier_RigImport = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.Import), Editor.ImageSet.Get(apImageSet.PRESET.Rig_LoadBones));
				}
				


				//" Export"
				//이전
				//if(GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.Export), Editor.ImageSet.Get(apImageSet.PRESET.Rig_SaveLoad)), GUILayout.Width((width / 2) - 4), GUILayout.Height(25)))

				//변경
				if(GUILayout.Button(_guiContent_Modifier_RigExport.Content, apGUILOFactory.I.Width((width / 2) - 4), apGUILOFactory.I.Height(25)))
				{
					//Export Dialog 호출
					apDialog_RetargetSinglePoseExport.ShowDialog(Editor, MeshGroup, Bone);
				}

				//" Import"
				//이전
				//if(GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.Import), Editor.ImageSet.Get(apImageSet.PRESET.Rig_LoadBones)), GUILayout.Width((width / 2) - 4), GUILayout.Height(25)))

				//변경
				if(GUILayout.Button(_guiContent_Modifier_RigImport.Content, apGUILOFactory.I.Width((width / 2) - 4), apGUILOFactory.I.Height(25)))
				{
					//Import Dialog 호출
					if (SubEditedParamSetGroup != null && ParamSetOfMod != null)
					{
						_loadKey_SinglePoseImport_Mod = apDialog_RetargetSinglePoseImport.ShowDialog(OnRetargetSinglePoseImportMod, Editor, MeshGroup, Modifier, ParamSetOfMod);
					}
					
					
				}

				EditorGUILayout.EndHorizontal();
			}


			GUILayout.Space(12);



			//--------------------------------------------------------------
			// Param Set 중 하나를 선택했을 때
			// 타겟을 등록 / 해제한다.
			// Transform 등록 / 해제
			//--------------------------------------------------------------
			bool isAnyTargetSelected = false;
			bool isContain = false;
			
			//string strTargetName = "";
			bool isTargetName = false;

			object selectedObj = null;

			if(_guiContent_ModProp_ParamSetTarget_Name == null)
			{
				_guiContent_ModProp_ParamSetTarget_Name = new apGUIContentWrapper();
			}
			if(_guiContent_ModProp_ParamSetTarget_StatusText == null)
			{
				_guiContent_ModProp_ParamSetTarget_StatusText = new apGUIContentWrapper();
			}
			_guiContent_ModProp_ParamSetTarget_Name.ClearText(false);
			_guiContent_ModProp_ParamSetTarget_StatusText.ClearText(false);

			bool isTarget_Bone = Modifier.IsTarget_Bone;
			bool isTarget_MeshTransform = Modifier.IsTarget_MeshTransform;
			bool isTarget_MeshGroupTransform = Modifier.IsTarget_MeshGroupTransform;
			bool isTarget_ChildMeshTransform = Modifier.IsTarget_ChildMeshTransform;

			bool isBoneTarget = false;

			// 타겟을 선택하자
			bool isAddable = false;
			if (isTarget_Bone && !isAnyTargetSelected)
			{
				//1. Bone 선택
				//TODO : Bone 체크
				if (Bone != null)
				{
					isAnyTargetSelected = true;
					isAddable = true;
					isContain = SubEditedParamSetGroup.IsBoneContain(Bone);
					
					//strTargetName = Bone._name;
					_guiContent_ModProp_ParamSetTarget_Name.AppendText(Bone._name, true);
					isTargetName = true;

					selectedObj = Bone;
					isBoneTarget = true;
				}
			}
			if (isTarget_MeshTransform && !isAnyTargetSelected)
			{
				//2. Mesh Transform 선택
				//Child 체크가 가능할까
				if (SubMeshInGroup != null)
				{
					apRenderUnit targetRenderUnit = null;
					//Child Mesh를 허용하는가
					if (isTarget_ChildMeshTransform)
					{
						//Child를 허용한다.
						targetRenderUnit = MeshGroup.GetRenderUnit(SubMeshInGroup);
					}
					else
					{
						//Child를 허용하지 않는다.
						targetRenderUnit = MeshGroup.GetRenderUnit_NoRecursive(SubMeshInGroup);
					}

					if (targetRenderUnit != null)
					{
						//유효한 선택인 경우
						isContain = SubEditedParamSetGroup.IsMeshTransformContain(SubMeshInGroup);
						isAnyTargetSelected = true;
						
						//strTargetName = SubMeshInGroup._nickName;
						_guiContent_ModProp_ParamSetTarget_Name.AppendText(SubMeshInGroup._nickName, true);
						isTargetName = true;

						selectedObj = SubMeshInGroup;

						isAddable = true;
					}
				}
			}
			if (isTarget_MeshGroupTransform && !isAnyTargetSelected)
			{
				if (SubMeshGroupInGroup != null)
				{
					//3. MeshGroup Transform 선택
					isContain = SubEditedParamSetGroup.IsMeshGroupTransformContain(SubMeshGroupInGroup);
					isAnyTargetSelected = true;

					//strTargetName = SubMeshGroupInGroup._nickName;
					_guiContent_ModProp_ParamSetTarget_Name.AppendText(SubMeshGroupInGroup._nickName, true);
					isTargetName = true;

					selectedObj = SubMeshGroupInGroup;

					isAddable = true;
				}
			}

			//타겟이 없었다면 빈이름 대입
			if(!isTargetName)
			{
				_guiContent_ModProp_ParamSetTarget_Name.AppendText(apStringFactory.I.None, true);
			}


			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check, isAnyTargetSelected);//"Modifier_Add Transform Check"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check_Inverse, !isAnyTargetSelected);//"Modifier_Add Transform Check_Inverse"

			bool isGUI_TargetSelected = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check);////"Modifier_Add Transform Check"
			bool isGUI_TargetUnSelected = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check_Inverse);//"Modifier_Add Transform Check_Inverse"

			if (isGUI_TargetSelected || isGUI_TargetUnSelected)
			{
				if (isGUI_TargetSelected)
				{
					//Color prevColor = GUI.backgroundColor;
					//GUIStyle boxGUIStyle = new GUIStyle(GUI.skin.box);
					//boxGUIStyle.alignment = TextAnchor.MiddleCenter;
					//boxGUIStyle.normal.textColor = apEditorUtil.BoxTextColor;

					if (isContain)
					{
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.Selected), true);


						GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
						//"[" + strTargetName + "]\nSelected"
						//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.Selected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
						GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						GUI.backgroundColor = prevColor;

						//"  Remove From Keys"
						//이전
						//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveFromKeys), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_RemoveFromControlParamKey)), GUILayout.Width(width), GUILayout.Height(35)))

						//변경
						if(_guiContent_Modifier_RemoveFromKeys == null)
						{
							_guiContent_Modifier_RemoveFromKeys = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveFromKeys), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_RemoveFromControlParamKey));
						}
						

						if (GUILayout.Button(_guiContent_Modifier_RemoveFromKeys.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35)))
						{

							//bool result = EditorUtility.DisplayDialog("Remove From Keys", "Remove From Keys [" + strTargetName + "]", "Remove", "Cancel");
							bool result = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveFromKeys_Title),
																Editor.GetTextFormat(TEXT.RemoveFromKeys_Body, _guiContent_ModProp_ParamSetTarget_Name.Content.text),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);

							if (result)
							{
								object targetObj = null;
								if (SubMeshInGroup != null && selectedObj == SubMeshInGroup)
								{
									targetObj = SubMeshInGroup;
								}
								else if (SubMeshGroupInGroup != null && selectedObj == SubMeshGroupInGroup)
								{
									targetObj = SubMeshGroupInGroup;
								}
								else if (Bone != null && selectedObj == Bone)
								{
									targetObj = Bone;
								}

								//Undo
								apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_RemoveModMeshFromParamSet, Editor, Modifier, targetObj, false);

								if (SubMeshInGroup != null && selectedObj == SubMeshInGroup)
								{
									SubEditedParamSetGroup.RemoveModifierMeshes(SubMeshInGroup);
								}
								else if (SubMeshGroupInGroup != null && selectedObj == SubMeshGroupInGroup)
								{
									SubEditedParamSetGroup.RemoveModifierMeshes(SubMeshGroupInGroup);
								}
								else if (Bone != null && selectedObj == Bone)
								{
									SubEditedParamSetGroup.RemoveModifierBones(Bone);
								}
								else
								{
									//?
								}

								//이전 <<<< ????
								Editor._portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_Modifier(MeshGroup, Modifier));
								//테스트
								//Editor._portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_ExceptAnimModifiers(MeshGroup));
								AutoSelectModMeshOrModBone();
								Editor.RefreshControllerAndHierarchy(false);

								Editor.SetRepaint();
							}
						}
					}
					else if (!isAddable)
					{
						//추가 가능하지 않다.

						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.NotAbleToBeAdded), true);


						GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
						//"[" + strTargetName + "]\nNot able to be Added"
						//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.NotAbleToBeAdded), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
						GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						GUI.backgroundColor = prevColor;
					}
					else
					{
						//아직 추가하지 않았다. 추가하자

						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
						_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.NotAddedtoEdit), true);

						GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
						//"[" + strTargetName + "]\nNot Added to Edit"
						//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.NotAddedtoEdit), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
						GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						GUI.backgroundColor = prevColor;




						//"  Add To Keys"
						//이전
						//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddToKeys), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToControlParamKey)), GUILayout.Width(width), GUILayout.Height(50)))

						//변경
						if (_guiContent_Modifier_AddToKeys == null)
						{
							_guiContent_Modifier_AddToKeys = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AddToKeys), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToControlParamKey));
						}
						if (GUILayout.Button(_guiContent_Modifier_AddToKeys.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(50)))
						{
							//ModMesh또는 ModBone으로 생성 후 추가한다.
							if (isBoneTarget)
							{
								//Bone
								Editor.Controller.AddModBone_WithSelectedBone();
							}
							else
							{
								//MeshTransform, MeshGroup
								Editor.Controller.AddModMesh_WithSubMeshOrSubMeshGroup();
							}

							Editor.SetRepaint();

							//추가 : ExEdit 모드가 아니라면, Modifier에 추가할 때 자동으로 ExEdit 상태로 전환
							if (ExEditingMode == EX_EDIT.None && IsExEditable)
							{
								SetModifierExclusiveEditing(EX_EDIT.ExOnly_Edit);

								//변경 3.23 : 선택 잠금을 무조건 켜는게 아니라, 에디터 설정에 따라 켤지 말지 결정한다.
								//true 또는 변경 없음 (false가 아님)
								//모디파이어의 종류에 따라서 다른 옵션을 적용
								if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Physic ||
									Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
								{
									if (Editor._isSelectionLockOption_RiggingPhysics)
									{
										_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
									}
								}
								else if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Morph ||
									Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.AnimatedMorph)
								{
									if (Editor._isSelectionLockOption_Morph)
									{
										_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
									}
								}
								else
								{
									if (Editor._isSelectionLockOption_Transform)
									{
										_isSelectionLock = true;//처음 Editing 작업시 Lock을 거는 것으로 변경
									}
								}
								
							}
						}
					}
					GUI.backgroundColor = prevColor;
				}

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(10));
				EditorGUILayout.EndVertical();
				GUILayout.Space(11);

				//ParamSetWeight를 사용하는 Modifier인가
				bool isUseParamSetWeight = Modifier.IsUseParamSetWeight;


				// Param Set 리스트를 출력한다.
				//-------------------------------------
				int iRemove = -1;
				for (int i = 0; i < SubEditedParamSetGroup._paramSetList.Count; i++)
				{
					bool isRemove = DrawModParamSetProperty(i, SubEditedParamSetGroup, SubEditedParamSetGroup._paramSetList[i], width - 10, ParamSetOfMod, isUseParamSetWeight);
					if (isRemove)
					{
						iRemove = i;
					}
				}
				if (iRemove >= 0)
				{
					Editor.Controller.RemoveRecordKey(SubEditedParamSetGroup._paramSetList[iRemove], null);
				}
			}


			//-----------------------------------------------------------------------------------
		}




		private void OnRetargetSinglePoseImportMod(	object loadKey, bool isSuccess, apRetarget resultRetarget,
													apMeshGroup targetMeshGroup,
													apModifierBase targetModifier, apModifierParamSet targetParamSet,
													apDialog_RetargetSinglePoseImport.IMPORT_METHOD importMethod)
		{
			if(loadKey != _loadKey_SinglePoseImport_Mod || !isSuccess)
			{
				_loadKey_SinglePoseImport_Mod = null;
				return;
			}

			_loadKey_SinglePoseImport_Mod = null;

			//Import 처리를 하자
			Editor.Controller.ImportBonePoseFromRetargetSinglePoseFileToModifier(targetMeshGroup, resultRetarget, targetModifier, targetParamSet, importMethod);
		}


		//추가 3.22 : Make Key가 아닌 Add Control Parameter 기능에 의한 "컨트롤 파라미터 > 모디파이어"
		public void OnAddControlParameterToModifierAsParamSetGroup(bool isSuccess, object loadKey, apControlParam resultControlParam, object savedObject)
		{
			if(loadKey != _loadKey_AddControlParam 
				|| !isSuccess
				|| resultControlParam == null)
			{
				_loadKey_AddControlParam = null;
				return;
			}
			
			_loadKey_AddControlParam = null;
			
			//현재 모디파이어 메뉴가 아니거나, 저장된 모디파이어가 아니라면 종료
			if(SelectionType != SELECTION_TYPE.MeshGroup ||
				MeshGroup == null ||
				Modifier == null)
			{
				return;
			}

			object curObject = Modifier;
			if (savedObject != curObject)
			{
				return;
			}

			//인자 2 : 기본값으로 생성하려면 False를 입력 (true는 현재값으로 생성)
			//인자 3 : 현재 메시나 본을 바로 등록하려면 True, 여기서는 False
			Editor.Controller.AddControlParamToModifier(resultControlParam, false, false);
		}




		private bool DrawModParamSetProperty(int index, apModifierParamSetGroup paramSetGroup, apModifierParamSet paramSet, int width, apModifierParamSet selectedParamSet, bool isUseParamSetWeight)
		{
			bool isRemove = false;
			Rect lastRect = GUILayoutUtility.GetLastRect();
			Color prevColor = GUI.backgroundColor;

			bool isSelect = false;
			if (paramSet == selectedParamSet)
			{
				GUI.backgroundColor = new Color(0.9f, 0.7f, 0.7f, 1.0f);
				isSelect = true;
			}
			else
			{
				GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
			}

			int heightOffset = 18;
			if (index == 0)
			{
				//heightOffset = 5;
				heightOffset = 9;
			}

			GUI.Box(new Rect(lastRect.x, lastRect.y + heightOffset, width + 10, 30), apStringFactory.I.None);
			GUI.backgroundColor = prevColor;



			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));

			GUILayout.Space(10);

			int compWidth = width - (55 + 20 + 5 + 10);
			if (isUseParamSetWeight)
			{
				//GUIStyle guiStyle = new GUIStyle(GUI.skin.textField);
				//guiStyle.alignment = TextAnchor.MiddleLeft;

				//ParamSetWeight를 출력/수정할 수 있게 한다.
				float paramSetWeight = EditorGUILayout.DelayedFloatField(paramSet._overlapWeight, apGUIStyleWrapper.I.TextField_MiddleLeft, apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(20));
				if (paramSetWeight != paramSet._overlapWeight)
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
					paramSet._overlapWeight = Mathf.Clamp01(paramSetWeight);
					apEditorUtil.ReleaseGUIFocus();
					MeshGroup.RefreshForce();
					Editor.RefreshControllerAndHierarchy(false);
				}
				compWidth -= 34;
			}

			switch (paramSetGroup._keyControlParam._valueType)
			{
				//case apControlParam.TYPE.Bool:
				//	{
				//		GUIStyle guiStyle = new GUIStyle(GUI.skin.toggle);
				//		guiStyle.alignment = TextAnchor.MiddleLeft;
				//		paramSet._conSyncValue_Bool = EditorGUILayout.Toggle(paramSet._conSyncValue_Bool, guiStyle, GUILayout.Width(compWidth), GUILayout.Height(20));
				//	}

				//	break;

				case apControlParam.TYPE.Int:
					{
						//GUIStyle guiStyle = new GUIStyle(GUI.skin.textField);
						//guiStyle.alignment = TextAnchor.MiddleLeft;
						int conInt = EditorGUILayout.DelayedIntField(paramSet._conSyncValue_Int, apGUIStyleWrapper.I.TextField_MiddleLeft, apGUILOFactory.I.Width(compWidth), apGUILOFactory.I.Height(20));
						if (conInt != paramSet._conSyncValue_Int)
						{
							//이건 Dirty만 하자
							apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
							paramSet._conSyncValue_Int = conInt;
							apEditorUtil.ReleaseGUIFocus();
						}

					}
					break;

				case apControlParam.TYPE.Float:
					{
						//GUIStyle guiStyle = new GUIStyle(GUI.skin.textField);
						//guiStyle.alignment = TextAnchor.MiddleLeft;
						float conFloat = EditorGUILayout.DelayedFloatField(paramSet._conSyncValue_Float, apGUIStyleWrapper.I.TextField_MiddleLeft, apGUILOFactory.I.Width(compWidth), apGUILOFactory.I.Height(20));
						if (conFloat != paramSet._conSyncValue_Float)
						{
							apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
							paramSet._conSyncValue_Float = conFloat;
							apEditorUtil.ReleaseGUIFocus();
						}
					}
					break;

				case apControlParam.TYPE.Vector2:
					{
						//GUIStyle guiStyle = new GUIStyle(GUI.skin.textField);
						//guiStyle.alignment = TextAnchor.MiddleLeft;
						float conVec2X = EditorGUILayout.DelayedFloatField(paramSet._conSyncValue_Vector2.x, apGUIStyleWrapper.I.TextField_MiddleLeft, apGUILOFactory.I.Width(compWidth / 2 - 2), apGUILOFactory.I.Height(20));
						float conVec2Y = EditorGUILayout.DelayedFloatField(paramSet._conSyncValue_Vector2.y, apGUIStyleWrapper.I.TextField_MiddleLeft, apGUILOFactory.I.Width(compWidth / 2 - 2), apGUILOFactory.I.Height(20));
						if (conVec2X != paramSet._conSyncValue_Vector2.x || conVec2Y != paramSet._conSyncValue_Vector2.y)
						{
							apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, null, false);
							paramSet._conSyncValue_Vector2.x = conVec2X;
							paramSet._conSyncValue_Vector2.y = conVec2Y;
							apEditorUtil.ReleaseGUIFocus();
						}

					}
					break;
			}

			if (isSelect)
			{
				//GUIStyle guiStyle = new GUIStyle(GUI.skin.box);
				//guiStyle.normal.textColor = Color.white;
				//guiStyle.alignment = TextAnchor.UpperCenter;

				GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
				GUILayout.Box(Editor.GetUIWord(UIWORD.Selected), apGUIStyleWrapper.I.Box_UpperCenter_WhiteColor, apGUILOFactory.I.Width(55), apGUILOFactory.I.Height(20));//"Editing" -> Selected
				GUI.backgroundColor = prevColor;
			}
			else
			{
				//"Select"
				if (GUILayout.Button(Editor.GetUIWord(UIWORD.Select), apGUILOFactory.I.Width(55), apGUILOFactory.I.Height(20)))
				{
					if (Editor.LeftTab != apEditor.TAB_LEFT.Controller)
					{
						//옵션이 허용하는 경우 (19.6.28 변경)
						if (Editor._isAutoSwitchControllerTab_Mod)
						{
							Editor.SetLeftTab(apEditor.TAB_LEFT.Controller);
						}
					}

					SetParamSetOfModifier(paramSet);
					if (ParamSetOfMod != null)
					{
						apControlParam targetControlParam = paramSetGroup._keyControlParam;
						if (targetControlParam != null)
						{
							//switch (ParamSetOfMod._controlParam._valueType)
							switch (targetControlParam._valueType)
							{
								//case apControlParam.TYPE.Bool:
								//	targetControlParam._bool_Cur = paramSet._conSyncValue_Bool;
								//	break;

								case apControlParam.TYPE.Int:
									targetControlParam._int_Cur = paramSet._conSyncValue_Int;
									//if (targetControlParam._isRange)
									{
										targetControlParam._int_Cur =
											Mathf.Clamp(targetControlParam._int_Cur,
														targetControlParam._int_Min,
														targetControlParam._int_Max);
									}
									break;

								case apControlParam.TYPE.Float:
									targetControlParam._float_Cur = paramSet._conSyncValue_Float;
									//if (targetControlParam._isRange)
									{
										targetControlParam._float_Cur =
											Mathf.Clamp(targetControlParam._float_Cur,
														targetControlParam._float_Min,
														targetControlParam._float_Max);
									}
									break;

								case apControlParam.TYPE.Vector2:
									targetControlParam._vec2_Cur = paramSet._conSyncValue_Vector2;
									//if (targetControlParam._isRange)
									{
										targetControlParam._vec2_Cur.x =
											Mathf.Clamp(targetControlParam._vec2_Cur.x,
														targetControlParam._vec2_Min.x,
														targetControlParam._vec2_Max.x);

										targetControlParam._vec2_Cur.y =
											Mathf.Clamp(targetControlParam._vec2_Cur.y,
														targetControlParam._vec2_Min.y,
														targetControlParam._vec2_Max.y);
									}
									break;


							}
						}
					}
				}
			}

			if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey), apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove Record Key", "Remove Record Key?", "Remove", "Cancel");
				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveRecordKey_Title),
																Editor.GetText(TEXT.RemoveRecordKey_Body),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel));
				if (isResult)
				{
					//삭제시 true 리턴
					isRemove = true;
				}
			}



			EditorGUILayout.EndHorizontal();
			GUILayout.Space(20);

			return isRemove;
		}

		




		private void DrawModifierPropertyGUI_KeyframeParamSet(int width, int height, string recordName)
		{
			//GUIStyle guiNone = new GUIStyle(GUIStyle.none);
			//guiNone.normal.textColor = GUI.skin.label.normal.textColor;

			//GUIStyle guiSelected = new GUIStyle(GUIStyle.none);
			//if(EditorGUIUtility.isProSkin)
			//{
			//	guiSelected.normal.textColor = Color.cyan;
			//}
			//else
			//{
			//	guiSelected.normal.textColor = Color.white;
			//}
			
			//"Animation Clips"
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AnimationClips), apGUILOFactory.I.Width(width));

			GUILayout.Space(5);

			// 생성된 ParamSet Group을 선택하자
			//------------------------------------------------------------------
			// AnimClip에 따른 Param Set Group Anim Pack 리스트
			//------------------------------------------------------------------
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(120));
			GUILayout.Space(5);

			Rect lastRect = GUILayoutUtility.GetLastRect();

			Color prevColor = GUI.backgroundColor;

			GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);

			GUI.Box(new Rect(lastRect.x + 5, lastRect.y, width, 120), apStringFactory.I.None);
			GUI.backgroundColor = prevColor;


			List<apModifierParamSetGroupAnimPack> paramSetGroupAnimPacks = Modifier._paramSetGroupAnimPacks;


			//등록된 Keyframe Param Group 리스트를 출력하자
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(120));
			_scrollBottom_Status = EditorGUILayout.BeginScrollView(_scrollBottom_Status, false, true);
			GUILayout.Space(2);
			int scrollWidth = width - (30);
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(scrollWidth), apGUILOFactory.I.Height(120));
			GUILayout.Space(3);


			//이전
			//Texture2D animIconImage = Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation);

			//추가
			if (_guiContent_Modifier_AnimIconText == null)
			{
				_guiContent_Modifier_AnimIconText = new apGUIContentWrapper();
				_guiContent_Modifier_AnimIconText.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation));
			}
			

			//현재 선택중인 파라미터 그룹
			apModifierParamSetGroupAnimPack curParamSetGroupAnimPack = SubEditedParamSetGroupAnimPack;

			GUIStyle curGUIStyle = null;//최적화된 코드
			for (int i = 0; i < paramSetGroupAnimPacks.Count; i++)
			{
				//GUIStyle curGUIStyle = guiNone;

				//이전 : 선택 여부에 따라서 
				//if (curParamSetGroupAnimPack == paramSetGroupAnimPacks[i])
				//{
				//	lastRect = GUILayoutUtility.GetLastRect();

				//	if (EditorGUIUtility.isProSkin)
				//	{
				//		GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
				//	}
				//	else
				//	{
				//		GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
				//	}

				//	int offsetHeight = 18 + 3;
				//	if (i == 0)
				//	{
				//		offsetHeight = 1 + 3;
				//	}

				//	GUI.Box(new Rect(lastRect.x, lastRect.y + offsetHeight, scrollWidth + 35, 20), apStringFactory.I.None);
				//	GUI.backgroundColor = prevColor;

				//	//curGUIStyle = guiSelected;
				//	curGUIStyle = apGUIStyleWrapper.I.None_White2Cyan;
				//}
				//else
				//{
				//	curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;
				//}

				curGUIStyle = apGUIStyleWrapper.I.None_LabelColor;

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
				GUILayout.Space(5);

				_guiContent_Modifier_AnimIconText.SetText(1, paramSetGroupAnimPacks[i].LinkedAnimClip._name);
				if (GUILayout.Button(_guiContent_Modifier_AnimIconText.Content, curGUIStyle,
									apGUILOFactory.I.Width(scrollWidth - (5)), apGUILOFactory.I.Height(20)))
				{
					//이전 : 클릭하면 선택을 한다.
					//SetParamSetGroupAnimPackOfModifier(paramSetGroupAnimPacks[i]);
					//Editor.RefreshControllerAndHierarchy(false);

					//변경 20.4.4 : 이걸 선택해서 사용할 수 있는 기능도 없고 AnimParamSetGroup이 Link가 안되어 있을 수 있다.
					//따라서 선택 기능을 아예 없앤다.
				}
				EditorGUILayout.EndHorizontal();
			}


			EditorGUILayout.EndVertical();

			GUILayout.Space(120);
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			//------------------------------------------------------------------ < Param Set Group 리스트

			//-----------------------------------------------------------------------------------
			// Param Set Group 선택시 / 선택된 Param Set Group 정보와 포함된 Param Set 리스트
			//-----------------------------------------------------------------------------------

			//GUILayout.Space(10);

			//>> 여기서 ParamSetGroup 설정을 할 순 없다. (ParamSetGroup이 TimelineLayer이므로.
			//AnimClip 기준으로는 ParamSetGroup을 묶은 가상의 그룹(SubEditedParamSetGroupAnimPack)을 설정해야하는데,
			//이건 묶음이므로 실제로는 Animation 설정에서 Timeline에서 해야한다. (Timelinelayer = ParamSetGroup이므로)
			//Editor.SetGUIVisible("Anim Selected ParamSetGroup", (SubEditedParamSetGroupAnimPack. != null));

			//if (!Editor.IsDelayedGUIVisible("Anim Selected ParamSetGroup"))
			//{
			//	return;
			//}


			//EditorGUILayout.LabelField("Selected Animation Clip");
			//EditorGUILayout.LabelField(_subEditedParamSetGroupAnimPack._keyAnimClip._name);
			//GUILayout.Space(5);

			//if ((int)(Modifier.CalculatedValueType & apCalculatedResultParam.CALCULATED_VALUE_TYPE.Color) != 0)
			//{
			//	//색상 옵션을 넣어주자
			//	EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			//	EditorGUILayout.LabelField("Color Option", GUILayout.Width(160));
			//	_subEditedParamSetGroupAnimPack._isColorPropertyEnabled = EditorGUILayout.Toggle(_subEditedParamSetGroupAnimPack._isColorPropertyEnabled, GUILayout.Width(width - 85));
			//	EditorGUILayout.EndHorizontal();
			//}
		}


		

		//Rigging Modifier UI를 출력한다.
		private void DrawModifierPropertyGUI_Rigging(int width, int height, string recordName)
		{	
			//"Target Mesh Transform" > 생략 20.3.29
			//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.TargetMeshTransform), apGUILOFactory.I.Width(width));

			//1. Mesh Transform 등록 체크
			//2. Weight 툴
			// 선택한 Vertex
			// Auto Normalize
			// Set Weight, +/- Weight, * Weight
			// Blend, Auto Rigging, Normalize, Prune,
			// Copy / Paste
			// Bone (Color, Remove)

			bool isTarget_MeshTransform = Modifier.IsTarget_MeshTransform;
			bool isTarget_ChildMeshTransform = Modifier.IsTarget_ChildMeshTransform;

			bool isContainInParamSetGroup = false;
			
			//string strTargetName = "";
			bool isTargetName = false;

			if(_guiContent_ModProp_ParamSetTarget_Name == null)
			{
				_guiContent_ModProp_ParamSetTarget_Name = new apGUIContentWrapper();
			}
			if(_guiContent_ModProp_ParamSetTarget_StatusText == null)
			{
				_guiContent_ModProp_ParamSetTarget_StatusText = new apGUIContentWrapper();
			}
			_guiContent_ModProp_ParamSetTarget_Name.ClearText(false);
			_guiContent_ModProp_ParamSetTarget_StatusText.ClearText(false);


			object selectedObj = null;
			bool isAnyTargetSelected = false;
			bool isAddable = false;

#if UNITY_EDITOR_OSX
			bool isCtrl = Event.current.command;
#else
			bool isCtrl = Event.current.control;
#endif

			apTransform_Mesh targetMeshTransform = SubMeshInGroup;
			apModifierParamSetGroup paramSetGroup = SubEditedParamSetGroup;
			if (paramSetGroup == null)
			{
				//? Rigging에서는 ParamSetGroup이 있어야 한다.
				Editor.Controller.AddStaticParamSetGroupToModifier();

				if (Modifier._paramSetGroup_controller.Count > 0)
				{
					SetParamSetGroupOfModifier(Modifier._paramSetGroup_controller[0]);
				}
				paramSetGroup = SubEditedParamSetGroup;
				if (paramSetGroup == null)
				{
					Debug.LogError("AnyPortrait : ParamSet Group Is Null (" + Modifier._paramSetGroup_controller.Count + ")");
					return;
				}

				AutoSelectModMeshOrModBone();
			}
			apModifierParamSet paramSet = ParamSetOfMod;
			if (paramSet == null)
			{
				//Rigging에서는 1개의 ParamSetGroup과 1개의 ParamSet이 있어야 한다.
				//선택된게 없다면, ParamSet이 1개 있는지 확인
				//그후 선택한다.

				if (paramSetGroup._paramSetList.Count == 0)
				{
					paramSet = new apModifierParamSet();
					paramSet.LinkParamSetGroup(paramSetGroup);
					paramSetGroup._paramSetList.Add(paramSet);
				}
				else
				{
					paramSet = paramSetGroup._paramSetList[0];
				}
				SetParamSetOfModifier(paramSet);
			}



			//1. Mesh Transform 등록 체크
			if (targetMeshTransform != null)
			{
				apRenderUnit targetRenderUnit = null;
				//Child Mesh를 허용하는가
				if (isTarget_ChildMeshTransform)
				{
					//Child를 허용한다.
					targetRenderUnit = MeshGroup.GetRenderUnit(targetMeshTransform);
				}
				else
				{
					//Child를 허용하지 않는다.
					targetRenderUnit = MeshGroup.GetRenderUnit_NoRecursive(targetMeshTransform);
				}
				if (targetRenderUnit != null)
				{
					//유효한 선택인 경우
					isContainInParamSetGroup = paramSetGroup.IsMeshTransformContain(targetMeshTransform);
					isAnyTargetSelected = true;
					
					//strTargetName = targetMeshTransform._nickName;
					_guiContent_ModProp_ParamSetTarget_Name.AppendText(targetMeshTransform._nickName, true);
					isTargetName = true;

					selectedObj = targetMeshTransform;

					isAddable = true;
				}
			}

			//대상이 없다면
			if(!isTargetName)
			{
				_guiContent_ModProp_ParamSetTarget_Name.AppendText(apStringFactory.I.None, true);
			}


			if (Event.current.type == EventType.Layout ||
				Event.current.type == EventType.Repaint)
			{
				_riggingModifier_prevSelectedTransform = targetMeshTransform;
				_riggingModifier_prevIsContained = isContainInParamSetGroup;
			}
			bool isSameSetting = (targetMeshTransform == _riggingModifier_prevSelectedTransform)
								&& (isContainInParamSetGroup == _riggingModifier_prevIsContained);


			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check__Rigging, isSameSetting);//"Modifier_Add Transform Check [Rigging]



			if (!Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check__Rigging))//"Modifier_Add Transform Check [Rigging]
			{
				return;
			}

			Color prevColor = GUI.backgroundColor;

			//GUIStyle boxGUIStyle = new GUIStyle(GUI.skin.box);
			//boxGUIStyle.alignment = TextAnchor.MiddleCenter;
			//boxGUIStyle.normal.textColor = apEditorUtil.BoxTextColor;

			if (targetMeshTransform == null)
			{
				//선택된 MeshTransform이 없다.
				GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
				//"No Mesh is Selected"
				GUILayout.Box(Editor.GetUIWord(UIWORD.NoMeshIsSelected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;
			}
			else if (isContainInParamSetGroup)
			{
				GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				//"[" + strTargetName + "]\nSelected"

				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.Selected), true);

				//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.Selected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
				GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;

				//"  Remove From Rigging"

				//이전
				//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveFromRigging), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_RemoveFromRigging)), GUILayout.Width(width), GUILayout.Height(30)))

				//변경
				if (_guiContent_Modifier_RemoveFromRigging == null)
				{
					_guiContent_Modifier_RemoveFromRigging = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveFromRigging), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_RemoveFromRigging));
				}
				
				if (GUILayout.Button(_guiContent_Modifier_RemoveFromRigging.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
				{

					//bool result = EditorUtility.DisplayDialog("Remove From Rigging", "Remove From Rigging [" + strTargetName + "]", "Remove", "Cancel");

					bool result = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveFromRigging_Title),
																Editor.GetTextFormat(TEXT.RemoveFromRigging_Body, _guiContent_ModProp_ParamSetTarget_Name.Content.text),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);

					if (result)
					{
						object targetObj = SubMeshInGroup;
						if (SubMeshGroupInGroup != null && selectedObj == SubMeshGroupInGroup)
						{
							targetObj = SubMeshGroupInGroup;
						}

						apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_RemoveBoneRigging, Editor, Modifier, targetObj, false);

						if (SubMeshInGroup != null && selectedObj == SubMeshInGroup)
						{
							SubEditedParamSetGroup.RemoveModifierMeshes(SubMeshInGroup);
						}
						else if (SubMeshGroupInGroup != null && selectedObj == SubMeshGroupInGroup)
						{
							SubEditedParamSetGroup.RemoveModifierMeshes(SubMeshGroupInGroup);
						}
						else
						{
							//TODO : Bone 제거
						}

						Editor._portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_Modifier(MeshGroup, Modifier));
						AutoSelectModMeshOrModBone();

						Editor.Hierarchy_MeshGroup.RefreshUnits();
						Editor.RefreshControllerAndHierarchy(false);

						Editor.SetRepaint();
					}
				}
			}
			else if (!isAddable)
			{
				//추가 가능하지 않다.

				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.NotAbleToBeAdded), true);

				GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				//"[" + strTargetName + "]\nNot able to be Added"
				//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.NotAbleToBeAdded), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
				GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;
			}
			else
			{
				//아직 추가하지 않았다. 추가하자

				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.NotAddedtoEdit), true);

				GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				//"[" + strTargetName + "]\nNot Added to Edit"
				//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.NotAddedtoEdit), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
				GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;

				//"  Add Rigging" -> "  Add to Rigging"
				//이전
				//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddToRigging), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToRigging)), GUILayout.Width(width), GUILayout.Height(30)))

				//변경
				if(_guiContent_Modifier_AddToRigging == null)
				{
					_guiContent_Modifier_AddToRigging = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AddToRigging), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToRigging));
				}

				if (GUILayout.Button(_guiContent_Modifier_AddToRigging.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
				{
					Editor.Controller.AddModMesh_WithSubMeshOrSubMeshGroup();

					Editor.Hierarchy_MeshGroup.RefreshUnits();

					Editor.SetRepaint();

					//추가 11.7 : 만약 Rig Edit 모드가 아니면, Rig Edit모드로 바로 활성화
					if(!IsRigEditBinding)
					{
						ToggleRigEditBinding();
					}
				}
			}
			GUI.backgroundColor = prevColor;

			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);

			List<ModRenderVert> selectedVerts = Editor.Select.ModRenderVertListOfMod;
			//bool isAnyVertSelected = (selectedVerts != null && selectedVerts.Count > 0);


			//2. Weight 툴
			// 선택한 Vertex
			// Auto Normalize
			// Set Weight, +/- Weight, * Weight
			// Blend, Auto Rigging, Normalize, Prune,
			// Copy / Paste
			// Bone (Color, Remove)

			//어떤 Vertex가 선택되었는지 표기한다.

			_rigEdit_vertRigDataList.Clear();
			VertRigData curBoneRigData = null;
			
			int nSelectedVerts = 0;
			if (isAnyTargetSelected)
			{
				nSelectedVerts = selectedVerts.Count;

				//리스트에 넣을 Rig 리스트를 완성하자
				for (int i = 0; i < selectedVerts.Count; i++)
				{
					apModifiedVertexRig modVertRig = selectedVerts[i]._modVertRig;
					if (modVertRig == null)
					{
						// -ㅅ-?
						continue;
					}
					for (int iPair = 0; iPair < modVertRig._weightPairs.Count; iPair++)
					{
						apModifiedVertexRig.WeightPair pair = modVertRig._weightPairs[iPair];
						VertRigData targetBoneData = _rigEdit_vertRigDataList.Find(delegate (VertRigData a)
						{
							return a._bone == pair._bone;
						});

						if (targetBoneData != null)
						{
							targetBoneData.AddRig(pair._weight);
						}
						else
						{
							targetBoneData = new VertRigData(pair._bone, pair._weight);
							_rigEdit_vertRigDataList.Add(targetBoneData);
						}

						if(Bone != null && targetBoneData._bone == Bone)
						{
							curBoneRigData = targetBoneData;
						}
					}
				}
			}

			//추가 19.7.27 : RigLock에 따라 최대 가중치가 1이 아닌 그 이하의 값일 수 있다.
			float maxRigWeight = GetMaxRigWeight(curBoneRigData);


			//-----------------------------------------------
			//2. 리깅 정보 리스트 (20.3.29 : 아래에서 위로 올라옴)
			//-----------------------------------------------
			int rigListHeight = 150;//200 > 150
			int nRigDataList = _rigEdit_vertRigDataList.Count;
			if (_riggingModifier_prevNumBoneWeights != nRigDataList)
			{
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rig_Mod__RigDataCount_Refreshed, true);//"Rig Mod - RigDataCount Refreshed"
				if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Rig_Mod__RigDataCount_Refreshed))//"Rig Mod - RigDataCount Refreshed"
				{
					_riggingModifier_prevNumBoneWeights = nRigDataList;
				}
			}
			else
			{
				Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rig_Mod__RigDataCount_Refreshed, false);//"Rig Mod - RigDataCount Refreshed"
			}

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(rigListHeight));
			GUILayout.Space(5);

			Rect lastRect = GUILayoutUtility.GetLastRect();

			GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);

			GUI.Box(new Rect(lastRect.x + 5, lastRect.y, width, rigListHeight), apStringFactory.I.None);
			GUI.backgroundColor = prevColor;


			//Weight 리스트를 출력하자
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(rigListHeight));
			_scrollBottom_Status = EditorGUILayout.BeginScrollView(_scrollBottom_Status, false, true);
			GUILayout.Space(2);
			int scrollWidth = width - (30);
			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(scrollWidth), apGUILOFactory.I.Height(rigListHeight));
			GUILayout.Space(3);

			Texture2D imgRemove = Editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey);

			VertRigData vertRigData = null;

			if (_guiContent_RiggingBoneWeightLabel == null)
			{
				_guiContent_RiggingBoneWeightLabel = new apGUIContentWrapper();
			}
			if(_guiContent_RiggingBoneWeightBoneName == null)
			{
				_guiContent_RiggingBoneWeightBoneName = new apGUIContentWrapper();
			}

			//string strLabel = "";
			//선택, 삭제할 리깅 데이터를 선택하면 화면 하단에서 처리한다. (안그러면 UI가 꼬인다)
			VertRigData selectRigData = null;
			VertRigData removeRigData = null;
			int widthLabel_Name = scrollWidth - (5 + 25 + 14 + 2 + 60);

			//GUIStyle guiStyle_RigIcon_Normal = apEditorUtil.WhiteGUIStyle_Box;
			if (_guiStyle_RigIcon_Lock == null)
			{
				_guiStyle_RigIcon_Lock = new GUIStyle(GUI.skin.box);//<<최적화된 코드
				_guiStyle_RigIcon_Lock.normal.background = Editor.ImageSet.Get(apImageSet.PRESET.Rig_Lock16px);
			}
			

			GUIStyle curGUIStyle = null;//<<최적화된 코드

			//for (int i = 0; i < _rigEdit_vertRigDataList.Count; i++)
			for (int i = 0; i < _riggingModifier_prevNumBoneWeights; i++)
			{
				if (i < _rigEdit_vertRigDataList.Count)
				{
					//GUIStyle curGUIStyle = guiNone;
					vertRigData = _rigEdit_vertRigDataList[i];
					if (vertRigData._bone == Bone)
					{
						lastRect = GUILayoutUtility.GetLastRect();

						if (EditorGUIUtility.isProSkin)
						{
							GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
						}
						else
						{
							GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
						}

						int offsetHeight = 18 + 3;
						if (i == 0)
						{
							offsetHeight = 1 + 3;
						}

						GUI.Box(new Rect(lastRect.x, lastRect.y + offsetHeight, scrollWidth + 35, 20), apStringFactory.I.None);
						GUI.backgroundColor = prevColor;

						//curGUIStyle = guiSelected;
						curGUIStyle = apGUIStyleWrapper.I.None_MiddleLeft_White2Cyan;
					}
					else
					{
						curGUIStyle = apGUIStyleWrapper.I.None_MiddleLeft_LabelColor;
					}


					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
					GUILayout.Space(5);

					//Bone의 색상, 이름, Weight, X를 출력
					

					if(vertRigData._bone != null && vertRigData._bone._isRigLock)
					{
						GUILayout.Box(apStringFactory.I.None, _guiStyle_RigIcon_Lock, apGUILOFactory.I.Width(14), apGUILOFactory.I.Height(14));//자물쇠 이미지
					}
					else
					{
						GUI.backgroundColor = vertRigData._bone._color;
						GUILayout.Box(apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box, apGUILOFactory.I.Width(14), apGUILOFactory.I.Height(14));//일반 박스 이미지
						GUI.backgroundColor = prevColor;
					}

					_guiContent_RiggingBoneWeightLabel.ClearText(false);
					

					if (nSelectedVerts > 1 && (vertRigData._weight_Max - vertRigData._weight_Min) > 0.01f)
					{
						//여러개가 섞여서 Weight가 의미가 없어졌다.
						//Min + Max로 표현하자
						//strLabel = string.Format("{0:N2}~{1:N2}", vertRigData._weight_Min, vertRigData._weight_Max);
						_guiContent_RiggingBoneWeightLabel.AppendText(string.Format("{0:N2}~{1:N2}", vertRigData._weight_Min, vertRigData._weight_Max), true);
						
					}
					else
					{
						//Weight를 출력한다.
						//strLabel = ((int)vertRigData._weight) + "." + ((int)(vertRigData._weight * 1000.0f + 0.5f) % 1000);
						//strLabel = string.Format("{0:N3}", vertRigData._weight);
						_guiContent_RiggingBoneWeightLabel.AppendText(string.Format("{0:N3}", vertRigData._weight), true);
					}

					//이전
					//string rigName = vertRigData._bone._name;
					//if (rigName.Length > 14)
					//{
					//	rigName = rigName.Substring(0, 12) + "..";
					//}

					//변경
					_guiContent_RiggingBoneWeightBoneName.ClearText(false);
					if(vertRigData._bone._name.Length > 14)
					{
						_guiContent_RiggingBoneWeightBoneName.AppendText(vertRigData._bone._name.Substring(0, 12), false);
						_guiContent_RiggingBoneWeightBoneName.AppendText(apStringFactory.I.Dot2, true);
					}
					else
					{
						_guiContent_RiggingBoneWeightBoneName.AppendText(vertRigData._bone._name, true);
					}

					if (GUILayout.Button(_guiContent_RiggingBoneWeightBoneName.Content,
										curGUIStyle,
										apGUILOFactory.I.Width(widthLabel_Name), apGUILOFactory.I.Height(20)))
					{	
						//Editor.Select.SetBone(vertRigData._bone);//이전
						selectRigData = vertRigData;//변경 : 바로 SetBone을 호출하지 말자
					}
					if (GUILayout.Button(_guiContent_RiggingBoneWeightLabel.Content,
										curGUIStyle,
										apGUILOFactory.I.Width(60), apGUILOFactory.I.Height(20)))
					{
						//Editor.Select.SetBone(vertRigData._bone);
						selectRigData = vertRigData;//변경 : 바로 SetBone을 호출하지 말자
					}

					if (GUILayout.Button(imgRemove, curGUIStyle, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
					{
						//Debug.LogError("TODO : Bone Remove From Rigging");
						removeRigData = vertRigData;
					}

					EditorGUILayout.EndHorizontal();
				}
				else
				{
					//GUI 렌더 문제로 더미 렌더링
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
					GUILayout.Space(5);

					GUILayout.Box(apStringFactory.I.None, apGUILOFactory.I.Width(14), apGUILOFactory.I.Height(14));

					if (GUILayout.Button(apStringFactory.I.None,
										apGUIStyleWrapper.I.None_MiddleLeft_LabelColor,
										apGUILOFactory.I.Width(widthLabel_Name), apGUILOFactory.I.Height(20)))
					{
						//Dummy
					}
					if (GUILayout.Button(apStringFactory.I.None,
										apGUIStyleWrapper.I.None_MiddleLeft_LabelColor,
										apGUILOFactory.I.Width(60), apGUILOFactory.I.Height(20)))
					{
						//Dummy
					}

					if (GUILayout.Button(imgRemove, apGUIStyleWrapper.I.None_MiddleLeft_LabelColor, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
					{
						//Debug.LogError("TODO : Bone Remove From Rigging");
						//removeRigData = vertRigData;
						//Dummy
					}


					EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.EndVertical();

			GUILayout.Space(120);
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();


			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);


			//기존 : 버텍스 선택 정보만 있다.
#region [미사용 코드] v1.1.8 이전의 UI
			//if (!isAnyTargetSelected || (selectedVerts != null && selectedVerts.Count == 0))
			//{
			//	//선택된게 없다.
			//	GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
			//	//"No Vetex is Selected"
			//	GUILayout.Box(Editor.GetUIWord(UIWORD.NoVertexisSelected), boxGUIStyle, GUILayout.Width(width), GUILayout.Height(25));

			//	GUI.backgroundColor = prevColor;
			//}
			//else if (selectedVerts.Count == 1)
			//{
			//	//1개의 Vertex
			//	GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
			//	//"[Vertex " + selectedVerts[0]._renderVert._vertex._index + "] is Selected"
			//	GUILayout.Box(Editor.GetUIWordFormat(UIWORD.SingleVertexSelected, selectedVerts[0]._renderVert._vertex._index), boxGUIStyle, GUILayout.Width(width), GUILayout.Height(25));

			//	GUI.backgroundColor = prevColor;

			//}
			//else
			//{
			//	GUI.backgroundColor = new Color(0.4f, 0.5f, 1.0f, 1.0f);
			//	//selectedVerts.Count + " Verts are Selected"
			//	GUILayout.Box(Editor.GetUIWordFormat(UIWORD.NumVertsareSelected, selectedVerts.Count), boxGUIStyle, GUILayout.Width(width), GUILayout.Height(25));

			//	GUI.backgroundColor = prevColor;
			//} 
#endregion


			int width_TabHalf = (width - (5)) / 2;

			//변경 19.7.25 : 버텍스 정보 + 본 정보와 Weight를 직접 설정할 수 있다.
			Color boxColor_VertexInfo = Color.black;
			//string str_VertexInfo = null;

			Color boxColor_BoneInfo = Color.black;
			//string str_BoneInfo = null;
			bool isBoneRigLock = false;

			if(_guiContent_ModProp_Rigging_VertInfo == null)
			{
				_guiContent_ModProp_Rigging_VertInfo = new apGUIContentWrapper();
			}
			if(_guiContent_ModProp_Rigging_BoneInfo == null)
			{
				_guiContent_ModProp_Rigging_BoneInfo = new apGUIContentWrapper();
			}
			
			_guiContent_ModProp_Rigging_VertInfo.ClearText(false);
			_guiContent_ModProp_Rigging_BoneInfo.ClearText(false);

			//TODO : 언어
			//버텍스 정보 :	박스 색상은 파란색 계열
			if (!isAnyTargetSelected || (selectedVerts != null && selectedVerts.Count == 0))
			{
				boxColor_VertexInfo = new Color(0.5f, 0.5f, 0.5f, 1.0f);
				//str_VertexInfo = "No Vertex";
				_guiContent_ModProp_Rigging_VertInfo.AppendText(apStringFactory.I.NoVertex, true);
			}
			else if (selectedVerts.Count == 1)
			{
				boxColor_VertexInfo = new Color(0.4f, 1.0f, 1.0f, 1.0f);
				//str_VertexInfo = "Vertex [" + selectedVerts[0]._renderVert._vertex._index + "]";
				_guiContent_ModProp_Rigging_VertInfo.AppendText(apStringFactory.I.VertexWithBracket, false);
				_guiContent_ModProp_Rigging_VertInfo.AppendText(selectedVerts[0]._renderVert._vertex._index, false);
				_guiContent_ModProp_Rigging_VertInfo.AppendText(apStringFactory.I.Bracket_2_R, true);
			}
			else
			{
				boxColor_VertexInfo = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				//str_VertexInfo = selectedVerts.Count + " Vertices";
				_guiContent_ModProp_Rigging_VertInfo.AppendText(selectedVerts.Count, false);
				_guiContent_ModProp_Rigging_VertInfo.AppendText(apStringFactory.I.VerticesWithSpace, true);
			}

			//본 정보 : 본의 색상 이용 (밝기를 0.8 이상으로 맞춘다.)
			if(Bone == null)
			{
				boxColor_BoneInfo = new Color(0.5f, 0.5f, 0.5f, 1.0f);
				//str_BoneInfo = "No Bone";
				_guiContent_ModProp_Rigging_BoneInfo.AppendText(apStringFactory.I.NoBone, true);
			}
			else
			{
				boxColor_BoneInfo = Bone._color;
				boxColor_BoneInfo.r = Mathf.Max(boxColor_BoneInfo.r, 0.2f);
				boxColor_BoneInfo.g = Mathf.Max(boxColor_BoneInfo.g, 0.2f);
				boxColor_BoneInfo.b = Mathf.Max(boxColor_BoneInfo.b, 0.2f);

				float lum = (boxColor_BoneInfo.r * 0.5f + boxColor_BoneInfo.g * 0.3f + boxColor_BoneInfo.b * 0.2f);
				if(lum < 0.7f)
				{
					boxColor_BoneInfo.r *= 0.7f / lum;
					boxColor_BoneInfo.g *= 0.7f / lum;
					boxColor_BoneInfo.b *= 0.7f / lum;
				}

				//str_BoneInfo = Bone._name;
				_guiContent_ModProp_Rigging_BoneInfo.AppendText(Bone._name, true);

				isBoneRigLock = Bone._isRigLock;
			}


			//버텍스 정보와 본 정보를 2개의 박스로 표시

			//버텍스 선택 정보
			GUI.backgroundColor = boxColor_VertexInfo;
			GUILayout.Box(_guiContent_ModProp_Rigging_VertInfo.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
			GUI.backgroundColor = prevColor;

			//본 선택 정보 + 본의 Rig Lock
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(22));
			GUILayout.Space(4);

			GUI.backgroundColor = boxColor_BoneInfo;
			GUILayout.Box(_guiContent_ModProp_Rigging_BoneInfo.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 24), apGUILOFactory.I.Height(20));
			GUI.backgroundColor = prevColor;

			
			if(apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Lock16px), Editor.ImageSet.Get(apImageSet.PRESET.Rig_Unlock16px), isBoneRigLock, Bone != null, 20, 22))
			{
				if(Bone != null)
				{
					Bone._isRigLock = !Bone._isRigLock;

					//Max 값을 바꾸자
					maxRigWeight = GetMaxRigWeight(curBoneRigData);

					apEditorUtil.ReleaseGUIFocus();
				}
			}

			EditorGUILayout.EndHorizontal();


			GUILayout.Space(2);

			//추가 19.7.25 : 현재 본과의 Weight를 직접 설정
			//다음의 3가지 상태가 있다.
			//- 편집 불가 상태 / 단일값의 Weight (버텍스가 여러개라도 Weight가 같은 경우) / 범위값의 Weight

			bool isRigUIInfo_MultipleVert = false;
			bool isRigUIInfo_SingleVert = false;
			bool isRigUIInfo_UnregRigData = false;
			int rigUIInfoMode = -1;

			if(isAnyTargetSelected && selectedVerts != null && selectedVerts.Count > 0)
			{
				if (curBoneRigData != null)
				{
					if (curBoneRigData._nRig > 1 && (curBoneRigData._weight_Max - curBoneRigData._weight_Min) > 0.01f)
					{
						isRigUIInfo_MultipleVert = true;
						rigUIInfoMode = 0;
					}
					else
					{
						isRigUIInfo_SingleVert = true;
						rigUIInfoMode = 1;
					}
				}
				else if(Bone != null)
				{
					isRigUIInfo_UnregRigData = true;
					rigUIInfoMode = 2;
				}
			}

			

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__MultipleVert, isRigUIInfo_MultipleVert);//"Rigging UI Info - MultipleVert"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__SingleVert, isRigUIInfo_SingleVert);//"Rigging UI Info - SingleVert"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__UnregRigData, isRigUIInfo_UnregRigData);//"Rigging UI Info - UnregRigData"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__SameMode, _riggingModifier_prevInfoMode == rigUIInfoMode);//"Rigging UI Info - SameMode"

			bool isSameRigUIInfoMode = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__SameMode);//"Rigging UI Info - SameMode"

			int height_VertRigInfo = 30;
			

			if(_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}

			if (isSameRigUIInfoMode)
			{
				if (isRigUIInfo_MultipleVert)
				{
					//범위값의 Weight : 범위값 알려주고 평균값으로 통일

					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__MultipleVert))//"Rigging UI Info - MultipleVert"
					{
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
						GUILayout.Space(5);

						_strWrapper_64.Clear();
						_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Weight), false);
						_strWrapper_64.Append(apStringFactory.I.Colon_Space, false);
						_strWrapper_64.Append(string.Format("{0:N2} ~ {1:N2}", curBoneRigData._weight_Min, curBoneRigData._weight_Max), true);

						//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Weight) + " : " + string.Format("{0:N2} ~ {1:N2}", curBoneRigData._weight_Min, curBoneRigData._weight_Max), apGUILOFactory.I.Width(width - 10));
						EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(width - 10));

						//float maxMultipleRigWeight = Mathf.Max(maxRigWeight, (curBoneRigData._weight_Min + curBoneRigData._weight_Max) / 2.0f);
						//if (GUILayout.Button(string.Format("Set {0:N2}", maxMultipleRigWeight), GUILayout.Height(18)))
						//{
						//	//평균값으로 적용한다.
						//	Editor.Controller.SetBoneWeight(maxMultipleRigWeight, 0, true);//True인자를 넣어서 다른 모든 Rig Weight가 0이 되었다고 해도 값이 할당될 수 있다.
						//}

						EditorGUILayout.EndHorizontal();
					}
					else
					{
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
						GUILayout.Space(5);
						EditorGUILayout.LabelField(apStringFactory.I.None);
						EditorGUILayout.EndHorizontal();
					}
				}
				else if (isRigUIInfo_SingleVert)
				{
					//단일값의 Weight : 슬라이더로 값 제어

					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__SingleVert))//"Rigging UI Info - SingleVert"
					{
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
						float nextWeight = apEditorUtil.FloatSlider(Editor.GetUIWord(UIWORD.Weight), curBoneRigData._weight, 0.0f, maxRigWeight, width - 5, 80);
						if (!Mathf.Approximately(nextWeight, curBoneRigData._weight))
						{
							if (curBoneRigData._bone == Bone)
							{
								Editor.Controller.SetBoneWeight(nextWeight, 0, true, true);//True인자를 넣어서 다른 모든 Rig Weight가 0이 되었다고 해도 값이 할당될 수 있다.
							}
							apEditorUtil.ReleaseGUIFocus();
						}
						EditorGUILayout.EndVertical();
					}
					else
					{
						EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
						GUILayout.Space(5);
						EditorGUILayout.LabelField(apStringFactory.I.None, apGUILOFactory.I.Width(80));
						GUILayout.HorizontalSlider(0, 0, 1);
						EditorGUILayout.FloatField(0);
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndVertical();
					}
				}
				else if (isRigUIInfo_UnregRigData)
				{
					//Rig Data만 없다.

					if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Rigging_UI_Info__UnregRigData))//"Rigging UI Info - UnregRigData"
					{
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
						GUILayout.Space(5);
						if (GUILayout.Button(Editor.GetUIWord(UIWORD.RegisterWithRigging), apGUILOFactory.I.Height(18)))//"Register With Rigging"
						{
							//0의 값을 넣고 등록
							Editor.Controller.SetBoneWeight(0.0f, 0);
							apEditorUtil.ReleaseGUIFocus();
						}
						EditorGUILayout.EndHorizontal();
					}
					else
					{
						EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
						GUILayout.Space(5);
						GUILayout.Button(apStringFactory.I.None);
						EditorGUILayout.EndHorizontal();
					}
				}
				else
				{
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
					GUILayout.Space(5);
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				//더미 UI
				switch (_riggingModifier_prevInfoMode)
				{
					case 0://MultipleVert의 더미
						{
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
							GUILayout.Space(5);

							EditorGUILayout.LabelField(apStringFactory.I.None, apGUILOFactory.I.Width(width - 10));
							//GUILayout.Button("", GUILayout.Height(18));

							EditorGUILayout.EndHorizontal();
						}
						break;

					case 1://SingleVert의 더미
						{
							EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
							GUILayout.Space(5);

							EditorGUILayout.LabelField(apStringFactory.I.None, apGUILOFactory.I.Width(80));
							GUILayout.HorizontalSlider(0, 0, 1);
							EditorGUILayout.FloatField(0);
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.EndVertical();
						}
						break;

					case 2://NoReg의 더미
						{
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
							GUILayout.Space(5);
							GUILayout.Button(apStringFactory.I.None, apGUILOFactory.I.Height(18));
							EditorGUILayout.EndHorizontal();
						}
						break;
					default:
						{
							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(height_VertRigInfo));
							GUILayout.Space(5);
							EditorGUILayout.EndHorizontal();
						}
						break;
				}
			}
			

			if(Event.current.type != EventType.Layout)
			{
				_riggingModifier_prevInfoMode = rigUIInfoMode;
			}
			

			//GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);



			//단축키 : Z, X로 가중치 증감 (20.3.29)
			//- Z키 : 감소, X키 : 증가 (값은 Shift를 누른 경우 0.05, 그냥은 0.02)
			Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingValueChanged_05,		apHotKey.LabelText.IncreaseRigWeight,	KeyCode.X,		true, false, false, true);
			Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingValueChanged_02,		apHotKey.LabelText.IncreaseRigWeight,	KeyCode.X,		false, false, false, true);
			Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingValueChanged_05,		apHotKey.LabelText.DecreaseRigWeight,	KeyCode.Z,		true, false, false, false);
			Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingValueChanged_02,		apHotKey.LabelText.DecreaseRigWeight,	KeyCode.Z,		false, false, false, false);


			// 기본 토대는 3ds Max와 유사하게 가자

			// Edit가 활성화되지 않으면 버튼 선택불가
			bool isBtnAvailable = _rigEdit_isBindingEdit;

			//변경 19.7.24 : 모드에 따라서 숫자 툴로 설정할지, 브러시 툴로 설정할지 결정
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(22));
			GUILayout.Space(5);

			//"Weight"
			if(apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_WeightMode16px), 
											1, Editor.GetUIWord(UIWORD.Numpad),  
											_rigEdit_WeightToolMode == RIGGING_WEIGHT_TOOL_MODE.NumericTool, true, width_TabHalf, 22))
			{
				_rigEdit_WeightToolMode = RIGGING_WEIGHT_TOOL_MODE.NumericTool;
				_rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
				Editor.Gizmos.EndBrush();
			}

			//"Brush"
			if(apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_PaintMode16px), 
											1, Editor.GetUIWord(UIWORD.Brush),  
											_rigEdit_WeightToolMode == RIGGING_WEIGHT_TOOL_MODE.BrushTool, true, width_TabHalf, 22))
			{
				if (_rigEdit_WeightToolMode != RIGGING_WEIGHT_TOOL_MODE.BrushTool)
				{
					_rigEdit_WeightToolMode = RIGGING_WEIGHT_TOOL_MODE.BrushTool;
					_rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None;
					Editor.Gizmos.EndBrush();
				}
				
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(4);

			//변경 19.7.24 : 모드에 따라서 숫자 툴로 설정할지, 브러시 툴로 설정할지 결정
			if (_rigEdit_WeightToolMode == RIGGING_WEIGHT_TOOL_MODE.NumericTool)
			{
				//기존의 "숫자 가중치 툴"

				int CALCULATE_SET = 0;
				int CALCULATE_ADD = 1;
				int CALCULATE_MULTIPLY = 2;

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
				GUILayout.Space(5);
				//고정된 Weight 값
				//0, 0.1, 0.3, 0.5, 0.7, 0.9, 1 (7개)
				int widthPresetWeight = ((width - 2 * 7) / 7) - 2;
				bool isPresetAdapt = false;
				float presetWeight = 0.0f;
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_00, false, isBtnAvailable, widthPresetWeight, 30))//"0"
				{
					isPresetAdapt = true;
					presetWeight = 0.0f;
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_01, false, isBtnAvailable, widthPresetWeight, 30))//".1"
				{
					isPresetAdapt = true;
					presetWeight = 0.1f;
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_03, false, isBtnAvailable, widthPresetWeight, 30))//".3"
				{
					isPresetAdapt = true;
					presetWeight = 0.3f;
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_05, false, isBtnAvailable, widthPresetWeight, 30))//".5"
				{
					isPresetAdapt = true;
					presetWeight = 0.5f;
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_07, false, isBtnAvailable, widthPresetWeight, 30))//".7"
				{
					isPresetAdapt = true;
					presetWeight = 0.7f;
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_09, false, isBtnAvailable, widthPresetWeight, 30))//".9"
				{
					isPresetAdapt = true;
					presetWeight = 0.9f;
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_10, false, isBtnAvailable, widthPresetWeight, 30))//"1"
				{
					isPresetAdapt = true;
					presetWeight = 1f;
				}
				EditorGUILayout.EndHorizontal();

				if (isPresetAdapt)
				{
					Editor.Controller.SetBoneWeight(presetWeight, CALCULATE_SET);
				}

				int heightSetWeight = 25;
				int widthSetBtn = 90;
				int widthIncDecBtn = 30;
				int widthValue = width - (widthSetBtn + widthIncDecBtn * 2 + 2 * 5 + 5);

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightSetWeight));
				GUILayout.Space(5);

				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(widthValue), apGUILOFactory.I.Height(heightSetWeight - 2));
				GUILayout.Space(8);
				_rigEdit_setWeightValue = EditorGUILayout.DelayedFloatField(_rigEdit_setWeightValue);
				EditorGUILayout.EndVertical();

				//"Set Weight"
				if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.SetWeight), false, isBtnAvailable, widthSetBtn, heightSetWeight))
				{
					//Debug.LogError("TODO : Weight 적용 - Set");
					Editor.Controller.SetBoneWeight(_rigEdit_setWeightValue, CALCULATE_SET);
					GUI.FocusControl(null);
				}

				if (apEditorUtil.ToggledButton(apStringFactory.I.Plus, false, isBtnAvailable, widthIncDecBtn, heightSetWeight))//"+"
				{
					////0.05 단위로 올라가거나 내려온다. (5%)
					////현재 값에서 "int형 반올림"을 수행하고 처리
					//_rigEdit_setWeightValue = Mathf.Clamp01((float)((int)(_rigEdit_setWeightValue * 20.0f + 0.5f) + 1) / 20.0f);
					//이게 아니었다..
					//0.05 추가
					Editor.Controller.SetBoneWeight(0.05f, CALCULATE_ADD);

					GUI.FocusControl(null);
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Minus, false, isBtnAvailable, widthIncDecBtn, heightSetWeight))//"-"
				{
					//0.05 단위로 올라가거나 내려온다. (5%)
					//현재 값에서 "int형 반올림"을 수행하고 처리
					//_rigEdit_setWeightValue = Mathf.Clamp01((float)((int)(_rigEdit_setWeightValue * 20.0f + 0.5f) - 1) / 20.0f);
					//0.05 빼기
					Editor.Controller.SetBoneWeight(-0.05f, CALCULATE_ADD);

					GUI.FocusControl(null);
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(3);

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightSetWeight));
				GUILayout.Space(5);


				EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(widthValue), apGUILOFactory.I.Height(heightSetWeight - 2));
				GUILayout.Space(8);
				_rigEdit_scaleWeightValue = EditorGUILayout.DelayedFloatField(_rigEdit_scaleWeightValue);
				EditorGUILayout.EndVertical();

				//"Scale Weight"
				if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.ScaleWeight), false, isBtnAvailable, widthSetBtn, heightSetWeight))
				{
					//Debug.LogError("TODO : Weight 적용 - Set");
					Editor.Controller.SetBoneWeight(_rigEdit_scaleWeightValue, CALCULATE_MULTIPLY);//Multiply 방식
					GUI.FocusControl(null);
				}

				if (apEditorUtil.ToggledButton(apStringFactory.I.Plus, false, isBtnAvailable, widthIncDecBtn, heightSetWeight))//"+"
				{
					//0.01 단위로 올라가거나 내려온다. (1%)
					//현재 값에서 반올림을 수행하고 처리
					//Scale은 Clamp가 걸리지 않는다.
					
					//x1.05
					Editor.Controller.SetBoneWeight(1.05f, CALCULATE_MULTIPLY);//Multiply 방식

					GUI.FocusControl(null);
				}
				if (apEditorUtil.ToggledButton(apStringFactory.I.Minus, false, isBtnAvailable, widthIncDecBtn, heightSetWeight))//"-"
				{
					//0.01 단위로 올라가거나 내려온다. (1%)
					//현재 값에서 반올림을 수행하고 처리
					
					//x0.95
					Editor.Controller.SetBoneWeight(0.95f, CALCULATE_MULTIPLY);//Multiply 방식

					GUI.FocusControl(null);
				}
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				//추가 19.7.24 : 리깅툴 v2의 브러시툴
				//- 브러시모드 : Add, Multiply, Blur
				//- 크기와 커브는 공유, 값은 별도
				//- 우클릭시 모드 취소 ("값"은 더미값이 들어간다.)
				//- 버텍스가 선택 안된 경우 모드 비활성화
				//- 브러시 단축키와 동일
				//- 이 모드 활성시 업데이트가 풀파워로 가동 (절전 모드시)
				//- 브러시 모드가 켜진 상태에서는 버텍스 선택이 불가능. 우클릭으로 모드 해제해야..

				//브러시 모드 선택
				int width_BrushTabBtn = ((width - 5) / 3) - 2;

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(5);
				if(apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_BrushAdd), _rigEdit_BrushToolMode == RIGGING_BRUSH_TOOL_MODE.Add, isBtnAvailable && Bone != null, width_BrushTabBtn, 25))
				{
					if(isBtnAvailable && Bone != null)
					{
						if(_rigEdit_BrushToolMode != RIGGING_BRUSH_TOOL_MODE.Add)	{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.Add; Editor.Gizmos.StartBrush(); }
						else														{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None; Editor.Gizmos.EndBrush(); }
					}
				}
				if(apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_BrushMultiply), _rigEdit_BrushToolMode == RIGGING_BRUSH_TOOL_MODE.Multiply, isBtnAvailable && Bone != null, width_BrushTabBtn, 25))
				{
					if(isBtnAvailable && Bone != null)
					{
						if(_rigEdit_BrushToolMode != RIGGING_BRUSH_TOOL_MODE.Multiply)	{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.Multiply; Editor.Gizmos.StartBrush(); }
						else															{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None; Editor.Gizmos.EndBrush(); }
					}
				}
				if(apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Rig_BrushBlur), _rigEdit_BrushToolMode == RIGGING_BRUSH_TOOL_MODE.Blur, isBtnAvailable && Bone != null, width_BrushTabBtn, 25))
				{
					if(isBtnAvailable && Bone != null)
					{
						if(_rigEdit_BrushToolMode != RIGGING_BRUSH_TOOL_MODE.Blur)	{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.Blur; Editor.Gizmos.StartBrush(); }
						else														{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None; Editor.Gizmos.EndBrush(); }
					}
				}
				
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(2);
				
				//브러시 사이즈
				_rigEdit_BrushRadius = apEditorUtil.IntSlider(Editor.GetUIWord(UIWORD.Radius), _rigEdit_BrushRadius, 1, apGizmos.MAX_BRUSH_RADIUS, width, 80);

				//단축키
				
				//브러시 크기 : [, ]
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushSizeChanged,	apHotKey.LabelText.IncreaseBrushRadius, KeyCode.RightBracket, false, false, false, true);//"Increase Brush Radius"
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushSizeChanged,	apHotKey.LabelText.DecreaseBrushRadius, KeyCode.LeftBracket, false, false, false, false);//"Decrease Brush Radius"
				
				//브러시 모드 선택 : Add-J, Multiply-K, Blur-L
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushMode_Add,		apHotKey.LabelText.BrushMode_Add, KeyCode.J, false, false, false, null);//"Brush Mode - Add"
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushMode_Multiply,	apHotKey.LabelText.BrushMode_Multiply, KeyCode.K, false, false, false, null);//"Brush Mode - Multiply"
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushMode_Blur,		apHotKey.LabelText.BrushMode_Blur, KeyCode.L, false, false, false, null);//"Brush Mode - Blur"

				//브러시 세기 : <, >
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushIntensity,		apHotKey.LabelText.IncreaseBrushIntensity, KeyCode.Period, false, false, false, true);//"Increase Brush Intensity"
				Editor.AddHotKeyEvent(OnHotKeyEvent_RiggingBrushIntensity,		apHotKey.LabelText.DecreaseBrushIntensity, KeyCode.Comma, false, false, false, false);//"Decrease Brush Intensity"


				//브러시 세기 (모드마다 다름)
				switch (_rigEdit_BrushToolMode)
				{
					case RIGGING_BRUSH_TOOL_MODE.None://툴이 선택 안된 상태
						apEditorUtil.IntSlider(Editor.GetUIWord(UIWORD.Intensity), 0, 0, 100, width, 80);
						break;
					case RIGGING_BRUSH_TOOL_MODE.Add:
						_rigEdit_BrushIntensity_Add = apEditorUtil.FloatSlider(Editor.GetUIWord(UIWORD.Intensity), _rigEdit_BrushIntensity_Add, -1, 1, width, 80);
						break;
					case RIGGING_BRUSH_TOOL_MODE.Multiply:
						_rigEdit_BrushIntensity_Multiply = apEditorUtil.FloatSlider(Editor.GetUIWord(UIWORD.Intensity), _rigEdit_BrushIntensity_Multiply, 0.5f, 1.5f, width, 80);
						break;
					case RIGGING_BRUSH_TOOL_MODE.Blur:
						_rigEdit_BrushIntensity_Blur = apEditorUtil.IntSlider(Editor.GetUIWord(UIWORD.Intensity), _rigEdit_BrushIntensity_Blur, 0, 100, width, 80);
						break;
				}
				
			}

			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);

			int heightToolBtn = 25;
			//int width4Btn = ((width - 5) / 4) - (2);

			//Blend, Prune, Normalize, Auto Rigging
			//Normalize On/Off
			//Copy / Paste

			int width2Btn = (width - 5) / 2;

			//Auto Rigging
			//"  Auto Normalize", "  Auto Normalize"
			if (apEditorUtil.ToggledButton_2Side(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_AutoNormalize), 
													2, Editor.GetUIWord(UIWORD.AutoNormalize), Editor.GetUIWord(UIWORD.AutoNormalize), 
													_rigEdit_isAutoNormalize, isBtnAvailable, width, 28))
			{
				_rigEdit_isAutoNormalize = !_rigEdit_isAutoNormalize;

				//Off -> On 시에 Normalize를 적용하자
				if (_rigEdit_isAutoNormalize)
				{
					Editor.Controller.SetBoneWeightNormalize();
				}
				//Auto Normalize는 에디터 옵션으로 저장된다.
				Editor.SaveEditorPref();
			}


			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightToolBtn));
			GUILayout.Space(5);
			//" Blend"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_Blend), 
											1, Editor.GetUIWord(UIWORD.Blend), 
											false, isBtnAvailable, width2Btn, heightToolBtn, 
											apStringFactory.I.RiggingTooltip_Blend))//"Blend the weights of vertices"
			{
				//Blend
				Editor.Controller.SetBoneWeightBlend();
			}
			//" Normalize"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_Normalize), 
											1, Editor.GetUIWord(UIWORD.Normalize), 
											false, isBtnAvailable, width2Btn, heightToolBtn, 
											apStringFactory.I.RiggingTooltip_Normalize))//"Normalize rigging weights"
			{
				//Normalize
				Editor.Controller.SetBoneWeightNormalize();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightToolBtn));
			GUILayout.Space(5);
			//" Prune"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_Prune), 
											1, Editor.GetUIWord(UIWORD.Prune), 
											false, isBtnAvailable, width2Btn, heightToolBtn, 
											apStringFactory.I.RiggingTooltip_Prune))//"Remove rigging bones its weight is under 0.01"
			{
				//Prune
				Editor.Controller.SetBoneWeightPrune();
			}
			//" Auto Rig"
			if (apEditorUtil.ToggledButton_Ctrl(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_Auto), 
											1, Editor.GetUIWord(UIWORD.AutoRig), 
											false, isBtnAvailable, width2Btn, heightToolBtn, 
											apStringFactory.I.RiggingTooltip_AutoRig,
											Event.current.control,
											Event.current.command))//"Rig Automatically"
			{
				//Auto
				//변경 19.12.29 : Ctrl 키를 누르면 본을 선택한다.
				//그냥 누르면 바로 AutoRig 실행
				if(isCtrl)
				{
					//apDialog_SelectMultipleObjects
					_loadKey_SelectBonesForAutoRig = apDialog_SelectBonesForAutoRig.ShowDialog(Editor, MeshGroup, targetMeshTransform, ModRenderVertListOfMod, OnSelectBonesForAutoRig);
				}
				else
				{	
					Editor.Controller.SetBoneAutoRig();
					//Editor.Controller.SetBoneAutoRig_Old();
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);


			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightToolBtn));
			GUILayout.Space(5);
			//" Grow"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_Grow), 
											1, Editor.GetUIWord(UIWORD.Grow), 
											false, isBtnAvailable, width2Btn, heightToolBtn, 
											apStringFactory.I.RiggingTooltip_Grow))//"Select more of the surrounding vertices"
			{
				Editor.Controller.SelectVertexRigGrowOrShrink(true);
			}
			//" Shrink"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Rig_Shrink), 
											1, Editor.GetUIWord(UIWORD.Shrink), 
											false, isBtnAvailable, width2Btn, heightToolBtn, 
											apStringFactory.I.RiggingTooltip_Shrink))//"Reduce selected vertices"
			{
				Editor.Controller.SelectVertexRigGrowOrShrink(false);
			}
			EditorGUILayout.EndHorizontal();

			//추가 19.7.25 : 현재 선택된 본에 리깅된 모든 버텍스들을 선택하기
			//"Select Vertices of the Bone"
			if(apEditorUtil.ToggledButton_Ctrl(	Editor.GetUIWord(UIWORD.SelectVerticesOfTheBone), 
												false, isBtnAvailable && Bone != null, width, heightToolBtn, 
												apStringFactory.I.RiggingTooltip_SelectVerticesOfBone, //"Select vertices connected to the current bone. Hold down the Ctrl(or Command) key and press the button to select with existing vertices."
												Event.current.control, Event.current.command))
			{
				Editor.Controller.SelectVerticesOfTheBone();
			}
			//Editor.GetUIWord(UIWORD.AddToRigging)

			bool isCopyAvailable = isBtnAvailable && selectedVerts.Count == 1;
			
			bool isPasteAvailable = false;
			if (isCopyAvailable)
			{
				if (apSnapShotManager.I.IsPastable(selectedVerts[0]._modVertRig))
				{
					isPasteAvailable = true;
				}
			}

			bool isPosCopyAvailable = isBtnAvailable && selectedVerts.Count >= 1;
			bool isPosPasteAvailable = false;
			if(isPosCopyAvailable)
			{
				if (apSnapShotManager.I.IsRiggingPosPastable(MeshGroup, selectedVerts))
				{
					isPosPasteAvailable = true;
				}
			}

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightToolBtn));
			GUILayout.Space(5);
			//" Copy"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy), 
											1, Editor.GetUIWord(UIWORD.Copy), 
											false, isCopyAvailable, width2Btn, heightToolBtn))
			{
				//Copy	
				apSnapShotManager.I.Copy_VertRig(selectedVerts[0]._modVertRig, "Mod Vert Rig");
			}
			//" Paste"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Edit_Paste), 
											1, Editor.GetUIWord(UIWORD.Paste), 
											false, isPasteAvailable, width2Btn, heightToolBtn))
			{
				apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_RiggingWeightChanged, Editor, Modifier, Modifier, false);
				//Paste
				if (apSnapShotManager.I.Paste_VertRig(selectedVerts[0]._modVertRig))
				{
					MeshGroup.RefreshForce();
				}
			}
			EditorGUILayout.EndHorizontal();

			//추가 19.7.25 : Pos-Copy / Pos-Paste
			EditorGUILayout.BeginHorizontal(	apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightToolBtn));
			GUILayout.Space(5);
			
			//" Pos-Copy"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy), 
											1, Editor.GetUIWord(UIWORD.PosCopy), 
											false, isPosCopyAvailable, width2Btn, heightToolBtn))
			{
				//Pos-Copy	
				if(ModMeshOfMod != null && ModMeshOfMod._renderUnit != null)
				{
					ModMeshOfMod._renderUnit.CalculateWorldPositionWithoutModifier();
					apSnapShotManager.I.Copy_MultipleVertRig(MeshGroup, selectedVerts);
				}
			}
			
			//" Pos-Paste"
			if (apEditorUtil.ToggledButton(	Editor.ImageSet.Get(apImageSet.PRESET.Edit_Paste), 
											1, Editor.GetUIWord(UIWORD.PosPaste), 
											false, isPosPasteAvailable, width2Btn, heightToolBtn))
			{
				//Pos-Paste	
				if (ModMeshOfMod != null && ModMeshOfMod._renderUnit != null)
				{
					ModMeshOfMod._renderUnit.CalculateWorldPositionWithoutModifier();
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_RiggingWeightChanged, Editor, Modifier, Modifier, false);

					if (apSnapShotManager.I.Paste_MultipleVertRig(MeshGroup, selectedVerts))
					{
						MeshGroup.RefreshForce();
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			//GUILayout.Space(5);
			//apEditorUtil.GUI_DelimeterBoxH(width);
			//GUILayout.Space(7);

			//이제 리스트를 불러오자
			// >> 20.3.29 : 위쪽으로 이동한다.
			//int rigListHeight = 150;//200 > 150
			//int nRigDataList = _rigEdit_vertRigDataList.Count;
			//if (_riggingModifier_prevNumBoneWeights != nRigDataList)
			//{
			//	Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rig_Mod__RigDataCount_Refreshed, true);//"Rig Mod - RigDataCount Refreshed"
			//	if (Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Rig_Mod__RigDataCount_Refreshed))//"Rig Mod - RigDataCount Refreshed"
			//	{
			//		_riggingModifier_prevNumBoneWeights = nRigDataList;
			//	}
			//}
			//else
			//{
			//	Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Rig_Mod__RigDataCount_Refreshed, false);//"Rig Mod - RigDataCount Refreshed"
			//}

			//EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(rigListHeight));
			//GUILayout.Space(5);

			//Rect lastRect = GUILayoutUtility.GetLastRect();

			//GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);

			//GUI.Box(new Rect(lastRect.x + 5, lastRect.y, width, rigListHeight), apStringFactory.I.None);
			//GUI.backgroundColor = prevColor;


			////Weight 리스트를 출력하자
			//EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(rigListHeight));
			//_scrollBottom_Status = EditorGUILayout.BeginScrollView(_scrollBottom_Status, false, true);
			//GUILayout.Space(2);
			//int scrollWidth = width - (30);
			//EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(scrollWidth), apGUILOFactory.I.Height(rigListHeight));
			//GUILayout.Space(3);

			//Texture2D imgRemove = Editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey);

			//VertRigData vertRigData = null;

			//if (_guiContent_RiggingBoneWeightLabel == null)
			//{
			//	_guiContent_RiggingBoneWeightLabel = new apGUIContentWrapper();
			//}
			//if(_guiContent_RiggingBoneWeightBoneName == null)
			//{
			//	_guiContent_RiggingBoneWeightBoneName = new apGUIContentWrapper();
			//}

			////string strLabel = "";

			//VertRigData removeRigData = null;
			//int widthLabel_Name = scrollWidth - (5 + 25 + 14 + 2 + 60);

			////GUIStyle guiStyle_RigIcon_Normal = apEditorUtil.WhiteGUIStyle_Box;
			//if (_guiStyle_RigIcon_Lock == null)
			//{
			//	_guiStyle_RigIcon_Lock = new GUIStyle(GUI.skin.box);//<<최적화된 코드
			//	_guiStyle_RigIcon_Lock.normal.background = Editor.ImageSet.Get(apImageSet.PRESET.Rig_Lock16px);
			//}
			

			//GUIStyle curGUIStyle = null;//<<최적화된 코드

			////for (int i = 0; i < _rigEdit_vertRigDataList.Count; i++)
			//for (int i = 0; i < _riggingModifier_prevNumBoneWeights; i++)
			//{
			//	if (i < _rigEdit_vertRigDataList.Count)
			//	{
			//		//GUIStyle curGUIStyle = guiNone;
			//		vertRigData = _rigEdit_vertRigDataList[i];
			//		if (vertRigData._bone == Bone)
			//		{
			//			lastRect = GUILayoutUtility.GetLastRect();

			//			if (EditorGUIUtility.isProSkin)
			//			{
			//				GUI.backgroundColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
			//			}
			//			else
			//			{
			//				GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);
			//			}

			//			int offsetHeight = 18 + 3;
			//			if (i == 0)
			//			{
			//				offsetHeight = 1 + 3;
			//			}

			//			GUI.Box(new Rect(lastRect.x, lastRect.y + offsetHeight, scrollWidth + 35, 20), apStringFactory.I.None);
			//			GUI.backgroundColor = prevColor;

			//			//curGUIStyle = guiSelected;
			//			curGUIStyle = apGUIStyleWrapper.I.None_MiddleLeft_White2Cyan;
			//		}
			//		else
			//		{
			//			curGUIStyle = apGUIStyleWrapper.I.None_MiddleLeft_LabelColor;
			//		}


			//		EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
			//		GUILayout.Space(5);

			//		//Bone의 색상, 이름, Weight, X를 출력
					

			//		if(vertRigData._bone != null && vertRigData._bone._isRigLock)
			//		{
			//			GUILayout.Box(apStringFactory.I.None, _guiStyle_RigIcon_Lock, apGUILOFactory.I.Width(14), apGUILOFactory.I.Height(14));//자물쇠 이미지
			//		}
			//		else
			//		{
			//			GUI.backgroundColor = vertRigData._bone._color;
			//			GUILayout.Box(apStringFactory.I.None, apEditorUtil.WhiteGUIStyle_Box, apGUILOFactory.I.Width(14), apGUILOFactory.I.Height(14));//일반 박스 이미지
			//			GUI.backgroundColor = prevColor;
			//		}

			//		_guiContent_RiggingBoneWeightLabel.ClearText(false);
					

			//		if (nSelectedVerts > 1 && (vertRigData._weight_Max - vertRigData._weight_Min) > 0.01f)
			//		{
			//			//여러개가 섞여서 Weight가 의미가 없어졌다.
			//			//Min + Max로 표현하자
			//			//strLabel = string.Format("{0:N2}~{1:N2}", vertRigData._weight_Min, vertRigData._weight_Max);
			//			_guiContent_RiggingBoneWeightLabel.AppendText(string.Format("{0:N2}~{1:N2}", vertRigData._weight_Min, vertRigData._weight_Max), true);
						
			//		}
			//		else
			//		{
			//			//Weight를 출력한다.
			//			//strLabel = ((int)vertRigData._weight) + "." + ((int)(vertRigData._weight * 1000.0f + 0.5f) % 1000);
			//			//strLabel = string.Format("{0:N3}", vertRigData._weight);
			//			_guiContent_RiggingBoneWeightLabel.AppendText(string.Format("{0:N3}", vertRigData._weight), true);
			//		}

			//		//이전
			//		//string rigName = vertRigData._bone._name;
			//		//if (rigName.Length > 14)
			//		//{
			//		//	rigName = rigName.Substring(0, 12) + "..";
			//		//}

			//		//변경
			//		_guiContent_RiggingBoneWeightBoneName.ClearText(false);
			//		if(vertRigData._bone._name.Length > 14)
			//		{
			//			_guiContent_RiggingBoneWeightBoneName.AppendText(vertRigData._bone._name.Substring(0, 12), false);
			//			_guiContent_RiggingBoneWeightBoneName.AppendText(apStringFactory.I.Dot2, true);
			//		}
			//		else
			//		{
			//			_guiContent_RiggingBoneWeightBoneName.AppendText(vertRigData._bone._name, true);
			//		}

			//		if (GUILayout.Button(_guiContent_RiggingBoneWeightBoneName.Content,
			//							curGUIStyle,
			//							apGUILOFactory.I.Width(widthLabel_Name), apGUILOFactory.I.Height(20)))
			//		{
			//			Editor.Select.SetBone(vertRigData._bone);
			//		}
			//		if (GUILayout.Button(_guiContent_RiggingBoneWeightLabel.Content,
			//							curGUIStyle,
			//							apGUILOFactory.I.Width(60), apGUILOFactory.I.Height(20)))
			//		{
			//			Editor.Select.SetBone(vertRigData._bone);
			//		}

			//		if (GUILayout.Button(imgRemove, curGUIStyle, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
			//		{
			//			//Debug.LogError("TODO : Bone Remove From Rigging");
			//			removeRigData = vertRigData;
			//		}

			//		EditorGUILayout.EndHorizontal();
			//	}
			//	else
			//	{
			//		//GUI 렌더 문제로 더미 렌더링
			//		EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(scrollWidth - 5));
			//		GUILayout.Space(5);

			//		GUILayout.Box(apStringFactory.I.None, apGUILOFactory.I.Width(14), apGUILOFactory.I.Height(14));

			//		if (GUILayout.Button(apStringFactory.I.None,
			//							apGUIStyleWrapper.I.None_MiddleLeft_LabelColor,
			//							apGUILOFactory.I.Width(widthLabel_Name), apGUILOFactory.I.Height(20)))
			//		{
			//			//Dummy
			//		}
			//		if (GUILayout.Button(apStringFactory.I.None,
			//							apGUIStyleWrapper.I.None_MiddleLeft_LabelColor,
			//							apGUILOFactory.I.Width(60), apGUILOFactory.I.Height(20)))
			//		{
			//			//Dummy
			//		}

			//		if (GUILayout.Button(imgRemove, apGUIStyleWrapper.I.None_MiddleLeft_LabelColor, apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
			//		{
			//			//Debug.LogError("TODO : Bone Remove From Rigging");
			//			//removeRigData = vertRigData;
			//			//Dummy
			//		}


			//		EditorGUILayout.EndHorizontal();
			//	}
			//}
			//EditorGUILayout.EndVertical();

			//GUILayout.Space(120);
			//EditorGUILayout.EndScrollView();
			//EditorGUILayout.EndVertical();
			//EditorGUILayout.EndHorizontal();

			if(selectRigData != null && selectRigData._bone != null)
			{
				Editor.Select.SetBone(selectRigData._bone);
			}
			else if (removeRigData != null)
			{
				Editor.Controller.RemoveVertRigData(selectedVerts, removeRigData._bone);
			}

			
		}

		/// <summary>
		/// RigDataList를 바탕으로 설정 가능한 "최대의 공통 Weight"를 리턴한다.
		/// </summary>
		/// <param name="curRigData"></param>
		/// <returns></returns>
		private float GetMaxRigWeight(VertRigData curRigData)
		{
			if(_rigEdit_vertRigDataList.Count == 0 || curRigData == null)
			{
				return 1.0f;
			}

			VertRigData rigData = null;
			float lockedWeight = 0.0f;
			for (int i = 0; i < _rigEdit_vertRigDataList.Count; i++)
			{
				rigData = _rigEdit_vertRigDataList[i];
				if(rigData == curRigData)
				{
					continue;
				}
				else if(rigData._bone != null && rigData._bone._isRigLock)
				{
					//RigLock이 켜진 Bone을 찾는다.
					if(rigData._nRig == 1)
					{
						lockedWeight += rigData._weight;
					}
					else
					{
						lockedWeight += rigData._weight_Max;
					}
				}
			}
			
			return Mathf.Clamp01(1.0f - lockedWeight);
		}



		//Rigging의 가중치 증감 단축키들
		private void OnHotKeyEvent_RiggingValueChanged_05(object paramObject)
		{
			if(!(paramObject is bool)) { return; }
			bool isIncrease = (bool)paramObject;

			if(Bone == null) { return; }
			int CALCULATE_ADD = 1;

			if(isIncrease)	{ Editor.Controller.SetBoneWeight(0.05f, CALCULATE_ADD); }
			else			{ Editor.Controller.SetBoneWeight(-0.05f, CALCULATE_ADD); }

			apEditorUtil.ReleaseGUIFocus();
		}


		private void OnHotKeyEvent_RiggingValueChanged_02(object paramObject)
		{
			if(!(paramObject is bool)) { return; }
			bool isIncrease = (bool)paramObject;

			if(Bone == null) { return; }
			int CALCULATE_ADD = 1;

			if(isIncrease)	{ Editor.Controller.SetBoneWeight(0.02f, CALCULATE_ADD); }
			else			{ Editor.Controller.SetBoneWeight(-0.02f, CALCULATE_ADD); }

			apEditorUtil.ReleaseGUIFocus();
		}

		//Rigging의 브러시 모드에서의 단축키들
		private void OnHotKeyEvent_RiggingBrushSizeChanged(object paramObject)
		{
			if(!(paramObject is bool)) { return; }
			bool isSizeUp = (bool)paramObject;

			if (isSizeUp)	{ _rigEdit_BrushRadius = Mathf.Clamp(_rigEdit_BrushRadius + 10, 1, apGizmos.MAX_BRUSH_RADIUS); }
			else			{ _rigEdit_BrushRadius = Mathf.Clamp(_rigEdit_BrushRadius - 10, 1, apGizmos.MAX_BRUSH_RADIUS); }
			apEditorUtil.ReleaseGUIFocus();
		}
		
		private void OnHotKeyEvent_RiggingBrushMode_Add(object paramObject)
		{
			if(_rigEdit_BrushToolMode != RIGGING_BRUSH_TOOL_MODE.Add)	{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.Add; Editor.Gizmos.StartBrush(); }
			else														{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None; Editor.Gizmos.EndBrush(); }
			apEditorUtil.ReleaseGUIFocus();
		}
		private void OnHotKeyEvent_RiggingBrushMode_Multiply(object paramObject)
		{
			if(_rigEdit_BrushToolMode != RIGGING_BRUSH_TOOL_MODE.Multiply)	{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.Multiply; Editor.Gizmos.StartBrush(); }
			else														{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None; Editor.Gizmos.EndBrush(); }
			apEditorUtil.ReleaseGUIFocus();
		}
		private void OnHotKeyEvent_RiggingBrushMode_Blur(object paramObject)
		{
			if(_rigEdit_BrushToolMode != RIGGING_BRUSH_TOOL_MODE.Blur)	{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.Blur; Editor.Gizmos.StartBrush(); }
			else														{ _rigEdit_BrushToolMode = RIGGING_BRUSH_TOOL_MODE.None; Editor.Gizmos.EndBrush(); }
			apEditorUtil.ReleaseGUIFocus();
		}

		private void OnHotKeyEvent_RiggingBrushIntensity(object paramObject)
		{
			if(!(paramObject is bool)) { return; }
			bool isIncrease = (bool)paramObject;

			switch (_rigEdit_BrushToolMode)
			{
				case RIGGING_BRUSH_TOOL_MODE.Add:
					if(isIncrease)	{ _rigEdit_BrushIntensity_Add = Mathf.Clamp(_rigEdit_BrushIntensity_Add + 0.1f, -1, 1); }
					else			{ _rigEdit_BrushIntensity_Add = Mathf.Clamp(_rigEdit_BrushIntensity_Add - 0.1f, -1, 1); }
					break;
				case RIGGING_BRUSH_TOOL_MODE.Multiply:
					if(isIncrease)	{ _rigEdit_BrushIntensity_Multiply = Mathf.Clamp(_rigEdit_BrushIntensity_Multiply + 0.05f, 0.5f, 1.5f); }
					else			{ _rigEdit_BrushIntensity_Multiply = Mathf.Clamp(_rigEdit_BrushIntensity_Multiply - 0.05f, 0.5f, 1.5f); }
					break;
				case RIGGING_BRUSH_TOOL_MODE.Blur:
					if(isIncrease)	{ _rigEdit_BrushIntensity_Blur = Mathf.Clamp(_rigEdit_BrushIntensity_Blur + 10, 0, 100); }
					else			{ _rigEdit_BrushIntensity_Blur = Mathf.Clamp(_rigEdit_BrushIntensity_Blur - 10, 0, 100); }
					break;
			}
			apEditorUtil.ReleaseGUIFocus();
		}


		private void OnSelectBonesForAutoRig(bool isSuccess, object loadKey, List<apBone> selectedBones)
		{
			//TODO
			if(!isSuccess || _loadKey_SelectBonesForAutoRig != loadKey)
			{
				_loadKey_SelectBonesForAutoRig = null;
				return;
			}
			_loadKey_SelectBonesForAutoRig = null;
			if(selectedBones != null && selectedBones.Count > 0)
			{
				Editor.Controller.SetBoneAutoRig(selectedBones);
			}
			
		}


		//Physics 모디파이어의 설정 화면
		

		private void DrawModifierPropertyGUI_Physics(int width, int height)
		{
			//"Target Mesh Transform"
			//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.TargetMeshTransform), apGUILOFactory.I.Width(width));
			//1. Mesh Transform 등록 체크
			//2. Weight 툴
			//3. Mesh Physics 툴

			bool isTarget_MeshTransform = Modifier.IsTarget_MeshTransform;
			bool isTarget_ChildMeshTransform = Modifier.IsTarget_ChildMeshTransform;

			bool isContainInParamSetGroup = false;
			
			
			//string strTargetName = "";
			bool isTargetName = false;

			if(_guiContent_ModProp_ParamSetTarget_Name == null)
			{
				_guiContent_ModProp_ParamSetTarget_Name = new apGUIContentWrapper();
			}
			if(_guiContent_ModProp_ParamSetTarget_StatusText == null)
			{
				_guiContent_ModProp_ParamSetTarget_StatusText = new apGUIContentWrapper();
			}
			_guiContent_ModProp_ParamSetTarget_Name.ClearText(false);
			_guiContent_ModProp_ParamSetTarget_StatusText.ClearText(false);


			object selectedObj = null;
			bool isAnyTargetSelected = false;
			bool isAddable = false;

			apTransform_Mesh targetMeshTransform = SubMeshInGroup;
			apModifierParamSetGroup paramSetGroup = SubEditedParamSetGroup;
			if (paramSetGroup == null)
			{
				//? Physics에서는 1개의 ParamSetGroup이 있어야 한다.
				Editor.Controller.AddStaticParamSetGroupToModifier();

				if (Modifier._paramSetGroup_controller.Count > 0)
				{
					SetParamSetGroupOfModifier(Modifier._paramSetGroup_controller[0]);
				}
				paramSetGroup = SubEditedParamSetGroup;
				if (paramSetGroup == null)
				{
					Debug.LogError("AnyPortrait : ParamSet Group Is Null (" + Modifier._paramSetGroup_controller.Count + ")");
					return;
				}

				AutoSelectModMeshOrModBone();
			}

			apModifierParamSet paramSet = ParamSetOfMod;
			if (paramSet == null)
			{
				//Rigging에서는 1개의 ParamSetGroup과 1개의 ParamSet이 있어야 한다.
				//선택된게 없다면, ParamSet이 1개 있는지 확인
				//그후 선택한다.

				if (paramSetGroup._paramSetList.Count == 0)
				{
					paramSet = new apModifierParamSet();
					paramSet.LinkParamSetGroup(paramSetGroup);
					paramSetGroup._paramSetList.Add(paramSet);
				}
				else
				{
					paramSet = paramSetGroup._paramSetList[0];
				}
				SetParamSetOfModifier(paramSet);
			}

			//1. Mesh Transform 등록 체크
			if (targetMeshTransform != null)
			{
				apRenderUnit targetRenderUnit = null;
				//Child Mesh를 허용하는가
				if (isTarget_ChildMeshTransform)
				{
					//Child를 허용한다.
					targetRenderUnit = MeshGroup.GetRenderUnit(targetMeshTransform);
				}
				else
				{
					//Child를 허용하지 않는다.
					targetRenderUnit = MeshGroup.GetRenderUnit_NoRecursive(targetMeshTransform);
				}
				if (targetRenderUnit != null)
				{
					//유효한 선택인 경우
					isContainInParamSetGroup = paramSetGroup.IsMeshTransformContain(targetMeshTransform);
					isAnyTargetSelected = true;

					//strTargetName = targetMeshTransform._nickName;
					_guiContent_ModProp_ParamSetTarget_Name.AppendText(targetMeshTransform._nickName, true);
					isTargetName = true;


					selectedObj = targetMeshTransform;

					isAddable = true;
				}
			}

			//대상이 없다면
			if(!isTargetName)
			{
				_guiContent_ModProp_ParamSetTarget_Name.AppendText(apStringFactory.I.None, true);
			}

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check__Physic__Valid, targetMeshTransform != null);//"Modifier_Add Transform Check [Physic] Valid"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check__Physic__Invalid, targetMeshTransform == null);//"Modifier_Add Transform Check [Physic] Invalid"


			bool isMeshTransformValid = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check__Physic__Valid);//"Modifier_Add Transform Check [Physic] Valid"
			bool isMeshTransformInvalid = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_Add_Transform_Check__Physic__Invalid);//"Modifier_Add Transform Check [Physic] Invalid"

			
			bool isDummyTransform = false;

			if (!isMeshTransformValid && !isMeshTransformInvalid)
			{
				//둘중 하나는 true여야 GUI를 그릴 수 있다.
				isDummyTransform = true;//<<더미로 출력해야한다...
			}
			else
			{
				_physicModifier_prevSelectedTransform = targetMeshTransform;
				_physicModifier_prevIsContained = isContainInParamSetGroup;
			}



			Color prevColor = GUI.backgroundColor;

			//GUIStyle boxGUIStyle = new GUIStyle(GUI.skin.box);
			//boxGUIStyle.alignment = TextAnchor.MiddleCenter;
			//boxGUIStyle.normal.textColor = apEditorUtil.BoxTextColor;

			//추가
			if(_guiContent_Modifier_AddToPhysics == null)
			{
				_guiContent_Modifier_AddToPhysics = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AddToPhysics), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToPhysics));
			}
			if(_guiContent_Modifier_RemoveFromPhysics == null)
			{
				_guiContent_Modifier_RemoveFromPhysics = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveFromPhysics), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_RemoveFromPhysics));
			}
			
			

			if (targetMeshTransform == null)
			{
				//선택된 MeshTransform이 없다.
				GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
				//"No Mesh is Selected"
				GUILayout.Box(Editor.GetUIWord(UIWORD.NoMeshIsSelected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;

				if (isDummyTransform)
				{
					//"  Add Physics"
					//이전
					//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddToPhysics), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToPhysics)), GUILayout.Width(width), GUILayout.Height(25)))
					
					//변경
					if (GUILayout.Button(_guiContent_Modifier_AddToPhysics.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))
					{
						//더미용 버튼
					}
				}
			}
			else if (isContainInParamSetGroup)
			{
				GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);

				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.Selected), true);

				//"[" + strTargetName + "]\nSelected"
				//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.Selected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
				GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));


				GUI.backgroundColor = prevColor;

				if (!isDummyTransform)
				{
					//더미 처리 중이 아닐때 버튼이 등장한다
					//"  Remove From Physics".
					//이전
					//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveFromPhysics), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_RemoveFromPhysics)), GUILayout.Width(width), GUILayout.Height(30)))
					
					//변경
					if (GUILayout.Button(_guiContent_Modifier_RemoveFromPhysics.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
					{

						//bool result = EditorUtility.DisplayDialog("Remove From Physics", "Remove From Physics [" + strTargetName + "]", "Remove", "Cancel");

						bool result = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveFromPhysics_Title),
																Editor.GetTextFormat(TEXT.RemoveFromPhysics_Body, _guiContent_ModProp_ParamSetTarget_Name.Content.text),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);

						if (result)
						{
							object targetObj = SubMeshInGroup;
							if (SubMeshGroupInGroup != null && selectedObj == SubMeshGroupInGroup)
							{
								targetObj = SubMeshGroupInGroup;
							}

							apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_RemovePhysics, Editor, Modifier, targetObj, false);

							if (SubMeshInGroup != null && selectedObj == SubMeshInGroup)
							{
								SubEditedParamSetGroup.RemoveModifierMeshes(SubMeshInGroup);
								SetModMeshOfModifier(null);
							}
							else if (SubMeshGroupInGroup != null && selectedObj == SubMeshGroupInGroup)
							{
								SubEditedParamSetGroup.RemoveModifierMeshes(SubMeshGroupInGroup);
								SetModMeshOfModifier(null);

							}



							if (MeshGroup != null)
							{
								MeshGroup.RefreshModifierLink(apUtil.LinkRefresh.Set_MeshGroup_Modifier(MeshGroup, Modifier));
							}

							SetSubMeshGroupInGroup(null);
							SetSubMeshInGroup(null);

							Editor._portrait.LinkAndRefreshInEditor(false, apUtil.LinkRefresh.Set_MeshGroup_Modifier(MeshGroup, Modifier));
							AutoSelectModMeshOrModBone();

							SetModifierExclusiveEditing(EX_EDIT.None);

							if (ModMeshOfMod != null)
							{
								ModMeshOfMod.RefreshVertexWeights(Editor._portrait, true, false);
							}

							Editor.Hierarchy_MeshGroup.RefreshUnits();
							Editor.RefreshControllerAndHierarchy(false);

							Editor.SetRepaint();

							isContainInParamSetGroup = false;
						}
					}
				}
			}
			else if (!isAddable)
			{
				//추가 가능하지 않다.

				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.NotAbleToBeAdded), true);


				GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				//"[" + strTargetName + "]\nNot able to be Added"
				//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.NotAbleToBeAdded), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
				GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;

				if (isDummyTransform)
				{
					//이전
					//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddToPhysics), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToPhysics)), GUILayout.Width(width), GUILayout.Height(25)))
					//변경
					if (GUILayout.Button(_guiContent_Modifier_AddToPhysics.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25)))
					{
						//더미용 버튼
					}
				}
			}
			else
			{
				//아직 추가하지 않았다. 추가하자

				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_L, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(_guiContent_ModProp_ParamSetTarget_Name.Content.text, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Bracket_2_R, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(apStringFactory.I.Return, false);
				_guiContent_ModProp_ParamSetTarget_StatusText.AppendText(Editor.GetUIWord(UIWORD.NotAddedtoEdit), true);


				GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				//"[" + strTargetName + "]\nNot Added to Edit"
				//GUILayout.Box("[" + strTargetName + "]\n" + Editor.GetUIWord(UIWORD.NotAddedtoEdit), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
				GUILayout.Box(_guiContent_ModProp_ParamSetTarget_StatusText.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

				GUI.backgroundColor = prevColor;

				if (!isDummyTransform)
				{
					//더미 처리 중이 아닐때 버튼이 등장한다.
					//이전
					//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddToPhysics), Editor.ImageSet.Get(apImageSet.PRESET.Modifier_AddToPhysics)), GUILayout.Width(width), GUILayout.Height(30)))
					
					//변경
					if (GUILayout.Button(_guiContent_Modifier_AddToPhysics.Content, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30)))
					{
						Editor.Controller.AddModMesh_WithSubMeshOrSubMeshGroup();

						Editor.Hierarchy_MeshGroup.RefreshUnits();

						//추가 3.24 : Physics에 등록했다면, Edit모드 시작
						if (ExEditingMode == EX_EDIT.None)
						{
							SetModifierExclusiveEditing(EX_EDIT.ExOnly_Edit);
						}

						Editor.SetRepaint();
					}
				}
			}
			GUI.backgroundColor = prevColor;

			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);

			List<ModRenderVert> selectedVerts = Editor.Select.ModRenderVertListOfMod;
			//bool isAnyVertSelected = (selectedVerts != null && selectedVerts.Count > 0);

			bool isExEditMode = ExEditingMode != EX_EDIT.None;

			//2. Weight 툴
			// 선택한 Vertex
			// Set Weight, +/- Weight, * Weight
			// Blend
			// Grow, Shrink

			//어떤 Vertex가 선택되었는지 표기한다.
			if (!isAnyTargetSelected || selectedVerts.Count == 0)
			{
				//선택된게 없다.
				GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
				//"No Vetex is Selected"
				GUILayout.Box(Editor.GetUIWord(UIWORD.NoVertexisSelected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));

				GUI.backgroundColor = prevColor;


			}
			else if (selectedVerts.Count == 1)
			{
				//1개의 Vertex
				GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				//"[Vertex " + selectedVerts[0]._renderVert._vertex._index + "] : " + selectedVerts[0]._modVertWeight._weight
				GUILayout.Box(string.Format("[ {0} {1} ] : {2}", Editor.GetUIWord(UIWORD.Vertex), selectedVerts[0]._renderVert._vertex._index, selectedVerts[0]._modVertWeight._weight), 
								apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));

				GUI.backgroundColor = prevColor;

			}
			else
			{
				GUI.backgroundColor = new Color(0.4f, 1.0f, 1.0f, 1.0f);
				//selectedVerts.Count + " Verts are Selected"
				GUILayout.Box(Editor.GetUIWordFormat(UIWORD.NumVertsareSelected, selectedVerts.Count), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));

				GUI.backgroundColor = prevColor;
			}
			int nSelectedVerts = selectedVerts.Count;

			bool isMainVert = false;
			bool isMainVertSwitchable = false;
			if (nSelectedVerts == 1)
			{
				if (selectedVerts[0]._modVertWeight._isEnabled)
				{
					isMainVert = selectedVerts[0]._modVertWeight._physicParam._isMain;
					isMainVertSwitchable = true;
				}
			}
			else if (nSelectedVerts > 1)
			{
				//전부다 MainVert인가
				bool isAllMainVert = true;
				for (int iVert = 0; iVert < selectedVerts.Count; iVert++)
				{
					if (!selectedVerts[iVert]._modVertWeight._physicParam._isMain)
					{
						isAllMainVert = false;
						break;
					}
				}
				isMainVert = isAllMainVert;
				isMainVertSwitchable = true;
			}

			//>> 여기서부터 하자


			//" Important Vertex", " Set Important",
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Physic_SetMainVertex),
												1, Editor.GetUIWord(UIWORD.ImportantVertex), Editor.GetUIWord(UIWORD.SetImportant),
												isMainVert, isMainVertSwitchable && isExEditMode, width, 25,
												"Force calculation is performed based on the [Important Vertex]"))
			{
				if (isMainVertSwitchable)
				{
					for (int i = 0; i < selectedVerts.Count; i++)
					{
						selectedVerts[i]._modVertWeight._physicParam._isMain = !isMainVert;
					}

					ModMeshOfMod.RefreshVertexWeights(Editor._portrait, true, false);
				}
			}

			//Weight Tool
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
			GUILayout.Space(5);
			//고정된 Weight 값
			//0, 0.1, 0.3, 0.5, 0.7, 0.9, 1 (7개)
			int CALCULATE_SET = 0;
			int CALCULATE_ADD = 1;
			int CALCULATE_MULTIPLY = 2;

			int widthPresetWeight = ((width - 2 * 7) / 7) - 2;
			bool isPresetAdapt = false;
			float presetWeight = 0.0f;

			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_00, false, isExEditMode, widthPresetWeight, 30))//"0"
			{
				isPresetAdapt = true;
				presetWeight = 0.0f;
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_01, false, isExEditMode, widthPresetWeight, 30))//".1"
			{
				isPresetAdapt = true;
				presetWeight = 0.1f;
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_03, false, isExEditMode, widthPresetWeight, 30))//".3"
			{
				isPresetAdapt = true;
				presetWeight = 0.3f;
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_05, false, isExEditMode, widthPresetWeight, 30))//".5"
			{
				isPresetAdapt = true;
				presetWeight = 0.5f;
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_07, false, isExEditMode, widthPresetWeight, 30))//".7"
			{
				isPresetAdapt = true;
				presetWeight = 0.7f;
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_09, false, isExEditMode, widthPresetWeight, 30))//".9"
			{
				isPresetAdapt = true;
				presetWeight = 0.9f;
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Weight_10, false, isExEditMode, widthPresetWeight, 30))//"1"
			{
				isPresetAdapt = true;
				presetWeight = 1f;
			}
			EditorGUILayout.EndHorizontal();

			if (isPresetAdapt)
			{
				//고정 Weight를 지정하자
				Editor.Controller.SetPhyVolWeight(presetWeight, CALCULATE_SET);
				isPresetAdapt = false;
			}



			int heightSetWeight = 25;
			int widthSetBtn = 90;
			int widthIncDecBtn = 30;
			int widthValue = width - (widthSetBtn + widthIncDecBtn * 2 + 2 * 5 + 5);

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightSetWeight));
			GUILayout.Space(5);

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(widthValue), apGUILOFactory.I.Height(heightSetWeight - 2));
			GUILayout.Space(8);
			_physics_setWeightValue = EditorGUILayout.DelayedFloatField(_physics_setWeightValue);
			EditorGUILayout.EndVertical();
			
			//"Set Weight"
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.SetWeight), false, isExEditMode, widthSetBtn, heightSetWeight))
			{
				Editor.Controller.SetPhyVolWeight(_physics_setWeightValue, CALCULATE_SET);
				GUI.FocusControl(null);
			}

			if (apEditorUtil.ToggledButton(apStringFactory.I.Plus, false, isExEditMode, widthIncDecBtn, heightSetWeight))//"+"
			{
				////0.05 단위로 올라가거나 내려온다. (5%)
				Editor.Controller.SetPhyVolWeight(0.05f, CALCULATE_ADD);

				GUI.FocusControl(null);
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Minus, false, isExEditMode, widthIncDecBtn, heightSetWeight))//"-"
			{
				//0.05 단위로 올라가거나 내려온다. (5%)
				Editor.Controller.SetPhyVolWeight(-0.05f, CALCULATE_ADD);

				GUI.FocusControl(null);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(3);

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightSetWeight));
			GUILayout.Space(5);


			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(widthValue), apGUILOFactory.I.Height(heightSetWeight - 2));
			GUILayout.Space(8);
			_physics_scaleWeightValue = EditorGUILayout.DelayedFloatField(_physics_scaleWeightValue);
			EditorGUILayout.EndVertical();

			//"Scale Weight"
			if (apEditorUtil.ToggledButton(Editor.GetUIWord(UIWORD.ScaleWeight), false, isExEditMode, widthSetBtn, heightSetWeight))
			{
				Editor.Controller.SetPhyVolWeight(_physics_scaleWeightValue, CALCULATE_MULTIPLY);//Multiply 방식
				GUI.FocusControl(null);
			}

			if (apEditorUtil.ToggledButton(apStringFactory.I.Plus, false, isExEditMode, widthIncDecBtn, heightSetWeight))//"+"
			{
				//x1.05
				//Debug.LogError("TODO : Physic Weight 적용 - x1.05");
				Editor.Controller.SetPhyVolWeight(1.05f, CALCULATE_MULTIPLY);//Multiply 방식

				GUI.FocusControl(null);
			}
			if (apEditorUtil.ToggledButton(apStringFactory.I.Minus, false, isExEditMode, widthIncDecBtn, heightSetWeight))//"-"
			{
				//x0.95
				//Debug.LogError("TODO : Physic Weight 적용 - x0.95");
				//Editor.Controller.SetBoneWeight(0.95f, CALCULATE_MULTIPLY);//Multiply 방식
				Editor.Controller.SetPhyVolWeight(0.95f, CALCULATE_MULTIPLY);//Multiply 방식

				GUI.FocusControl(null);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(8);

			int heightToolBtn = 25;
			int width2Btn = (width - 5) / 2;
			
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Blend), 1, Editor.GetUIWord(UIWORD.Blend), false, isExEditMode, width, heightToolBtn,
											apStringFactory.I.RiggingTooltip_Blend))//"The weights of vertices are blended" // 리깅 툴의 툴팁과 동일
			{
				//Blend
				Editor.Controller.SetPhyVolWeightBlend();
			}

			GUILayout.Space(5);

			
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightToolBtn));
			GUILayout.Space(5);
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Grow), 1, Editor.GetUIWord(UIWORD.Grow), false, isExEditMode, width2Btn, heightToolBtn,
											apStringFactory.I.RiggingTooltip_Grow))//"Select more of the surrounding vertices"
			{
				//Grow
				Editor.Controller.SelectVertexWeightGrowOrShrink(true);
			}
			if (apEditorUtil.ToggledButton(Editor.ImageSet.Get(apImageSet.PRESET.Rig_Shrink), 1, Editor.GetUIWord(UIWORD.Shrink), false, isExEditMode, width2Btn, heightToolBtn,
											apStringFactory.I.RiggingTooltip_Shrink))//"Reduce selected vertices"
			{
				//Shrink
				Editor.Controller.SelectVertexWeightGrowOrShrink(false);
			}
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);

			//추가
			//Viscosity를 위한 그룹
			int viscosityGroupID = 0;
			bool isViscosityAvailable = false;
			if (isExEditMode && nSelectedVerts > 0)
			{
				for (int i = 0; i < selectedVerts.Count; i++)
				{
					viscosityGroupID |= selectedVerts[i]._modVertWeight._physicParam._viscosityGroupID;
				}
				isViscosityAvailable = true;
			}
			int iViscosityChanged = -1;
			bool isViscosityAdd = false;

			int heightVisTool = 20;
			int widthVisTool = ((width - 5) / 5) - 2;

			//5줄씩 총 10개 (0은 모두 0으로 만든다.)

			//"Viscosity Group ID"
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.ViscosityGroupID));
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightVisTool));
			GUILayout.Space(5);


			if(_guiContent_PhysicsGroupID_None == null)		{ _guiContent_PhysicsGroupID_None = apGUIContentWrapper.Make(apStringFactory.I.X, false); }
			if(_guiContent_PhysicsGroupID_1 == null)		{ _guiContent_PhysicsGroupID_1 = apGUIContentWrapper.Make(apStringFactory.I.Num1, false); }
			if(_guiContent_PhysicsGroupID_2 == null)		{ _guiContent_PhysicsGroupID_2 = apGUIContentWrapper.Make(apStringFactory.I.Num2, false); }
			if(_guiContent_PhysicsGroupID_3 == null)		{ _guiContent_PhysicsGroupID_3 = apGUIContentWrapper.Make(apStringFactory.I.Num3, false); }
			if(_guiContent_PhysicsGroupID_4 == null)		{ _guiContent_PhysicsGroupID_4 = apGUIContentWrapper.Make(apStringFactory.I.Num4, false); }
			if(_guiContent_PhysicsGroupID_5 == null)		{ _guiContent_PhysicsGroupID_5 = apGUIContentWrapper.Make(apStringFactory.I.Num5, false); }
			if(_guiContent_PhysicsGroupID_6 == null)		{ _guiContent_PhysicsGroupID_6 = apGUIContentWrapper.Make(apStringFactory.I.Num6, false); }
			if(_guiContent_PhysicsGroupID_7 == null)		{ _guiContent_PhysicsGroupID_7 = apGUIContentWrapper.Make(apStringFactory.I.Num7, false); }
			if(_guiContent_PhysicsGroupID_8 == null)		{ _guiContent_PhysicsGroupID_8 = apGUIContentWrapper.Make(apStringFactory.I.Num8, false); }
			if(_guiContent_PhysicsGroupID_9 == null)		{ _guiContent_PhysicsGroupID_9 = apGUIContentWrapper.Make(apStringFactory.I.Num9, false); }
			
			apGUIContentWrapper curGUIContent = null;

			for (int i = 0; i < 10; i++)
			{
				if (i == 5)
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(heightVisTool));
					GUILayout.Space(5);
				}


				//string label = "";
				int iResult = 0;
				//이전
				//switch (i)
				//{
				//	case 0:	label = "X";	iResult = 0;	break;
				//	case 1:	label = "1";	iResult = 1;	break;
				//	case 2:	label = "2";	iResult = 2;	break;
				//	case 3:	label = "3";	iResult = 4;	break;
				//	case 4:	label = "4";	iResult = 8;	break;
				//	case 5:	label = "5";	iResult = 16;	break;
				//	case 6:	label = "6";	iResult = 32;	break;
				//	case 7:	label = "7";	iResult = 64;	break;
				//	case 8:	label = "8";	iResult = 128;	break;
				//	case 9:	label = "9";	iResult = 256;	break;
				//}

				//변경 : string 쓰지 말자
				switch (i)
				{
					case 0:	curGUIContent = _guiContent_PhysicsGroupID_None;	iResult = 0;	break;
					case 1:	curGUIContent = _guiContent_PhysicsGroupID_1;	iResult = 1;	break;
					case 2:	curGUIContent = _guiContent_PhysicsGroupID_2;	iResult = 2;	break;
					case 3:	curGUIContent = _guiContent_PhysicsGroupID_3;	iResult = 4;	break;
					case 4:	curGUIContent = _guiContent_PhysicsGroupID_4;	iResult = 8;	break;
					case 5:	curGUIContent = _guiContent_PhysicsGroupID_5;	iResult = 16;	break;
					case 6:	curGUIContent = _guiContent_PhysicsGroupID_6;	iResult = 32;	break;
					case 7:	curGUIContent = _guiContent_PhysicsGroupID_7;	iResult = 64;	break;
					case 8:	curGUIContent = _guiContent_PhysicsGroupID_8;	iResult = 128;	break;
					case 9:	curGUIContent = _guiContent_PhysicsGroupID_9;	iResult = 256;	break;
				}

				bool isSelected = (viscosityGroupID & iResult) != 0;
				if (apEditorUtil.ToggledButton_2Side(curGUIContent.Content.text, curGUIContent.Content.text, isSelected, isViscosityAvailable, widthVisTool, heightVisTool))
				{
					iViscosityChanged = iResult;
					isViscosityAdd = !isSelected;
				}
			}
			EditorGUILayout.EndHorizontal();

			if (iViscosityChanged > -1)
			{
				Editor.Controller.SetPhysicsViscostyGroupID(iViscosityChanged, isViscosityAdd);
			}



			

			//메시 설정
			apPhysicsMeshParam physicMeshParam = null;
			if (ModMeshOfMod != null && ModMeshOfMod.PhysicParam != null)
			{
				physicMeshParam = ModMeshOfMod.PhysicParam;
			}

			//물리 프리셋을 출력할 수 있는가
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_PhysicsPreset_Valid, ModMeshOfMod != null);
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_PhysicsPreset_Invalid, ModMeshOfMod == null);
			bool isDraw_PhysicsPreset = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_PhysicsPreset_Valid)
										|| Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.Modifier_PhysicsPreset_Invalid);

			if (!isDraw_PhysicsPreset)
			{
				return;
			}

			//더미를 다시 정의하자
			//위에서는 추가/삭제의 MeshTransform
			//여기서부터는 ModMesh의 존재
			isDummyTransform = false;
			if(ModMeshOfMod == null || physicMeshParam == null)
			{
				isDummyTransform = true;
			}

			if ((physicMeshParam == null && !isDummyTransform)
				|| (physicMeshParam != null && isDummyTransform))
			{
				//Mesh도 없고, Dummy도 없으면..
				//또는 Mesh가 있는데도 Dummy 판정이 났다면.. 
				//Debug.Log("Unmatched Param");
				return;
			}

			//여기서부턴 Dummy가 있으면 그 값을 이용한다.
			//if (physicMeshParam != null)
			//{
			//	isDummyTransform = false;
			//}

			if (isDummyTransform && (_physicModifier_prevSelectedTransform == null || !_physicModifier_prevIsContained))
			{
				//Debug.Log("Dummy > NoData");
				return;
			}



			//물리 재질에 대한 UI

			GUILayout.Space(5);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(5);


			int labelHeight = 30;

			apPhysicsPresetUnit presetUnit = null;
			if (!isDummyTransform)
			{
				if (physicMeshParam._presetID >= 0)
				{
					presetUnit = Editor.PhysicsPreset.GetPresetUnit(physicMeshParam._presetID);
					if (presetUnit == null)
					{
						physicMeshParam._presetID = -1;
					}
				}
			}
			//EditorGUILayout.LabelField("Physical Material");
			//GUIStyle guiStyle_BoxStyle = new GUIStyle(GUI.skin.box);
			//guiStyle_BoxStyle.alignment = TextAnchor.MiddleCenter;
			//guiStyle_BoxStyle.normal.textColor = apEditorUtil.BoxTextColor;

			if (_guiContent_Modifier_PhysicsSetting_NameIcon == null)		{ _guiContent_Modifier_PhysicsSetting_NameIcon = new apGUIContentWrapper(); }
			if (_guiContent_Modifier_PhysicsSetting_Basic == null)			{ _guiContent_Modifier_PhysicsSetting_Basic = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.BasicSetting), Editor.ImageSet.Get(apImageSet.PRESET.Physic_BasicSetting)); }
			if (_guiContent_Modifier_PhysicsSetting_Stretchiness == null)	{ _guiContent_Modifier_PhysicsSetting_Stretchiness = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Stretchiness), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Stretch)); }
			if (_guiContent_Modifier_PhysicsSetting_Inertia == null)		{ _guiContent_Modifier_PhysicsSetting_Inertia = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Inertia), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Inertia)); }
			if (_guiContent_Modifier_PhysicsSetting_Restoring == null)		{ _guiContent_Modifier_PhysicsSetting_Restoring = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Restoring), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Recover)); }
			if (_guiContent_Modifier_PhysicsSetting_Viscosity == null)		{ _guiContent_Modifier_PhysicsSetting_Viscosity = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Viscosity), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Viscosity)); }
			if (_guiContent_Modifier_PhysicsSetting_Gravity == null)		{ _guiContent_Modifier_PhysicsSetting_Gravity = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Gravity), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Gravity)); }
			if (_guiContent_Modifier_PhysicsSetting_Wind == null)			{ _guiContent_Modifier_PhysicsSetting_Wind = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.Wind), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Wind)); }


			if (presetUnit != null)
			{
				bool isPropertySame = presetUnit.IsSameProperties(physicMeshParam);
				if (isPropertySame)
				{
					GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
				}
				else
				{
					GUI.backgroundColor = new Color(0.4f, 1.0f, 1.1f, 1.0f);
				}

				//이전
				//GUILayout.Box(
				//	new GUIContent("  " + presetUnit._name,
				//					Editor.ImageSet.Get(apEditorUtil.GetPhysicsPresetIconType(presetUnit._icon))),
				//	guiStyle_BoxStyle, GUILayout.Width(width), GUILayout.Height(30));

				//변경
				_guiContent_Modifier_PhysicsSetting_NameIcon.SetText(2, presetUnit._name);
				_guiContent_Modifier_PhysicsSetting_NameIcon.SetImage(Editor.ImageSet.Get(apEditorUtil.GetPhysicsPresetIconType(presetUnit._icon)));
				GUILayout.Box(_guiContent_Modifier_PhysicsSetting_NameIcon.Content, apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));

				GUI.backgroundColor = prevColor;
			}
			else
			{
				//"Physical Material"
				GUILayout.Box(Editor.GetUIWord(UIWORD.PhysicalMaterial), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
			}

			GUILayout.Space(5);
			//TODO : Preset
			//값이 바뀌었으면 Dirty
			//"  Basic Setting"
			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.BasicSetting), Editor.ImageSet.Get(apImageSet.PRESET.Physic_BasicSetting)), GUILayout.Height(labelHeight));

			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Basic.Content, apGUILOFactory.I.Height(labelHeight));
			
			float nextMass = EditorGUILayout.DelayedFloatField(Editor.GetUIWord(UIWORD.Mass), (!isDummyTransform) ? physicMeshParam._mass : 0.0f);//"Mass"
			float nextDamping = EditorGUILayout.DelayedFloatField(Editor.GetUIWord(UIWORD.Damping), (!isDummyTransform) ? physicMeshParam._damping : 0.0f);//"Damping"
			float nextAirDrag = EditorGUILayout.DelayedFloatField(Editor.GetUIWord(UIWORD.AirDrag), (!isDummyTransform) ? physicMeshParam._airDrag : 0.0f);//"Air Drag"
			bool nextIsRestrictMoveRange = EditorGUILayout.Toggle(Editor.GetUIWord(UIWORD.SetMoveRange), (!isDummyTransform) ? physicMeshParam._isRestrictMoveRange : false);//"Set Move Range"
			float nextMoveRange = (!isDummyTransform) ? physicMeshParam._moveRange : 0.0f;
			if (nextIsRestrictMoveRange)
			{
				nextMoveRange = EditorGUILayout.DelayedFloatField(Editor.GetUIWord(UIWORD.MoveRange), (!isDummyTransform) ? physicMeshParam._moveRange : 0.0f);//"Move Range"
			}
			else
			{
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.MoveRangeUnlimited));//"Move Range : Unlimited"
			}

			GUILayout.Space(5);

			int valueWidth = 74;//캬... 꼼꼼하다
			int labelWidth = width - (valueWidth + 2 + 5);
			int leftMargin = 3;
			int topMargin = 10;

			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.Stretchiness), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Stretch)), GUILayout.Height(labelHeight));//"  Stretchiness"

			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Stretchiness.Content, apGUILOFactory.I.Height(labelHeight));//"  Stretchiness"

			
			float nextStretchK = EditorGUILayout.DelayedFloatField(Editor.GetUIWord(UIWORD.K_Value), (!isDummyTransform) ? physicMeshParam._stretchK : 0.0f);//"K-Value"
			bool nextIsRestrictStretchRange = EditorGUILayout.Toggle(Editor.GetUIWord(UIWORD.SetStretchRange), (!isDummyTransform) ? physicMeshParam._isRestrictStretchRange : false);//"Set Stretch Range"
			float nextStretchRange_Max = (!isDummyTransform) ? physicMeshParam._stretchRangeRatio_Max : 0.0f;
			if (nextIsRestrictStretchRange)
			{
				//"Lengthen Ratio"
				nextStretchRange_Max = EditorGUILayout.DelayedFloatField(Editor.GetUIWord(UIWORD.LengthenRatio), (!isDummyTransform) ? physicMeshParam._stretchRangeRatio_Max : 0.0f);
			}
			else
			{
				//"Lengthen Ratio : Unlimited"
				EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.LengthenRatioUnlimited));
			}
			GUILayout.Space(5);


			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelHeight));
			GUILayout.Space(leftMargin);
			
			//"  Inertia"
			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.Inertia), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Inertia)), GUILayout.Width(labelWidth), GUILayout.Height(labelHeight));
			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Inertia.Content, apGUILOFactory.I.Width(labelWidth), apGUILOFactory.I.Height(labelHeight));

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(valueWidth), apGUILOFactory.I.Height(labelHeight));//>>2
			GUILayout.Space(topMargin);
			float nextInertiaK = EditorGUILayout.DelayedFloatField((!isDummyTransform) ? physicMeshParam._inertiaK : 0.0f, apGUILOFactory.I.Width(valueWidth));
			EditorGUILayout.EndVertical();//<<2
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);


			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelHeight));
			GUILayout.Space(leftMargin);
			
			//"  Restoring"
			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.Restoring), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Recover)), GUILayout.Width(labelWidth), GUILayout.Height(labelHeight));
			
			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Restoring.Content, apGUILOFactory.I.Width(labelWidth), apGUILOFactory.I.Height(labelHeight));

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(valueWidth), apGUILOFactory.I.Height(labelHeight));//>>2
			GUILayout.Space(topMargin);
			float nextRestoring = EditorGUILayout.DelayedFloatField((!isDummyTransform) ? physicMeshParam._restoring : 0.0f, apGUILOFactory.I.Width(valueWidth));
			EditorGUILayout.EndVertical();//<<2
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);


			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(labelHeight));
			GUILayout.Space(leftMargin);
			//"  Viscosity"
			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.Viscosity), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Viscosity)), GUILayout.Width(labelWidth), GUILayout.Height(labelHeight));
			
			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Viscosity.Content, apGUILOFactory.I.Width(labelWidth), apGUILOFactory.I.Height(labelHeight));

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(valueWidth), apGUILOFactory.I.Height(labelHeight));//>>2
			GUILayout.Space(topMargin);
			float nextViscosity = EditorGUILayout.DelayedFloatField((!isDummyTransform) ? physicMeshParam._viscosity : 0.0f, apGUILOFactory.I.Width(valueWidth));
			EditorGUILayout.EndVertical();//<<2
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(5);

			

			//값이 바뀌었으면 적용
			if (!isDummyTransform)
			{
				if (nextMass != physicMeshParam._mass
					|| nextDamping != physicMeshParam._damping
					|| nextAirDrag != physicMeshParam._airDrag
					|| nextMoveRange != physicMeshParam._moveRange
					|| nextStretchK != physicMeshParam._stretchK
					//|| nextStretchRange_Min != physicMeshParam._stretchRangeRatio_Min
					|| nextStretchRange_Max != physicMeshParam._stretchRangeRatio_Max
					|| nextInertiaK != physicMeshParam._inertiaK
					|| nextRestoring != physicMeshParam._restoring
					|| nextViscosity != physicMeshParam._viscosity
					|| nextIsRestrictStretchRange != physicMeshParam._isRestrictStretchRange
					|| nextIsRestrictMoveRange != physicMeshParam._isRestrictMoveRange)
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, ModMeshOfMod, false);

					physicMeshParam._mass = nextMass;
					physicMeshParam._damping = nextDamping;
					physicMeshParam._airDrag = nextAirDrag;
					physicMeshParam._moveRange = nextMoveRange;
					physicMeshParam._stretchK = nextStretchK;

					//physicMeshParam._stretchRangeRatio_Min = Mathf.Clamp01(nextStretchRange_Min);
					physicMeshParam._stretchRangeRatio_Max = nextStretchRange_Max;
					if (physicMeshParam._stretchRangeRatio_Max < 0.0f)
					{
						physicMeshParam._stretchRangeRatio_Max = 0.0f;
					}

					physicMeshParam._isRestrictStretchRange = nextIsRestrictStretchRange;
					physicMeshParam._isRestrictMoveRange = nextIsRestrictMoveRange;


					physicMeshParam._inertiaK = nextInertiaK;
					physicMeshParam._restoring = nextRestoring;
					physicMeshParam._viscosity = nextViscosity;

					apEditorUtil.ReleaseGUIFocus();
				}
			}



			


			//GUILayout.Space(5);

			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.Gravity), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Gravity)), GUILayout.Height(labelHeight));//"  Gravity"

			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Gravity.Content, apGUILOFactory.I.Height(labelHeight));//"  Gravity"

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.InputType));//"Input Type"
			apPhysicsMeshParam.ExternalParamType nextGravityParam = (apPhysicsMeshParam.ExternalParamType)EditorGUILayout.EnumPopup((!isDummyTransform) ? physicMeshParam._gravityParamType : apPhysicsMeshParam.ExternalParamType.Constant);

			Vector2 nextGravityConstValue = (!isDummyTransform) ? physicMeshParam._gravityConstValue : Vector2.zero;

			apPhysicsMeshParam.ExternalParamType curGravityParam = (physicMeshParam != null) ? physicMeshParam._gravityParamType : apPhysicsMeshParam.ExternalParamType.Constant;

			if (curGravityParam == apPhysicsMeshParam.ExternalParamType.Constant)
			{
				nextGravityConstValue = apEditorUtil.DelayedVector2Field((!isDummyTransform) ? physicMeshParam._gravityConstValue : Vector2.zero, width - 4);
			}
			else
			{
				//?
				//TODO : GravityControlParam 링크할 것
				apControlParam controlParam = physicMeshParam._gravityControlParam;
				if (controlParam == null && physicMeshParam._gravityControlParamID > 0)
				{
					physicMeshParam._gravityControlParam = Editor._portrait._controller.FindParam(physicMeshParam._gravityControlParamID);
					controlParam = physicMeshParam._gravityControlParam;
					if (controlParam == null)
					{
						physicMeshParam._gravityControlParamID = -1;
					}
				}

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(5);
				if (controlParam != null)
				{
					if(_strWrapper_64 == null)
					{
						_strWrapper_64 = new apStringWrapper(64);
					}

					_strWrapper_64.Clear();
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
					_strWrapper_64.Append(controlParam._keyName, false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

					GUI.backgroundColor = new Color(0.5f, 1.0f, 1.0f, 1.0f);
					//GUILayout.Box("[" + controlParam._keyName + "]", apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 34), apGUILOFactory.I.Height(25));
					GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 34), apGUILOFactory.I.Height(25));

					GUI.backgroundColor = prevColor;
				}
				else
				{
					GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
					GUILayout.Box(Editor.GetUIWord(UIWORD.NoControlParam), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 34), apGUILOFactory.I.Height(25));//"No ControlParam

					GUI.backgroundColor = prevColor;
				}

				if (GUILayout.Button(Editor.GetUIWord(UIWORD.Set), apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(25)))//"Set"
				{
					//Control Param을 선택하는 Dialog를 호출하자
					_loadKey_SelectControlParamToPhyGravity = apDialog_SelectControlParam.ShowDialog(Editor, apDialog_SelectControlParam.PARAM_TYPE.Vector2, OnSelectControlParamToPhysicGravity, null);
				}
				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(5);

			//이전
			//EditorGUILayout.LabelField(new GUIContent("  " + Editor.GetUIWord(UIWORD.Wind), Editor.ImageSet.Get(apImageSet.PRESET.Physic_Wind)), GUILayout.Height(labelHeight));//"  Wind"
			
			//변경
			EditorGUILayout.LabelField(_guiContent_Modifier_PhysicsSetting_Wind.Content, apGUILOFactory.I.Height(labelHeight));//"  Wind"

			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.InputType));//"Input Type"
			apPhysicsMeshParam.ExternalParamType nextWindParamType = (apPhysicsMeshParam.ExternalParamType)EditorGUILayout.EnumPopup((!isDummyTransform) ? physicMeshParam._windParamType : apPhysicsMeshParam.ExternalParamType.Constant);

			Vector2 nextWindConstValue = (!isDummyTransform) ? physicMeshParam._windConstValue : Vector2.zero;
			Vector2 nextWindRandomRange = (!isDummyTransform) ? physicMeshParam._windRandomRange : Vector2.zero;

			apPhysicsMeshParam.ExternalParamType curWindParamType = (physicMeshParam != null) ? physicMeshParam._windParamType : apPhysicsMeshParam.ExternalParamType.Constant;

			if (curWindParamType == apPhysicsMeshParam.ExternalParamType.Constant)
			{
				nextWindConstValue = apEditorUtil.DelayedVector2Field((!isDummyTransform) ? physicMeshParam._windConstValue : Vector2.zero, width - 4);
			}
			else
			{
				//?
				//TODO : GravityControlParam 링크할 것
				apControlParam controlParam = physicMeshParam._windControlParam;
				if (controlParam == null && physicMeshParam._windControlParamID > 0)
				{
					physicMeshParam._windControlParam = Editor._portrait._controller.FindParam(physicMeshParam._windControlParamID);
					controlParam = physicMeshParam._windControlParam;
					if (controlParam == null)
					{
						physicMeshParam._windControlParamID = -1;
					}
				}

				EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(25));
				GUILayout.Space(5);
				if (controlParam != null)
				{
					if(_strWrapper_64 == null)
					{
						_strWrapper_64 = new apStringWrapper(64);
					}

					_strWrapper_64.Clear();
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
					_strWrapper_64.Append(controlParam._keyName, false);
					_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);


					GUI.backgroundColor = new Color(0.5f, 1.0f, 1.0f, 1.0f);
					//GUILayout.Box("[" + controlParam._keyName + "]", apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width - 34), GUILayout.Height(25));
					GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 34), apGUILOFactory.I.Height(25));

					GUI.backgroundColor = prevColor;
				}
				else
				{
					GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
					GUILayout.Box(Editor.GetUIWord(UIWORD.NoControlParam), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width - 34), apGUILOFactory.I.Height(25));//"No ControlParam"

					GUI.backgroundColor = prevColor;
				}

				if (GUILayout.Button(Editor.GetUIWord(UIWORD.Set), apGUILOFactory.I.Width(30), apGUILOFactory.I.Height(25)))//"Set"
				{
					//Control Param을 선택하는 Dialog를 호출하자
					_loadKey_SelectControlParamToPhyWind = apDialog_SelectControlParam.ShowDialog(Editor, apDialog_SelectControlParam.PARAM_TYPE.Vector2, OnSelectControlParamToPhysicWind, null);
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.WindRandomRangeSize));//"Wind Random Range Size"
			nextWindRandomRange = apEditorUtil.DelayedVector2Field((!isDummyTransform) ? physicMeshParam._windRandomRange : Vector2.zero, width - 4);

			GUILayout.Space(10);
			

			//Preset 창을 열자
			//" Physics Presets"
			if (apEditorUtil.ToggledButton_2Side(	Editor.ImageSet.Get(apImageSet.PRESET.Physic_Palette), 
													1, Editor.GetUIWord(UIWORD.PhysicsPresets), 
													Editor.GetUIWord(UIWORD.PhysicsPresets), 
													false, physicMeshParam != null, width, 32))
			{
				_loadKey_SelectPhysicsParam = apDialog_PhysicsPreset.ShowDialog(Editor, ModMeshOfMod, OnSelectPhysicsPreset);
			}


			if (!isDummyTransform)
			{
				if (nextGravityParam != physicMeshParam._gravityParamType
					|| nextGravityConstValue.x != physicMeshParam._gravityConstValue.x
					|| nextGravityConstValue.y != physicMeshParam._gravityConstValue.y
					|| nextWindParamType != physicMeshParam._windParamType
					|| nextWindConstValue.x != physicMeshParam._windConstValue.x
					|| nextWindConstValue.y != physicMeshParam._windConstValue.y
					|| nextWindRandomRange.x != physicMeshParam._windRandomRange.x
					|| nextWindRandomRange.y != physicMeshParam._windRandomRange.y
					)
				{
					apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, ModMeshOfMod, false);

					physicMeshParam._gravityParamType = nextGravityParam;
					physicMeshParam._gravityConstValue = nextGravityConstValue;
					physicMeshParam._windParamType = nextWindParamType;
					physicMeshParam._windConstValue = nextWindConstValue;
					physicMeshParam._windRandomRange = nextWindRandomRange;
					apEditorUtil.ReleaseGUIFocus();
				}
			}
		}


		//Physic Modifier에서 Gravity/Wind를 Control Param에 연결할 때, Dialog를 열어서 선택하도록 한다.
		

		public void OnSelectControlParamToPhysicGravity(bool isSuccess, object loadKey, apControlParam resultControlParam, object savedObject)
		{
			//Debug.Log("Select Control Param : OnSelectControlParamToPhysicGravity (" + isSuccess + ")");
			if (_loadKey_SelectControlParamToPhyGravity != loadKey || !isSuccess)
			{
				//Debug.LogError("AnyPortrait : Wrong loadKey");
				_loadKey_SelectControlParamToPhyGravity = null;
				return;
			}

			_loadKey_SelectControlParamToPhyGravity = null;
			if (Modifier == null
				|| (Modifier.ModifiedValueType & apModifiedMesh.MOD_VALUE_TYPE.VertexWeightList_Physics) == 0
				|| ModMeshOfMod == null)
			{
				return;
			}
			if (ModMeshOfMod.PhysicParam == null)
			{
				return;
			}

			apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, ModMeshOfMod, false);

			ModMeshOfMod.PhysicParam._gravityControlParam = resultControlParam;
			if (resultControlParam == null)
			{
				ModMeshOfMod.PhysicParam._gravityControlParamID = -1;
			}
			else
			{
				ModMeshOfMod.PhysicParam._gravityControlParamID = resultControlParam._uniqueID;
			}
		}

		

		public void OnSelectControlParamToPhysicWind(bool isSuccess, object loadKey, apControlParam resultControlParam, object savedObject)
		{
			if (_loadKey_SelectControlParamToPhyWind != loadKey || !isSuccess)
			{
				_loadKey_SelectControlParamToPhyWind = null;
				return;
			}

			_loadKey_SelectControlParamToPhyWind = null;
			if (Modifier == null
				|| (Modifier.ModifiedValueType & apModifiedMesh.MOD_VALUE_TYPE.VertexWeightList_Physics) == 0
				|| ModMeshOfMod == null)
			{
				return;
			}
			if (ModMeshOfMod.PhysicParam == null)
			{
				return;
			}

			apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SettingChanged, Editor, Modifier, ModMeshOfMod, false);

			ModMeshOfMod.PhysicParam._windControlParam = resultControlParam;
			if (resultControlParam == null)
			{
				ModMeshOfMod.PhysicParam._windControlParamID = -1;
			}
			else
			{
				ModMeshOfMod.PhysicParam._windControlParamID = resultControlParam._uniqueID;
			}
		}

		

		private void OnSelectPhysicsPreset(bool isSuccess, object loadKey, apPhysicsPresetUnit physicsUnit, apModifiedMesh targetModMesh)
		{
			if (!isSuccess || physicsUnit == null || targetModMesh == null || loadKey != _loadKey_SelectPhysicsParam || targetModMesh != ModMeshOfMod)
			{
				_loadKey_SelectPhysicsParam = null;
				return;
			}
			_loadKey_SelectPhysicsParam = null;
			if (targetModMesh.PhysicParam == null || SelectionType != SELECTION_TYPE.MeshGroup)
			{
				return;
			}
			//값 복사를 해주자
			
			apEditorUtil.SetRecord_Modifier(apUndoGroupData.ACTION.Modifier_SetPhysicsProperty, Editor, Modifier, null, false);

			apPhysicsMeshParam physicsMeshParam = targetModMesh.PhysicParam;

			physicsMeshParam._presetID = physicsUnit._uniqueID;
			physicsMeshParam._moveRange = physicsUnit._moveRange;

			physicsMeshParam._isRestrictMoveRange = physicsUnit._isRestrictMoveRange;
			physicsMeshParam._isRestrictStretchRange = physicsUnit._isRestrictStretchRange;

			//physicsMeshParam._stretchRangeRatio_Min = physicsUnit._stretchRange_Min;
			physicsMeshParam._stretchRangeRatio_Max = physicsUnit._stretchRange_Max;
			physicsMeshParam._stretchK = physicsUnit._stretchK;
			physicsMeshParam._inertiaK = physicsUnit._inertiaK;
			physicsMeshParam._damping = physicsUnit._damping;
			physicsMeshParam._mass = physicsUnit._mass;

			physicsMeshParam._gravityConstValue = physicsUnit._gravityConstValue;
			physicsMeshParam._windConstValue = physicsUnit._windConstValue;
			physicsMeshParam._windRandomRange = physicsUnit._windRandomRange;

			physicsMeshParam._airDrag = physicsUnit._airDrag;
			physicsMeshParam._viscosity = physicsUnit._viscosity;
			physicsMeshParam._restoring = physicsUnit._restoring;

		}

		

		// Animation Right 2 GUI
		//------------------------------------------------------------------------------------
		private void DrawEditor_Right2_Animation(int width, int height)
		{
			// 상단부는 AnimClip의 정보를 출력하며,
			// 하단부는 선택된 Timeline의 정보를 출력한다.

			// AnimClip 정보 출력 부분

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_AnimClip, (AnimClip != null));//"AnimationRight2GUI_AnimClip"
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_Timeline, (AnimTimeline != null));//"AnimationRight2GUI_Timeline"

			if (AnimClip == null)
			{
				return;
			}

			if (!Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_AnimClip))//"AnimationRight2GUI_AnimClip"
			{
				//아직 출력하면 안된다.
				return;
			}

			apAnimClip animClip = AnimClip;

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(50));
			GUILayout.Space(10);

			//이전
			//EditorGUILayout.LabelField(
			//	new GUIContent(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation)),
			//	GUILayout.Width(50), GUILayout.Height(50));

			//변경
			if(_guiContent_Right_MeshGroup_AnimIcon == null)
			{
				_guiContent_Right_MeshGroup_AnimIcon = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_Animation));
			}
			EditorGUILayout.LabelField(_guiContent_Right_MeshGroup_AnimIcon.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(50));

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width - (50 + 10)));
			GUILayout.Space(5);
			EditorGUILayout.LabelField(animClip._name, apGUILOFactory.I.Width(width - (50 + 10)));


			if(_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}

			if (animClip._targetMeshGroup != null)
			{
				_strWrapper_64.Clear();
				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Target), false);
				_strWrapper_64.Append(apStringFactory.I.Colon_Space, false);
				_strWrapper_64.Append(animClip._targetMeshGroup._name, true);

				//"Target : " + animClip._targetMeshGroup._name
				//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.Target) + " : " + animClip._targetMeshGroup._name, apGUILOFactory.I.Width(width - (50 + 10)));
				EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(width - (50 + 10)));
			}
			else
			{
				_strWrapper_64.Clear();
				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Target), false);
				_strWrapper_64.Append(apStringFactory.I.Colon_Space, false);
				_strWrapper_64.Append(Editor.GetUIWord(UIWORD.NoMeshGroup), true);

				//EditorGUILayout.LabelField(string.Format("{0} : {1}", Editor.GetUIWord(UIWORD.Target), Editor.GetUIWord(UIWORD.NoMeshGroup)), GUILayout.Width(width - (50 + 10)));
				EditorGUILayout.LabelField(_strWrapper_64.ToString(), apGUILOFactory.I.Width(width - (50 + 10)));
			}


			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			//애니메이션 기본 정보
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.AnimationSettings), apGUILOFactory.I.Width(width));//"Animation Settings"
			GUILayout.Space(2);
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.StartFrame), apGUILOFactory.I.Width(110));//"Start Frame"
			int nextStartFrame = EditorGUILayout.DelayedIntField(animClip.StartFrame, apGUILOFactory.I.Width(width - (110 + 5)));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.EndFrame), apGUILOFactory.I.Width(110));//"End Frame"
			int nextEndFrame = EditorGUILayout.DelayedIntField(animClip.EndFrame, apGUILOFactory.I.Width(width - (110 + 5)));
			EditorGUILayout.EndHorizontal();

			bool isNextLoop = animClip.IsLoop;
			//" Loop On", " Loop Off"
			if (apEditorUtil.ToggledButton_2Side(Editor.ImageSet.Get(apImageSet.PRESET.Anim_Loop),
													1, Editor.GetUIWord(UIWORD.LoopOn),
													Editor.GetUIWord(UIWORD.LoopOff),
													animClip.IsLoop, true, width, 24))
			{
				isNextLoop = !animClip.IsLoop;
				//값 적용은 아래에서
			}

			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width));
			EditorGUILayout.LabelField(apStringFactory.I.FPS, apGUILOFactory.I.Width(110));//<<이건 고정//"FPS"
			
			int nextFPS = EditorGUILayout.DelayedIntField(animClip.FPS, apGUILOFactory.I.Width(width - (110 + 5)));
			//int nextFPS = EditorGUILayout.IntSlider("FPS", animClip._FPS, 1, 240, GUILayout.Width(width));
			
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);
			//추가 : 애니메이션 이벤트
			int nAnimEvents = 0;
			if (animClip._animEvents != null)
			{
				nAnimEvents = animClip._animEvents.Count;
			}

			_strWrapper_64.Clear();
			_strWrapper_64.Append(Editor.GetUIWord(UIWORD.AnimationEvents), false);
			_strWrapper_64.AppendSpace(1, false);
			_strWrapper_64.Append(apStringFactory.I.Bracket_2_L, false);
			_strWrapper_64.Append(nAnimEvents, false);
			_strWrapper_64.Append(apStringFactory.I.Bracket_2_R, true);

			//"Animation Events..
			//if (GUILayout.Button(string.Format("{0} [{1}]", Editor.GetUIWord(UIWORD.AnimationEvents), nAnimEvents), GUILayout.Height(22)))
			if (GUILayout.Button(_strWrapper_64.ToString(), apGUILOFactory.I.Height(22)))
			{
				apDialog_AnimationEvents.ShowDialog(Editor, Editor._portrait, animClip);
			}

			//" Export/Import"
			//이전
			//if (GUILayout.Button(new GUIContent(" " + Editor.GetUIWord(UIWORD.ExportImport), Editor.ImageSet.Get(apImageSet.PRESET.Anim_Save)), GUILayout.Height(22)))

			if(_guiContent_Right_Animation_ExportImportAnim == null)
			{
				_guiContent_Right_Animation_ExportImportAnim = apGUIContentWrapper.Make(1, Editor.GetUIWord(UIWORD.ExportImport), Editor.ImageSet.Get(apImageSet.PRESET.Anim_Save));
			}
			
			if (GUILayout.Button(_guiContent_Right_Animation_ExportImportAnim.Content, apGUILOFactory.I.Height(22)))
			{
				//AnimClip을 Export/Import 하자
				_loadKey_ImportAnimClipRetarget = apDialog_RetargetPose.ShowDialog(Editor, _animClip._targetMeshGroup, _animClip, OnImportAnimClipRetarget);
			}

			if (nextStartFrame != animClip.StartFrame
				|| nextEndFrame != animClip.EndFrame
				|| nextFPS != animClip.FPS
				|| isNextLoop != animClip.IsLoop)
			{
				//바뀌었다면 타임라인 GUI를 세팅할 필요가 있을 수 있다.
				//Debug.Log("Anim Setting Changed");

				//Undo에 저장하자
				if(animClip._targetMeshGroup != null)
				{
					apEditorUtil.SetRecord_PortraitMeshGroup(	apUndoGroupData.ACTION.Anim_SettingChanged,
																Editor, 
																Editor._portrait,
																animClip._targetMeshGroup,
																animClip,
																false,
																false);
				}
				else
				{
					apEditorUtil.SetRecord_Portrait(	apUndoGroupData.ACTION.Anim_SettingChanged,
														Editor, 
														Editor._portrait,
														animClip,
														false);
				}

				apEditorUtil.SetEditorDirty();

				//Start Frame과 Next Frame의 값이 뒤집혀져있는지 확인
				if (nextStartFrame > nextEndFrame)
				{
					int tmp = nextStartFrame;
					nextStartFrame = nextEndFrame;
					nextEndFrame = tmp;
				}

				animClip.SetOption_StartFrame(nextStartFrame);
				animClip.SetOption_EndFrame(nextEndFrame);
				animClip.SetOption_FPS(nextFPS);
				animClip.SetOption_IsLoop(isNextLoop);

				//추가 20.4.14 : 애니메이션의 길이나 루프 설정이 바뀌면, 전체적으로 리프레시를 하자
				animClip.RefreshTimelines(null);

				apEditorUtil.ReleaseGUIFocus();
			}



			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);


			// Timeline 정보 출력 부분

			if (AnimTimeline == null)
			{
				return;
			}

			if (!Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_Timeline))//"AnimationRight2GUI_Timeline"
			{
				//아직 출력하면 안된다.
				return;
			}

			apAnimTimeline animTimeline = AnimTimeline;
			apAnimTimelineLayer animTimelineLayer = AnimTimelineLayer;

			
			
			//Timeline 정보 출력
			//이전
			//Texture2D iconTimeline = null;
			//switch (animTimeline._linkType)
			//{
			//	case apAnimClip.LINK_TYPE.AnimatedModifier:
			//		iconTimeline = Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithMod);
			//		break;

			//	//case apAnimClip.LINK_TYPE.Bone:
			//	//	iconTimeline = Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithBone);
			//	//	break;

			//	case apAnimClip.LINK_TYPE.ControlParam:
			//		iconTimeline = Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithControlParam);
			//		break;

			//	default:
			//		iconTimeline = Editor.ImageSet.Get(apImageSet.PRESET.Edit_Copy);//<<이상한 걸 넣어서 나중에 수정할 수 있게 하자
			//		break;
			//}

			if(_guiContent_Right_Animation_TimelineIcon_AnimWithMod == null)
			{
				_guiContent_Right_Animation_TimelineIcon_AnimWithMod = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithMod));
			}
			if(_guiContent_Right_Animation_TimelineIcon_AnimWithControlParam == null)
			{
				_guiContent_Right_Animation_TimelineIcon_AnimWithControlParam = apGUIContentWrapper.Make(Editor.ImageSet.Get(apImageSet.PRESET.Anim_WithControlParam));
			}
			
			apGUIContentWrapper curIconGUIContent = (animTimeline._linkType == apAnimClip.LINK_TYPE.AnimatedModifier) ? _guiContent_Right_Animation_TimelineIcon_AnimWithMod : _guiContent_Right_Animation_TimelineIcon_AnimWithControlParam;
			
			//1. 아이콘 / 타입
			EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(30));
			GUILayout.Space(10);
			
			//이전
			//EditorGUILayout.LabelField(new GUIContent(iconTimeline), GUILayout.Width(50), GUILayout.Height(50));

			//변경
			EditorGUILayout.LabelField(curIconGUIContent.Content, apGUILOFactory.I.Width(50), apGUILOFactory.I.Height(30));

			EditorGUILayout.BeginVertical(apGUILOFactory.I.Width(width - (50 + 10)));
			GUILayout.Space(5);
			EditorGUILayout.LabelField(animTimeline.DisplayName, apGUILOFactory.I.Width(width - (50 + 10)));


			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			//GUILayout.Space(10);


			//현재 선택한 객체를 레이어로 만들 수 있다.
			//상태 : 선택한게 없다. / 선택은 했으나 레이어에 등록이 안되었다. (등록할 수 있다) / 선택한게 이미 등록한 객체다. (
			//bool isAnyTargetObjectSelected = false;
			bool isAddableType = false;
			bool isAddable = false;
			
			//string targetObjectName = "";
			if(_guiContent_Right2_Animation_TargetObjectName == null)
			{
				_guiContent_Right2_Animation_TargetObjectName = new apGUIContentWrapper();
			}

			
			

			object targetObject = null;
			bool isAddingLayerOnce = false;
			bool isAddChildTransformAddable = false;
			switch (animTimeline._linkType)
			{
				case apAnimClip.LINK_TYPE.AnimatedModifier:
					//Transform이 속해있는지 확인하자
					if (SubMeshTransformOnAnimClip != null)
					{
						//isAnyTargetObjectSelected = true;
						//targetObjectName = SubMeshTransformOnAnimClip._nickName;//이전
						_guiContent_Right2_Animation_TargetObjectName.ClearText(false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(SubMeshTransformOnAnimClip._nickName, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_R, true);//변경

						targetObject = SubMeshTransformOnAnimClip;

						//레이어로 등록가능한가
						isAddableType = animTimeline.IsLayerAddableType(SubMeshTransformOnAnimClip);
						isAddable = !animTimeline.IsObjectAddedInLayers(SubMeshTransformOnAnimClip);
					}
					else if (SubMeshGroupTransformOnAnimClip != null)
					{
						//isAnyTargetObjectSelected = true;
						//targetObjectName = SubMeshGroupTransformOnAnimClip._nickName;//이전
						_guiContent_Right2_Animation_TargetObjectName.ClearText(false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(SubMeshGroupTransformOnAnimClip._nickName, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_R, true);//변경

						targetObject = SubMeshGroupTransformOnAnimClip;

						//레이어로 등록가능한가.
						isAddableType = animTimeline.IsLayerAddableType(SubMeshGroupTransformOnAnimClip);
						isAddable = !animTimeline.IsObjectAddedInLayers(SubMeshGroupTransformOnAnimClip);
					}
					else if (Bone != null)
					{
						//isAnyTargetObjectSelected = true;
						//targetObjectName = Bone._name;//이전
						_guiContent_Right2_Animation_TargetObjectName.ClearText(false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(Bone._name, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_R, true);//변경

						targetObject = Bone;

						isAddableType = animTimeline.IsLayerAddableType(Bone);
						isAddable = !animTimeline.IsObjectAddedInLayers(Bone);
					}
					else
					{
						_guiContent_Right2_Animation_TargetObjectName.ClearText(true);//추가
					}
					isAddingLayerOnce = true;//한번에 레이어를 추가할 수 있다.
					isAddChildTransformAddable = animTimeline._linkedModifier.IsTarget_ChildMeshTransform;
					break;


				case apAnimClip.LINK_TYPE.ControlParam:
					if (SubControlParamOnAnimClip != null)
					{
						//isAnyTargetObjectSelected = true;
						//targetObjectName = SubControlParamOnAnimClip._keyName;//이전
						_guiContent_Right2_Animation_TargetObjectName.ClearText(false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_L, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(SubControlParamOnAnimClip._keyName, false);
						_guiContent_Right2_Animation_TargetObjectName.AppendText(apStringFactory.I.Bracket_2_R, true);//변경

						targetObject = SubControlParamOnAnimClip;

						isAddableType = animTimeline.IsLayerAddableType(SubControlParamOnAnimClip);
						isAddable = !animTimeline.IsObjectAddedInLayers(SubControlParamOnAnimClip);
					}
					else
					{
						_guiContent_Right2_Animation_TargetObjectName.ClearText(true);//추가
					}

					isAddingLayerOnce = false;
					break;

				default:
					_guiContent_Right2_Animation_TargetObjectName.ClearText(true);//추가
					break;
			}
			bool isRemoveTimeline = false;

			bool isRemoveTimelineLayer = false;
			apAnimTimelineLayer removeLayer = null;

			//추가 : 추가 가능한 모든 객체에 대해서 TimelineLayer를 추가한다.
			if (isAddingLayerOnce)
			{
				//string strTargetObject = "";
				bool isTargetTF = true;
				//Texture2D addIconImage = null;

				//변경
				if(_guiContent_Right_Animation_AllObjectToLayers == null)
				{
					_guiContent_Right_Animation_AllObjectToLayers = new apGUIContentWrapper();
				}

				if (_meshGroupChildHierarchy_Anim == MESHGROUP_CHILD_HIERARCHY.ChildMeshes)
				{
					isTargetTF = true;
					//이전
					//strTargetObject = Editor.GetUIWord(UIWORD.Meshes);//"Meshes"
					//addIconImage = Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddAllMeshesToLayer);
					
					//변경
					_guiContent_Right_Animation_AllObjectToLayers.SetText(Editor.GetUIWordFormat(UIWORD.AllObjectToLayers, Editor.GetUIWord(UIWORD.Meshes)));
					_guiContent_Right_Animation_AllObjectToLayers.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddAllMeshesToLayer));
				}
				else
				{
					isTargetTF = false;
					//이전
					//strTargetObject = Editor.GetUIWord(UIWORD.Bones);//"Bones"
					//addIconImage = Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddAllBonesToLayer);

					//변경
					_guiContent_Right_Animation_AllObjectToLayers.SetText(Editor.GetUIWordFormat(UIWORD.AllObjectToLayers, Editor.GetUIWord(UIWORD.Bones)));
					_guiContent_Right_Animation_AllObjectToLayers.SetImage(Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddAllBonesToLayer));
				}
				
				//이전
				//if (GUILayout.Button(new GUIContent(Editor.GetUIWordFormat(UIWORD.AllObjectToLayers, strTargetObject), addIconImage), GUILayout.Height(30)))

				//변경
				if (GUILayout.Button(_guiContent_Right_Animation_AllObjectToLayers.Content, apGUILOFactory.I.Height(30)))
				{
					//bool isResult = EditorUtility.DisplayDialog("Add to Timelines", "All " + strTargetObject + " are added to Timeline Layers?", "Add All", "Cancel");

					bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.AddAllObjects2Timeline_Title),
																//Editor.GetTextFormat(TEXT.AddAllObjects2Timeline_Body, strTargetObject),//이전
																Editor.GetTextFormat(TEXT.AddAllObjects2Timeline_Body, isTargetTF ? Editor.GetUIWord(UIWORD.Meshes) : Editor.GetUIWord(UIWORD.Bones)),//변경 19.12.23
																Editor.GetText(TEXT.Okay),
																Editor.GetText(TEXT.Cancel)
																);

					if (isResult)
					{
						//모든 객체를 TimelineLayer로 등록한다.
						Editor.Controller.AddAnimTimelineLayerForAllTransformObject(animClip._targetMeshGroup,
																						isTargetTF,
																						isAddChildTransformAddable,
																						animTimeline);
					}
				}
			}

			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			//"  Remove Timeline"
			//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveTimeline),
			//										Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform)
			//										),
			//						GUILayout.Height(24)))

			if(_guiContent_Right_Animation_RemoveTimeline == null)
			{
				_guiContent_Right_Animation_RemoveTimeline = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveTimeline), Editor.ImageSet.Get(apImageSet.PRESET.Hierarchy_RemoveTransform));
			}
			
			if (GUILayout.Button(_guiContent_Right_Animation_RemoveTimeline.Content, apGUILOFactory.I.Height(24)))
			{
				//bool isResult = EditorUtility.DisplayDialog("Remove Timeline", "Is Really Remove Timeline?", "Remove", "Cancel");

				bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveTimeline_Title),
																Editor.GetTextFormat(TEXT.RemoveTimeline_Body, animTimeline.DisplayName),
																Editor.GetText(TEXT.Remove),
																Editor.GetText(TEXT.Cancel)
																);

				if (isResult)
				{
					isRemoveTimeline = true;
				}
			}
			GUILayout.Space(10);
			apEditorUtil.GUI_DelimeterBoxH(width);
			GUILayout.Space(10);

			Color prevColor = GUI.backgroundColor;



			//Editor.SetGUIVisible("AnimationRight2GUI_Timeline_SelectedObject", _prevSelectedAnimObject == targetObject || _isIgnoreAnimTimelineGUI);
			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_Timeline_SelectedObject, (_prevSelectedAnimObject != null) == (targetObject != null));//"AnimationRight2GUI_Timeline_SelectedObject"
			bool isGUI_TargetSelected = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_Timeline_SelectedObject);//"AnimationRight2GUI_Timeline_SelectedObject"

			Editor.SetGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_Timeline_Layers, 
				(_prevSelectedAnimTimeline == _subAnimTimeline && _prevSelectedAnimTimelineLayer == _subAnimTimelineLayer) || _isIgnoreAnimTimelineGUI);//"AnimationRight2GUI_Timeline_Layers"
			bool isGUI_SameLayer = Editor.IsDelayedGUIVisible(apEditor.DELAYED_UI_TYPE.AnimationRight2GUI_Timeline_Layers);//"AnimationRight2GUI_Timeline_Layers"
			

			if (Event.current.type == EventType.Repaint && Event.current.type != EventType.Ignore)
			{
				_prevSelectedAnimTimeline = _subAnimTimeline;
				_prevSelectedAnimTimelineLayer = _subAnimTimelineLayer;
				_prevSelectedAnimObject = targetObject;

				if (_isIgnoreAnimTimelineGUI)
				{
					_isIgnoreAnimTimelineGUI = false;
				}
			}

			if(_strWrapper_64 == null)
			{
				_strWrapper_64 = new apStringWrapper(64);
			}

			// -----------------------------------------
			if (isGUI_TargetSelected)
			{
				//GUIStyle boxGUIStyle = new GUIStyle(GUI.skin.box);
				//boxGUIStyle.alignment = TextAnchor.MiddleCenter;
				//boxGUIStyle.normal.textColor = apEditorUtil.BoxTextColor;

				if (isAddableType)
				{

					if (isAddable)
					{
						//아직 레이어로 추가가 되지 않았다.
						GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
						//"[" + targetObjectName + "]\nNot Added to Edit"

						//이전
						//GUILayout.Box("[" + targetObjectName + "]\n" + Editor.GetUIWord(UIWORD.NotAddedtoEdit), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));

						//변경
						_strWrapper_64.Clear();
						_strWrapper_64.Append(_guiContent_Right2_Animation_TargetObjectName.Content.text, false);
						_strWrapper_64.Append(apStringFactory.I.Return, false);
						_strWrapper_64.Append(Editor.GetUIWord(UIWORD.NotAddedtoEdit), true);
						GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						

						GUI.backgroundColor = prevColor;

						//"  Add Timeline Layer to Edit"
						//이전
						//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.AddTimelineLayerToEdit), Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddTimeline)), GUILayout.Height(35)))

						//변경
						if (_guiContent_Right_Animation_AddTimelineLayerToEdit == null)
						{
							_guiContent_Right_Animation_AddTimelineLayerToEdit = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.AddTimelineLayerToEdit), Editor.ImageSet.Get(apImageSet.PRESET.Anim_AddTimeline));
						}

						if (GUILayout.Button(_guiContent_Right_Animation_AddTimelineLayerToEdit.Content, apGUILOFactory.I.Height(35)))
						{
							//Debug.LogError("TODO ; Layer 추가하기");
							Editor.Controller.AddAnimTimelineLayer(targetObject, animTimeline);
						}
					}
					else
					{
						//레이어에 이미 있다.
						GUI.backgroundColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
						//"[" + targetObjectName + "]\nSelected"
						//이전
						//GUILayout.Box("[" + targetObjectName + "]\n" + Editor.GetUIWord(UIWORD.Selected), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));

						//변경
						_strWrapper_64.Clear();
						_strWrapper_64.Append(_guiContent_Right2_Animation_TargetObjectName.Content.text, false);
						_strWrapper_64.Append(apStringFactory.I.Return, false);
						_strWrapper_64.Append(Editor.GetUIWord(UIWORD.Selected), true);
						GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						GUI.backgroundColor = prevColor;


						//"  Remove Timeline Layer"
						//이전
						//if (GUILayout.Button(new GUIContent("  " + Editor.GetUIWord(UIWORD.RemoveTimelineLayer),
						//						Editor.ImageSet.Get(apImageSet.PRESET.Anim_RemoveTimelineLayer)
						//						),
						//		GUILayout.Height(24)))

						//변경
						if(_guiContent_Right_Animation_RemoveTimelineLayer == null)
						{
							_guiContent_Right_Animation_RemoveTimelineLayer = apGUIContentWrapper.Make(2, Editor.GetUIWord(UIWORD.RemoveTimelineLayer), Editor.ImageSet.Get(apImageSet.PRESET.Anim_RemoveTimelineLayer));
						}
						

						if (GUILayout.Button(_guiContent_Right_Animation_RemoveTimelineLayer.Content, apGUILOFactory.I.Height(24)))
						{
							//bool isResult = EditorUtility.DisplayDialog("Remove TimelineLayer", "Is Really Remove Timeline Layer?", "Remove", "Cancel");

							bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveTimelineLayer_Title),
															Editor.GetTextFormat(TEXT.RemoveTimelineLayer_Body, animTimelineLayer.DisplayName),
															Editor.GetText(TEXT.Remove),
															Editor.GetText(TEXT.Cancel)
															);

							if (isResult)
							{
								isRemoveTimelineLayer = true;
								removeLayer = animTimelineLayer;
							}
						}
					}
				}
				else
				{
					if (targetObject != null)
					{
						//추가할 수 있는 타입이 아니다.
						GUI.backgroundColor = new Color(1.0f, 0.5f, 0.5f, 1.0f);
						//"[" + targetObjectName + "]\nUnable to be Added"
						//이전
						//GUILayout.Box("[" + targetObjectName + "]\n" + Editor.GetUIWord(UIWORD.NotAbleToBeAdded), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, GUILayout.Width(width), GUILayout.Height(35));
						
						_strWrapper_64.Clear();
						_strWrapper_64.Append(_guiContent_Right2_Animation_TargetObjectName.Content.text, false);
						_strWrapper_64.Append(apStringFactory.I.Return, false);
						_strWrapper_64.Append(Editor.GetUIWord(UIWORD.NotAbleToBeAdded), true);
						GUILayout.Box(_strWrapper_64.ToString(), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						GUI.backgroundColor = prevColor;
					}
					else
					{
						//객체가 없다.
						GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
						//"[" + targetObjectName + "]\nUnable to be Added"
						GUILayout.Box(Editor.GetUIWord(UIWORD.NotAbleToBeAdded), apGUIStyleWrapper.I.Box_MiddleCenter_BoxTextColor, apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(35));

						GUI.backgroundColor = prevColor;
					}
				}


				//EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(10));
				//EditorGUILayout.EndVertical();
				GUILayout.Space(11);



				//EditorGUILayout.LabelField(Editor.GetUIWord(UIWORD.TimelineLayers));//"Timeline Layers"
				_isFoldUI_AnimationTimelineLayers = EditorGUILayout.Foldout(_isFoldUI_AnimationTimelineLayers, Editor.GetUIWord(UIWORD.TimelineLayers));
				if (_isFoldUI_AnimationTimelineLayers)
				{
					GUILayout.Space(8);


					//현재의 타임라인 레이어 리스트를 만들어야한다.
					List<apAnimTimelineLayer> timelineLayers = animTimeline._layers;
					apAnimTimelineLayer curLayer = null;

					//레이어 정보가 Layout 이벤트와 동일한 경우에만 작동

					if (isGUI_SameLayer)
					{
						for (int i = 0; i < timelineLayers.Count; i++)
						{
							Rect lastRect = GUILayoutUtility.GetLastRect();

							curLayer = timelineLayers[i];
							if (animTimelineLayer == curLayer)
							{
								//선택된 레이어다.
								GUI.backgroundColor = new Color(0.9f, 0.7f, 0.7f, 1.0f);
							}
							else
							{
								//선택되지 않은 레이어다.
								GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
							}

							int heightOffset = 18;
							if (i == 0)
							{
								heightOffset = 8;//9
							}

							GUI.Box(new Rect(lastRect.x, lastRect.y + heightOffset, width + 10, 30), apStringFactory.I.None);
							GUI.backgroundColor = prevColor;

							int compWidth = width - (55 + 20 + 5 + 10);

							//GUIStyle guiStyle_Label = new GUIStyle(GUI.skin.label);
							//guiStyle_Label.alignment = TextAnchor.MiddleLeft;

							EditorGUILayout.BeginHorizontal(apGUILOFactory.I.Width(width), apGUILOFactory.I.Height(20));
							GUILayout.Space(10);
							EditorGUILayout.LabelField(curLayer.DisplayName, apGUIStyleWrapper.I.Label_MiddleLeft, apGUILOFactory.I.Width(compWidth), apGUILOFactory.I.Height(20));

							if (animTimelineLayer == curLayer)
							{
								//GUIStyle guiStyle = new GUIStyle(GUI.skin.box);
								//guiStyle.normal.textColor = Color.white;
								//guiStyle.alignment = TextAnchor.UpperCenter;

								GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
								GUILayout.Box(Editor.GetUIWord(UIWORD.Selected), apGUIStyleWrapper.I.Box_UpperCenter_WhiteColor, apGUILOFactory.I.Width(55), apGUILOFactory.I.Height(20));//"Selected"
								GUI.backgroundColor = prevColor;
							}
							else
							{
								if (GUILayout.Button(Editor.GetUIWord(UIWORD.Select), apGUILOFactory.I.Width(55), apGUILOFactory.I.Height(20)))//"Select"
								{
									_isIgnoreAnimTimelineGUI = true;//<깜빡이지 않게..
									SetAnimTimelineLayer(curLayer, true);
								}
							}

							if (GUILayout.Button(Editor.ImageSet.Get(apImageSet.PRESET.Controller_RemoveRecordKey), apGUILOFactory.I.Width(20), apGUILOFactory.I.Height(20)))
							{
								//bool isResult = EditorUtility.DisplayDialog("Remove Timeline Layer", "Remove Timeline Layer?", "Remove", "Cancel");

								bool isResult = EditorUtility.DisplayDialog(Editor.GetText(TEXT.RemoveTimelineLayer_Title),
																		Editor.GetTextFormat(TEXT.RemoveTimelineLayer_Body, curLayer.DisplayName),
																		Editor.GetText(TEXT.Remove),
																		Editor.GetText(TEXT.Cancel)
																		);

								if (isResult)
								{
									isRemoveTimelineLayer = true;
									removeLayer = curLayer;
								}
							}
							EditorGUILayout.EndHorizontal();
							GUILayout.Space(20);
						}
					}
				}
			}


			//----------------------------------
			// 삭제 플래그가 있다.
			if (isRemoveTimelineLayer)
			{
				Editor.Controller.RemoveAnimTimelineLayer(removeLayer);
				SetAnimTimelineLayer(null, true, true);
				SetAnimClipGizmoEvent(true);
			}
			else if (isRemoveTimeline)
			{
				Editor.Controller.RemoveAnimTimeline(animTimeline);
				SetAnimTimeline(null, true);
				SetAnimClipGizmoEvent(true);

			}

		}

		

		private void OnImportAnimClipRetarget(bool isSuccess, object loadKey, apRetarget retargetData, apMeshGroup targetMeshGroup, apAnimClip targetAnimClip, bool isMerge)
		{
			if(!isSuccess 
				|| loadKey != _loadKey_ImportAnimClipRetarget 
				|| retargetData == null 
				|| targetMeshGroup == null
				|| targetAnimClip == null
				|| AnimClip != targetAnimClip
				|| AnimClip == null)
			{
				_loadKey_ImportAnimClipRetarget = null;
				return;
			}

			_loadKey_ImportAnimClipRetarget = null;

			if(AnimClip._targetMeshGroup != targetMeshGroup)
			{
				return;
			}

			//로드를 합시다.
			if(retargetData.IsAnimFileLoaded)
			{
				Editor.Controller.ImportAnimClip(retargetData, targetMeshGroup, targetAnimClip, isMerge);
			}
		}

		/// <summary>
		/// 단축키 [A]로 Anim의 Editing 상태를 토글한다.
		/// </summary>
		/// <param name="paramObject"></param>
		public void OnHotKey_AnimEditingToggle(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null)
			{
				return;
			}

			SetAnimEditingToggle();
		}

		//단축키 [S]로 Anim의 SelectionLock을 토글한다.
		public void OnHotKey_AnimSelectionLockToggle(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null || _exAnimEditingMode == EX_EDIT.None)
			{
				return;
			}
			_isAnimSelectionLock = !_isAnimSelectionLock;
		}


		/// <summary>
		/// 단축키 [D]로 Anim의 LayerLock을 토글한다.
		/// </summary>
		/// <param name="paramObject"></param>
		public void OnHotKey_AnimLayerLockToggle(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.Animation || _animClip == null || _exAnimEditingMode == EX_EDIT.None)
			{
				return;
			}

			SetAnimEditingLayerLockToggle();//Mod Layer Lock을 토글
		}


		private void OnHotKey_AnimAddKeyframe(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.Animation
				|| _animClip == null)
			{
				return;
			}

			//Debug.LogError("TODO : Set Key");
			if (AnimTimelineLayer != null)
			{
				apAnimKeyframe addedKeyframe = Editor.Controller.AddAnimKeyframe(AnimClip.CurFrame, AnimTimelineLayer, true);
				if (addedKeyframe != null)
				{
					//프레임을 이동하자
					_animClip.SetFrame_Editor(addedKeyframe._frameIndex);
					SetAnimKeyframe(addedKeyframe, true, apGizmos.SELECT_TYPE.New);

					//추가 : 자동 스크롤
					AutoSelectAnimTimelineLayer(true, false);

					Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
				}
			}
		}

		private void OnHotKey_AnimMoveFrame(object paramObject)
		{
			if (_selectionType != SELECTION_TYPE.Animation 
				|| _animClip == null 
				)
			{
				Debug.LogError("애니메이션 단축키 처리 실패");
				return;
			}

			if(paramObject is int)
			{
				int iParam = (int)paramObject;

				switch (iParam)
				{
					case 0:
						//Play/Pause Toggle
						{
							if (AnimClip.IsPlaying_Editor)
							{
								// 플레이 -> 일시 정지
								AnimClip.Pause_Editor();
							}
							else
							{
								//마지막 프레임이라면 첫 프레임으로 이동하여 재생한다.
								if (AnimClip.CurFrame == AnimClip.EndFrame)
								{
									AnimClip.SetFrame_Editor(AnimClip.StartFrame);
									Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
								}
								// 일시 정지 -> 플레이
								AnimClip.Play_Editor();
							}

							//Play 전환 여부에 따라서도 WorkKeyframe을 전환한다.
							AutoSelectAnimWorkKeyframe();
							Editor.SetRepaint();
							Editor.Gizmos.SetUpdate();
						}
						break;

					case 1:
						//Move [Prev Frame]
						{
							int prevFrame = AnimClip.CurFrame - 1;
							if (prevFrame < AnimClip.StartFrame)
							{
								if (AnimClip.IsLoop)
								{
									prevFrame = AnimClip.EndFrame;
								}
							}
							AnimClip.SetFrame_Editor(prevFrame);
							AutoSelectAnimWorkKeyframe();

							Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
						}
						break;

					case 2:
						//Move [Next Frame]
						{
							int nextFrame = AnimClip.CurFrame + 1;
							if (nextFrame > AnimClip.EndFrame)
							{
								if (AnimClip.IsLoop)
								{
									nextFrame = AnimClip.StartFrame;
								}
							}
							AnimClip.SetFrame_Editor(nextFrame);
							AutoSelectAnimWorkKeyframe();

							Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
						}
						break;

					case 3:
						//Move [First Frame]
						{
							AnimClip.SetFrame_Editor(AnimClip.StartFrame);
							AutoSelectAnimWorkKeyframe();

							Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
						}
						break;

					case 4:
						//Move [Last Frame]
						{
							AnimClip.SetFrame_Editor(AnimClip.EndFrame);
							AutoSelectAnimWorkKeyframe();

							Editor.SetMeshGroupChanged();//<<추가 : 강제 업데이트를 해야한다.
						}
						break;

					default:
						Debug.LogError("애니메이션 단축키 처리 실패 - 알 수 없는 코드");
						break;
				}
			}
			else
			{
				Debug.LogError("애니메이션 단축키 처리 실패 - 알 수 없는 파라미터");
			}
		}


		private void OnHotKey_AnimCopyKeyframes(object paramObject)
		{
			//Debug.Log("TODO : 키프레임 복사");
			if(AnimClip == null || _subAnimKeyframeList == null || _subAnimKeyframeList.Count == 0)
			{
				return;
			}

			apSnapShotManager.I.Copy_KeyframesOnTimelineUI(AnimClip, _subAnimKeyframeList);

		}

		private void OnHotKey_AnimPasteKeyframes(object paramObject)
		{
			if(AnimClip == null)
			{
				return;
			}
			Editor.Controller.CopyAnimKeyframeFromSnapShot(AnimClip, AnimClip.CurFrame);
			
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// 객체 통계를 다시 계산할 필요가 있을때 호출한다.
		/// </summary>
		public void SetStatisticsRefresh()
		{
			_isStatisticsNeedToRecalculate = true;
		}
		public void CalculateStatistics()
		{
			if(!_isStatisticsNeedToRecalculate)
			{
				//재계산이 필요없으면 생략
				return;
			}
			_isStatisticsNeedToRecalculate = false;

			_isStatisticsAvailable = false;
			_statistics_NumMesh = 0;
			_statistics_NumVert = 0;
			_statistics_NumEdge = 0;
			_statistics_NumTri = 0;
			_statistics_NumClippedMesh = 0;
			_statistics_NumClippedVert = 0;

			_statistics_NumTimelineLayer = -1;
			_statistics_NumKeyframes = -1;
			_statistics_NumBones = 0;

			if(Editor._portrait == null)
			{	
				return;
			}
			
			//apMesh mesh = null;
			//apTransform_Mesh meshTransform = null;
			switch (_selectionType)
			{
				case SELECTION_TYPE.Overall:
					{
						if (_rootUnit == null || _rootUnit._childMeshGroup == null)
						{
							return;
						}

						CalculateStatisticsMeshGroup(_rootUnit._childMeshGroup);

						if (_curRootUnitAnimClip != null)
						{
							_statistics_NumTimelineLayer = 0;
							_statistics_NumKeyframes = 0;

							apAnimTimeline timeline = null;
							for (int i = 0; i < _curRootUnitAnimClip._timelines.Count; i++)
							{
								timeline = _curRootUnitAnimClip._timelines[i];

								_statistics_NumTimelineLayer += timeline._layers.Count;
								for (int iLayer = 0; iLayer < timeline._layers.Count; iLayer++)
								{
									_statistics_NumKeyframes += timeline._layers[iLayer]._keyframes.Count;
								}
							}
						}
					}
					break;

				case SELECTION_TYPE.Mesh:
					{
						if (_mesh == null)
						{
							return;
						}

						_statistics_NumMesh = -1;//<<어차피 1개인데 이건 출력 생략
						_statistics_NumClippedVert = -1;
						_statistics_NumVert = _mesh._vertexData.Count;
						_statistics_NumEdge = _mesh._edges.Count;
						_statistics_NumTri = (_mesh._indexBuffer.Count / 3);
						_statistics_NumBones = 0;
					}
					
					break;

				case SELECTION_TYPE.MeshGroup:
					{
						if (_meshGroup == null)
						{
							return;
						}

						CalculateStatisticsMeshGroup(_meshGroup);
					}
					break;

				case SELECTION_TYPE.Animation:
					{
						if(_animClip == null)
						{
							return;
						}

						if(_animClip._targetMeshGroup == null)
						{
							return;
						}

						CalculateStatisticsMeshGroup(_animClip._targetMeshGroup);

						_statistics_NumTimelineLayer = 0;
						_statistics_NumKeyframes = 0;

						apAnimTimeline timeline = null;
						for (int i = 0; i < _animClip._timelines.Count; i++)
						{
							timeline = _animClip._timelines[i];

							_statistics_NumTimelineLayer += timeline._layers.Count;
							for (int iLayer = 0; iLayer < timeline._layers.Count; iLayer++)
							{
								_statistics_NumKeyframes += timeline._layers[iLayer]._keyframes.Count;
							}
						}
						
					}
					break;

				default:
					return;
			}

			if(_statistics_NumClippedMesh == 0)
			{
				_statistics_NumClippedMesh = -1;
				_statistics_NumClippedVert = -1;
			}

			_isStatisticsAvailable = true;
		}

		private void CalculateStatisticsMeshGroup(apMeshGroup targetMeshGroup)
		{
			if (targetMeshGroup == null)
			{
				return;
			}

			apMesh mesh = null;
			apTransform_Mesh meshTransform = null;

			for (int i = 0; i < targetMeshGroup._childMeshTransforms.Count; i++)
			{
				meshTransform = targetMeshGroup._childMeshTransforms[i];
				if (meshTransform == null)
				{
					continue;
				}

				mesh = meshTransform._mesh;
				if (mesh == null)
				{
					continue;
				}
				_statistics_NumMesh += 1;
				_statistics_NumVert += mesh._vertexData.Count;
				_statistics_NumEdge += mesh._edges.Count;
				_statistics_NumTri += (mesh._indexBuffer.Count / 3);

				//클리핑이 되는 경우 Vert를 따로 계산해준다.
				//Parent도 같이 포함한다. (렌더링은 같이 되므로)
				if (meshTransform._isClipping_Child)
				{
					_statistics_NumClippedMesh +=1;
					_statistics_NumClippedVert += mesh._vertexData.Count;

					if(meshTransform._clipParentMeshTransform != null &&
						meshTransform._clipParentMeshTransform._mesh != null)
					{
						_statistics_NumClippedVert += meshTransform._clipParentMeshTransform._mesh._vertexData.Count;
					}
				}
			}

			//추가 19.12.25 : 본 개수도 표시
			_statistics_NumBones += targetMeshGroup._boneList_All.Count;

			//Child MeshGroupTransform이 있으면 재귀적으로 호출하자
			for (int i = 0; i < targetMeshGroup._childMeshGroupTransforms.Count; i++)
			{
				CalculateStatisticsMeshGroup(targetMeshGroup._childMeshGroupTransforms[i]._meshGroup);
			}
		}

		//_isStatisticsAvailable = false;
		//	_statistics_NumMesh = 0;
		//	_statistics_NumVert = 0;
		//	_statistics_NumEdge = 0;
		//	_statistics_NumTri = 0;
		//	_statistics_NumClippedVert = 0;

		//	_statistics_NumTimelineLayer = -1;
		//	_statistics_NumKeyframes = -1;

		public bool IsStatisticsCalculated		{  get { return _isStatisticsAvailable; } }
		public int Statistics_NumMesh			{  get { return _statistics_NumMesh; } }
		public int Statistics_NumVertex			{  get { return _statistics_NumVert; } }
		public int Statistics_NumEdge			{  get { return _statistics_NumEdge; } }
		public int Statistics_NumTri			{  get { return _statistics_NumTri; } }
		public int Statistics_NumClippedMesh	{  get { return _statistics_NumClippedMesh; } }
		public int Statistics_NumClippedVertex	{  get { return _statistics_NumClippedVert; } }
		public int Statistics_NumTimelineLayer	{  get { return _statistics_NumTimelineLayer; } }
		public int Statistics_NumKeyframe		{  get { return _statistics_NumKeyframes; } }
		public int Statistics_NumBone			{  get { return _statistics_NumBones; } }


		public bool IsSelectionLockGUI
		{
			get
			{
				if(_selectionType == SELECTION_TYPE.Animation)
				{
					return IsAnimSelectionLock;
				}
				else if(_selectionType == SELECTION_TYPE.MeshGroup)
				{
					return IsSelectionLock;
				}
				return false;
			}
		}

		// 추가 : Bone IK Update/Rendering 옵션을 계산한다.
		//---------------------------------------------------------------------------------------------
		/// <summary>
		/// Bone의 IK Matrix를 업데이트할 수 있는가
		/// </summary>
		/// <returns></returns>
		public bool IsBoneIKMatrixUpdatable
		{
			get
			{
				switch (_selectionType)
				{
					case SELECTION_TYPE.Overall:
						return true;

					case SELECTION_TYPE.MeshGroup:
						if (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Modifier)
						{
							if (Modifier != null)
							{
								if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
								{
									//리깅 모디파이어 타입인 경우
									//그냥 IK 불가
									return false;
								}
								else
								{
									if (ExEditingMode == EX_EDIT.None)
									{
										//편집 모드가 아닐땐 True
										return true;
									}
								}
							}
						}
						else if (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Setting)
						{
							return true;
						}
						//else if(Editor.GetModLockOption_BonePreview(ExEditingMode))
						//{
						//	//편집모드 일때 + Bone Preview 모드일때 
						//	return true;
						//}
						return false;
					case SELECTION_TYPE.Animation:
						if (ExAnimEditingMode == EX_EDIT.None)
						{
							// 편집 모드가 아닐 때
							return true;
						}
						//else if(Editor.GetModLockOption_BonePreview(ExAnimEditingMode))
						//{
						//	//편집모드 일때 + Bone Preview 모드일때 
						//	return true;
						//}
						return false;
				}
				//그 외에는 False
				return false;
			}
		}

		/// <summary>
		/// Bone의 IK 계산이 Rigging에 적용되어야 하는가
		/// </summary>
		/// <returns></returns>
		public bool IsBoneIKRiggingUpdatable
		{
			get
			{
				switch (_selectionType)
				{
					case SELECTION_TYPE.Overall:
						return true;

					case SELECTION_TYPE.MeshGroup:
						if (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Modifier)
						{
							if (Modifier != null)
							{
								if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
								{
									//리깅 모디파이어 타입인 경우
									//그냥 IK 불가
									return false;
								}
								else
								{
									if (ExEditingMode == EX_EDIT.None)
									{
										//편집 모드가 아닐땐 True
										return true;
									}
								}
							}
						}
						else if (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Setting)
						{
							return true;
						}
						return false;

					case SELECTION_TYPE.Animation:
						if (ExAnimEditingMode == EX_EDIT.None)
						{
							// 편집 모드가 아닐 때
							return true;
						}
						return false;
				}
				//그 외에는 False
				return false;
			}
		}


		/// <summary>
		/// IK가 적용된 Bone이 렌더링 되는 경우 (아웃라인은 아니다)
		/// 작업 중일 때에는 렌더링이 되면 안되는 것이 원칙
		/// </summary>
		/// <returns></returns>
		public bool IsBoneIKRenderable
		{
			get
			{
				switch (_selectionType)
				{
					case SELECTION_TYPE.Overall:
						return true;

					case SELECTION_TYPE.MeshGroup:
						if (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Modifier)
						{
							if (Modifier != null)
							{
								if (Modifier.ModifierType == apModifierBase.MODIFIER_TYPE.Rigging)
								{
									//리깅 모디파이어 타입인 경우
									//그냥 IK 불가
									return false;
								}
								else
								{
									if (ExEditingMode == EX_EDIT.None)
									{
										//편집 모드가 아닐땐 True
										return true;
									}
								}
							}
						}
						else if (Editor._meshGroupEditMode == apEditor.MESHGROUP_EDIT_MODE.Setting)
						{
							//Setting에서도 True
							return true;
						}

						//else if(Editor.GetModLockOption_BonePreview(ExEditingMode))
						//{
						//	//편집모드 일때 + Bone Preview 모드일때 
						//	return true;
						//}
						return false;
					case SELECTION_TYPE.Animation:
						if (ExAnimEditingMode == EX_EDIT.None)
						{
							// 편집 모드가 아닐 때
							return true;
						}
						//else if(Editor.GetModLockOption_BonePreview(ExAnimEditingMode))
						//{
						//	//편집모드 일때 + Bone Preview 모드일때 
						//	return true;
						//}
						return false;
				}
				//그 외에는 False
				return false;
			}
		}
		//--------------------------------------------------------------------------------------------
		public class RestoredResult
		{
			public bool _isAnyRestored = false;
			public bool _isRestoreToAdded = false;//삭제된 것이 복원되었다.
			public bool _isRestoreToRemoved = false;//추가되었던 것이 다시 없어졌다.

			public apTextureData _restoredTextureData = null;
			public apMesh _restoredMesh = null;
			public apMeshGroup _restoredMeshGroup = null;
			public apAnimClip _restoredAnimClip = null;
			public apControlParam _restoredControlParam = null;
			public apModifierBase _restoredModifier = null;

			public SELECTION_TYPE _changedType = SELECTION_TYPE.None;

			private static RestoredResult _instance = null;
			public static RestoredResult I { get { if(_instance == null) { _instance = new RestoredResult(); } return _instance; } }

			private RestoredResult()
			{

			}

			public void Init()
			{
				_isAnyRestored = false;
				_isRestoreToAdded = false;//삭제된 것이 복원되었다.
				_isRestoreToRemoved = false;//추가되었던 것이 다시 없어졌다.

				_restoredTextureData = null;
				_restoredMesh = null;
				_restoredMeshGroup = null;
				_restoredAnimClip = null;
				_restoredControlParam = null;
				_restoredModifier = null;

				_changedType = SELECTION_TYPE.None;
			}
		}

		

		/// <summary>
		/// Editor에서 Undo가 수행될 때, Undo 직전의 상태를 확인하여 자동으로 페이지를 전환한다.
		/// RestoredResult를 리턴한다.
		/// </summary>
		/// <param name="portrait"></param>
		/// <param name="recordList_TextureData"></param>
		/// <param name="recordList_Mesh"></param>
		/// <param name="recordList_MeshGroup"></param>
		/// <param name="recordList_AnimClip"></param>
		/// <param name="recordList_ControlParam"></param>
		/// <returns></returns>
		public RestoredResult SetAutoSelectWhenUndoPerformed(		apPortrait portrait,
																	List<int> recordList_TextureData,
																	List<int> recordList_Mesh,
																	//List<int> recordList_MeshGroup,
																	List<int> recordList_AnimClip,
																	List<int> recordList_ControlParam,
																	List<int> recordList_Modifier,
																	List<int> recordList_AnimTimeline,
																	List<int> recordList_AnimTimelineLayer,
																	//List<int> recordList_Transform,
																	List<int> recordList_Bone,
																	Dictionary<int, List<int>> recordList_MeshGroupAndTransform,
																	Dictionary<int, int> recordList_AnimClip2TargetMeshGroup,//<<추가
																	bool isStructChanged)
		{
			//추가. 만약 개수가 변경된 경우, 그것이 삭제 되거나 추가된 경우이다.
			//Prev  <-- ( Undo ) --- Next
			// 있음        <-        없음 : 삭제된 것이 복원 되었다. 해당 메뉴를 찾아서 이동해야한다.
			// 없음        <-        있음 : 새로 추가되었다. 

			RestoredResult.I.Init();

			if(portrait == null)
			{
				return RestoredResult.I;
			}

			
			//개수로 체크하면 빠르다.

			//1. 텍스쳐
			if (portrait._textureData != null && portrait._textureData.Count != recordList_TextureData.Count)
			{
				//텍스쳐 리스트와 개수가 다른 경우
				if (portrait._textureData.Count > recordList_TextureData.Count)
				{
					//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
					RestoredResult.I._isRestoreToAdded = true;
					RestoredResult.I._changedType = SELECTION_TYPE.ImageRes;

					
					//복원된 것을 찾자
					for (int i = 0; i < portrait._textureData.Count; i++)
					{
						int uniqueID = portrait._textureData[i]._uniqueID;
						if(!recordList_TextureData.Contains(uniqueID))
						{
							//Undo 전에 없었던 ID이다. 새로 추가되었다.
							RestoredResult.I._restoredTextureData = portrait._textureData[i];
							break;
						}
					}
				}
				else
				{
					//Restored < Record : 추가되었던 것이 삭제되었다.
					RestoredResult.I._isRestoreToRemoved = true;
					RestoredResult.I._changedType = SELECTION_TYPE.ImageRes;
				}
			}

			//2. 메시
			if (portrait._meshes != null)
			{
				//실제 Monobehaviour를 체크
				if(portrait._subObjectGroup_Mesh != null)
				{
					//Unity에서 제공하는 childMeshes를 기준으로 동기화를 해야한다.
					apMesh[] childMeshes = portrait._subObjectGroup_Mesh.GetComponentsInChildren<apMesh>();

					int nMeshesInList = 0;
					int nMeshesInGameObj = 0;
					for (int i = 0; i < portrait._meshes.Count; i++)
					{
						if (portrait._meshes[i] != null)
						{
							nMeshesInList++;
						}
					}
					if(portrait._meshes.Count != nMeshesInList)
					{
						//실제 데이터와 다르다. (Null이 포함되어 있다.)
						portrait._meshes.RemoveAll(delegate(apMesh a)
						{
							return a == null;
						});
					}

					if(childMeshes == null)
					{
						//Debug.LogError("Child Mesh가 없다.");
					}
					else
					{
						//Debug.LogError("Child Mesh의 개수 [" + childMeshes.Length + "] / 리스트 데이터 상의 개수 [" + portrait._meshes.Count + "]");
						nMeshesInGameObj = childMeshes.Length;
					}

					if(nMeshesInList != nMeshesInGameObj)
					{
						if(nMeshesInGameObj > 0)
						{
							for (int i = 0; i < childMeshes.Length; i++)
							{
								apMesh childMesh = childMeshes[i];
								if(!portrait._meshes.Contains(childMesh))
								{
									portrait._meshes.Add(childMesh);
								}
							}
						}
					}
				}

				if (portrait._meshes.Count != recordList_Mesh.Count)
				{
					//Mesh 리스트와 개수가 다른 경우
					if (portrait._meshes.Count > recordList_Mesh.Count)
					{
						//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
						RestoredResult.I._isRestoreToAdded = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Mesh;

						//복원된 것을 찾자
						for (int i = 0; i < portrait._meshes.Count; i++)
						{
							int uniqueID = portrait._meshes[i]._uniqueID;
							if (!recordList_Mesh.Contains(uniqueID))
							{
								//Undo 전에 없었던 ID이다. 새로 추가되었다.
								RestoredResult.I._restoredMesh = portrait._meshes[i];
								break;
							}
						}
					}
					else
					{
						//Restored < Record : 추가되었던 것이 삭제되었다.
						RestoredResult.I._isRestoreToRemoved = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Mesh;
					}
				}
			}


			//3. 메시 그룹
			if (portrait._meshGroups != null)
			{
				if (portrait._subObjectGroup_MeshGroup != null)
				{
					//Unity에서 제공하는 childMeshGroups를 기준으로 동기화를 해야한다.
					apMeshGroup[] childMeshGroups = portrait._subObjectGroup_MeshGroup.GetComponentsInChildren<apMeshGroup>();

					int nMeshGroupsInList = 0;
					int nMeshGroupsInGameObj = 0;
					for (int i = 0; i < portrait._meshGroups.Count; i++)
					{
						if(portrait._meshGroups[i] != null)
						{
							nMeshGroupsInList++;
						}
					}
					if(portrait._meshGroups.Count != nMeshGroupsInList)
					{
						//실제 데이터와 다르다. (Null이 포함되어 있다.)
						portrait._meshGroups.RemoveAll(delegate(apMeshGroup a)
						{
							return a == null;
						});
					}

					if(childMeshGroups != null)
					{
						nMeshGroupsInGameObj = childMeshGroups.Length;
					}

					if(nMeshGroupsInList != nMeshGroupsInGameObj)
					{
						if(nMeshGroupsInGameObj > 0)
						{
							for (int i = 0; i < childMeshGroups.Length; i++)
							{
								apMeshGroup childMeshGroup = childMeshGroups[i];
								if(!portrait._meshGroups.Contains(childMeshGroup))
								{
									portrait._meshGroups.Add(childMeshGroup);
								}
							}
						}
					}

				}

				//이전
				//if (portrait._meshGroups.Count != recordList_MeshGroup.Count)
				//{
				//	//메시 그룹 리스트와 다른 경우
				//	if (portrait._meshGroups.Count > recordList_MeshGroup.Count)
				//	{
				//		//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
				//		RestoredResult.I._isRestoreToAdded = true;
				//		RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;

						
				//		//복원된 것을 찾자
				//		for (int i = 0; i < portrait._meshGroups.Count; i++)
				//		{
				//			int uniqueID = portrait._meshGroups[i]._uniqueID;
				//			if (!recordList_MeshGroup.Contains(uniqueID))
				//			{
				//				//Undo 전에 없었던 ID이다. 새로 추가되었다.
				//				RestoredResult.I._restoredMeshGroup = portrait._meshGroups[i];
				//				break;
				//			}
				//		}
				//	}
				//	else
				//	{
				//		//Restored < Record : 추가되었던 것이 삭제되었다.
				//		RestoredResult.I._isRestoreToRemoved = true;
				//		RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
				//	}
				//}

				//변경 20.1.28
				if (portrait._meshGroups.Count != recordList_MeshGroupAndTransform.Count)
				{
					//메시 그룹 리스트와 다른 경우
					if (portrait._meshGroups.Count > recordList_MeshGroupAndTransform.Count)
					{
						//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
						RestoredResult.I._isRestoreToAdded = true;
						RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;

						
						//복원된 것을 찾자
						for (int i = 0; i < portrait._meshGroups.Count; i++)
						{
							int uniqueID = portrait._meshGroups[i]._uniqueID;
							if (!recordList_MeshGroupAndTransform.ContainsKey(uniqueID))
							{
								//Undo 전에 없었던 ID이다. 새로 추가되었다.
								RestoredResult.I._restoredMeshGroup = portrait._meshGroups[i];
								break;
							}
						}
					}
					else
					{
						//Restored < Record : 추가되었던 것이 삭제되었다.
						RestoredResult.I._isRestoreToRemoved = true;
						RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
					}
				}
				else
				{
					//MeshGroup과 비교를 했으며, 만약 개수에 변화가 없다면
					//Transform과 비교를 하자
					apMeshGroup curMeshGroup = null;
					List<int> transforms = null;
					int nTransform_Recorded = 0;
					int nTransform_Current = 0;
					for (int iMG = 0; iMG < portrait._meshGroups.Count; iMG++)
					{
						curMeshGroup = portrait._meshGroups[iMG];
						if (!recordList_MeshGroupAndTransform.ContainsKey(curMeshGroup._uniqueID))
						{
							//만약 개수가 같지만 모르는 메시 그룹이 나왔을 경우
							RestoredResult.I._isRestoreToAdded = true;
							RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
							RestoredResult.I._restoredMeshGroup = curMeshGroup;
							break;
						}
						//Transform을 비교하자
						transforms = recordList_MeshGroupAndTransform[curMeshGroup._uniqueID];
						int nMeshTransform = curMeshGroup._childMeshTransforms == null ? 0 : curMeshGroup._childMeshTransforms.Count;
						int nMeshGroupTransform = curMeshGroup._childMeshGroupTransforms == null ? 0 : curMeshGroup._childMeshGroupTransforms.Count;
						
						nTransform_Current = nMeshTransform + nMeshGroupTransform;
						nTransform_Recorded = transforms.Count;

						if(nTransform_Current > nTransform_Recorded)
						{
							//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
							RestoredResult.I._isRestoreToAdded = true;
							RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
							break;
						}
						else if(nTransform_Current < nTransform_Recorded)
						{
							//Restored < Record : 추가되었던 것이 삭제되었다.
							RestoredResult.I._isRestoreToRemoved = true;
							RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
							break;
						}
					}
					
				}
			}


			//4. 애니메이션 클립
			if (portrait._animClips != null)
			{
				if (portrait._animClips.Count != recordList_AnimClip.Count)
				{
					//Anim 리스트와 개수가 다른 경우
					if (portrait._animClips.Count > recordList_AnimClip.Count)
					{
						//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
						RestoredResult.I._isRestoreToAdded = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Animation;

						
						//복원된 것을 찾자
						for (int i = 0; i < portrait._animClips.Count; i++)
						{
							int uniqueID = portrait._animClips[i]._uniqueID;
							if (!recordList_AnimClip.Contains(uniqueID))
							{
								//Undo 전에 없었던 ID이다. 새로 추가되었다.
								RestoredResult.I._restoredAnimClip = portrait._animClips[i];
								break;
							}
						}
					}
					else
					{
						//Restored < Record : 추가되었던 것이 삭제되었다.
						RestoredResult.I._isRestoreToRemoved = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Animation;
					}
				}
				else
				{
					//만약, AnimClip 개수는 변동이 없는데, 타임라인 개수에 변동이 있다면
					//최소한 Refresh는 해야한다.
					int nTimeline = 0;
					int nTimelineLayer = 0;
					apAnimClip animClip = null;
					
					//추가 20.3.19 : TargetMeshGroup이 바뀌었다면?
					bool isTargetMeshGroupChanged = false;
					apAnimClip targetChangedAnimClip = null;
					

					for (int iAnimClip = 0; iAnimClip < portrait._animClips.Count; iAnimClip++)
					{
						animClip = portrait._animClips[iAnimClip];
						nTimeline += animClip._timelines.Count;

						for (int iTimeline = 0; iTimeline < animClip._timelines.Count; iTimeline++)
						{
							nTimelineLayer += animClip._timelines[iTimeline]._layers.Count;
						}

						if(recordList_AnimClip2TargetMeshGroup.ContainsKey(animClip._uniqueID))
						{
							int recLinkedTargetMeshGroupID = recordList_AnimClip2TargetMeshGroup[animClip._uniqueID];
							int curLinkedTargetMeshGroupID = (animClip._targetMeshGroup != null ? animClip._targetMeshGroup._uniqueID : -1);
							if(recLinkedTargetMeshGroupID != curLinkedTargetMeshGroupID)
							{
								//연결된 MeshGroup이 변경되었다.
								isTargetMeshGroupChanged = true;
								targetChangedAnimClip = animClip;

								//Debug.LogError("AnimClip [" + targetChangedAnimClip._name + "]과 연결된 메시 그룹이 변경되었다.");
							}
						}
						else
						{
							isTargetMeshGroupChanged = true;
							targetChangedAnimClip = animClip;
						}
					}

					if(nTimeline > recordList_AnimTimeline.Count
						|| nTimelineLayer > recordList_AnimTimelineLayer.Count)
					{
						//타임라인이나 타임라인 레이어가 증가했다.
						RestoredResult.I._isRestoreToAdded = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Animation;
					}
					else if(nTimeline < recordList_AnimTimeline.Count
							|| nTimelineLayer < recordList_AnimTimelineLayer.Count)
					{
						//Restored < Record : 추가되었던 것이 삭제되었다.
						RestoredResult.I._isRestoreToRemoved = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Animation;
					}
					else if(isTargetMeshGroupChanged)
					{
						//변경 내역이 있는 AnimClip이다.
						RestoredResult.I._restoredAnimClip = targetChangedAnimClip;
						isStructChanged = true;
						RestoredResult.I._changedType = SELECTION_TYPE.Animation;
					}
				}
				
			}


			//5. 컨트롤 파라미터
			if (portrait._controller._controlParams != null 
				&& portrait._controller._controlParams.Count != recordList_ControlParam.Count)
			{
				//Param 리스트와 개수가 다른 경우
				if (portrait._controller._controlParams.Count > recordList_ControlParam.Count)
				{
					//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
					RestoredResult.I._isRestoreToAdded = true;
					RestoredResult.I._changedType = SELECTION_TYPE.Param;

					//복원된 것을 찾자
					for (int i = 0; i < portrait._controller._controlParams.Count; i++)
					{
						int uniqueID = portrait._controller._controlParams[i]._uniqueID;
						if(!recordList_ControlParam.Contains(uniqueID))
						{
							//Undo 전에 없었던 ID이다. 새로 추가되었다.
							RestoredResult.I._restoredControlParam = portrait._controller._controlParams[i];
							break;
						}
					}
				}
				else
				{
					//Restored < Record : 추가되었던 것이 삭제되었다.
					RestoredResult.I._isRestoreToRemoved = true;
					RestoredResult.I._changedType = SELECTION_TYPE.Param;
				}
			}

			//6. 모디파이어 > TODO
			if(RestoredResult.I._changedType != SELECTION_TYPE.MeshGroup)
			{
				//MeshGroup에서 복원 기록이 없는 경우에 한해서 Modifier의 추가가 있었는지 확인한다.
				//MeshGroup의 복원 기록이 있다면 Modifier는 자동으로 포함되기 때문
				//모든 모디파이어를 모아야 한다.
				List<apModifierBase> allModifiers = new List<apModifierBase>();

				apMeshGroup meshGroup = null;
				apModifierBase modifier = null;

				for (int iMG = 0; iMG < portrait._meshGroups.Count; iMG++)
				{
					meshGroup = portrait._meshGroups[iMG];
					if(meshGroup == null)
					{
						continue;
					}

					for (int iMod = 0; iMod < meshGroup._modifierStack._modifiers.Count; iMod++)
					{
						modifier = meshGroup._modifierStack._modifiers[iMod];
						if(modifier == null)
						{
							continue;
						}
						allModifiers.Add(modifier);
					}
				}

				//이제 실제 포함된 Modifier를 비교해야한다.
				//이건 데이터 누락이 있을 수 있다.
				if(portrait._subObjectGroup_Modifier != null)
				{
					//Unity에서 제공하는 childModifer기준으로 동기화를 해야한다.
					apModifierBase[] childModifiers = portrait._subObjectGroup_Modifier.GetComponentsInChildren<apModifierBase>();

					int nModInList = allModifiers.Count;
					int nModInGameObj = 0;
					
					if(childModifiers != null)
					{
						nModInGameObj = childModifiers.Length;
					}

					if(nModInList != nModInGameObj)
					{
						if(nModInGameObj > 0)
						{
							for (int i = 0; i < childModifiers.Length; i++)
							{
								apModifierBase childModifier = childModifiers[i];
								//이제 어느 MeshGroup의 Modifier인지 찾아야 한다 ㅜㅜ

								if(childModifier._meshGroup == null)
								{
									//연결이 안되었다면 찾자
									int meshGroupUniqueID = childModifier._meshGroupUniqueID;
									childModifier._meshGroup = portrait.GetMeshGroup(meshGroupUniqueID);
								}

								if(childModifier._meshGroup != null)
								{
									if(!childModifier._meshGroup._modifierStack._modifiers.Contains(childModifier))
									{
										childModifier._meshGroup._modifierStack._modifiers.Add(childModifier);
									}

									//체크용 allModifiers 리스트에도 넣자
									if (!allModifiers.Contains(childModifier))
									{
										allModifiers.Add(childModifier);
									}

								}

							}
						}
					}
				}

				if(allModifiers.Count != recordList_Modifier.Count)
				{
					//모디파이어 리스트와 다른 경우 => 뭔가 복원 되었거나 삭제된 것이다.
					
					if(allModifiers.Count > recordList_Modifier.Count)
					{
						//Restored > Record : 삭제된 것이 다시 복원되어 추가 되었다.
						RestoredResult.I._isRestoreToAdded = true;
						RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;

						//복원된 것을 찾자
						for (int i = 0; i < allModifiers.Count; i++)
						{
							int uniqueID = allModifiers[i]._uniqueID;
							if(!recordList_Modifier.Contains(uniqueID))
							{
								RestoredResult.I._restoredModifier = allModifiers[i];
								break;
							}
						}
					}
					else
					{
						//Restored < Record : 추가되었던 것이 삭제되었다.
						RestoredResult.I._isRestoreToRemoved = true;
						RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
					}
				}
			}

			//7. RootUnit -> MeshGroup의 변동 사항이 없다면 RootUnit을 체크해볼 필요가 있다.
			if(RestoredResult.I._changedType != SELECTION_TYPE.MeshGroup)
			{
				//RootUnit의 ID와 실제 RootUnit이 같은지 확인한다.
				int nRootUnit = portrait._rootUnits.Count;
				int nMainMeshGroup = portrait._mainMeshGroupList.Count;
				int nMainMeshGroupID = portrait._mainMeshGroupIDList.Count;

				if (nRootUnit != nMainMeshGroup ||
					nMainMeshGroup != nMainMeshGroupID ||
					nRootUnit != nMainMeshGroupID)
				{
					//3개의 값이 다르다.
					//ID를 기준으로 하자
					if(nRootUnit < nMainMeshGroupID ||
						nMainMeshGroup < nMainMeshGroupID ||
						nRootUnit < nMainMeshGroup)
					{
						//ID가 더 많다. -> 복원할게 있다.
						RestoredResult.I._changedType = SELECTION_TYPE.Overall;
						RestoredResult.I._isRestoreToAdded = true;
					}
					else
					{
						//ID가 더 적다. -> 삭제할게 있다.
						RestoredResult.I._changedType = SELECTION_TYPE.Overall;
						RestoredResult.I._isRestoreToRemoved = true;
					}
				}
				else
				{
					//개수는 같은데, 데이터가 빈게 있나.. 아니면 다를수도
					apRootUnit rootUnit = null;
					apMeshGroup mainMeshGroup = null;
					int mainMeshGroupID = -1;
					for (int i = 0; i < nRootUnit; i++)
					{
						rootUnit = portrait._rootUnits[i];
						mainMeshGroup = portrait._mainMeshGroupList[i];
						mainMeshGroupID = portrait._mainMeshGroupIDList[i];

						if(rootUnit == null || mainMeshGroup == null)
						{
							//데이터가 없다.
							if(mainMeshGroupID >= 0)
							{
								//유효한 ID가 있다. -> 복원할게 있다.
								RestoredResult.I._changedType = SELECTION_TYPE.Overall;
								RestoredResult.I._isRestoreToAdded = true;
							}
							else
							{
								//유효하지 않는 ID와 데이터가 있다. -> 삭제할 게 있다.
								RestoredResult.I._changedType = SELECTION_TYPE.Overall;
								RestoredResult.I._isRestoreToRemoved = true;
							}
						}
						else if(rootUnit._childMeshGroup == null 
							|| rootUnit._childMeshGroup != mainMeshGroup
							|| rootUnit._childMeshGroup._uniqueID != mainMeshGroupID)
						{
							//데이터가 맞지 않다.
							//삭제인지 추가인지 모르지만 일단 갱신 필요
							RestoredResult.I._changedType = SELECTION_TYPE.Overall;
							RestoredResult.I._isRestoreToRemoved = true;
						}
					}
				}
			}

			if (!RestoredResult.I._isRestoreToAdded && !RestoredResult.I._isRestoreToRemoved)
			{
				//MeshGroup의 변동이 없을 때
				//-> 1. Transform에 변동이 있는가
				//-> 2. Bone에 변동이 있는가
				//만약, MeshGroup은 그대로지만, Trasnform이 다른 경우 -> 갱신 필요
				//변경, Transform은 위에서 체크했으므로, 여기의 코드는 생략한다. (20.1.28)
				//List<int> allTransforms = new List<int>();
				List<int> allBones = new List<int>();
				for (int iMSG = 0; iMSG < portrait._meshGroups.Count; iMSG++)
				{
					apMeshGroup meshGroup = portrait._meshGroups[iMSG];
					//for (int iMeshTF = 0; iMeshTF < meshGroup._childMeshTransforms.Count; iMeshTF++)
					//{
					//	allTransforms.Add(meshGroup._childMeshTransforms[iMeshTF]._transformUniqueID);
					//}
					//for (int iMeshGroupTF = 0; iMeshGroupTF < meshGroup._childMeshGroupTransforms.Count; iMeshGroupTF++)
					//{
					//	allTransforms.Add(meshGroup._childMeshGroupTransforms[iMeshGroupTF]._transformUniqueID);
					//}
					//<BONE_EDIT> 모든 Bone이므로 수정하지 않음
					for (int iBone = 0; iBone < meshGroup._boneList_All.Count; iBone++)
					{
						allBones.Add(meshGroup._boneList_All[iBone]._uniqueID);
					}
				}

				//1. Transform 체크
				//if(allTransforms.Count != recordList_Transform.Count)
				//{
				//	//Transform 개수가 Undo를 전후로 바뀌었다.
				//	if(allTransforms.Count > recordList_Transform.Count)
				//	{
				//		//삭제 -> 복원
				//		RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
				//		RestoredResult.I._isRestoreToAdded = true;
				//	}
				//	else
				//	{
				//		//추가 -> 삭제
				//		RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
				//		RestoredResult.I._isRestoreToRemoved = true;
				//	}
				//}

				//2. Bone 체크
				if(allBones.Count != recordList_Bone.Count)
				{
					//Bone 개수가 Undo를 전후로 바뀌었다.
					if(allBones.Count > recordList_Bone.Count)
					{
						//삭제 -> 복원
						RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
						RestoredResult.I._isRestoreToAdded = true;
					}
					else
					{
						//추가 -> 삭제
						RestoredResult.I._changedType = SELECTION_TYPE.MeshGroup;
						RestoredResult.I._isRestoreToRemoved = true;
					}
				}

			}


			if (!RestoredResult.I._isRestoreToAdded 
				&& !RestoredResult.I._isRestoreToRemoved
				&& !isStructChanged//추가 20.1.21
				)
			{
				RestoredResult.I._isAnyRestored = false;
			}
			else
			{
				RestoredResult.I._isAnyRestored = true;
			}

			return RestoredResult.I;
			
		}

		public void SetAutoSelectOrUnselectFromRestore(RestoredResult restoreResult, apPortrait portrait)
		{
			if (!restoreResult._isRestoreToAdded && !restoreResult._isRestoreToRemoved)
			{
				//아무것도 바뀐게 없다면
				return;
			}

			if (restoreResult._isRestoreToAdded)
			{
				// 삭제 -> 복원해서 새로운게 생겼을 경우 : 그걸 선택해야한다.
				switch (restoreResult._changedType)
				{
					case SELECTION_TYPE.ImageRes:
						if (restoreResult._restoredTextureData != null)
						{
							SetImage(restoreResult._restoredTextureData);
						}
						break;

					case SELECTION_TYPE.Mesh:
						if (restoreResult._restoredMesh != null)
						{
							SetMesh(restoreResult._restoredMesh);
						}
						break;

					case SELECTION_TYPE.MeshGroup:
						if (restoreResult._restoredMeshGroup != null)
						{
							SetMeshGroup(restoreResult._restoredMeshGroup);
						}
						else if(restoreResult._restoredModifier != null)
						{
							if(restoreResult._restoredModifier._meshGroup != null)
							{
								SetMeshGroup(restoreResult._restoredModifier._meshGroup);
							}
						}
						break;

					case SELECTION_TYPE.Animation:
						if (restoreResult._restoredAnimClip != null)
						{
							SetAnimClip(restoreResult._restoredAnimClip);
						}
						break;

					case SELECTION_TYPE.Param:
						if (restoreResult._restoredControlParam != null)
						{
							SetParam(restoreResult._restoredControlParam);
						}
						break;

					case SELECTION_TYPE.Overall:
						//RootUnit은 새로 복원되어도 별도의 행동을 취하지 않는다.
						break;

					default:
						//뭐징..
						restoreResult.Init();
						return;
				}
			}

			if (restoreResult._isRestoreToRemoved)
			{
				// 추가 -> 취소해서 삭제되었을 경우 : 타입을 보고 해당 페이지의 것이 이미 사라진 것인지 확인
				//페이지를 나와야 한다.
				bool isRemovedPage = false;
				if (SelectionType == restoreResult._changedType)
				{
					switch (restoreResult._changedType)
					{
						case SELECTION_TYPE.ImageRes:
							if (_image != null)
							{
								if (!portrait._textureData.Contains(_image))
								{
									//삭제되어 없는 이미지를 가리키고 있다.
									isRemovedPage = true;
								}
							}
							break;

						case SELECTION_TYPE.Mesh:
							if (_mesh != null)
							{
								if (!portrait._meshes.Contains(_mesh))
								{
									//삭제되어 없는 메시를 가리키고 있다.
									isRemovedPage = true;
								}
							}
							break;

						case SELECTION_TYPE.MeshGroup:
							if (_meshGroup != null)
							{
								if (!portrait._meshGroups.Contains(_meshGroup))
								{
									//삭제되어 없는 메시 그룹을 가리키고 있다.
									isRemovedPage = true;
								}
							}
							break;

						case SELECTION_TYPE.Animation:
							if (_animClip != null)
							{
								if (!portrait._animClips.Contains(_animClip))
								{
									//삭제되어 없는 AnimClip을 가리키고 있다.
									isRemovedPage = true;
								}
							}
							break;

						case SELECTION_TYPE.Param:
							if (_param != null)
							{
								if (!portrait._controller._controlParams.Contains(_param))
								{
									//삭제되어 없는 Param을 가리키고 있다.
									isRemovedPage = true;
								}
							}
							break;

						case SELECTION_TYPE.Overall:
							{
								if(_rootUnit != null)
								{
									if(!portrait._rootUnits.Contains(_rootUnit))
									{
										isRemovedPage = true;
									}
								}
							}
							break;
						
						default:
							//뭐징..
							restoreResult.Init();
							return;
					}
				}

				if (isRemovedPage)
				{
					SetNone();
				}
			}

			restoreResult.Init();
		}

		//--------------------------------------------------------------------------------------
		// Reset GUI Contents
		//--------------------------------------------------------------------------------------
		public void ResetGUIContents()
		{
			_guiContent_StepCompleted = null;
			_guiContent_StepUncompleted = null;
			_guiContent_StepUnUsed = null;

			_guiContent_imgValueUp = null;
			_guiContent_imgValueDown = null;
			_guiContent_imgValueLeft = null;
			_guiContent_imgValueRight = null;

			_guiContent_MeshProperty_ResetVerts = null;
			_guiContent_MeshProperty_RemoveMesh = null;
			_guiContent_MeshProperty_ChangeImage = null;
			_guiContent_MeshProperty_AutoLinkEdge = null;
			_guiContent_MeshProperty_Draw_MakePolygones = null;
			_guiContent_MeshProperty_MakePolygones = null;
			_guiContent_MeshProperty_RemoveAllVertices = null;
			_guiContent_MeshProperty_HowTo_MouseLeft = null;
			_guiContent_MeshProperty_HowTo_MouseMiddle = null;
			_guiContent_MeshProperty_HowTo_MouseRight = null;
			_guiContent_MeshProperty_HowTo_KeyDelete = null;
			_guiContent_MeshProperty_HowTo_KeyCtrl = null;
			_guiContent_MeshProperty_HowTo_KeyShift = null;
			_guiContent_MeshProperty_Texture = null;

			_guiContent_Bottom2_Physic_WindON = null;
			_guiContent_Bottom2_Physic_WindOFF = null;

			_guiContent_Image_RemoveImage = null;
			_guiContent_Animation_SelectMeshGroupBtn = null;
			_guiContent_Animation_AddTimeline = null;
			_guiContent_Animation_RemoveAnimation = null;
			_guiContent_Animation_TimelineUnit_AnimMod = null;
			_guiContent_Animation_TimelineUnit_ControlParam = null;

			_guiContent_Overall_SelectedAnimClp = null;
			_guiContent_Overall_MakeThumbnail = null;
			_guiContent_Overall_TakeAScreenshot = null;
			_guiContent_Overall_AnimItem = null;

			_guiContent_Param_Presets = null;
			_guiContent_Param_RemoveParam = null;
			_guiContent_Param_IconPreset = null;

			_guiContent_MeshGroupProperty_RemoveMeshGroup = null;
			_guiContent_MeshGroupProperty_RemoveAllBones = null;
			_guiContent_MeshGroupProperty_ModifierLayerUnit = null;
			_guiContent_MeshGroupProperty_SetRootUnit = null;
			_guiContent_MeshGroupProperty_AddModifier = null;

			_guiContent_Bottom_Animation_TimelineLayerInfo = null;
			_guiContent_Bottom_Animation_RemoveKeyframes = null;
			_guiContent_Bottom_Animation_RemoveNumKeyframes = null;
			_guiContent_Bottom_Animation_Fit = null;

			_guiContent_Right_MeshGroup_MaterialSet = null;
			_guiContent_Right_MeshGroup_CustomShader = null;
			_guiContent_Right_MeshGroup_MatSetName = null;
			_guiContent_Right_MeshGroup_CopySettingToOtherMeshes = null;
			_guiContent_Right_MeshGroup_RiggingIconAndText = null;
			_guiContent_Right_MeshGroup_ParamIconAndText = null;
			_guiContent_Right_MeshGroup_RemoveBone = null;
			_guiContent_Right_MeshGroup_RemoveModifier = null;

			_guiContent_Modifier_ParamSetItem = null;
			_guiContent_Modifier_AddControlParameter = null;
			_guiContent_CopyTextIcon = null;
			_guiContent_PasteTextIcon = null;
			_guiContent_Modifier_RigExport = null;
			_guiContent_Modifier_RigImport = null;
			_guiContent_Modifier_RemoveFromKeys = null;
			_guiContent_Modifier_AddToKeys = null;
			_guiContent_Modifier_AnimIconText = null;
			_guiContent_Modifier_RemoveFromRigging = null;
			_guiContent_Modifier_AddToRigging = null;
			_guiContent_Modifier_AddToPhysics = null;
			_guiContent_Modifier_RemoveFromPhysics = null;
			_guiContent_Modifier_PhysicsSetting_NameIcon = null;
			_guiContent_Modifier_PhysicsSetting_Basic = null;
			_guiContent_Modifier_PhysicsSetting_Stretchiness = null;
			_guiContent_Modifier_PhysicsSetting_Inertia = null;
			_guiContent_Modifier_PhysicsSetting_Restoring = null;
			_guiContent_Modifier_PhysicsSetting_Viscosity = null;
			_guiContent_Modifier_PhysicsSetting_Gravity = null;
			_guiContent_Modifier_PhysicsSetting_Wind = null;
			_guiContent_Right_Animation_ExportImportAnim = null;
			_guiContent_Right_Animation_AllObjectToLayers = null;
			_guiContent_Right_Animation_RemoveTimeline = null;
			_guiContent_Right_Animation_AddTimelineLayerToEdit = null;
			_guiContent_Right_Animation_RemoveTimelineLayer = null;
			_guiContent_Bottom_EditMode_CommonIcon = null;
			_guiContent_Icon_ModTF_Pos = null;
			_guiContent_Icon_ModTF_Rot = null;
			_guiContent_Icon_ModTF_Scale = null;
			_guiContent_Icon_Mod_Color = null;
			_guiContent_Right_MeshGroup_MeshIcon = null;
			_guiContent_Right_MeshGroup_MeshGroupIcon = null;
			_guiContent_Right_MeshGroup_ModIcon = null;
			_guiContent_Right_MeshGroup_AnimIcon = null;
			_guiContent_Right_Animation_TimelineIcon_AnimWithMod = null;
			_guiContent_Right_Animation_TimelineIcon_AnimWithControlParam = null;
			_guiContent_Bottom_Animation_FirstFrame = null;
			_guiContent_Bottom_Animation_PrevFrame = null;
			_guiContent_Bottom_Animation_Play = null;
			_guiContent_Bottom_Animation_Pause = null;
			_guiContent_Bottom_Animation_NextFrame = null;
			_guiContent_Bottom_Animation_LastFrame = null;

			_guiContent_MakeMesh_PointCount_X = null;
			_guiContent_MakeMesh_PointCount_Y = null;
			_guiContent_MakeMesh_AutoGenPreview = null;
			_guiContent_MakeMesh_GenerateMesh = null;

			_guiContent_AnimKeyframeProp_PrevKeyLabel = null;
			_guiContent_AnimKeyframeProp_NextKeyLabel = null;

			_guiContent_Right2MeshGroup_ObjectProp_Name = null;
			_guiContent_Right2MeshGroup_ObjectProp_Type = null;
			_guiContent_Right2MeshGroup_ObjectProp_NickName = null;

			_guiContent_MaterialSet_ON = null;
			_guiContent_MaterialSet_OFF = null;

			_guiContent_Right2MeshGroup_MaskParentName = null;
			_guiContent_Right2MeshGroup_DuplicateTransform = null;
			_guiContent_Right2MeshGroup_MigrateTransform = null;
			_guiContent_Right2MeshGroup_DetachObject = null;

			_guiContent_ModProp_ParamSetTarget_Name = null;
			_guiContent_ModProp_ParamSetTarget_StatusText = null;

			_guiContent_ModProp_Rigging_VertInfo = null;
			_guiContent_ModProp_Rigging_BoneInfo = null;

			_guiContent_RiggingBoneWeightLabel = null;
			_guiContent_RiggingBoneWeightBoneName = null;

			_guiContent_PhysicsGroupID_None = null;
			_guiContent_PhysicsGroupID_1 = null;
			_guiContent_PhysicsGroupID_2 = null;
			_guiContent_PhysicsGroupID_3 = null;
			_guiContent_PhysicsGroupID_4 = null;
			_guiContent_PhysicsGroupID_5 = null;
			_guiContent_PhysicsGroupID_6 = null;
			_guiContent_PhysicsGroupID_7 = null;
			_guiContent_PhysicsGroupID_8 = null;
			_guiContent_PhysicsGroupID_9 = null;

			_guiContent_Right2_Animation_TargetObjectName = null;
			

			//GUI Content 추가시 여기에 코드를 적자
		}
	}
}