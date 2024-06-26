using System;
using System.Runtime.InteropServices;
using SpiderEye.Linux.Interop;

namespace SpiderEye.Linux.Native
{
    internal static class GLib
    {
        private const string GLibNativeDll = "libglib-2.0.so.0";
        private const string GObjectNativeDll = "libgobject-2.0.so.0";
        private const string GIONativeDll = "libgio-2.0.so.0";

        [DllImport(GLibNativeDll, EntryPoint = "g_malloc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Malloc(UIntPtr size);

        [DllImport(GLibNativeDll, EntryPoint = "g_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Free(IntPtr mem);

        [DllImport(GObjectNativeDll, EntryPoint = "g_signal_connect_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ConnectSignalData(IntPtr instance, IntPtr signal_name, Delegate handler, IntPtr data, IntPtr destroy_data, int connect_flags);

        [DllImport(GIONativeDll, EntryPoint = "g_memory_input_stream_new_from_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateStreamFromData(IntPtr data, long len, IntPtr destroy);

        [DllImport(GIONativeDll, EntryPoint = "g_file_new_for_path", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FileForPath(IntPtr path);

        [DllImport(GIONativeDll, EntryPoint = "g_file_read", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ReadFile(IntPtr file, IntPtr cancellable, IntPtr error);

        [DllImport(GIONativeDll, EntryPoint = "g_file_input_stream_query_info", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr QueryFileInfo(IntPtr fileStream, IntPtr attributes, IntPtr cancellable, IntPtr error);

        [DllImport(GIONativeDll, EntryPoint = "g_file_info_get_size", CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetSizeFromFileInfo(IntPtr fileInfo);

        [DllImport(GObjectNativeDll, EntryPoint = "g_object_unref", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnrefObject(IntPtr obj);

        [DllImport(GLibNativeDll, EntryPoint = "g_error_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeError(IntPtr error);

        [DllImport(GLibNativeDll, EntryPoint = "g_slist_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeSList(IntPtr slist);

        [DllImport(GLibNativeDll, EntryPoint = "g_list_prepend", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ListPrepend(IntPtr list, IntPtr data);

        [DllImport(GLibNativeDll, EntryPoint = "g_list_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeList(IntPtr list);

        [DllImport(GLibNativeDll, EntryPoint = "g_list_length", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetListLength(IntPtr list);

        [DllImport(GLibNativeDll, EntryPoint = "g_list_nth_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetListNthData(IntPtr list, uint index);

        [DllImport(GObjectNativeDll, EntryPoint = "g_file_error_quark", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetFileErrorQuark();

        [DllImport(GLibNativeDll, EntryPoint = "g_bytes_get_size", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr GetBytesSize(IntPtr bytes);

        [DllImport(GLibNativeDll, EntryPoint = "g_bytes_unref", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr UnrefBytes(IntPtr bytes);

        [DllImport(GLibNativeDll, EntryPoint = "g_bytes_get_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetBytesDataPointer(IntPtr bytes, out UIntPtr size);

        [DllImport(GObjectNativeDll, EntryPoint = "g_object_set", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetProperty(IntPtr obj, IntPtr propertyName, IntPtr value, IntPtr terminator);

        [DllImport(GLibNativeDll, EntryPoint = "g_main_context_invoke", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ContextInvoke(IntPtr context, GSourceDelegate function, IntPtr data);

        public static void ConnectSignal(IntPtr instance, string signalName, Delegate handler, IntPtr data)
        {
            using (GLibString gname = signalName)
            {
                ConnectSignalData(instance, gname, handler, data, IntPtr.Zero, 0);
            }
        }

        public static void SetProperty(IntPtr obj, string propertyName, IntPtr value)
        {
            using (GLibString gname = propertyName)
            {
                SetProperty(obj, gname, value, IntPtr.Zero);
            }
        }
    }
}
