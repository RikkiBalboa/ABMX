﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KKABMX.Core
{
    public class BoneModifierBody
    {
        private readonly ShapeInfoBase.BoneInfo _boneInfo;

        public int BoneIndex { get; }
        public string BoneName { get; }
        public bool ScaleBone { get; private set; }
        public Transform ManualTarget { get; internal set; }
        public bool IsNotManual { get; }

        public float LenMod = 1f;
        public Vector3 SclMod = Vector3.one;

        private bool _hasBaseline;
        private float _lenBaseline;
        private Vector3 _sclBaseline = Vector3.one;

        private bool _enabled = true;

        public BoneModifierBody(int boneIndex, ShapeInfoBase sib, string boneName)
        {
            BoneIndex = boneIndex;
            var shapeInfoBase = sib;
            IsNotManual = boneIndex != Utilities.ManualBoneId;
            BoneName = boneName;

            if (IsNotManual && shapeInfoBase != null && shapeInfoBase.GetDictDst().ContainsKey(boneIndex))
                _boneInfo = shapeInfoBase.GetDictDst()[boneIndex];
        }

        public bool Enabled
        {
            get
            {
                if (!Vector3.one.Equals(SclMod) || Math.Abs(LenMod - 1f) > 0.0001f)
                {
                    _enabled = true;
                    return true;
                }

                // The point is to let the boner run for 1 extra frame after it's disabled to properly reset stuff
                if (_enabled)
                {
                    _enabled = false;
                    return true;
                }

                return false;
            }
            set
            {
                if (_enabled && !value)
                    Reset();
                _enabled = value;
            }
        }

        public void Apply()
        {
            if (Enabled)
            {
                var target = GetTarget();
                if (target != null)
                {
                    var localScale = new Vector3(_sclBaseline.x * SclMod.x, _sclBaseline.y * SclMod.y,
                        _sclBaseline.z * SclMod.z);
                    target.localScale = localScale;
                    if (LenMod != 1f && target.localPosition != Vector3.zero && _lenBaseline != 0f)
                        target.localPosition = target.localPosition / target.localPosition.magnitude * _lenBaseline * LenMod;
                }
            }
        }

        public void Clear()
        {
            Enabled = true;
            SclMod = Vector3.one;
            LenMod = 1f;
        }

        public void Reset()
        {
            var target = GetTarget();
            if (target != null && Enabled && _hasBaseline)
            {
                target.localScale = _sclBaseline;
                if (LenMod != 1f && target.localPosition != Vector3.zero && _lenBaseline != 0f)
                    target.localPosition = target.localPosition / target.localPosition.magnitude * _lenBaseline;
            }
        }

        public static SortedDictionary<string, BoneModifierBody> CreateListForBody(ShapeInfoBase sibBody)
        {
            var sortedDictionary = new SortedDictionary<string, BoneModifierBody>();
            var dict = sibBody.GetDictDst();
            foreach (var key in dict.Keys)
            {
                var boneInfo = dict[key];
                var boneModifierBody = new BoneModifierBody(key, sibBody, boneInfo.trfBone.name)
                {
                    SclMod = Vector3.one,
                    ScaleBone = IsScaleBone(boneInfo, sibBody, key)
                };
                sortedDictionary.Add(boneModifierBody.BoneName, boneModifierBody);
            }
            return sortedDictionary;
        }

        public static void AddFaceBones(ShapeInfoBase sibFace, SortedDictionary<string, BoneModifierBody> result)
        {
            var dict = sibFace.GetDictDst();
            foreach (var key in dict.Keys)
            {
                var boneInfo = dict[key];
                var boneModifierBody = new BoneModifierBody(key, sibFace, boneInfo.trfBone.name)
                {
                    SclMod = Vector3.one,
                    ScaleBone = IsScaleBone(boneInfo, sibFace, key)
                };
                result.Add(boneModifierBody.BoneName, boneModifierBody);
            }
        }

        private Transform GetTarget()
        {
            if (BoneIndex == Utilities.ManualBoneId)
                return ManualTarget;
            return _boneInfo.trfBone;
        }

        public void CollectBaseline()
        {
            var target = GetTarget();
            if (target != null)
            {
                _sclBaseline = target.localScale;
                _lenBaseline = target.localPosition.magnitude;
                _hasBaseline = true;
            }
        }
        
        private static bool IsScaleBone(ShapeInfoBase.BoneInfo boneInfo, ShapeInfoBase sibBody, int boneIndex)
        {
            if (boneInfo == null || boneInfo.trfBone == null)
                return false;

            int[] array = null;
            if (sibBody is ShapeBodyInfoFemale)
                array = Utilities.ScaleBodyBonesF;
            else if (sibBody is ShapeBodyInfoMale)
                array = Utilities.ScaleBodyBonesM;
            else if (sibBody is ShapeHeadInfoFemale)
                array = Utilities.ScaleFaceBonesF;
            else if (sibBody is ShapeHeadInfoMale)
                array = Utilities.ScaleFaceBonesM;
            return array != null && array.Contains(boneIndex);
        }
    }
}