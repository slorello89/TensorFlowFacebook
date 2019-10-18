using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TensorFlowFacebook.Sender;

namespace TensorFlowFacebook
{
    public class ClassificationHandler
    {
        public static void ClassifyAndRespond(object state) {
            var request = state as ClassifyRequest;
            var response = TFEngine.Instance.ClassifySingleImage(request.imageUrl);
            MessageSender.SendMessage(MessageSender.messageType.messenger, response, request.toId, request.fromid);
        }

        public static void AddTrainingData(object state)
        {
            var request = state as TrainRequest;
            var response = TFEngine.Instance.AddTrainingImage(request.imageUrl, request.Label);
            MessageSender.SendMessage(MessageSender.messageType.messenger, response, request.toId, request.fromid);
        }
        public class TrainRequest : Request
        {
            public string Label { get; set; }
        }
        public class ClassifyRequest : Request
        {
            

        }
        public abstract class Request
        {
            public string imageUrl { get; set; }
            public string toId { get; set; }
            public string fromid { get; set; }
        }
    }
}
