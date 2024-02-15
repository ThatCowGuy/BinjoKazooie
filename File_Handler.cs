﻿using System;
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
        public static ulong bytes_2_int(byte[] input, int offset, int len, bool little_endian)
        {
            ulong res = 0;
            if (little_endian == false)
            {
                for (int i = 0; i < len; i++)
                {
                    res *= 0x100;
                    res += input[offset + i];
                }
            }
            if (little_endian == true)
            {
                for (int i = 0; i < len; i++)
                {
                    res *= 0x100;
                    res += input[offset + (len - 1) - i];
                }
            }
            return res;
        }
        public static ulong bytes_2_int(byte[] input, int len, bool little_endian)
        {
            return bytes_2_int(input, 0, len, little_endian);
        }

        public static ulong read_long(byte[] file_content, int address, bool little_endian)
        {
            int data_size = 8;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (ulong) File_Handler.bytes_2_int(buffer, 0, data_size, little_endian);
        }
        public static uint read_int(byte[] file_content, int address, bool little_endian)
        {
            int data_size = 4;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (uint) File_Handler.bytes_2_int(buffer, 0, data_size, little_endian);
        }
        public static ushort read_short(byte[] file_content, int address, bool little_endian)
        {
            int data_size = 2;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (ushort) File_Handler.bytes_2_int(buffer, 0, data_size, little_endian);
        }
        public static byte read_char(byte[] file_content, int address, bool little_endian)
        {
            int data_size = 1;
            byte[] buffer = new byte[data_size];
            for (int i = 0; i < data_size; i++)
                buffer[i] = file_content[address + i];
            return (byte) File_Handler.bytes_2_int(buffer, 0, data_size, little_endian);
        }
        public static Single convert_to_float(int hex)
        {
            var bytes = BitConverter.GetBytes(hex);
            return BitConverter.ToSingle(bytes, 0);
        }
        public static Single read_float(byte[] file_content, int address, bool little_endian)
        {
            int hex = (int) read_int(file_content, address, little_endian);
            return convert_to_float(hex);
        }


        public static void write_data(byte[] file_content, int offset, byte[] data)
        {
            for (int b = 0; b < data.Length; b++)
            {
                file_content[offset + b] = data[b];
            }
        }
        public static string binary_to_string(uint input, int digit_cnt)
        {
            string result = "";
            for (int digits = 0; digits < digit_cnt; digits++)
            {
                if (digits > 0 && digits % 4 == 0)
                    result = '_' + result;
                result = ((input % 2 == 1) ? '1' : '0') + result;
                input /= 2;
            }
            return result;
        }
        public static string uint_to_string(uint input, uint format)
        {
            switch(format)
            {
                case (10):
                    return String.Format("{0}", input);
                case (0xFF):
                    return String.Format("{0:X02}", input & format);
                case (0xFFFF):
                    return String.Format("{0:X04}", input & format);
                case (0xFFFFFFFF):
                    return String.Format("{0:X08}", input & format);
                case (0b1):
                    return binary_to_string(input, 16);
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
                    return String.Format("{0:X02}", input & format);
                case (0xFFFF):
                    return String.Format("{0:X04}", input & format);
                case (0xFFFFFFFF):
                    return String.Format("{0:X08}", input & format);
                case (0b1):
                    return binary_to_string((uint)input, 16);
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
        public static uint apply_bitmask(uint input, uint bitmask)
        {
            uint res = input & bitmask;
            while (bitmask % 2 == 0)
            {
                res = res >> 1;
                bitmask = bitmask >> 1;
            }
            return res;
        }

        public static uint get_bits(int input, uint bitcnt, uint rshift)
        {
            return 0;
        }
    }
}
