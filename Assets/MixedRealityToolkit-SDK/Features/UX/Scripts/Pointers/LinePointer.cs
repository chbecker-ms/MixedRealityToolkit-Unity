﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Internal.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Internal.Utilities.Lines.DataProviders;
using Microsoft.MixedReality.Toolkit.Internal.Utilities.Lines.Renderers;
using Microsoft.MixedReality.Toolkit.Internal.Utilities.Physics.Distorters;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    /// <summary>
    /// A simple line pointer for drawing lines from the input source origin to the current pointer position.
    /// </summary>
    [RequireComponent(typeof(DistorterGravity))]
    public class LinePointer : BaseControllerPointer
    {
        [SerializeField]
        protected Gradient LineColorSelected = new Gradient();

        [SerializeField]
        protected Gradient LineColorValid = new Gradient();

        [SerializeField]
        protected Gradient LineColorInvalid = new Gradient();

        [SerializeField]
        protected Gradient LineColorNoTarget = new Gradient();

        [SerializeField]
        protected Gradient LineColorLockFocus = new Gradient();

        [Range(2, 100)]
        [SerializeField]
        protected int LineCastResolution = 25;

        [SerializeField]
        private BaseMixedRealityLineDataProvider lineBase;

        public BaseMixedRealityLineDataProvider LineBase => lineBase;

        [SerializeField]
        [Tooltip("If no line renderers are specified, this array will be auto-populated on startup.")]
        private BaseMixedRealityLineRenderer[] lineRenderers;

        public BaseMixedRealityLineRenderer[] LineRenderers
        {
            get { return lineRenderers; }
            set { lineRenderers = value; }
        }

        [SerializeField]
        private DistorterGravity gravityDistorter = null;

        public DistorterGravity GravityDistorter => gravityDistorter;

        private void OnValidate()
        {
            CheckInitialization();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CheckInitialization();
        }

        private void CheckInitialization()
        {
            if (lineBase == null)
            {
                lineBase = GetComponent<BaseMixedRealityLineDataProvider>();
            }

            if (lineBase == null)
            {
                Debug.LogError($"No Mixed Reality Line Data Provider found on {gameObject.name}. Did you forget to add a Line Data provider?");
            }

            if (gravityDistorter == null)
            {
                gravityDistorter = GetComponent<DistorterGravity>();
            }

            if (lineBase != null && (lineRenderers == null || lineRenderers.Length == 0))
            {
                lineRenderers = lineBase.GetComponentsInChildren<BaseMixedRealityLineRenderer>();
            }

            if (lineRenderers == null || lineRenderers.Length == 0)
            {
                Debug.LogError($"No Mixed Reality Line Renderers found on {gameObject.name}. Did you forget to add a Mixed Reality Line Renderer?");
            }
        }

        /// <inheritdoc />
        public override void OnPreRaycast()
        {
            Debug.Assert(lineBase != null);

            Vector3 pointerPosition;
            TryGetPointerPosition(out pointerPosition);

            // Set our first and last points
            lineBase.FirstPoint = pointerPosition;
            lineBase.LastPoint = pointerPosition + (PointerDirection * PointerExtent);

            // Make sure our array will hold
            if (Rays == null || Rays.Length != LineCastResolution)
            {
                Rays = new RayStep[LineCastResolution];
            }

            // Set up our rays
            if (!IsFocusLocked)
            {
                // Turn off gravity so we get accurate rays
                gravityDistorter.enabled = false;
            }

            float stepSize = 1f / Rays.Length;
            Vector3 lastPoint = lineBase.GetUnClampedPoint(0f);

            for (int i = 0; i < Rays.Length; i++)
            {
                Vector3 currentPoint = lineBase.GetUnClampedPoint(stepSize * (i + 1));
                Rays[i] = new RayStep(lastPoint, currentPoint);
                lastPoint = currentPoint;
            }
        }

        /// <inheritdoc />
        public override void OnPostRaycast()
        {
            // Use the results from the last update to set our NavigationResult
            float clearWorldLength = 0f;
            gravityDistorter.enabled = false;
            Gradient lineColor = LineColorNoTarget;

            if (IsInteractionEnabled)
            {
                lineBase.enabled = true;

                if (IsSelectPressed)
                {
                    lineColor = LineColorSelected;
                }

                // If we hit something
                if (Result.CurrentPointerTarget != null)
                {
                    // Use the step index to determine the length of the hit
                    for (int i = 0; i <= Result.RayStepIndex; i++)
                    {
                        if (i == Result.RayStepIndex)
                        {
                            // Only add the distance between the start point and the hit
                            clearWorldLength += Vector3.Distance(Result.StartPoint, Result.StartPoint);
                        }
                        else if (i < Result.RayStepIndex)
                        {
                            // Add the full length of the step to our total distance
                            clearWorldLength += Rays[i].Length;
                        }
                    }

                    // Clamp the end of the parabola to the result hit's point
                    lineBase.LineEndClamp = lineBase.GetNormalizedLengthFromWorldLength(clearWorldLength, LineCastResolution);

                    if (FocusTarget != null)
                    {
                        lineColor = LineColorValid;
                    }

                    if (IsFocusLocked)
                    {
                        gravityDistorter.enabled = true;
                        gravityDistorter.WorldCenterOfGravity = Result.CurrentPointerTarget.transform.position;
                    }
                }
                else
                {
                    lineBase.LineEndClamp = 1f;
                }
            }
            else
            {
                lineBase.enabled = false;
            }

            if (IsFocusLocked)
            {
                lineColor = LineColorLockFocus;
            }

            for (int i = 0; i < lineRenderers.Length; i++)
            {
                lineRenderers[i].LineColor = lineColor;
            }
        }
    }
}