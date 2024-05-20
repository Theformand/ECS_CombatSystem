using System;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.PackageManager;
using UnityEngine;

public class CurveLibAuth : MonoBehaviour
{
    public CurveAuthWrapper RocketHeightCurve;
    public CurveAuthWrapper PickupVelocityCurve;

    public class Baker : Baker<CurveLibAuth>
    {
        public override void Bake(CurveLibAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.None);
            AddComponent(ent, new CurveLib
            {
                RocketHeightCurve = BakeCurve(authoring.RocketHeightCurve),
                PickupVelocityCurve = BakeCurve(authoring.PickupVelocityCurve)
            });
        }

        public DotsCurve BakeCurve(CurveAuthWrapper curveAuth)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var sampledCurve = ref builder.ConstructRoot<DiscretizedCurve>();
            var sampledCurveArray = builder.Allocate(ref sampledCurve.Points, curveAuth.NumSamples);
            sampledCurve.NumSamples = curveAuth.NumSamples;

            for (int i = 0; i < curveAuth.NumSamples; i++)
            {
                var samplePoint = (float)i / (curveAuth.NumSamples - 1);
                var sampleValue = curveAuth.Curve.Evaluate(samplePoint);
                sampledCurveArray[i] = sampleValue;
            }
            var blobAssetRef = builder.CreateBlobAssetReference<DiscretizedCurve>(Allocator.Temp);
            return new DotsCurve { BlobRef = blobAssetRef };
        }
    }
}

[Serializable]
public class CurveAuthWrapper
{
    public AnimationCurve Curve;
    public int NumSamples;
}