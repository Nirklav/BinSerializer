namespace ThirtyNineEighty.BinarySerializer
{
  public static class BinSerializerConfig
  {
    internal static volatile bool ValueArrayEnabled = true;

    public static void NotUseValueArrays() =>
      ValueArrayEnabled = false;
  }
}
