using System.Collections.Generic;

namespace PerfTests
{
  public static class TestDataFactory
  {
    public static TestData Create()
    {
      var inner1 = new Inner1
      {
        Value1 = "TestStringTestStringTestString2",
        Value2 = 909091231239911,
        Value3 = 234235211,
        //Value4 = new Inner1
        //{
        //  Value1 = "TestStringTestStringTestString3",
        //  Value2 = 14235368796,
        //  Value3 = 769679679,
        //  //Value4 = null,
        //  Value5 = new Inner2
        //  {
        //    Value1 = 67907807,
        //    Value2 = 8070780789789678
        //  }
        //},
        Value5 = new Inner2
        {
          Value1 = 124124124,
          Value2 = 233245232234
        }
      };

      var test = new TestData
      {
        Value1 = long.MaxValue / 2 + long.MaxValue / 3,
        Value2 = int.MaxValue / 2 + int.MaxValue / 3,
        Value3 = "TestStringTestStringTestString1",
        Value4 = inner1,
        Value5 = new Inner2
        {
          Value1 = 456456456,
          Value2 = 456456574556
        },
        Value6 = TestEnum.One,
        Value8 = new Inner3<int, string>
        {
          Value1 = 353254653,
          Value2 = "TestStringTestStringTestString4"
        },
        Value9 = new int[]
        {
          100,
          200,
          300,
          400,
          500,
          600,
          700,
          800,
          900
        },
        Value10 = new List<string>
        {
          "Value1",
          "Value2",
          "Value3",
          "Value4",
          "Value5",
          "Value6",
          "Value7"
        }
      };
      //test.Value7 = test;
      return test;
    }
  }
}
