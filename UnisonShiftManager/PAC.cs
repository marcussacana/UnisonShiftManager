using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnisonShiftManager {
    public static class PAC {
        const string HeaderInfo = "~PACINFO~";
        public static File[] Extract(Stream Packget) {
            StructReader Reader = new StructReader(Packget, Encoding: Encoding.ASCII);//not sure
            PACHeader Header = new PACHeader();

            Reader.ReadStruct(ref Header);

            if (Header.Signature != "PAC ")
                throw new Exception("Bad Signature");

            File[] Files = new File[Header.FileCount + 1];

            Files[0] = new File() {
                FileName = HeaderInfo,
                Content = new MemoryStream(Header.Dummy)
            };

            for (int i = 1; i < Files.Length; i++) {
                Files[i] = new File();
                Reader.ReadStruct(ref Files[i]);

                Files[i].FileName = Files[i].FileName.TrimEnd('\x0');
                Files[i].Content = new VirtStream(Reader.BaseStream, Files[i].Offset, Files[i].Length);
            }

            return Files;
        }

        public static void Repack(File[] Files, Stream Output, bool CloseStreams = true) {
            if ((from x in Files where x.FileName == HeaderInfo select x).Count() != 1)
                throw new Exception("Header Info Not Found");

            byte[] HInfo = new byte[0x7F8];
            File tmp = (from x in Files where x.FileName == HeaderInfo select x).First();
            tmp.Content.Read(HInfo, 0, HInfo.Length);
            tmp.Content.Close();

            Files = (from x in Files where x.FileName != HeaderInfo select x).ToArray();

            StructWriter Writer = new StructWriter(Output, Encoding: Encoding.ASCII);
            PACHeader Header = new PACHeader() {
                Signature = "PAC ",
                FileCount = (uint)Files.Length,
                Dummy = HInfo
            };

            Writer.WriteStruct(ref Header);

            for (uint i = 0, x = 0; i < Files.Length; i++) {
                tmp = new File() {
                    FileName = Files[i].FileName,
                    Length = (uint)Files[i].Content.Length,
                    Offset = x
                };
                Writer.WriteStruct(ref tmp);

                x += tmp.Length;
            }

            for (uint i = 0; i < Files.Length; i++) {
                Files[i].Content.CopyTo(Writer.BaseStream);

                if (CloseStreams)
                    Files[i].Content.Close();
            }
            Writer.Flush();

            if (CloseStreams)
                Writer.Close();
        }
    }

    public struct PACHeader {
        [FString(Length = 4)]
        public string Signature;

        uint unk;
        public uint FileCount;

        [FArray(Length = 0x7F8)]
        public byte[] Dummy;
    }

    public struct File {
        [FString(Length = 0x20)]
        public string FileName;

        internal uint Length;
        internal uint Offset;

        [Ignore]
        public Stream Content;
    }
}
