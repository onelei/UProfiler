using System.IO;

namespace LemonFramework.UProfiler.Core
{
    public interface IBinarySerializable
    {
        void DeSerialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
    };
}
