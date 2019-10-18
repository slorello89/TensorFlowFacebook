using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TensorFlowFacebook.Controllers
{
    public class FacebookController : Controller
    {
        private static Dictionary<string, InboundMessage> _observedMessages = new Dictionary<string, InboundMessage>();
        private static string _nextTrainLabel = "";
        public IActionResult Index()
        {
            return View();
        }
        public const string TRAIN = "train";
        [HttpPost]
        public IActionResult Status([FromBody]StatusMessage request)
        {
            var status = JsonConvert.SerializeObject(request);
            Debug.WriteLine(status);
            return new StatusCodeResult(204);
        }

        [HttpPost]
        public IActionResult Inbound([FromBody]InboundMessage request)
        {
            try
            {
                if (_observedMessages.ContainsKey(request.message_uuid))
                {
                    return new StatusCodeResult(204);
                }
                _observedMessages.Add(request.message_uuid, request);                
                Debug.WriteLine(JsonConvert.SerializeObject(request));
                if (!string.IsNullOrEmpty(request.message.content.text))
                {
                    var split = request.message.content.text.Split(new[] { ' ' });
                    if (split.Length > 1)
                    {
                        if (split[0].ToLower() == TRAIN)
                        {
                            _nextTrainLabel = split[1];
                            
                        }
                    }
                }
                if(!string.IsNullOrEmpty(_nextTrainLabel) && request.message.content?.image?.url != null)
                {
                    ThreadPool.QueueUserWorkItem(ClassificationHandler.AddTrainingData, new ClassificationHandler.TrainRequest()
                    {
                        toId = request.to.id,
                        fromid = request.from.id,
                        imageUrl = request.message.content.image.url,
                        Label = _nextTrainLabel
                    });
                    _nextTrainLabel = "";
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(ClassificationHandler.ClassifyAndRespond,
                    new ClassificationHandler.ClassifyRequest()
                    {
                        toId = request.to.id,
                        fromid = request.from.id,
                        imageUrl = request.message.content.image.url
                    });
                }

                return new StatusCodeResult(204);
            }
            catch (Exception ex)
            {
                return new StatusCodeResult(204);
            }
            
        }

        public class StatusMessage 
        {
            public string message_uuid { get; set; }
            public string timestamp { get; set; }
            public string status { get; set; }
            public ToObj to { get; set; }
            public FromObj from { get; set; }
            public ErrorObj error { get; set; }
            public UsageObj usage { get; set; }
            public string client_ref { get; set; }

            public class ToObj
            {
                [JsonProperty("type")]
                public string type { get; set; }
                [JsonProperty("id")]
                public string id { get; set; }
                [JsonProperty("number")]
                public string number { get; set; }
            }

            public class FromObj
            {
                [JsonProperty("type")]
                public string type { get; set; }

                [JsonProperty("id")]
                public string id { get; set; }

                [JsonProperty("number")]
                public string number { get; set; }

            }
            public class ErrorObj
            {
                public int code { get; set; }
                public string reason { get; set; }
            }
            public class UsageObj
            {
                public string currency { get; set; }
                public string price { get; set; }
            }           
            
        }
        public class InboundMessage
        {
            [JsonProperty("message_uuid")]
            public string message_uuid { get; set; }

            [JsonProperty("to")]
            public ToObj to { get; set; }

            [JsonProperty("from")]
            public FromObj from { get; set; }

            [JsonProperty("timestamp")]
            public string timestamp { get; set; }

            [JsonProperty("message")]
            public MessageObj message { get; set; }
            public class ToObj
            {
                [JsonProperty("type")]
                public string type { get; set; }
                [JsonProperty("id")]
                public string id { get; set; }
                [JsonProperty("number")]
                public string number { get; set; }
            }
            
            public class FromObj
            {
                [JsonProperty("type")]
                public string type { get; set; }

                [JsonProperty("id")]
                public string id { get; set; }

                [JsonProperty("number")]
                public string number { get; set; }

            }
            public class MessageObj
            {
                [JsonProperty("content")]
                public Content content { get; set; }
            }
            public class Content {
                [JsonProperty("type")]
                public string type { get; set; }

                [JsonProperty("text")]
                public string text { get; set; }

                [JsonProperty("image")]
                public Image image { get; set; }

                [JsonProperty("audio")]
                public Audio audio { get; set; }

                [JsonProperty("video")]
                public Video video { get; set; }

                [JsonProperty("file")]
                public File file { get; set; }

                [JsonProperty("location")]
                public Location location { get; set; }
            }

            public class Image {
                [JsonProperty("url")]
                public string url { get; set; }

                [JsonProperty("caption")]
                public string caption { get; set; }
            }
            public class Audio
            {
                [JsonProperty("url")]
                public string url { get; set; }
            }
            public class Video
            {
                [JsonProperty("url")]
                public string url { get; set; }

                [JsonProperty("caption")]
                public string caption { get; set; }
            }
            public class File
            {
                [JsonProperty("url")]
                public string url { get; set; }

                [JsonProperty("caption")]
                public string caption { get; set; }
            }
            public class Location
            {
                [JsonProperty("lat")]
                public string lat { get; set; }

                [JsonProperty("long")]
                public string Longitude { get; set; }

                [JsonProperty("url")]
                public string url { get; set; }

                [JsonProperty("address")]
                public string address { get; set; }

                [JsonProperty("name")]
                public string name { get; set; }
            }

            
        }
    }
}