using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Newtonsoft.Json.Linq;
using Phantasma.Business.Tests;
using Phantasma.Core.Utils;

namespace Phantasma.Core.Tests.Performance;

using Xunit;
using Phantasma.Core.Performance;

[Collection(nameof(SystemTestCollectionDefinition))]
public class ProfileMarkerTests
{
    private struct Event
    {
        public string name;
        public long ts;
        public string ph;
        public string args;
        public int pid;
        public int tid;
        
        public Event(string name, long ts, string ph, string args, int pid, int tid)
        {
            this.name = name;
            this.ts = ts;
            this.ph = ph;
            this.args = args;
            this.pid = pid;
            this.tid = tid;
        }
    }
    
    // write tests for ProfileMarker
    [Fact()]
    public void TestProfileMarker()
    {
        var marker = new ProfileMarker("test");
        marker = new ProfileMarker();
        
        // var create a profile Marker parent
        var parent = new ProfileMarker("parent");
        marker = new ProfileMarker("test", parent);
        
        // create a ProfileSession 
        var bytes = new byte[8096];
        var stream = new MemoryStream(bytes);
        var stream2 = new MemoryStream(bytes);
        var reader = new BinaryReader(stream2);
        var session = new ProfileSession(stream);
        
        // re created the profileMarker
        parent = new ProfileMarker("parent");
        marker = new ProfileMarker("test", parent);
       
        //session.Pop("test");
        //session.Push("test2");
        session.Stop();
        Thread.Sleep(1000);
        var json = Encoding.UTF8.GetString(bytes);
        //var fixJson = concatJsons.Replace("\"traceEvents\":", "").Replace(", \"otherData\":{} }", "").Replace(" ", "");
        var fixJson = json.Replace(" ", "");
        var jsonObject = JsonObject.Parse(fixJson);
        var jsonArray = jsonObject["traceEvents"].AsArray();
        var events = new List<Event>();
        foreach (var value in (IEnumerable)jsonArray)
        {
            var obj = (JsonObject)value;
            var name = obj["name"].GetValue<string>();
            var ts = obj["ts"].GetValue<Int64>();
            var ph = obj["ph"].GetValue<string>();
            var args = "{}";
            var pid = 0;
            var tid = 0;
            events.Add(new Event(name, ts, ph, args, pid, tid));
        }
        Assert.True(events.Count == 5);
        Assert.True(events[0].name == "parent");
        Assert.True(events[0].ph == "B");
        Assert.True(events[1].name == "parent");
        Assert.True(events[1].ph == "E");
        Assert.True(events[2].name == "test");
        Assert.True(events[2].ph == "B");
        Assert.True(events[3].name == "test");
        Assert.True(events[3].ph == "E");
        Assert.True(events[4].name == "parent");
        Assert.True(events[4].ph == "E");
    }
    
    [Fact]
    public void TestProfileMarkerPop()
    {
        var marker = new ProfileMarker("test");
        marker = new ProfileMarker();

        // var create a profile Marker parent
        var parent = new ProfileMarker("parent");
        marker = new ProfileMarker("test", parent);

        // create a ProfileSession 
        var bytes = new byte[8096];
        var stream = new MemoryStream(bytes);
        var stream2 = new MemoryStream(bytes);
        var reader = new BinaryReader(stream2);
        var session = new ProfileSession(stream);

        // re created the profileMarker
        parent = new ProfileMarker("parent");
        marker = new ProfileMarker("test", parent);

        //session.Pop("test");
        session.Push("test2");
        session.Pop("test2");
        session.Stop();
        Thread.Sleep(1000);
        var json = Encoding.UTF8.GetString(bytes);
        var fixJson = json.Replace(" ", "");
        var jsonObject = JsonObject.Parse(fixJson);
        var jsonArray = jsonObject["traceEvents"].AsArray();
        var events = new List<Event>();
        foreach (var value in (IEnumerable)jsonArray)
        {
            var obj = (JsonObject)value;
            var name = obj["name"].GetValue<string>();
            var ts = obj["ts"].GetValue<Int64>();
            var ph = obj["ph"].GetValue<string>();
            var args = "{}";
            var pid = 0;
            var tid = 0;
            events.Add(new Event(name, ts, ph, args, pid, tid));
        }

        Assert.Equal(9, events.Count );
        Assert.True(events[0].name == "parent");
        Assert.True(events[0].ph == "B");
        Assert.True(events[1].name == "parent");
        Assert.True(events[1].ph == "E");
        Assert.True(events[2].name == "test");
        Assert.True(events[2].ph == "B");
        Assert.True(events[3].name == "test");
        Assert.True(events[3].ph == "E");
        Assert.True(events[4].name == "parent");
        Assert.True(events[4].ph == "E");
        Assert.True(events[5].name == "test2");
        Assert.True(events[5].ph == "B");
        Assert.True(events[6].name == "test2");
        Assert.True(events[6].ph == "E");
        Assert.Equal("test", events[7].name);
        Assert.Equal("E", events[7].ph);
        Assert.Equal("parent", events[8].name);
        Assert.Equal("E", events[8].ph);
    }

}
