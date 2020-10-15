namespace ThirtyNineEighty.BinarySerializer
{
    public interface IBinSerializable
  {
    void OnSerializing(SerializationInfo info);
    void OnDeserialized(DeserializationInfo info);
  }
}
