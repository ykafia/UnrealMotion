using System;
using Stride.Graphics;
using Stride.Rendering.Images;
using Stride.Core;
using Stride.Core.Annotations;
using System.ComponentModel;
using Stride.Rendering;

namespace UnrealMotion
{

    /// <summary>
    /// Simple motion blur effect.
    /// </summary>
    [DataContract("MotionBlur")]
    public class MotionBlur : ImageEffect
    {
        private ImageEffectShader Blur;
        private ImageEffectShader Tiling;
        private ImageEffectShader Flattening;

        private ImageEffectShader NMax;
        

        public bool RequiresDepthBuffer => true;

        public bool RequiresVelocityBuffer => true;

        
        [DataMemberRange(0,64,1,2,1)]
        public uint TileSize {get;set;} = 8;

        [DataMember]
        public MotionBlurQuality Quality = MotionBlurQuality.VERY_HIGH;


        protected override void InitializeCore()
        {
            base.InitializeCore(); 
            
            Blur = ToLoadAndUnload(new ImageEffectShader("UMotionBlur"));
            Flattening = ToLoadAndUnload(new ImageEffectShader("VelocityFlatten"));
            Tiling = ToLoadAndUnload(new ImageEffectShader("VelocityTile"));
            NMax = ToLoadAndUnload(new ImageEffectShader("NeighborMax"));
        }

        protected override void DrawCore(RenderDrawContext context)
        { 
            // Inputs:
            Texture colorBuffer = GetSafeInput(0);
            Texture velocityBuffer = GetSafeInput(1);
            Texture depthBuffer = GetSafeInput(2);

            // Computed inputs
            Texture flattenedV = NewScopedRenderTarget2D(colorBuffer.Width, colorBuffer.Height, velocityBuffer.Format);
            Texture tiledV = NewScopedRenderTarget2D(colorBuffer.Width/(int)TileSize, colorBuffer.Height/(int)TileSize, velocityBuffer.Format);
            Texture nmax = NewScopedRenderTarget2D(colorBuffer.Width/(int)TileSize, colorBuffer.Height/(int)TileSize, velocityBuffer.Format);

            // Output:
            Texture outputBuffer = GetSafeOutput(0);

            

            
            Flattening.SetInput(0, velocityBuffer);
            Flattening.SetInput(1, depthBuffer);
            Flattening.SetOutput(flattenedV);
            Flattening.Draw(context);
            Tiling.SetInput(0,velocityBuffer);
            Tiling.SetInput(1, depthBuffer);
            Tiling.SetOutput(tiledV);
            Tiling.Parameters.Set(TileMaxShaderKeys.u_TileSize, (int)Quality);
            Tiling.Draw(context);
            NMax.SetInput(0,tiledV);
            NMax.SetInput(1, depthBuffer);
            NMax.SetOutput(nmax);
            NMax.Parameters.Set(TileMaxShaderKeys.u_TileSize, (int)Quality);
            NMax.Draw(context);

            // SimpleBlur.Parameters.Set(ChapmanBlurShaderKeys.u_MaxSamples, MaxSamples);
            // SimpleBlur.Parameters.Set(ChapmanBlurShaderKeys.u_BlurRadius, Math.Min(10, context.RenderContext.Time.FramePerSecond * ShutterAngle / 360));
            Blur.SetInput(0, colorBuffer);
            Blur.SetInput(1, velocityBuffer);
            Blur.SetInput(2, nmax);
            Blur.SetInput(3, flattenedV);
            Blur.SetInput(4, depthBuffer);
            Blur.SetOutput(outputBuffer);

            Blur.Parameters.Set(UMotionBlurShaderKeys.u_Quality, (int)Quality);

            Blur.Draw(context);

        }
    }
}