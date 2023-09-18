using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL.Structures
{
    public class AvatarCollection
    {
        public Dictionary<string, GLTexture> AvailableAvatars = new Dictionary<string, GLTexture>();

        public static AvatarCollection Load(DirectoryInfo sourceDir)
        {
            AvatarCollection returnVal = new AvatarCollection();
            foreach (FileInfo pngFile in sourceDir.EnumerateFiles("*.png"))
            {
               GLTexture texture = GLTexture.Load(pngFile);
               returnVal.AvailableAvatars[pngFile.Name] = texture;
            }

            return returnVal;
        }
    }
}
