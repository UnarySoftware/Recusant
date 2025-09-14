using System;
using System.IO;

namespace Core
{
    public class PackageStream : Stream
    {
        private readonly FileStream _fileStream;
        private readonly long _offset;
        private readonly long _size;
        private long _position; // Relative position within the sub-stream

        public PackageStream(string filePath, long offset, long size, int bufferSize)
        {
            if (offset < 0 || size < 0)
            {
                throw new ArgumentException("Offset and size must be non-negative.");
            }

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
            _offset = offset;
            _size = size;
            _position = 0; // Start at the beginning of the sub-stream
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false; // Read-only for AssetBundle loading
        public override long Length => _size;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > _size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Clamp the read to stay within the sub-stream bounds
            long remaining = _size - _position;
            if (remaining <= 0) return 0; // End of sub-stream

            int toRead = (int)Math.Min(count, remaining);

            // Seek to the absolute position in the file
            _fileStream.Seek(_offset + _position, SeekOrigin.Begin);

            // Read from the file
            int bytesRead = _fileStream.Read(buffer, offset, toRead);

            // Update relative position
            _position += bytesRead;

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = _position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = _size + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid seek origin.");
            }

            if (newPosition < 0 || newPosition > _size)
            {
                throw new ArgumentOutOfRangeException("Seek position out of bounds.");
            }
            _position = newPosition;
            return _position;
        }

        public override void Flush() { } // No-op for read-only

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set length on a sub-stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Writing is not supported.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /*
    public class PackageStream : FileStream
    {
        public PackageStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) : base(path, mode, access, share, bufferSize)
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = base.Read(buffer, offset, count);
            UnityEngine.Debug.Log($"Read(offset: {offset}, count: {count}) => {result}");
            Position += result;
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long result = base.Seek(offset + FileOffset, origin);
            result -= FileOffset;
            UnityEngine.Debug.Log($"Seek(offset: {offset}, origin: {origin}) => {result}");
            return result;
        }

        public override long Length
        {
            get
            {
                UnityEngine.Debug.Log($"Length: {FileLength}");
                return FileLength;
            }
        }

        protected override void Dispose(bool disposing)
        {
            UnityEngine.Debug.Log($"Dispose");
            base.Dispose(disposing);
        }

        public long FileOffset = 0;
        public long FileLength = 0;
    }
    */
}
