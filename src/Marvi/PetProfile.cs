using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Marvi
{
    [DataContract]
    internal sealed class PetProfile
    {
        [DataMember] public string Name = "Marvi";
        [DataMember] public int Coat;
        public static readonly string[] CoatNames = { "Canela y crema", "Gris azulado", "Chocolate", "Marrón", "Negro", "Gris", "Blanco" };
        public static PetProfile Load(string directory)
        {
            try
            {
                using (var stream = File.OpenRead(Path.Combine(directory,"profile.json")))
                {
                    var profile = (PetProfile)new DataContractJsonSerializer(typeof(PetProfile)).ReadObject(stream);
                    if (profile != null && !String.IsNullOrWhiteSpace(profile.Name) && profile.Name.Length <= 24 &&
                        profile.Coat >= 0 && profile.Coat < CoatNames.Length) { if(profile.Name=="VPet") profile.Name="Marvi"; return profile; }
                }
            }
            catch (IOException) { }
            catch (SerializationException) { }
            catch (UnauthorizedAccessException) { }
            return new PetProfile();
        }

        public void Save(string directory)
        {
            LocalData.Write(Path.Combine(directory, "profile.json"), this);
        }

        public static BitmapImage Sprite(int coat, int pose)
        {
            string prefix = coat == 0 ? "dog" : "coat" + coat;
            var image = new BitmapImage(new Uri("pack://application:,,,/Marvi;component/Assets/" + prefix + "-" + pose + ".png"));
            image.Freeze();
            return image;
        }
    }

    internal sealed class ProfileDialog : Window
    {
        public PetProfile Result { get; private set; }

        public ProfileDialog(PetProfile current,string directory)
        {
            Title = "Mi pitbull";
            Width = 300;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var panel = new StackPanel { Margin = new Thickness(20) };
            Content = panel;
            var portrait = new Image { Width = 144, Height = 144, SnapsToDevicePixels = true };
            RenderOptions.SetBitmapScalingMode(portrait, BitmapScalingMode.NearestNeighbor);
            panel.Children.Add(portrait);
            var name = new TextBox { Text = current.Name, MaxLength = 24, Padding = new Thickness(4) };
            panel.Children.Add(new Label { Content = "_Nombre", Target = name });
            panel.Children.Add(name);
            var coat = new ComboBox { ItemsSource = PetProfile.CoatNames };
            panel.Children.Add(new Label { Content = "_Pelaje", Target = coat });
            panel.Children.Add(coat);
            coat.SelectionChanged += (s, e) => portrait.Source = PetProfile.Sprite(coat.SelectedIndex, 0);
            coat.SelectedIndex = current.Coat;
            var buttons = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1, Margin = new Thickness(0,16,0,0) };
            var cancel = new Button { Content = "Cancelar", IsCancel = true };
            var save = new Button { Content = "Guardar", IsDefault = true };
            buttons.Children.Add(cancel);
            buttons.Children.Add(save);
            panel.Children.Add(buttons);
            name.TextChanged += (s, e) => save.IsEnabled = !String.IsNullOrWhiteSpace(name.Text);
            save.Click += (s, e) =>
            {
                var profile = new PetProfile { Name = name.Text.Trim(), Coat = coat.SelectedIndex };
                try { profile.Save(directory); }
                catch (IOException)
                {
                    MessageBox.Show(this, "No se pudo guardar el perfil. Revisá el acceso a la carpeta de datos de Marvi.");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "Marvi no tiene permiso para guardar el perfil.");
                    return;
                }
                Result = profile;
                DialogResult = true;
            };
        }
    }
}


