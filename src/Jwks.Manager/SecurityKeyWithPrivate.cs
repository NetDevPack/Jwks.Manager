using Jwks.Manager.Jwk;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

namespace Jwks.Manager
{

    /// <summary>
    /// This points to a JSON file in the format: 
    /// {
    ///  "Modulus": "",
    ///  "Exponent": "",
    ///  "P": "",
    ///  "Q": "",
    ///  "DP": "",
    ///  "DQ": "",
    ///  "InverseQ": "",
    ///  "D": ""
    /// }
    /// </summary>
    public class SecurityKeyWithPrivate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Parameters { get; set; }
        public string KeyId { get; set; }
        public string Type { get; set; }
        public string Algorithm { get; set; }
        public DateTime CreationDate { get; set; }

        public void SetParameters(SecurityKey key, Algorithm alg)
        {
            if (key is RsaSecurityKey rsaKey)
            {
                Parameters = JsonConvert.SerializeObject(rsaKey.Rsa.ExportParameters(true), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else if (key is ECDsaSecurityKey eCDsa)
            {
                Parameters = JsonConvert.SerializeObject(eCDsa.ECDsa.ExportParameters(true), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else
            {
                Parameters = JsonConvert.SerializeObject(key, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            Type = alg.Kty();
            KeyId = key.KeyId;
            Algorithm = alg;
            CreationDate = DateTime.Now;
        }

        public SecurityKey GetSecurityKey()
        {
            var key = JsonConvert.DeserializeObject<JsonWebKey>(Parameters);
            if (Type == JsonWebAlgorithmsKeyTypes.RSA)
            {
                var parameters = new RSAParameters()
                {
                    Modulus = Base64UrlEncoder.DecodeBytes(key.N),
                    Exponent = Base64UrlEncoder.DecodeBytes(key.E),
                    D = string.IsNullOrEmpty(key.D) ? (byte[])null : Base64UrlEncoder.DecodeBytes(key.D),
                    P = string.IsNullOrEmpty(key.P) ? (byte[])null : Base64UrlEncoder.DecodeBytes(key.P),
                    Q = string.IsNullOrEmpty(key.Q) ? (byte[])null : Base64UrlEncoder.DecodeBytes(key.Q),
                    DP = string.IsNullOrEmpty(key.DP) ? (byte[])null : Base64UrlEncoder.DecodeBytes(key.DP),
                    DQ = string.IsNullOrEmpty(key.DQ) ? (byte[])null : Base64UrlEncoder.DecodeBytes(key.DQ),
                    InverseQ = string.IsNullOrEmpty(key.QI) ? (byte[])null : Base64UrlEncoder.DecodeBytes(key.QI)
                };
                return new RsaSecurityKey(parameters)
                {
                    KeyId = key.KeyId
                };
            }
            if (Type == JsonWebAlgorithmsKeyTypes.EllipticCurve)
            {
                var ecp = new ECParameters();
                var ecPoint = new ECPoint();
                ecp.Curve = CryptoService.GetCurveFromCrvValue(key.Crv);
                ecp.D = Base64UrlEncoder.DecodeBytes(key.D);
                ecPoint.X = Base64UrlEncoder.DecodeBytes(key.X);
                ecPoint.Y = Base64UrlEncoder.DecodeBytes(key.Y);
                ecp.Q = ecPoint;
                
                return new ECDsaSecurityKey(ECDsa.Create(ecp))
                {
                    KeyId = key.KeyId
                };
            }
            return key;
        }


        public SigningCredentials GetSigningCredentials()
        {
            return new SigningCredentials(GetSecurityKey(), Algorithm);
        }
    }
}