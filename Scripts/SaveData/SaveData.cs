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
using System.Buffers.Binary;

namespace EyeOfRubiss
{
    /// <summary> Base class for loading DQB2 .BIN files. </summary>
    public class SaveData
    {
        public string Path { get; set; }
        public string GetFileName() => System.IO.Path.GetFileName(Path);

        protected byte[] _Header { get; set; }
        public int HeaderSize { get { return _Header.Length; } }
        protected byte[] _Buffer { get; set; }
        public int BufferSize { get { return _Buffer.Length; } }

        public bool IsLoaded { get; set; } = false;

        public bool UnsavedChanges { get; set; } = false;

        public static bool TryLoad(string path, out SaveData result, int headerLength, bool decompress = true)
        {
            result = null;
            SaveData saveData = new();
            if (saveData._TryLoad(path, headerLength, decompress: decompress))
            {
                result = saveData;
                return true;
            }
            else return false;
        }
        protected bool _TryLoad(string path, int headerLength, bool decompress = true)
        {
            if (!Godot.FileAccess.FileExists(path))
                return false;

            try
            {
                using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.ReadWrite);
                {
                    fileAccess.Seek(0);
                    _Header = fileAccess.GetBuffer(headerLength);
                    if (decompress)
                        _Buffer = Util.Decompress(fileAccess.GetBuffer((long)fileAccess.GetLength() - headerLength));
                    else
                        _Buffer = fileAccess.GetBuffer((long)fileAccess.GetLength() - headerLength);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr(ex);
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
                    fileAccess.Seek(0);
                    _Header = fileAccess.GetBuffer(headerLength);
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
            Path = path;

            byte[] data = [.. _Header, .. Util.Compress(_Buffer, System.IO.Compression.CompressionLevel.Fastest)];
            byte[] size = BitConverter.GetBytes(data.Length);
            if (_Header.Length >= 0x14)
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

        public void SetBuffer(byte[] newBuffer) => _Buffer = newBuffer;

        public int GetBufferSize() => _Buffer.Length;

        #region Data get/set operations
        public byte GetByte(int address, bool header = false) => (header ? _Header : _Buffer)[address];
        public void SetByte(int address, byte value, bool header = false)
        {
            (header ? _Header : _Buffer)[address] = value;
            UnsavedChanges = true;
        }
        public short GetInt16(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadInt16LittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+2)]) :
                BinaryPrimitives.ReadInt16BigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+2)]);
        }
        public void SetInt16(int address, short value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 2);
            else
            {
                destination[address] = bytes[1];
                destination[address + 1] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public ushort GetUInt16(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadUInt16LittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+2)]) :
                BinaryPrimitives.ReadUInt16BigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+2)]);
        }
        public void SetUInt16(int address, ushort value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 2);
            else
            {
                destination[address] = bytes[1];
                destination[address + 1] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public int GetInt32(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadInt32LittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+4)]) :
                BinaryPrimitives.ReadInt32BigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+4)]);
        }
        public void SetInt32(int address, int value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 4);
            else
            {
                destination[address] = bytes[3];
                destination[address + 1] = bytes[2];
                destination[address + 2] = bytes[1];
                destination[address + 3] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public uint GetUInt32(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadUInt32LittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+4)]) :
                BinaryPrimitives.ReadUInt32BigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+4)]);
        }
        public void SetUInt32(int address, uint value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 4);
            else
            {
                destination[address] = bytes[3];
                destination[address + 1] = bytes[2];
                destination[address + 2] = bytes[1];
                destination[address + 3] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public long GetInt64(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadInt64LittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+8)]) :
                BinaryPrimitives.ReadInt64BigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+8)]);
        }
        public void SetInt64(int address, long value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 8);
            else
            {
                destination[address] = bytes[7];
                destination[address + 1] = bytes[6];
                destination[address + 2] = bytes[5];
                destination[address + 3] = bytes[4];
                destination[address + 4] = bytes[3];
                destination[address + 5] = bytes[2];
                destination[address + 6] = bytes[1];
                destination[address + 7] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public ulong GetUInt64(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadUInt64LittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+8)]) :
                BinaryPrimitives.ReadUInt64BigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+8)]);
        }
        public void SetUInt64(int address, ulong value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 8);
            else
            {
                destination[address] = bytes[7];
                destination[address + 1] = bytes[6];
                destination[address + 2] = bytes[5];
                destination[address + 3] = bytes[4];
                destination[address + 4] = bytes[3];
                destination[address + 5] = bytes[2];
                destination[address + 6] = bytes[1];
                destination[address + 7] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public float GetSingle(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadSingleLittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+4)]) :
                BinaryPrimitives.ReadSingleBigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+4)]);
        }
        public void SetSingle(int address, float value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 4);
            else
            {
                destination[address] = bytes[3];
                destination[address + 1] = bytes[2];
                destination[address + 2] = bytes[1];
                destination[address + 3] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public double GetDouble(int address, bool littleEndian = true, bool header = false)
        {
            return littleEndian ?
                BinaryPrimitives.ReadSingleLittleEndian((header ? _Header : _Buffer).AsSpan()[address..(address+8)]) :
                BinaryPrimitives.ReadSingleBigEndian((header ? _Header : _Buffer).AsSpan()[address..(address+8)]);
        }
        public void SetDouble(int address, double value, bool littleEndian = true, bool header = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] destination = header ? _Header : _Buffer;

            if (littleEndian == BitConverter.IsLittleEndian)
                Array.Copy(bytes, 0, destination, address, 8);
            else
            {
                destination[address] = bytes[7];
                destination[address + 1] = bytes[6];
                destination[address + 2] = bytes[5];
                destination[address + 3] = bytes[4];
                destination[address + 4] = bytes[3];
                destination[address + 5] = bytes[2];
                destination[address + 6] = bytes[1];
                destination[address + 7] = bytes[0];
            }

            UnsavedChanges = true;
        }
        public string GetString(int address, int length, bool header = false, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(header ? _Header : _Buffer, address, length).Split('\0')[0];
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
            Array.Copy(bytes, 0, header ? _Header : _Buffer, address, length ?? bytes.Length);
            UnsavedChanges = true;
        }

        public uint GetNumberBitwise(int address, int bit, int bitCount, bool littleEndian = true, bool header = false)
        {
            if (bit > 31 || bitCount > 32)
                throw new Exception("Integers larger than 32 bits are not supported.");
            if (bit < 0 || bitCount < 0)
                throw new Exception("Negative numbers are not allowed.");

            byte[] target = header ? _Header : _Buffer;
            Span<byte> span = target.AsSpan()[address .. Math.Min(address + 4, target.Length)];
            if (littleEndian)
            {
                uint number = span.Length switch
                {
                    0 => throw new IndexOutOfRangeException(),
                    1 => span[0],
                    2 => BinaryPrimitives.ReadUInt16LittleEndian(span),
                    3 => BinaryPrimitives.ReadUInt16LittleEndian(span) + span[2] * (uint)0x10000,
                    _ => BinaryPrimitives.ReadUInt32LittleEndian(span),
                };
                uint numberShifted = number >> bit;
                uint bitMask = (uint)((1 << bitCount) - 1);
                return numberShifted & bitMask;   
            }
            else
            {
                uint number = span.Length switch
                {
                    0 => throw new IndexOutOfRangeException(),
                    1 => span[0],
                    2 => BinaryPrimitives.ReadUInt16BigEndian(span),
                    3 => BinaryPrimitives.ReadUInt16BigEndian(span) * (uint)0x100 + span[2],
                    _ => BinaryPrimitives.ReadUInt32BigEndian(span),
                };
                uint numberShifted = number >> (32 - bit - bitCount);
                uint bitMask = (uint)((1 << bitCount) - 1);
                return numberShifted & bitMask;
            }
        }
        public void SetNumberBitwise(int address, int bit, int bitCount, uint value, bool littleEndian = true, bool header = false)
        {
            if (bit > 31 || bitCount > 31)
                throw new Exception("Integers larger than 32 bits are not supported.");
            if (bit < 0 || bitCount < 0)
                throw new Exception("Negative numbers are not allowed.");

            byte[] destination = header ? _Header : _Buffer;
            Span<byte> span = destination.AsSpan()[address .. Math.Min(address + 4, destination.Length)];
            if (littleEndian)
            {
                uint number = span.Length switch
                {
                    0 => throw new IndexOutOfRangeException(),
                    1 => span[0],
                    2 => BinaryPrimitives.ReadUInt16LittleEndian(span),
                    3 => BinaryPrimitives.ReadUInt16LittleEndian(span) + span[2] * (uint)0x10000,
                    _ => BinaryPrimitives.ReadUInt32LittleEndian(span),
                };
                uint bitMask = (uint)((1 << bitCount) - 1);
                uint newValue = (value & bitMask) << bit;
                uint oldValue = number & ((bitMask << bit) ^ 0b_11111111_11111111_11111111_11111111);
                Array.Copy(BitConverter.GetBytes(newValue | oldValue), 0, destination, address, Math.Min(4, destination.Length - address));
            }
            else
            {
                uint number = span.Length switch
                {
                    0 => throw new IndexOutOfRangeException(),
                    1 => span[0],
                    2 => BinaryPrimitives.ReadUInt16BigEndian(span),
                    3 => BinaryPrimitives.ReadUInt16BigEndian(span) * (uint)0x100 + span[2],
                    _ => BinaryPrimitives.ReadUInt32BigEndian(span),
                };
                uint bitMask = (uint)((1 << bitCount) - 1);
                uint newValue = (value & bitMask) << (32 - bit - bitCount);
                uint oldValue = number & ((bitMask << (32 - bit - bitCount)) ^ 0b_11111111_11111111_11111111_11111111);
                
                byte[] bytes = BitConverter.GetBytes(newValue | oldValue);
                for (int i = 0; i < 4; i++)
                {
                    if (address + i < destination.Length)
                        destination[address + i] = bytes[3 - i];
                    else
                        break;
                }
            }
            UnsavedChanges = true;
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