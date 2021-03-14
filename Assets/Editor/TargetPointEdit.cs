using System;
using UnityEditor;
using UnityEngine;


namespace MyEditor
{

    [CustomEditor(typeof(Lift))]
    public class TargetPointEdit :UnityEditor.Editor
    {
        private Lift _lift;

        private void OnEnable()
        {
            _lift = (Lift) target;
        }

        void OnSceneGUI()
        {
            Vector2 newPos = Handles.FreeMoveHandle(_lift.target, Quaternion.identity, .1f, Vector2.zero,
                Handles.CircleHandleCap);
            if (_lift.target != newPos)
            {
                Undo.RecordObject(target, "MoveHandle");
                _lift.target = newPos;
            }
        }
    }
}