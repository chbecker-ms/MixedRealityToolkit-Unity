﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Internal.Utilities.Lines.DataProviders
{
    /// <summary>
    /// A Bezier line.
    /// TODO: Support point set array, with the ability to add/remove point sets.
    /// </summary>
    public class BezierLineDataProvider : BaseMixedRealityLineDataProvider
    {
        [Serializable]
        private struct PointSet
        {
            public PointSet(float spread)
            {
                Point1 = Vector3.right * spread * 0.5f;
                Point2 = Vector3.right * spread * 0.25f;
                Point3 = Vector3.left * spread * 0.25f;
                Point4 = Vector3.left * spread * 0.5f;
            }

            public Vector3 Point1;
            public Vector3 Point2;
            public Vector3 Point3;
            public Vector3 Point4;
        }

        [SerializeField]
        [HideInInspector]
        private PointSet points = new PointSet(0.5f);

        public Vector3[] Points
        {
            get
            {
                return new[]
                {
                    points.Point1,
                    points.Point2,
                    points.Point3,
                    points.Point4
                };
            }
            set
            {
                if (value.Length != 4)
                {
                    Debug.LogError("Points must have 4 vectors in the array.");
                    return;
                }

                points.Point1 = value[0];
                points.Point2 = value[1];
                points.Point3 = value[2];
                points.Point4 = value[3];
            }
        }

        public override int PointCount => 4;

        protected override Vector3 GetPointInternal(int pointIndex)
        {
            switch (pointIndex)
            {
                case 0:
                    return points.Point1;
                case 1:
                    return points.Point2;
                case 2:
                    return points.Point3;
                case 3:
                    return points.Point4;
                default:
                    Debug.LogError("Invalid Point index!");
                    return Vector3.zero;
            }
        }

        protected override void SetPointInternal(int pointIndex, Vector3 point)
        {
            switch (pointIndex)
            {
                case 0:
                    points.Point1 = point;
                    break;
                case 1:
                    points.Point2 = point;
                    break;
                case 2:
                    points.Point3 = point;
                    break;
                case 3:
                    points.Point4 = point;
                    break;
                default:
                    Debug.LogError("Invalid point index!");
                    break;
            }
        }

        protected override Vector3 GetPointInternal(float normalizedDistance)
        {
            return LineUtility.InterpolateBezeirPoints(points.Point1, points.Point2, points.Point3, points.Point4, normalizedDistance);
        }

        protected override float GetUnClampedWorldLengthInternal()
        {
            // Crude approximation
            // TODO optimize
            float distance = 0f;
            Vector3 last = GetUnClampedPoint(0f);

            for (int i = 1; i < 10; i++)
            {
                Vector3 current = GetUnClampedPoint((float)i / 10);
                distance += Vector3.Distance(last, current);
            }

            return distance;
        }

        protected override Vector3 GetUpVectorInternal(float normalizedLength)
        {
            // Bezier up vectors just use transform up
            return transform.up;
        }
    }
}