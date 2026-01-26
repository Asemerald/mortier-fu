using UnityEngine;

namespace MortierFu.Shared
{
    public static class ParticleSystemUtils
    {
        public static void CopyMainModule(this ParticleSystem from, ParticleSystem to)
        {
            var src = from.main;
            var dst = to.main;

            dst.duration = src.duration;
            dst.loop = src.loop;
            dst.prewarm = src.prewarm;
            dst.startDelay = src.startDelay;
            dst.startLifetime = src.startLifetime;
            dst.startSpeed = src.startSpeed;
            dst.startSize = src.startSize;
            dst.startRotation = src.startRotation;
            dst.startRotation3D = src.startRotation3D;
            dst.startColor = src.startColor;
            dst.gravityModifier = src.gravityModifier;
            dst.simulationSpace = src.simulationSpace;
            dst.simulationSpeed = src.simulationSpeed;
            dst.scalingMode = src.scalingMode;
            dst.playOnAwake = src.playOnAwake;
            dst.maxParticles = src.maxParticles;
            dst.emitterVelocityMode = src.emitterVelocityMode;
            dst.emitterVelocity = src.emitterVelocity;
            dst.stopAction = src.stopAction;
            dst.cullingMode = src.cullingMode;
            dst.ringBufferMode = src.ringBufferMode;
            dst.ringBufferLoopRange = src.ringBufferLoopRange;
        }
    }
}