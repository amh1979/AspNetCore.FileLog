/* ===============================================
* 功能描述：AspNetCore.FileLog.FileBufferingReadStream
* 创 建 者：WeiGe
* 创建日期：1/3/2019 2:10:58 PM
* ===============================================*/

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// A Stream that wraps another stream and enables rewinding by buffering the content as it is read.
    /// The content is buffered in memory up to a certain size and then spooled to a temp file on disk.
    /// The temp file will be deleted on Dispose.
    /// </summary>
    internal class FileBufferingReadStream : Stream
    {
        private const int _maxRentedBufferSize = 1024 * 1024; // 1MB
        private readonly Stream _inner;
        private readonly ArrayPool<byte> _bytePool;
        private readonly int _memoryThreshold;
        private readonly long? _bufferLimit;
        private string _tempFileDirectory;
        private readonly Func<string> _tempFileDirectoryAccessor;
        private string _tempFileName;

        private Stream _buffer;
        private byte[] _rentedBuffer;
        private bool _inMemory = true;
        private bool _completelyBuffered;

        private bool _disposed;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectoryAccessor"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            Func<string> tempFileDirectoryAccessor)
            : this(inner, memoryThreshold, bufferLimit, tempFileDirectoryAccessor, ArrayPool<byte>.Shared)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectoryAccessor"></param>
        /// <param name="bytePool"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            Func<string> tempFileDirectoryAccessor,
            ArrayPool<byte> bytePool)
        {
            _bytePool = bytePool;
            if (memoryThreshold < _maxRentedBufferSize)
            {
                _rentedBuffer = bytePool.Rent(memoryThreshold);
                _buffer = new MemoryStream(_rentedBuffer);
                _buffer.SetLength(0);
            }
            else
            {
                _buffer = new MemoryStream();
            }

            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _memoryThreshold = memoryThreshold;
            _bufferLimit = bufferLimit;
            _tempFileDirectoryAccessor = tempFileDirectoryAccessor ?? throw new ArgumentNullException(nameof(tempFileDirectoryAccessor));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectory"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            string tempFileDirectory)
            : this(inner, memoryThreshold, bufferLimit, tempFileDirectory, ArrayPool<byte>.Shared)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="memoryThreshold"></param>
        /// <param name="bufferLimit"></param>
        /// <param name="tempFileDirectory"></param>
        /// <param name="bytePool"></param>
        public FileBufferingReadStream(
            Stream inner,
            int memoryThreshold,
            long? bufferLimit,
            string tempFileDirectory,
            ArrayPool<byte> bytePool)
        {
            _bytePool = bytePool;
            if (memoryThreshold < _maxRentedBufferSize)
            {
                _rentedBuffer = bytePool.Rent(memoryThreshold);
                _buffer = new MemoryStream(_rentedBuffer);
                _buffer.SetLength(0);
            }
            else
            {
                _buffer = new MemoryStream();
            }

            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _memoryThreshold = memoryThreshold;
            _bufferLimit = bufferLimit;
            _tempFileDirectory = tempFileDirectory ?? throw new ArgumentNullException(nameof(tempFileDirectory));
        }
        /// <summary>
        /// 
        /// </summary>
        public bool InMemory
        {
            get { return _inMemory; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string TempFileName
        {
            get { return _tempFileName; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override bool CanSeek
        {
            get { return true; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override long Length
        {
            get { return _buffer.Length; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override long Position
        {
            get { return _buffer.Position; }
            // Note this will not allow seeking forward beyond the end of the buffer.
            set
            {
                ThrowIfDisposed();
                _buffer.Position = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            if (!_completelyBuffered && origin == SeekOrigin.End)
            {
                // Can't seek from the end until we've finished consuming the inner stream
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            else if (!_completelyBuffered && origin == SeekOrigin.Current && offset + Position > Length)
            {
                // Can't seek past the end of the buffer until we've finished consuming the inner stream
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            else if (!_completelyBuffered && origin == SeekOrigin.Begin && offset > Length)
            {
                // Can't seek past the end of the buffer until we've finished consuming the inner stream
                throw new NotSupportedException("The content has not been fully buffered yet.");
            }
            return _buffer.Seek(offset, origin);
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsDisposed { get{ return _disposed; } }
        private Stream CreateTempFile()
        {
            if (_tempFileDirectory == null)
            {
                Debug.Assert(_tempFileDirectoryAccessor != null);
                _tempFileDirectory = _tempFileDirectoryAccessor();
                Debug.Assert(_tempFileDirectory != null);
            }

            _tempFileName = Path.Combine(_tempFileDirectory, "ASPNETCORE_" + Guid.NewGuid().ToString() + ".tmp");
            return new FileStream(_tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 1024 * 16,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose | FileOptions.SequentialScan);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            if (_buffer.Position < _buffer.Length || _completelyBuffered)
            {
                // Just read from the buffer
                return _buffer.Read(buffer, offset, (int)Math.Min(count, _buffer.Length - _buffer.Position));
            }

            int read = _inner.Read(buffer, offset, count);

            if (_bufferLimit.HasValue && _bufferLimit - read < _buffer.Length)
            {
                Dispose();
                throw new IOException("Buffer limit exceeded.");
            }

            if (_inMemory && _buffer.Length + read > _memoryThreshold)
            {
                _inMemory = false;
                var oldBuffer = _buffer;
                _buffer = CreateTempFile();
                if (_rentedBuffer == null)
                {
                    oldBuffer.Position = 0;
                    var rentedBuffer = _bytePool.Rent(Math.Min((int)oldBuffer.Length, _maxRentedBufferSize));
                    var copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length);
                    while (copyRead > 0)
                    {
                        _buffer.Write(rentedBuffer, 0, copyRead);
                        copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length);
                    }
                    _bytePool.Return(rentedBuffer);
                }
                else
                {
                    _buffer.Write(_rentedBuffer, 0, (int)oldBuffer.Length);
                    _bytePool.Return(_rentedBuffer);
                    _rentedBuffer = null;
                }
            }

            if (read > 0)
            {
                _buffer.Write(buffer, offset, read);
            }
            else
            {
                _completelyBuffered = true;
            }

            return read;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_buffer.Position < _buffer.Length || _completelyBuffered)
            {
                // Just read from the buffer
                return await _buffer.ReadAsync(buffer, offset, (int)Math.Min(count, _buffer.Length - _buffer.Position), cancellationToken);
            }

            int read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);

            if (_bufferLimit.HasValue && _bufferLimit - read < _buffer.Length)
            {
                Dispose();
                throw new IOException("Buffer limit exceeded.");
            }

            if (_inMemory && _buffer.Length + read > _memoryThreshold)
            {
                _inMemory = false;
                var oldBuffer = _buffer;
                _buffer = CreateTempFile();
                if (_rentedBuffer == null)
                {
                    oldBuffer.Position = 0;
                    var rentedBuffer = _bytePool.Rent(Math.Min((int)oldBuffer.Length, _maxRentedBufferSize));
                    // oldBuffer is a MemoryStream, no need to do async reads.
                    var copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length);
                    while (copyRead > 0)
                    {
                        await _buffer.WriteAsync(rentedBuffer, 0, copyRead, cancellationToken);
                        copyRead = oldBuffer.Read(rentedBuffer, 0, rentedBuffer.Length);
                    }
                    _bytePool.Return(rentedBuffer);
                }
                else
                {
                    await _buffer.WriteAsync(_rentedBuffer, 0, (int)oldBuffer.Length, cancellationToken);
                    _bytePool.Return(_rentedBuffer);
                    _rentedBuffer = null;
                }
            }

            if (read > 0)
            {
                await _buffer.WriteAsync(buffer, offset, read, cancellationToken);
            }
            else
            {
                _completelyBuffered = true;
            }

            return read;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_rentedBuffer != null)
                {
                    _bytePool.Return(_rentedBuffer);
                }

                if (disposing)
                {
                    _buffer.Dispose();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileBufferingReadStream));
            }
        }
    }
}
