using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase
{
    public static class Util
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        #region WindowEnums
        [Flags]
        internal enum ExtendedWindowStyle : uint
        {
            DialogModalFrame = 0x00000001,
            NoParentNotify = 0x00000004,
            Topmost = 0x00000008,
            AcceptFiles = 0x00000010,
            Transparent = 0x00000020,

            // #if(WINVER >= 0x0400)
            MdiChild = 0x00000040,
            ToolWindow = 0x00000080,
            WindowEdge = 0x00000100,
            ClientEdge = 0x00000200,
            ContextHelp = 0x00000400,
            // #endif

            // #if(WINVER >= 0x0400)
            Right = 0x00001000,
            Left = 0x00000000,
            RightToLeftReading = 0x00002000,
            LeftToRightReading = 0x00000000,
            LeftScrollbar = 0x00004000,
            RightScrollbar = 0x00000000,

            ControlParent = 0x00010000,
            StaticEdge = 0x00020000,
            ApplicationWindow = 0x00040000,

            OverlappedWindow = WindowEdge | ClientEdge,
            PaletteWindow = WindowEdge | ToolWindow | Topmost,
            // #endif

            // #if(_WIN32_WINNT >= 0x0500)
            Layered = 0x00080000,
            // #endif

            // #if(WINVER >= 0x0500)
            NoInheritLayout = 0x00100000, // Disable inheritence of mirroring by children
            RightToLeftLayout = 0x00400000, // Right to left mirroring
                                            // #endif /* WINVER >= 0x0500 */

            // #if(_WIN32_WINNT >= 0x0501)
            Composited = 0x02000000,
            // #endif /* _WIN32_WINNT >= 0x0501 */

            // #if(_WIN32_WINNT >= 0x0500)
            NoActivate = 0x08000000
            // #endif /* _WIN32_WINNT >= 0x0500 */
        }

        internal static string KillDuplicateWhiteSpaces(string value)
        {
            string pattern = "\\s+";
            string replacement = " ";
            Regex rgx = new Regex(pattern);
            string result = rgx.Replace(value.Trim(), replacement);
            return result;
        }

        [Flags]
        internal enum WindowStyle : uint
        {
            Overlapped = 0x00000000,
            Popup = 0x80000000,
            Child = 0x40000000,
            Minimize = 0x20000000,
            Visible = 0x10000000,
            Disabled = 0x08000000,
            ClipSiblings = 0x04000000,
            ClipChildren = 0x02000000,
            Maximize = 0x01000000,
            Caption = 0x00C00000,    // Border | DialogFrame
            Border = 0x00800000,
            DialogFrame = 0x00400000,
            VScroll = 0x00200000,
            HScreen = 0x00100000,
            SystemMenu = 0x00080000,
            ThickFrame = 0x00040000,
            Group = 0x00020000,
            TabStop = 0x00010000,

            MinimizeBox = 0x00020000,
            MaximizeBox = 0x00010000,

            Tiled = Overlapped,
            Iconic = Minimize,
            SizeBox = ThickFrame,
            TiledWindow = OverlappedWindow,

            // Common window styles:
            OverlappedWindow = Overlapped | Caption | SystemMenu | ThickFrame | MinimizeBox | MaximizeBox,
            PopupWindow = Popup | Border | SystemMenu,
            ChildWindow = Child
        }
        #endregion 


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateWindowEx(
            ExtendedWindowStyle ExStyle,
            IntPtr ClassAtom,
            IntPtr WindowName,
            WindowStyle Style,
            int X, int Y,
            int Width, int Height,
            IntPtr HandleToParentWindow,
            IntPtr Menu,
            IntPtr Instance,
            IntPtr Param);

        public static uint GetMemoryTypeIndex(uint memoryTypeBits, MemoryPropertyFlags desiredMemoryFlags)
        {
            uint memoryTypeIndex = 0;
            PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties = VContext.Instance.physicalDevice.GetMemoryProperties();
            for (uint i = 0; i < physicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                if ((memoryTypeBits & 1) == 1)
                {
                    MemoryType memoryType = physicalDeviceMemoryProperties.MemoryTypes[i];
                    if ((memoryType.PropertyFlags & desiredMemoryFlags) == desiredMemoryFlags)
                    {
                        memoryTypeIndex = i;
                        break;
                    }
                }
                memoryTypeBits >>= 1;
            }

            return memoryTypeIndex;
        }
        
    }
}
