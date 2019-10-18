using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace TensorFlowFacebook
{
    public class TokenGenerator
    {
        public static string GenerateToken(List<Claim> claims, string privateKey)
        {
            RSAParameters rsaParams;
            using (var tr = new StringReader(privateKey))
            {
                var pemReader = new PemReader(tr);
                var kp = pemReader.ReadObject();
                var privateRsaParams = kp as RsaPrivateCrtKeyParameters;
                rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
                return Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256);
            }
        }
    }
}
