using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HashDepot;

namespace b2clone_lib
{
    public static class Utils
    {
        private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        private static readonly unsafe uint* _lookup32UnsafeP = (uint*)GCHandle.Alloc(_lookup32Unsafe,GCHandleType.Pinned).AddrOfPinnedObject();

        private static uint[] CreateLookup32Unsafe()
        {
            uint[] result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s=i.ToString("X2");
                if(BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        }

        public static unsafe string ByteArrayToHexViaLookup32Unsafe(byte[] bytes)
        {
            uint* lookupP = _lookup32UnsafeP;
            char[] result = new char[bytes.Length * 2];
            fixed(byte* bytesP = bytes)
            fixed (char* resultP = result)
            {
                uint* resultP2 = (uint*)resultP;
                for (int i = 0; i < bytes.Length; i++)
                {
                    resultP2[i] = lookupP[bytesP[i]];
                }
            }
            return new string(result);
        }


        public static string FilepathToSha1Hash(string filePath)
        {
            string hash = String.Empty;
            using (FileStream fileStream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using(SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
                {
                    hash = BitConverter
                        .ToString(cryptoProvider.ComputeHash(fileStream)).Replace("-","");
                }
            }
            return hash;
        }
        
        public static string BytesToSha1Hash(byte[] fileData) {
            using (var sha1 = SHA1.Create()) {
                return HexStringFromBytes(sha1.ComputeHash(fileData));
            }
        }
        
        private static string HexStringFromBytes(byte[] bytes) {
            var sb = new StringBuilder();
            foreach (byte b in bytes) {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }
        
        public static ulong FilepathToXhash(string filePath)
        {
            ulong result;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) 
            {
                result = XXHash.Hash64(fileStream);
            }
            return result;
        }
        
        public static string GetB2Filename(string filePath, string parentDir, string b2Dir)
        {
            string b2RealPath = b2Dir.StartsWith("/") ? b2Dir.Remove(0, 1) : b2Dir;
            string resultPath = b2RealPath + filePath.Replace(parentDir, "")
                .Replace("\\", "/");
            return resultPath.StartsWith("/") ? resultPath.Remove(0, 1) : resultPath;
        }
        
        public static Task ForEachAsync<T>(
            this IEnumerable<T> source, int dop, Func<T, Task> body) 
        { 
            return Task.WhenAll( 
                from partition in Partitioner.Create(source).GetPartitions(dop) 
                select Task.Run(async delegate { 
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);/*.ContinueWith(t => 
                            {
                                //observe exceptions
                            });*/
                })); 
        }
        
        public static async Task RunWithMaxDegreeOfConcurrency<T>(
            int maxDegreeOfConcurrency, IEnumerable<T> collection, Func<T, Task> taskFactory)
        {
            List<Task> activeTasks = new List<Task>(maxDegreeOfConcurrency);
            foreach (Task task in collection.Select(taskFactory))
            {
                activeTasks.Add(task);
                if (activeTasks.Count == maxDegreeOfConcurrency)
                {
                    await Task.WhenAny(activeTasks.ToArray());
                    //observe exceptions here
                    activeTasks.RemoveAll(t => t.IsCompleted); 
                }
            }
            await Task.WhenAll(activeTasks.ToArray()).ContinueWith(t => 
            {
                //observe exceptions in a manner consistent with the above   
            });
        }
        
        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }
    }
}