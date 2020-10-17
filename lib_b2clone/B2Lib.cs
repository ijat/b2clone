using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using b2clone_lib;
using b2clone_lib.Models;
using B2Net;
using B2Net.Models;
using MimeTypes;
using Serilog;

namespace lib_b2clone
{
    public class B2Lib
    {
        private B2UploadUrl UploadUrl { get; set; }
        private B2Options B2Options { get; set; }
        private B2Db B2Db { get; }
        private B2Client Client { get; }
        private B2FileList B2FileList { get; set; }
        
        private ConcurrentBag<UploadObject> UploadList { get; set; } = new ConcurrentBag<UploadObject>();

        public B2Lib(string keyId, string appId, string bucketId)
        {
            try
            {
                B2Db = new B2Db();
                B2Options = new B2Options()
                {
                    BucketId = bucketId,
                    PersistBucket = true,
                    KeyId = keyId,
                    ApplicationKey = appId,
                };
                Client = new B2Client(B2Options);
                UploadUrl = Client.Files.GetUploadUrl().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            } 
        }

        private async Task GetB2List()
        {
            Log.Information("Getting updates from B2");
            B2FileList = await Client.Files.GetList();
            Log.Information("Update received | Total objects = {0}", B2FileList.Files.Count);
        }

        public async Task ScanFolderForUploads(string localPath, string b2Path)
        {
            try
            {
                if (B2FileList == null)
                    await GetB2List();
                
                IDictionary<string, FileObject> dbFileObjects = B2Db.GetDbFileObjects();
                Log.Information("Scan started | Path = {0}", localPath);

                int lastToUpload = UploadList.Count;


                int count = 0;

                IEnumerable<string> dirEnumerable =
                    Directory.EnumerateFiles(localPath, "*.*", SearchOption.AllDirectories);

                Stopwatch stopWatch = Stopwatch.StartNew();

                await dirEnumerable.ForEachAsync(1, async filePath =>
                {
                    try
                    {
                        Interlocked.Increment(ref count);
                        ulong xhash = Utils.FilepathToXhash(filePath);

                        Log.Verbose("[Thread {1}] Path: {0}", filePath,
                            Thread.CurrentThread.ManagedThreadId, xhash);

                        B2File existingB2File = B2FileList.Files.FirstOrDefault(
                            x => x.FileName == Utils.GetB2Filename(filePath, localPath, b2Path));
                        if (existingB2File != null)
                            await HandleIfExistsInCloud(B2Db, filePath, existingB2File, xhash, localPath, b2Path);
                        else
                            await HandleNotExistsInCloud(B2Db, filePath, xhash, localPath, b2Path);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                });

                Log.Information(
                    "Scan finished | Path = {0}, B2 Path = {1}, Total = {2}, To Upload = {3}, Execution time = {4} s",
                    localPath, b2Path, count, UploadList.Count - lastToUpload, 
                    stopWatch.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private DateTime progressLastUpdate = DateTime.Now;
        private long progressLastLength = 0;
        private Queue<double> progressSpeedAverage = new Queue<double>(8);
        public async Task UploadToB2()
        {
            IDictionary<string, FileObject> dbFileObjects = B2Db.GetDbFileObjects();
            if (UploadList.Count == 0)
            {
                Log.Information("No local changes | Nothing to upload.");
                return;
            }

            Log.Information("Upload started");
            Log.Information("Total files to upload = {0}", UploadList.Count);
            Stopwatch stopWatch = Stopwatch.StartNew();
            int currentIndex = 0;
            foreach(UploadObject uploadObject in UploadList)
            {
                try
                {
                    // reset progress bar 
                    progressLastLength = 0;
                    progressSpeedAverage.Clear();
                    for (short i = 0; i<8; i++) progressSpeedAverage.Enqueue(0); 
                    progressLastUpdate = DateTime.Now;
                    
                    string b2Path = Utils.GetB2Filename(uploadObject.FileObject.FilePath, uploadObject.ParentPath, uploadObject.B2Path);
                    Log.Information("File = {0} | B2 Path = {1}", uploadObject.FileObject.FilePath, "/" + b2Path);
                    
                    string contentType = MimeTypeMap.GetMimeType(Path.GetExtension(uploadObject.FileObject.FilePath));
                    
                    await using (FileStream fileStream = new FileStream(uploadObject.FileObject.FilePath, FileMode.Open,
                        FileAccess.Read, FileShare.ReadWrite))
                    {
                        await using (UploadStream uploadStream = new UploadStream(fileStream))
                        {
                            uploadStream.OnProgressUpdate += UploadStreamOnProgressUpdate;
                            B2File file = await Client.Files.Upload(uploadStream,
                                b2Path,
                                UploadUrl, contentType, true, dontSHA: true);
                            if (file != null)
                            {
                                uploadObject.FileObject.B2Files.Add(file.FileId, file);
                                uploadObject.FileObject.DateModified = DateTime.Now;
                                await B2Db.UpdateFileObject(uploadObject.FileObject);
                            }
                        }
                    }
                    Log.Information("\nTotal Progress = {2}% [{0}/{1}]", ++currentIndex, UploadList.Count,
                        Math.Round((Convert.ToDouble(currentIndex) / Convert.ToDouble(UploadList.Count)) * 100));
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
            Log.Information("Upload finished | Execution time = {0} seconds", stopWatch.Elapsed.TotalSeconds);
        }
        
        private void UploadStreamOnProgressUpdate(object sender, EventArgs e)
        {
            ProgressUpdateEventArgs updateEventArgs = e as ProgressUpdateEventArgs;
            if (updateEventArgs != null)
            {
                if (progressLastLength == 0)
                {
                    progressLastLength = updateEventArgs.CurrentBytes;
                    progressLastUpdate = DateTime.Now;
                }
                else
                {
                    double secondsDiff = (DateTime.Now - progressLastUpdate).TotalSeconds;
                    double currentSpeed = ((updateEventArgs.CurrentBytes - progressLastLength) / secondsDiff) ;
                    progressSpeedAverage.Enqueue(currentSpeed);
                    progressSpeedAverage.Dequeue();
                    progressLastLength = updateEventArgs.CurrentBytes;
                    progressLastUpdate = DateTime.Now;
                }

                double currentAvgSpeed = progressSpeedAverage.Average();
                string prefix = "KB/s";
                if (currentAvgSpeed > (1024 * 1024))
                {
                    currentAvgSpeed = Math.Round(currentAvgSpeed / (1024 * 1024), 2);
                    prefix = "MB/s";
                }
                else
                {
                    currentAvgSpeed = Math.Round(currentAvgSpeed / 1024, 2);
                }
                
                Console.Write("└─ Uploading {0}% | {4} / {3} | {1} {2}        \r", Math.Round(updateEventArgs.Percent, 2), 
                    currentAvgSpeed, prefix,
                    Utils.FormatFileSize(updateEventArgs.TotalBytes),
                    Utils.FormatFileSize(updateEventArgs.CurrentBytes));
            }
        }

        private async Task HandleNotExistsInCloud(B2Db b2Db, string filePath, ulong xhash, string parentPath, string b2Path)
        {
            FileObject existingFileObject = b2Db.GetFileObject(filePath);
            // fo exists but not uploaded yet
            if (existingFileObject != null && existingFileObject.B2Files.Count == 0)
            {
                UploadList.Add(new UploadObject(existingFileObject, parentPath, b2Path));
            }
            // fo exists and b2file exits but not available on cloud, so clear b2file object
            else if (existingFileObject != null && existingFileObject.B2Files.Count >= 0)
            {
                existingFileObject.B2Files.Clear();
                existingFileObject.DateModified = DateTime.Now;
                await b2Db.UpdateFileObject(existingFileObject);
                UploadList.Add(new UploadObject(existingFileObject, parentPath, b2Path));
            }
            // no fo, no b2file
            else
            {
                FileObject tempFileObject = new FileObject(filePath, xhash);
                await b2Db.Add(tempFileObject);
                UploadList.Add(new UploadObject(tempFileObject, parentPath, b2Path));
            }
        }

        private async Task HandleIfExistsInCloud(B2Db b2Db, string filePath, B2File existingB2File, ulong xhash,
            string parentPath, string b2Path)
        {
            // try find local object in db
            FileObject existingFileObject = b2Db.GetFileObject(filePath);
            if (existingFileObject != null && existingFileObject.B2Files.ContainsKey(existingB2File.FileId))
            {
                // local object xhash diff from cloud, then mark this for upload
                if (existingFileObject.Xhash != xhash)
                {
                    existingFileObject.Xhash = xhash;
                    UploadList.Add(new UploadObject(existingFileObject, parentPath, b2Path));
                }
            }
            // if existing file object is available but b2file object is not available
            else if (existingFileObject != null && !existingFileObject.B2Files.ContainsKey(existingB2File.FileId))
            {
                string sha1Hash = Utils.FilepathToSha1Hash(filePath);

                // update b2file object and does not need to reupload
                if (sha1Hash.Equals(existingB2File.ContentSHA1.Replace("unverified:",""), StringComparison.InvariantCultureIgnoreCase))
                {
                    existingFileObject.B2Files.Add(existingB2File.FileId, existingB2File);
                    existingFileObject.DateModified = DateTime.Now;
                    await b2Db.UpdateFileObject(existingFileObject);
                }
                else // mark this object to upload if diff sha
                {
                    existingFileObject.Xhash = xhash;
                    UploadList.Add(new UploadObject(existingFileObject, parentPath, b2Path));
                }
            }
            else
            {
                // local not available, so check it's sha1hash when same add to db or mark to upload
                FileObject tempFileObject = new FileObject(filePath, xhash);
                string sha1Hash = Utils.FilepathToSha1Hash(filePath);
                if (sha1Hash.Equals(existingB2File.ContentSHA1.Replace("unverified:",""), StringComparison.InvariantCultureIgnoreCase))
                {
                    tempFileObject.B2Files.Add(existingB2File.FileId, existingB2File);
                    await b2Db.Add(tempFileObject);
                }
                else
                {
                    UploadList.Add(new UploadObject(tempFileObject, parentPath, b2Path));
                }
            }
        }
    }
}