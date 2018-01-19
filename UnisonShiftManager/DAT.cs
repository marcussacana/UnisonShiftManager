using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnisonShiftManager {
    public class DAT {
        public Encoding Encoding = Encoding.GetEncoding(932);
        private List<Entry> Data;
        byte[] Content;
        public DAT(byte[] Script) {
            Content = Script;
        }

        public string[] Import() {
            if (!EqualsAt(Content, new byte[] { 0x24, 0x54, 0x45, 0x58, 0x54, 0x5F, 0x4C, 0x49, 0x53, 0x54, 0x5F, 0x5F }, 0))
                throw new Exception("Invalid DAT Text Signature");

            byte[] Script = Decrypt(Content);
            Data = new List<Entry>();
            using (Stream MEM = new MemoryStream(Script))
            using (StructReader Reader = new StructReader(MEM, Encoding: Encoding)) {
                while (MEM.Length - Reader.BaseStream.Position > 4) {
                    Entry Entry = new Entry();
                    Reader.ReadStruct(ref Entry);
                    Data.Add(Entry);
                }
            }

            return (from x in Data select x.Content).ToArray();
        }

        private bool EqualsAt(byte[] Data, byte[] DataToCompare, uint At) {
            if (DataToCompare.Length + At >= Data.Length)
                return false;

            for (uint i = 0; i < DataToCompare.Length; i++)
                if (Data[i + At] != DataToCompare[i])
                    return false;

            return true;
        }

        public byte[] Export(string[] Content) {
            if (Content.Length != Data.Count)
                throw new Exception("You Can't Modify The Count of Strings.");

            using (MemoryStream Buffer = new MemoryStream())
            using (StructWriter Writer = new StructWriter(Buffer, Encoding: Encoding)) {

                for (int i = 0; i < Content.Length; i++) {
                    Entry Entry = Data[i];
                    Entry.Content = Content[i];

                    Writer.WriteStruct(ref Entry);
                }
                Writer.Flush();

                return Encrypt(Buffer.ToArray());
            }
        }

        private byte[] Decrypt(byte[] Script) {
            //Based on http://aluigi.altervista.org/bms/leyline_dat.bms
            const int HeaderSize = 0x10;
            int Missing = 4 - ((Script.Length + HeaderSize) % 4);

            byte[] Result = new byte[(Script.Length - HeaderSize)];
            for (int i = HeaderSize; i < Script.Length; i++) {
                Result[i - HeaderSize] = Script[i];
            }           


            for (int i = 0, Bits = 4; i < Result.Length - Missing; i += 4, Bits &= 7) {
                Result[i] = RotateLeft(Result[i], (byte)Bits++);

                uint DW = BitConverter.ToUInt32(Result, i);
                BitConverter.GetBytes(((DW ^ 0x084DF873) ^ 0xFF987DEE)).CopyTo(Result, i);
            }
            
            return Result;
        }

        private byte[] Encrypt(byte[] Script) {
            const int HeaderSize = 0x10;
            int Missing = 4 - ((Script.Length + HeaderSize) % 4);


            byte[] Result = new byte[Script.Length + HeaderSize];
            for (int i = 0; i < HeaderSize; i++) {
                Result[i] = Content[i];
            }

            for (int i = 0; i < Script.Length; i++) {
                Result[i + HeaderSize] = Script[i];
            }


            for (int i = HeaderSize, Bits = 4; i < Result.Length - Missing; i += 4, Bits &= 7) {
                uint DW = BitConverter.ToUInt32(Result, i);
                BitConverter.GetBytes(((DW ^ 0xFF987DEE) ^ 0x084DF873)).CopyTo(Result, i);

                Result[i] = RotateRight(Result[i], (byte)Bits++);
            }
            
            return Result;
        }

        private byte RotateLeft(byte value, int count) {
            return (byte)((value << count) | (value >> (8 - count)));
        }

        private byte RotateRight(byte value, int count) {
            return (byte)((value >> count) | (value << (8 - count)));
        }
    }

#pragma warning disable 169
    internal struct Entry {
        uint ID;

        [CString]
        public string Content;
    }
#pragma warning restore 169
}
