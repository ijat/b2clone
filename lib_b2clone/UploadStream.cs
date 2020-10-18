using System;
using System.IO;
using Memstate.Models.KeyValue;

namespace lib_b2clone
{
    public class ProgressUpdateEventArgs : EventArgs
    {
        public float Percent { get; }
        public long CurrentBytes { get; }
        public long TotalBytes { get; }

        public ProgressUpdateEventArgs(long currentBytes, long totalBytes)
        {
            CurrentBytes = currentBytes;
            TotalBytes = totalBytes;
            Percent = (1.0f * currentBytes / totalBytes) * 100;
        }
    }

    public class UploadParts
    {
        public long LastBytes {get;set;}
        public long CurrentBytes { get; set; }
        public int PartNumber { get; set; }

        public UploadParts(long lastBytes, long currentBytes, int partNumber)
        {
            LastBytes = lastBytes;
            CurrentBytes = currentBytes;
            PartNumber = partNumber;
        }
    }
    
    public class UploadStream : Stream 
    {
        private Stream m_input = null;
        private long m_length = 0L;
        private long m_position = 0L;
        public event EventHandler OnProgressUpdate;

        public UploadStream(Stream input)
        {
            m_input = input;
            m_length = input.Length;
        }
        public override void Flush()
        {
            m_input.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = m_input.Read(buffer, offset, count);
            m_position += n;
            _OnProgressUpdate(new ProgressUpdateEventArgs(m_position, m_length));
            return n;
        }
        
        protected virtual void _OnProgressUpdate(ProgressUpdateEventArgs e)
        {
            EventHandler handler = OnProgressUpdate;
            handler?.Invoke(this, e);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_input.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {    
            m_input.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => m_length;

        public override long Position
        {
            get => m_position;
            set => throw new System.NotImplementedException();
        }
    }
}