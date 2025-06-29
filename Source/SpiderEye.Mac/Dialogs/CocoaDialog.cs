﻿using System.Threading.Tasks;
using SpiderEye.Mac.Interop;
using SpiderEye.Mac.Native;
using SpiderEye.Tools;

namespace SpiderEye.Mac
{
    internal abstract class CocoaDialog : IDialog
    {
        public string Title { get; set; }

        public Task<DialogResult> Show()
        {
            return Show(null);
        }

        public Task<DialogResult> Show(IWindow parent)
        {
            var window = NativeCast.To<CocoaWindow>(parent);
            var dialog = CreateDialog();

            ObjC.Call(dialog.Handle, "setTitle:", NSString.Create(Title ?? string.Empty));
            ObjC.Call(dialog.Handle, "setCanCreateDirectories:", true);

            BeforeShow(dialog);
            int result = dialog.Run(window);
            var mappedResult = MapResult(result);
            BeforeReturn(dialog, mappedResult);

            return Task.FromResult(mappedResult);
        }

        protected abstract NSDialog CreateDialog();

        protected virtual void BeforeShow(NSDialog dialog)
        {
        }

        protected virtual void BeforeReturn(NSDialog dialog, DialogResult result)
        {
        }

        private DialogResult MapResult(int result)
        {
            switch (result)
            {
                case 1:
                    return DialogResult.Ok;

                case 0:
                    return DialogResult.Cancel;

                default:
                    return DialogResult.None;
            }
        }
    }
}
