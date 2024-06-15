using Unity.Entities;
using UnityEngine;

public class ECSAudioSourceAuth : MonoBehaviour
{
    public AudioSource AudioSource;

    public class Baker : Baker<ECSAudioSourceAuth>
    {
        public override void Bake(ECSAudioSourceAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            var Source = new UnityObjectRef<AudioSource>();
            Source.Value = authoring.AudioSource;
            AddComponent(ent, new ECSAudioSource()
            {
                Source = Source
            });
        }
    }
}
