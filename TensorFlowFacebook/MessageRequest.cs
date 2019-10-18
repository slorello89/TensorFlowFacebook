using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TensorFlowFacebook
{
    public class MessageRequest
    {
        public To to { get; set; }

        public From from { get; set; }

        public Message message { get; set; }

        public string client_ref { get; set; }
        public class To
        {
            public string type { get; set; }
            public string id { get; set; }
            public string number { get; set; }
        }
        public class From
        {
            public string type { get; set; }
            public string id { get; set; }

            public string number { get; set; }

        }
        public class Message
        {
            public Content content { get; set; }

            public ViberServiceMsg viber_service_msg { get; set; }
            public Messenger messenger { get; set; }

            public Whatsapp whatsapp { get; set; }

            public class Content
            {
                public string type { get; set; }

                public string text { get; set; }

                public Image image { get; set; }

                public Audio audio { get; set; }

                public Video video { get; set; }

                public File file { get; set; }

                public Template template { get; set; }
                public class Image
                {
                    public string url { get; set; }
                    public string caption { get; set; }
                }
                public class Audio
                {
                    public string url { get; set; }
                }

                public class Video 
                {
                    public string url { get; set; }
                }
                public class File
                {
                    public string url { get; set; }
                    public string caption { get; set; }
                }
                public class Template
                {
                    public string name { get; set; }
                    public object[] parameters { get; set; }
                }
            }
            public class ViberServiceMsg
            {
                public string category { get; set; }

                public uint ttl { get; set; }

                public string type { get; set; }
            }

            public class Messenger
            {
                public string category { get; set; }
                public string tag { get; set; }
            }
            public class Whatsapp
            {
                public string policy { get; set; }
                public string locale { get; set; }
            }
            
        }
    }
}
