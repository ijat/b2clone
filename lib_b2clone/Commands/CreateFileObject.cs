using System;
using b2clone_lib.Models;
using Memstate;

namespace b2clone_lib.Commands
{
    public class CreateFileObject : Command<DbRepo,FileObject>
    {
        private FileObject FileObject { get; }
        
        public CreateFileObject(FileObject fileObject)
        {
            this.FileObject = fileObject;
        }
        
        public override FileObject Execute(DbRepo model)
        {
            model.Files[this.FileObject.FilePath] = this.FileObject;
            return this.FileObject;
        }
    }
}