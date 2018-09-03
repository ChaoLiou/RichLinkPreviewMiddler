using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RichLinkPreviewMiddler
{
    [DataContract]
    public class RLPMInfo
    {
        [DataMember]
        public Config config { get; set; }
        [DataMember]
        public List<Message> messages { get; set; }
    }

    [DataContract]
    public class Config
    {
        [DataMember]
        public string host { get; set; }
        [DataMember]
        public string imgroot { get; set; }
    }

    [DataContract]
    public class Message
    {
        [DataMember]
        public string filename { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public string image { get; set; }
        [DataMember]
        public string link { get; set; }
        [DataMember]
        public string script { get; set; }
    }
}
