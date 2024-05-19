using System;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.PackageManager;
using UnityEngine;

public class ConfigAuth : MonoBehaviour
{
    public CurveAuthWrapper RocketHeightCurve;

    public class Baker : Baker<ConfigAuth>
    {
        public override void Bake(ConfigAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.None);
            AddComponent(ent, new Config
            {
                RocketHeightCurve = BakeCurve(authoring.RocketHeightCurve),
            });
        }

        public DotsCurve BakeCurve(CurveAuthWrapper curveAuth)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var sampledCurve = ref builder.ConstructRoot<SampledCurve>();
            var sampledCurveArray = builder.Allocate(ref sampledCurve.Points, curveAuth.NumSamples);
            sampledCurve.NumSamples = curveAuth.NumSamples;

            for (int i = 0; i < curveAuth.NumSamples; i++)
            {
                var samplePoint = (float)i / (curveAuth.NumSamples - 1);
                var sampleValue = curveAuth.Curve.Evaluate(samplePoint);
                sampledCurveArray[i] = sampleValue;
            }
            var blobAssetRef = builder.CreateBlobAssetReference<SampledCurve>(Allocator.Temp);
            return  new DotsCurve { Value = blobAssetRef };
        }
    }
}

[Serializable]
public class CurveAuthWrapper
{
    public AnimationCurve Curve;
    public int NumSamples;
}