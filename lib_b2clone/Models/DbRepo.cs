using System;
using System.Collections;
using System.Collections.Generic;

namespace b2clone_lib.Models
{
    public class DbRepo
    {
        public DbRepo() {}
        public IDictionary<string, FileObject> Files { get; } = new Dictionary<string, FileObject>();
    }
}