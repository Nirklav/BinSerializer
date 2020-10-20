using System.Security;

namespace ThirtyNineEighty.BinarySerializer
{
  [SecuritySafeCritical]
  public interface IBinSerializable
  {
    [SecuritySafeCritical]
    void OnSerializing(SerializationInfo info);

    [SecuritySafeCritical]
    void OnDeserialized(DeserializationInfo info);
  }


  [SecuritySafeCritical]
  public struct SerializationInfo
  {
  }


  [SecuritySafeCritical]
  public struct DeserializationInfo
  {
    public readonly int Version;

    [SecuritySafeCritical]
    public DeserializationInfo(int version)
    {
      Version = version;
    }
  }
}
