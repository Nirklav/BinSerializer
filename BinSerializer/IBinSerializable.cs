namespace ThirtyNineEighty.BinarySerializer
{
  public interface IBinSerializable
  {
    void OnSerializing(SerializationInfo info);
    void OnDeserialized(DeserializationInfo info);
  }

  public struct SerializationInfo
  {
  }

  public struct DeserializationInfo
  {
    public readonly int Version;

    public DeserializationInfo(int version)
    {
      Version = version;
    }
  }
}
