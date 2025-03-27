using System;
using Gio;

namespace SpiderEye.Linux
{
    internal class GtkLabelMenuItem : GtkMenuItem, ILabelMenuItem
    {
        // Prepend with a letter to ensure that it does not start with a number
        private readonly string actionId = "a" + Guid.NewGuid().ToString("N");
        private readonly SimpleAction action;
        public event EventHandler Click;
        private GtkSubMenu? subMenu;

        public string Label { get; set; }

        public bool Enabled
        {
            get { return action.Enabled; }
            set { action.Enabled = value; }
        }

        public GtkLabelMenuItem(string label)
        {
            Label = label;
            action = SimpleAction.New(actionId, null);
            action.Enabled = true;
            action.OnActivate += ActionOnActivate;
        }

        public void AddToMenu(Gio.Menu parent)
        {
            if (subMenu?.MenuItems.Count > 0)
            {
                parent.AppendSubmenu(Label, subMenu.BuildMenu());
            }
            else
            {
                var menuItem = Gio.MenuItem.New(Label, $"{GtkTopMenu.MenuActionPrefix}.{actionId}");
                parent.AppendItem(menuItem);
            }
        }

        public void AddToActionGroup(SimpleActionGroup actionGroup)
        {
            actionGroup.AddAction(action);
            subMenu?.AddToActionGroup(actionGroup);
        }

        public override IMenu CreateSubMenu()
        {
            return subMenu = new GtkSubMenu();
        }

        public void SetShortcut(ModifierKey modifier, Key key)
        {
            var shortcut = KeyMapper.ResolveShortcut(modifier, key);
            LinuxApplication.App.NativeApplication.SetAccelsForAction($"{GtkTopMenu.MenuActionPrefix}.{actionId}", [shortcut]);
        }

        public void SetSystemShorcut(SystemShortcut shortcut)
        {
            var (modifier, key) = KeyMapper.ResolveSystemShortcut(shortcut);
            SetShortcut(modifier, key);
        }

        public override void Dispose()
        {
            LinuxApplication.App.NativeApplication.SetAccelsForAction($"{GtkTopMenu.MenuActionPrefix}.{actionId}", []);
            action.OnActivate -= ActionOnActivate;
            action.Dispose();
            subMenu?.Dispose();
        }

        private void ActionOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}
