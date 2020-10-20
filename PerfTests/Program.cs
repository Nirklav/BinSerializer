using BenchmarkDotNet.Running;
using System;

namespace PerfTests
{
  class Program
  {
    // WARNING: Before benchmarking you should comment methods in BSDebug class.
    private static void Main(string[] args)
    {
      BenchmarkRunner.Run<Benchmark>();
      Console.ReadLine();
    }
  }
}
