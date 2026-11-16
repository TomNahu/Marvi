using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
        private readonly Dictionary<Key,Button> shortcuts = new Dictionary<Key,Button>();
        private readonly List<KeyValuePair<Key,Button>> actions = new List<KeyValuePair<Key,Button>>();
        private Key flowerKey, letterKey, rereadKey;
        private Button recallItem;
        private int secretProgress;

        // El menú y los atajos resuelven las mismas acciones, tanto desde Marvi como desde la bandeja.
        private void OpenMenu()
        {
            AssignShortcuts();
            recallItem.Visibility=IsVisible ? Visibility.Collapsed : Visibility.Visible;
            menu.Open();
        }

        private Button AddItem(string label, Key key, Action action)
        {
            var item=new Button { Content=label, Tag="Ctrl + Shift + "+key };
            item.Click+=(s,e)=>{ if(menu.IsVisible) menu.Hide(); action(); };
            actions.Add(new KeyValuePair<Key,Button>(key,item));
            menu.Items.Add(item);
            return item;
        }

        private void AssignShortcuts()
        {
            shortcuts.Clear();
            var used=new HashSet<Key>();
            flowerKey=ChooseKey(Key.F8,used);
            letterKey=ChooseKey(Key.F9,used);
            rereadKey=ChooseKey(Key.F7,used);
            // Retain the original reread chord on desktops where it is free.
            if(PetMenu.KeyAvailable(Key.M)) used.Add(Key.M);
            foreach(var pair in actions)
                if(!used.Contains(pair.Key) && PetMenu.KeyAvailable(pair.Key))
                {
                    used.Add(pair.Key);
                    shortcuts.Add(pair.Key,pair.Value);
                }
            foreach(var pair in actions)
            {
                Key actual=pair.Key;
                if(!shortcuts.ContainsValue(pair.Value))
                {
                    actual=ChooseKey(Key.None,used);
                    if(actual!=Key.None) shortcuts.Add(actual,pair.Value);
                }
                string label=actual>=Key.D0 && actual<=Key.D9 ? ((int)actual-(int)Key.D0).ToString() : actual.ToString();
                pair.Value.Tag=actual==Key.None ? "" : "Ctrl + Shift + "+label;
            }
        }

        private static Key ChooseKey(Key preferred,HashSet<Key> used)
        {
            if(preferred!=Key.None && !used.Contains(preferred) && PetMenu.KeyAvailable(preferred))
            { used.Add(preferred); return preferred; }
            Key[] alternatives={Key.D1,Key.D2,Key.D3,Key.D4,Key.D5,Key.D6,Key.D7,Key.D8,Key.D9,Key.D0,
                Key.F1,Key.F2,Key.F3,Key.F4,Key.F5,Key.F6,Key.F10,Key.F11,Key.D,Key.G,Key.K,Key.Y};
            foreach(Key key in alternatives)
                if(!used.Contains(key) && PetMenu.KeyAvailable(key)) { used.Add(key); return key; }
            return Key.None;
        }

        private Button AddPetItem(string label,Key key,Action action)
        {
            return AddItem(label,key,()=>{ if(!IsVisible) Show(); action(); });
        }

        private void MenuKey(object sender, KeyEventArgs e)
        {
            // Reconoce el acorde simultáneo y la secuencia manteniendo Ctrl dentro de Marvi.
            if(e.Handled || e.IsRepeat) return;
            if(Keyboard.Modifiers==ModifierKeys.Control)
            {
                if(e.Key==Key.LeftCtrl || e.Key==Key.RightCtrl) return;
                bool fullChord=Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.A) && Keyboard.IsKeyDown(Key.R);
                if(fullChord && (e.Key==Key.M || e.Key==Key.A || e.Key==Key.R))
                {
                    secretProgress=0;
                    ToggleSpecials();
                }
                else if(e.Key==Key.M) secretProgress=1;
                else if(secretProgress==1 && e.Key==Key.A) secretProgress=2;
                else if(secretProgress==2 && e.Key==Key.R)
                {
                    secretProgress=0;
                    ToggleSpecials();
                }
                else { secretProgress=0; return; }
                e.Handled=true;
                return;
            }
            secretProgress=0;
            if(HandleShortcut(e.Key,Keyboard.Modifiers)) e.Handled=true;
        }

        private bool HandleShortcut(Key key, ModifierKeys modifiers)
        {
            if((!menu.IsActive && !IsActive) || modifiers!=(ModifierKeys.Control|ModifierKeys.Shift)) return false;
            if(key==Key.None) return false;
            if(key==flowerKey || key==letterKey)
            {
                if(!inbox.SpecialsUnlocked) return true;
                if(menu.IsVisible) menu.Hide();
                QueueGifts(1,key==letterKey);
                if(!IsVisible) Recall();
                TryStartGift();
                return true;
            }
            if(inbox.SpecialsUnlocked && (key==rereadKey || key==Key.M) && inbox.Latest!=null && presentation==null)
            {
                if(menu.IsVisible) menu.Hide();
                ReadGift(inbox.Latest);
                return true;
            }
            Button item;
            if(!shortcuts.TryGetValue(key,out item)) return false;
            if(!item.IsEnabled || item.Visibility!=Visibility.Visible) return true;
            item.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,item));
            return true;
        }

        private void InitializeShortcuts()
        {
            KeyEventHandler release=(s,e)=>{ if(e.Key==Key.LeftCtrl || e.Key==Key.RightCtrl) secretProgress=0; };
            menu.PreviewKeyUp+=release;
            PreviewKeyUp+=release;
            Deactivated+=(s,e)=>secretProgress=0;
            menu.Deactivated+=(s,e)=>secretProgress=0;
        }
    }
}
