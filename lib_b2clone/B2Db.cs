using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using b2clone_lib.Commands;
using b2clone_lib.Models;
using b2clone_lib.Queries;
using Memstate;
using Memstate.Configuration;

namespace lib_b2clone
{
    public class B2Db
    {
        private BlockingCollection<FileObject> _fileQueue;
        private string WireFileName = "b2clone-db";
        private Engine<DbRepo> dbEngine;
        
        public B2Db()
        {
            _fileQueue = new BlockingCollection<FileObject>();
            DbInit();
            Task.Run(QueueHandler);
        }

        private void DbInit()
        {
            Config config = Config.Current;
            config.SerializerName = "Wire";
            EngineSettings settings = config.GetSettings<EngineSettings>();
            settings.StreamName = WireFileName;
            dbEngine = Engine.Start<DbRepo>().Result;
        }

        public void AddQueue(FileObject fileObject)
        {
            _fileQueue.Add(fileObject);
        }

        public async Task Add(FileObject fileObject)
        {
            await dbEngine.Execute(new CreateFileObject(fileObject));
        }

        public IDictionary<string, FileObject> GetDbFileObjects()
        {
            return dbEngine.Execute(new GetAllFileObjects()).Result;
        }

        public bool SafeExit()
        {
            return _fileQueue.Count == 0;
        }

        ~B2Db()
        {
            dbEngine.DisposeAsync();
        }

        public bool IsExists(string filePath, ulong xHash)
        {
            FileObject fo = dbEngine.Execute(new GetFileObjectWithXhash(filePath, xHash)).Result;
            return fo != null;
        }

        public bool IsExists(string filePath)
        {
            FileObject fo = dbEngine.Execute(new GetFileObject(filePath)).Result;
            return fo != null;
        }

        public FileObject GetFileObject(string filePath)
        {
            FileObject fo = dbEngine.Execute(new GetFileObject(filePath)).Result;
            return fo;
        }

        public async Task UpdateFileObject(FileObject fileObject)
        {
            await dbEngine.Execute(new UpdateFileObject(fileObject));
        }

        private async Task QueueHandler()
        {
            while (true)
            {
                FileObject fileObject = _fileQueue.Take();
                
                FileObject oldFileObject = await dbEngine.Execute(new GetFileObject(fileObject.FilePath));
                if (oldFileObject == null)
                    await dbEngine.Execute(new CreateFileObject(fileObject));
                
                Console.WriteLine(fileObject.FilePath);
            }
        }
    }
}