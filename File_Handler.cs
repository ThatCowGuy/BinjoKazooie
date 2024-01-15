using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace BK_BIN_Analyzer
{
    public static class File_Handler
    {
        public static ulong bytes_2_int(byte[] input, int len)
        {
            ulong res = 0;
            for (int i = 0; i < len; i++)
            {
                res *= 0x100;
                res += input[i];
            }
            return res;
        }

        public static ulong read_long(byte[] file_content, int address)
        {
            int data_size = 8;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (ulong) File_Handler.bytes_2_int(buffer, data_size);
        }
        public static uint read_int(byte[] file_content, int address)
        {
            int data_size = 4;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (uint) File_Handler.bytes_2_int(buffer, data_size);
        }
        public static ushort read_short(byte[] file_content, int address)
        {
            int data_size = 2;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (ushort) File_Handler.bytes_2_int(buffer, data_size);
        }
        public static byte read_char(byte[] file_content, int address)
        {
            int data_size = 1;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (byte) File_Handler.bytes_2_int(buffer, data_size);
        }


        public static void write_data(byte[] file_content, int offset, byte[] data)
        {
            for (int b = 0; b < data.Length; b++)
            {
                file_content[offset + b] = data[b];
            }
        }
        public static string uint_to_string(uint input, uint format)
        {
            switch(format)
            {
                case (10):
                    return String.Format("{0}", input);
                case (0xFF):
                    return String.Format("0x{0:X02}", input & format);
                case (0xFFFF):
                    return String.Format("0x{0:X04}", input & format);
                case (0xFFFFFFFF):
                    return String.Format("0x{0:X08}", input & format);
            }
            return "";
        }
        // overload for possibly negative values
        public static string uint_to_string(int input, uint format)
        {
            switch (format)
            {
                case (10):
                return String.Format("{0}", input);
                case (0xFF):
                return String.Format("0x{0:X02}", input & format);
                case (0xFFFF):
                return String.Format("0x{0:X04}", input & format);
                case (0xFFFFFFFF):
                return String.Format("0x{0:X08}", input & format);
            }
            return "";
        }

        public static string get_basedir_or_assets()
        {
            string target_dir = Directory.GetCurrentDirectory();
            // while working on the code, the exe is started from within Debug/ which sucks
            if (target_dir.Contains("Debug") == true)
                target_dir = Directory.GetParent(Directory.GetParent(target_dir).ToString()).FullName;

            // see if there is an assets/ folder here
            if (Directory.Exists(Path.Combine(target_dir, "assets")))
                target_dir = Path.Combine(target_dir, "assets");

            return target_dir;
        }
        public static string get_basedir_or_exports()
        {
            string target_dir = Directory.GetCurrentDirectory();
            // while working on the code, the exe is started from within Debug/ which sucks
            if (target_dir.Contains("Debug") == true)
                target_dir = Directory.GetParent(Directory.GetParent(target_dir).ToString()).FullName;

            // see if there is an assets/ folder here
            if (Directory.Exists(Path.Combine(target_dir, "exports")))
                target_dir = Path.Combine(target_dir, "exports");

            return target_dir;
        }
    }
}
