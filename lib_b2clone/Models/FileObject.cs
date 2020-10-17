using System;
using System.Collections.Generic;
using B2Net.Models;

namespace b2clone_lib.Models
{
    public class FileObject
    {
        public Guid Id { get; }
        public string FilePath { get; }
        public ulong Xhash { get; set; }
        public DateTime DateCreated { get; }
        public DateTime DateModified { get; set; }
        public Dictionary<string, B2File> B2Files { get; }

        public FileObject(string filePath, ulong xhash)
        {
            this.Id = Guid.NewGuid();
            this.FilePath = filePath;
            this.DateCreated = DateTime.Now;
            this.DateModified = DateTime.Now;
            this.Xhash = xhash;
            this.B2Files = new Dictionary<string, B2File>();
        }
    }
}