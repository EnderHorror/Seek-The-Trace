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
	/// <summary>
	/// PSD에 들어가는 레이어 데이터이다.
	/// Import에서 사용되는 apPSDLayerData와 유사하다. 헷갈리지 말자.
	/// Import할때 저장되는 데이터 정보를 저장하는 역할
	/// Texture Atlas와 Mesh / MeshGroup과 연결한다.
	/// 색상 정보는 저장하지 않는다.
	/// Reimport를 위한 것이므로, 그 외의 정보는 저장되지 않는다. (레이어 속성은 처음 Bake시에 정해진다)
	/// </summary>
	[Serializable]
	public class apPSDSetLayer
	{
		// Members
		//---------------------------------------------
		public int _layerIndex = -1;
		public string _name = "";
		public int _width = -1;
		public int _height = -1;
		//public int _posOffset_Left = 0;
		//public int _posOffset_Top = 0;
		//public int _posOffset_Right = 0;
		//public int _posOffset_Bottom = 0;

		//_isImageLayer가 True이면 : TextureData + Mesh (Atlas) + MeshTransform 정보가 포함된다.
		//_isImageLayer가 False이면 : MeshGroupTransform 정보만 포함된다.
		public bool _isImageLayer = false;

		//1. ImageLayer인 경우
		//Bake된 정보
		//public int _bakedWidth = 0;
		//public int _bakedHeight = 0;
		//public int _bakedImagePos_Left = 0;
		//public int _bakedImagePos_Top = 0;

		public int _transformID = -1;

		public float _bakedLocalPosOffset_X = 0;
		public float _bakedLocalPosOffset_Y = 0;

		public bool _isBaked = false;

		

	

		// Init
		//---------------------------------------------
		public apPSDSetLayer()
		{

		}


		// Functions
		//---------------------------------------------
		public void SetBakeData(	int layerIndex,
									string name, 
									int width, 
									int height, 
									//int posOffset_Left, 
									//int posOffset_Top, 
									//int posOffset_Right, 
									//int posOffset_Bottom, 
									bool isImageLayer,
									//int bakedWidth,
									//int bakedHeight,
									//int bakedImagePos_Left, 
									//int bakedImagePos_Top, 
									int transformID, 
									float bakedLocalPosOffset_X,
									float bakedLocalPosOffset_Y)
		{
			_layerIndex = layerIndex;
			_name = name;
			_width = width;
			_height = height;
			//_posOffset_Left = posOffset_Left;
			//_posOffset_Top = posOffset_Top;
			//_posOffset_Right = posOffset_Right;
			//_posOffset_Bottom = posOffset_Bottom;
			_isImageLayer = isImageLayer;

			//_bakedWidth = bakedWidth;
			//_bakedHeight = bakedHeight;
			//_bakedImagePos_Left = bakedImagePos_Left;
			//_bakedImagePos_Top = bakedImagePos_Top;

			_transformID = transformID;
			_bakedLocalPosOffset_X = bakedLocalPosOffset_X;
			_bakedLocalPosOffset_Y = bakedLocalPosOffset_Y;

			_isBaked = true;
		}

		public void SetNotBaked(int layerIndex, string name, bool isImageLayer)
		{
			_layerIndex = layerIndex;
			_name = name;
			_isImageLayer = isImageLayer;

			_bakedLocalPosOffset_X = 0;
			_bakedLocalPosOffset_Y = 0;

			_isBaked = false;
		}


		// Get / Set
		//---------------------------------------------
	}
}