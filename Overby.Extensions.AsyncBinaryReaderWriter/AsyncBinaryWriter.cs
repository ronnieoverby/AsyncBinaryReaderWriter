using System;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Overby.Extensions.AsyncBinaryReaderWriter
{
    // This abstract base class represents a writer that can write
    // primitives to an arbitrary stream. A subclass can override methods to
    // give unique encodings.
    //
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class AsyncBinaryWriter : IDisposable
    {
        public static readonly AsyncBinaryWriter Null = new AsyncBinaryWriter();

        protected Stream OutStream;
        private byte[] _buffer;    // temp space for writing primitives to.
        private Encoding _encoding;
        private Encoder _encoder;

        [OptionalField]  // New in .NET FX 4.5.  False is the right default value.
        private bool _leaveOpen;

        // This field should never have been serialized and has not been used since before v2.0.
        // However, this type is serializable, and we need to keep the field name around when deserializing.
        // Also, we'll make .NET FX 4.5 not break if it's missing.
#pragma warning disable 169
        [OptionalField]
        private char[] _tmpOneCharBuffer;
#pragma warning restore 169

        // Perf optimization stuff
        private byte[] _largeByteBuffer;  // temp space for writing chars.
        private int _maxChars;   // max # of chars we can put in _largeByteBuffer
        // Size should be around the max number of chars/string * Encoding's max bytes/char
        private const int LargeByteBufferSize = 256;

        // Protected default constructor that sets the output stream
        // to a null stream (a bit bucket).
        protected AsyncBinaryWriter()
        {
            OutStream = Stream.Null;
            _buffer = new byte[16];
            _encoding = new UTF8Encoding(false, true);
            _encoder = _encoding.GetEncoder();
        }

        public AsyncBinaryWriter(Stream output) : this(output, new UTF8Encoding(false, true), false)
        {
        }

        public AsyncBinaryWriter(Stream output, Encoding encoding) : this(output, encoding, false)
        {
        }

        public AsyncBinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (!output.CanWrite)
                throw new ArgumentException("Stream Not Writable");

            Contract.EndContractBlock();

            OutStream = output;
            _buffer = new byte[16];
            _encoding = encoding ?? throw new ArgumentNullException("encoding");
            _encoder = _encoding.GetEncoder();
            _leaveOpen = leaveOpen;
        }

        // Closes this writer and releases any system resources associated with the
        // writer. Following a call to Close, any operations on the writer
        // may raise exceptions. 
        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_leaveOpen)
                    OutStream.Flush();
                else
                    OutStream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual async Task<Stream> GetBaseStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await OutStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return OutStream;
        }

        // Clears all buffers for this writer and causes any buffered data to be
        // written to the underlying device. 
        public virtual Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return OutStream.FlushAsync(cancellationToken);
        }

        public virtual long Seek(int offset, SeekOrigin origin)
        {
            return OutStream.Seek(offset, origin);
        }

        // Writes a boolean to this stream. A single byte is written to the stream
        // with the value 0 representing false or the value 1 representing true.
        // 
        public virtual Task WriteAsync(bool value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)(value ? 1 : 0);
            return OutStream.WriteAsync(_buffer, 0, 1, cancellationToken);
        }

        // Writes a byte to this stream. The current position of the stream is
        // advanced by one.
        // 
        public virtual Task WriteAsync(byte value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return OutStream.WriteByteAsync(value, cancellationToken);
        }

        // Writes a signed byte to this stream. The current position of the stream 
        // is advanced by one.
        // 
        public virtual Task WriteAsync(sbyte value, CancellationToken cancellationToken = default(CancellationToken))
        {
            return OutStream.WriteByteAsync((byte)value, cancellationToken);
        }

        // Writes a byte array to this stream.
        // 
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        // 
        public virtual Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            Contract.EndContractBlock();
            return OutStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        // Writes a section of a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        // 
        public virtual Task WriteAsync(byte[] buffer, int index, int count, CancellationToken cancellationToken = default(CancellationToken))
        {
            return OutStream.WriteAsync(buffer, index, count, cancellationToken);
        }


        // Writes a character to this stream. The current position of the stream is
        // advanced by two.
        // Note this method cannot handle surrogates properly in UTF-8.
        // 
        public unsafe virtual Task WriteAsync(char ch, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Char.IsSurrogate(ch))
                throw new ArgumentException("Surrogates Not Allowed As Single Char");
            Contract.EndContractBlock();

            Contract.Assert(_encoding.GetMaxByteCount(1) <= 16, "_encoding.GetMaxByteCount(1) <= 16)");
            int numBytes = 0;
            fixed (byte* pBytes = _buffer)
            {
                numBytes = _encoder.GetBytes(&ch, 1, pBytes, _buffer.Length, true);
            }

            return OutStream.WriteAsync(_buffer, 0, numBytes, cancellationToken);
        }

        // Writes a character array to this stream.
        // 
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        // 
        public virtual Task WriteAsync(char[] chars, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            Contract.EndContractBlock();

            byte[] bytes = _encoding.GetBytes(chars, 0, chars.Length);
            return OutStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        // Writes a section of a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        // 
        public virtual Task WriteAsync(char[] chars, int index, int count, CancellationToken cancellationToken = default(CancellationToken))
        {
            byte[] bytes = _encoding.GetBytes(chars, index, count);
            return OutStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }


        // Writes a double to this stream. The current position of the stream is
        // advanced by eight.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual Task WriteAsync(double value, CancellationToken cancellationToken = default(CancellationToken))
        {
            ulong TmpValue = *(ulong*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            _buffer[4] = (byte)(TmpValue >> 32);
            _buffer[5] = (byte)(TmpValue >> 40);
            _buffer[6] = (byte)(TmpValue >> 48);
            _buffer[7] = (byte)(TmpValue >> 56);
            return OutStream.WriteAsync(_buffer, 0, 8, cancellationToken);
        }

        public virtual Task WriteAsync(decimal value, CancellationToken cancellationToken = default(CancellationToken))
        {
            GetBytes(value, _buffer);
            return OutStream.WriteAsync(_buffer, 0, 16, cancellationToken);
        }

        internal static void GetBytes(Decimal d, byte[] buffer)
        {
            Contract.Requires((buffer != null && buffer.Length >= 16), "[GetBytes]buffer != null && buffer.Length >= 16");

            var bits = decimal.GetBits(d);
            var lo = bits[0];
            var mid = bits[1];
            var hi = bits[2];
            var flags = bits[3];

            buffer[0] = (byte)lo;
            buffer[1] = (byte)(lo >> 8);
            buffer[2] = (byte)(lo >> 16);
            buffer[3] = (byte)(lo >> 24);

            buffer[4] = (byte)mid;
            buffer[5] = (byte)(mid >> 8);
            buffer[6] = (byte)(mid >> 16);
            buffer[7] = (byte)(mid >> 24);

            buffer[8] = (byte)hi;
            buffer[9] = (byte)(hi >> 8);
            buffer[10] = (byte)(hi >> 16);
            buffer[11] = (byte)(hi >> 24);

            buffer[12] = (byte)flags;
            buffer[13] = (byte)(flags >> 8);
            buffer[14] = (byte)(flags >> 16);
            buffer[15] = (byte)(flags >> 24);
        }

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        // 
        public virtual Task WriteAsync(short value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            return OutStream.WriteAsync(_buffer, 0, 2, cancellationToken);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        // 
        public virtual Task WriteAsync(ushort value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            return OutStream.WriteAsync(_buffer, 0, 2, cancellationToken);
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual Task WriteAsync(int value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            return OutStream.WriteAsync(_buffer, 0, 4, cancellationToken);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        // 
        public virtual Task WriteAsync(uint value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            return OutStream.WriteAsync(_buffer, 0, 4, cancellationToken);
        }

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        // 
        public virtual Task WriteAsync(long value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            return OutStream.WriteAsync(_buffer, 0, 8, cancellationToken);
        }

        // Writes an eight-byte unsigned integer to this stream. The current 
        // position of the stream is advanced by eight.
        // 
        public virtual Task WriteAsync(ulong value, CancellationToken cancellationToken = default(CancellationToken))
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            return OutStream.WriteAsync(_buffer, 0, 8, cancellationToken);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe virtual Task WriteAsync(float value, CancellationToken cancellationToken = default(CancellationToken))
        {
            uint TmpValue = *(uint*)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            return OutStream.WriteAsync(_buffer, 0, 4, cancellationToken);
        }


        // Writes a length-prefixed string to this stream in the BinaryWriter's
        // current Encoding. This method first writes the length of the string as 
        // a four-byte unsigned integer, and then writes that many characters 
        // to the stream.
        // 
        //[System.Security.SecuritySafeCritical]  // auto-generated
        public virtual async Task WriteAsync(String value, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            int len = _encoding.GetByteCount(value);
            await Write7BitEncodedIntAsync(len, cancellationToken).ConfigureAwait(false);

            if (_largeByteBuffer == null)
            {
                _largeByteBuffer = new byte[LargeByteBufferSize];
                _maxChars = _largeByteBuffer.Length / _encoding.GetMaxByteCount(1);
            }

            if (len <= _largeByteBuffer.Length)
            {
                //Contract.Assert(len == _encoding.GetBytes(chars, 0, chars.Length, _largeByteBuffer, 0), "encoding's GetByteCount & GetBytes gave different answers!  encoding type: "+_encoding.GetType().Name);
                _encoding.GetBytes(value, 0, value.Length, _largeByteBuffer, 0);
                await OutStream.WriteAsync(_largeByteBuffer, 0, len, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Aggressively try to not allocate memory in this loop for
                // runtime performance reasons.  Use an Encoder to write out 
                // the string correctly (handling surrogates crossing buffer
                // boundaries properly).  
                int charStart = 0;
                int numLeft = value.Length;
#if _DEBUG
                int totalBytes = 0;
#endif
                while (numLeft > 0)
                {
                    // Figure out how many chars to process this round.
                    int charCount = (numLeft > _maxChars) ? _maxChars : numLeft;
                    int byteLen;

                    checked
                    {
                        if (charStart < 0 || charCount < 0 || charStart + charCount > value.Length)
                        {
                            throw new ArgumentOutOfRangeException("charCount");
                        }

                        setbyteLen();

                        unsafe void setbyteLen()
                        {
                            fixed (char* pChars = value)
                            {
                                fixed (byte* pBytes = _largeByteBuffer)
                                {
                                    byteLen = _encoder.GetBytes(pChars + charStart, charCount, pBytes, _largeByteBuffer.Length, charCount == numLeft);
                                }
                            }
                        }
                    }
#if _DEBUG
                    totalBytes += byteLen;
                    Contract.Assert (totalBytes <= len && byteLen <= _largeByteBuffer.Length, "BinaryWriter::Write(String) - More bytes encoded than expected!");
#endif
                    await OutStream.WriteAsync(_largeByteBuffer, 0, byteLen, cancellationToken).ConfigureAwait(false);
                    charStart += charCount;
                    numLeft -= charCount;
                }
#if _DEBUG
                Contract.Assert(totalBytes == len, "BinaryWriter::Write(String) - Didn't write out all the bytes!");
#endif
            }
        }

        protected async Task Write7BitEncodedIntAsync(int value, CancellationToken cancellationToken)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                await WriteAsync((byte)(v | 0x80), cancellationToken).ConfigureAwait(false);
                v >>= 7;
            }
            await WriteAsync((byte)v, cancellationToken).ConfigureAwait(false);
        }

    }
}