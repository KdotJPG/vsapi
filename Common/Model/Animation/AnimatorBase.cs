﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common
{
    public delegate double WalkSpeedSupplierDelegate();

    /// <summary>
    /// Syncs every frame with entity.ActiveAnimationsByAnimCode, starts, progresses and stops animations when necessary 
    /// </summary>
    public abstract class AnimatorBase : IAnimator
    {
        // 0 3 6 9
        // 1 4 7 10
        // 2 5 8 11
        public static readonly float[] identMat4x3 = new float[] { 
                1, 0, 0, 
                0, 1, 0, 
                0, 0, 1, 
                0, 0, 0 
            };

        public RunningAnimation[] anims;

        WalkSpeedSupplierDelegate WalkSpeedSupplier;
        Action<string> onAnimationStoppedListener = null;

        /// <summary>
        /// We skip the last row - https://stackoverflow.com/questions/32565827/whats-the-purpose-of-magic-4-of-last-row-in-matrix-4x4-for-3d-graphics 
        /// </summary>
        public float[] TransformationMatrices4x3 = new float[12 * GlobalConstants.MaxAnimatedElements];

        /// <summary>
        /// The entities default pose. Meaning for most elements this is the identity matrix, with exception of individually controlled elements such as the head.
        /// </summary>
        public float[] TransformationMatricesDefaultPose4x3 = new float[12 * GlobalConstants.MaxAnimatedElements];



        public Dictionary<string, AttachmentPointAndPose> AttachmentPointByCode = new Dictionary<string, AttachmentPointAndPose>();

        protected int curAnimCount = 0;
        public RunningAnimation[] CurAnims = new RunningAnimation[20];

        public bool CalculateMatrices { get; set; } = true;

        public float[] Matrices4x3
        {
            get {
                return curAnimCount > 0 ? TransformationMatrices4x3 : TransformationMatricesDefaultPose4x3;
            }
        }

        public int ActiveAnimationCount
        {
            get { return curAnimCount; }
        }

        public RunningAnimation[] RunningAnimations => anims;

        public RunningAnimation GetAnimationState(string code)
        {
            for (int i = 0; i < anims.Length; i++)
            {
                RunningAnimation anim = anims[i];

                if (anim.Animation.Code == code)
                {
                    return anim;
                }
            }

            return null;
        }

        public AnimatorBase(WalkSpeedSupplierDelegate WalkSpeedSupplier, Animation[] Animations, Action<string> onAnimationStoppedListener = null)
        {
            this.WalkSpeedSupplier = WalkSpeedSupplier;
            this.onAnimationStoppedListener = onAnimationStoppedListener;

            anims = new RunningAnimation[Animations == null ? 0 : Animations.Length];

            for (int i = 0; i < anims.Length; i++)
            {
                Animations[i].Code = Animations[i].Code.ToLower();

                anims[i] = new RunningAnimation()
                {
                    Active = false,
                    Running = false,
                    Animation = Animations[i],
                    CurrentFrame = 0
                };
            }
            
            //float[] identMat = Mat4f.Create();
            for (int i = 0; i < TransformationMatricesDefaultPose4x3.Length; i++)
            {
                //TransformationMatricesDefaultPose4x3[i] = identMat[i % 16];
                TransformationMatricesDefaultPose4x3[i] = identMat4x3[i % 12];
            }
        }

        float accum = 0.25f;
        double walkSpeed;

        public virtual void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
        {
            curAnimCount = 0;

            accum += dt;
            if (accum > 0.25f)
            {
                walkSpeed = WalkSpeedSupplier == null ? 1f : WalkSpeedSupplier();
                accum = 0;
            }
            
            
            for (int i = 0; i < anims.Length; i++)
            {
                RunningAnimation anim = anims[i];

                AnimationMetaData animData;
                activeAnimationsByAnimCode.TryGetValue(anim.Animation.Code, out animData);

                bool wasActive = anim.Active;
                anim.Active = animData != null;

                // Animation got started
                if (!wasActive && anim.Active)
                {   
                    AnimNowActive(anim, animData);
                }

                // Animation got stopped
                if (wasActive && !anim.Active)
                {
                    if (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Rewind)
                    {
                        anim.ShouldRewind = true;
                    }

                    if (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Stop)
                    {
                        anim.Stop();
                        activeAnimationsByAnimCode.Remove(anim.Animation.Code);
                        onAnimationStoppedListener?.Invoke(anim.Animation.Code);
                    }

                    if (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd)
                    {
                        anim.ShouldPlayTillEnd = true;
                    }
                }


                if (!anim.Running)
                {
                    continue;
                }

                bool shouldStop =
                    (anim.Iterations > 0 && anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop) ||
                    (anim.Iterations > 0 && !anim.Active && (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd || anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.EaseOut) && anim.EasingFactor < 0.002f) ||
                    (anim.Iterations > 0 && (anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut) && anim.EasingFactor < 0.002f) ||
                    (anim.Iterations < 0 && !anim.Active && anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Rewind && anim.EasingFactor < 0.002f)
                ;

                if (shouldStop)
                {
                    anim.Stop();
                    if (anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop || anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut)
                    {
                        activeAnimationsByAnimCode.Remove(anim.Animation.Code);
                        onAnimationStoppedListener?.Invoke(anim.Animation.Code);
                    }
                    continue;
                }

                CurAnims[curAnimCount] = anim;

                if ((anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold && anim.Iterations != 0 && !anim.Active) || (anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && anim.Iterations != 0))
                {
                    anim.EaseOut(dt);
                }

                anim.Progress(dt, (float)walkSpeed);

                curAnimCount++;
            }


            calculateMatrices(dt);
        }




        protected virtual void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
        {
            anim.Running = true;
            anim.Active = true;
            anim.meta = animData;
            anim.ShouldRewind = false;
            anim.ShouldPlayTillEnd = false;
            anim.CurrentFrame = animData.StartFrameOnce;
            animData.StartFrameOnce = 0;
        }

        protected abstract void calculateMatrices(float dt);
        
        // This is fast animation blending, but it creates broken blended states. So correctness goees above performance I guess.
        /*{
            bool first = true;

            for (int i = 0; i < curAnimCount; i++)
            {
                RunningAnimation anim = curAnims[i];

                cheapMatrixLerp(anim, first);
                first = false;
            }
        }*/


        /*protected void cheapMatrixLerp(RunningAnimation anim, bool first)
        {
            try
            {
                AnimationFrame curFrame = anim.Animation.AllFrames[(int)anim.CurrentFrame % anim.Animation.AllFrames.Length];
                AnimationFrame nextFrame = anim.Animation.AllFrames[((int)anim.CurrentFrame + 1) % anim.Animation.AllFrames.Length];

                float l = anim.CurrentFrame - (int)anim.CurrentFrame;

                if (first)
                {
                    for (int i = 0; i < TransformationMatrices.Length; i++)
                    {
                        TransformationMatrices[i] = curFrame.transformationMatrices[i] * (1 - l) + nextFrame.transformationMatrices[i] * (l);
                    }
                }
                else
                {
                    for (int i = 0; i < TransformationMatrices.Length; i++)
                    {
                        TransformationMatrices[i] += curFrame.transformationMatrices[i] * (1 - l) + nextFrame.transformationMatrices[i] * (l);
                    }
                }
            } catch (Exception e)
            {
                string str = string.Format("Something crashed while trying to calculate an animation frame for {0}. AllFrames.Length={1}, currframee={2}, tf mats length={3}. Exception: {4}", entity?.Code, anim.Animation.AllFrames.Length,  anim.CurrentFrame, TransformationMatrices.Length, e);
                throw new Exception(str);
            }
        }*/


        public AttachmentPointAndPose GetAttachmentPointPose(string code)
        {
            AttachmentPointAndPose apap = null;
            AttachmentPointByCode.TryGetValue(code, out apap);
            return apap;
        }

        public virtual ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            throw new NotImplementedException();
        }
    }
}
