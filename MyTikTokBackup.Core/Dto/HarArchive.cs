using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyTikTokBackup.Core.Dto
{
    public partial class HarArchive
    {
        [JsonPropertyName("log")]
        public Log Log { get; set; }
    }

    public partial class Log
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("creator")]
        public Creator Creator { get; set; }

        [JsonPropertyName("pages")]
        public List<object> Pages { get; set; }

        [JsonPropertyName("entries")]
        public List<Entry> Entries { get; set; }
    }

    public partial class Creator
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public partial class Entry
    {
        [JsonPropertyName("startedDateTime")]
        public DateTimeOffset StartedDateTime { get; set; }

        [JsonPropertyName("time")]
        public double Time { get; set; }

        [JsonPropertyName("request")]
        public Request Request { get; set; }

        [JsonPropertyName("response")]
        public Response Response { get; set; }

        [JsonPropertyName("cache")]
        public Cache Cache { get; set; }

        [JsonPropertyName("timings")]
        public Timings Timings { get; set; }

        [JsonPropertyName("serverIPAddress")]
        public string ServerIpAddress { get; set; }

        [JsonPropertyName("_initiator")]
        public Initiator Initiator { get; set; }

        [JsonPropertyName("_priority")]
        public string Priority { get; set; }

        [JsonPropertyName("_resourceType")]
        public string ResourceType { get; set; }
    }

    public partial class Cache
    {
    }

    public partial class Initiator
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("stack")]
        public Stack Stack { get; set; }
    }

    public partial class Stack
    {
        [JsonPropertyName("callFrames")]
        public List<CallFrame> CallFrames { get; set; }
    }

    public partial class CallFrame
    {
        [JsonPropertyName("functionName")]
        public string FunctionName { get; set; }

        [JsonPropertyName("scriptId")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long ScriptId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("lineNumber")]
        public long LineNumber { get; set; }

        [JsonPropertyName("columnNumber")]
        public long ColumnNumber { get; set; }
    }

    public partial class Request
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("httpVersion")]
        public string HttpVersion { get; set; }

        [JsonPropertyName("headers")]
        public List<Header> Headers { get; set; }

        [JsonPropertyName("queryString")]
        public List<Header> QueryString { get; set; }

        [JsonPropertyName("cookies")]
        public List<Cooky> Cookies { get; set; }

        [JsonPropertyName("headersSize")]
        public long HeadersSize { get; set; }

        [JsonPropertyName("bodySize")]
        public long BodySize { get; set; }
    }

    public partial class Cooky
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("expires")]
        public object Expires { get; set; }

        [JsonPropertyName("httpOnly")]
        public bool HttpOnly { get; set; }

        [JsonPropertyName("secure")]
        public bool Secure { get; set; }
    }

    public partial class Header
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public partial class Response
    {
        [JsonPropertyName("status")]
        public long Status { get; set; }

        [JsonPropertyName("statusText")]
        public string StatusText { get; set; }

        [JsonPropertyName("httpVersion")]
        public string HttpVersion { get; set; }

        [JsonPropertyName("headers")]
        public List<Header> Headers { get; set; }

        [JsonPropertyName("cookies")]
        public List<object> Cookies { get; set; }

        [JsonPropertyName("content")]
        public Content Content { get; set; }

        [JsonPropertyName("redirectURL")]
        public string RedirectUrl { get; set; }

        [JsonPropertyName("headersSize")]
        public long HeadersSize { get; set; }

        [JsonPropertyName("bodySize")]
        public long BodySize { get; set; }

        [JsonPropertyName("_transferSize")]
        public long TransferSize { get; set; }
    }

    public partial class Content
    {
        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public partial class Timings
    {
        [JsonPropertyName("blocked")]
        public double Blocked { get; set; }

        [JsonPropertyName("dns")]
        public long Dns { get; set; }

        [JsonPropertyName("ssl")]
        public long Ssl { get; set; }

        [JsonPropertyName("connect")]
        public long Connect { get; set; }

        [JsonPropertyName("send")]
        public double Send { get; set; }

        [JsonPropertyName("wait")]
        public double Wait { get; set; }

        [JsonPropertyName("receive")]
        public double Receive { get; set; }

        [JsonPropertyName("_blocked_queueing")]
        public double BlockedQueueing { get; set; }
    }
}
