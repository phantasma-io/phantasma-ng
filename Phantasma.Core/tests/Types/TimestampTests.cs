using System;
using System.Numerics;
using Phantasma.Core.Types.Structs;

namespace Phantasma.Core.Tests.Types;

using Xunit;

using Phantasma.Core.Types;

public class TimestampTests
{
    // write all the tests for the Timestamp class
    [Fact]
    public void TestTimestamp()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.Equal((uint)1234567890, timestamp.Value);
    }
    
    [Fact]
    public void TestTimestampString()
    {
        var timestamp = new Timestamp("1234567890");
        Assert.Equal((uint)1234567890, timestamp.Value);
        
        timestamp = new Timestamp("asjd");
        Assert.Equal(Timestamp.Null.Value, timestamp.Value);
    }
    
    [Fact]
    public void TestTimestampBigInt()
    {
        var timestamp = new Timestamp((BigInteger)1234567890);
        Assert.Equal((uint)1234567890, timestamp.Value);
    }
    
    [Fact]
    public void TestTimestampNow()
    {
        var timestamp = Timestamp.Now;
        Assert.True(timestamp.Value > 0);
    }
    
    [Fact]
    public void TestTimestampFromDateTime()
    {
        var timestamp = (Timestamp)new DateTime(2009, 02, 13, 23, 31, 30);
        Assert.Equal((uint)1234567890, timestamp.Value);
    }
    
    [Fact]
    public void TestTimestampCompare()
    {
        var timestamp1 = new Timestamp(1234567890);
        var timestamp2 = new Timestamp(1234567890);
        var timestamp3 = new Timestamp(1234567891);
        Assert.True(timestamp1 == timestamp2);
        Assert.True(timestamp1 != timestamp3);
        Assert.True(timestamp1 < timestamp3);
        Assert.True(timestamp3 > timestamp1);
        Assert.True(timestamp1 <= timestamp2);
        Assert.True(timestamp1 <= timestamp3);
        Assert.True(timestamp3 >= timestamp1);
        Assert.True(timestamp3 >= timestamp2);
    }
    
    [Fact]
    public void TestTimestampAdd()
    {
        var timestamp1 = new Timestamp(1234567890);
        var timestamp2 = (Timestamp) (timestamp1.Value + 10);
        Assert.Equal((uint)1234567900, timestamp2.Value);
    }
    
    [Fact]
    public void TestTimestampSubtract()
    {
        var timestamp1 = new Timestamp(1234567890);
        var timestamp2 = (Timestamp) (timestamp1.Value - 10);
        Assert.Equal((uint)1234567880, timestamp2.Value);
    }
    
    [Fact]
    public void TestTimestampToDateTime()
    {
        var timestamp = new Timestamp(1234567890);
        var dateTime = (DateTime)timestamp;
        Assert.Equal(2009, dateTime.Year);
        Assert.Equal(02, dateTime.Month);
        Assert.Equal(13, dateTime.Day);
        Assert.Equal(23, dateTime.Hour);
        Assert.Equal(31, dateTime.Minute);
        Assert.Equal(30, dateTime.Second);
    }
    
    [Fact]
    public void TestTimestampToUnixTime()
    {
        var timestamp = new Timestamp(1234567890);
        var unixTime = new DateTimeOffset (timestamp);
        Assert.Equal(1234567890, unixTime.ToUnixTimeSeconds());
    }
    
    [Fact]
    public void TestTimestampFromUnixTime()
    {
        var unixTime = DateTimeOffset.FromUnixTimeSeconds(1234567890);
        var timestamp = (Timestamp) unixTime.DateTime;
        Assert.Equal((uint)1234567890, timestamp.Value);
    }
    
    [Fact]
    public void TestTimestampToDateTimeOffset()
    {
        var timestamp = new Timestamp(1234567890);
        var dateTimeOffset = new DateTimeOffset (timestamp);
        Assert.Equal(2009, dateTimeOffset.Year);
        Assert.Equal(02, dateTimeOffset.Month);
        Assert.Equal(13, dateTimeOffset.Day);
        Assert.Equal(23, dateTimeOffset.Hour);
        Assert.Equal(31, dateTimeOffset.Minute);
        Assert.Equal(30, dateTimeOffset.Second);
    }
    
    [Fact]
    public void TestTimestampFromDateTimeOffset()
    {
        var dateTimeOffset = new DateTimeOffset (2009, 02, 13, 23, 31, 30, TimeSpan.Zero);
        var timestamp = (Timestamp) dateTimeOffset.DateTime;
        Assert.Equal((uint)1234567890, timestamp.Value);
    }
    
    [Fact]
    public void TestTimetampUsingEquals()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.True(timestamp.Equals((Timestamp) 1234567890));
    }
    
    [Fact]
    public void TestTimestampUsingEqualsToNull()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.False(timestamp.Equals(null));
    }
    
    [Fact]
    public void TestTimestampUsingEqualsOperator()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.True(timestamp == (Timestamp) 1234567890);
    }
    
    [Fact]
    public void TestTimestampGetSize()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.Equal(4, timestamp.GetSize());
    }
    
    [Fact]
    public void TestTimestampGetHashCode()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.Equal(1234567890, timestamp.GetHashCode());
    }

    [Fact]
    public void TestTimestampCompareTo()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.Equal(0, timestamp.CompareTo((Timestamp) 1234567890));
        Assert.Equal(-1, timestamp.CompareTo((Timestamp) 1234567891));
        Assert.Equal(1, timestamp.CompareTo((Timestamp) 1234567889));
    }
    
   [Fact]
    public void TestTimestampToString()
    {
        var timestamp = new Timestamp(1234567890);
        var expected = ((DateTime)timestamp).ToString();
        Assert.Equal(expected, timestamp.ToString());
    }
    
    [Fact]
    public void TestTimestampToStringWithFormat()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.Equal("13/02/2009 23:31:30", ((DateTime)timestamp).ToString("dd/MM/yyyy HH:mm:ss"));
    }
    
    [Fact]
    public void TestTimestampToStringFormat()
    {
        var timestamp = new Timestamp(1234567890);
        Assert.Equal("13/02/2009 23:31:30", timestamp.ToString("dd/MM/yyyy HH:mm:ss"));
    }
    
    [Fact]
    public void TestTimestampSubtraction()
    {
        var timestamp1 = new Timestamp(1234567890);
        var timestamp2 = new Timestamp(1234567880);
        var timestamp3 = timestamp1 - TimeSpan.FromSeconds(1234567880);
        var expected = new Timestamp(10);
        
        Assert.Equal(expected, timestamp1 - timestamp2);
        Assert.Equal(expected, timestamp3);
    }
    
    [Fact]
    public void TestTimestampAddition()
    {
        var timestamp1 = new Timestamp(1234567890);
        var expected = new Timestamp(1234740690);
        
        Assert.Equal(expected, timestamp1 + TimeSpan.FromDays(2));
    }
}
