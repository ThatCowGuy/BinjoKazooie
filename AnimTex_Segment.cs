using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
    public class AnimTex_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================

        // locators
        public uint file_offset;
        public uint file_offset_data;

        public void populate(byte[] file_data, int file_offset)
        {
            if (file_offset == 0)
            {
                System.Console.WriteLine("No AnimatedTexture Segment");
                this.valid = false;
                return;
            }
            this.valid = true;
            this.file_offset = (uint)file_offset;
            this.file_offset_data = (uint)file_offset + 0x00;

            // parsing properties
            // === 0x00 ===============================
        }

        public List<string[]> get_content()
        {
            List<string[]> content = new List<string[]>();
            if (this.valid == false)
            {
                content.Add(new string[] { "No Data" });
                return content;
            }

            content.Add(new string[] {
                "File Offset",
                File_Handler.uint_to_string(this.file_offset, 0xFFFFFFFF),
                "",
                "from File Start"
            });

            return content;
        }
    }
}
