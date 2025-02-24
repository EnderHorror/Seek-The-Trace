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
	/// Control Param의 보간을 위한 데이터
	/// 가상의 격자 형태에 배치되므로 위치 인덱스를 가진다.
	/// 2D 공간에 위치한다.
	/// </summary>
	public class apCalculatedLerpPoint
	{
		// Members
		//-----------------------------------------
		public Vector2 _pos = Vector2.zero;//Float / Vector2 값을 사용하는 경우
		public int _iPos = 0;//Int 값을 사용하는 경우

		//True이면 실제 데이터가 있는 것이며, False이면 미리 다른 키로부터 보간된 가상 Point이다.
		public bool _isRealPoint = true;

		//보간 처리를 위해 Param을 저장한다. 여러개 저장 가능 (가상 Point인 경우)
		public List<apCalculatedResultParam.ParamKeyValueSet> _refParams = new List<apCalculatedResultParam.ParamKeyValueSet>();
		public List<float> _refWeights = new List<float>();

		public float _calculatedWeight = 0.0f;


		// Init
		//-----------------------------------------
		public apCalculatedLerpPoint(Vector2 vPos, bool isRealPoint)
		{
			_pos = vPos;
			_isRealPoint = isRealPoint;

			_refParams.Clear();
			_refWeights.Clear();
		}

		public apCalculatedLerpPoint(float fPos, bool isRealPoint)
		{
			_pos.x = fPos;
			_isRealPoint = isRealPoint;

			_refParams.Clear();
			_refWeights.Clear();
		}

		public apCalculatedLerpPoint(int iPos, bool isRealPoint)
		{
			_iPos = iPos;
			_isRealPoint = isRealPoint;

			_refParams.Clear();
			_refWeights.Clear();
		}


		// Functions
		//-----------------------------------------
		public void AddPoint(apCalculatedResultParam.ParamKeyValueSet point, float weight)
		{
			_refParams.Add(point);
			_refWeights.Add(weight);
		}

		public void Addpoints(apCalculatedLerpPoint lerpPoint, float weight)
		{
			for (int i = 0; i < lerpPoint._refParams.Count; i++)
			{
				AddPoint(lerpPoint._refParams[i], lerpPoint._refWeights[i] * weight);
			}
		}

		public void CalculateITPWeight()
		{
			for (int i = 0; i < _refParams.Count; i++)
			{
				_refParams[i]._isCalculated = true;
				_refParams[i]._weight += _refWeights[i] * _calculatedWeight;
			}
		}


		// Get / Set
		//-----------------------------------------
	}
}