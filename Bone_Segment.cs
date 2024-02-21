using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*/=====================================================
 * Thanks to Unalive for documenting most of the
 * information that's being parsed in this Segment 
=====================================================/*/
namespace Binjo
{
    public class Bone_Elem
    {
        // parsed properties
        // === 0x00 ===============================
        public Single x;
        public Single y;
        public Single z;
        public ushort internal_ID;
        public ushort parent_ID;
    }

    public class Bone_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================
        public Single scaling_factor; // in percent
        public ushort bone_cnt;
        public ushort padding;

        public Bone_Elem[] bone_list;

        // locators
        public uint file_offset;
        public uint file_offset_data;

        public void populate(byte[] file_data, int file_offset)
        {
            if (file_offset == 0)
            {
                System.Console.WriteLine("No Bone Segment");
                this.valid = false;
                return;
            }
            this.valid = true;
            this.file_offset = (uint)file_offset;
            this.file_offset_data = (uint)file_offset + 0x08;

            // parsing properties
            // === 0x00 ===============================
            this.scaling_factor = File_Handler.read_float(file_data, file_offset + 0x00, false);
            this.bone_cnt = File_Handler.read_short(file_data, file_offset + 0x04, false);
            this.padding = File_Handler.read_short(file_data, file_offset + 0x06, false);

            this.bone_list = new Bone_Elem[this.bone_cnt];
            for (int i = 0; i < this.bone_cnt; i++)
            {
                Bone_Elem bone = new Bone_Elem();
                int file_offset_bone = (int)(this.file_offset_data + (i * 0x10));

                // parsing properties
                // === 0x00 ===============================
                bone.x = File_Handler.read_float(file_data, file_offset_bone + 0x00, false);
                bone.y = File_Handler.read_float(file_data, file_offset_bone + 0x04, false);
                bone.z = File_Handler.read_float(file_data, file_offset_bone + 0x08, false);
                bone.internal_ID = File_Handler.read_short(file_data, file_offset_bone + 0x0C, false);
                bone.parent_ID = File_Handler.read_short(file_data, file_offset_bone + 0x0E, false);

                this.bone_list[i] = bone;
            }
        }

        public List<string[]> get_bone_content(int id)
        {
            Bone_Elem bone = this.bone_list[id];
            List<string[]> content = new List<string[]>();

            content.Add(new string[] {
                File_Handler.uint_to_string(id, 0xFFFF),
                String.Format("{0,0:F4}", bone.x),
                String.Format("{0,0:F4}", bone.y),
                String.Format("{0,0:F4}", bone.z),
                File_Handler.uint_to_string(bone.internal_ID, 0xFFFF),
                File_Handler.uint_to_string(bone.parent_ID, 0xFFFF),
            });

            return content;
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
            content.Add(new string[] {
                "Scaling Factor",
                "",
                String.Format("{0,0:F4} %", this.scaling_factor),
                ""
            });
            content.Add(new string[] {
                "Bone Count",
                File_Handler.uint_to_string(this.bone_cnt, 0xFFFF),
                "",
                ""
            });

            return content;
        }
    }
}
