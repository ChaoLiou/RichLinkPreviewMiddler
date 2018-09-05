using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichLinkPreviewMiddler
{
    public static class ExtensionMethods
    {
        public static string ToOutput(this object obj)
        {
            var maxLength = obj.GetType().GetProperties().Select(p => p.Name).Max(n => n.Length);
            return "\r\n".PadRight(maxLength * 3, '-') + "\r\n" 
                + string.Join("\r\n", obj.GetType().GetProperties().Select(p => p.Name.PadRight(maxLength, ' ') + " : " + p.GetValue(obj)))
                + "\r\n".PadRight(maxLength * 3, '-') + "\r\n";
        }
    }
}
