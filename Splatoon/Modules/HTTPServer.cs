using Newtonsoft.Json;
using Splatoon.Structures;
using Splatoon.Utils;
using System.Net;
using System.Threading;

namespace Splatoon.Modules;

class HTTPServer : IDisposable
{
    HttpListener listener;
    Splatoon p;
    public HTTPServer(Splatoon p)
    {
        this.p = p;
        listener = new HttpListener()
        {
            Prefixes = { "http://127.0.0.1:" + p.Config.port + "/" }
        };
        listener.Start();
        new Thread((ThreadStart)delegate
        {
            while (listener != null && listener.IsListening)
            {
                try
                {
                    List<string> status = new List<string>();
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    var elementsName = request.QueryString.Get("namespace");
                    var directElements = request.QueryString.Get("elements");
                    var destroyElements = request.QueryString.Get("destroy");
                    var destroyAt = request.QueryString.Get("destroyAt");
                    var enableElements = request.QueryString.Get("enable");
                    var disableElements = request.QueryString.Get("disable");
                    var rawElement = request.QueryString.Get("raw");
                    var contents = "";
                    using (var a = new StreamReader(context.Request.InputStream))
                    {
                        contents = a.ReadToEnd();
                        //p.Log("Body length: " + contents.Length);
                        //p.Log(contents);
                    }
                    try
                    {
                        if (elementsName == null)
                        {
                            elementsName = "";
                        }
                        if (disableElements != null)
                        {
                            var names = disableElements.Split(',');
                            foreach (var n in names)
                            {
                                p.tickScheduler.Enqueue(delegate { p.CommandManager.SwitchState(n, false, true); });
                                status.Add("Disabling: " + n);
                            }
                        }

                        if (enableElements != null)
                        {
                            var names = enableElements.Split(',');
                            foreach (var n in names)
                            {
                                p.tickScheduler.Enqueue(delegate { p.CommandManager.SwitchState(n, true, true); });
                                status.Add("Enabling: " + n);
                            }
                        }

                        if (destroyElements != null)
                        {
                            foreach (var s in destroyElements.Split(','))
                            {
                                status.Add("Requesting destruction: " + s);
                                p.tickScheduler.Enqueue(delegate
                                {
                                    for (var i = p.dynamicElements.Count - 1; i >= 0; i--)
                                    {
                                        var de = p.dynamicElements[i];
                                        if (s == "*" || de.Name == s)
                                        {
                                            p.dynamicElements.RemoveAt(i);
                                        }
                                    }
                                });
                            }
                        }

                        if (directElements != null || rawElement != null || contents != null && contents != "")
                        {
                            var dynElem = new DynamicElement()
                            {
                                DestroyTime = new long[] { 0 },
                                Name = elementsName,
                            };

                            var Layouts = new List<Layout>();
                            var Elements = new List<Element>();
                            if (destroyAt != null)
                            {
                                var dAtArray = new List<long>();
                                foreach (var destr in destroyAt.Split(','))
                                {
                                    if (long.TryParse(destr, out var dAt) && dAt > 0)
                                    {
                                        dAtArray.Add(Environment.TickCount64 + dAt);
                                    }
                                    else
                                    {
                                        dAtArray.Add((long)Enum.Parse(typeof(DestroyCondition), destr, true));
                                    }
                                }
                                dynElem.DestroyTime = dAtArray.ToArray();
                            }
                            if (rawElement != null)
                            {
                                status.Add("Raw payload found");
                                //status.Add(rawElement);
                                ProcessElement(rawElement, ref Layouts, ref Elements);
                            }
                            if (contents != null && contents != "")
                            {
                                status.Add("Body payload found");
                                //status.Add(rawElement);
                                ProcessElement(contents, ref Layouts, ref Elements);
                            }
                            if (directElements != null)
                            {
                                var encodedElements = directElements.Split(',');
                                foreach (var e in encodedElements)
                                {
                                    //status.Add(directElements);
                                    string decoded;
                                    try
                                    {
                                        decoded = e.Decompress();
                                    }
                                    catch (Exception)
                                    {
                                        decoded = e.FromBase64UrlSafe();
                                    }
                                    ProcessElement(decoded, ref Layouts, ref Elements);
                                }
                            }
                            dynElem.Elements = Elements.ToArray();
                            dynElem.Layouts = Layouts.ToArray();
                            p.tickScheduler.Enqueue(delegate
                            {
                                p.dynamicElements.Add(dynElem);
                            });
                            status.Add($"Requesting dynamic element addition: {dynElem.Name} (Elements: {dynElem.Elements.Length}, " +
                                $"Layouts: {dynElem.Layouts.Length}, destroyAt: {dynElem.DestroyTime})");
                        }
                    }
                    catch (Exception e)
                    {
                        status.Add("Error:");
                        status.Add(e.Message);
                        status.Add(e.StackTrace);
                    }
                    HttpListenerResponse response = context.Response;
                    response.AppendHeader("Access-Control-Allow-Origin", "*");
                    string responseString = string.Join("\n", status);
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                catch (Exception e)
                {
                    p.tickScheduler.Enqueue(delegate { p.Log("Error: " + e + "\n" + e.StackTrace); });
                }
            }
        }).Start();
    }

    private void ProcessElement(string decoded, ref List<Layout> Layouts, ref List<Element> Elements)
    {
        if (decoded.StartsWith("~"))
        {
            //status.Add(decoded);
            var l = JsonConvert.DeserializeObject<Layout>(decoded.Substring(1));
            l.Enabled = true;
            foreach (var el in l.ElementsL) el.Enabled = true;
            Layouts.Add(l);
        }
        else
        {
            var l = JsonConvert.DeserializeObject<Element>(decoded);
            l.Enabled = true;
            Elements.Add(l);
        }
    }

    public void Dispose()
    {
        listener.Abort();
        listener = null;
    }
}
