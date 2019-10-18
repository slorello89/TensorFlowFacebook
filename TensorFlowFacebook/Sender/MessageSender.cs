using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using TensorFlowFacebook;
using static TensorFlowFacebook.MessageRequest;
using static TensorFlowFacebook.MessageRequest.Message;

namespace TensorFlowFacebook.Sender
{
    public class MessageSender
    {
        private const string MESSAGING_URL = @"https://api.nexmo.com/v0.1/messages";
        private const int SECONDS_EXPIRY = 3600;
        public enum messageType
        {
            messenger
        }
        public static void SendMessage(messageType type, string message, string fromNumber, string toNumber)
        {
            try
            {
                var key = Startup.StaticConfiguration["Authentication:apiKey"];
                var secret = Startup.StaticConfiguration["Authentication:apiSecret"];
                var appId = Startup.StaticConfiguration["Authentication:appId"];
                var priavteKeyPath = Startup.StaticConfiguration["Authentication:privateKey"];
                string privateKey = "";
                using (var reader = File.OpenText(priavteKeyPath)) // file containing RSA PKCS1 private key
                    privateKey = reader.ReadToEnd();

                var jwt = TokenGenerator.GenerateToken(GetClaimsList(appId), privateKey);

                var requestObject = new MessageRequest()
                {
                    to = new To()
                    {
                        id = toNumber,
                        type = type.ToString()
                    },
                    from = new From()
                    {
                        id = fromNumber,
                        type = type.ToString()
                    },
                    message = new Message()
                    {
                        content = new Content()
                        {
                            type = "text",
                            text = message
                        },
                        messenger = new Messenger()
                        {
                            category = "RESPONSE"
                        }
                    }
                };
                var requestPayload = JsonConvert.SerializeObject(requestObject,new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore});
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(MESSAGING_URL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.PreAuthenticate = true;
                httpWebRequest.Headers.Add("Authorization", "Bearer " + jwt);
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream(), Encoding.Unicode))
                {
                    streamWriter.Write(requestPayload);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Console.WriteLine(result);
                    Console.WriteLine("Message Sent");
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            
        }

        private static List<Claim> GetClaimsList(string appId)
        {
            var t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var iat = new Claim("iat", ((Int32)t.TotalSeconds).ToString(), ClaimValueTypes.Integer32); // Unix Timestamp for right now
            var application_id = new Claim("application_id", appId); // Current app ID
            var exp = new Claim("exp", ((Int32)(t.TotalSeconds + SECONDS_EXPIRY)).ToString(), ClaimValueTypes.Integer32); // Unix timestamp for when the token expires
            var jti = new Claim("jti", Guid.NewGuid().ToString()); // Unique Token ID
            var claims = new List<Claim>() { iat, application_id, exp, jti };

            return claims;
        }
    }
}
