using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace T3D
{
    // ReSharper disable once InconsistentNaming
    public class T3DArchive : IDisposable
    {
        private static readonly byte[] MagicBytes = { 0x02, 0x3D, 0xFF, 0xFF, 0x00, 0x57, 0x01, 0x00 };

        public ReadOnlyCollection<T3DEntry> Entries { get; }

        private readonly Stream _stream;

        private T3DArchive(ReadOnlyCollection<T3DEntry> entries, Stream stream)
        {
            Entries = entries;
            _stream = stream;
        }

        public static T3DArchive OpenRead(string path)
        {
            var stream = File.OpenRead(path);

            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                VerifyMagicBytes(reader);

                var numFilesInArchive = reader.ReadUInt32() - 1;

                // Unused, filename section size
                reader.ReadUInt32();

                var contentOffsets = new uint[numFilesInArchive];
                var fileNameOffsets = new uint[numFilesInArchive];

                for (var i = 0; i < numFilesInArchive; i++)
                {
                    contentOffsets[i] = reader.ReadUInt32();
                    fileNameOffsets[i] = reader.ReadUInt32() + (uint)reader.BaseStream.Position - 8;
                }

                var fileSize = reader.ReadUInt64();
                
                var entries = new List<T3DEntry>();
                for (var i = 0; i < numFilesInArchive; i++)
                {
                    var size = i == numFilesInArchive - 1
                        ? fileSize - contentOffsets[i]
                        : contentOffsets[i + 1] - contentOffsets[i];

                    entries.Add(new T3DEntry(ReadString(reader, fileNameOffsets[i]), contentOffsets[i], size, stream));
                }

                return new T3DArchive(new ReadOnlyCollection<T3DEntry>(entries), stream);
            }
        }

        private static void VerifyMagicBytes(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(MagicBytes.Length);
            if (bytes.Where((t, i) => t != MagicBytes[i]).Any())
            {
                throw new T3DArchiveException("Not a valid T3D file");
            }
        }

        private static string ReadString(BinaryReader reader, uint offset)
        {
            var str = "";
            char character;

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            do
            {
                character = reader.ReadChar();

                if (character != 0x00)
                {
                    str += character;
                }

            } while (character != 0x00);

            return str;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public override string ToString()
        {
            if (_stream is FileStream stream)
            {
                return stream.Name;
            }

            return base.ToString();
        }
    }

    // ReSharper disable once InconsistentNaming
    public class T3DEntry
    {
        public string FileName { get; }

        public ulong Size { get; }

        private uint Offset { get; }

        private readonly Stream _stream;

        internal T3DEntry(string fileName, uint offset, ulong size, Stream stream)
        {
            FileName = fileName;
            Offset = offset;
            Size = size;
            _stream = stream;
        }

        public void ExtractToDirectory(string path)
        {
            _stream.Seek(Offset, SeekOrigin.Begin);

            using (var output = File.OpenWrite(Path.Combine(path, FileName)))
            {
                _stream.CopyBytes(output, (int)Size);
            }
        }

        public override string ToString()
        {
            return $"{FileName}, offset: {Offset}, size: {Size}";
        }
    }

    [Serializable]
    // ReSharper disable once InconsistentNaming
    public class T3DArchiveException : Exception
    {
        public T3DArchiveException(string message)
            : base(message)
        {
        }
    }
}
