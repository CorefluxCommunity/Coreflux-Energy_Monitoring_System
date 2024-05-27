using System;
using System.IO;
using System.Text;
using Renci.SshNet;
using Cloud.Interfaces;

namespace Cloud.Models
{
    public class PrivateKeyProvider : IPrivateKeyProvider
    {
        public PrivateKeyFile GetPrivateKey(string privateKeyString)
        {
            string formattedPrivateKey = privateKeyString.Replace("\n", Environment.NewLine);
            byte[] byteArray = Encoding.UTF8.GetBytes(formattedPrivateKey);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                return new PrivateKeyFile(stream);
            }
        }
    }
}