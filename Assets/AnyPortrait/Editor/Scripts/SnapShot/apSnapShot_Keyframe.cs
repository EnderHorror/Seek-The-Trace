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

	public class apSnapShot_Keyframe : apSnapShotBase
	{
		// Members
		//--------------------------------------------
		//키값 (같은 키값일때 복사가 가능하다.
		private apAnimTimelineLayer _key_TimelineLayer = null;
		public apAnimTimelineLayer KeyTimelineLayer {  get {  return _key_TimelineLayer; } }

		private int _savedFrameIndex = 0;
		public int SavedFrameIndex
		{
			get {  return _savedFrameIndex; }
		}
		//<< 다른 AnimClip간에는 복사가 안되나?

		//저장되는 멤버 데이터
		//ModMesh 정보와 키프레임의 기본 정보를 모두 저장해야한다.
		public apAnimCurve _animCurve = null;
		public bool _isKeyValueSet = false;

		//public bool _conSyncValue_Bool = false;
		public int _conSyncValue_Int = 0;
		public float _conSyncValue_Float = 0.0f;
		public Vector2 _conSyncValue_Vector2 = Vector2.zero;
		//public Vector3 _conSyncValue_Vector3 = Vector3.zero;
		//public Color _conSyncValue_Color = Color.black;

		//ModMesh의 값도 넣어준다.
		public class VertData
		{
			public apVertex _key_Vert = null;
			public Vector2 _deltaPos = Vector2.zero;

			public VertData(apVertex key_Vert, Vector2 deltaPos)
			{
				_key_Vert = key_Vert;
				_deltaPos = deltaPos;
			}
		}
		private List<VertData> _vertices = new List<VertData>();
		private apMatrix _transformMatrix = new apMatrix();
		private Color _meshColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
		private bool _isVisible = true;


		//추가 3.29 : ExtraOption도 저장하자
		public class ExtraDummyValue
		{
			public bool _isDepthChanged = false;
			public int _deltaDepth = 0;

			public bool _isTextureChanged = false;
			public apTextureData _linkedTextureData = null;

			public int _textureDataID = -1;

			public float _weightCutout = 0.5f;
			public float _weightCutout_AnimPrev = 0.5f;
			public float _weightCutout_AnimNext = 0.6f;

			public ExtraDummyValue(apModifiedMesh.ExtraValue srcValue)
			{
				_isDepthChanged = srcValue._isDepthChanged;
				_deltaDepth = srcValue._deltaDepth;

				_isTextureChanged = srcValue._isTextureChanged;
				_linkedTextureData = srcValue._linkedTextureData;

				_textureDataID = srcValue._textureDataID;

				_weightCutout = srcValue._weightCutout;
				_weightCutout_AnimPrev = srcValue._weightCutout_AnimPrev;
				_weightCutout_AnimNext = srcValue._weightCutout_AnimNext;
			}
		}

		private bool _isExtraValueEnabled = false;
		private ExtraDummyValue _extraValue = null;

		// Init
		//--------------------------------------------
		public apSnapShot_Keyframe() : base()
		{

		}

		// Functions
		//--------------------------------------------
		public override bool IsKeySyncable(object target)
		{
			if (!(target is apAnimKeyframe))
			{
				return false;
			}

			apAnimKeyframe keyframe = target as apAnimKeyframe;
			if (keyframe == null)
			{
				return false;
			}

			//Key가 같아야 한다.
			if (keyframe._parentTimelineLayer != _key_TimelineLayer)
			{
				return false;
			}

			return true;
		}

		public override bool Save(object target, string strParam)
		{
			base.Save(target, strParam);



			apAnimKeyframe keyframe = target as apAnimKeyframe;
			if (keyframe == null)
			{
				return false;
			}

			//추가 3.29 : 저장된 당시의 프레임을 기억하자
			_savedFrameIndex = keyframe._frameIndex;

			_key_TimelineLayer = keyframe._parentTimelineLayer;
			if (_key_TimelineLayer == null)
			{
				return false;
			}

			_animCurve = new apAnimCurve(keyframe._curveKey, keyframe._frameIndex);
			_isKeyValueSet = keyframe._isKeyValueSet;

			//_conSyncValue_Bool = keyframe._conSyncValue_Bool;
			_conSyncValue_Int = keyframe._conSyncValue_Int;
			_conSyncValue_Float = keyframe._conSyncValue_Float;
			_conSyncValue_Vector2 = keyframe._conSyncValue_Vector2;
			//_conSyncValue_Vector3 = keyframe._conSyncValue_Vector3;
			//_conSyncValue_Color = keyframe._conSyncValue_Color;

			_vertices.Clear();
			_transformMatrix = new apMatrix();
			_meshColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
			_isVisible = true;

			_isExtraValueEnabled = false;
			_extraValue = null;

			if (keyframe._linkedModMesh_Editor != null)
			{
				apModifiedMesh modMesh = keyframe._linkedModMesh_Editor;
				_vertices.Clear();
				int nVert = modMesh._vertices.Count;

				for (int i = 0; i < nVert; i++)
				{
					apModifiedVertex modVert = modMesh._vertices[i];
					_vertices.Add(new VertData(modVert._vertex, modVert._deltaPos));
				}

				_transformMatrix = new apMatrix(modMesh._transformMatrix);
				_meshColor = modMesh._meshColor;
				_isVisible = modMesh._isVisible;

				//추가 3.29 : ExtraValue도 복사
				if(modMesh._isExtraValueEnabled)
				{
					_isExtraValueEnabled = true;
					_extraValue = new ExtraDummyValue(modMesh._extraValue);
				}
			}
			else if (keyframe._linkedModBone_Editor != null)
			{
				apModifiedBone modBone = keyframe._linkedModBone_Editor;

				_transformMatrix = new apMatrix(modBone._transformMatrix);
			}

			
			return true;
		}

		public override bool Load(object targetObj)
		{
			apAnimKeyframe keyframe = targetObj as apAnimKeyframe;
			if (keyframe == null)
			{
				return false;
			}

			keyframe._curveKey = new apAnimCurve(_animCurve, keyframe._frameIndex);
			keyframe._isKeyValueSet = _isKeyValueSet;

			//keyframe._conSyncValue_Bool = _conSyncValue_Bool;
			keyframe._conSyncValue_Int = _conSyncValue_Int;
			keyframe._conSyncValue_Float = _conSyncValue_Float;
			keyframe._conSyncValue_Vector2 = _conSyncValue_Vector2;
			//keyframe._conSyncValue_Vector3 = _conSyncValue_Vector3;
			//keyframe._conSyncValue_Color = _conSyncValue_Color;


			if (keyframe._linkedModMesh_Editor != null)
			{
				apModifiedMesh modMesh = keyframe._linkedModMesh_Editor;

				VertData vertData = null;
				apModifiedVertex modVert = null;
				int nVert = _vertices.Count;
				for (int i = 0; i < nVert; i++)
				{
					vertData = _vertices[i];
					modVert = modMesh._vertices.Find(delegate (apModifiedVertex a)
					{
						return a._vertex == vertData._key_Vert;
					});

					if (modVert != null)
					{
						modVert._deltaPos = vertData._deltaPos;
					}
				}

				modMesh._transformMatrix.SetMatrix(_transformMatrix);
				modMesh._meshColor = _meshColor;
				modMesh._isVisible = _isVisible;

				//추가 3.29 : ExtraProperty도 복사
				modMesh._isExtraValueEnabled = _isExtraValueEnabled;
				if (modMesh._extraValue == null)
				{
					modMesh._extraValue = new apModifiedMesh.ExtraValue();
					modMesh._extraValue.Init();
				}

				if (_isExtraValueEnabled)
				{
					if (_extraValue != null)
					{
						modMesh._extraValue._isDepthChanged = _extraValue._isDepthChanged;
						modMesh._extraValue._deltaDepth = _extraValue._deltaDepth;
						modMesh._extraValue._isTextureChanged = _extraValue._isTextureChanged;
						modMesh._extraValue._linkedTextureData = _extraValue._linkedTextureData;
						modMesh._extraValue._textureDataID = _extraValue._textureDataID;
						modMesh._extraValue._weightCutout = _extraValue._weightCutout;
						modMesh._extraValue._weightCutout_AnimPrev = _extraValue._weightCutout_AnimPrev;
						modMesh._extraValue._weightCutout_AnimNext = _extraValue._weightCutout_AnimNext;
					}
				}
				else
				{
					modMesh._extraValue.Init();
				}
			}
			else if (keyframe._linkedModBone_Editor != null)
			{
				apModifiedBone modBone = keyframe._linkedModBone_Editor;
				modBone._transformMatrix.SetMatrix(_transformMatrix);
			}
			

			return true;
		}
	}

}