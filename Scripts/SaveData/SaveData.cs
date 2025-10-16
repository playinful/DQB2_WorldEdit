using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Net.Http;
using System.IO;
using System.Linq;
using Godot;
using System.Threading;
using System.Text;
using System.Runtime.CompilerServices;

namespace EyeOfRubiss
{
    /// <summary> Base class for loading DQB2 .BIN files. </summary>
    public class SaveData
    {
        private readonly byte[] ExpectedFileHeader = [ 0x61, 0x65, 0x72, 0x43 ];

        public string Path { get; set; }
        public string GetFileName() => System.IO.Path.GetFileName(Path);

        protected byte[] _Header { get; set; }
        protected byte[] _Buffer { get; set; }

        public bool IsLoaded { get; set; } = false;

        public bool UnsavedChanges { get; set; } = false;

        protected bool _TryLoad(string path, int headerLength)
        {
            if (!Godot.FileAccess.FileExists(path))
                return false;

            try
            {
                using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.ReadWrite);
                {
                    if (!ExpectedFileHeader.SequenceEqual(fileAccess.GetBuffer(4)))
                        return false;

                    fileAccess.Seek(0);
                    _Header = fileAccess.GetBuffer(headerLength);
                    _Buffer = Decompress(fileAccess.GetBuffer((long)fileAccess.GetLength() - headerLength));
                }
            }
            catch
            {
                return false;
            }

            Path = path;
            UnsavedChanges = false;
            return IsLoaded = true;
        }

        protected bool _QuickLoad(string path, int headerLength)
        {
            if (!Godot.FileAccess.FileExists(path))
                return false;

            try
            {
                using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.ReadWrite);
                {
                    if (!ExpectedFileHeader.SequenceEqual(fileAccess.GetBuffer(4)))
                        return false;

                    fileAccess.Seek(0);
                    _Header = fileAccess.GetBuffer(headerLength);
                    //_Buffer = Decompress(fileAccess.GetBuffer((long)fileAccess.GetLength() - headerLength));
                }
            }
            catch
            {
                return false;
            }

            Path = path;
            UnsavedChanges = false;
            return IsLoaded = true;
        }

        public virtual void Save(string path = null)
        {
            path ??= Path;

            byte[] data = [.. _Header, .. Compress(_Buffer, System.IO.Compression.CompressionLevel.Fastest)];
            byte[] size = BitConverter.GetBytes(data.Length);
            Array.Copy(size, 0, data, 0x10, size.Length);

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(data);

            UnsavedChanges = false;
        }
        public void Export(string path)
        {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(_Buffer);
        }
        public void Import(string path)
        {
            // TODO: Error handling maybe
            byte[] fileBytes = Godot.FileAccess.GetFileAsBytes(path);
            _Buffer = fileBytes;
        }

        public static byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);

            output.Flush();
            return output.ToArray();
        }
        public static byte[] Compress(byte[] data, System.IO.Compression.CompressionLevel compressionLevel)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            using (var zlib = new System.IO.Compression.ZLibStream(output, compressionLevel))
            {
                input.CopyTo(zlib);
                zlib.Flush();
            }
            return output.ToArray();
        }

        public void SetBuffer(byte[] newBuffer) => _Buffer = newBuffer;

        public int GetBufferSize() => _Buffer.Length;

        #region Data get/set operations
        public byte GetByte(int address, bool header = false) => (header ? _Header : _Buffer)[address];
        public void SetByte(int address, byte value, bool header = false)
        {
            (header ? _Header : _Buffer)[address] = value;
            UnsavedChanges = true;
        }
        public short GetInt16(int address, bool header = false) => BitConverter.ToInt16(header ? _Header : _Buffer, address);
        public void SetInt16(int address, short value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 2);
            UnsavedChanges = true;
        }
        public ushort GetUInt16(int address, bool header = false) => BitConverter.ToUInt16(header ? _Header : _Buffer, address);
        public void SetUInt16(int address, ushort value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 2);
            UnsavedChanges = true;
        }
        public int GetInt32(int address, bool header = false) => BitConverter.ToInt32(header ? _Header : _Buffer, address);
        public void SetInt32(int address, int value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 4);
            UnsavedChanges = true;
        }
        public uint GetUInt32(int address, bool header = false) => BitConverter.ToUInt32(header ? _Header : _Buffer, address);
        public void SetUInt32(int address, uint value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 4);
            UnsavedChanges = true;
        }
        public long GetInt64(int address, bool header = false) => BitConverter.ToInt64(header ? _Header : _Buffer, address);
        public void SetInt64(int address, long value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 8);
            UnsavedChanges = true;
        }
        public ulong GetUInt64(int address, bool header = false) => BitConverter.ToUInt64(header ? _Header : _Buffer, address);
        public void SetUInt64(int address, ulong value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 8);
            UnsavedChanges = true;
        }
        public float GetSingle(int address, bool header = false) => BitConverter.ToSingle(header ? _Header : _Buffer, address);
        public void SetSingle(int address, float value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 4);
            UnsavedChanges = true;
        }
        public double GetDouble(int address, bool header = false) => BitConverter.ToDouble(header ? _Header : _Buffer, address);
        public void SetDouble(int address, double value, bool header = false)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, header ? _Header : _Buffer, address, 8);
            UnsavedChanges = true;
        }
        public string GetString(int address, int length, bool header = false, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(header ? _Header : _Buffer, address, length);
        }
        public void SetString(int address, string value, int? length = null, bool header = false, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] stringBytes = encoding.GetBytes(value);
            length ??= stringBytes.Length;

            Fill(0, address, length, header);

            if (stringBytes.Length < length)
                length = stringBytes.Length;

            Array.Copy(stringBytes, 0, header ? _Header : _Buffer, address, (int)length);
            UnsavedChanges = true;
        }
        public bool GetBit(int address, int bit, bool header = false)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException();
            if (bit < 0)
                throw new ArgumentOutOfRangeException();

            return ((header ? _Header : _Buffer)[address] & (1 << bit)) != 0;
        }
        public void SetBit(int address, int bit, bool value, bool header = false)
        {
            if (bit > 7)
                throw new ArgumentOutOfRangeException();
            if (bit < 0)
                throw new ArgumentOutOfRangeException();

            byte[] bytes = header ? _Header : _Buffer;
            int left = (value ? 1 : 0) << bit;
            int right = bytes[address] & ((1 << bit) ^ 0b11111111);
            bytes[address] = (byte)(left | right);
            UnsavedChanges = true;
        }
        public Span<byte> GetBytes(System.Range? range = null, bool header = false)
        {
            range ??= ..;
            return header ? _Header.AsSpan((System.Range)range) : _Buffer.AsSpan((System.Range)range);
        }
        public Span<byte> GetBytes(int address, int? length = null, bool header = false)
        {
            length ??= (header ? _Header.Length : _Buffer.Length) - address;
            return header ? _Header.AsSpan(address, (int)length) : _Buffer.AsSpan(address, (int)length);
        }
        public void SetBytes(int address, byte[] bytes, int? length = null, bool header = false)
        {
            length ??= bytes.Length;
            Array.Copy(bytes, 0, header ? _Header : _Buffer, address, (int)length);
            UnsavedChanges = true;
        }

        public uint GetNumberBitwise(int address, int bit, int bitCount, bool header = false)
        {
            if (bit > 31 || bitCount > 31)
                throw new Exception("Integers larger than 32 bits are not supported.");
            if (bit < 0 || bitCount < 0)
                throw new Exception("Negative numbers are not allowed.");

            uint left = BitConverter.ToUInt32(header ? _Header : _Buffer, address) >> bit;
            uint right = (uint)((1 << bitCount) - 1);
            return left & right;
        }
        public void SetNumberBitwise(int address, int bit, int bitCount, uint value, bool header = false)
        {
            if (bit > 31 || bitCount > 31)
                throw new Exception("Integers larger than 32 bits are not supported.");
            if (bit < 0 || bitCount < 0)
                throw new Exception("Negative numbers are not allowed.");

            byte[] bytes = header ? _Header : _Buffer;
            uint bitMask = (uint)((1 << bitCount) - 1);
            uint newValue = (value & bitMask) << bit;
            uint oldValue = BitConverter.ToUInt32(bytes, address) & ((bitMask << bit) ^ 0b_11111111_11111111_11111111_11111111);
            Array.Copy(BitConverter.GetBytes(newValue | oldValue), 0, bytes, address, 4);
            UnsavedChanges = true;
            /*Array.Copy(BitConverter.GetBytes(
                (ushort)(
                    (ushort)(BitConverter.ToUInt16(_Buffer, DataAddress + 9) & 0b1111110000011111) |
                    (ushort)((value & 0b00011111) << 5)
                )
            ), 0, _Buffer, ListAddress, 2);*/
        }

        public void Extend(int length)
        {
            if (length <= GetBufferSize())
            {
                GD.Print("For some reason the length was considered smaller");
                return;
            }

            byte[] extension = new byte[length - GetBufferSize()];
            _Buffer = [.. _Buffer, .. extension];
        }
        public void Fill(byte value, int address = 0, int? length = null, bool header = false)
        {
            byte[] bytes = header ? _Header : _Buffer;
            length ??= bytes.Length - address;
            Array.Fill(bytes, value, (int)address, (int)length);
            UnsavedChanges = true;
        }
        #endregion
    }
}