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
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using AnyPortrait;

namespace AnyPortrait
{
	/// <summary>
	/// Pose를 저장하는 Bone의 정보를 가지는 클래스
	/// Bone의 기본 정보와 World Matrix를 저장한다.
	/// UniqueID 
	/// </summary>
	public class apRetargetBonePoseUnit
	{
		// Members
		//------------------------------------------------------
		public int _unitID = -1;
		public int _uniqueID = -1;

		public string _name = "";

		public apMatrix _defaultMatrix = new apMatrix();
		public apMatrix _localMatrix = new apMatrix();
		public apMatrix _worldMatrix = new apMatrix();

		public int _sortIndex = -1;

		//Export 설정
		public bool _isExported = false;

		//Import 설정
		public bool _isImported = false;
		public apBone _targetBone = null;

		// Init
		//------------------------------------------------------
		public apRetargetBonePoseUnit()
		{
			_isExported = false;
			_isImported = false;
			_targetBone = null;
		}

		// Functions
		//------------------------------------------------------
		// Bone -> File
		//------------------------------------------------------
		public void SetBone(int unitID, apBone bone)
		{
			_unitID = unitID;
			_uniqueID = bone._uniqueID;

			_name = bone._name;

			_defaultMatrix.SetMatrix(bone._defaultMatrix);
			_localMatrix.SetMatrix(bone._localMatrix);
			_worldMatrix.SetMatrix(bone._worldMatrix);

			_targetBone = bone;//<<Export할때도 연결해주자
		}


		public string GetEncodingData()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			if(_name.Length < 10)
			{
				sb.Append("00");
				sb.Append(_name.Length);
			}
			else if(_name.Length < 100)
			{
				sb.Append("0");
				sb.Append(_name.Length);
			}
			else
			{
				sb.Append(_name.Length);
			}
			sb.Append(_name);

			sb.Append(_unitID);		sb.Append("/");
			sb.Append(_uniqueID);	sb.Append("/");

			sb.Append(_defaultMatrix._pos.x);		sb.Append("/");
			sb.Append(_defaultMatrix._pos.y);		sb.Append("/");
			sb.Append(_defaultMatrix._angleDeg);	sb.Append("/");
			sb.Append(_defaultMatrix._scale.x);		sb.Append("/");
			sb.Append(_defaultMatrix._scale.y);		sb.Append("/");

			sb.Append(_localMatrix._pos.x);		sb.Append("/");
			sb.Append(_localMatrix._pos.y);		sb.Append("/");
			sb.Append(_localMatrix._angleDeg);	sb.Append("/");
			sb.Append(_localMatrix._scale.x);		sb.Append("/");
			sb.Append(_localMatrix._scale.y);		sb.Append("/");

			sb.Append(_worldMatrix._pos.x);		sb.Append("/");
			sb.Append(_worldMatrix._pos.y);		sb.Append("/");
			sb.Append(_worldMatrix._angleDeg);	sb.Append("/");
			sb.Append(_worldMatrix._scale.x);		sb.Append("/");
			sb.Append(_worldMatrix._scale.y);		sb.Append("/");

			return sb.ToString();
		}


		public bool DecodeData(string strSrc)
		{
			try
			{
				int nameLength = int.Parse(strSrc.Substring(0, 3));
				_name = strSrc.Substring(3, nameLength);

				strSrc = strSrc.Substring(3 + nameLength);

				string[] strParse = strSrc.Split(new string[] { "/" }, StringSplitOptions.None);

				_unitID = int.Parse(strParse[0]);
				_uniqueID = int.Parse(strParse[1]);

				_defaultMatrix.SetIdentity();
				_localMatrix.SetIdentity();
				_worldMatrix.SetIdentity();

				_defaultMatrix._pos.x = float.Parse(strParse[2]);
				_defaultMatrix._pos.y = float.Parse(strParse[3]);
				_defaultMatrix._angleDeg = float.Parse(strParse[4]);
				_defaultMatrix._scale.x = float.Parse(strParse[5]);
				_defaultMatrix._scale.y = float.Parse(strParse[6]);
				_defaultMatrix.MakeMatrix();

				_localMatrix._pos.x = float.Parse(strParse[7]);
				_localMatrix._pos.y = float.Parse(strParse[8]);
				_localMatrix._angleDeg = float.Parse(strParse[9]);
				_localMatrix._scale.x = float.Parse(strParse[10]);
				_localMatrix._scale.y = float.Parse(strParse[11]);
				_localMatrix.MakeMatrix();

				_worldMatrix._pos.x = float.Parse(strParse[12]);
				_worldMatrix._pos.y = float.Parse(strParse[13]);
				_worldMatrix._angleDeg = float.Parse(strParse[14]);
				_worldMatrix._scale.x = float.Parse(strParse[15]);
				_worldMatrix._scale.y = float.Parse(strParse[16]);
				_worldMatrix.MakeMatrix();
				
			}
			catch (Exception ex)
			{
				Debug.LogError("DecodeData Exception : " + ex);
				return false;
			}
			return true;
		}


		// Get / Set
		//------------------------------------------------------

	}
}