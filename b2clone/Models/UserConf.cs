using System.Collections.Generic;

namespace b2clone.Models
{
    public class UserConf
    {
        public string KeyId { get; set; }
        public string ApplicationId { get; set; }
        public string BucketId { get; set; }
        public Dictionary<string, string> PathMapper { get; set; }

        public UserConf(){}
        public UserConf(bool addTemplate)
        {
            if (addTemplate)
            {
                KeyId = "KEY_ID";
                ApplicationId = "APPLICATION_ID";
                BucketId = "BUCKET_ID";
                PathMapper = new Dictionary<string, string>();
                PathMapper.Add("/", @"C:\Users\Example\Documents");
                PathMapper.Add("/Downloads", @"C:\Users\Example\Downloads");
                PathMapper.Add("/ABC123/LUL", @"D:\");
            }
        }
    }
}