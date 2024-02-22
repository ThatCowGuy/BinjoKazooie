using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
{
    public class GeoLayout_Command
    {
        // minimally, only 03 and 0D matter. 0C is pretty cool though.
        Dictionary<int, string> GEO_NAMES = new Dictionary<int, string>
        {
            { 0x01, "SORT" },
            { 0x02, "BONE" },
            { 0x03, "LOAD DL" }, // just starts running a DL (offset is defined in int[2])
            { 0x04, "UNKNOWN_04" },
            { 0x05, "SKINNING" },
            { 0x06, "BRANCH" },
            { 0x07, "UNKNOWN_07" },
            { 0x08, "LOD" },
            { 0x09, "UNKNOWN_09" },
            { 0x0A, "REFERENCE POINT" },
            { 0x0B, "UNKNOWN_0B" },
            { 0x0C, "SELECTOR" }, // for model-swapping: int[2] holds short[0] child_count, short[1] selector index, then list of children
            // 0000000C 00000060 00040001 00000020 00000030 00000040 00000050 00000000 00000003...
            //                   ^ 4 children
            //                       ^ sel ID = 1
            //                            ^ 1st child: offset 0x20 bytes to next geolayout command
            { 0x0D, "DRAW DISTANCE" },
            { 0x0E, "UNKNOWN_0E" },
            { 0x0F, "UNKNOWN_0F" },
        };

        public List<uint> content = new List<uint>();
    }

    public class GeoLayout_Segment
    {
        public bool valid = false;

        // this segment does not have a header...

        // locators
        public uint file_offset;

        public List<GeoLayout_Command> commands = new List<GeoLayout_Command>();

        public void populate(byte[] file_data, int file_offset)
        {
            if (file_offset == 0)
            {
                System.Console.WriteLine("No Texture Segment");
                this.valid = false;
                return;
            }
            this.valid = true;
            this.file_offset = (uint) file_offset;

            // this setup is the final one, so we can iterate until the File ends
            uint cmd_len = 0;
            GeoLayout_Command cmd = null;
            this.commands = new List<GeoLayout_Command>();
            for (int i = (int) this.file_offset; i < file_data.Length; )
            {
                if (cmd_len == 0)
                {
                    // if cmd len reached 0, and cmd is set, the command is done
                    if (cmd != null)
                        this.commands.Add(cmd);

                    cmd = new GeoLayout_Command();
                    // read cmd_len (which is 1 int after the first cmd int)
                    cmd_len = File_Handler.read_int(file_data, (i + 0x04), false);
                }
                // read whatever else
                cmd.content.Add(File_Handler.read_int(file_data, (i + 0x00), false));
                Console.WriteLine(cmd_len);
                cmd_len -= 0x04;
                i += 0x04;
            }
            // after this, we have one final command to add.
            // The final command might not even specify its own size, so this catches it either way
            this.commands.Add(cmd);
        }
        public List<string[]> get_content()
        {
            List<string[]> content = new List<string[]>();

            //===============================
            content.Add(new string[] {
                "File Offset",
                File_Handler.uint_to_string(this.file_offset, 0xFFFFFFFF),
                ""
            });

            return content;
        }
        public List<string[]> get_content_of_elements()
        {
            List<string[]> content = new List<string[]>();

            foreach (GeoLayout_Command cmd in this.commands)
            {
                String result = "";
                foreach (uint value in cmd.content)
                    result += File_Handler.uint_to_string(value, 0xFFFFFFFF) + " ";

                content.Add(new string[] {
                    result
                });
            }
            return content;
        }
    }
}
