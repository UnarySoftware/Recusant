using System;
using System.IO;

namespace Unary.Core
{
    public class PackageStream : Stream
    {
        private readonly FileStream _fileStream;
        private readonly long _offset;
        private readonly long _size;
        private long _position;

        public PackageStream(string filePath, long offset, long size, int bufferSize)
        {
            if (offset < 0 || size < 0)
            {
                throw new ArgumentException("Offset and size must be non-negative.");
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
            _offset = offset;
            _size = size;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _size;

        public override long Position
        {
            get
            {
                return _position;
            }
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
            long remaining = _size - _position;

            if (remaining <= 0)
            {
                return 0;
            }

            int toRead = (int)System.Math.Min(count, remaining);

            _fileStream.Seek(_offset + _position, SeekOrigin.Begin);

            int bytesRead = _fileStream.Read(buffer, offset, toRead);

            _position += bytesRead;

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        newPosition = offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        newPosition = _position + offset;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        newPosition = _size + offset;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Invalid seek origin.");
                    }
            }

            if (newPosition < 0 || newPosition > _size)
            {
                throw new ArgumentOutOfRangeException("Seek position out of bounds.");
            }
            _position = newPosition;
            return _position;
        }

        public override void Flush()
        {
            // No-op for read-only
        }

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
}
