using System.Collections.Generic;
using System.Linq;
using b2clone_lib.Models;
using Memstate;

namespace b2clone_lib.Queries
{
    public class GetFileObjectWithXhash : Query<DbRepo, FileObject>
    {
        private string FilePath { get; }
        private ulong XHash { get; }
        
        public GetFileObjectWithXhash(string filePath, ulong xHash)
        {
            this.FilePath = filePath;
            this.XHash = xHash;
        }
        
        public override FileObject Execute(DbRepo model)
        {
            KeyValuePair<string, FileObject> t = model.Files.FirstOrDefault(
                x => x.Value.FilePath == FilePath 
                     && x.Value.Xhash == XHash);
            return t.Value;
        }
    }
}