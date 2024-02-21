using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
{
    public class GeoLayout_Command
    {
        public int cmd_id;

    }

    public class GeoLayout_Segment
    {
        public bool valid = false;

        // this segment does not have a header...

        // locators
        public uint file_offset;

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
    }
}
