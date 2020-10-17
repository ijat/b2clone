namespace b2clone_lib.Models
{
    public class UploadObject
    {
        public FileObject FileObject { get; }
        public string ParentPath { get; }
        public string B2Path { get; }

        public UploadObject(FileObject fileObject, string parentPath, string b2Path)
        {
            this.FileObject = fileObject;
            this.ParentPath = parentPath;
            this.B2Path = b2Path;
        }
    }
}