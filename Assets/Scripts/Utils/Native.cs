using System;
using System.Runtime.InteropServices;

namespace RDOnline.Utils
{
    public class Native
    {
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);
        /// <summary>
        /// 显示打开文件对话框
        /// </summary>
        /// <param name="filter">文件过滤器，例如 "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*"</param>
        /// <param name="title">对话框标题</param>
        /// <param name="initialDir">初始目录</param>
        /// <returns>选中的文件路径，如果用户取消则返回null</returns>
        public static string OpenFile(string filter, string initialDir = null, string title = null)
        {
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);          // 设置结构体大小 [citation:5]
            ofn.hwndOwner = IntPtr.Zero;
            ofn.lpstrFilter = filter ?? "所有文件(*.*)\0*.*\0\0";
            ofn.lpstrFile = new string(new char[260]);         // 为文件路径分配缓冲区（MAX_PATH = 260）
            ofn.nMaxFile = 260;
            ofn.lpstrInitialDir = initialDir;
            ofn.lpstrTitle = title;

            bool result = GetOpenFileName(ref ofn);
            return result ? ofn.lpstrFile : null;
        }
// 定义 OPENFILENAME 结构体（Unicode 版本）
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OpenFileName
        {
            public int lStructSize;              // 结构体大小
            public IntPtr hwndOwner;              // 父窗口句柄
            public IntPtr hInstance;               // 实例句柄（通常为 IntPtr.Zero）
            public string lpstrFilter;             // 文件过滤器
            public string lpstrCustomFilter;// 自定义过滤器缓冲区
            public int nMaxCustFilter;             // 自定义过滤器缓冲区大小
            public int nFilterIndex;                // 默认选中的过滤器索引
            public string lpstrFile;         // 接收文件路径的缓冲区
            public int nMaxFile;                     // 缓冲区大小（字符数）
            public string lpstrFileTitle;            // 接收文件名（不含路径）的缓冲区
            public int nMaxFileTitle;                 // 文件名缓冲区大小
            public string lpstrInitialDir;            // 初始目录
            public string lpstrTitle;                  // 对话框标题
            public int Flags;                           // 行为标志
            public short nFileOffset;                    // 文件路径中文件名的起始位置（从 lpstrFile 偏移）
            public short nFileExtension;                  // 文件路径中扩展名的起始位置
            public string lpstrDefExt;                     // 默认扩展名
            public IntPtr lCustData;                        // 自定义数据
            public IntPtr lpfnHook;                          // 钩子函数指针
            public string lpTemplateName;                    // 模板名称
            public IntPtr pvReserved;                         // 保留字段
            public int dwReserved;                             // 保留字段
            public int FlagsEx;                                 // 扩展标志（Windows 2000 及以上）
        }

    }
}