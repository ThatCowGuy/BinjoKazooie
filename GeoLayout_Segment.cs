using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
{
    public class GeoLayout_Command
    {
        public List<uint> content = new List<uint>();

        public static GeoLayout_Command GEO_LOAD_DL(short xmin, short ymin, short zmin, short xmax, short ymax, short zmax)
        {
            GeoLayout_Command cmd = new GeoLayout_Command();
            cmd.content.Add((uint) Dicts.GEO_CMD_NAMES_REV["DRAW_DISTANCE"]);
            cmd.content.Add((uint) 0x00000028);
            cmd.content.Add((uint) ((xmin << 16) + ymin));
            cmd.content.Add((uint) ((zmin << 16) + xmax));
            cmd.content.Add((uint) ((ymax << 16) + zmax));
            cmd.content.Add((uint) 0x001808D3);
            cmd.content.Add((uint) Dicts.GEO_CMD_NAMES_REV["LOAD_DL"]);
            cmd.content.Add((uint) 0x00000000);
            cmd.content.Add((uint) 0x00000000); // this contains the offset
            cmd.content.Add((uint) 0x00000000); // just padding
            return cmd;
        }
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0];
            foreach (uint val in this.content)
            {
                bytes = File_Handler.concat_arrays(bytes, File_Handler.uint_to_bytes(val, 4));
            }
            return bytes;
        }
    }

    public class GeoLayout_Segment
    {
        public bool valid = false;

        // this segment does not have a header...

        // locators
        public uint file_offset;

        public List<GeoLayout_Command> commands = new List<GeoLayout_Command>();

        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0];
            foreach (GeoLayout_Command geo_cmd in this.commands)
            {
                bytes = File_Handler.concat_arrays(bytes, geo_cmd.get_bytes());
            }
            return bytes;
        }

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
