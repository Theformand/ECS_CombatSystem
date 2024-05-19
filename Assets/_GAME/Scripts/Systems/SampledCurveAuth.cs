using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SampledCurveAuth : MonoBehaviour
{
    [SerializeField] private AnimationCurve Curve;
    [SerializeField] private int NumSamples;

    public class Baker : Baker<SampledCurveAuth>
    {
        public override void Bake(SampledCurveAuth authoring)
        {
            //using var builder = new BlobBuilder(Allocator.Temp);
            //ref var sampledCurve = ref builder.ConstructRoot<SampledCurve>();
            //var sampledCurveArray = builder.Allocate(ref sampledCurve.Points,authoring.NumSamples);
            //sampledCurve.NumSamples = authoring.NumSamples;

            //for (int i = 0; i < authoring.NumSamples; i++)
            //{
            //    var samplePoint = (float)i / authoring.NumSamples-1;
            //    var sampleValue = authoring.Curve.Evaluate(samplePoint);
            //    sampledCurveArray[i] = sampleValue;
            //    Debug.Log("added point");
            //}

            //var blobAssetRef = builder.CreateBlobAssetReference<SampledCurve>(Allocator.Temp);
            //var curveRef = new DotsCurve { Value = blobAssetRef };
            //var ent = GetEntity(TransformUsageFlags.None);
            //AddComponent(ent, curveRef);
        }
    }
}
